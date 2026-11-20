using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    internal class RetrieveSharedPrincipalsAndAccessRequestHandler : ServiceRequestHandler
    {
        internal RetrieveSharedPrincipalsAndAccessRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "RetrieveSharedPrincipalsAndAccess") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<RetrieveSharedPrincipalsAndAccessRequest>(orgRequest);
            Retrieve(request.Target, userRef);
            var response = new RetrieveSharedPrincipalsAndAccessResponse();
            response.Results["PrincipalAccesses"] = security.GetSharedPrincipalsAndAccess(request.Target);
            return response;
        }
    }
}
