using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class SelectFirstNonNull : IWorkflowNode
    {
        [DataMember]
        public string[] Parameters { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }

        public SelectFirstNonNull(string[] Parameters, string VariableName)
        {
            this.Parameters = Parameters;
            this.VariableName = VariableName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            object value = null;
            foreach (var param in Parameters)
            {
                if (variables.ContainsKey(param) && variables[param] != null)
                {
                    value = variables[param];
                    break;
                }
            }

            variables[VariableName] = value;

        }
    }
}
