using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class AssignEntity : IWorkflowNode
    {
        [DataMember]
        public string EntityId { get; private set; }
        [DataMember]
        public string OwnerId { get; private set; }

        public AssignEntity(string EntityId, string OwnerId)
        {
            this.EntityId = EntityId;
            this.OwnerId = OwnerId;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            if (!variables.ContainsKey(OwnerId))
            {
                throw new WorkflowException($"There is no variable with the id '{OwnerId}'");
            }
            var entity = variables[EntityId] as Entity;
            var assignee = variables[OwnerId] as EntityReference;
            var req = new AssignRequest()
            {
                Target = entity.ToEntityReference(),
                Assignee = assignee
            };
            orgService.Execute(req);

        }
    }
}
