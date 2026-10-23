using DG.Tools.XrmMockup.Database;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;

namespace DG.Tools.XrmMockup.Internal
{
    /// <summary>
    /// Infrastructure contract that Core exposes to the pipeline and its managers.
    /// The pipeline orchestrates execution stages; Core provides the underlying services.
    /// PluginManager, WorkflowManager, and PluginTrigger also depend on this interface
    /// so they can be used from the pipeline without a direct Core reference.
    /// </summary>
    internal interface ICoreOperations
    {
        // Identity
        Guid OrganizationId { get; }
        string OrganizationName { get; }

        // Time — used by workflow execution and formula fields
        TimeSpan TimeOffset { get; }

        // DB helpers
        Entity TryRetrieve(EntityReference reference);
        DbRow GetDbRow(EntityReference reference);
        EntityReference GetBusinessUnit(EntityReference owner);
        EntityMetadata GetEntityMetadata(string logicalName);

        // Pre-context setup
        void HandleInternalPreOperations(OrganizationRequest request, EntityReference userRef);

        // Post-operation image helper — returns the attributes it added to the target
        ISet<string> CopySystemAttributes(Entity postImage, Entity target);

        // Request handler list — used by pipeline for security check and pre-op init
        List<RequestHandler> RequestHandlers { get; }

        // Recursive entry point for nested requests (Assign → Update, SetState → Update)
        OrganizationResponse Execute(OrganizationRequest request, EntityReference userRef, PluginContext parentPluginContext);

        // Dispatch helpers — Core owns the managers so the pipeline delegates through these
        OrganizationResponse ExecuteAction(OrganizationRequest request);
        bool HandlesCustomApi(string requestName);
        IEnumerable<KeyValuePair<string, object>> GetCustomApiInputParameters(OrganizationRequest request);
        OrganizationResponse ExecuteCustomApi(OrganizationRequest request, PluginContext pluginContext);
        bool IsExceptionFreeRequest(string requestName);

        // Extension stage support
        bool HasMockupExtensions { get; }
        IOrganizationService CreateMockupService(Guid? userId, PluginContext pluginContext);
        void TriggerExtension(IOrganizationService service, OrganizationRequest request, Entity entity, Entity preImage, EntityReference userRef);

        // Service factories — used by PluginTrigger and WorkflowManager to create execution providers
        MockupServiceProviderAndFactory ServiceFactory { get; }
        ITracingServiceFactory TracingServiceFactory { get; }
        MockupServiceProviderAndFactory CreateServiceProviderAndFactory(PluginContext pluginContext);

        // Grouped trace log — PluginTrigger records one entry per plugin execution
        void RecordPluginTrace(PluginTraceLog log);

        // Settings — used by WorkflowManager
        XrmMockupSettings GetMockupSettings();
    }
}
