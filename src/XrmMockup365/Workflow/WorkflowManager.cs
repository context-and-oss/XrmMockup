using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Security.Cryptography;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using WorkflowExecuter;
using System.Activities;
using XrmPluginCore.Enums;
using DG.Tools.XrmMockup.Internal;
using DG.Tools.XrmMockup.Extensions;


namespace DG.Tools.XrmMockup {


    internal class WorkflowManager {
        private readonly ILogger _logger;
        private readonly ICoreOperations _core;

        // Static caches shared across all WorkflowManager instances
        private static readonly ConcurrentDictionary<string, WorkflowTree> _staticParsedWorkflows = new ConcurrentDictionary<string, WorkflowTree>();
        private static readonly ConcurrentDictionary<string, CodeActivity> _staticCodeActivityCache = new ConcurrentDictionary<string, CodeActivity>();
        private static readonly object _cacheLock = new object();

        private List<Entity> actions;
        private List<Entity> synchronousWorkflows;
        private List<Entity> asynchronousWorkflows;
        private List<WaitInfo> waitingWorkflows;
        private Dictionary<string, EntityMetadata> metadata;
        private Dictionary<string, CodeActivity> codeActivityTriggers;
        private Dictionary<Guid, WorkflowTree> parsedWorkflows;

        private ConcurrentQueue<WorkflowExecutionContext> pendingAsyncWorkflows;

        internal int SynchronousWorkflowCount => synchronousWorkflows.Count;
        internal int AsynchronousWorkflowCount => asynchronousWorkflows.Count;
        internal int ActionsCount => actions.Count;
        internal int CodeActivityCount => codeActivityTriggers.Count;

        public WorkflowManager(ICoreOperations core, IEnumerable<Type> codeActivityInstances, bool? IncludeAllWorkflows, List<Entity> mixedWorkflows, Dictionary<string, EntityMetadata> metadata, ILogger logger = null) {
            _core = core;
            _logger = logger ?? NullLogger.Instance;
            this.metadata = metadata;
            this.actions = mixedWorkflows.Where(w => w.GetAttributeValue<OptionSetValue>("category").Value == (int)Workflow_Category.Action).ToList();

            var workflows = mixedWorkflows.Where(w => w.GetAttributeValue<OptionSetValue>("category").Value == (int)Workflow_Category.Workflow).ToList();
            synchronousWorkflows = new List<Entity>();
            asynchronousWorkflows = new List<Entity>();
            waitingWorkflows = new List<WaitInfo>();
            codeActivityTriggers = new Dictionary<string, CodeActivity>();
            parsedWorkflows = new Dictionary<Guid, WorkflowTree>();

            pendingAsyncWorkflows = new ConcurrentQueue<WorkflowExecutionContext>();

            if (!workflows.All(e => e.LogicalName == "workflow")) {
                throw new MockupException("Trying to parse workflow, but found a non workflow entity");
            }

            if (codeActivityInstances != null) {
                foreach (var codeActivityInstance in codeActivityInstances) {
                    foreach (var type in codeActivityInstance.Assembly.GetTypes()) {
                        if (type.BaseType != codeActivityInstance.BaseType || type.Module.Name.StartsWith("System") || type.IsAbstract) continue;

                        // Use static cache to avoid recreating CodeActivity instances
                        var codeActivity = _staticCodeActivityCache.GetOrAdd(type.Name, typeName =>
                        {
                            return (CodeActivity)Activator.CreateInstance(type);
                        });

                        if (!codeActivityTriggers.ContainsKey(type.Name)) {
                            codeActivityTriggers.Add(type.Name, codeActivity);
                            _logger.LogDebug("Discovered CodeActivity: {TypeName}", type.Name);
                        }
                    }
                }
            }

            if (IncludeAllWorkflows != false) {
                foreach (Entity workflow in workflows) {
                    AddWorkflow(workflow);
                }
            }

            _logger.LogInformation("Workflows loaded: sync={SyncCount}, async={AsyncCount}, actions={ActionsCount}, code activities={CodeActivityCount}",
                synchronousWorkflows.Count, asynchronousWorkflows.Count, actions.Count, codeActivityTriggers.Count);
        }

        
        /// <summary>
        /// Trigger all plugin steps which match the given parameters.
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="stage"></param>
        /// <param name="entity"></param>
        /// <param name="preImage"></param>
        /// <param name="postImage"></param>
        /// <param name="pluginContext"></param>
        /// <param name="core"></param>
        public void TriggerSync(string operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext,
                ISet<string> systemInjectedAttributes = null)
        {
            var toExecute = synchronousWorkflows.Where(x => ShouldExecute(x, operation, stage, entity, pluginContext, systemInjectedAttributes)).ToList();
            foreach (var workflow in toExecute)
            {
                Execute(workflow, operation, entity, preImage, postImage, pluginContext);
            }
        }


