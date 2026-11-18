using DG.Tools.XrmMockup.Internal;
using DG.Tools.XrmMockup.Plugin.RegistrationStrategy;
using DG.Tools.XrmMockup.SystemPlugins;
using XrmPluginCore.Enums;
using XrmPluginCore.Interfaces.Plugin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using XrmMockupShared.Plugin;

namespace DG.Tools.XrmMockup
{
    using StageToTriggerMap = Dictionary<ExecutionStage, List<PluginTrigger>>;
    using EventOperation = String;
    using EventOperationEnum = EventOperation;

    internal class PluginManager
    {
        private readonly ILogger _logger;
        private readonly ICoreOperations _core;

        // Static caches shared across all PluginManager instances
        private static readonly ConcurrentDictionary<string, Dictionary<EventOperation, StageToTriggerMap>> _cachedRegisteredPlugins = new ConcurrentDictionary<string, Dictionary<EventOperation, StageToTriggerMap>>();
        private static readonly ConcurrentDictionary<string, Dictionary<EventOperation, StageToTriggerMap>> _cachedSystemPlugins = new ConcurrentDictionary<string, Dictionary<EventOperation, StageToTriggerMap>>();
        private static readonly ConcurrentDictionary<Type, IPlugin> _pluginInstanceCache = new ConcurrentDictionary<Type, IPlugin>();
        private static readonly object _cacheLock = new object();

        // TODO: We can probably optimize lookup by creating a key record and using that instead of a Dictionary of Dictionaries
        private readonly Dictionary<EventOperation, StageToTriggerMap> registeredPlugins = new Dictionary<EventOperation, StageToTriggerMap>();
        private readonly Dictionary<EventOperation, StageToTriggerMap> temporaryPlugins = new Dictionary<EventOperation, StageToTriggerMap>();
        private readonly Dictionary<EventOperation, StageToTriggerMap> registeredSystemPlugins = new Dictionary<EventOperation, StageToTriggerMap>();

        internal readonly List<Type> missingRegistrations = new List<Type>();

        // Queue for AsyncPlugins - using ConcurrentQueue for thread safety in parallel test scenarios
        private readonly ConcurrentQueue<PluginExecutionProvider> pendingAsyncPlugins = new ConcurrentQueue<PluginExecutionProvider>();

        // If true, registered plugins will not be executed
        private bool disableRegisteredPlugins = false;

        // List of SystemPlugins to execute
        private readonly List<AbstractSystemPlugin> systemPlugins = new List<AbstractSystemPlugin>
        {
            new UpdateInactiveIncident(),
            new DefaultBusinessUnitTeams(),
            new DefaultBusinessUnitTeamMembers(),
            new SetAnnotationIsDocument()
        };

        private readonly List<IRegistrationStrategy<IPluginStepConfig>> registrationStrategies;

