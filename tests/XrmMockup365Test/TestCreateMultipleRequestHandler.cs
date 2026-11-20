using System;
using System.Collections.Generic;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestCreateMultipleRequestHandler : UnitTestBase
    {
        public TestCreateMultipleRequestHandler(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestCreateMultipleEntities()
        {
            var contact1 = new Contact { FirstName = "John", LastName = "Doe" };
            var contact2 = new Contact { FirstName = "Jane", LastName = "Doe" };

            var createMultipleRequest = new CreateMultipleRequest
            {
                Targets = new EntityCollection
                {
                    EntityName = Contact.EntityLogicalName,
                    Entities = { contact1, contact2 }
                }
            };

            var response = (CreateMultipleResponse)orgAdminService.Execute(createMultipleRequest);

            Assert.Equal(2, response.Ids.Length);

            var createdContact1 = Contact.Retrieve(orgAdminService, response.Ids[0]);
            var createdContact2 = Contact.Retrieve(orgAdminService, response.Ids[1]);

            Assert.Equal(contact1.FirstName, createdContact1.FirstName);
            Assert.Equal(contact2.FirstName, createdContact2.FirstName);
        }

        [Fact]
        public void TestCreateMultipleThrowsWhenEntityNameMissing()
        {
            var contact = new Contact { FirstName = "John", LastName = "Doe" };

            var createMultipleRequest = new CreateMultipleRequest
            {
                Targets = new EntityCollection
                {
                    Entities = { contact }
                }
            };

            var exception = Assert.Throws<FaultException>(() => orgAdminService.Execute(createMultipleRequest));
            Assert.Equal("The required field 'EntityName' is missing.", exception.Message);
        }

        [Fact]
        public void TestCreateMultipleThrowsWhenEntityLogicalNameMismatch()
        {
            var account = new Account { Name = "Acme" };

            var createMultipleRequest = new CreateMultipleRequest
            {
                Targets = new EntityCollection
                {
                    EntityName = Contact.EntityLogicalName,
                    Entities = { account }
                }
            };

            var exception = Assert.Throws<FaultException>(() => orgAdminService.Execute(createMultipleRequest));
            Assert.Equal($"The entity logical name '{Account.EntityLogicalName}' does not match the expected entity logical name '{Contact.EntityLogicalName}'.", exception.Message);
        }
    }
}
