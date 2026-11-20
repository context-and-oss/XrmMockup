namespace DG.Some.Namespace {
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using XrmPluginCore;
    using XrmPluginCore.Enums;

    public class TaskFilteredAttributesProbePlugin : Plugin {
        public TaskFilteredAttributesProbePlugin() {
#pragma warning disable CS0618 // Type or member is obsolete
            RegisterPluginStep<Task>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Execute)
                .AddFilteredAttributes(
                    x => x.StatusCode,
                    x => x.StateCode,
                    x => x.Description);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected void Execute(LocalPluginContext localContext)
        {
            var target = (Entity)localContext.PluginExecutionContext.InputParameters["Target"];

            // Stamping the marker is itself an update, so skip it when this execution is that update.
            if (target.Contains("category")) return;

            localContext.OrganizationService.Update(new Task { Id = target.Id, Category = "FilterMatched" });
        }
    }
}
