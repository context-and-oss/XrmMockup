using XrmPluginCore.Enums;
using XrmPluginCore.Interfaces.Plugin;
using System;
using System.Collections.Generic;

namespace DG.Tools.XrmMockup
{
    internal class PluginStepConfig : IPluginStepConfig
    {
        public string EntityLogicalName { get; internal set; }

        public string Name { get; internal set; }

        public string EventOperation { get; internal set; }

        public ExecutionStage ExecutionStage { get; internal set; }

        public ExecutionMode ExecutionMode { get; internal set; }

        public int ExecutionOrder { get; internal set; }

        public Deployment Deployment { get; internal set; }

        public string FilteredAttributes { get; internal set; }

        public Guid? ImpersonatingUserId { get; internal set; }

        public bool AsyncAutoDelete { get; internal set; }

        public IEnumerable<IImageSpecification> ImageSpecifications { get; internal set; }
    }
}