        public void TriggerAsync()
        {
            while (pendingAsyncWorkflows.TryDequeue(out var workflowContext))
            {
                Entity primaryEntity = _core.GetDbRow(workflowContext.primaryRef).ToEntity();

                var execution = ExecuteWorkflow(workflowContext.workflow, primaryEntity, workflowContext.pluginContext, workflowContext.workflowName);

                if (execution.Variables["Wait"] != null)
                {
                    waitingWorkflows.Add(execution.Variables["Wait"] as WaitInfo);
                }
            }
        }

        public void StageAsync(string operation, ExecutionStage stage,
                object entity, Entity preImage, Entity postImage, PluginContext pluginContext,
                ISet<string> systemInjectedAttributes = null)
        {
            var toExecute = asynchronousWorkflows.Where(x => ShouldStage(x, operation, stage, entity, pluginContext, systemInjectedAttributes)).ToList();
            foreach (var workflow in toExecute)
            {
                Stage(workflow, operation, stage, entity, pluginContext, systemInjectedAttributes);
            }
        }

        internal void ExecuteWaitingWorkflows(PluginContext pluginContext) {
            var provider = _core.CreateServiceProviderAndFactory(pluginContext);
            var triggerWorkflows = _core.GetMockupSettings().TriggerWorkflows ?? true;
            var service = provider.CreateOrganizationService(null, new MockupServiceSettings(true, triggerWorkflows, true, MockupServiceSettings.Role.SDK));
            foreach (var waitInfo in waitingWorkflows.ToList()) {
                waitingWorkflows.Remove(waitInfo);
                var variables = waitInfo.VariablesInstance;
                var shadowService = _core.ServiceFactory.CreateAdminOrganizationService(new MockupServiceSettings(false, true, MockupServiceSettings.Role.SDK));

                var primaryEntity = shadowService.Retrieve(waitInfo.PrimaryEntity.LogicalName, waitInfo.PrimaryEntity.Id, new ColumnSet(true));
                variables["InputEntities(\"primaryEntity\")"] = primaryEntity;
                waitInfo.Element.Execute(waitInfo.ElementIndex, ref variables, _core.TimeOffset, service, provider, provider.GetService<ITracingService>());
                if (variables["Wait"] != null) {
                    waitingWorkflows.Add(variables["Wait"] as WaitInfo);
                }
            }
        }

        //Save context for Asynchronous workflows.
        public class WorkflowExecutionContext
        {
            public WorkflowTree workflow;
            public PluginContext pluginContext;
            public EntityReference primaryRef;
            public string workflowName;

            public WorkflowExecutionContext(WorkflowTree workflow,PluginContext pluginContext, EntityReference primaryRef, string workflowName = null)
            {
                this.workflow = workflow;
                this.pluginContext = pluginContext;
                this.primaryRef = primaryRef;
                this.workflowName = workflowName;
            }
        }

        private void checkInfiniteRecursion(PluginContext pluginContext)
        {
            if (pluginContext.Depth > 8)
            {
                throw new FaultException(
                    "This workflow job was canceled because the workflow that started it included an infinite loop." +
                    " Correct the workflow logic and try again.");
            }
        }

        private PluginContext createPluginContext(PluginContext pluginContext, Entity workflow, workflow_stage? thisStage, Guid guid, string logicalName)
        {
            var thisPluginContext = pluginContext.Clone();
            thisPluginContext.Mode = ((int)workflow.GetOptionSetValue<Workflow_Mode>("mode") + 1) % 2;
            thisPluginContext.Stage = thisStage.HasValue ? (int)thisStage : (int)workflow_stage.Postoperation;
            thisPluginContext.PrimaryEntityId = guid;
            thisPluginContext.PrimaryEntityName = logicalName;

            return thisPluginContext;
        }

        private void Stage(Entity workflow, string operation, ExecutionStage stage,
            object entityObject, PluginContext pluginContext, ISet<string> systemInjectedAttributes)
        {
            if (workflow.LogicalName != "workflow") return;
            var entity = entityObject as Entity;
            var entityRef = entityObject as EntityReference;

