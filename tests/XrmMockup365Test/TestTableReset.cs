using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using System.Linq;

namespace DG.XrmMockupTest
{
    [TestCaseOrderer("DG.XrmMockupTest.PriorityOrderer", "XrmMockup365Test")]

    public class TestResetTables : UnitTestBaseNoReset
    {
        public TestResetTables(XrmMockupFixture fixture) : base(fixture) 
        {
            //create some contacts
            var contact1 = new Contact() { FirstName = "ResetTest1" };
            orgAdminService.Create(contact1);
            var contact2 = new Contact() { FirstName = "ResetTest2" };
            orgAdminService.Create(contact2);
        }

        [Fact]
        [TestPriority(1)]
        public void TestResetTable()
        {
            var account1 = new Account() { Name = "ResetTest1" };
            orgAdminService.Create(account1);
            var account2 = new Account() { Name = "ResetTest2" };
            orgAdminService.Create(account2);
            var account3 = new Account() { Name = "ResetTest3" };
            orgAdminService.Create(account3);

            var accountQuery = new QueryExpression("account");
            accountQuery.ColumnSet = new ColumnSet(true);
            var accounts = orgAdminService.RetrieveMultiple(accountQuery);
            Assert.Equal(3, accounts.Entities.Where(x => x.Contains("name") && x.GetAttributeValue<string>("name").StartsWith("ResetTest")).Count());

            crm.ResetTable("account");

            accounts = orgAdminService.RetrieveMultiple(accountQuery);
            Assert.Empty(accounts.Entities);

            account1 = new Account() { Name = "ResetTest1" };
            orgAdminService.Create(account1);
        }
    }
}
