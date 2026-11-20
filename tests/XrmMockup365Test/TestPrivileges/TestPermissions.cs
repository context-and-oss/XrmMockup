using System;
using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup;
using System.ServiceModel;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestPermissions : UnitTestBase
    {
        public TestPermissions(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestPermissionWhenThroughTeam()
        {
            var businessunit = new BusinessUnit();
            businessunit["name"] = "Business unit name";
            businessunit.Id = orgAdminUIService.Create(businessunit);
            // Create a user which does not have read access to Contact
            var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.XrmMockupTestNoContactAccess);
            // Create a service with the user
            var userService = crm.CreateOrganizationService(user.Id);
            // Create a Team that does have write access to Contact
            var createTeam1 = new Team
            {
                BusinessUnitId = businessunit.ToEntityReference()
            };
            // Access-team role: BU-level contact write, so a member of team2 can update a contact
            // owned by team1 (XrmMockupTestUser's contact write is only user-level/Basic).
            var team1 = crm.CreateTeam(orgAdminUIService, createTeam1, SecurityRoles.XrmMockupTestAccessTeam);
            var createTeam2 = new Team
            {
                BusinessUnitId = businessunit.ToEntityReference()
            };
            var team2 = crm.CreateTeam(orgAdminUIService, createTeam2, SecurityRoles.XrmMockupTestAccessTeam);
            // Create a Contact with Team as owner
            var contact = new Contact
            {
                OwnerId = team1.ToEntityReference()
            };
            var contactId = orgAdminUIService.Create(contact);
            // Update Contact using the user service
            var updateContact = new Contact
            {
                Id = contactId,
                JobTitle = "CEO"
            };
            try
            {
                userService.Update(updateContact);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }

            // Add user to team
            crm.AddUsersToTeam(team2.ToEntityReference(), user.ToEntityReference());
            // Update contact using the user service
            userService.Update(updateContact);
            // Assert success
            contact = (Contact)orgAdminUIService.Retrieve(Contact.EntityLogicalName, contactId, new ColumnSet(true));
            Assert.Equal("CEO", contact.GetAttributeValue<string>("jobtitle"));
        }
    }
}
