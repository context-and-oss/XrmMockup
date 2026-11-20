
using System;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace TestPluginAssembly365.Plugins.LegacyDaxif {
    public class LegacyAccountPlugin : LegacyPlugin {

        public LegacyAccountPlugin() : base(typeof(LegacyAccountPlugin)) {
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Execute);

            RegisterPluginStep<Account>(
                EventOperation.Create,
                ExecutionStage.PostOperation,
                Execute);
        }

        protected void Execute(LocalPluginContext localContext) {
            if (localContext == null) {
                throw new ArgumentNullException(nameof(localContext));
            }

            if (localContext.PluginExecutionContext == null)
            {
                throw new ArgumentNullException(nameof(localContext.PluginExecutionContext));
            }

            if (localContext.OrganizationService == null)
            {
                throw new ArgumentNullException(nameof(localContext.OrganizationService));
            }

            if (localContext.TracingService == null)
            {
                throw new ArgumentNullException(nameof(localContext.TracingService));
            }

            if (localContext.OrganizationAdminService == null)
            {
                throw new ArgumentNullException(nameof(localContext.OrganizationAdminService));
            }

            var service = localContext.OrganizationService;

            var account = GetEntity<Account>(localContext);
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            var rand = new Random();
            // Migrated from Lead -> ctx_parent (see AccountPostPlugin for rationale).
            service.Create(new ctx_parent() {
                ctx_Name = nameof(LegacyAccountPlugin) + " " + localContext.PluginExecutionContext.MessageName + ": Some new lead " + rand.Next(0, 1000),
                ctx_AccountId = new Account(localContext.PluginExecutionContext.PrimaryEntityId).ToEntityReference()
            });
        }
    }
}
