using System;
using System.Collections.Generic;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestUpdateMultipleRequestHandler : UnitTestBase
    {
        public TestUpdateMultipleRequestHandler(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestUpdateMultipleEntities()
        {
            var contactId1 = orgGodService.Create(new Contact { FirstName = "John", LastName = "Doe" });
            var contactId2 = orgGodService.Create(new Contact { FirstName = "Jane", LastName = "Doe" });

            var updateContact1 = new Contact(contactId1) { FirstName = "Johny" };
            var updateContact2 = new Contact(contactId2) { FirstName = "Janey" };

            var updateMultipleRequest = new UpdateMultipleRequest
            {
                Targets = new EntityCollection
                {
                    EntityName = Contact.EntityLogicalName,
                    Entities = { updateContact1, updateContact2 }
                }
            };

            var response = (UpdateMultipleResponse)orgAdminService.Execute(updateMultipleRequest);

            //Assert.Equal(2, response.Ids.Length);

            var updatedContact1 = Contact.Retrieve(orgAdminService, contactId1);
            var updatedContact2 = Contact.Retrieve(orgAdminService, contactId2);

            Assert.Equal(updateContact1.FirstName, updatedContact1.FirstName);
            Assert.Equal(updateContact2.FirstName, updatedContact2.FirstName);
        }

        [Fact]
        public void TestUpdateMultipleSameKeyIgnoresSubsequent()
        {
            var contactId = orgGodService.Create(new Contact { FirstName = "John", LastName = "Doe" });

            var updateContact1 = new Contact(contactId) { FirstName = "Johny" };
            var updateContact2 = new Contact(contactId) { FirstName = "Janey" };

            var updateMultipleRequest = new UpdateMultipleRequest
            {
                Targets = new EntityCollection
                {
                    EntityName = Contact.EntityLogicalName,
                    Entities = { updateContact1, updateContact2 }
                }
            };

            var response = (UpdateMultipleResponse)orgAdminService.Execute(updateMultipleRequest);

            var updatedContact = Contact.Retrieve(orgAdminService, contactId);
            Assert.Equal(updateContact1.FirstName, updatedContact.FirstName);
        }

        [Fact]
        public void TestUpdateMultipleThrowsWhenEntityNameMissing()
        {
            var contactId = orgGodService.Create(new Contact { FirstName = "John", LastName = "Doe" });

            var updateMultipleRequest = new UpdateMultipleRequest
            {
                Targets = new EntityCollection
                {
                    Entities = { new Contact(contactId) { FirstName = "Johny" } }
                }
            };

            var exception = Assert.Throws<FaultException>(() => orgAdminService.Execute(updateMultipleRequest));
            Assert.Equal("The required field 'EntityName' is missing.", exception.Message);
        }

        [Fact]
        public void TestUpdateMultipleThrowsWhenEntityLogicalNameMismatch()
        {
            var contactId = orgGodService.Create(new Contact { FirstName = "John", LastName = "Doe" });

            var updateMultipleRequest = new UpdateMultipleRequest
            {
                Targets = new EntityCollection
                {
                    EntityName = Account.EntityLogicalName,
                    Entities = { new Contact(contactId) { FirstName = "Johny" } }
                }
            };

            var exception = Assert.Throws<FaultException>(() => orgAdminService.Execute(updateMultipleRequest));
            Assert.Equal($"The entity logical name '{Contact.EntityLogicalName}' does not match the expected entity logical name '{Account.EntityLogicalName}'.", exception.Message);
        }
    }
}
