using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class Postpone : IWorkflowNode
    {
        [DataMember]
        public string BlockExecution { get; private set; }
        [DataMember]
        public string PostponeUntil { get; private set; }

        public Postpone(string BlockExecution, string PostponeUntil)
        {
            this.BlockExecution = BlockExecution;
            this.PostponeUntil = PostponeUntil;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            if (BlockExecution == "True")
            {
                variables["Wait"] = null;
            }
        }
    }
}
