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
using System.Xml.Linq;
using DG.Tools.XrmMockup.Internal;

namespace DG.Tools.XrmMockup
{
    internal class IsValidStateTransitionRequestHandler : RequestHandler
    {
        internal IsValidStateTransitionRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "IsValidStateTransition") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<IsValidStateTransitionRequest>(orgRequest);

            if (request.Entity == null)
            {
                throw new FaultException("Required field 'Entity' is missing");
            }
            if (request.NewState == null)
            {
                throw new FaultException("Required field 'NewState' is missing");
            }

            var entityMetadata = metadata.EntityMetadata.GetMetadata(request.Entity.LogicalName);

            var row = db.GetDbRowOrNull(request.Entity);
            if (row == null)
            {
                throw new FaultException($"{request.Entity.LogicalName} With Id = {request.Entity.Id} Does Not Exist");
            }

            var prevStatusCode = row.GetColumn<int>("statuscode");
            var prevStateCode = row.GetColumn<int>("statecode");

            var stateOption = Utility.GetStateOptionMetadataFromInvariantName(request.NewState, entityMetadata);
            CheckEnforceStateTransitions(request, entityMetadata, stateOption);
            var statusOptionMeta = Utility.GetStatusOptionMetadata(entityMetadata);

            if (!statusOptionMeta.Any(o => (o as StatusOptionMetadata).State == prevStateCode
                && o.Value == prevStatusCode
                && stateOption.Value != prevStateCode))
            {
                throw new FaultException($"{request.NewStatus} is not a valid status code on {request.Entity.LogicalName} with Id {request.Entity.Id}.");
            }

            var prevValueOptionMeta = statusOptionMeta.FirstOrDefault(o => o.Value == prevStatusCode) as StatusOptionMetadata;

            var transitions = prevValueOptionMeta.TransitionData;
            var resp = new IsValidStateTransitionResponse();
            if (transitions != null && transitions != "")
            {
                if (Utility.IsValidStatusTransition(transitions, request.NewStatus))
                {
                    resp.Results["IsValid"] = true;
                }
                else
                {
                    throw new FaultException($"{request.NewStatus} is not a valid status code for state code {request.Entity.LogicalName}State.{request.NewState} on {request.Entity.LogicalName} with Id {request.Entity.Id}.");
                }
            }
            else
            {
                resp.Results["IsValid"] = null;
            }
            return resp;
        }

        private static void CheckEnforceStateTransitions(IsValidStateTransitionRequest request, EntityMetadata entityMetadata, OptionMetadata stateOption)
        {
            if (entityMetadata.EnforceStateTransitions != true)
            {
                var tryState = stateOption != null ? stateOption.Value.ToString() : "-1";
                throw new FaultException($"This message can not be used to check the state transition of {request.Entity.LogicalName} to {tryState}");
            }
            if (stateOption == null)
            {
                throw new FaultException($"-1 is not a valid state code on {request.Entity.LogicalName} with Id {request.Entity.Id}.");
            }
        }
    }
}
