using System;
using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestServiceRoles : UnitTestBase
    {
        public TestServiceRoles(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestUpdateInactiveRecords()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var account = new Account();
                account.Id = orgAdminService.Create(account);

                account.StateCode = account_statecode.Inactive;
                account.StatusCode = account_statuscode.Inactive;
                orgAdminService.Update(account);

                account.Name = "SDK can do";
                orgAdminService.Update(account);
                var retrieved = orgAdminService.Retrieve(Account.EntityLogicalName, account.Id, new ColumnSet(true)) as Account;
                Assert.Equal(account.Name, retrieved.Name);

                try
                {
                    orgAdminUIService.Update(account);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<MockupException>(e);
                }
            }
        }


        [Fact(Skip = "Disagreement over test")]
        // majakubowski: 
        // I don't agree with this test - I don't have experience with earlier version than 2015, 
        // but from this version all boolean attributes are set to default values also when created via SDK 
        public void TestCreateDefaultValues()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var account = new Account();
                account.Id = orgAdminService.Create(account);

                var retrieved = orgAdminService.Retrieve(Account.EntityLogicalName, account.Id, new ColumnSet(true)) as Account;
                Assert.Null(retrieved.DoNotPhone);

                var anotherAccount = new Account();
                anotherAccount.Id = orgAdminUIService.Create(anotherAccount);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, anotherAccount.Id, new ColumnSet(true)) as Account;
                Assert.True(retrieved.DoNotPhone.HasValue);
                Assert.False(retrieved.DoNotPhone.Value);
            }
        }

        [Fact]
        public void TestCreateCurrency()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var account = new Account();
                account.Id = orgAdminService.Create(account);
                var retrieved = orgAdminService.Retrieve(Account.EntityLogicalName, account.Id, new ColumnSet(true)) as Account;
                Assert.Null(retrieved.TransactionCurrencyId);

                account = new Account
                {
                    Revenue = 20m
                };
                account.Id = orgAdminService.Create(account);
                retrieved = orgAdminService.Retrieve(Account.EntityLogicalName, account.Id, new ColumnSet(true)) as Account;
                Assert.NotNull(retrieved.TransactionCurrencyId);

                account = new Account();
                account.Id = orgAdminUIService.Create(account);
                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, account.Id, new ColumnSet(true)) as Account;
                Assert.NotNull(retrieved.TransactionCurrencyId);
            }
        }
    }

}