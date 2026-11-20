namespace DG.Some.Namespace.Test
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using XrmPluginCore.Enums;
    using XrmPluginCore;
    using TestPluginAssembly365.Plugins.SyncAsyncTest;

    public class ASyncWithExecutionOrder : TestPlugin
    {
        public ASyncWithExecutionOrder()
        {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Contact>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                ExecuteASyncWithExecutionOrder)
                .SetExecutionMode(ExecutionMode.Asynchronous)
                .SetExecutionOrder(1)
                .AddFilteredAttributes(x => x.EMailAddress1);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected void ExecuteASyncWithExecutionOrder(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            var personel = Contact.Retrieve(service, localContext.PluginExecutionContext.PrimaryEntityId, x => x.FirstName);

            var personelUpd = new Contact(personel.Id)
            {
                FirstName = personel.FirstName + ", Async",
            };

            service.Update(personelUpd);
        }
    }
}
