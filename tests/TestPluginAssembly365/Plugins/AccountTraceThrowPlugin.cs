namespace DG.Some.Namespace {
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using TestPluginAssembly365.Plugins.SyncAsyncTest;
    using XrmPluginCore;
    using XrmPluginCore.Enums;

    /// <summary>
    /// Test plugin that traces and then throws, used to verify that XrmMockup captures
    /// exception details in the grouped plugin trace log.
    /// </summary>
    public class AccountTraceThrowPlugin : TestPlugin {
        public const string ThrowMessage = "AccountTraceThrowPlugin failed on purpose";

        public AccountTraceThrowPlugin() {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Account>(
                EventOperation.Create,
                ExecutionStage.PreOperation,
                Execute);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected void Execute(LocalPluginContext localContext) {
            if (localContext == null) {
                throw new ArgumentNullException("localContext");
            }

            localContext.TracingService.Trace("About to throw");
            throw new InvalidPluginExecutionException(ThrowMessage);
        }
    }
}
