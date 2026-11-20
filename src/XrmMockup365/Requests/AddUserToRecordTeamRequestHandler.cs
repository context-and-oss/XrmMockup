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
using Microsoft.Xrm.Sdk.Metadata.Query;

namespace DG.Tools.XrmMockup
{
    internal class AddUserToRecordTeamRequestHandler : RequestHandler
    {
        internal AddUserToRecordTeamRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "AddUserToRecordTeam") { }

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

            var addedUserPrivs = security.GetPrincipalPrivilege((Guid)orgRequest["SystemUserId"]);
            var addedPrivs = addedUserPrivs[entityMetadata.Value.LogicalName];

            //also check that the user being added has rights on the entity which match the access team
            foreach (var right in Enum.GetValues(typeof(AccessRights)))
            {
                if ((ttRow.GetAttributeValue<int>("defaultaccessrightsmask") & (int)right) > 0)
                {
                    if (!addedPrivs.ContainsKey((AccessRights)right))
                    {
                        throw new FaultException($"Added user cannot join team as does not have {Enum.GetName(typeof(AccessRights), right)} permission on the {entityMetadata.Value.LogicalName} entity");
                    }
                }
            }
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            //validate that the team template exists
            var ttId = (Guid)orgRequest["TeamTemplateId"];
            var ttRow = core.GetDbRow(new EntityReference("teamtemplate", ttId)).ToEntity();

            var record = orgRequest["Record"] as EntityReference;

            var accessTeam = security.GetAccessTeam(ttId, record.Id);
            if (accessTeam == null)
            {
                accessTeam = security.AddAccessTeam(ttId, record);
            }
            
            var membershiprow = security.GetTeamMembership(accessTeam.Id, (Guid)orgRequest["SystemUserId"]);
            if (membershiprow==null)
            {
                membershiprow = security.AddTeamMembership(accessTeam.Id, (Guid)orgRequest["SystemUserId"]);

                var poa = security.GetPOA((Guid)orgRequest["SystemUserId"], record.Id);

                if (poa == null)
                {
                    poa = security.AddPOA((Guid)orgRequest["SystemUserId"], record, ttRow.GetAttributeValue<int>("defaultaccessrightsmask"));
                }
                else
                {
                    //update the existing poa to include the new privlege
                    security.UpdatePOAMask(poa.Id, ttRow.GetAttributeValue<int>("defaultaccessrightsmask"));
                }
            }

            var resp = new AddUserToRecordTeamResponse();
            resp.Results["AccessTeamId"] = accessTeam.Id;
            return resp;
        }
    }
}
