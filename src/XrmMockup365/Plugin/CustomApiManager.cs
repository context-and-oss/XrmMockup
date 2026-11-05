using DG.Tools.XrmMockup.Internal;
using DG.Tools.XrmMockup.Plugin.RegistrationStrategy;
using XrmPluginCore.Enums;
using XrmPluginCore.Interfaces.CustomApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;

namespace DG.Tools.XrmMockup
{
    internal class CustomApiManager
    {
        private readonly ILogger _logger;
        private readonly Core _core;
        private readonly bool _strictParameters;

        // Static caches shared across all CustomApiManager instances
        private static readonly ConcurrentDictionary<string, Dictionary<string, RegisteredApi>> _cachedApis = new ConcurrentDictionary<string, Dictionary<string, RegisteredApi>>();
        private static readonly ConcurrentDictionary<Type, IPlugin> _apiInstanceCache = new ConcurrentDictionary<Type, IPlugin>();
        private static readonly object _cacheLock = new object();

        private class RegisteredApi
        {
            public Action<MockupServiceProviderAndFactory> Execute { get; set; }
            public HashSet<string> RequestParameters { get; set; }
            public HashSet<string> ResponseProperties { get; set; }
        }

        // Optional parameters accepted on any request, which are not part of a Custom API registration
        // https://learn.microsoft.com/power-apps/developer/data-platform/optional-parameters
        private static readonly HashSet<string> _commonRequestParameters = new HashSet<string>
        {
            MockupExecutionContext.Key,
            RequestExecutionPipeline.TagParameter,
            "BypassCustomPluginExecution",
            "BypassBusinessLogicExecution",
            "BypassBusinessLogicExecutionStepIds",
            "SuppressCallbackRegistrationExpanderJob",
        };

        internal List<Type> missingRegistration = new List<Type>();
        private Dictionary<string, RegisteredApi> registeredApis = new Dictionary<string, RegisteredApi>();

        internal int RegisteredApiCount => registeredApis.Count;

        private readonly List<IRegistrationStrategy<ICustomApiConfig>> registrationStrategies;

        public CustomApiManager(Core core, IEnumerable<Tuple<string, Type>> baseCustomApiTypes, bool strictParameters = false, ILogger logger = null)
        {
            _core = core;
            _strictParameters = strictParameters;
            _logger = logger ?? NullLogger.Instance;
            registrationStrategies = new List<IRegistrationStrategy<ICustomApiConfig>>
            {
                new Plugin.RegistrationStrategy.XrmPluginCore.CustomApiRegistrationStrategy(_logger),
                new Plugin.RegistrationStrategy.DAXIF.CustomApiRegistrationStrategy(_logger)
            };
            var cacheKey = GenerateApiCacheKey(baseCustomApiTypes);

            // Check if we have cached results
            if (_cachedApis.ContainsKey(cacheKey))
            {
                // Use cached results - no reflection/instantiation needed
                registeredApis = new Dictionary<string, RegisteredApi>(_cachedApis[cacheKey]);
                _logger.LogDebug("Loaded {Count} custom API registrations from cache", registeredApis.Count);
                return;
            }

            lock (_cacheLock)
            {
                // Double-check locking pattern
                if (_cachedApis.ContainsKey(cacheKey))
                {
                    registeredApis = new Dictionary<string, RegisteredApi>(_cachedApis[cacheKey]);
                    _logger.LogDebug("Loaded {Count} custom API registrations from cache", registeredApis.Count);
                    return;
                }

                // First time - do the work and cache it
                registeredApis = new Dictionary<string, RegisteredApi>();
                if (baseCustomApiTypes != null)
                {
                    RegisterCustomApis(baseCustomApiTypes);
                }

                // Cache for future instances
                _cachedApis[cacheKey] = new Dictionary<string, RegisteredApi>(registeredApis);

                foreach (var apiKey in registeredApis.Keys)
                {
                    _logger.LogInformation("  Registered custom API: {ApiKey}", apiKey);
                }

                foreach (var missing in missingRegistration)
                {
                    _logger.LogWarning("  Custom API type missing registration: {TypeName}", missing.FullName);
                }

                _logger.LogInformation("Custom API scanning complete: {Count} registrations, {MissingCount} missing",
                    registeredApis.Count, missingRegistration.Count);
            }
        }

        private void RegisterCustomApis(IEnumerable<Tuple<string, Type>> customApiBaseTypeMappings)
        {
            foreach (var customApiMapping in customApiBaseTypeMappings)
            {
                if (customApiMapping == null) continue;

                var prefix = customApiMapping.Item1;
                var baseApiType = customApiMapping.Item2;

                _logger.LogDebug("Scanning for types extending custom API base type: {BaseType} (prefix={Prefix})", baseApiType.FullName, prefix);

                var customApiTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetLoadableTypes().Where(t => !t.IsAbstract && t.IsPublic && t.BaseType != null && (t.BaseType == baseApiType || (t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == baseApiType))))
                    .ToList();

                _logger.LogDebug("Found {Count} concrete type(s) extending {BaseType}", customApiTypes.Count, baseApiType.FullName);

                foreach (var type in customApiTypes)
                {
                    RegisterApi(prefix, type);
                }
            }
        }

