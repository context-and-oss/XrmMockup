
namespace DG.Some.Namespace {
    using System;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using XrmPluginCore;
    using XrmPluginCore.Enums;

    public class AccountChainPostPlugin : Plugin {

        public AccountChainPostPlugin() {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Execute)
                .AddFilteredAttributes(x => x.Fax);
#pragma warning restore CS0618 // Type or member is obsolete

        }

        protected void Execute(LocalPluginContext localContext) {
            if (localContext == null) {
                throw new ArgumentNullException("localContext");
            }


            var service = localContext.OrganizationService;

            var rand = new Random();

            var newAcc = new Account(localContext.PluginExecutionContext.PrimaryEntityId) {
                Fax = rand.Next().ToString()
            };
            service.Update(newAcc);
        }
    }
}
