using System;
using DG.Some.Namespace;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DG.Delegate.TSTOnboarding.Plugins
{
    // Late-bound re-creation of the old early-bound IncidentDeleteAllRelatedResolutionsOnClose plugin
    // (the Incident early-bound type was removed). On an incident state change Active -> not-Active for
    // the test incident, it deletes all related incidentresolution records. Registered via
    // IPluginMetadata in XrmMockupFixture (incident Update, post-op, pre+post images on statecode).
    public class IncidentDeleteResolutionsOnClose : PluginNonDaxif
    {
        public override void Execute(IServiceProvider serviceProvider)
        {
            var localContext = new LocalPluginContext(serviceProvider);
            if (localContext.PluginExecutionContext.Depth > 1) return;

            if (!localContext.PluginExecutionContext.PreEntityImages.Contains("PreImage")) return;
            var preImage = localContext.PluginExecutionContext.PreEntityImages["PreImage"];
            if (preImage.GetAttributeValue<string>("title") != "TestRemovalOfResolutionsAfterClose") return;

            var postImage = localContext.PluginExecutionContext.PostEntityImages.Contains("PostImage")
                ? localContext.PluginExecutionContext.PostEntityImages["PostImage"]
                : null;

            var preState = preImage.GetAttributeValue<OptionSetValue>("statecode")?.Value;
            var postState = postImage?.GetAttributeValue<OptionSetValue>("statecode")?.Value;

            // incident statecode: Active = 0. Delete resolutions when leaving the Active state.
            if (preState == 0 && postState != 0)
            {
                var service = localContext.OrganizationService;
                var query = new QueryExpression("incidentresolution")
                {
                    ColumnSet = new ColumnSet(false),
                    Criteria =
                    {
                        Conditions = { new ConditionExpression("incidentid", ConditionOperator.Equal, localContext.PluginExecutionContext.PrimaryEntityId) }
                    }
                };
                foreach (var resolution in service.RetrieveMultiple(query).Entities)
                {
                    service.Delete(resolution.LogicalName, resolution.Id);
                }
            }
        }
    }
}
