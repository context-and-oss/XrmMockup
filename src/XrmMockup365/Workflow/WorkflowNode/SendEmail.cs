using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class SendEmail : IWorkflowNode
    {
        [DataMember]
        public string EntityId;
        [DataMember]
        public string DisplayName;
        [DataMember]
        public string Entity;

        public SendEmail(string entityId, string displayName, string entity)
        {
            EntityId = entityId;
            DisplayName = displayName;
            Entity = entity;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            if (EntityId == "{x:Null}")
            {
                orgService.Create(variables[Entity.TrimEdge()] as Entity);
            }
        }
    }
}
