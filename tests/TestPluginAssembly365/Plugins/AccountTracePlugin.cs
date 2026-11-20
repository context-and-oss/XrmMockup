namespace DG.Some.Namespace {
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using TestPluginAssembly365.Plugins.SyncAsyncTest;
    using XrmPluginCore;
    using XrmPluginCore.Enums;

    /// <summary>
    /// Test plugin that emits trace messages during a post-Create operation.
    /// Used to verify that XrmMockup collects trace messages and exposes them via TraceLog.
    /// </summary>
    public class AccountTracePlugin : TestPlugin {
        public AccountTracePlugin() {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Account>(
                EventOperation.Create,
                ExecutionStage.PostOperation,
                Execute);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected void Execute(LocalPluginContext localContext) {
            if (localContext == null) {
                throw new ArgumentNullException("localContext");
            }

            var target = (Entity)localContext.PluginExecutionContext.InputParameters["Target"];
            localContext.TracingService.Trace("Creating account");
            localContext.TracingService.Trace("Account name is {0}", target.GetAttributeValue<string>("name"));
        }
    }
}
