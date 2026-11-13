using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace DG.XrmMockupTest
{
    public abstract class QueueRequestHandlerTestBase : RequestHandlerTestBase
    {
        protected QueueRequestHandlerTestBase(XrmMockupFixture fixture) : base(fixture)
        {
            // Queue metadata is not included in the checked-in organization's extraction.
            AddTable("queue", Text("name"));
            AddTable("queueitem", Lookup("queueid", "queue"), Lookup("objectid", "task", "email", "incident"),
                Lookup("workerid", "systemuser"), Date("workeridmodifiedon"), Text("title"));
        }

        protected Guid Queue() => Create("queue", ("name", "Test queue"));

        protected EntityReference Task() => new EntityReference("task", orgAdminService.Create(new DG.XrmFramework.BusinessDomain.ServiceContext.Task { Subject = "Queue task" }));

        protected Guid AddToQueue(EntityReference target, Guid destination) => ((AddToQueueResponse)orgAdminService.Execute(new AddToQueueRequest { Target = target, DestinationQueueId = destination })).QueueItemId;
    }
}
