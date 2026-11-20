using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class RetrieveLastExecutionTime : IWorkflowNode
    {
        [DataMember]
        public string VariableName { get; private set; }

        public RetrieveLastExecutionTime(string VariableName)
        {
            this.VariableName = VariableName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            variables[VariableName] = variables["ExecutionTime"];
        }
    }
}