        private void RegisterApi(string prefix, Type pluginType)
        {
            _logger.LogDebug("Evaluating custom API type: {TypeName} (prefix={Prefix})", pluginType.FullName, prefix);

            var plugin = _apiInstanceCache.GetOrAdd(pluginType, Utility.CreatePluginInstance);

            if (plugin == null)
            {
                _logger.LogWarning("Failed to create instance of custom API type: {TypeName}", pluginType.FullName);
                return;
            }

            var registration = registrationStrategies
                .SelectMany(s => s.AnalyzeType(plugin))
                .SingleOrDefault();

            if (registration is null)
            {
                _logger.LogDebug("{TypeName}: no custom API registration found from any strategy", pluginType.FullName);
                missingRegistration.Add(pluginType);
            }
            else
            {
                var key = $"{prefix}_{registration.UniqueName}";
                _logger.LogDebug("{TypeName}: registered as '{ApiKey}'", pluginType.FullName, key);

                var requestParameters = new HashSet<string>(
                    registration.RequestParameters?.Select(r => r.UniqueName) ?? Enumerable.Empty<string>());
                // Entity-bound APIs receive the bound record as an implicit Target parameter
                if (registration.BindingType == BindingType.Entity)
                {
                    requestParameters.Add("Target");
                }

                registeredApis.Add(key, new RegisteredApi
                {
                    Execute = plugin.Execute,
                    RequestParameters = requestParameters,
                    ResponseProperties = new HashSet<string>(
                        registration.ResponseProperties?.Select(r => r.UniqueName) ?? Enumerable.Empty<string>())
                });
            }
        }

        public bool HandlesRequest(string requestName)
        {
            return registeredApis.ContainsKey(requestName);
        }

        internal IEnumerable<KeyValuePair<string, object>> GetInputParameters(OrganizationRequest request)
        {
            var api = registeredApis[request.RequestName];
            foreach (var parameter in request.Parameters)
            {
                if (api.RequestParameters.Contains(parameter.Key) || _commonRequestParameters.Contains(parameter.Key))
                {
                    yield return parameter;
                }
                else
                {
                    ReportUnregisteredParameter(
                        $"Custom API '{request.RequestName}' was called with request parameter '{parameter.Key}' which is not a registered request parameter.",
                        "It is not included in the input parameters.");
                }
            }
        }

        internal OrganizationResponse Execute(OrganizationRequest request, PluginContext pluginContext)
        {
            if (!registeredApis.ContainsKey(request.RequestName))
            {
                throw new FaultException($"Request '{request.RequestName}' is not a registered Custom API");
            }

            var thisPluginContext = pluginContext.Clone();
            thisPluginContext.Stage = 30;
            thisPluginContext.Mode = 0;

            var serviceProvider = new MockupServiceProviderAndFactory(_core, thisPluginContext, _core.TracingServiceFactory);
            var api = registeredApis[request.RequestName];
            PluginTraceRecorder.Run(
                _core,
                serviceProvider,
                thisPluginContext,
                typeName: api.Execute.Target?.GetType().FullName,
                messageName: request.RequestName,
                primaryEntity: thisPluginContext.PrimaryEntityName,
                operationType: PluginTraceOperationType.Plugin,
                mode: XrmPluginCore.Enums.ExecutionMode.Synchronous,
                execute: () => api.Execute(serviceProvider),
                unwrapTargetInvocation: false,
                record: true);

            // Dataverse only returns output parameters registered as response properties
            pluginContext.OutputParameters.Clear();
            foreach (var output in thisPluginContext.OutputParameters)
            {
                if (api.ResponseProperties.Contains(output.Key))
                {
                    pluginContext.OutputParameters[output.Key] = output.Value;
                }
                else
                {
                    ReportUnregisteredParameter(
                        $"Custom API '{request.RequestName}' set output parameter '{output.Key}' which is not a registered response property.",
                        "It is not included in the response.");
                }
            }

            return new OrganizationResponse()
            {
                Results = pluginContext.OutputParameters
            };
        }

        private void ReportUnregisteredParameter(string message, string consequence)
        {
            if (_strictParameters)
            {
                throw new MockupException(message);
            }

            _logger.LogWarning("{Message} {Consequence}", message, consequence);
        }

        private string GenerateApiCacheKey(IEnumerable<Tuple<string, Type>> baseCustomApiTypes)
        {
            if (baseCustomApiTypes == null) return "null_types";

            var typeKeys = baseCustomApiTypes.Where(t => t != null && t.Item2 != null)
                .Select(t => $"{t.Item1}:{t.Item2.FullName}")
                .OrderBy(k => k);
            var combinedTypes = string.Join("|", typeKeys);

            if (string.IsNullOrEmpty(combinedTypes)) return "empty_types";

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedTypes));
                return Convert.ToBase64String(hash);
            }
        }
    }
}