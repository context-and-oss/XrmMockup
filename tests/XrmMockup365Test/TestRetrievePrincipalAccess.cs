using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.Tools.XrmMockup;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestRetrievePrincipalAccess : UnitTestBase
    {
        public SystemUser TestUser;

        public TestRetrievePrincipalAccess(XrmMockupFixture fixture) : base(fixture)
        {
            TestUser = crm.CreateUser(orgAdminService, new SystemUser { BusinessUnitId = crm.RootBusinessUnit }, SecurityRoles.XrmMockupTestReadOnly) as SystemUser;
        }

        [Fact]
        public void TestRetrievePrincipalAccessForTestUserOnParent()
        {
            // XrmMockupTestReadOnly has GLOBAL (org) read on ctx_parent, so the test user gets
            // ReadAccess even on an admin-owned ctx_parent record. (Originally Lead, then Contact;
            // Contact read is only user-level for this role, so an admin-owned record granted no access.)
            var parent = new ctx_parent();
            parent.Id = orgAdminService.Create(parent);

            var principalAccess = (RetrievePrincipalAccessResponse)orgAdminService.Execute(new RetrievePrincipalAccessRequest
            {
                Principal = TestUser.ToEntityReference(),
                Target = parent.ToEntityReference()
            });

            Assert.True(principalAccess.AccessRights.HasFlag(AccessRights.ReadAccess));
        }

        [Fact]
        public void TestRetrievePrincipalAccessForTestUserOnAccount()
        {
            var account = new Account();
            account.Id = orgAdminService.Create(account);

            var principalAccess = (RetrievePrincipalAccessResponse)orgAdminService.Execute(new RetrievePrincipalAccessRequest
            {
                Principal = TestUser.ToEntityReference(),
                Target = account.ToEntityReference()
            });

            Assert.True(principalAccess.AccessRights == (AccessRights.ReadAccess | AccessRights.CreateAccess));
        }
    }
}