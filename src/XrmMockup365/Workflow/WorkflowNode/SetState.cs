using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class SetState : IWorkflowNode
    {
        [DataMember]
        public string EntityId { get; private set; }
        [DataMember]
        public string EntityIdKey { get; private set; }
        [DataMember]
        public string EntityName { get; private set; }
        [DataMember]
        public OptionSetValue StateCode { get; private set; }
        [DataMember]
        public OptionSetValue StatusCode { get; private set; }

        public SetState(string EntityKey, string EntityIdKey, string EntityName, OptionSetValue StateCode, OptionSetValue StatusCode)
        {
            this.EntityId = EntityKey;
            this.EntityIdKey = EntityIdKey;
            this.EntityName = EntityName;
            this.StateCode = StateCode;
            this.StatusCode = StatusCode;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var entity = variables[EntityId] as Entity;
            if (entity.LogicalName != EntityName)
            {
                throw new WorkflowException($"primary entity has logicalname '{entity.LogicalName}' instead of '{EntityName}'");
            }

            var req = new SetStateRequest();
            req.EntityMoniker = entity.ToEntityReference();
            req.State = StateCode;
            req.Status = StatusCode;
            orgService.Execute(req);
        }
    }
}
