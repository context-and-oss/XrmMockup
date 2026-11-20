using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestTeams : UnitTestBase
    {
        private EntityReference parent1;
        private EntityReference parent2;
        private EntityReference account11;
        private EntityReference account12;
        private EntityReference account21;
        private EntityReference account22;
        private EntityReference account1;
        private EntityReference account2;
        private EntityReference account3;
        private EntityReference account4;

        SystemUser user11;
        SystemUser user12;
        SystemUser user21;
        SystemUser user22;

        BusinessUnit businessUnit1;
        BusinessUnit businessUnit2;
        private Team team1;
        private Team team2;
        private Team team3;
        private Team team4;

        public TestTeams(XrmMockupFixture fixture) : base(fixture)
        {
            EntityReference businessUnitId = crm.RootBusinessUnit;

            businessUnit1 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 1" };
            businessUnit1.Id = orgAdminService.Create(businessUnit1);

            businessUnit2 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 2" };
            businessUnit2.Id = orgAdminService.Create(businessUnit2);

            team1 = new Team
            {
                Name = "Team 1",
                BusinessUnitId = businessUnit1.ToEntityReference(),
                TeamType = team_type.Owner
            };
            team1 = crm.CreateTeam(orgAdminService, team1, SecurityRoles.XrmMockupTestNoContactAccess).ToEntity<Team>();

            team2 = new Team
            {
                Name = "Team 2",
                BusinessUnitId = businessUnit1.ToEntityReference(),
                TeamType = team_type.Owner
            };
            team2 = crm.CreateTeam(orgAdminService, team2, SecurityRoles.XrmMockupTestNoContactAccess).ToEntity<Team>();

            team3 = new Team
            {
                Name = "Team 3",
                BusinessUnitId = businessUnit1.ToEntityReference(),
                TeamType = team_type.Owner
            };
            team3 = crm.CreateTeam(orgAdminService, team3, SecurityRoles.SystemAdministrator).ToEntity<Team>();

            team4 = new Team
            {
                Name = "Team 4",
                BusinessUnitId = businessUnit1.ToEntityReference(),
                TeamType = team_type.Owner
            };
            team4 = crm.CreateTeam(orgAdminService, team4, SecurityRoles.SystemAdministrator).ToEntity<Team>();

            // SystemCustomizer - read account - user level, write account - user level
            user11 = crm.CreateUser(orgAdminService, businessUnit1.ToEntityReference(), SecurityRoles.XrmMockupTestNoContactAccess) as SystemUser;
            user12 = crm.CreateUser(orgAdminService, businessUnit1.ToEntityReference(), SecurityRoles.XrmMockupTestNoContactAccess) as SystemUser;
            user21 = crm.CreateUser(orgAdminService, businessUnit2.ToEntityReference(), SecurityRoles.XrmMockupTestNoContactAccess) as SystemUser;
            user22 = crm.CreateUser(orgAdminService, businessUnit2.ToEntityReference(), SecurityRoles.XrmMockupTestNoContactAccess) as SystemUser;

            parent1 = new EntityReference(Account.EntityLogicalName, orgAdminService.Create(new Account { Name = "Parent 1" }));
            parent2 = new EntityReference(Account.EntityLogicalName, orgAdminService.Create(new Account { Name = "Parent 2" }));

            account11 = new EntityReference(Account.EntityLogicalName, orgGodService.Create(new Account { Name = "Child 1.1", ParentAccountId = parent1, OwnerId = user11.ToEntityReference() }));
            account12 = new EntityReference(Account.EntityLogicalName, orgGodService.Create(new Account { Name = "Child 1.2", ParentAccountId = parent1, OwnerId = user12.ToEntityReference() }));
            account21 = new EntityReference(Account.EntityLogicalName, orgGodService.Create(new Account { Name = "Child 2.1", ParentAccountId = parent2, OwnerId = user21.ToEntityReference() }));
            account22 = new EntityReference(Account.EntityLogicalName, orgGodService.Create(new Account { Name = "Child 2.2", ParentAccountId = parent2, OwnerId = user22.ToEntityReference() }));
            account1 = new EntityReference(Account.EntityLogicalName, orgGodService.Create(new Account { Name = "Child 1", ParentAccountId = parent1, OwnerId = team1.ToEntityReference() }));
            account2 = new EntityReference(Account.EntityLogicalName, orgGodService.Create(new Account { Name = "Child 2", ParentAccountId = parent2, OwnerId = team2.ToEntityReference() }));
            account3 = new EntityReference(Account.EntityLogicalName, orgGodService.Create(new Account { Name = "Child 3", ParentAccountId = parent1, OwnerId = team2.ToEntityReference() }));
            account4 = new EntityReference(Account.EntityLogicalName, orgGodService.Create(new Account { Name = "Child 4", ParentAccountId = parent2, OwnerId = team3.ToEntityReference() }));
        }

        [Fact]
        public void CreateTeam()
        {
            var team = crm.CreateTeam(orgAdminService, crm.RootBusinessUnit, SecurityRoles.XrmMockupTestNoContactAccess);
            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedTeam = context.TeamSet.FirstOrDefault(x => x.Id == team.Id);
                Assert.NotNull(fetchedTeam);
                Assert.Equal(team_type.Owner, fetchedTeam.TeamType);
            }
        }


        [Fact]
        public void TestAddMembers()
        {
            var bu1 = new BusinessUnit() { Name = "bu1", ParentBusinessUnitId = crm.RootBusinessUnit };
            bu1.Id = orgAdminService.Create(bu1);
            var bu2 = new BusinessUnit() { Name = "bu2", ParentBusinessUnitId = crm.RootBusinessUnit };
            bu2.Id = orgAdminService.Create(bu2);
            var user1 = crm.CreateUser(orgAdminService, bu1.ToEntityReference(), SecurityRoles.XrmMockupTestNoContactAccess);
            var user2 = crm.CreateUser(orgAdminService, bu2.ToEntityReference(), SecurityRoles.XrmMockupTestNoContactAccess);

            var team = crm.CreateTeam(orgAdminService, crm.RootBusinessUnit, SecurityRoles.XrmMockupTestNoContactAccess);
            crm.AddUsersToTeam(team.ToEntityReference(), user1.ToEntityReference(), user2.ToEntityReference());

            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedTeam = context.TeamMembershipSet.Where(x => x.TeamId == team.Id).ToList();
                Assert.Equal(2, fetchedTeam.Count);
                var user1Member = fetchedTeam.FirstOrDefault(x => x.SystemUserId == user1.Id);
                Assert.NotNull(user1Member);
                var user2Member = fetchedTeam.FirstOrDefault(x => x.SystemUserId == user2.Id);
                Assert.NotNull(user2Member);
            }
        }

        [Fact]
        public void TestRemoveMembers()
        {
            var bu1 = new BusinessUnit() { Name = "bu1", ParentBusinessUnitId = crm.RootBusinessUnit };
            bu1.Id = orgAdminService.Create(bu1);
            var bu2 = new BusinessUnit() { Name = "bu2", ParentBusinessUnitId = crm.RootBusinessUnit };
            bu2.Id = orgAdminService.Create(bu2);
            var user1 = crm.CreateUser(orgAdminService, bu1.ToEntityReference(), SecurityRoles.XrmMockupTestNoContactAccess);
            var user2 = crm.CreateUser(orgAdminService, bu2.ToEntityReference(), SecurityRoles.XrmMockupTestNoContactAccess);

            var team = crm.CreateTeam(orgAdminService, crm.RootBusinessUnit, SecurityRoles.XrmMockupTestNoContactAccess);
            crm.AddUsersToTeam(team.ToEntityReference(), user1.ToEntityReference(), user2.ToEntityReference());

            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedTeam = context.TeamMembershipSet.Where(x => x.TeamId == team.Id).ToList();
                Assert.Equal(2, fetchedTeam.Count);
                var user1Member = fetchedTeam.FirstOrDefault(x => x.SystemUserId == user1.Id);
                Assert.NotNull(user1Member);
                var user2Member = fetchedTeam.FirstOrDefault(x => x.SystemUserId == user2.Id);
                Assert.NotNull(user2Member);
            }

            crm.RemoveUsersFromTeam(team.ToEntityReference(), user1.ToEntityReference(), user2.ToEntityReference());
            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedTeam = context.TeamMembershipSet.Where(x => x.TeamId == team.Id).ToList();
                Assert.Empty(fetchedTeam);
            }
        }


        #region Positive Tests
        //A User can see there own Account
        [Fact]
        public void UserCanReadOwn()
        {
            Assert.True(CanRead(user11, account11));
            Assert.True(CanRead(user12, account12));
            Assert.True(CanRead(user21, account21));
            Assert.True(CanRead(user22, account22));
        }

        //A User can edit there own Account
        [Fact]
        public void UserCanWriteOwn()
        {
            Assert.True(CanWrite(user11, account11));
            Assert.True(CanWrite(user12, account12));
            Assert.True(CanWrite(user21, account21));
            Assert.True(CanWrite(user22, account22));
        }

        //A User can see the account of its teams
        [Fact]
        public void UserCanReadTeam()
        {
            crm.AddUsersToTeam(team1.ToEntityReference(), user11.ToEntityReference(), user21.ToEntityReference());
            crm.AddUsersToTeam(team2.ToEntityReference(), user12.ToEntityReference(), user22.ToEntityReference());

            Assert.True(CanRead(user11, account1));
            Assert.True(CanRead(user12, account2));
            Assert.True(CanRead(user21, account1));
            Assert.True(CanRead(user22, account2));
        }
        //A user can access a team when joining it.
        [Fact]
        public void UserCanReadAfterJoining()
        {
            Assert.False(CanRead(user12, account1));
            crm.AddUsersToTeam(team1.ToEntityReference(), user12.ToEntityReference());
            Assert.True(CanRead(user12, account1));
        }
        #endregion

        #region Negative Tests
        //A User can't see the account beloning to different officies
        [Fact]
        public void UserCantReadOtherUnit()
        {
            Assert.False(CanRead(user11, account21));
            Assert.False(CanRead(user12, account22));
            Assert.False(CanRead(user21, account12));
            Assert.False(CanRead(user22, account11));
        }
        //A User can't see the account  beloning to different team
        [Fact]
        public void UserCantReadOtherTeam()
        {
            crm.AddUsersToTeam(team1.ToEntityReference(), user11.ToEntityReference(), user21.ToEntityReference());
            crm.AddUsersToTeam(team2.ToEntityReference(), user12.ToEntityReference(), user22.ToEntityReference());

            Assert.False(CanRead(user11, account2));
            Assert.False(CanRead(user12, account1));
            Assert.False(CanRead(user21, account2));
            Assert.False(CanRead(user22, account1));
        }

        //A user from a different business unit can't read another members account when team has user level access
        [Fact]
        public void UserCantReadOtherTeamMembersUserLevel()
        {
            crm.AddUsersToTeam(team1.ToEntityReference(), user11.ToEntityReference(), user21.ToEntityReference());
            crm.AddUsersToTeam(team2.ToEntityReference(), user12.ToEntityReference(), user22.ToEntityReference());

            Assert.False(CanRead(user11, account21));
            Assert.False(CanRead(user12, account22));
            Assert.False(CanRead(user21, account11));
            Assert.False(CanRead(user22, account12));
        }

        //A user from a different business unit can read another members account when team has global level access
        [Fact]
        public void UserCanReadOtherTeamMembersGlobalLevel()
        {
            crm.AddUsersToTeam(team3.ToEntityReference(), user11.ToEntityReference(), user21.ToEntityReference());
            crm.AddUsersToTeam(team4.ToEntityReference(), user12.ToEntityReference(), user22.ToEntityReference());

            Assert.True(CanRead(user11, account21));
            Assert.True(CanRead(user12, account22));
            Assert.True(CanRead(user21, account11));
            Assert.True(CanRead(user22, account12));
        }

        //A user can't access a team after leaving it.
        [Fact]
        public void UserCantReadAfterLeaving()
        {
            crm.AddUsersToTeam(team2.ToEntityReference(), user12.ToEntityReference());
            Assert.True(CanRead(user12, account2));
            crm.RemoveUsersFromTeam(team2.ToEntityReference(), user12.ToEntityReference());
            Assert.False(CanRead(user12, account2));
        }

        #endregion
        public bool CanRead(SystemUser user, EntityReference account)
        {
            try
            {
                IOrganizationService userService = crm.CreateOrganizationService(user.Id);

                var entity = Account.Retrieve(userService, account.Id);
                return entity != null;
            }
            catch (FaultException e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public bool CanWrite(SystemUser user, EntityReference account)
        {
            try
            {
                IOrganizationService userService = crm.CreateOrganizationService(user.Id);
                userService.Update(new Account
                {
                    Id = account.Id,
                    EMailAddress1 = "Test address"
                });
            }
            catch (FaultException)
            {
                //throw e;
                return false;
            }
            return true;
        }
    }
}
