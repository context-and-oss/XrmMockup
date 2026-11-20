using System;
using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestTime : UnitTestBase
    {
        public TestTime(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestModifiedOn()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var offset = new TimeSpan(1, 0, 0, 0);
                var beforeCreate = DateTime.UtcNow.Add(offset.Subtract(new TimeSpan(1)));
                crm.AddTime(offset);
                var acc = new Account();
                acc.Id = orgAdminUIService.Create(acc);

                var afterCreate = DateTime.UtcNow.Add(offset.Add(new TimeSpan(1)));
                var service = crm.GetAdminService(new MockupServiceSettings(true, true, MockupServiceSettings.Role.SDK));
                var retrieved = service.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.True(DateTime.Compare(beforeCreate, retrieved.CreatedOn.Value) < 0);
                Assert.True(DateTime.Compare(afterCreate, retrieved.CreatedOn.Value) > 0);

                var beforeUpdate = DateTime.UtcNow.Add(offset.Subtract(new TimeSpan(1)));
                crm.AddTime(offset);
                acc.Name = "something";
                orgAdminUIService.Update(acc);
                var afterUpdate = DateTime.UtcNow.Add(offset.Add(offset.Add(new TimeSpan(1))));
                retrieved = service.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.True(DateTime.Compare(beforeUpdate, retrieved.ModifiedOn.Value) < 0);
                Assert.True(DateTime.Compare(afterUpdate, retrieved.ModifiedOn.Value) > 0);
                Assert.True(DateTime.Compare(afterCreate, retrieved.CreatedOn.Value) > 0);
            }
        }
    }
}