            var guid = entity?.Id ?? entityRef.Id;
            var logicalName = entity?.LogicalName ?? entityRef.LogicalName;

            if (workflow.GetAttributeValue<string>("primaryentity") != "" && workflow.GetAttributeValue<string>("primaryentity") != logicalName) return;

            checkInfiniteRecursion(pluginContext);

            var isCreate = operation.Matches(EventOperation.Create);
            var isUpdate = operation.Matches(EventOperation.Update);
            var isDelete = operation.Matches(EventOperation.Delete);

            if (!ShouldTriggerOnAction(isCreate, isUpdate, isDelete, workflow)) return;

            if (isUpdate && !MatchesTriggerFields(workflow, entity, systemInjectedAttributes)) return;

            var thisStage = isCreate ? workflow.GetOptionSetValue<workflow_stage>("createstage") :
                (isDelete ? workflow.GetOptionSetValue<workflow_stage>("deletestage") : workflow.GetOptionSetValue<workflow_stage>("updatestage"));
            if (thisStage == null)
            {
                thisStage = workflow_stage.Postoperation;
            }

            if ((int)thisStage != (int)stage) return;

            var thisPluginContext = createPluginContext(pluginContext, workflow, thisStage, guid, logicalName);

            var parsedWorkflow = ParseWorkflow(workflow);
            if (parsedWorkflow == null) return;

            pendingAsyncWorkflows.Enqueue(new WorkflowExecutionContext(parsedWorkflow, thisPluginContext, new EntityReference(logicalName, guid), workflow.GetAttributeValue<string>("name")));
        }

        private bool ShouldStage(Entity workflow, string operation, ExecutionStage stage,
            object entityObject, PluginContext pluginContext, ISet<string> systemInjectedAttributes)
        {
            if (workflow.LogicalName != "workflow") return false;
            var entity = entityObject as Entity;
            var entityRef = entityObject as EntityReference;

            var guid = entity?.Id ?? entityRef.Id;
            var logicalName = entity?.LogicalName ?? entityRef.LogicalName;

            if (workflow.GetAttributeValue<string>("primaryentity") != "" && workflow.GetAttributeValue<string>("primaryentity") != logicalName) return false;

            checkInfiniteRecursion(pluginContext);

            var isCreate = operation.Matches(EventOperation.Create);
            var isUpdate = operation.Matches(EventOperation.Update);
            var isDelete = operation.Matches(EventOperation.Delete);

            if (!ShouldTriggerOnAction(isCreate, isUpdate, isDelete, workflow)) return false;

            if (isUpdate && !MatchesTriggerFields(workflow, entity, systemInjectedAttributes)) return false;

            var thisStage = isCreate ? workflow.GetOptionSetValue<workflow_stage>("createstage") :
                (isDelete ? workflow.GetOptionSetValue<workflow_stage>("deletestage") : workflow.GetOptionSetValue<workflow_stage>("updatestage"));
            if (thisStage == null)
            {
                thisStage = workflow_stage.Postoperation;
            }

            if ((int)thisStage != (int)stage) return false;

            var thisPluginContext = createPluginContext(pluginContext, workflow, thisStage, guid, logicalName);

            var parsedWorkflow = ParseWorkflow(workflow);
            if (parsedWorkflow == null) return false;

            return true;
        }

        /// <summary>
        /// Matches the workflow's triggering attributes against the attributes present in the Target.
        /// Attributes XrmMockup copied onto the Target for post-operation visibility are skipped —
        /// they were not part of the update the caller requested, so they must not trigger a workflow
        /// (a workflow triggering on e.g. statecode would otherwise run on every update of the table).
        /// </summary>
        private static bool MatchesTriggerFields(Entity workflow, Entity entity, ISet<string> systemInjectedAttributes)
        {
            var triggerList = workflow.GetAttributeValue<string>("triggeronupdateattributelist");
            if (string.IsNullOrEmpty(triggerList)) return false;

            var triggerFields = new HashSet<string>(triggerList.Split(','));
            return entity.Attributes.Keys.Any(key =>
                (systemInjectedAttributes == null || !systemInjectedAttributes.Contains(key))
                && triggerFields.Contains(key));
        }

