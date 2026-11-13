using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System;
using DG.Tools.XrmMockup.Database;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestBulkDeleteRequestHandler : RequestHandlerTestBase
    {
        public TestBulkDeleteRequestHandler(XrmMockupFixture fixture) : base(fixture)
        {
            AddTable("asyncoperation", Text("name"), Choice("operationtype", 13), Choice("statecode", 0, 3), Choice("statuscode", 0, 30), Date("startedon"), Date("completedon"));
            metadata.DefaultStateStatus["asyncoperation"] = new Dictionary<int, int> { [0] = 0, [3] = 30 };
        }

        [Fact]
        public void HandlerIsRegisteredExactlyOnce()
        {
            Assert.Single(engine.RequestHandlers.Where(handler => handler.HandlesRequest("BulkDelete")));
        }

        private BulkDeleteRequest Bulk(params QueryExpression[] queries) => new BulkDeleteRequest { JobName = "Delete matching", QuerySet = queries, ToRecipients = Array.Empty<Guid>(), CCRecipients = Array.Empty<Guid>(), StartDateTime = DateTime.UtcNow.AddMinutes(-1), RecurrencePattern = "" };

        [Fact]
        public void BulkDeleteDeduplicatesOverlappingQueriesAndCompletesJob()
        {
            var first = orgAdminService.Create(new Account { Name = "Delete" });
            var second = orgAdminService.Create(new Account { Name = "Keep" });
            var query = new QueryExpression("account"); query.Criteria.AddCondition("name", ConditionOperator.Equal, "Delete");
            var response = (BulkDeleteResponse)orgAdminService.Execute(Bulk(query, query));
            Assert.Equal(second, Assert.Single(All("account").Entities).Id);
            var job = Read("asyncoperation", response.JobId);
            Assert.Equal(3, job.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(30, job.GetAttributeValue<OptionSetValue>("statuscode").Value);
            Assert.Equal(13, job.GetAttributeValue<OptionSetValue>("operationtype").Value);
            Assert.Equal("Delete matching", job["name"]);
            Assert.True(job.Contains("completedon"));
            Assert.Null(query.PageInfo.PagingCookie);
        }

        [Fact]
        public void BulkDeleteRejectsScheduledJobsInsteadOfRunningEarly()
        {
            var id = orgAdminService.Create(new Account { Name = "Keep" });
            var request = Bulk(new QueryExpression("account")); request.StartDateTime = DateTime.UtcNow.AddDays(1);
            Assert.Throws<NotSupportedException>(() => orgAdminService.Execute(request));
            Assert.Equal(id, Assert.Single(All("account").Entities).Id);
            Assert.Empty(All("asyncoperation").Entities);
        }

        [Fact]
        public void BulkDeleteRejectsMissingParametersAndHonorsDeleteAccess()
        {
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new BulkDeleteRequest()));
            orgAdminService.Create(new Account { Name = "Protected" });
            Assert.Throws<FaultException>(() => testUser1Service.Execute(Bulk(new QueryExpression("account"))));
            Assert.Single(All("account").Entities);
        }

        [Fact]
        public void BulkDeleteRetrievesEveryPageBeforeDeleting()
        {
            AddTable("parity_record", Text("name"));
            var database = (XrmDb)typeof(Core).GetField("db", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(engine);
            for (var i = 0; i < 5001; i++) database.Add(new Entity("parity_record", Guid.NewGuid()) { ["name"] = "Record " + i });
            orgAdminService.Execute(Bulk(new QueryExpression("parity_record")));
            Assert.Empty(All("parity_record").Entities);
        }

        [Fact]
        public void BulkDeleteRespectsTopCountWithoutMutatingQuery()
        {
            orgAdminService.Create(new Account { Name = "First" });
            orgAdminService.Create(new Account { Name = "Second" });
            var query = new QueryExpression("account") { TopCount = 1 };
            orgAdminService.Execute(Bulk(query));
            Assert.Single(All("account").Entities);
            Assert.Equal(1, query.TopCount);
            Assert.Equal(0, query.PageInfo.Count);
        }
    }
}
