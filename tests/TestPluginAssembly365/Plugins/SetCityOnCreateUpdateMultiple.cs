using System;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using XrmPluginCore;
using XrmPluginCore.Enums;
using Microsoft.Xrm.Sdk;

namespace DG.Some.Namespace
{
    public class SetCityOnCreateUpdateMultiple : Plugin
    {
        public SetCityOnCreateUpdateMultiple()
        {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Contact>(
                EventOperation.CreateMultiple,
                ExecutionStage.PreOperation,
                Execute);

            RegisterPluginStep<Contact>(
                EventOperation.UpdateMultiple,
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

            var targets = localContext.PluginExecutionContext.InputParameters["Targets"] as EntityCollection;

            if (targets == null)
            {
                throw new InvalidPluginExecutionException("Invalid request type.");
            }

            foreach (var entity in targets.Entities)
            {
                entity["address2_city"] = "Copenhagen";
            }
        }
    }
}
