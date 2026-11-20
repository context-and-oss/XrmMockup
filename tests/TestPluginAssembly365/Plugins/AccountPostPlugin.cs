
namespace DG.Some.Namespace {
    using System;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using XrmPluginCore;
    using XrmPluginCore.Enums;

    public class AccountPostPlugin : Plugin {

        public AccountPostPlugin() {

#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Execute);

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

            var service = localContext.OrganizationService;

            var rand = new Random();
            // Migrated from Lead -> ctx_parent (the Lead replacement): ctx_Name replaces Subject and
            // ctx_AccountId replaces ParentAccountId. Several account->child join tests in
            // TestRetrieveMultiple rely on these account-linked records being created here.
            service.Create(new ctx_parent() {
                ctx_Name = "Some new lead " + rand.Next(0, 1000),
                ctx_AccountId = new Account(localContext.PluginExecutionContext.PrimaryEntityId).ToEntityReference()
            });

            service.Create(new ctx_parent() {
                ctx_Name = "Some other lead " + rand.Next(0, 1000),
                ctx_AccountId = new Account(localContext.PluginExecutionContext.PrimaryEntityId).ToEntityReference()
            });
        }
    }
}
