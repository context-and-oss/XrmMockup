using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestMultipleRequestPluginImages : UnitTestBase
    {
        public TestMultipleRequestPluginImages(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void UpdateMultiple_TriggersUpdatePostOpPlugin_WithPostImages()
        {
            // Arrange: create contacts
            var contact1Id = orgGodService.Create(new Contact { FirstName = "Alice", LastName = "One" });
            var contact2Id = orgGodService.Create(new Contact { FirstName = "Bob", LastName = "Two" });

            // Act: UpdateMultiple - this previously threw NullReferenceException
            // because plugins with post-images received null images from the Multiple->Single cross-trigger
            var updateMultiple = new UpdateMultipleRequest
            {
                Targets = new EntityCollection
                {
                    EntityName = Contact.EntityLogicalName,
                    Entities =
                    {
                        new Contact(contact1Id) { Description = "updated-1" },
                        new Contact(contact2Id) { Description = "updated-2" },
                    }
                }
            };

            // Should not throw NullReferenceException
            orgAdminService.Execute(updateMultiple);

            // Assert: entities were updated
            var retrieved1 = Contact.Retrieve(orgAdminService, contact1Id);
            var retrieved2 = Contact.Retrieve(orgAdminService, contact2Id);
            Assert.Equal("updated-1", retrieved1.Description);
            Assert.Equal("updated-2", retrieved2.Description);
        }

        [Fact]
        public void CreateMultiple_DoesNotCrash()
        {
            var createMultiple = new CreateMultipleRequest
            {
                Targets = new EntityCollection
                {
                    EntityName = Contact.EntityLogicalName,
                    Entities =
                    {
                        new Contact { FirstName = "Charlie", LastName = "Three" },
                        new Contact { FirstName = "Diana", LastName = "Four" },
                    }
                }
            };

            // Should not throw
            var response = (CreateMultipleResponse)orgAdminService.Execute(createMultiple);
            Assert.NotNull(response);
        }

        [Fact]
        public void SingleUpdate_TriggersUpdateMultiplePlugin()
        {
            // The SetCityOnCreateUpdateMultiple plugin is registered on UpdateMultiple/PreOperation
            // and should fire via the Single->Multiple cross-trigger
            var contactId = orgGodService.Create(new Contact { FirstName = "Eve", Address2_City = "Berlin" });

            orgAdminService.Update(new Contact(contactId) { FirstName = "Eve-Updated" });

            var retrieved = Contact.Retrieve(orgAdminService, contactId);
            Assert.Equal("Copenhagen", retrieved.Address2_City);
        }

        [Fact]
        public void SingleCreate_TriggersCreateMultiplePlugin()
        {
            // The SetCityOnCreateUpdateMultiple plugin is registered on CreateMultiple/PreOperation
            var contactId = orgAdminService.Create(new Contact { FirstName = "Frank" });

            var retrieved = Contact.Retrieve(orgAdminService, contactId);
            Assert.Equal("Copenhagen", retrieved.Address2_City);
        }
    }
}
