using Microsoft.Xrm.Sdk;
using System.Linq;
using DG.Tools.XrmMockup.Database;
using Microsoft.Xrm.Sdk.Messages;

namespace DG.Tools.XrmMockup
{
    internal class RetrieveAllEntitiesRequestHandler : RequestHandler
    {
        internal RetrieveAllEntitiesRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RetrieveAllEntities") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<RetrieveAllEntitiesRequest>(orgRequest);

            var response = new RetrieveAllEntitiesResponse();
            response.Results["EntityMetadata"] = metadata.EntityMetadata.Values
                .Select(entityMetadata => RetrieveEntityRequestHandler.FilterEntityMetadataProperties(entityMetadata, request.EntityFilters)).ToArray();

            return response;
        }
    }
}
