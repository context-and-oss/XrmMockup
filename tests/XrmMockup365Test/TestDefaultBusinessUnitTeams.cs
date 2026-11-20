using DG.XrmFramework.BusinessDomain.ServiceContext;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Query;
using Xunit;
using Xunit.Sdk;
using DG.Tools.XrmMockup.Internal;

namespace DG.XrmMockupTest
{
    public class TestDefaultBusinessUnitTeams : UnitTestBase
    {
        private BusinessUnit _businessUnit1;
        private BusinessUnit _businessUnit2;
        private BusinessUnit _businessUnit3;

        public TestDefaultBusinessUnitTeams(XrmMockupFixture fixture) : base(fixture)
        {
            EntityReference businessUnitId = crm.RootBusinessUnit;

            _businessUnit1 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 1" };

            _businessUnit2 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 2" };
            _businessUnit2.Id = orgAdminService.Create(_businessUnit2);

            _businessUnit3 = new BusinessUnit { ParentBusinessUnitId = businessUnitId, Name = "Business Unit 3" };
            _businessUnit3.Id = orgAdminService.Create(_businessUnit3);
        }

        [Fact]
        public void CreateBusinessUnit()
        {
            _businessUnit1.Id = orgAdminService.Create(_businessUnit1);

            RetrieveBusinessUnitDefaultTeamAndCheckAttributes(_businessUnit1);
        }

        [Fact]
        public void UpdateBusinessUnit()
        {
            _businessUnit2.Name = "A new business unit name";
            orgAdminService.Update(_businessUnit2);

            RetrieveBusinessUnitDefaultTeamAndCheckAttributes(_businessUnit2);
        }

        [Fact]
        public void DeleteBusinessUnit()
        {
            var fetchedTeam = RetrieveBusinessUnitDefaultTeamAndCheckAttributes(_businessUnit3);

            orgAdminService.Delete("businessunit", _businessUnit3.Id);

            try
            {
                orgAdminService.Retrieve("team", fetchedTeam.Id, new ColumnSet(true));
            }
            catch (FaultException e)
            {
                // Test error message
                Assert.Equal(
                     $"The record of type 'team' with id '{fetchedTeam.Id}' does not exist. If you use hard-coded records from CRM, then make sure you create those records before retrieving them.",
                     e.Message);
                return;
            }
            throw new XunitException("Exception when trying to fetch deleted team isn't thrown.");
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
