namespace DG.Some.Namespace {
    using System;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using TestPluginAssembly365.Plugins.SyncAsyncTest;
    using XrmPluginCore;
    using XrmPluginCore.Enums;

    /// <summary>
    /// Test plugin that traces where the optional 'tag' request parameter is visible in each stage of a Create.
    /// </summary>
    public class AccountTagPlugin : TestPlugin {
        public AccountTagPlugin() {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PreValidation, Execute);
            RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PreOperation, Execute);
            RegisterPluginStep<Account>(EventOperation.Create, ExecutionStage.PostOperation, Execute);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected void Execute(LocalPluginContext localContext) {
            if (localContext == null) {
                throw new ArgumentNullException("localContext");
            }

            var context = localContext.PluginExecutionContext;
            context.SharedVariables.TryGetValue("tag", out var tag);
            localContext.TracingService.Trace("Stage {0}: tag={1}, inInputParameters={2}",
                context.Stage, tag, context.InputParameters.ContainsKey("tag"));
        }
    }
}