        private bool ShouldTriggerOnAction(bool isCreate,bool isUpdate,bool isDelete,Entity workflow)
        {
            if (!isCreate && !isUpdate && !isDelete) return false;
            if (isCreate && (!workflow.GetAttributeValue<bool?>("triggeroncreate").HasValue || !workflow.GetAttributeValue<bool?>("triggeroncreate").Value)) return false;
            if (isDelete && (!workflow.GetAttributeValue<bool?>("triggerondelete").HasValue || !workflow.GetAttributeValue<bool?>("triggerondelete").Value)) return false;
            return true;
        }

        private bool ShouldExecute(Entity workflow, string operation, ExecutionStage stage, object entityObject, PluginContext pluginContext,
            ISet<string> systemInjectedAttributes)
        {
            // Check if it is supposed to execute. Returns preemptively, if it should not.
            if (workflow.LogicalName != "workflow") return false;
            var entity = entityObject as Entity;
            var entityRef = entityObject as EntityReference;

            var guid = entity?.Id ?? entityRef.Id;
            var logicalName = entity?.LogicalName ?? entityRef.LogicalName;
            
            if (workflow.GetAttributeValue<string>("primaryentity") != "" && workflow.GetAttributeValue<string>("primaryentity") != logicalName) return false;

            checkInfiniteRecursion(pluginContext);

            var isCreate = operation.Matches(EventOperation.Create);
            var isUpdate = operation.Matches(EventOperation.Update);
            var isDelete = operation.Matches(EventOperation.Delete);

            if (!ShouldTriggerOnAction(isCreate, isUpdate, isDelete, workflow)) return false;

            if (isUpdate && !MatchesTriggerFields(workflow, entity, systemInjectedAttributes)) return false;

            var thisStage = isCreate ? workflow.GetOptionSetValue<workflow_stage>("createstage") :
                (isDelete ? workflow.GetOptionSetValue<workflow_stage>("deletestage") : workflow.GetOptionSetValue<workflow_stage>("updatestage"));
            if (thisStage == null)
            {
                thisStage = workflow_stage.Postoperation;
            }

            if ((int)thisStage != (int)stage) return false;

            var thisPluginContext = createPluginContext(pluginContext, workflow, thisStage, guid, logicalName);

            var parsedWorkflow = ParseWorkflow(workflow);
            if (parsedWorkflow == null) return false;

            return true;
        }

        private void Execute(Entity workflow, string operation, object entityObject, Entity preImage, Entity postImage, PluginContext pluginContext)
        {
            // Check if it is supposed to execute. Returns preemptively, if it should not.
            var entity = entityObject as Entity;
            var entityRef = entityObject as EntityReference;

            var guid = entity?.Id ?? entityRef.Id;
            var logicalName = entity?.LogicalName ?? entityRef.LogicalName;

            checkInfiniteRecursion(pluginContext);

            var isCreate = operation.Matches(EventOperation.Create);
            var isUpdate = operation.Matches(EventOperation.Update);
            var isDelete = operation.Matches(EventOperation.Delete);

            var thisStage = isCreate ? workflow.GetOptionSetValue<workflow_stage>("createstage") :
                (isDelete ? workflow.GetOptionSetValue<workflow_stage>("deletestage") : workflow.GetOptionSetValue<workflow_stage>("updatestage"));

            if (thisStage == null)
            {
                thisStage = workflow_stage.Postoperation;
            }
            
            var thisPluginContext = createPluginContext(pluginContext, workflow, thisStage, guid, logicalName);

            var parsedWorkflow = ParseWorkflow(workflow);

            WorkflowTree postExecution = null;
            var workflowName = workflow.GetAttributeValue<string>("name");
            if (thisStage == workflow_stage.Preoperation)
            {
                postExecution = ExecuteWorkflow(parsedWorkflow, preImage.CloneEntity(), thisPluginContext, workflowName);
            }
            else
            {
                postExecution = ExecuteWorkflow(parsedWorkflow, postImage.CloneEntity(), thisPluginContext, workflowName);
            }

            if (postExecution.Variables["Wait"] != null)
            {
                waitingWorkflows.Add(postExecution.Variables["Wait"] as WaitInfo);
            }
        }

