using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Activities;
using DG.Tools.XrmMockup;

namespace WorkflowExecuter
{
    [DataContract]
    internal class CallCodeActivity : IWorkflowNode
    {
        [DataMember]
        public string CodeActivityName;
        [DataMember]
        public Dictionary<string, string> inArguments;
        [DataMember]
        public Dictionary<string, string> outArguments;

        [DataMember]
        public string WorkflowName;


        public CallCodeActivity(string CodeActivityName, string WorkflowName, Dictionary<string, string> inArguments, Dictionary<string, string> outArguments)
        {
            this.CodeActivityName = CodeActivityName;
            this.WorkflowName = WorkflowName;
            this.inArguments = inArguments;
            this.outArguments = outArguments;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var variablesInstance = variables;

            var filteredArgs = new List<KeyValuePair<string, string>>();
            foreach (var arg in this.inArguments)
            {
                var inOutArguments = outArguments.ContainsKey(arg.Value);
                var hasVarInInstance = variablesInstance.TryGetValue(arg.Value, out var instanceVar);
                if (!inOutArguments && hasVarInInstance && instanceVar != null)
                {
                    filteredArgs.Add(arg);
                }
            }

            var arguments = new Dictionary<string, object>();
            foreach (var arg in filteredArgs)
            {
                arguments[arg.Key] = variablesInstance.TryGetValue(arg.Value, out var v) ? v : null;
            }

            var codeActivities = variables["CodeActivites"] as Dictionary<string, CodeActivity>;
            if (!codeActivities.TryGetValue(CodeActivityName, out var codeActivity))
            {
                throw new MockupException("Attempting to execute step with name {0} in the workflow \"{1}\", but no code activity by that name was found.", CodeActivityName, WorkflowName);
            }

            var primaryEntity = variables["InputEntities(\"primaryEntity\")"] as Entity;
            if (primaryEntity is null)
            {
                throw new MockupException("Attempting to pass primary entity to CodeActivity failed, entity is null or not of type Entity");
            }

            var workflowContext = new XrmWorkflowContext
            {
                PrimaryEntityId = primaryEntity.Id,
                PrimaryEntityName = primaryEntity.LogicalName
            };

            if (factory is IServiceProvider serviceProvider)
            {
                var pluginExecutionContext = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
                workflowContext.InitiatingUserId = pluginExecutionContext.InitiatingUserId;
                workflowContext.UserId = pluginExecutionContext.UserId;
            }
            
            var invoker = new WorkflowInvoker(codeActivity);
            invoker.Extensions.Add(trace);
            invoker.Extensions.Add(workflowContext);
            invoker.Extensions.Add(factory);
            var variablesPostExecution = invoker.Invoke(arguments);
            foreach (var outArg in outArguments)
            {
                variables[outArg.Key] = variablesPostExecution[outArg.Value];
            }

            foreach (var outArg in outArguments.Where(arg => variablesPostExecution[arg.Value] is EntityReference))
            {
                var reference = variablesPostExecution[outArg.Value] as EntityReference;
                var retrieved = orgService.Retrieve(reference.LogicalName, reference.Id, new ColumnSet(true));
                var regex = new Regex(@"[\w]+(?=_)");
                var entityVariableName = regex.Match(outArg.Key).Value + "_entity";
                variables[$"CreatedEntities(\"{entityVariableName}\")"] = retrieved;
            }
        }
    }
}
