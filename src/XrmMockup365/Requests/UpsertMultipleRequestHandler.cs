using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.Linq;
using DG.Tools.XrmMockup.Database;
using System.ServiceModel;
using XrmPluginCore.Enums;

namespace DG.Tools.XrmMockup
{
    internal class UpsertMultipleRequestHandler : RequestHandler
    {
        internal UpsertMultipleRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) 
            : base(core, db, metadata, security, nameof(EventOperation.UpsertMultiple))
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<UpsertMultipleRequest>(orgRequest);

            if (string.IsNullOrEmpty(request.Targets.EntityName))
            {
                throw new FaultException("The required field 'EntityName' is missing.");
            }

            var mismatchedEntity = request.Targets.Entities.FirstOrDefault(e => e.LogicalName != request.Targets.EntityName);
            if (mismatchedEntity != null)
            {
                throw new FaultException($"The entity logical name '{mismatchedEntity.LogicalName}' does not match the expected entity logical name '{request.Targets.EntityName}'.");
            }

            var seenIds = new HashSet<Guid>();

            var results = request.Targets.Entities.Select(entity =>
            {
                if (seenIds.Contains(entity.Id))
                {
                    throw new FaultException($"Duplicate Ids are not allowed in the Target list of an {nameof(UpsertMultipleRequest)}: {entity.Id}.");
                }
                seenIds.Add(entity.Id);

                var upsertRequest = new UpsertRequest
                {
                    Target = entity
                };

                return core.Execute(upsertRequest, userRef);
            }).Cast<UpsertResponse>().ToArray();

            var response = new UpsertMultipleResponse();
            response["Results"] = results;

            return response;
        }
    }
}
