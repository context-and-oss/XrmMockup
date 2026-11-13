using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    internal class AddListMembersListRequestHandler : ListMembershipRequestHandler
    {
        internal AddListMembersListRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "AddListMembersList") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<AddListMembersListRequest>(orgRequest);
            AddListMembers(request.ListId, request.MemberIds, userRef);
            return new AddListMembersListResponse();
        }
    }
}
