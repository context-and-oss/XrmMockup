namespace DG.Some.Namespace.Test
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using XrmPluginCore.Enums;
    using XrmPluginCore;
    using TestPluginAssembly365.Plugins.SyncAsyncTest;

    public class Test6Plugin1Sync : TestPlugin
    {
        public Test6Plugin1Sync()
        {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Contact>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Sync1NameAndEmailAddress2Update)
                .AddFilteredAttributes(x => x.EMailAddress1);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected void Sync1NameAndEmailAddress2Update(LocalPluginContext localContext)
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
                EMailAddress2 = "trigger@valid.com"
            };

            service.Update(personelUpd);
        }
    }
}
