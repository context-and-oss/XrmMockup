using DG.XrmFramework.BusinessDomain.ServiceContext;
using System;
using TestPluginAssembly365.Plugins.SyncAsyncTest;
using XrmPluginCore;
using XrmPluginCore.Enums;
using XrmPluginCore.Extensions;

namespace DG.Some.Namespace.Test
{
    public class TestPlugin0 : TestPlugin
    {
        public TestPlugin0()
        {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Sync0NameUpdate)
                .AddImage(ImageType.PostImage, x => x.Name)
                .AddFilteredAttributes(x => x.EMailAddress1)
                .SetExecutionOrder(0);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected void Sync0NameUpdate(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            var account = localContext.GetPostImage<Account>();

            var accountUpd = new Account(account.Id)
            {
                Name = account.Name + "Sync0"
            };
            service.Update(accountUpd);
        }
    }
}
