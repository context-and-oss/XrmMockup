using DG.XrmFramework.BusinessDomain.ServiceContext;
using XrmPluginCore;
using XrmPluginCore.Enums;
using Microsoft.Xrm.Sdk;

namespace DG.Some.Namespace
{
    public class ContactPostImageOnUpdatePlugin : Plugin
    {
        public ContactPostImageOnUpdatePlugin()
        {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Contact>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Execute)
                .AddFilteredAttributes(c => c.Description)
                .WithPostImage();
#pragma warning restore CS0618
        }

        protected void Execute(LocalPluginContext localContext)
        {
            // Access post-images to verify they don't throw NullReferenceException
            var postImages = localContext.PluginExecutionContext.PostEntityImages;
            localContext.PluginExecutionContext.SharedVariables["PostImageCount"] = postImages.Count;
        }
    }
}
