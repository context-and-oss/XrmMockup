using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Microsoft.Xrm.Sdk;

namespace DG.XrmMockupTest
{
    public class TestActivities : UnitTestBase
    {
        public TestActivities(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestCreateEmail()
        {
            var senderList = new List<ActivityParty>() { new ActivityParty() { PartyId = crm.AdminUser } };
            var receivingList = new List<ActivityParty>() { new ActivityParty() { PartyId = crm.AdminUser } };
            var email = new Email()
            {
                Subject = "subject",
                Description = "description",
                from = senderList,
                to = receivingList
            };
            Guid emailId = orgAdminService.Create(email);
            Assert.NotEqual(Guid.Empty, emailId);
        }

        [Fact]
        public void TestActivityPointer()
        {
            var senderList = new List<ActivityParty>() { new ActivityParty() { PartyId = crm.AdminUser } };
            var receivingList = new List<ActivityParty>() { new ActivityParty() { PartyId = crm.AdminUser } };
            var email = new Email()
            {
                Subject = "subject",
                Description = "description",
                from = senderList,
                to = receivingList
            };
            email.Id = orgAdminService.Create(email);

            var retrieved = orgAdminService.Retrieve(email.LogicalName, email.Id, new ColumnSet(true)).ToEntity<Email>();

            using (var context = new Xrm(orgAdminService))
            {
                var ap = context.ActivityPointerSet.FirstOrDefault(a => a.Id == email.Id);
                Assert.NotNull(ap);
                Assert.Equal(retrieved.Subject, ap.Subject);
                Assert.Equal(retrieved.Description, ap.Description);
                Assert.Equal(retrieved.OwnerId, ap.OwnerId);
                Assert.Equal(retrieved.CreatedOn.Value.Date, ap.CreatedOn.Value.Date);
            }

            email.Subject = "updated subject";
            orgAdminService.Update(email);
            retrieved = orgAdminService.Retrieve(email.LogicalName, email.Id, new ColumnSet(true)).ToEntity<Email>();
            using (var context = new Xrm(orgAdminService))
            {
                var ap = context.ActivityPointerSet.FirstOrDefault(a => a.Id == email.Id);
                Assert.NotNull(ap);
                Assert.Equal(retrieved.Subject, ap.Subject);
            }
        }

        [Theory]
        // Trimmed to the activity entities present in the regenerated metadata. The others
        // (appointment, incidentresolution, letter, opportunityclose, phonecall, serviceappointment,
        // campaignresponse, campaignactivity) are not available in the environment.
        [InlineData("email", 4202)]
        [InlineData("fax", 4204)]
        [InlineData("task", 4212)]
        public void TestActivityPointer_Create_SystemActivities(string entityName, int activityTypeCode)
        {
            var entity = new Entity(entityName)
            {
                ["subject"] = "Test Activity"
            };
            entity.Id = orgAdminService.Create(entity);

            // Retrieve the entity to ensure it was created correctly
            var retrieved = orgAdminService.Retrieve(entity.LogicalName, entity.Id, new ColumnSet(true));
            Assert.Equal(activityTypeCode, retrieved.GetAttributeValue<OptionSetValue>("activitytypecode").Value);
            Assert.Equal("Test Activity", retrieved.GetAttributeValue<string>("subject"));

            var ap = orgAdminService.Retrieve("activitypointer", entity.Id, new ColumnSet(true));
            Assert.Equal(activityTypeCode, ap.GetAttributeValue<OptionSetValue>("activitytypecode").Value);
            Assert.Equal("Test Activity", ap.GetAttributeValue<string>("subject"));
        }

        [Theory]
        // Trimmed to the activity entities present in the regenerated metadata. The others
        // (appointment, incidentresolution, letter, opportunityclose, phonecall, serviceappointment,
        // campaignresponse, campaignactivity) are not available in the environment.
        [InlineData("email", 4202)]
        [InlineData("fax", 4204)]
        [InlineData("task", 4212)]
        public void TestActivityPointer_Update_SystemActivities(string entityName, int activityTypeCode)
        {
            var entity = new Entity(entityName)
            {
                ["subject"] = "Test Activity"
            };
            entity.Id = orgAdminService.Create(entity);

            var retrievedActivityPointer = orgAdminService.Retrieve("activitypointer", entity.Id, new ColumnSet(true));
            var apModifiedOn = retrievedActivityPointer.GetAttributeValue<DateTime>("modifiedon");
            var apCreatedOn = retrievedActivityPointer.GetAttributeValue<DateTime>("createdon");

            crm.AddHours(1); // Simulate some time passing

            // Update the entity
            entity = new Entity(entityName, entity.Id)
            {
                ["subject"] = "Updated Activity"
            };
            orgAdminService.Update(entity);

            // Retrieve the entity to ensure it was updated correctly
            var retrievedEntity = orgAdminService.Retrieve(entity.LogicalName, entity.Id, new ColumnSet(true));
            Assert.Equal(activityTypeCode, retrievedEntity.GetAttributeValue<OptionSetValue>("activitytypecode").Value);
            Assert.Equal("Updated Activity", retrievedEntity.GetAttributeValue<string>("subject"));

            retrievedActivityPointer = orgAdminService.Retrieve("activitypointer", entity.Id, new ColumnSet(true));
            Assert.Equal(activityTypeCode, retrievedActivityPointer.GetAttributeValue<OptionSetValue>("activitytypecode").Value);
            Assert.Equal("Updated Activity", retrievedActivityPointer.GetAttributeValue<string>("subject"));
            var apModifiedOnNew = retrievedActivityPointer.GetAttributeValue<DateTime>("modifiedon");
            var apCreatedOnNew = retrievedActivityPointer.GetAttributeValue<DateTime>("createdon");
            Assert.Equal(apCreatedOn, apCreatedOnNew);
            Assert.True(apModifiedOnNew > apModifiedOn, "ModifiedOn should be updated after the entity update");
        }
    }
}