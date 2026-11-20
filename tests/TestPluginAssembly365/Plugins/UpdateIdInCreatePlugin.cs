namespace DG.Some.Namespace
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using XrmPluginCore;
    using XrmPluginCore.Enums;

    public class UpdateIdInCreatePlugin : Plugin
    {
        public UpdateIdInCreatePlugin()
        {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Contact>(
                EventOperation.Create,
                ExecutionStage.PostOperation,
                Execute);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected void Execute(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;
            var con = localContext.PluginExecutionContext.InputParameters["Target"] as Contact;

            // Migrated from dg_child -> Contact (dg_name -> LastName). This plugin is registered
            // globally on Contact Create, so it is gated on a sentinel LastName to avoid mutating
            // every contact created across the suite (same gating pattern used elsewhere in these
            // test plugins). Only the create in TestCreate.TestCreateWithUpdate uses this sentinel.
            if (con?.LastName != "Donald Duck")
            {
                return;
            }

            service.Update(new Contact()
            {
                Id = con.Id,
                LastName = "Micky Mouse",
            });
        }
    }
}
