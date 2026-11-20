using Microsoft.Xrm.Sdk;
using System.Collections.Generic;

namespace WorkflowExecuter
{
    internal class WaitInfo
    {
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
