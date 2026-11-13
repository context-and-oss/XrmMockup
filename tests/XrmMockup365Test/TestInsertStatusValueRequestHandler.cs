using System.Linq;
using System.ServiceModel;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestInsertStatusValueRequestHandler : RequestHandlerTestBase
    {
        public TestInsertStatusValueRequestHandler(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void HandlerIsRegisteredExactlyOnce()
        {
            Assert.Single(engine.RequestHandlers.Where(handler => handler.HandlesRequest("InsertStatusValue")));
        }

        [Fact]
        public void InsertStatusPreservesStateAndDescriptionAndCanBeUsedInUpdate()
        {
            var response = (InsertStatusValueResponse)orgAdminService.Execute(new InsertStatusValueRequest { EntityLogicalName = "account", AttributeLogicalName = "statuscode", StateCode = 0, Label = new Label("New status", 1033), Description = new Label("Not the label", 1033) });
            var status = (StatusAttributeMetadata)AccountAttribute("statuscode");
            var option = (StatusOptionMetadata)status.OptionSet.Options.Single(o => o.Value == response.NewOptionValue);
            Assert.Equal(0, option.State);
            Assert.Equal("Not the label", option.Description.LocalizedLabels[0].Label);
            var id = orgAdminService.Create(new Account());
            orgAdminService.Update(new Entity("account", id) { ["statuscode"] = new OptionSetValue(response.NewOptionValue) });
            Assert.Equal(response.NewOptionValue, Read("account", id).GetAttributeValue<OptionSetValue>("statuscode").Value);
        }

        [Fact]
        public void InsertStatusRejectsUnknownStateAndNonStatusColumn()
        {
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new InsertStatusValueRequest { EntityLogicalName = "account", AttributeLogicalName = "statuscode", StateCode = 999, Label = new Label("Invalid", 1033) }));
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new InsertStatusValueRequest { EntityLogicalName = "account", AttributeLogicalName = "preferredcontactmethodcode", StateCode = 0, Label = new Label("Invalid", 1033) }));
        }
    }
}