        public PluginManager(ICoreOperations core, IEnumerable<Type> basePluginTypes, SortedDictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins, ILogger logger = null)
        {
            _core = core;
            _logger = logger ?? NullLogger.Instance;
            registrationStrategies = new List<IRegistrationStrategy<IPluginStepConfig>>
            {
                new Plugin.RegistrationStrategy.XrmPluginCore.PluginRegistrationStrategy(_logger),
                new Plugin.RegistrationStrategy.DAXIF.PluginRegistrationStrategy(_logger)
            };
            temporaryPlugins = new Dictionary<EventOperation, StageToTriggerMap>();

            var pluginCacheKey = GeneratePluginCacheKey(basePluginTypes, plugins);
            var systemCacheKey = "system_plugins";

            // Check if we have cached results
            if (_cachedRegisteredPlugins.ContainsKey(pluginCacheKey) && _cachedSystemPlugins.ContainsKey(systemCacheKey))
            {
                // Use cached results - no reflection/instantiation needed
                registeredPlugins = ClonePluginDictionary(_cachedRegisteredPlugins[pluginCacheKey]);
                registeredSystemPlugins = ClonePluginDictionary(_cachedSystemPlugins[systemCacheKey]);

                _logger.LogDebug("Loaded {Count} plugin registrations from cache", registeredPlugins.Values.SelectMany(s => s.Values.SelectMany(l => l)).Count());
                foreach (var reg in PluginRegistrations)
                {
                    _logger.LogDebug("  Plugin: {Registration}", reg);
                }
            }
            else
            {
                lock (_cacheLock)
                {
                    // Double-check locking pattern
                    if (_cachedRegisteredPlugins.ContainsKey(pluginCacheKey) && _cachedSystemPlugins.ContainsKey(systemCacheKey))
                    {
                        registeredPlugins = ClonePluginDictionary(_cachedRegisteredPlugins[pluginCacheKey]);
                        registeredSystemPlugins = ClonePluginDictionary(_cachedSystemPlugins[systemCacheKey]);

                        _logger.LogDebug("Loaded {Count} plugin registrations from cache", registeredPlugins.Values.SelectMany(s => s.Values.SelectMany(l => l)).Count());
                    }
                    else
                    {
                        // First time - do the work and cache it
                        registeredPlugins = new Dictionary<EventOperation, StageToTriggerMap>();
                        registeredSystemPlugins = new Dictionary<EventOperation, StageToTriggerMap>();

                        _logger.LogInformation("Scanning assemblies for plugin registrations...");

                        RegisterPlugins(basePluginTypes, metadata, plugins, registeredPlugins);
                        RegisterDirectPlugins(basePluginTypes, metadata, plugins, registeredPlugins);
                        RegisterSystemPlugins(registeredSystemPlugins, metadata);

                        // Cache for future instances
                        _cachedRegisteredPlugins[pluginCacheKey] = ClonePluginDictionary(registeredPlugins);
                        _cachedSystemPlugins[systemCacheKey] = ClonePluginDictionary(registeredSystemPlugins);

                        foreach (var reg in PluginRegistrations)
                        {
                            _logger.LogInformation("  Registered plugin: {Registration}", reg);
                        }

                        foreach (var missing in missingRegistrations)
                        {
                            _logger.LogWarning("  Plugin type missing registration: {TypeName}", missing.FullName);
                        }

                        _logger.LogInformation("Plugin scanning complete: {Count} registrations, {MissingCount} missing",
                            PluginRegistrations.Count, missingRegistrations.Count);
                    }
                }
            }
        }

        internal List<string> PluginRegistrations => FlattenPluginDictionary(registeredPlugins);
        internal List<string> TemporaryPluginRegistrations => FlattenPluginDictionary(temporaryPlugins);
        internal List<string> SystemPluginRegistrations => FlattenPluginDictionary(registeredSystemPlugins);

        private static List<string> FlattenPluginDictionary(Dictionary<EventOperation, StageToTriggerMap> plugins)
        {
            return plugins
                .SelectMany(kvpOperation =>
                    kvpOperation.Value.SelectMany(kvpStage =>
                        kvpStage.Value.Select(z =>
                            $"{kvpOperation.Key} {kvpStage.Key} {z.EntityName ?? "AnyEntity"}: {z.PluginExecute.Target.GetType().Name}")))
                .OrderBy(s => s)
                .ToList();
        }

        private void RegisterPlugins(IEnumerable<Type> basePluginTypes, SortedDictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins, Dictionary<EventOperation, StageToTriggerMap> register)
        {
            foreach (var basePluginType in basePluginTypes)
            {
                if (basePluginType == null) continue;

                _logger.LogDebug("Scanning for types extending base plugin type: {BaseType}", basePluginType.FullName);

                // Look for any currently loaded types in the AppDomain that implement the base type
                var pluginTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a =>
                        a.GetLoadableTypes().Where(t =>
                            !t.IsAbstract
                            && t.IsPublic
                            && t.BaseType != null
                            && (t.BaseType == basePluginType || (t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == basePluginType))))
                    .ToList();

                _logger.LogDebug("Found {Count} concrete type(s) extending {BaseType}", pluginTypes.Count, basePluginType.FullName);

                foreach (var type in pluginTypes)
                {
                    RegisterPlugin(type, metadata, plugins, register);
                }
            }
            SortAllLists(register);
        }

