using System.Linq;
using System.ServiceModel;
using System;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestAddToQueueRequestHandler : QueueRequestHandlerTestBase
    {
        public TestAddToQueueRequestHandler(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void HandlerIsRegisteredExactlyOnce()
        {
            Assert.Single(engine.RequestHandlers.Where(handler => handler.HandlesRequest("AddToQueue")));
        }

        [Fact]
        public void QueueMoveUpdatesExistingItemInsteadOfCreatingDuplicate()
        {
            var first = Queue(); var second = Queue(); var target = Task();
            var id = AddToQueue(target, first);
            var response = (AddToQueueResponse)orgAdminService.Execute(new AddToQueueRequest { Target = target, DestinationQueueId = second, SourceQueueId = first });
            Assert.Equal(id, response.QueueItemId);
            Assert.Equal(second, Read("queueitem", id).GetAttributeValue<EntityReference>("queueid").Id);
            Assert.Single(All("queueitem").Entities);
            Assert.Equal(id, AddToQueue(target, second));
        }

        [Fact]
        public void QueuePropertiesInitializeWithoutMutatingCaller()
        {
            var properties = new Entity("queueitem") { ["title"] = "Custom title", ["workerid"] = crm.AdminUser };
            var id = ((AddToQueueResponse)orgAdminService.Execute(new AddToQueueRequest { DestinationQueueId = Queue(), Target = Task(), QueueItemProperties = properties })).QueueItemId;
            Assert.Equal("Custom title", Read("queueitem", id)["title"]);
            Assert.False(properties.Contains("queueid"));
            Assert.Equal(Guid.Empty, properties.Id);
        }

        [Fact]
        public void ReceivedEmailCanBelongToTwoQueuesButNotTwiceToSameQueue()
        {
            var email = new Email { Subject = "Received", StatusCode = email_statuscode.Received };
            email.Id = orgAdminService.Create(email);
            orgAdminService.Update(new Entity("email", email.Id) { ["statecode"] = new OptionSetValue(1), ["statuscode"] = new OptionSetValue(4) });
            var first = Queue(); var second = Queue();
            var id = AddToQueue(email.ToEntityReference(), first);
            var other = AddToQueue(email.ToEntityReference(), second);
            Assert.NotEqual(id, other);
            Assert.Equal(id, AddToQueue(email.ToEntityReference(), first));
            Assert.Equal(2, All("queueitem").Entities.Count);
        }

        [Fact]
        public void InvalidSourceQueueDoesNotMoveItem()
        {
            var first = Queue(); var target = Task(); var id = AddToQueue(target, first);
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new AddToQueueRequest { Target = target, DestinationQueueId = Queue(), SourceQueueId = Queue() }));
            Assert.Equal(first, Read("queueitem", id).GetAttributeValue<EntityReference>("queueid").Id);
        }

        [Fact]
        public void QueueRequiresTargetAndNonEmptyDestination()
        {
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new AddToQueueRequest { Target = Task() }));
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new AddToQueueRequest { DestinationQueueId = Queue() }));
        }

        [Fact]
        public void GenericOrganizationRequestsUseTheSameHandlers()
        {
            var target = Task(); var queue = Queue();
            var response = (AddToQueueResponse)orgAdminService.Execute(new OrganizationRequest("AddToQueue") { ["Target"] = target, ["DestinationQueueId"] = queue });
            Assert.Equal(target.Id, Read("queueitem", response.QueueItemId).GetAttributeValue<EntityReference>("objectid").Id);
        }
    }
}
