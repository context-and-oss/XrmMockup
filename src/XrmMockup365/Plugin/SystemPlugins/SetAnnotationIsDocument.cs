using System;
using DG.Tools.XrmMockup.Internal;
using XrmPluginCore.Enums;
using Microsoft.Xrm.Sdk;

namespace DG.Tools.XrmMockup.SystemPlugins
{
    internal class SetAnnotationIsDocument : AbstractSystemPlugin
    {
        // Register when/how to execute
        public SetAnnotationIsDocument()
        {
            RegisterPluginStep(LogicalNames.Annotation,
                EventOperation.Create,
                ExecutionStage.PreOperation,
                Execute);

            RegisterPluginStep(LogicalNames.Annotation,
                EventOperation.Update,
                ExecutionStage.PreOperation,
                Execute);
        }

        // Execute plugin logic
        protected void Execute(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException(nameof(localContext));
            }

            var targetAnnotation = localContext.PluginExecutionContext.InputParameters["Target"] as Entity;
            if (!targetAnnotation.Contains("documentbody")) return;

            var documentBody = targetAnnotation.GetAttributeValue<string>("documentbody");
            
            targetAnnotation["isdocument"] = !string.IsNullOrEmpty(documentBody);
        }
    }
}
