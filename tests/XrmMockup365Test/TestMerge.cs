using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestMerge : UnitTestBase
    {
        public TestMerge(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestMergeAll()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var _account1Id = orgAdminUIService.Create(
                    new Account
                    {
                        Name = "Fourth Coffee",
                        Description = "Coffee House"
                    });
                var _account2Id = orgAdminUIService.Create(
                    new Account
                    {
                        Name = "Fourth Coffee",
                        NumberOfEmployees = 55,
                    });

                var target = new EntityReference
                {
                    Id = _account1Id,
                    LogicalName = Account.EntityLogicalName
                };

                var updateContent = new Account
                {
                    Address1_Line1 = "Test",
                    NumberOfEmployees = 45
                };

                var req = new MergeRequest
                {
                    Target = target,
                    SubordinateId = _account2Id,
                    UpdateContent = updateContent,
                    PerformParentingChecks = false
                };

                // Execute the request.
                MergeResponse merged = (MergeResponse)orgAdminUIService.Execute(req);


                Account mergeeAccount =
                    (Account)orgAdminUIService.Retrieve(Account.EntityLogicalName,
                    _account2Id, new ColumnSet(true));

                Assert.True(mergeeAccount.StateCode.Equals(account_statecode.Inactive));
                Assert.True(mergeeAccount.StatusCode.Equals(account_statuscode.Inactive));
                Assert.True(mergeeAccount.Merged.Value);

                Account mergedAccount =
                    (Account)orgAdminUIService.Retrieve(Account.EntityLogicalName,
                    _account1Id, new ColumnSet(true));

                Assert.Equal("Fourth Coffee", mergedAccount.Name);
                Assert.Equal("Coffee House", mergedAccount.Description);
                Assert.Equal(updateContent.NumberOfEmployees, mergedAccount.NumberOfEmployees);
                Assert.Equal(updateContent.Address1_Line1, mergedAccount.Address1_Line1);

            }
        }
    }

}
