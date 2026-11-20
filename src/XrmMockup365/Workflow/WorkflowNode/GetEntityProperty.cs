using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class GetEntityProperty : IWorkflowNode
    {
        [DataMember]
        public string Attribute { get; private set; }
        [DataMember]
        public string EntityId { get; private set; }
        [DataMember]
        public string EntityLogicalName { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }
        [DataMember]
        public string TargetType { get; private set; }

        public GetEntityProperty(string Attribute, string EntityId, string EntityLogicalName, string VariableName, string TargetType)
        {
            this.Attribute = Attribute;
            this.EntityId = EntityId;
            this.EntityLogicalName = EntityLogicalName;
            this.VariableName = VariableName;
            this.TargetType = TargetType;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            Entity entity = null;
            if (EntityId.Contains("related_"))
            {
                var regex = new Regex(@"_.+#");
                var relatedAttr = regex.Match(EntityId).Value.TrimEdge();
                var primaryEntity = variables["InputEntities(\"primaryEntity\")"] as Entity;
                if (!primaryEntity.Attributes.ContainsKey(relatedAttr))
                {
                    variables[VariableName] = null;
                    return;
                }
                var entRef = primaryEntity.Attributes[relatedAttr] as EntityReference;
                if (entRef == null)
                {
                    variables[VariableName] = null;
                    return;
                }
                entity = orgService.Retrieve(EntityLogicalName, entRef.Id, new ColumnSet(true));

            }
            else
            {
                entity = variables.ContainsKey(EntityId) ? variables[EntityId] as Entity : null;
            }

            if (entity == null)
            {
                variables[VariableName] = null;
                return;
            }

            if (entity.LogicalName != EntityLogicalName)
            {
                variables[VariableName] = null;
                return;
            }

            if (Attribute == "!Process_Custom_Attribute_URL_")
            {
                variables[VariableName] = "https://somedummycrm.crm.dynamics.com/main.aspx?someparametrs";
                return;
            }

            if (!entity.Attributes.ContainsKey(Attribute))
            {
                variables[VariableName] = null;
                return;
            }

            var attr = entity.Attributes[Attribute];
            // Null is a valid empty lookup; only non-null values need conversion.
            if (TargetType == "EntityReference" && attr != null)
            {
                if (attr is Guid guid)
                {
                    attr = new EntityReference(EntityLogicalName, guid);
                }
                else if (!(attr is EntityReference))
                {
                    throw new InvalidCastException($"Cannot convert {attr.GetType().Name} to {TargetType}");
                }
            }

            variables[VariableName] = attr;
        }
    }
}