        private void RegisterDirectPlugins(IEnumerable<Type> basePluginTypes, SortedDictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins, Dictionary<EventOperation, StageToTriggerMap> register)
        {
            if (basePluginTypes == null) return;

            var scannedAssemblies = new HashSet<Assembly>();

            foreach (var pluginType in basePluginTypes)
            {
                if (pluginType == null) continue;

                Assembly proxyTypeAssembly = pluginType.Assembly;

                if (!scannedAssemblies.Add(proxyTypeAssembly))
                {
                    _logger.LogDebug("Skipping already-scanned assembly {Assembly}", proxyTypeAssembly.GetName().Name);
                    continue;
                }

                _logger.LogDebug("Scanning assembly {Assembly} for direct IPlugin implementations", proxyTypeAssembly.GetName().Name);

                // Look for any currently loaded types in assembly that implement IPlugin
                var types = proxyTypeAssembly.GetLoadableTypes()
                    .Where(t => t.BaseType == typeof(object) && !t.IsAbstract && t.IsPublic && typeof(IPlugin).IsAssignableFrom(t))
                    .ToList();

                _logger.LogDebug("Found {Count} direct IPlugin type(s) in {Assembly}", types.Count, proxyTypeAssembly.GetName().Name);

                foreach (var type in types)
                {
                    RegisterPlugin(type, metadata, plugins, register);
                }
            }
            SortAllLists(register);
        }

        private void RegisterPlugin(Type pluginType, SortedDictionary<string, EntityMetadata> metadata, List<MetaPlugin> metaPlugins, Dictionary<EventOperation, StageToTriggerMap> register)
        {
            _logger.LogDebug("Evaluating plugin type: {TypeName}", pluginType.FullName);

            var plugin = _pluginInstanceCache.GetOrAdd(pluginType, Utility.CreatePluginInstance);
            if (plugin == null)
            {
                _logger.LogWarning("Failed to create instance of plugin type: {TypeName}", pluginType.FullName);
                return;
            }

            // Get the plugin step registrations from the plugin type
            var steps = registrationStrategies
                .SelectMany(s => s.AnalyzeType(plugin));

            // If we didn't find any steps, try the MetadataRegistrationStrategy as a last resort
            if (!steps.Any()) {
                _logger.LogDebug("{TypeName}: no steps from primary strategies, trying MetadataRegistrationStrategy", pluginType.FullName);
                steps = new MetadataRegistrationStrategy(_logger).AnalyzeType(pluginType, metaPlugins);
            }

            var triggers = steps.Select(t => new PluginTrigger(t.EventOperation, t.ExecutionStage, plugin.Execute, t, metadata));

            if (!triggers.Any())
            {
                _logger.LogDebug("{TypeName}: no registrations found from any strategy", pluginType.FullName);
                missingRegistrations.Add(pluginType);
                return;
            }

            foreach (var trigger in triggers)
            {
                AddTrigger(trigger, register);
            }
        }

        public void ResetPlugins()
        {
            disableRegisteredPlugins = false;
            temporaryPlugins.Clear();
        }

        public void DisableRegisteredPlugins(bool disable)
        {
            disableRegisteredPlugins = disable;
        }

        public void RegisterAdditionalPlugin(Type pluginType, SortedDictionary<string, EntityMetadata> metadata, List<MetaPlugin> plugins, PluginRegistrationScope scope)
        {
            if (scope == PluginRegistrationScope.Permanent)
            {
                RegisterPlugin(pluginType, metadata, plugins, registeredPlugins);
            }
            else if (scope == PluginRegistrationScope.Temporary)
            {
                RegisterPlugin(pluginType, metadata, plugins, temporaryPlugins);
            }
        }

