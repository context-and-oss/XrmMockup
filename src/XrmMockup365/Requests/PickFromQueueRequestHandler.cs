using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup
{
    internal class PickFromQueueRequestHandler : ServiceRequestHandler
    {
        internal PickFromQueueRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "PickFromQueue") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<PickFromQueueRequest>(orgRequest);
            RequireId(request.QueueItemId, "QueueItemId");
            RequireId(request.WorkerId, "WorkerId");
            var worker = Retrieve(new EntityReference("systemuser", request.WorkerId), userRef);
            Retrieve(new EntityReference("queueitem", request.QueueItemId), userRef);
            if (request.RemoveQueueItem)
                core.Execute(new DeleteRequest { Target = new EntityReference("queueitem", request.QueueItemId) }, userRef);
            else
                core.Execute(new UpdateRequest { Target = new Entity("queueitem", request.QueueItemId)
                {
                    ["workerid"] = worker.ToEntityReference(),
                    ["workeridmodifiedon"] = DateTime.UtcNow.Add(core.TimeOffset)
                } }, userRef);
            return new PickFromQueueResponse();
        }
    }
}
