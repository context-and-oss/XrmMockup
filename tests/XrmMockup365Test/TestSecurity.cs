//using System;
//using Microsoft.Xrm.Sdk.Query;
//using DG.Tools.XrmMockup;
//using System.ServiceModel;
//using Microsoft.Crm.Sdk.Messages;
//using DG.XrmFramework.BusinessDomain.ServiceContext;
//using Xunit;
//using Xunit.Sdk;
//using Microsoft.Xrm.Sdk;

//namespace DG.XrmMockupTest
//{
//    public class TestSecurity : UnitTestBase
//    {
//        public TestSecurity(XrmMockupFixture fixture) : base(fixture) { }

//        [Fact ]
//        public void TestBasicSecurity()
//        {
//            //create a team
//            var team1 = new Team { Name = "* RECORD OWNER TEAM *", BusinessUnitId = crm.RootBusinessUnit };
//            team1.TeamType = team_type.Owner;
//            team1 = crm.CreateTeam(orgAdminService, team1, SecurityRoles.MockRecordOwnerTeamRole).ToEntity<Team>();

//            var user1 = new SystemUser() { BusinessUnitId = crm.RootBusinessUnit };
//            var user = crm.CreateUser(orgAdminService, user1, SecurityRoles.MockSecurityRole);
//            var userService = crm.CreateOrganizationService(user.Id);

//            var child = new Entity("mock_child");
//            child.Id = userService.Create(child);

//            //check that the child has the parent id populated
//            var checkChild = orgAdminService.Retrieve("mock_child", child.Id, new ColumnSet(true));
//            Assert.NotNull(checkChild.GetAttributeValue<EntityReference>("mock_parentid"));
//        }
//    }
//}
