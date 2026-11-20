using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.Tools.XrmMockup;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestWhoAmI : UnitTestBase
    {
        public TestWhoAmI(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void WhoAmIRequestForAdmin()
        {
            var whoAmI = (WhoAmIResponse)orgAdminService.Execute(new WhoAmIRequest());
            var admin = orgAdminService.Retrieve<SystemUser>(crm.AdminUser.Id);
            Assert.Equal(whoAmI.UserId, admin.Id);
            Assert.Equal(whoAmI.BusinessUnitId, admin.BusinessUnitId.Id);
        }

        [Fact]
        public void WhoAmIRequestForOtherUser()
        {
            var bu = new BusinessUnit { Name = "child bu", ParentBusinessUnitId = crm.RootBusinessUnit };
            bu.Id = orgAdminUIService.Create(bu);
            var user = crm.CreateUser(orgAdminService, new SystemUser { BusinessUnitId = bu.ToEntityReference() }, SecurityRoles.XrmMockupTestReadOnly) as SystemUser;
            var service = crm.CreateOrganizationService(user.Id);
            var whoAmI = (WhoAmIResponse)service.Execute(new WhoAmIRequest());
            Assert.Equal(whoAmI.UserId, user.Id);
            Assert.Equal(whoAmI.BusinessUnitId, bu.Id);
        }
    }
}