        internal WorkflowTree ExecuteWorkflow(WorkflowTree workflow, Entity primaryEntity, PluginContext pluginContext, string workflowName = null) {
            var provider = _core.CreateServiceProviderAndFactory(pluginContext);
            var triggerWorkflows = _core.GetMockupSettings().TriggerWorkflows ?? true;
            var service = provider.CreateAdminOrganizationService(new MockupServiceSettings(true, triggerWorkflows, true, MockupServiceSettings.Role.SDK));

            WorkflowTree result = null;
            // A null plugin context indicates a directly-invoked workflow/action (ExecuteWorkflowRequest);
            // without context metadata we run it but skip recording a grouped trace entry.
            PluginTraceRecorder.Run(
                _core,
                provider,
                pluginContext,
                typeName: workflowName,
                messageName: pluginContext?.MessageName,
                primaryEntity: pluginContext?.PrimaryEntityName ?? primaryEntity?.LogicalName,
                operationType: PluginTraceOperationType.WorkflowActivity,
                mode: pluginContext != null ? (ExecutionMode)pluginContext.Mode : ExecutionMode.Synchronous,
                execute: () => result = workflow.Execute(primaryEntity, _core.TimeOffset, service, provider, provider.GetService<ITracingService>()),
                unwrapTargetInvocation: false,
                record: pluginContext != null);
            return result;
        }

        internal WorkflowTree ParseWorkflow(Entity workflow) {
            var cacheKey = GenerateWorkflowCacheKey(workflow);
            
            // Check static cache first
            if (_staticParsedWorkflows.TryGetValue(cacheKey, out var cachedWorkflow))
            {
                cachedWorkflow.HardReset();
                
                // Also store in instance cache for legacy compatibility
                if (!parsedWorkflows.ContainsKey(workflow.Id)) {
                    parsedWorkflows[workflow.Id] = cachedWorkflow;
                }
                
                return cachedWorkflow;
            }
            
            // Check instance cache
            if (parsedWorkflows.ContainsKey(workflow.Id)) {
                parsedWorkflows[workflow.Id].HardReset();
                return parsedWorkflows[workflow.Id];
            }
            
            try {
                var parsed = WorkflowConstructor.Parse(workflow, codeActivityTriggers, WorkflowConstructor.ParseAs.Workflow);
                parsedWorkflows.Add(workflow.Id, parsed);
                
                // Cache in static cache for future instances
                _staticParsedWorkflows.TryAdd(cacheKey, parsed);
                
                return parsed;
            } catch (Exception ex) {
                _logger.LogWarning(ex, "Failed to parse workflow '{WorkflowName}'", workflow.Attributes["name"]);
            }
            return null;
        }

        internal void AddWorkflow(Entity workflow) {
            if (workflow.LogicalName != LogicalNames.Workflow) return;
            var name = workflow.GetAttributeValue<string>("name") ?? workflow.Id.ToString();
            // Register actions so they can be invoked by message name (Core dispatches
            // to ExecuteAction via GetActionDefaultNull); otherwise an added action would be mis-filed
            // as a workflow and never be reachable.
            if (workflow.GetAttributeValue<OptionSetValue>("category")?.Value == (int)Workflow_Category.Action) {
                actions.Add(workflow);
                _logger.LogDebug("Added action: {WorkflowName}", name);
                return;
            }
            if (workflow.GetOptionSetValue<Workflow_Mode>("mode") == Workflow_Mode.Background) {
                asynchronousWorkflows.Add(workflow);
                _logger.LogDebug("Added async workflow: {WorkflowName}", name);
            } else {
                synchronousWorkflows.Add(workflow);
                _logger.LogDebug("Added sync workflow: {WorkflowName}", name);
            }
        }

        public Entity GetActionDefaultNull(string requestName) {
            return actions.FirstOrDefault(e => e.GetAttributeValue<string>("name") == requestName);
        }

        internal void ResetWorkflows(bool? IncludeAllWorkflows) {
            if (IncludeAllWorkflows == false)
            {
                synchronousWorkflows = new List<Entity>();
                asynchronousWorkflows = new List<Entity>();
                parsedWorkflows = new Dictionary<Guid, WorkflowTree>();
            }
            waitingWorkflows = new List<WaitInfo>();
        }

        private string GenerateWorkflowCacheKey(Entity workflow)
        {
            if (workflow == null) return "null_workflow";
            
            // Use workflow XAML + ID for cache key since XAML defines the workflow structure
            var xaml = workflow.GetAttributeValue<string>("xaml") ?? "";
            var workflowId = workflow.Id.ToString();
            var combinedKey = $"{workflowId}:{xaml}";
            
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedKey));
                return Convert.ToBase64String(hash);
            }
        }
    }
}
