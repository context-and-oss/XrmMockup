using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class RollUp : IWorkflowNode
    {
        [DataMember]
        public string HierarchicalRelationshipName { get; private set; }
        [DataMember]
        public string AggregateResult { get; private set; }
        [DataMember]
        public List<IWorkflowNode> Filter { get; private set; }
        [DataMember]
        public List<Entity> Filtered { get; private set; }
        [DataMember]
        public List<IWorkflowNode> Aggregation { get; private set; }
        private string FilterResult;

        public RollUp(string hierarchicalRelationshipName, string filterResult, string aggregateResult,
            List<IWorkflowNode> filter, List<IWorkflowNode> aggregation)
        {
            this.HierarchicalRelationshipName = hierarchicalRelationshipName;
            this.FilterResult = filterResult;
            this.AggregateResult = aggregateResult;
            this.Filter = filter;
            this.Aggregation = aggregation;
            this.Filtered = new List<Entity>();
        }


        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var primaryEntityKey = "InputEntities(\"primaryEntity\")";
            var relation = Filter.First(x => x is SetAttributeValue) as SetAttributeValue;
            relation.Execute(ref variables, timeOffset, orgService, factory, trace);
            var relatedEntities = Util.GetRelatedEntities(relation.EntityName, variables, orgService);
            foreach (var entity in relatedEntities)
            {
                var tmpVariables = variables;
                tmpVariables[primaryEntityKey] = entity;
                foreach (var node in Filter)
                {
                    node.Execute(ref tmpVariables, timeOffset, null, factory, trace);
                }
                if (FilterResult == null || (bool)tmpVariables[FilterResult])
                {
                    Filtered.Add(entity);
                }
            }

            var relatedField = (Aggregation[0] as GetEntityProperty).Attribute;
            var filteredLocation = (Aggregation[0] as GetEntityProperty).VariableName;

            if (Filtered.Any(e => e.Attributes.ContainsKey(relatedField) && e.Attributes[relatedField] is Money))
            {
                var targetExchangeRate = (decimal?)variables["ExchangeRate"];
                if (!targetExchangeRate.HasValue)
                {
                    var primary = variables[primaryEntityKey] as Entity;
                    throw new WorkflowException($"Entity with logicalname '{primary.LogicalName}' and id '{primary.Id}'" +
                        " has no transactioncurrency. Make sure to update your metadata.");
                }

                var exchangerate = "exchangerate";
                variables[filteredLocation] =
                    Filtered.Where(e => e.Attributes.ContainsKey(relatedField))
                    .Select(e => new Money(
                        (e.Attributes[relatedField] as Money).Value * (targetExchangeRate.Value / (decimal)e.Attributes[exchangerate])));
            }
            else
            {
                variables[filteredLocation] = Filtered.Select(e => e.Attributes.ContainsKey(relatedField) ? e.Attributes[relatedField] : null).ToList();
            }

            Aggregation[1].Execute(ref variables, timeOffset, orgService, factory, trace);
        }
    }
}
