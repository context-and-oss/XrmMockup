using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class WaitStart : IWorkflowNode
    {
        public WaitStart() { }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            WorkflowDiagnostics.Trace(trace, "Entering {0}", this.GetType().Name);

        }
    }
}
