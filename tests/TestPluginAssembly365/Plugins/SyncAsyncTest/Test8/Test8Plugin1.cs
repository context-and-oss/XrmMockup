namespace DG.Some.Namespace.Test
{
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using Microsoft.Xrm.Sdk;
    using System;
    using TestPluginAssembly365.Plugins.SyncAsyncTest;
    using XrmPluginCore;
    using XrmPluginCore.Enums;
    using XrmPluginCore.Extensions;

    public class Test8Plugin1 : TestPlugin
    {
        public Test8Plugin1()
        {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Sync1NameAndEmailAddress2Update)
                .AddImage(ImageType.PostImage,(x => x.Name))
                .AddFilteredAttributes(x => x.EMailAddress1)
                .SetExecutionMode(ExecutionMode.Synchronous)
                .SetExecutionOrder(1);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected void Sync1NameAndEmailAddress2Update(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            var account = localContext.GetPostImage<Account>();

            var accountUpd = new Account(account.Id)
            {
                Name = account.Name + "Sync1",
                EMailAddress2 = account.EMailAddress2
            };
            service.Update(accountUpd);
        }
    }
}
