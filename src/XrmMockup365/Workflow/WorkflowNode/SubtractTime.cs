using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class SubtractTime : IWorkflowNode
    {
        [DataMember]
        public string[][] Parameters { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }
        [DataMember]
        public string Amount { get; private set; }

        public SubtractTime(string[][] Parameters, string VariableName, string Amount)
        {
            this.Parameters = Parameters;
            this.VariableName = VariableName;
            this.Amount = Amount;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var toSubtract = variables[Parameters[0][0]] as int?;
            var date = variables[Parameters[0][1]] as DateTime?;
            if (toSubtract.HasValue && date.HasValue)
            {
                switch (Amount)
                {
                    case "SubtractDays":
                        variables[VariableName] = date.Value.AddDays(-toSubtract.Value);
                        break;
                    case "SubtractHours":
                        variables[VariableName] = date.Value.AddHours(-toSubtract.Value);
                        break;
                    case "SubtractMonths":
                        variables[VariableName] = date.Value.AddMonths(-toSubtract.Value);
                        break;
                    case "SubtractWeeks":
                        variables[VariableName] = date.Value.AddDays(-7 * toSubtract.Value);
                        break;
                    case "SubtractYears":
                        variables[VariableName] = date.Value.AddYears(-toSubtract.Value);
                        break;
                }
            }
            else
            {
                variables[VariableName] = null;
            }
        }
    }
}
