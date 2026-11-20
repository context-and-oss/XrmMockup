using System;
using DG.Some.Namespace;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    /// <summary>
    /// Covers Update filtering attributes against the system-managed attributes XrmMockup copies onto
    /// the Target at the start of the post-operation stage (statecode, statuscode, ownerid, ...).
    /// Those are visible to post-operation plugins, as in Dataverse, but must not satisfy the filter.
    /// </summary>
    public class TestFilteredAttributes : UnitTestBase
    {

        private Guid taskId;

        public TestFilteredAttributes(XrmMockupFixture fixture) : base(fixture)
        {
            taskId = orgAdminService.Create(new Task { Subject = "Created" });
        }

        private string GetMarker(Guid taskId) =>
            Task.Retrieve(orgAdminService, taskId, x => x.Category).Category;

        [Fact]
        public void TestFilteredStepNotTriggeredByUnfilteredAttribute()
        {
            orgAdminService.Update(new Task
            {
                Id = taskId,
                Subject = "Updated",
            });

            Assert.Null(GetMarker(taskId));
        }

        [Fact]
        public void TestFilteredStepTriggeredByFilteredAttribute()
        {
            orgAdminService.Update(new Task
            {
                Id = taskId,
                Description = "Updated",
            });

            Assert.Equal("FilterMatched", GetMarker(taskId));
        }

        [Fact]
        public void TestFilteredStepTriggeredByStatusCodeSentByCaller()
        {
            orgAdminService.Update(new Task
            {
                Id = taskId,
                StatusCode = task_statuscode.InProgress,
            });

            Assert.Equal("FilterMatched", GetMarker(taskId));
        }
    }
}
