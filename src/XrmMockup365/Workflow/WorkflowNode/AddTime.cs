using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class AddTime : IWorkflowNode
    {
        [DataMember]
        public string[][] Parameters { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }
        [DataMember]
        public string Amount { get; private set; }

        public AddTime(string[][] Parameters, string VariableName, string Amount)
        {
            this.Parameters = Parameters;
            this.VariableName = VariableName;
            this.Amount = Amount;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var toAdd = variables[Parameters[0][0]] as int?;
            var date = variables[Parameters[0][1]] as DateTime?;
            if (toAdd.HasValue && date.HasValue)
            {
                switch (Amount)
                {
                    case "AddDays":
                        variables[VariableName] = date.Value.AddDays(toAdd.Value);
                        break;
                    case "AddHours":
                        variables[VariableName] = date.Value.AddHours(toAdd.Value);
                        break;
                    case "AddMonths":
                        variables[VariableName] = date.Value.AddMonths(toAdd.Value);
                        break;
                    case "AddWeeks":
                        variables[VariableName] = date.Value.AddDays(7 * toAdd.Value);
                        break;
                    case "AddYears":
                        variables[VariableName] = date.Value.AddYears(toAdd.Value);
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
