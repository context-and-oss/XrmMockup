using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class RetrieveCurrentTime : IWorkflowNode
    {
        [DataMember]
        public string VariableName { get; private set; }

        public RetrieveCurrentTime(string VariableName)
        {
            this.VariableName = VariableName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            WorkflowDiagnostics.Trace(trace, "Entering {0}. VariableName: {1}", this.GetType().Name, VariableName);
            variables[VariableName] = DateTime.Now.Add(timeOffset);
        }
    }
}
