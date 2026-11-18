using System.Linq;
using System.ServiceModel;
using System;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestNavigateToNextEntityRequestHandler : RequestHandlerTestBase
    {
        public TestNavigateToNextEntityRequestHandler(XrmMockupFixture fixture) : base(fixture)
        {
            AddTable("processstage", Lookup("processid", "workflow"), Text("primaryentitytypecode"));
        }

        [Fact]
        public void HandlerIsRegisteredExactlyOnce()
        {
            Assert.Single(engine.RequestHandlers.Where(handler => handler.HandlesRequest("NavigateToNextEntity")));
        }

        private OrganizationRequest Navigate(Guid process, Guid stage, Guid current, Guid next) => new OrganizationRequest("NavigateToNextEntity")
        {
            ["ProcessId"] = process, ["NewActiveStageId"] = stage, ["CurrentEntityLogicalName"] = "account", ["CurrentEntityId"] = current,
            ["NextEntityLogicalName"] = "contact", ["NextEntityId"] = next, ["NewTraversedPath"] = stage.ToString("D")
        };

        [Fact]
        public void NavigateUpdatesBothRecordsAndReturnsPath()
        {
            var process = Create("workflow", ("name", "Flow"), ("category", new OptionSetValue(4)));
            var stage = Create("processstage", ("processid", new EntityReference("workflow", process)), ("primaryentitytypecode", "contact"));
            var current = orgAdminService.Create(new Account()); var next = orgAdminService.Create(new Contact { FirstName = "Next" });
            var response = orgAdminService.Execute(Navigate(process, stage, current, next));
            Assert.Equal(stage.ToString("D"), response["TraversedPath"]);
            Assert.Equal(stage, Read("account", current).GetAttributeValue<Guid>("stageid"));
            Assert.Equal(stage, Read("contact", next).GetAttributeValue<Guid>("stageid"));
        }

        [Fact]
        public void NavigateValidatesBothRecordsBeforeUpdatingEither()
        {
            var process = Create("workflow", ("name", "Flow"), ("category", new OptionSetValue(4)));
            var stage = Create("processstage", ("processid", new EntityReference("workflow", process)), ("primaryentitytypecode", "contact"));
            var current = orgAdminService.Create(new Account());
            Assert.Throws<FaultException>(() => orgAdminService.Execute(Navigate(process, stage, current, Guid.NewGuid())));
            Assert.Equal(Guid.Empty, Read("account", current).GetAttributeValue<Guid>("stageid"));
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new OrganizationRequest("NavigateToNextEntity") { ["ProcessId"] = Guid.Empty }));
        }
    }
}
