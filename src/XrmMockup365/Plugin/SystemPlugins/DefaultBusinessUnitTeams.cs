using DG.Tools.XrmMockup.Internal;
using XrmPluginCore;
using XrmPluginCore.Enums;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.ServiceModel;

namespace DG.Tools.XrmMockup.SystemPlugins
{
    internal class DefaultBusinessUnitTeams : AbstractSystemPlugin
    {
        internal DefaultBusinessUnitTeams()
        {
            RegisterPluginStep(LogicalNames.BusinessUnit,
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Update);

            RegisterPluginStep(LogicalNames.BusinessUnit,
                EventOperation.Delete,
                ExecutionStage.PreOperation,
                Delete);
        }

        private void Delete(LocalPluginContext localContext)
        {
            var orgService = localContext.OrganizationService;

            var retrievedBusinessUnit = orgService.Retrieve(LogicalNames.BusinessUnit, localContext.PluginExecutionContext.PrimaryEntityId, new ColumnSet("name"));

            var team = GetBusinessUnitDefaultTeam(orgService, retrievedBusinessUnit.Id);

            orgService.Delete(LogicalNames.Team, team.Id);
        }

        private void Update(LocalPluginContext localContext)
        {
            var orgService = localContext.OrganizationService;

            var retrievedBusinessUnit = orgService.Retrieve(LogicalNames.BusinessUnit, localContext.PluginExecutionContext.PrimaryEntityId, new ColumnSet("name"));

            var team = GetBusinessUnitDefaultTeam(orgService, retrievedBusinessUnit.Id);

            var newTeam = new Entity(LogicalNames.Team);
            newTeam["name"] = retrievedBusinessUnit.Attributes["name"];
            newTeam.Id = team.Id;

            orgService.Update(newTeam);
        }

        private Entity GetBusinessUnitDefaultTeam(IOrganizationService orgService, Guid businessUnitGuid)
        {
            var teamQuery = new QueryExpression(LogicalNames.Team);
            teamQuery.ColumnSet = new ColumnSet();
            teamQuery.Criteria.AddCondition("isdefault", ConditionOperator.Equal, true);
            teamQuery.Criteria.AddCondition("businessunitid", ConditionOperator.Equal, businessUnitGuid);

            var retrievedTeams = orgService.RetrieveMultiple(teamQuery);

            if (retrievedTeams.Entities.Count > 1)
            {
                throw new FaultException("There cannot be more than one default business unit team!");
            }

            return orgService.RetrieveMultiple(teamQuery).Entities[0];
        }
    }
}
