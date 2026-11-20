using DG.XrmFramework.BusinessDomain.ServiceContext;
using XrmPluginCore;
using XrmPluginCore.Enums;
using Microsoft.Xrm.Sdk;


namespace DG.Some.Namespace
{
    public class RetrievePlugin : Plugin
    {
        public RetrievePlugin()
        {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Contact>(
                EventOperation.Retrieve,
                ExecutionStage.PostOperation,
                ExecutePostRetrieve);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected void ExecutePostRetrieve(LocalPluginContext localContext)
        {
            var entity = localContext.PluginExecutionContext.OutputParameters["BusinessEntity"] as Entity;

            if (entity.ToEntity<Contact>().StateCode == contact_statecode.Inactive)
            {
                throw new InvalidPluginExecutionException("Inactive contacts cannot be retrieved.");
            }
        }
    }
}
