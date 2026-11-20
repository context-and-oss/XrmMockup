using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace DG.Tools.XrmMockup
{
    internal class RetrievePrincipalAccessRequestHandler : RequestHandler
    {
        internal RetrievePrincipalAccessRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RetrievePrincipalAccess") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<RetrievePrincipalAccessRequest>(orgRequest);
            var resp = new RetrievePrincipalAccessResponse();
            resp.Results["AccessRights"] = security.GetAccessRights(request.Target, request.Principal);
            return resp;
        }
    }
}
