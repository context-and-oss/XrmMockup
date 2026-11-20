using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;
using DG.Tools.XrmMockup.Internal;

namespace DG.XrmMockupTest
{
    public class TestDefaultBusinessUnitTeamMembers : UnitTestBase
    {
        private BusinessUnit _businessUnit1;
        private BusinessUnit _businessUnit2;
        private BusinessUnit _businessUnit3;
        private BusinessUnit _businessUnit4;
        private SystemUser _user1;
        private SystemUser _user2;

        public TestDefaultBusinessUnitTeamMembers(XrmMockupFixture fixture) : base(fixture)
        {
            EntityReference businessUnitId = crm.RootBusinessUnit;

            _businessUnit1 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 1" };
            _businessUnit1.Id = orgAdminService.Create(_businessUnit1);

            _businessUnit2 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 2" };
            _businessUnit2.Id = orgAdminService.Create(_businessUnit2);
            _user1 = crm.CreateUser(orgAdminService, _businessUnit2.ToEntityReference(), SecurityRoles.XrmMockupTestNoContactAccess) as SystemUser;

            _businessUnit3 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 3" };
            _businessUnit3.Id = orgAdminService.Create(_businessUnit3);
            _businessUnit4 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 4" };
            _businessUnit4.Id = orgAdminService.Create(_businessUnit4);
            _user2 = crm.CreateUser(orgAdminService, _businessUnit3.ToEntityReference(), SecurityRoles.XrmMockupTestNoContactAccess) as SystemUser;
        }

        [Fact]
        public void AddMembersToBusinessUnit()
        {
            var team = RetrieveBusinessUnitDefaultTeamAndCheckAttributes(_businessUnit1);

            crm.CreateUser(orgAdminService, _businessUnit1.ToEntityReference(), SecurityRoles.XrmMockupTestNoContactAccess);

            using (var context = new Xrm(orgAdminService))
            {
                var fetchedTeam = context.TeamMembershipSet.Where(x => x.TeamId == team.Id).ToList();
                Assert.Single(fetchedTeam);
            }
        }

        [Fact]
        public void RemoveMemberFromBusinessUnit()
        {
            var team = RetrieveBusinessUnitDefaultTeamAndCheckAttributes(_businessUnit2);

            orgAdminService.Delete("systemuser", _user1.Id);

            using (var context = new Xrm(orgAdminService))
            {
                var fetchedTeam = context.TeamMembershipSet.Where(x => x.TeamId == team.Id).ToList();
                Assert.Empty(fetchedTeam);
            }
        }

        [Fact]
        public void MoveMemberFromBUOneToBUTwo()
        {
            var retrievedUser = orgAdminService.Retrieve("systemuser", _user2.Id, new ColumnSet("businessunitid"));

            retrievedUser.Attributes["businessunitid"] = _businessUnit4.ToEntityReference();
            // retrievedUser.Attributes["name"] = "Just changed the name ;)";

            orgAdminService.Update(retrievedUser);

            var team3 = RetrieveBusinessUnitDefaultTeamAndCheckAttributes(_businessUnit3);
            var team4 = RetrieveBusinessUnitDefaultTeamAndCheckAttributes(_businessUnit4);

            using (var context = new Xrm(orgAdminService))
            {
                var fetchedTeam = context.TeamMembershipSet.Where(x => x.TeamId == team3.Id).ToList();
                Assert.Empty(fetchedTeam);
            }

            using (var context = new Xrm(orgAdminService))
            {
                var fetchedTeam = context.TeamMembershipSet.Where(x => x.TeamId == team4.Id).ToList();
                Assert.Single(fetchedTeam);
            }
        }

        private Team RetrieveBusinessUnitDefaultTeamAndCheckAttributes(BusinessUnit businessUnit)
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedTeam = context.TeamSet
                    .Where(x => x.BusinessUnitId.Id == businessUnit.Id)
                    .Where(x => x.Name == businessUnit.Name)
                    .FirstOrDefault();

                Assert.NotNull(fetchedTeam);
                CheckTeamAttributes(fetchedTeam, businessUnit);

                return fetchedTeam;
            }
        }

        private void CheckTeamAttributes(Team fetchedTeam, BusinessUnit businessUnit)
        {
            businessUnit = (BusinessUnit)orgAdminService.Retrieve(LogicalNames.BusinessUnit, businessUnit.Id, new ColumnSet("name", "createdby"));

            Assert.Equal(businessUnit.Name, fetchedTeam.Name);
            Assert.Equal(team_type.Owner, fetchedTeam.TeamType);
            Assert.Equal(true, fetchedTeam.IsDefault);
            Assert.Equal("Default team for the parent business unit. The name and membership for default team are inherited from their parent business unit.",
                 fetchedTeam.Description);
            Assert.Equal(businessUnit.CreatedBy.Id, fetchedTeam.AdministratorId.Id);
            Assert.Equal(businessUnit.Id, fetchedTeam.BusinessUnitId.Id);
        }
    }
}
