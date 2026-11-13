using System.Linq;
using System.ServiceModel;
using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestInsertOptionValueRequestHandler : RequestHandlerTestBase
    {
        public TestInsertOptionValueRequestHandler(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void HandlerIsRegisteredExactlyOnce()
        {
            Assert.Single(engine.RequestHandlers.Where(handler => handler.HandlesRequest("InsertOptionValue")));
        }

        [Fact]
        public void InsertLocalChoiceReturnsGeneratedValueAndPreservesExistingMetadata()
        {
            var before = (PicklistAttributeMetadata)AccountAttribute("preferredcontactmethodcode");
            var count = before.OptionSet.Options.Count;
            var response = (InsertOptionValueResponse)orgAdminService.Execute(new InsertOptionValueRequest { EntityLogicalName = "account", AttributeLogicalName = "preferredcontactmethodcode", Label = new Label("New", 1033), Description = new Label("Description", 1033) });
            Assert.True(response.NewOptionValue >= 100000000);
            var after = (PicklistAttributeMetadata)AccountAttribute("preferredcontactmethodcode");
            Assert.Equal(count + 1, after.OptionSet.Options.Count);
            Assert.Equal(count, before.OptionSet.Options.Count);
            Assert.Equal("Description", after.OptionSet.Options.Single(o => o.Value == response.NewOptionValue).Description.LocalizedLabels[0].Label);
            var other = XrmMockup365.GetInstance(settings).GetAdminService();
            var otherAttribute = (RetrieveAttributeResponse)other.Execute(new RetrieveAttributeRequest { EntityLogicalName = "account", LogicalName = "preferredcontactmethodcode" });
            Assert.Equal(count, ((PicklistAttributeMetadata)otherAttribute.AttributeMetadata).OptionSet.Options.Count);
        }

        [Fact]
        public void InsertGlobalChoiceIsVisibleInRetrieveAllOptionSets()
        {
            var response = (InsertOptionValueResponse)orgAdminService.Execute(new InsertOptionValueRequest { OptionSetName = "workflow_stage", Label = new Label("Added", 1033), Value = 100000123 });
            Assert.Equal(100000123, response.NewOptionValue);
            var all = (RetrieveAllOptionSetsResponse)orgAdminService.Execute(new RetrieveAllOptionSetsRequest());
            Assert.Contains(((OptionSetMetadata)all.OptionSetMetadata.Single(o => o.Name == "workflow_stage")).Options, o => o.Value == response.NewOptionValue);
        }

        [Fact]
        public void InsertChoiceRejectsEmptyLabelsDuplicatesAndUnknownColumnsWithoutMutation()
        {
            var before = (PicklistAttributeMetadata)AccountAttribute("preferredcontactmethodcode");
            var count = before.OptionSet.Options.Count;
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new InsertOptionValueRequest { Label = new Label(), EntityLogicalName = "account", AttributeLogicalName = "preferredcontactmethodcode" }));
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new InsertOptionValueRequest { Label = new Label("Duplicate", 1033), EntityLogicalName = "account", AttributeLogicalName = "preferredcontactmethodcode", Value = before.OptionSet.Options[0].Value }));
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new InsertOptionValueRequest { Label = new Label("Invalid", 1033), EntityLogicalName = "account", AttributeLogicalName = "name" }));
            Assert.Equal(count, ((PicklistAttributeMetadata)AccountAttribute("preferredcontactmethodcode")).OptionSet.Options.Count);
        }

        [Fact]
        public void GlobalChoiceInsertionUpdatesReferencingColumnsWithoutLeaking()
        {
            var original = ((OptionSetMetadata)metadata.OptionSets.Single(o => o.Name == "workflow_stage"));
            var entity = CloneMetadata(metadata.EntityMetadata["account"]);
            ((PicklistAttributeMetadata)entity.Attributes.First(a => a.LogicalName == "preferredcontactmethodcode")).OptionSet = original;
            metadata.EntityMetadata["account"] = entity;
            var response = (InsertOptionValueResponse)orgAdminService.Execute(new InsertOptionValueRequest { OptionSetName = "workflow_stage", Label = new Label("Global inserted", 1033) });
            var options = ((PicklistAttributeMetadata)AccountAttribute("preferredcontactmethodcode")).OptionSet.Options;
            Assert.Contains(options, o => o.Value == response.NewOptionValue);
            Assert.DoesNotContain(original.Options, o => o.Value == response.NewOptionValue);
        }

        [Fact]
        public void InsertedChoiceProducesFormattedValueAndDoesNotAliasRequestLabel()
        {
            var label = new Label("New label", 1033);
            var response = (InsertOptionValueResponse)orgAdminService.Execute(new InsertOptionValueRequest { EntityLogicalName = "account", AttributeLogicalName = "preferredcontactmethodcode", Label = label });
            label.LocalizedLabels[0].Label = "Changed by caller";
            var id = orgAdminService.Create(new Entity("account") { ["preferredcontactmethodcode"] = new OptionSetValue(response.NewOptionValue) });
            Assert.Equal("New label", Read("account", id).FormattedValues["preferredcontactmethodcode"]);
        }

        [Fact]
        public void LocalMultiSelectChoiceCanInsertOptions()
        {
            var entity = CloneMetadata(metadata.EntityMetadata["account"]);
            var attribute = new MultiSelectPicklistAttributeMetadata
            {
                LogicalName = "parity_choices", OptionSet = new OptionSetMetadata(new OptionMetadataCollection())
            };
            Set(attribute, "IsValidForCreate", true);
            Set(attribute, "IsValidForUpdate", true);
            Set(attribute, "IsValidForRead", true);
            Set(entity, "Attributes", entity.Attributes.Concat(new[] { attribute }).ToArray());
            metadata.EntityMetadata["account"] = entity;
            var response = (InsertOptionValueResponse)orgAdminService.Execute(new InsertOptionValueRequest
            {
                EntityLogicalName = "account", AttributeLogicalName = "parity_choices", Label = new Label("Multi", 1033)
            });
            var choices = (MultiSelectPicklistAttributeMetadata)AccountAttribute("parity_choices");
            Assert.Equal(response.NewOptionValue, Assert.Single(choices.OptionSet.Options).Value);
        }
    }
}
