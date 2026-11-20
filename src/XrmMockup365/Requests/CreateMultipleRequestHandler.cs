using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.Linq;
using System.ServiceModel;
using DG.Tools.XrmMockup.Database;
using XrmPluginCore.Enums;

namespace DG.Tools.XrmMockup
{
    internal class CreateMultipleRequestHandler : RequestHandler
    {
        internal CreateMultipleRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) 
            : base(core, db, metadata, security, nameof(EventOperation.CreateMultiple))
        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<CreateMultipleRequest>(orgRequest);

            if (string.IsNullOrEmpty(request.Targets.EntityName))
            {
                throw new FaultException("The required field 'EntityName' is missing.");
            }

            var mismatchedEntity = request.Targets.Entities.FirstOrDefault(e => e.LogicalName != request.Targets.EntityName);
            if (mismatchedEntity != null)
            {
                throw new FaultException($"The entity logical name '{mismatchedEntity.LogicalName}' does not match the expected entity logical name '{request.Targets.EntityName}'.");
            }

            var ids =
                request.Targets.Entities.Select(entity =>
                {
                    var createRequest = new CreateRequest
                    {
                        Target = entity
                    };
                    var createResponse = core.Execute(createRequest, userRef) as CreateResponse;
                    return createResponse.id;
                }).ToArray();

            var response = new CreateMultipleResponse();
            response.Results["Ids"] = ids;
            return response;
        }
    }
}
