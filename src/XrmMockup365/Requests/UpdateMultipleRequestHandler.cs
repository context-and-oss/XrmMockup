using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;
using XrmPluginCore.Enums;

namespace DG.Tools.XrmMockup
{
    internal class UpdateMultipleRequestHandler : RequestHandler
    {
        internal UpdateMultipleRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) 
            : base(core, db, metadata, security, nameof(EventOperation.UpdateMultiple))
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<UpdateMultipleRequest>(orgRequest);

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
            foreach (var entity in request.Targets.Entities)
            {
                if (seenIds.Contains(entity.Id))
                {
                    continue;
                }

                seenIds.Add(entity.Id);
                var updateRequest = new UpdateRequest
                {
                    Target = entity
                };

                core.Execute(updateRequest, userRef);
            }

            return new UpdateMultipleResponse();
        }
    }
}
