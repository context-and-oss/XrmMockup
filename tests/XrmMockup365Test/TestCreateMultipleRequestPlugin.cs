using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestCreateMultipleRequestPlugin : UnitTestBase
    {
        public TestCreateMultipleRequestPlugin(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestCreateMultipleEntitiesPlugin_CreateMultiple()
        {
            var contact1 = new Contact { Address2_City = "Texas", Address2_Country = "USA" };
            var contact2 = new Contact { Address2_City = "Nuuk", Address2_Country = "Greenland" };

            var createMultipleRequest = new CreateMultipleRequest()
            {
                Targets = new EntityCollection() {
                    EntityName = Contact.EntityLogicalName,
                    Entities = { contact1, contact2 }
                }
            };

            var response = (CreateMultipleResponse)orgAdminService.Execute(createMultipleRequest);

            Assert.Equal(2, response.Ids.Length);

            var createdContact1 = Contact.Retrieve(orgAdminService, response.Ids[0]);
            var createdContact2 = Contact.Retrieve(orgAdminService, response.Ids[1]);

            Assert.Equal("Copenhagen", createdContact1.Address2_City);
            Assert.Equal("Denmark", createdContact1.Address2_Country);

            Assert.Equal("Copenhagen", createdContact2.Address2_City);
            Assert.Equal("Denmark", createdContact2.Address2_Country);
        }

        [Fact]
        public void TestCreateMultipleEntitiesPlugin_Create()
        {
            var contact = new Contact { Address2_City = "Texas", Address2_Country = "USA" };
            contact.Id = orgAdminService.Create(contact);

            var retrievedContact = Contact.Retrieve(orgAdminService, contact.Id);

            Assert.Equal("Copenhagen", retrievedContact.Address2_City);
            Assert.Equal("Denmark", retrievedContact.Address2_Country);
        }
    }
}
