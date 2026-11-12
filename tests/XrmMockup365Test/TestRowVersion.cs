using System;
using System.Globalization;
using System.IO;
using DG.Tools.XrmMockup.Internal;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestRowVersion : UnitTestBase
    {
        public TestRowVersion(XrmMockupFixture fixture) : base(fixture) { }

        private Entity Retrieve(Guid id, ColumnSet columns = null)
        {
            return orgAdminService.Retrieve(Account.EntityLogicalName, id, columns ?? new ColumnSet(true));
        }

        private static long AssertVersion(Entity entity)
        {
            Assert.False(string.IsNullOrEmpty(entity.RowVersion));
            var version = long.Parse(entity.RowVersion, CultureInfo.InvariantCulture);
            Assert.True(version > 0);
            Assert.Equal(version, entity.GetAttributeValue<long>("versionnumber"));
            return version;
        }

        [Fact]
        public void CreateAndUpdateGenerateVersionsAndReadsDoNotChangeThem()
        {
            var id = orgAdminService.Create(new Account { Name = "Versioned account" });
            var created = Retrieve(id);
            var initialVersion = AssertVersion(created);
            Assert.Equal(created.RowVersion, Retrieve(id).RowVersion);

            orgAdminService.Update(new Account(id) { Name = "Updated account" });
            var updated = Retrieve(id);
            Assert.True(AssertVersion(updated) > initialVersion);
            Assert.Equal(updated.RowVersion, Retrieve(id).RowVersion);
        }

        [Fact]
        public void VersionsAreGeneratedAcrossTables()
        {
            var accountId = orgAdminService.Create(new Account { Name = "Account" });
            var accountVersion = AssertVersion(Retrieve(accountId));
            var contactId = orgAdminService.Create(new Contact { FirstName = "Contact" });
            var contact = orgAdminService.Retrieve(Contact.EntityLogicalName, contactId, new ColumnSet("versionnumber"));
            Assert.True(AssertVersion(contact) > accountVersion);
        }

        [Fact]
        public void SuppliedVersionsCannotOverrideGeneratedVersions()
        {
            var account = new Account { Name = "Account", RowVersion = "9223372036854775807" };
            account["versionnumber"] = long.MaxValue;
            var id = orgAdminService.Create(account);
            var initialVersion = AssertVersion(Retrieve(id));
            Assert.NotEqual(long.MaxValue, initialVersion);

            var update = new Account(id) { Name = "Updated", RowVersion = "1" };
            update["versionnumber"] = 1L;
            orgAdminService.Update(update);
            Assert.True(AssertVersion(Retrieve(id)) > initialVersion);
        }

        [Fact]
        public void RowVersionIsReturnedRegardlessOfSelectedColumns()
        {
            var id = orgAdminService.Create(new Account { Name = "Account" });
            var expected = Retrieve(id).RowVersion;
            foreach (var columns in new[] { new ColumnSet(), new ColumnSet("name"), new ColumnSet("versionnumber"), new ColumnSet(true) })
            {
                var entity = Retrieve(id, columns);
                Assert.Equal(expected, entity.RowVersion);
                Assert.Equal(columns.AllColumns || columns.Columns.Contains("versionnumber"), entity.Contains("versionnumber"));
            }
        }

        [Fact]
        public void RetrieveMultiplePreservesVersionsAndHonorsColumnSelection()
        {
            var id = orgAdminService.Create(new Account { Name = "Account" });
            var expected = Retrieve(id).RowVersion;
            foreach (var columns in new[] { new ColumnSet("name"), new ColumnSet("versionnumber"), new ColumnSet(true) })
            {
                var query = new QueryExpression(Account.EntityLogicalName) { ColumnSet = columns };
                query.Criteria.AddCondition("accountid", ConditionOperator.Equal, id);
                var entity = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);
                Assert.Equal(expected, entity.RowVersion);
                Assert.Equal(columns.AllColumns || columns.Columns.Contains("versionnumber"), entity.Contains("versionnumber"));
                if (entity.Contains("versionnumber")) AssertVersion(entity);
            }
        }

        [Fact]
        public void FetchXmlCanFilterOnVersionNumber()
        {
            var id = orgAdminService.Create(new Account { Name = "Account" });
            var expected = Retrieve(id);
            var version = AssertVersion(expected);
            var fetch = new FetchExpression($"<fetch><entity name='account'><attribute name='versionnumber'/>" +
                $"<filter><condition attribute='versionnumber' operator='eq' value='{version}'/></filter></entity></fetch>");
            var entity = Assert.Single(orgAdminService.RetrieveMultiple(fetch).Entities);
            Assert.Equal(id, entity.Id);
            Assert.Equal(expected.RowVersion, entity.RowVersion);
            AssertVersion(entity);
        }

        [Fact]
        public void CloningPreservesRowVersionWithoutSelectingVersionNumber()
        {
            var id = orgAdminService.Create(new Account { Name = "Account" });
            var original = Retrieve(id);
            var clone = original.CloneEntity(null, new ColumnSet("name"));
            Assert.Equal(original.RowVersion, clone.RowVersion);
            Assert.False(clone.Contains("versionnumber"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SnapshotsPreserveVersionsAndVersionGeneration(bool zip)
        {
            var id = orgAdminService.Create(new Account { Name = "Account" });
            var original = Retrieve(id);
            var originalVersion = AssertVersion(original);
            var snapshot = Path.Combine(Path.GetTempPath(), "row-version-" + Guid.NewGuid().ToString("N"));
            try
            {
                if (zip) crm.TakeZipSnapshot(snapshot);
                else crm.TakeSnapshot(snapshot);

                orgAdminService.Update(new Account(id) { Name = "Updated" });
                Assert.True(AssertVersion(Retrieve(id)) > originalVersion);
                crm.ResetEnvironment();
                if (zip) crm.RestoreZipSnapshot(snapshot);
                else crm.RestoreToSnapshot(snapshot);

                Assert.Equal(original.RowVersion, Retrieve(id).RowVersion);
                orgAdminService.Update(new Account(id) { Name = "Updated after restore" });
                Assert.True(AssertVersion(Retrieve(id)) > originalVersion);
                var contactId = orgAdminService.Create(new Contact { FirstName = "New record" });
                Assert.True(AssertVersion(orgAdminService.Retrieve(Contact.EntityLogicalName, contactId,
                    new ColumnSet("versionnumber"))) > originalVersion);
            }
            finally
            {
                if (zip) File.Delete(Path.ChangeExtension(snapshot, ".zip"));
                else crm.DeleteSnapshot(snapshot);
            }
        }
    }
}
