using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class SetEntityProperty : IWorkflowNode
    {
        [DataMember]
        public string Attribute { get; private set; }
        [DataMember]
        public string EntityId { get; private set; }
        [DataMember]
        public string EntityLogicalName { get; private set; }
        [DataMember]
        public string VariableId { get; private set; }
        [DataMember]
        public string TargetType { get; private set; }

        public SetEntityProperty(string Attribute, string ParametersId, string EntityLogicalName, string VariableId, string TargetType)
        {
            this.Attribute = Attribute;
            this.EntityId = ParametersId;
            this.EntityLogicalName = EntityLogicalName;
            this.VariableId = VariableId;
            this.TargetType = TargetType;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            if (!variables.ContainsKey(VariableId))
            {
                Console.WriteLine($"The attribute '{Attribute}' was not created with id '{VariableId}' before being set");
                variables[VariableId] = null;
            }

            var attr = variables[VariableId];
            if (TargetType == "Int32" && attr != null)
            {
                // Round only when assigning the final whole-number value, including direct
                // Money mappings. A numeric conversion must not apply a currency exchange rate.
                var value = attr is Money moneyValue ? moneyValue.Value : Convert.ToDecimal(attr);
                attr = decimal.ToInt32(decimal.Round(value, 0, MidpointRounding.AwayFromZero));
            }
            else if (TargetType == "Money" && attr != null && !(attr is Money))
            {
                // Direct whole-number/decimal mappings need the SDK Money wrapper too.
                // Preserve the numeric value; the existing Money exchange conversion below
                // applies only to values that were already Money, not to this new wrapper.
                attr = new Money(Convert.ToDecimal(attr));
            }
            else if (attr is Money money)
            {
                var exchangeRate = variables["ExchangeRate"] as decimal?;
                var amount = money.Value * exchangeRate.GetValueOrDefault(1.0m);
                attr = new Money(amount);
            }

            if (TargetType == "EntityReference")
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

            if (TargetType == "String")
            {
                if (attr is OptionSetValue value)
                {
                    attr = Util.GetOptionSetValueLabel(EntityLogicalName, Attribute, value, orgService);
                }
                else if (attr is bool boolVal)
                {
                    attr = Util.GetBooleanLabel(EntityLogicalName, Attribute, boolVal, orgService);
                }
                else if (attr is EntityReference reference)
                {
                    attr = Util.GetPrimaryName(reference, orgService);
                }
                else if (attr is Money moneyTarget)
                {
                    // TODO: should respect record currency and user format preferences
                    attr = $"{moneyTarget?.Value:C}";
                }
                else if (attr is int number)
                {
                    // TODO: should respect user format preferences
                    attr = $"{number:N0}";
                }
                else if (attr is DateTime time)
                {
                    // TODO: what format does CRM do?
                    attr = $"{time:g}";
                }
                else if (attr != null && !(attr is string))
                {
                    throw new InvalidCastException($"Cannot convert {attr.GetType().Name} to {TargetType}");
                }
            }

            (variables[EntityId] as Entity).Attributes[Attribute] = attr;
        }
    }
}
