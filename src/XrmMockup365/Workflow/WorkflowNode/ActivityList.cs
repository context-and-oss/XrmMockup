using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class ActivityList : IWorkflowNode
    {
        [DataMember]
        public List<string> VariableNames { get; private set; }

        [DataMember]
        public IWorkflowNode[] Activities { get; private set; }

        public ActivityList(IWorkflowNode[] Activities) : this(Activities, new List<string>()) { }

        public ActivityList(IWorkflowNode[] Activities, List<string> variableNames)
        {
            this.Activities = Activities;
            this.VariableNames = variableNames;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            Execute(0, ref variables, timeOffset, orgService, factory, trace);
        }

        public void Execute(int loopStart, ref Dictionary<string, object> variables, TimeSpan timeOffset,
             IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            foreach (var variableName in VariableNames)
            {
                if (!variables.ContainsKey(variableName))
                {
                    variables.Add(variableName, null);
                }
            }

            for (var i = loopStart; i < Activities.Length; i++)
            {
                if (Activities[i] is WaitStart)
                {
                    var primaryEntityreference = (variables["InputEntities(\"primaryEntity\")"] as Entity).ToEntityReference();
                    variables["Wait"] = new WaitInfo(this, i, new Dictionary<string, object>(variables), primaryEntityreference);
                }
                Activities[i].Execute(ref variables, timeOffset, orgService, factory, trace);
            }
        }
    }
}
