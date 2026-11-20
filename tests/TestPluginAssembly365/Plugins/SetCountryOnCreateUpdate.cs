using System;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using XrmPluginCore;
using XrmPluginCore.Enums;
using Microsoft.Xrm.Sdk;

namespace DG.Some.Namespace
{
    public class SetCountryOnCreateUpdate : Plugin
    {
        public SetCountryOnCreateUpdate()
        {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Contact>(
                EventOperation.Create,
                ExecutionStage.PreOperation,
                Execute);

            RegisterPluginStep<Contact>(
                EventOperation.Update,
                ExecutionStage.PreOperation,
                Execute);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected void Execute(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var target = localContext.PluginExecutionContext.InputParameters["Target"] as Entity;

            if (target == null)
            {
                throw new InvalidPluginExecutionException("Invalid request type.");
            }

            target["address2_country"] = "Denmark";
        }
    }
}
