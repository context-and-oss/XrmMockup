using System.Linq;
using System.ServiceModel;
using System;
using Microsoft.Crm.Sdk.Messages;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestRemoveFromQueueRequestHandler : QueueRequestHandlerTestBase
    {
        public TestRemoveFromQueueRequestHandler(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void HandlerIsRegisteredExactlyOnce()
        {
            Assert.Single(engine.RequestHandlers.Where(handler => handler.HandlesRequest("RemoveFromQueue")));
        }

        [Fact]
        public void RemoveQueueRequiresExistingNonEmptyId()
        {
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new RemoveFromQueueRequest()));
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new RemoveFromQueueRequest { QueueItemId = Guid.NewGuid() }));
        }

        [Fact]
        public void RemoveDeletesExistingQueueItem()
        {
            var id = AddToQueue(Task(), Queue());
            orgAdminService.Execute(new RemoveFromQueueRequest { QueueItemId = id });
            Assert.Empty(All("queueitem").Entities);
        }
    }
}
