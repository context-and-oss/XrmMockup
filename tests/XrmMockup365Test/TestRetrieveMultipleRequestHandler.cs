using System.Linq;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestRetrieveMultipleRequestHandler : RequestHandlerTestBase
    {
        public TestRetrieveMultipleRequestHandler(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void RetrieveMultiplePagesAdvanceAndCountAllMatches()
        {
            for (var i = 0; i < 5; i++) orgAdminService.Create(new Account { Name = "Page " + i });
            var query = new QueryExpression("account") { ColumnSet = new ColumnSet("name"), PageInfo = new PagingInfo { Count = 2, PageNumber = 1, ReturnTotalRecordCount = true } };
            var first = orgAdminService.RetrieveMultiple(query);
            query.PageInfo.PageNumber = 2;
            var second = orgAdminService.RetrieveMultiple(query);
            query.PageInfo.PageNumber = 3;
            var third = orgAdminService.RetrieveMultiple(query);
            Assert.True(first.MoreRecords);
            Assert.True(second.MoreRecords);
            Assert.False(third.MoreRecords);
            Assert.Equal(5, second.TotalRecordCount);
            Assert.Equal(5, first.Entities.Concat(second.Entities).Concat(third.Entities).Select(e => e.Id).Distinct().Count());
        }
    }
}
