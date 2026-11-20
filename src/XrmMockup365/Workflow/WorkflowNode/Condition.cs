using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class Condition : IWorkflowNode
    {
        [DataMember]
        public string GuardId { get; private set; }
        [DataMember]
        public IWorkflowNode Then { get; private set; }
        [DataMember]
        public IWorkflowNode Otherwise { get; internal set; }

        public Condition(string GuardId, IWorkflowNode Then, IWorkflowNode Otherwise)
        {
            this.GuardId = GuardId;
            this.Then = Then;
            this.Otherwise = Otherwise;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            if (variables[GuardId] != null && (bool)variables[GuardId])
            {
                Then.Execute(ref variables, timeOffset, orgService, factory, trace);
            }
            else
            {
                Otherwise.Execute(ref variables, timeOffset, orgService, factory, trace);
            }
        }
    }
}
