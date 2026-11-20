namespace DG.Some.Namespace
{
    using System.Linq;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using XrmPluginCore;
    using XrmPluginCore.Enums;

    /// <summary>
    /// Diagnostic plugin used by the system-field alignment tests. When a Contact is created with
    /// <see cref="Marker"/> as its first name, it records which system-managed attributes are present
    /// in the Target at each stage (and in the post-image) into observable string fields:
    ///   address1_line1 -> PreValidation Target
    ///   address1_line2 -> PreOperation Target
    ///   address1_line3 -> PostOperation post-image
    ///   address1_city  -> PostOperation Target
    /// This lets tests assert that system fields are absent from the pre-stage Target (matching
    /// Dataverse) but present in the post-image and post-operation Target.
    /// </summary>
    public class ContactSystemFieldsProbePlugin : Plugin
    {
        public const string Marker = "ProbeSystemFields";

        private static readonly string[] SystemFields =
            { "ownerid", "createdon", "createdby", "modifiedon", "modifiedby", "statecode", "statuscode" };

        public ContactSystemFieldsProbePlugin()
        {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<Contact>(EventOperation.Create, ExecutionStage.PreValidation, ExecutePreValidation);
            RegisterPluginStep<Contact>(EventOperation.Create, ExecutionStage.PreOperation, ExecutePreOperation);
            RegisterPluginStep<Contact>(EventOperation.Create, ExecutionStage.PostOperation, ExecutePostOperation)
                .WithPostImage();
#pragma warning restore CS0618
        }

        internal static string Flags(Entity entity) =>
            string.Join(",", SystemFields.Where(entity.Contains));

        private static Entity GetProbeTarget(LocalPluginContext localContext)
        {
            var target = localContext.PluginExecutionContext.InputParameters["Target"] as Entity;
            return target?.GetAttributeValue<string>("firstname") == Marker ? target : null;
        }

        private void ExecutePreValidation(LocalPluginContext localContext)
        {
            var target = GetProbeTarget(localContext);
            if (target == null) return;
            target["address1_line1"] = Flags(target);
        }

        private void ExecutePreOperation(LocalPluginContext localContext)
        {
            var target = GetProbeTarget(localContext);
            if (target == null) return;
            target["address1_line2"] = Flags(target);
        }

        private void ExecutePostOperation(LocalPluginContext localContext)
        {
            var target = GetProbeTarget(localContext);
            if (target == null) return;

            var postImage = localContext.PluginExecutionContext.PostEntityImages.Values.First();

            // Post-operation writes to Target are not persisted, so record the observations via an update.
            var record = new Entity(target.LogicalName, target.Id)
            {
                ["address1_line3"] = Flags(postImage),
                ["address1_city"] = Flags(target),
            };
            localContext.OrganizationService.Update(record);
        }
    }
}
