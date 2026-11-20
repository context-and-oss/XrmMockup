using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace DG.Tools.XrmMockup
{
    internal class RetrieveCurrentOrganizationRequestHandler : RequestHandler
    {
        internal RetrieveCurrentOrganizationRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata,
            Security security) : base(core, db, metadata, security,
            "RetrieveCurrentOrganization")

        {
        }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var response = new RetrieveCurrentOrganizationResponse();
            response.Results["Detail"] = core.OrganizationDetail;
            return response;
        }
    }
}