        private void RegisterSystemPlugins(Dictionary<EventOperation, StageToTriggerMap> register, SortedDictionary<string, EntityMetadata> metadata)
        {
            var registrations = new List<IPluginStepConfig>();

            // TODO: Auto-discover system plugins instead of hardcoding the types, we know they all implement the same class
            foreach (var plugin in systemPlugins)
            {
                registrations.AddRange(plugin.GetRegistrations());

                // Add discovered plugin triggers
                foreach (var registration in registrations)
                {
                    var trigger = new PluginTrigger(registration.EventOperation, registration.ExecutionStage, plugin.Execute, registration, metadata);
                    AddTrigger(trigger, register);
                }
            }

            SortAllLists(register);
        }

        private static void AddTrigger(PluginTrigger trigger, Dictionary<EventOperation, StageToTriggerMap> register)
        {
            var operation = trigger.Operation;
            var stage = trigger.Stage;

            if (!register.TryGetValue(operation, out var operationRegister))
            {
                register[operation] = operationRegister = new StageToTriggerMap();
            }

            if (!operationRegister.TryGetValue(stage, out var stageRegister))
            {
                operationRegister[stage] = stageRegister = new List<PluginTrigger>();
            }

            stageRegister.Add(trigger);
        }

        /// <summary>
        /// Sorts all the registered which shares the same entry point based on their given order.
        /// Uses a stable sort so plugins with equal ExecutionOrder preserve their registration order.
        /// </summary>
        private static void SortAllLists(Dictionary<EventOperation, StageToTriggerMap> plugins)
        {
            foreach (var dictEntry in plugins)
            {
                foreach (var listEntry in dictEntry.Value)
                {
                    var sorted = listEntry.Value.OrderBy(t => t.Order).ToList();
                    listEntry.Value.Clear();
                    listEntry.Value.AddRange(sorted);
                }
            }
        }

