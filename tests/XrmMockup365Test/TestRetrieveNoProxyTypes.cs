using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using System.ServiceModel;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.Tools.XrmMockup;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestRetrieveNoProxyTypes : UnitTestBaseNoProxyTypes
    {
        public TestRetrieveNoProxyTypes(XrmMockupFixtureNoProxyTypes fixture) : base(fixture) {  }
        
        [Fact]
        public void TestLookupFormattedValues()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var id1 = this.orgAdminUIService.Create(new Account() { Name = "MLJ UnitTest" });
                var id2 = this.orgAdminUIService.Create(new Account() { Name = "MLJ UnitTest2" });

                var acc1a = new Account(id1)
                {
                    ParentAccountId = new EntityReference(Account.EntityLogicalName, id2)
                };
                this.orgAdminUIService.Update(acc1a);

                var retrieved = this.orgAdminUIService.Retrieve(Account.EntityLogicalName, id1,
                    new ColumnSet("accountid", "parentaccountid"));
              //  Assert.NotNull(retrieved.ParentAccountId);
                Assert.Equal("MLJ UnitTest2", retrieved.FormattedValues["parentaccountid"]);
            }
        }
    }
}
