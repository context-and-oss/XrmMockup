using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestPluginExecutionContext7 : UnitTestBase
    {
        public TestPluginExecutionContext7(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestIPluginExecutionContext7IsResolvable()
        {
            // Creating an account triggers AccountPluginExecutionContext7PreOp,
            // which resolves IPluginExecutionContext7, validates defaults,
            // and stamps Description = "Context7Resolved" on the target.
            var account = new Account { Name = "Context7Test" };
            account.Id = orgAdminService.Create(account);

            var retrieved = orgAdminService.Retrieve(
                Account.EntityLogicalName, account.Id, new ColumnSet("description"))
                .ToEntity<Account>();

            Assert.Equal("Context7Resolved", retrieved.Description);
        }
    }
}