        public void TriggerSync(string operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext, Func<PluginTrigger, bool> executionOrderFilter,
                ISet<string> systemInjectedAttributes = null)
        {
            TriggerSyncInternal(operation, stage, entity, preImage, postImage, pluginContext, executionOrderFilter, systemInjectedAttributes);

            // Check if this is a Single -> Multiple request
            var isKnownOp = Enum.TryParse<EventOperationEnum>(operation, out var knownOp);
            if (isKnownOp && Mappings.RequestToMultipleRequest.TryGetValue(knownOp, out var multipleOperation))
            {
                var multiplePluginContext = pluginContext.Clone();
                var entityCollection = new EntityCollection()
                {
                    EntityName = pluginContext.PrimaryEntityName,
                    Entities = { (Entity)entity },
                };

                // Try to get the request type for the multiple
                var multipleRequestType = Mappings.EventOperationToRequest(multipleOperation)
                    ?? throw new MockupException($"Could not find request type for operation {multipleOperation}");

                // Now try to get the image property for the multiple request
                if (!Mappings.EntityImageProperty.TryGetValue(multipleRequestType, out var imageProperty))
                {
                    throw new MockupException($"Could not find image property for operation {multipleOperation} using request type {multipleRequestType}");
                }

                multiplePluginContext.InputParameters[imageProperty] = entityCollection;
                multiplePluginContext.MessageName = Activator.CreateInstance(multipleRequestType) is OrganizationRequest request
                    ? request.RequestName
                    : throw new MockupException($"Could not create request for operation {operation}");

                // Populate IPluginExecutionContext4 image collections
                multiplePluginContext.PreEntityImagesCollection = new[] {
                    preImage != null ? new EntityImageCollection { { "PreImage", preImage } } : new EntityImageCollection()
                };
                multiplePluginContext.PostEntityImagesCollection = new[] {
                    postImage != null ? new EntityImageCollection { { "PostImage", postImage } } : new EntityImageCollection()
                };


                TriggerSyncInternal(multipleOperation.ToString(), stage, entityCollection, null, null, multiplePluginContext, executionOrderFilter, systemInjectedAttributes);
            }

            // Check if this is a Multiple -> Single request
            if (isKnownOp && Mappings.SingleOperationFromMultiple(knownOp) is EventOperationEnum singleOperation)
            {
                // Try to get the request type for the multiple
                var multipleRequestType = Mappings.EventOperationToRequest(knownOp)
                    ?? throw new MockupException($"Could not find request type for operation {operation}");

                // Now try to get the image property for the multiple request
                if (!Mappings.EntityImageProperty.TryGetValue(multipleRequestType, out var multipleImageProperty))
                {
                    throw new MockupException($"Could not find image property for operation {operation} using request type {multipleRequestType}");
                }

                // Try to get the request type for the single
                var singleRequestType = Mappings.EventOperationToRequest(singleOperation)
                    ?? throw new MockupException($"Could not find request type for operation {singleOperation}");

                // Now try to get the image property for the single request
                if (!Mappings.EntityImageProperty.TryGetValue(singleRequestType, out var singleImageProperty))
                {
                    throw new MockupException($"Could not find image property for operation {singleOperation} using request type {singleRequestType}");
                }

                // Get the message for the single message
                var singleMessageName = Activator.CreateInstance(singleRequestType) is OrganizationRequest request
                    ? request.RequestName
                    : throw new MockupException($"Could not create request for operation {operation}");

                var targets = pluginContext.InputParameters[multipleImageProperty] as EntityCollection;

                for (int i = 0; i < targets.Entities.Count; i++)
                {
                    var targetEntity = targets.Entities[i];
                    var singlePluginContext = pluginContext.Clone();
                    singlePluginContext.InputParameters[singleImageProperty] = targetEntity;
                    singlePluginContext.MessageName = singleMessageName;

                    // Extract per-entity images from IPluginExecutionContext4 collections
                    var entityPreImage = pluginContext.PreEntityImagesCollection.Length > i
                        && pluginContext.PreEntityImagesCollection[i].TryGetValue("PreImage", out var pre) ? pre : preImage;
                    var entityPostImage = pluginContext.PostEntityImagesCollection.Length > i
                        && pluginContext.PostEntityImagesCollection[i].TryGetValue("PostImage", out var post) ? post : postImage;

                    TriggerSyncInternal(singleOperation.ToString(), stage, targetEntity, entityPreImage, entityPostImage, singlePluginContext, executionOrderFilter, systemInjectedAttributes);
                }
            }
        }

