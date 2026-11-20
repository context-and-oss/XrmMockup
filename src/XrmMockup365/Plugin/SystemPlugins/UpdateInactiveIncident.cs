using System;
using System.Linq;
using DG.Tools.XrmMockup.Internal;
using XrmPluginCore.Enums;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DG.Tools.XrmMockup.SystemPlugins
{
    internal class UpdateInactiveIncident : AbstractSystemPlugin
    {
        // Register when/how to execute
        public UpdateInactiveIncident()
        {
            RegisterPluginStep(LogicalNames.Incident,
                EventOperation.Update,
                ExecutionStage.PreValidation,
                Execute);
        }

        // Execute plugin logic
        protected void Execute(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var orgService = localContext.OrganizationService;

            var retrievedTarget = orgService.Retrieve("incident", localContext.PluginExecutionContext.PrimaryEntityId, new ColumnSet("incidentid","statecode"));
            var stateCode = retrievedTarget.GetAttributeValue<OptionSetValue>("statecode").Value;

            var incident =
                (localContext.PluginExecutionContext.InputParameters["Target"] as Entity);

            string[] legalUpdates = new [] {"ownerid", "owneridyominame", "owneridtype", "owninguser",
                                         "statecode", "statuscode", "modifiedon", "modifiedby",
                                         "modifiedonbehalfby", "owningbusinessunit", "processid", "incidentid" };

            string errorMessage = "Only the following fields can be edited for inactive incident: " + string.Join(", ", legalUpdates.Select(x => "\"" + x + "\""));

            var illegalUpdates = incident.Attributes.Keys.Except(legalUpdates);

            if (illegalUpdates.Count() > 0 && (stateCode == 1 || stateCode == 2))
            {
                throw new System.ServiceModel.FaultException(errorMessage);
            } 
        }
    }
}
