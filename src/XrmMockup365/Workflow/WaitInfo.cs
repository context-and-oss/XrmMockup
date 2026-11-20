using Microsoft.Xrm.Sdk;
using DG.Tools.XrmMockup.Internal;
using System.Collections.Generic;
using System;

namespace WorkflowExecuter
{
    internal class WaitInfo
    {
        internal string WorkflowName { get; set; }
        internal Guid WorkflowId { get; set; }
        internal Workflow_Mode? WorkflowMode { get; set; }

        internal void Resume(TimeSpan timeOffset, IOrganizationService service,
            IOrganizationServiceFactory factory, ITracingService trace, bool enableWorkflowTracing)
        {
            var variables = VariablesInstance;
            WorkflowDiagnostics.Run(WorkflowName, WorkflowId, WorkflowMode, PrimaryEntity, timeOffset,
                trace, enableWorkflowTracing, true,
                executionTrace => Element.Execute(ElementIndex, ref variables, timeOffset, service, factory, executionTrace),
                () => variables["Wait"] as WaitInfo);
        }

        public ActivityList Element { get; private set; }
        public int ElementIndex { get; private set; }
        public Dictionary<string, object> VariablesInstance { get; private set; }
        public EntityReference PrimaryEntity { get; private set; }

        public WaitInfo(ActivityList Element, int ElementIndex, Dictionary<string, object> VariablesInstance, EntityReference PrimaryEntity)
        {
            this.Element = Element;
            this.ElementIndex = ElementIndex;
            this.VariablesInstance = VariablesInstance;
            this.PrimaryEntity = PrimaryEntity;
        }
    }
}