        private void TriggerSyncInternal(EventOperation operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext, Func<PluginTrigger, bool> executionOrderFilter,
                ISet<string> systemInjectedAttributes = null)
        {
            if (!disableRegisteredPlugins && registeredPlugins.TryGetValue(operation, out var operationPlugins) && operationPlugins.TryGetValue(stage, out var stagePlugins))
                stagePlugins
                    .Where(p => p.GetExecutionMode() == ExecutionMode.Synchronous)
                    .Where(executionOrderFilter)
                    .OrderBy(p => p.GetExecutionOrder())
                    .ToList()
                    .ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, _core, systemInjectedAttributes: systemInjectedAttributes));

            if (temporaryPlugins.TryGetValue(operation, out var tempOperationPlugins) && tempOperationPlugins.TryGetValue(stage, out var tempStagePlugins))
                tempStagePlugins
                    .Where(p => p.GetExecutionMode() == ExecutionMode.Synchronous)
                    .Where(executionOrderFilter)
                    .OrderBy(p => p.GetExecutionOrder())
                    .ToList()
                    .ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, _core, systemInjectedAttributes: systemInjectedAttributes));
        }

        public void StageAsync(EventOperation operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext,
                ISet<string> systemInjectedAttributes = null)
        {
            if (!disableRegisteredPlugins && registeredPlugins.TryGetValue(operation, out var operationPlugins) && operationPlugins.TryGetValue(stage, out var stagePlugins))
                stagePlugins
                    .Where(p => p.GetExecutionMode() == ExecutionMode.Asynchronous)
                    .OrderBy(p => p.GetExecutionOrder())
                    .Select(p => p.ToPluginExecution(entity, preImage, postImage, pluginContext, _core, systemInjectedAttributes))
                    .ToList()
                    .ForEach(pendingAsyncPlugins.Enqueue);

            if (temporaryPlugins.TryGetValue(operation, out var tempOperationPlugins) && tempOperationPlugins.TryGetValue(stage, out var tempStagePlugins))
                tempStagePlugins
                    .Where(p => p.GetExecutionMode() == ExecutionMode.Asynchronous)
                    .OrderBy(p => p.GetExecutionOrder())
                    .Select(p => p.ToPluginExecution(entity, preImage, postImage, pluginContext, _core, systemInjectedAttributes))
                    .ToList()
                    .ForEach(pendingAsyncPlugins.Enqueue);
        }

        public void TriggerAsyncWaitingJobs()
        {
            while (pendingAsyncPlugins.TryDequeue(out var pendingPlugin))
            {
                pendingPlugin?.ExecuteAction();
            }
        }

        public void TriggerSystem(EventOperation operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext,
                ISet<string> systemInjectedAttributes = null)
        {
            if (!registeredSystemPlugins.TryGetValue(operation, out var stagePlugins))
            {
                return;
            }

            if (!stagePlugins.TryGetValue(stage, out var plugins))
            {
                return;
            }

            // System plugins are XrmMockup's own internal simulation, not user-registered steps,
            // so they are excluded from the grouped plugin trace log.
            plugins.ForEach(p => p.ExecuteIfMatch(entity, preImage, postImage, pluginContext, _core, recordTrace: false, systemInjectedAttributes: systemInjectedAttributes));
        }

        private string GeneratePluginCacheKey(IEnumerable<Type> basePluginTypes, IEnumerable<MetaPlugin> plugins)
        {
            var typeNames = (basePluginTypes ?? Enumerable.Empty<Type>())
                .Where(t => t != null).Select(t => t.FullName).OrderBy(n => n);

            // The registered plugin set depends on the supplied plugin metadata (e.g. IPluginMetadata),
            // not just the base types. Two settings with the same base types but different plugin
            // metadata must NOT share a cache entry, otherwise the registrations leak between them
            // depending on which Core is constructed first (order-dependent failures).
            var pluginKeys = (plugins ?? Enumerable.Empty<MetaPlugin>())
                .Select(p => string.Join("~",
                    p.PluginTypeAssemblyName, p.AssemblyName, p.MessageName, p.PrimaryEntity,
                    p.Stage, p.Rank, p.Mode, p.FilteredAttributes, p.AsyncAutoDelete, p.ImpersonatingUserId,
                    p.Images == null ? "" : string.Join(",", p.Images
                        .Select(i => $"{i.Name}/{i.EntityAlias}/{i.ImageType}/{i.Attributes}").OrderBy(s => s))))
                .OrderBy(s => s);

            var combined = string.Join("|", typeNames) + "##" + string.Join("|", pluginKeys);

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                return Convert.ToBase64String(hash);
            }
        }

        private Dictionary<EventOperation, StageToTriggerMap> ClonePluginDictionary(Dictionary<EventOperation, StageToTriggerMap> source)
        {
            var result = new Dictionary<EventOperation, StageToTriggerMap>();
            foreach (var operationEntry in source)
            {
                var stageDict = new StageToTriggerMap();
                foreach (var stageEntry in operationEntry.Value)
                {
                    stageDict[stageEntry.Key] = new List<PluginTrigger>(stageEntry.Value);
                }
                result[operationEntry.Key] = stageDict;
            }
            return result;
        }
    }
}
