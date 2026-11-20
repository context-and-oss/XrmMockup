namespace DG.Some.Namespace.Test
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using XrmPluginCore.Enums;
    using XrmPluginCore;
    using TestPluginAssembly365.Plugins.SyncAsyncTest;
    using XrmPluginCore.Extensions;

    public class Test1Plugin2 : TestPlugin
    {
        public Test1Plugin2()
        {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Sync2NameUpdate)
                .AddImage(ImageType.PostImage, x => x.Name)
                .AddFilteredAttributes(x => x.EMailAddress1)
                .SetExecutionMode(ExecutionMode.Synchronous)
                .SetExecutionOrder(2);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected void Sync2NameUpdate(LocalPluginContext localContext)
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
                Name = account.Name + "Sync2"
            };
            service.Update(accountUpd);
        }
    }
}
