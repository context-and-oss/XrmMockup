using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class Concat : IWorkflowNode
    {
        [DataMember]
        public string[][] Parameters { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }

        public Concat(string[][] Parameters, string VariableName)
        {
            this.Parameters = Parameters;
            this.VariableName = VariableName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var variablesInstance = variables;
            // Dataverse permits a calculated field's Concat to receive non-string operands (numbers,
            // money, dates, etc.) and stringifies them with invariant culture before joining. Mirror
            // that — the previous (string) cast threw InvalidCastException on any non-string value,
            // which propagated out of Retrieve/RetrieveMultiple even when the calculated column was
            // not requested.
            var strings = Parameters[0].Select(p => Stringify(variablesInstance[p])).ToArray();
            variables[VariableName] = string.Concat(strings);
        }

        private static string Stringify(object value)
        {
            switch (value)
            {
                case null:
                    return string.Empty;
                case string s:
                    return s;
                case Money money:
                    return money.Value.ToString(CultureInfo.InvariantCulture);
                case EntityReference er:
                    return er.Name ?? string.Empty;
                case OptionSetValue osv:
                    return osv.Value.ToString(CultureInfo.InvariantCulture);
                case IFormattable f:
                    return f.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return value.ToString();
            }
        }
    }
}
