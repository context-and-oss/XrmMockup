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

namespace DG.Tools.XrmMockup
{
    internal class RemoveUserFromRecordTeamRequestHandler : RequestHandler
    {
        internal RemoveUserFromRecordTeamRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RemoveUserFromRecordTeam") { }

        internal override void CheckSecurity(OrganizationRequest orgRequest, EntityReference userRef)
        {
            //check that the caller has share permission on the entity
            var ttId = (Guid)orgRequest["TeamTemplateId"];
            var ttRow = core.GetDbRow(new EntityReference("teamtemplate", ttId)).ToEntity();
            var record = orgRequest["Record"] as EntityReference;

            var callingUserPrivs = security.GetPrincipalPrivilege(userRef.Id);

            var entityMetadata = metadata.EntityMetadata.Single(x => x.Value.ObjectTypeCode.HasValue && x.Value.ObjectTypeCode.Value == ttRow.GetAttributeValue<int>("objecttypecode"));

            var callingPrivs = callingUserPrivs[entityMetadata.Value.LogicalName];

            if (!callingPrivs.ContainsKey(AccessRights.ShareAccess))
            {
                throw new FaultException($"User does not have share permission on the {entityMetadata.Value.LogicalName} entity");
            }

            //also check that the calling user has rights on the record which match the rights being assigned by the access team
            foreach (var right in Enum.GetValues(typeof(AccessRights)))
            {
                if ((ttRow.GetAttributeValue<int>("defaultaccessrightsmask") & (int)right) > 0)
                {
                    if (!security.HasPermission(record, (AccessRights)right, userRef))
                    {
                        throw new FaultException($"User does not have {Enum.GetName(typeof(AccessRights), right)} permission on the {entityMetadata.Value.LogicalName} entity with id {record.Id}");
                    }
                }
            }
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            //validate that the team template exists
            var ttId = (Guid)orgRequest["TeamTemplateId"];

            var record = orgRequest["Record"] as EntityReference;

            var accessTeam = security.GetAccessTeam(ttId, record.Id);

            var membershiprow = security.GetTeamMembership(accessTeam.Id, (Guid)orgRequest["SystemUserId"]);
            db.Delete(membershiprow);

            if (membershiprow != null)
            {
                var poa = security.GetPOA((Guid)orgRequest["SystemUserId"], record.Id);

                if (poa != null)
                {
                    //we need t update the poa record with the access masks from the the access teams the user is left in
                    //get the users remaining team memberships
                    var remainingAccessTeams = security.GetAccessTeams(record.Id);
                    int mask = 0;
                    foreach (var remainingAccessTeam in remainingAccessTeams)
                    {
                        var ttRow = core.GetDbRow(new EntityReference("teamtemplate", remainingAccessTeam.GetAttributeValue<EntityReference>("teamtemplateid").Id)).ToEntity();
                        var remainingTeamMembership = security.GetTeamMembership(remainingAccessTeam.Id, (Guid)orgRequest["SystemUserId"]);
                        if (remainingTeamMembership != null)
                        {
                            mask = mask | ttRow.GetAttributeValue<int>("defaultaccessrightsmask");
                        }
                    }
                    security.OverwritePOAMask(poa.Id, mask);
                }
            }

            var resp = new RemoveUserFromRecordTeamResponse();
            resp.Results["AccessTeamId"] = accessTeam.Id;
            return resp;
        }
    }
}