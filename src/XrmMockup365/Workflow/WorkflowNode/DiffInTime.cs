using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class DiffInTime : IWorkflowNode
    {
        [DataMember]
        public string[][] Parameters { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }
        [DataMember]
        public string Amount { get; private set; }

        public DiffInTime(string[][] Parameters, string VariableName, string Amount)
        {
            this.Parameters = Parameters;
            this.VariableName = VariableName;
            this.Amount = Amount;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var date1 = variables[Parameters[0][0]] as DateTime?;
            var date2 = variables[Parameters[0][1]] as DateTime?;
            if (date1.HasValue && date2.HasValue)
            {
                var timespan = date1.Value.Subtract(date2.Value);
                switch (Amount)
                {
                    case "DiffInDays":
                        variables[VariableName] = timespan.Days;
                        break;
                    case "DiffInHours":
                        variables[VariableName] = timespan.Hours;
                        break;
                    case "DiffInMinutes":
                        variables[VariableName] = timespan.Minutes;
                        break;
                    case "DiffInMonths":
                        variables[VariableName] = Utility.GetDiffMonths(date1.Value, date2.Value);
                        break;
                    case "DiffInWeeks":
                        variables[VariableName] = timespan.Days * 7;
                        break;
                    case "DiffInYears":
                        variables[VariableName] = Utility.GetDiffYears(date1.Value, date2.Value);
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
