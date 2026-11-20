using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;

namespace DG.Tools.XrmMockup.Internal
{
    /// <summary>
    /// Carries all intermediate state for a single request execution through the pipeline stages.
    /// Populated progressively: BuildContext → PreValidation → PreOperation → Operation → PostOperation.
    /// </summary>
    internal class ExecutionPipelineContext
    {
        // Immutable inputs — set once during BuildPipelineContext
        public OrganizationRequest Request { get; set; }
        public EntityReference UserRef { get; set; }
        public PluginContext ParentPluginContext { get; set; }
        public MockupServiceSettings Settings { get; set; }

        // Derived during BuildPipelineContext
        public PluginContext PluginContext { get; set; }
        public string RequestMessage { get; set; }
        public Tuple<object, string, Guid> EntityInfo { get; set; }
        public EntityReference PrimaryRef { get; set; }
        public EntityCollection EntityCollection { get; set; }
        public bool ShouldTrigger { get; set; }

        // Images — populated at specific stage boundaries
        public Entity PreImage { get; set; }       // fetched before PreValidation
        public Entity SyncPostImage { get; set; }  // fetched at start of PostOperation (sync)
        public Entity AsyncPostImage { get; set; } // fetched before async staging

        // System-managed attributes copied onto the Target at the start of PostOperation. They are
        // visible to post-operation plugins (as in Dataverse) but must not count towards Update
        // filtering attributes, since the caller never asked for them to change.
        public ISet<string> SystemInjectedAttributes { get; set; }

        // Output — set by the main operation stage
        public OrganizationResponse Response { get; set; }
    }
}
