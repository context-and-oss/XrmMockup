using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    internal class RemoveFromQueueRequestHandler : ServiceRequestHandler
    {
        internal RemoveFromQueueRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "RemoveFromQueue") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<RemoveFromQueueRequest>(orgRequest);
            RequireId(request.QueueItemId, "QueueItemId");
            core.Execute(new DeleteRequest { Target = new EntityReference("queueitem", request.QueueItemId) }, userRef);
            return new RemoveFromQueueResponse();
        }
    }
}
