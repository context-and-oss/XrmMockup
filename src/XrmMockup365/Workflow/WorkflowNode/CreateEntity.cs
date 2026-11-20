using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class CreateEntity : IWorkflowNode
    {
        [DataMember]
        public string EntityId { get; private set; }
        [DataMember]
        public string VariableId { get; private set; }
        [DataMember]
        public string EntityName { get; private set; }

        public CreateEntity(string EntityId, string VariableId, string EntityName)
        {
            this.EntityId = EntityId;
            this.VariableId = VariableId;
            this.EntityName = EntityName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var entity = variables[VariableId] as Entity;
            entity.Id = EntityId == null ? Guid.NewGuid() : new Guid(EntityId);
            entity[entity.LogicalName + "id"] = entity.Id;
            orgService.Create(entity);
        }
    }
}
