using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;
using XrmPluginCore.Enums;
using System.Collections.Generic;
using DG.Tools.XrmMockup.Internal;

namespace DG.Tools.XrmMockup.SystemPlugins
{
    internal class DefaultBusinessUnitTeamMembers : AbstractSystemPlugin
    {
        internal DefaultBusinessUnitTeamMembers()
        {
            RegisterPluginStep(LogicalNames.SystemUser,
                EventOperation.Create,
                ExecutionStage.PostOperation,
                AddMember);

            RegisterPluginStep(LogicalNames.SystemUser,
                EventOperation.Delete,
                ExecutionStage.PreOperation,
                RemoveMember);

            RegisterPluginStep(LogicalNames.SystemUser,
                EventOperation.Update,
                ExecutionStage.PostOperation,
                new List<ImageSpecification> { new ImageSpecification { ImageName = nameof(ImageType.PreImage), EntityAlias = nameof(ImageType.PreImage), ImageType = ImageType.PreImage } },
                UpdatePreOperation);
        }

        private void UpdatePreOperation(LocalPluginContext localContext)
        {
            var orgService = localContext.OrganizationService;

            var preSystemUser = localContext.PluginExecutionContext.PreEntityImages.First().Value;
            var postSystemUser = orgService.Retrieve(LogicalNames.SystemUser, localContext.PluginExecutionContext.PrimaryEntityId,
                new ColumnSet("businessunitid"));

            if (postSystemUser.Attributes["businessunitid"] != preSystemUser.Attributes["businessunitid"])
            {
                RemoveMember(orgService, preSystemUser);
                AddMember(localContext);
            }
        }

        private void AddMember(LocalPluginContext localContext)
        {
            var orgService = localContext.OrganizationService;

            var retrievedSystemUser = orgService.Retrieve(LogicalNames.SystemUser, localContext.PluginExecutionContext.PrimaryEntityId, new ColumnSet("businessunitid"));

            var team = GetBusinessUnitDefaultTeam(orgService, retrievedSystemUser);

            var request = new AddMembersTeamRequest
            {
                TeamId = team.Id,
                MemberIds = new[] { retrievedSystemUser.Id }
            };

            orgService.Execute(request);
        }

        private void RemoveMember(LocalPluginContext localContext)
        {
            var orgService = localContext.OrganizationService;

            var retrievedSystemUser = orgService.Retrieve(LogicalNames.SystemUser, localContext.PluginExecutionContext.PrimaryEntityId, new ColumnSet("businessunitid"));

            RemoveMember(orgService, retrievedSystemUser);
        }

        private void RemoveMember(IOrganizationService orgService, Entity systemUser)
        {
            var team = GetBusinessUnitDefaultTeam(orgService, systemUser);

            var request = new RemoveMembersTeamRequest
            {
                TeamId = team.Id,
                MemberIds = new[] { systemUser.Id }
            };

            orgService.Execute(request);
        }

        private Entity GetBusinessUnitDefaultTeam(IOrganizationService orgService, Entity retrievedSystemUser)
        {
            EntityReference businessUnitReference = (EntityReference)retrievedSystemUser.Attributes["businessunitid"];

            var teamQuery = new QueryExpression(LogicalNames.Team);
            teamQuery.ColumnSet = new ColumnSet("isdefault", "businessunitid");
            teamQuery.Criteria.AddCondition("isdefault", ConditionOperator.Equal, true);
            teamQuery.Criteria.AddCondition("businessunitid", ConditionOperator.Equal, businessUnitReference.Id);

            var retrievedTeams = orgService.RetrieveMultiple(teamQuery);

            if (retrievedTeams.Entities.Count > 1)
            {
                throw new FaultException("There cannot be more than one default business unit team!");
            }

            return orgService.RetrieveMultiple(teamQuery).Entities[0];
        }
    }
}
