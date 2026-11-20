
namespace DG.Some.Namespace {
    using System;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using TestPluginAssembly365.Plugins.SyncAsyncTest;
    using XrmPluginCore;
    using XrmPluginCore.Enums;

    /// <summary>
    /// Test plugin that deletes the target account in a post-Update operation.
    /// Used to verify that XrmMockup handles record deletion inside a post-operation plugin gracefully.
    /// </summary>
    public class AccountDeleteInPostPlugin : TestPlugin {
        public AccountDeleteInPostPlugin() {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Execute);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected void Execute(LocalPluginContext localContext) {
            if (localContext == null) {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;
            service.Delete(Account.EntityLogicalName, localContext.PluginExecutionContext.PrimaryEntityId);
        }
    }
}
