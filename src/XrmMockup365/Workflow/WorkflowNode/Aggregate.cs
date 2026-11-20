using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class Aggregate : IWorkflowNode
    {
        [DataMember]
        public string[][] Parameters { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }
        [DataMember]
        public string Method { get; private set; }

        public Aggregate(string[][] Parameters, string VariableName, string Method)
        {
            this.Parameters = Parameters;
            this.VariableName = VariableName;
            this.Method = Method;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var parameterKey = Parameters[0][0];
            var parameters = variables[parameterKey] as IEnumerable<object>;
            var paramType = parameters.FirstOrDefault();
            var variablesInstance = variables;

            if (paramType == null)
            {
                variables[VariableName] = null;
                return;
            }

            IEnumerable<decimal> comparableParameters = null;
            if (paramType is int)
            {
                comparableParameters = parameters.Select(p => (decimal)p);
            }
            else if (paramType is Money)
            {
                comparableParameters = parameters.Select(p => (p as Money).Value);
            }
            else if (paramType is decimal)
            {
                comparableParameters = parameters.Select(p => (decimal)p);
            }
            else if (paramType is DateTime)
            {
                comparableParameters = parameters.Select(p => (decimal)((DateTime)p).Ticks);
            }

            if (comparableParameters == null)
            {
                throw new NotImplementedException($"Unknown type when aggregating '{paramType}'");
            }

            decimal? result = null;
            switch (Method)
            {
                case "Maximum":
                    result = comparableParameters.Max();
                    break;
                case "Minimum":
                    result = comparableParameters.Min();
                    break;
                case "Sum":
                    if (!(paramType is DateTime)) result = comparableParameters.Sum();
                    break;
                case "Average":
                    if (!(paramType is DateTime)) result = comparableParameters.Average();
                    break;
                default:
                    break;
            }

            if (!result.HasValue)
            {
                throw new NotImplementedException($"Unknown aggregate method '{Method}'");
            }

            if (paramType is int)
            {
                variables[VariableName] = (int)result.Value;
            }
            else if (paramType is Money)
            {
                variables[VariableName] = new Money(result.Value);
            }
            else if (paramType is decimal)
            {
                variables[VariableName] = result.Value;
            }
            else if (paramType is DateTime)
            {
                variables[VariableName] = new DateTime((long)result.Value);
            }
        }
    }
}
