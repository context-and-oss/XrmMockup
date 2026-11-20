using System;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using Xunit;
using Xunit.Sdk;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest
{
    // Late-bound tests for the Win/Lose opportunity request handlers; Opportunity/OpportunityClose
    // metadata is supplied by RemovedEntitiesMetadata.xml (entities not in the environment).
    public class TestOpportunity : UnitTestBase
    {
        public TestOpportunity(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestWinOpportunity()
        {
            var opportunity = new Entity("opportunity");
            opportunity.Id = orgAdminUIService.Create(opportunity);

            var winReq = new WinOpportunityRequest();
            var opclose = new Entity("opportunityclose")
            {
                ["actualrevenue"] = new Money(1000m),
                ["actualend"] = DateTime.Now,
                ["statecode"] = new OptionSetValue(1),
                ["statuscode"] = new OptionSetValue(2),
                ["opportunityid"] = opportunity.ToEntityReference()
            };
            winReq.OpportunityClose = opclose;
            winReq.Status = new OptionSetValue(3);

            orgAdminUIService.Execute(winReq);

            var retrieved = orgAdminUIService.Retrieve("opportunity", opportunity.Id, new ColumnSet(true));
            Assert.Equal(1, retrieved.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(3, retrieved.GetAttributeValue<OptionSetValue>("statuscode").Value);
            Assert.True(crm.ContainsEntity(opclose));
            Assert.Equal("SetFromWinLose", retrieved.GetAttributeValue<string>("description"));
        }

        [Fact]
        public void TestLoseOpportunity()
        {
            var opportunity = new Entity("opportunity");
            opportunity.Id = orgAdminUIService.Create(opportunity);

            var loseReq = new LoseOpportunityRequest();
            var opclose = new Entity("opportunityclose")
            {
                ["actualrevenue"] = new Money(1000m),
                ["actualend"] = DateTime.Now,
                ["statuscode"] = new OptionSetValue(3),
                ["opportunityid"] = opportunity.ToEntityReference()
            };
            loseReq.OpportunityClose = opclose;
            loseReq.Status = new OptionSetValue(4);

            orgAdminUIService.Execute(loseReq);

            var retrieved = orgAdminUIService.Retrieve("opportunity", opportunity.Id, new ColumnSet(true));
            Assert.Equal(2, retrieved.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(4, retrieved.GetAttributeValue<OptionSetValue>("statuscode").Value);
            Assert.True(crm.ContainsEntity(opclose));
            Assert.Equal("SetFromWinLose", retrieved.GetAttributeValue<string>("description"));
        }
    }
}
