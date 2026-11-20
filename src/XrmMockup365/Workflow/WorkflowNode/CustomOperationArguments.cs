using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class CustomOperationArguments : IWorkflowNode
    {
        [DataMember]
        public string[][] Parameters { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }

        public CustomOperationArguments(string[][] Parameters, string VariableName)
        {
            this.Parameters = Parameters;
            this.VariableName = VariableName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            WorkflowDiagnostics.Trace(trace, "Entering {0}. VariableName: {1}", this.GetType().Name, VariableName);
            var variableName = "{" + Parameters[0][0] + "(Arguments)}";
            variables[VariableName] = variables.ContainsKey(variableName) ? variables[variableName] : null;
        }
    }
}
