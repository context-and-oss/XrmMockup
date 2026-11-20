using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestUpdateMultipleRequestPlugin : UnitTestBase
    {
        public TestUpdateMultipleRequestPlugin(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestUpdateMultipleEntitiesPlugin_UpdateMultiple()
        {
            var contact1 = orgGodService.Create(new Contact { Address2_City = "Texas", Address2_Country = "USA" });
            var contact2 = orgGodService.Create(new Contact { Address2_City = "Nuuk", Address2_Country = "Greenland" });

            var updateContact1 = new Contact(contact1) { Address2_City = "Houston" };
            var updateContact2 = new Contact(contact2) { Address2_City = "Sisimiut" };

            var updateMultipleRequest = new UpdateMultipleRequest()
            {
                Targets = new EntityCollection() {
                    EntityName = Contact.EntityLogicalName,
                    Entities = { updateContact1, updateContact2 }
                }
            };

            var response = (UpdateMultipleResponse)orgAdminService.Execute(updateMultipleRequest);

            var createdContact1 = Contact.Retrieve(orgAdminService, contact1);
            var createdContact2 = Contact.Retrieve(orgAdminService, contact2);

            Assert.Equal("Copenhagen", createdContact1.Address2_City);
            Assert.Equal("Denmark", createdContact1.Address2_Country);

            Assert.Equal("Copenhagen", createdContact2.Address2_City);
            Assert.Equal("Denmark", createdContact2.Address2_Country);
        }

        [Fact]
        public void TestUpdateMultipleEntitiesPlugin_Update()
        {
            var contact = new Contact { Address2_City = "Texas", Address2_Country = "USA" };
            contact.Id = orgGodService.Create(contact);

            orgAdminService.Update(new Contact(contact.Id)
            {
                FirstName = "John",
                LastName = "Lennon"
            });
            var retrievedContact = Contact.Retrieve(orgAdminService, contact.Id);

            Assert.Equal("Copenhagen", retrievedContact.Address2_City);
            Assert.Equal("Denmark", retrievedContact.Address2_Country);
        }
    }
}
