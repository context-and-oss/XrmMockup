namespace DG.Some.Namespace.Test
{
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using Microsoft.Xrm.Sdk;
    using System;
    using TestPluginAssembly365.Plugins.SyncAsyncTest;
    using XrmPluginCore;
    using XrmPluginCore.Enums;
    using XrmPluginCore.Extensions;

    public class Test8Plugin3 : TestPlugin
    {
        public Test8Plugin3()
        {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                ASync2NameUpdate)
                .AddImage(ImageType.PostImage, (x => x.Name))
                .AddFilteredAttributes(x => x.EMailAddress1)
                .SetExecutionMode(ExecutionMode.Asynchronous);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected void ASync2NameUpdate(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            var account = localContext.GetPostImage<Account>();

            var accountUpd = new Account(account.Id)
            {
                Name = account.Name + "ASync2",
            };
            service.Update(accountUpd);
        }
    }
}
