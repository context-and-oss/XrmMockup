namespace DG.Some.Namespace.Test
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using XrmPluginCore.Enums;
    using XrmPluginCore;
    using TestPluginAssembly365.Plugins.SyncAsyncTest;

    public class Test4PluginSync : TestPlugin
    {
        public Test4PluginSync()
        {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Contact>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Sync1NameUpdate)
                .AddFilteredAttributes(x => x.EMailAddress1);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected void Sync1NameUpdate(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            var personel = Contact.Retrieve(service, localContext.PluginExecutionContext.PrimaryEntityId, x => x.FirstName);

            var personelUpd = new Contact(personel.Id)
            {
                FirstName = personel.FirstName + "Sync1",
            };

            service.Update(personelUpd);
        }
    }
}
