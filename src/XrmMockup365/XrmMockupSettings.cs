using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk.Organization;
using System.Reflection;
#if DATAVERSE_SERVICE_CLIENT
using DG.Tools.XrmMockup.Online;
#endif

namespace DG.Tools.XrmMockup
{
    /// <summary>
    /// Settings for XrmMockup instance
    /// </summary>
    public class XrmMockupSettings
    {
        /// <summary>
        /// List of base-types which all your plugins extend.
        /// This is used to locate the assemblies required.
        /// </summary>
        public IEnumerable<Type> BasePluginTypes { get; set; }

        /// <summary>
        /// List of at least one instance of a CodeActivity in each of your projects that contain CodeActivities. 
        /// This is used to locate the assemblies required to find all CodeActivity.
        /// </summary>
        public IEnumerable<Type> CodeActivityInstanceTypes { get; set; }

        /// <summary>
        /// Enable early-bound proxy types.
        /// </summary>
        public bool? EnableProxyTypes { get; set; }

        /// <summary>
        /// Sets whether all workflow definitions should be included on startup. Default is true.
        /// </summary>
        public bool? IncludeAllWorkflows { get; set; }

        /// <summary>
        /// Sets whether workflows should be triggered during execution. Default is true.
        /// </summary>
        public bool? TriggerWorkflows { get; set; }

        /// <summary>
        /// List of request that will not throw exceptions.
        /// </summary>
        public IEnumerable<string> ExceptionFreeRequests { get; set; }

#if DATAVERSE_SERVICE_CLIENT
        /// <summary>
        /// Settings for connecting to an online Dataverse environment for live debugging.
        /// Uses Azure DefaultAzureCredential for authentication (supports managed identity,
        /// Visual Studio credentials, Azure CLI, etc.).
        /// </summary>
        public Env? OnlineEnvironment { get; set; }

        /// <summary>
        /// Optional factory for creating IOnlineDataService. For testing purposes.
        /// If set, this takes precedence over OnlineEnvironment.
        /// </summary>
        internal Func<IOnlineDataService> OnlineDataServiceFactory { get; set; }
#endif

        /// <summary>
        /// Overwrites the path to the directory containing metadata files. Default is '../../Metadata/'.
        /// </summary>
        public string MetadataDirectoryPath { get; set; }

        /// <summary>
        /// Flag for if Append And Append To privilege should be check on create and update. Default is true
        /// </summary>
        public bool? AppendAndAppendToPrivilegeCheck { get; set; }

        /// <summary>
        /// Additional Plugin Metatdata for IPlugin direct plugin registration
        /// </summary>
        public MetaPlugin[] IPluginMetadata { get; set; }

        /// <summary>
        /// <para>Optional factory for creating new instances of ITracingService.</para>
        /// <para>If not specified, uses the built-in <see cref="TracingService"/></para>
        /// </summary>
        public ITracingServiceFactory TracingServiceFactory { get; set; }

        /// <summary>
        /// List of Extensions to XrmMockup. This can be used to extend XrmMockup functionality to a certain degree.
        /// </summary>
        public List<IXrmMockupExtension> MockUpExtensions { get; set; } = new List<IXrmMockupExtension>();

        /// <summary>
        /// Optional configuration required for RetrieveCurrenctOrganizationRequest.
        /// </summary>
        public OrganizationDetail OrganizationDetail { get; set; }

        /// <summary>
        /// List of base-types with their prefix which all your custom apis extend.
        /// This is used to locate the assemblies required.
        /// </summary>
        public IEnumerable<Tuple<string, Type>> BaseCustomApiTypes { get; set; }

        /// <summary>
        /// Optional:
        /// Proxy type assemblies to load. Used for specifying specific context types.
        /// If left empty, assemblies are found relative to dll.
        /// </summary>
        public IEnumerable<Assembly> Assemblies { get; set; }

        /// <summary>
        /// Optional:
        /// Enable the evaluation of PowerFx fields.
        /// Default is true.
        /// </summary>
        public bool EnablePowerFxFields { get; set; } = true;

        /// <summary>
        /// Optional:
        /// Throw a <see cref="MockupException"/> when a Custom API is called with a request parameter that is not registered,
        /// or sets an output parameter that is not a registered response property.
        /// When disabled, such parameters are left out and a warning is logged.
        /// Default is false.
        /// </summary>
        public bool StrictCustomApiParameters { get; set; }

        /// <summary>
        /// Optional file path for diagnostic logging. When set, XrmMockup writes startup
        /// information (discovered plugins, workflows, custom APIs) to this file.
        /// </summary>
        public string LogFilePath { get; set; }

        /// <summary>
        /// Minimum log level for the built-in file logger. Default is <see cref="LogLevel.Information"/>.
        /// Only applies when <see cref="LogFilePath"/> is set. Ignored when <see cref="LoggerFactory"/> is provided,
        /// as the custom factory controls its own filtering.
        /// </summary>
        public LogLevel MinLogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Optional logger factory for diagnostic logging. When set, takes precedence
        /// over <see cref="LogFilePath"/>. Use this to integrate with your own logging infrastructure.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; set; }
    }

#if DATAVERSE_SERVICE_CLIENT
    /// <summary>
    /// Settings for connecting to an online Dataverse environment.
    /// </summary>
    public struct Env
    {
        /// <summary>
        /// URL of the Dataverse environment (e.g., https://org.crm.dynamics.com).
        /// Uses Azure DefaultAzureCredential for authentication.
        /// </summary>
        public string Url;
    }
#endif
}
