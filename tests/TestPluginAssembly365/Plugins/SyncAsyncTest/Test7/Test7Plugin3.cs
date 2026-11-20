namespace DG.Some.Namespace.Test
{
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using Microsoft.Xrm.Sdk;
    using System;
    using TestPluginAssembly365.Plugins.SyncAsyncTest;
    using XrmPluginCore;
    using XrmPluginCore.Enums;
    using XrmPluginCore.Extensions;

    public class Test7Plugin3 : TestPlugin
    {
        public Test7Plugin3()
        {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Syn3NameUpdate)
                .AddImage(ImageType.PostImage,(x => x.Name))
                .AddFilteredAttributes(x => x.EMailAddress1)
                .SetExecutionMode(ExecutionMode.Synchronous)
                .SetExecutionOrder(2);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected void Syn3NameUpdate(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            //var account = Account.Retrieve(service, localContext.PluginExecutionContext.PrimaryEntityId, x => x.Name);
            var account = localContext.GetPostImage<Account>();

            var accountUpd = new Account(account.Id)
            {
                Name = account.Name + "Sync3"
            };
            service.Update(accountUpd);
        }
    }
}
