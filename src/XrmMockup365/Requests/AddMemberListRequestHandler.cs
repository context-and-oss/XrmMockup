using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    internal class AddMemberListRequestHandler : ListMembershipRequestHandler
    {
        internal AddMemberListRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "AddMemberList") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<AddMemberListRequest>(orgRequest);
            var ids = AddListMembers(request.ListId, new[] { request.EntityId }, userRef);
            var response = new AddMemberListResponse();
            response.Results["Id"] = ids[0];
            return response;
        }
    }
}
