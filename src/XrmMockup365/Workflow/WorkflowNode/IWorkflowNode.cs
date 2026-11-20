using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;

namespace WorkflowExecuter
{
    public interface IWorkflowNode
    {
        void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset, IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace);
    }
}
