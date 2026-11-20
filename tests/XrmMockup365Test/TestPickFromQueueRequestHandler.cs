using System.Linq;
using System.ServiceModel;
using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestPickFromQueueRequestHandler : QueueRequestHandlerTestBase
    {
        public TestPickFromQueueRequestHandler(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void HandlerIsRegisteredExactlyOnce()
        {
            Assert.Single(engine.RequestHandlers.Where(handler => handler.HandlesRequest("PickFromQueue")));
        }

        [Fact]
        public void PickAssignsWorkerAndUtcModifiedTime()
        {
            var id = AddToQueue(Task(), Queue());
            orgAdminService.Execute(new PickFromQueueRequest { QueueItemId = id, WorkerId = crm.AdminUser.Id });
            var item = Read("queueitem", id);
            Assert.Equal(crm.AdminUser.Id, item.GetAttributeValue<EntityReference>("workerid").Id);
            Assert.Equal(DateTimeKind.Utc, item.GetAttributeValue<DateTime>("workeridmodifiedon").Kind);
        }

        [Fact]
        public void PickCanRemoveAndRejectsUnknownWorker()
        {
            var id = AddToQueue(Task(), Queue());
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new PickFromQueueRequest { QueueItemId = id, WorkerId = Guid.NewGuid() }));
            orgAdminService.Execute(new PickFromQueueRequest { QueueItemId = id, WorkerId = crm.AdminUser.Id, RemoveQueueItem = true });
            Assert.Empty(All("queueitem").Entities);
        }
    }
}
