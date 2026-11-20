using System;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestDeleteMultipleRequestHandler : UnitTestBase
    {
        public TestDeleteMultipleRequestHandler(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestDeleteMultipleEntities()
        {
            var contactId1 = orgGodService.Create(new Contact { FirstName = "John", LastName = "Doe" });
            var contactId2 = orgGodService.Create(new Contact { FirstName = "Jane", LastName = "Doe" });

            var deleteMultipleRequest = new DeleteMultipleRequest
            {
                Targets = new EntityReferenceCollection
                {
                    new EntityReference(Contact.EntityLogicalName, contactId1),
                    new EntityReference(Contact.EntityLogicalName, contactId2)
                }
            };

            orgAdminService.Execute(deleteMultipleRequest);

            Assert.Throws<FaultException>(() => orgAdminService.Retrieve(Contact.EntityLogicalName, contactId1, new Microsoft.Xrm.Sdk.Query.ColumnSet(true)));
            Assert.Throws<FaultException>(() => orgAdminService.Retrieve(Contact.EntityLogicalName, contactId2, new Microsoft.Xrm.Sdk.Query.ColumnSet(true)));
        }

        [Fact]
        public void TestDeleteMultipleThrowsWhenEntityNameMissing()
        {
            var contactId = orgGodService.Create(new Contact { FirstName = "John", LastName = "Doe" });

            var deleteMultipleRequest = new DeleteMultipleRequest
            {
                Targets = new EntityReferenceCollection
                {
                    new EntityReference(string.Empty, contactId)
                }
            };

            var exception = Assert.Throws<FaultException>(() => orgAdminService.Execute(deleteMultipleRequest));
            Assert.Equal("The required field 'EntityName' is missing.", exception.Message);
        }

        [Fact]
        public void TestDeleteMultipleThrowsWhenEntityLogicalNamesMismatch()
        {
            var contactId = orgGodService.Create(new Contact { FirstName = "John", LastName = "Doe" });
            var accountId = orgGodService.Create(new Account { Name = "Acme" });

            var deleteMultipleRequest = new DeleteMultipleRequest
            {
                Targets = new EntityReferenceCollection
                {
                    new EntityReference(Contact.EntityLogicalName, contactId),
                    new EntityReference(Account.EntityLogicalName, accountId)
                }
            };

            var exception = Assert.Throws<FaultException>(() => orgAdminService.Execute(deleteMultipleRequest));
            Assert.Equal("All entity references in a DeleteMultipleRequest must have the same entity logical name.", exception.Message);
        }
    }
}
