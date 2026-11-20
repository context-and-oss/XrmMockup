using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class UpdateEntity : IWorkflowNode
    {
        [DataMember]
        public string VariableId { get; private set; }
        [DataMember]
        public string EntityName { get; private set; }

        public UpdateEntity(string VariableId, string EntityName)
        {
            this.VariableId = VariableId;
            this.EntityName = EntityName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            WorkflowDiagnostics.Trace(trace, "Entering {0}. VariableId: {1} EntityName: {2}", this.GetType().Name, VariableId, EntityName);
            orgService.Update(variables[VariableId] as Entity);
        }
    }
}
