using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using System;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestSharing : UnitTestBase
    {
        public TestSharing(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestSharingAccess()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name";
                businessunit.Id = orgAdminUIService.Create(businessunit);
                var otherUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.XrmMockupTestReadOnly);
                var sharingUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator);

                var otherService = crm.CreateOrganizationService(otherUser.Id);
                var sharingService = crm.CreateOrganizationService(sharingUser.Id);
                var contact = new Contact();
                contact.Id = sharingService.Create(contact);

                try
                {
                    otherService.Retrieve(Contact.EntityLogicalName, contact.Id, new ColumnSet(true));
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }
                var req = new GrantAccessRequest
                {
                    PrincipalAccess = new PrincipalAccess()
                    {
                        AccessMask = AccessRights.ReadAccess,
                        Principal = otherUser.ToEntityReference()
                    },
                    Target = contact.ToEntityReference()
                };
                sharingService.Execute(req);

                otherService.Retrieve(Contact.EntityLogicalName, contact.Id, new ColumnSet(true));
            }
        }
    }
}
