using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using DG.Tools.XrmMockup.Database;
using DG.Tools.XrmMockup.Internal;

namespace DG.Tools.XrmMockup
{
    internal class RemoveMembersTeamRequestHandler : RequestHandler
    {
        internal RemoveMembersTeamRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RemoveMembersTeam") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<RemoveMembersTeamRequest>(orgRequest);

            // Check if the team exist
            if (!db.HasRow(new EntityReference(LogicalNames.Team, request.TeamId)))
            {
                throw new MockupException($"Team with id {request.TeamId} does not exist");
            }

            var teamMembers = db.GetDBEntityRows(LogicalNames.TeamMembership).Select(x => x.ToEntity()).Where(x => x.GetAttributeValue<Guid>("teamid") == request.TeamId);

            foreach (var userId in request.MemberIds)
            {
                // Check if the user exist
                if (!db.HasRow(new EntityReference(LogicalNames.SystemUser, userId)))
                {
                    throw new MockupException($"User with id {userId} does not exist");
                }
            }

            foreach (var userId in request.MemberIds)
            {
                var teamMember = teamMembers.FirstOrDefault(t => t.GetAttributeValue<Guid>("systemuserid") == userId);
                if (teamMember != null)
                {
                    db.Delete(teamMember);
                }
            }
            return new RemoveMembersTeamResponse();
        }
    }
}
