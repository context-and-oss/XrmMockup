using DG.Tools.XrmMockup.Internal;
using XrmPluginCore.Enums;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    /// <summary>
    /// Orchestrates the Dataverse request execution pipeline across its six stages:
    /// Setup → PreValidation → PreOperation → MainOperation → PostOperation → Extensions.
    ///
    /// Core provides infrastructure (DB, security, extensions).
    /// PluginManager and WorkflowManager drive what fires at each stage.
    /// </summary>
    internal class RequestExecutionPipeline
    {
        // Optional request parameter which is passed to plugins as a shared variable
        internal const string TagParameter = "tag";

        private readonly ICoreOperations core;
        private readonly PluginManager pluginManager;
        private readonly WorkflowManager workflowManager;

        public RequestExecutionPipeline(ICoreOperations core, PluginManager pluginManager, WorkflowManager workflowManager)
        {
            this.core = core;
            this.pluginManager = pluginManager;
            this.workflowManager = workflowManager;
        }

        public OrganizationResponse Execute(OrganizationRequest request, EntityReference userRef,
            PluginContext parentPluginContext)
        {
            var ctx = BuildPipelineContext(request, userRef, parentPluginContext);
            ExecutePreValidationStage(ctx);
            ExecuteSecurityAndInit(ctx);
            ExecutePreOperationStage(ctx);
            ctx.Response = DispatchRequest(ctx);
            ExecutePostOperationStage(ctx);
            ExecuteExtensionStage(ctx);
            return ctx.Response;
        }

        // ── Stage 1: build context ────────────────────────────────────────────────

        private ExecutionPipelineContext BuildPipelineContext(OrganizationRequest request, EntityReference userRef,
            PluginContext parentPluginContext)
        {
            core.HandleInternalPreOperations(request, userRef);

            var primaryRef = Mappings.GetPrimaryEntityReferenceFromRequest(request);

            var pluginContext = new PluginContext()
            {
                UserId = userRef.Id,
                InitiatingUserId = userRef.Id,
                MessageName = RequestNameToMessageName(request.RequestName),
                Depth = 1,
                ExtensionDepth = 1,
                OrganizationName = core.OrganizationName,
                OrganizationId = core.OrganizationId,
                PrimaryEntityName = primaryRef?.LogicalName,
                IsPortalsClientCall = false,
                IsApplicationUser = false,
                AuthenticatedUserId = userRef.Id,
            };

            if (primaryRef != null)
            {
                var existingEntity = core.TryRetrieve(primaryRef);
                pluginContext.PrimaryEntityId = existingEntity == null ? Guid.Empty : existingEntity.Id;
            }

            var inputParameters = core.HandlesCustomApi(request.RequestName)
                ? core.GetCustomApiInputParameters(request)
                : request.Parameters;
            foreach (var prop in inputParameters)
            {
                if (prop.Key == TagParameter)
                    pluginContext.SharedVariables[TagParameter] = prop.Value;
                else
                    pluginContext.InputParameters[prop.Key] = prop.Value;
            }

            if (parentPluginContext != null)
            {
                pluginContext.ParentContext = parentPluginContext;
                pluginContext.Depth = parentPluginContext.Depth + 1;
                pluginContext.ExtensionDepth = parentPluginContext.ExtensionDepth + 1;
                parentPluginContext.ExtensionDepth = pluginContext.ExtensionDepth;
            }

            pluginContext.BusinessUnitId = core.GetBusinessUnit(userRef).Id;

            var requestMessage = Mappings.RequestToEventOperation.TryGetValue(request.GetType(), out var eventOperation)
                ? eventOperation.ToString()
                : request.RequestName;

            var entityInfo = GetEntityInfo(request);

            var requestSettings = MockupExecutionContext.GetSettings(request);
            if (!requestSettings.SetUnsettableFields && (request is UpdateRequest || request is CreateRequest))
            {
                var entity = request is UpdateRequest
                    ? (request as UpdateRequest).Target
                    : (request as CreateRequest).Target;
                Utility.RemoveUnsettableAttributes(request.RequestName,
                    core.GetEntityMetadata(entity.LogicalName), entity);
            }

            var entityCollection = entityInfo?.Item1 as EntityCollection;

            var preImage = core.TryRetrieve(primaryRef);
            if (preImage != null)
                primaryRef.Id = preImage.Id;

            // Populate IPluginExecutionContext4 pre-images collection for Multiple operations
            if (entityCollection != null)
            {
                pluginContext.PreEntityImagesCollection = entityCollection.Entities
                    .Select(e =>
                    {
                        var img = new EntityImageCollection();
                        var pre = e.Id != Guid.Empty ? core.TryRetrieve(e.ToEntityReference()) : null;
                        if (pre != null) img["PreImage"] = pre;
                        return img;
                    }).ToArray();
            }

            return new ExecutionPipelineContext
            {
                Request = request,
                UserRef = userRef,
                ParentPluginContext = parentPluginContext,
                Settings = requestSettings,
                PluginContext = pluginContext,
                RequestMessage = requestMessage,
                EntityInfo = entityInfo,
                PrimaryRef = primaryRef,
                EntityCollection = entityCollection,
                ShouldTrigger = requestSettings.TriggerProcesses && entityInfo != null,
                PreImage = preImage,
            };
        }

        // ── Stage 2: PreValidation (stage 10) ────────────────────────────────────

        private void ExecutePreValidationStage(ExecutionPipelineContext ctx)
        {
            if (!ctx.ShouldTrigger) return;

            pluginManager.TriggerSystem(ctx.RequestMessage, ExecutionStage.PreValidation,
                ctx.EntityInfo.Item1, ctx.PreImage, null, ctx.PluginContext);
            pluginManager.TriggerSync(ctx.RequestMessage, ExecutionStage.PreValidation,
                ctx.EntityInfo.Item1, ctx.PreImage, null, ctx.PluginContext, (_) => true);
        }

        // ── Stage 3: security check + attribute initialisation ───────────────────

        private void ExecuteSecurityAndInit(ExecutionPipelineContext ctx)
        {
            var handler = core.RequestHandlers.FirstOrDefault(x => x.HandlesRequest(ctx.Request.RequestName));
            handler?.CheckSecurity(ctx.Request, ctx.UserRef);
            handler?.InitializePreOperation(ctx.Request, ctx.UserRef, ctx.PreImage);
        }

        // ── Stage 4: PreOperation (stage 20) ─────────────────────────────────────

        private void ExecutePreOperationStage(ExecutionPipelineContext ctx)
        {
            if (!ctx.ShouldTrigger) return;

            // Shared variables move to the parent context when transitioning stage 10 → stage 20
            ctx.PluginContext.ParentContext = ctx.PluginContext.Clone();
            ctx.PluginContext.SharedVariables.Clear();
            if (ctx.Request.Parameters.TryGetValue(TagParameter, out var tag))
                ctx.PluginContext.SharedVariables[TagParameter] = tag;

            pluginManager.TriggerSync(ctx.RequestMessage, ExecutionStage.PreOperation,
                ctx.EntityInfo.Item1, ctx.PreImage, null, ctx.PluginContext,
                p => p.GetExecutionOrder() == 0);

            if (ctx.Settings.TriggerWorkflows)
                workflowManager.TriggerSync(ctx.RequestMessage, ExecutionStage.PreOperation,
                    ctx.EntityInfo.Item1, ctx.PreImage, null, ctx.PluginContext);

            pluginManager.TriggerSync(ctx.RequestMessage, ExecutionStage.PreOperation,
                ctx.EntityInfo.Item1, ctx.PreImage, null, ctx.PluginContext,
                p => p.GetExecutionOrder() != 0);

            pluginManager.TriggerSystem(ctx.RequestMessage, ExecutionStage.PreOperation,
                ctx.EntityInfo.Item1, ctx.PreImage, null, ctx.PluginContext);
        }

        // ── Stage 5: main operation (stage 30) ───────────────────────────────────

        private OrganizationResponse DispatchRequest(ExecutionPipelineContext ctx)
        {
            var request = ctx.Request;
            var userRef = ctx.UserRef;

            if (request is AssignRequest assignRequest)
            {
                var targetEntity = core.TryRetrieve(assignRequest.Target);
                if (targetEntity?.GetAttributeValue<EntityReference>("ownerid") != assignRequest.Assignee)
                {
                    var req = new UpdateRequest
                    {
                        Target = new Entity(assignRequest.Target.LogicalName, assignRequest.Target.Id)
                    };
                    req.Target.Attributes["ownerid"] = assignRequest.Assignee;
                    core.Execute(req, userRef, ctx.ParentPluginContext);
                }
                return new AssignResponse();
            }

            if (request is SetStateRequest setstateRequest)
            {
                var targetEntity = core.TryRetrieve(setstateRequest.EntityMoniker);
                if (targetEntity?.GetAttributeValue<OptionSetValue>("statecode") != setstateRequest.State ||
                    targetEntity?.GetAttributeValue<OptionSetValue>("statuscode") != setstateRequest.Status)
                {
                    var req = new UpdateRequest
                    {
                        Target = new Entity(setstateRequest.EntityMoniker.LogicalName, setstateRequest.EntityMoniker.Id)
                    };
                    req.Target.Attributes["statecode"] = setstateRequest.State;
                    req.Target.Attributes["statuscode"] = setstateRequest.Status;
                    core.Execute(req, userRef, ctx.ParentPluginContext);
                }
                return new SetStateResponse();
            }

            if (workflowManager.GetActionDefaultNull(request.RequestName) != null)
                return core.ExecuteAction(request);

            if (core.HandlesCustomApi(request.RequestName))
                return core.ExecuteCustomApi(request, ctx.PluginContext);

            var handler = core.RequestHandlers.FirstOrDefault(x => x.HandlesRequest(request.RequestName));
            if (handler != null)
                return handler.Execute(request, userRef);

            if (core.IsExceptionFreeRequest(request.RequestName))
                return new OrganizationResponse();

            throw new NotImplementedException(
                $"Execute for the request '{request.RequestName}' has not been implemented yet.");
        }

        // ── Stage 6: PostOperation (stage 40) ────────────────────────────────────

        private void ExecutePostOperationStage(ExecutionPipelineContext ctx)
        {
            if (!ctx.ShouldTrigger) return;

            // Populate OutputParameters for retrieve operations so plugins can read them
            if (ctx.Request is RetrieveMultipleRequest)
            {
                ctx.PluginContext.OutputParameters["BusinessEntityCollection"] =
                    (ctx.Response as RetrieveMultipleResponse)?.EntityCollection;
            }
            else if (ctx.Request is RetrieveRequest)
            {
                ctx.PluginContext.OutputParameters["BusinessEntity"] =
                    core.TryRetrieve((ctx.Request as RetrieveRequest).Target);
            }

            // Populate IPluginExecutionContext4 post-images collection for Multiple operations
            if (ctx.EntityCollection != null)
            {
                // For CreateMultiple, the original entities may not have their Ids set.
                // Use the response Ids (from CreateMultipleResponse) when available.
                var responseIds = ctx.Response.Results.TryGetValue("Ids", out var idsObj)
                    ? idsObj as Guid[]
                    : null;

                ctx.PluginContext.PostEntityImagesCollection = ctx.EntityCollection.Entities
                    .Select((e, i) =>
                    {
                        var img = new EntityImageCollection();
                        var entityId = responseIds != null && i < responseIds.Length ? responseIds[i] : e.Id;
                        var post = entityId != Guid.Empty
                            ? core.TryRetrieve(new EntityReference(ctx.EntityCollection.EntityName ?? e.LogicalName, entityId))
                            : null;
                        if (post != null) img["PostImage"] = post;
                        return img;
                    }).ToArray();
            }

            ctx.SyncPostImage = core.TryRetrieve(ctx.PrimaryRef);
            if (ctx.SyncPostImage != null)
                ctx.SystemInjectedAttributes =
                    core.CopySystemAttributes(ctx.SyncPostImage, ctx.EntityInfo.Item1 as Entity);

            // Sync post-operation: system first, then user plugins ordered by ExecutionOrder, interleaved with workflows
            pluginManager.TriggerSystem(ctx.RequestMessage, ExecutionStage.PostOperation,
                ctx.EntityInfo.Item1, ctx.PreImage, ctx.SyncPostImage, ctx.PluginContext,
                ctx.SystemInjectedAttributes);

            pluginManager.TriggerSync(ctx.RequestMessage, ExecutionStage.PostOperation,
                ctx.EntityInfo.Item1, ctx.PreImage, ctx.SyncPostImage, ctx.PluginContext,
                p => p.GetExecutionOrder() == 0, ctx.SystemInjectedAttributes);

            if (ctx.Settings.TriggerWorkflows)
                workflowManager.TriggerSync(ctx.RequestMessage, ExecutionStage.PostOperation,
                    ctx.EntityInfo.Item1, ctx.PreImage, ctx.SyncPostImage, ctx.PluginContext,
                    ctx.SystemInjectedAttributes);

            pluginManager.TriggerSync(ctx.RequestMessage, ExecutionStage.PostOperation,
                ctx.EntityInfo.Item1, ctx.PreImage, ctx.SyncPostImage, ctx.PluginContext,
                p => p.GetExecutionOrder() != 0, ctx.SystemInjectedAttributes);

            // Stage async work — re-fetch post-image so async jobs see the final committed state
            ctx.AsyncPostImage = core.TryRetrieve(ctx.PrimaryRef);
            pluginManager.StageAsync(ctx.RequestMessage, ExecutionStage.PostOperation,
                ctx.EntityInfo.Item1, ctx.PreImage, ctx.AsyncPostImage, ctx.PluginContext,
                ctx.SystemInjectedAttributes);

            if (ctx.Settings.TriggerWorkflows)
                workflowManager.StageAsync(ctx.RequestMessage, ExecutionStage.PostOperation,
                    ctx.EntityInfo.Item1, ctx.PreImage, ctx.AsyncPostImage, ctx.PluginContext,
                    ctx.SystemInjectedAttributes);

            // Async jobs only fire at the top-level call, not from within a plugin
            if (ctx.ParentPluginContext == null)
            {
                pluginManager.TriggerAsyncWaitingJobs();

                if (ctx.Settings.TriggerWorkflows)
                    workflowManager.TriggerAsync();
            }

            if (ctx.Settings.TriggerWorkflows)
                workflowManager.ExecuteWaitingWorkflows(ctx.PluginContext);
        }

        // ── Stage 7: MockupExtensions ─────────────────────────────────────────────

        private void ExecuteExtensionStage(ExecutionPipelineContext ctx)
        {
            if (!core.HasMockupExtensions) return;

            // Guard against infinite recursion when moving business units (8+ layers can occur)
            if (ctx.PluginContext.ExtensionDepth > 8)
            {
                throw new FaultException(
                    "This workflow job was canceled because the workflow that started it included an infinite loop." +
                    " Correct the workflow logic and try again.");
            }

            var service = core.CreateMockupService(ctx.UserRef.Id, ctx.PluginContext);
            switch (ctx.Request.RequestName)
            {
                case "Create":
                    var createResponse = (CreateResponse)ctx.Response;
                    var entityLogicalName = ((Entity)ctx.Request.Parameters["Target"]).LogicalName;
                    var reference = ctx.PrimaryRef ?? new EntityReference(entityLogicalName, createResponse.id);
                    core.TriggerExtension(service, ctx.Request, core.TryRetrieve(reference), null, ctx.UserRef);
                    break;
                case "Update":
                    core.TriggerExtension(service, ctx.Request, core.TryRetrieve(ctx.PrimaryRef), ctx.PreImage, ctx.UserRef);
                    break;
                case "Delete":
                    core.TriggerExtension(service, ctx.Request, null, ctx.PreImage, ctx.UserRef);
                    break;
            }
        }

        // ── Pure helpers ──────────────────────────────────────────────────────────

        private static string RequestNameToMessageName(string requestName)
        {
            switch (requestName)
            {
                case "LoseOpportunity": return "Lose";
                case "WinOpportunity": return "Win";
                case "CloseQuote": return "Lose";
                case "WinQuote": return "Win";
                default: return requestName;
            }
        }

        private static Tuple<object, string, Guid> GetEntityInfo(OrganizationRequest request)
        {
            Mappings.EntityImageProperty.TryGetValue(request.GetType(), out string key);
            object obj = null;
            if (key != null)
                obj = request.Parameters[key];

            if (request is WinOpportunityRequest || request is LoseOpportunityRequest)
            {
                var close = request is WinOpportunityRequest
                    ? (request as WinOpportunityRequest).OpportunityClose
                    : (request as LoseOpportunityRequest).OpportunityClose;
                obj = close.GetAttributeValue<EntityReference>("opportunityid");
            }
            else if (request is WinQuoteRequest || request is CloseQuoteRequest)
            {
                var close = request is WinQuoteRequest
                    ? (request as WinQuoteRequest).QuoteClose
                    : (request as CloseQuoteRequest).QuoteClose;
                obj = close.GetAttributeValue<EntityReference>("quoteid");
            }
            else if (request is CloseIncidentRequest closeIncidentRequest)
            {
                obj = closeIncidentRequest.IncidentResolution?.GetAttributeValue<EntityReference>("incidentid");
            }
            else if (request is RetrieveMultipleRequest retrieveMultiple)
            {
                string entityName = null;
                switch (retrieveMultiple.Query)
                {
                    case FetchExpression fe:
                        entityName = XmlHandling.FetchXmlToQueryExpression(fe.Query).EntityName;
                        break;
                    case QueryExpression query:
                        entityName = query.EntityName;
                        break;
                    case QueryByAttribute qba:
                        entityName = qba.EntityName;
                        break;
                }

                if (entityName != null)
                    return new Tuple<object, string, Guid>(
                        new EntityReference { LogicalName = entityName, Id = Guid.Empty },
                        entityName, Guid.Empty);
            }

            if (obj is Entity entity)
                return new Tuple<object, string, Guid>(obj, entity.LogicalName, entity.Id);

            if (obj is EntityReference entityRef)
                return new Tuple<object, string, Guid>(obj, entityRef.LogicalName, entityRef.Id);

            if (obj is EntityCollection entityCollection)
                return new Tuple<object, string, Guid>(obj, entityCollection.EntityName, Guid.Empty);

            return null;
        }
    }
}
