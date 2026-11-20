using System;
using DG.Some.Namespace;
using Microsoft.Xrm.Sdk;

namespace DG.Delegate.TSTOnboarding.Plugins
{
    // Late-bound re-creation of the old early-bound OpportunityWinLose plugin (the Opportunity early-
    // bound type was removed). On the Win/Lose opportunity messages it stamps the opportunity's
    // description with "SetFromWinLose". Registered via IPluginMetadata (Win + Lose, post-operation).
    public class OpportunityWinLoseDescription : PluginNonDaxif
    {
        public override void Execute(IServiceProvider serviceProvider)
        {
            var localContext = new LocalPluginContext(serviceProvider);
            var ctx = localContext.PluginExecutionContext;
            if (ctx.Depth > 1) return;

            // Both Win and Lose carry the close activity (OpportunityClose) which references the opportunity.
            if (!(ctx.InputParameters.Contains("OpportunityClose") && ctx.InputParameters["OpportunityClose"] is Entity opClose))
                return;
            var opportunityRef = opClose.GetAttributeValue<EntityReference>("opportunityid");
            if (opportunityRef == null) return;

            localContext.OrganizationService.Update(new Entity("opportunity", opportunityRef.Id)
            {
                ["description"] = "SetFromWinLose"
            });
        }
    }
}
