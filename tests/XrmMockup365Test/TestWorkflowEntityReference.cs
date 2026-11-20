using System;
using System.Collections.Generic;
using System.IO;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using WorkflowExecuter;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestWorkflowEntityReference
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SettingNullOrMissingLookupClearsExistingValue(bool initializeVariable)
        {
            var account = new Account { PrimaryContactId = new EntityReference("contact", Guid.NewGuid()) };
            var variables = new Dictionary<string, object> { ["account"] = account };
            if (initializeVariable)
            {
                variables["value"] = null;
            }
            var node = new SetEntityProperty("primarycontactid", "account", "account", "value", "EntityReference");

            node.Execute(ref variables, TimeSpan.Zero, null, null, null);

            Assert.True(account.Contains("primarycontactid"));
            Assert.Null(account.PrimaryContactId);
            Assert.Null(variables["value"]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GettingNullOrMissingLookupReturnsNull(bool includeAttribute)
        {
            var account = new Account();
            if (includeAttribute)
            {
                account.PrimaryContactId = null;
            }
            var variables = new Dictionary<string, object> { ["account"] = account, ["value"] = "stale value" };
            var node = new GetEntityProperty("primarycontactid", "account", "account", "value", "EntityReference");

            node.Execute(ref variables, TimeSpan.Zero, null, null, null);

            Assert.Null(variables["value"]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EntityReferenceIsPreserved(bool read)
        {
            var reference = new EntityReference("contact", Guid.NewGuid()) { Name = "Synthetic contact" };
            var account = new Account { PrimaryContactId = reference };
            var variables = new Dictionary<string, object> { ["account"] = account, ["value"] = reference };

            ExecuteLookupNode(read, "primarycontactid", ref variables);

            Assert.Same(reference, variables["value"]);
            Assert.Same(reference, account.PrimaryContactId);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GuidIsConvertedToEntityReference(bool read)
        {
            var id = Guid.NewGuid();
            var account = new Account { Id = id };
            var variables = new Dictionary<string, object> { ["account"] = account, ["value"] = id };

            ExecuteLookupNode(read, read ? "accountid" : "parentaccountid", ref variables);

            var reference = Assert.IsType<EntityReference>(read ? variables["value"] : account.ParentAccountId);
            Assert.Equal("account", reference.LogicalName);
            Assert.Equal(id, reference.Id);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void InvalidNonNullLookupStillReportsActualType(bool read)
        {
            // Late-bound attribute value intentionally exercises an invalid runtime type.
            var account = new Account();
            account["primarycontactid"] = "not a lookup";
            var variables = new Dictionary<string, object> { ["account"] = account, ["value"] = "not a lookup" };

            var exception = Assert.Throws<InvalidCastException>(() => ExecuteLookupNode(read, "primarycontactid", ref variables));

            Assert.Equal("Cannot convert String to EntityReference", exception.Message);
        }

        private static void ExecuteLookupNode(bool read, string attribute, ref Dictionary<string, object> variables)
        {
            IWorkflowNode node = read
                ? (IWorkflowNode)new GetEntityProperty(attribute, "account", "account", "value", "EntityReference")
                : new SetEntityProperty(attribute, "account", "account", "value", "EntityReference");
            node.Execute(ref variables, TimeSpan.Zero, null, null, null);
        }
    }

    public class TestWorkflowNullableLookup : UnitTestBase
    {
        public TestWorkflowNullableLookup(XrmMockupFixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("missing")]
        [InlineData("null")]
        [InlineData("reference")]
        public void SyntheticWorkflowCopiesOptionalLookupAndPersistsUpdate(string sourceValue)
        {
            // This workflow is authored from scratch; no customer workflow or metadata is used.
            var workflow = new Entity("workflow")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Synthetic optional lookup assignment",
                ["primaryentity"] = Account.EntityLogicalName,
                ["ownerid"] = new EntityReference("systemuser", Guid.NewGuid()),
                ["xaml"] = File.ReadAllText(Path.Combine("..", "..", "..", "Metadata", "OtherWorkflows", "NullableLookupAssignment.xaml")),
            };
            var tree = WorkflowConstructor.Parse(workflow, null, WorkflowConstructor.ParseAs.Workflow);
            var originalContact = new Contact { LastName = "Original synthetic contact" };
            originalContact.Id = orgAdminService.Create(originalContact);
            var account = new Account { Name = "Synthetic lookup target", PrimaryContactId = originalContact.ToEntityReference() };
            account.Id = orgAdminService.Create(account);
            var input = new Account { Id = account.Id };
            EntityReference expected = null;
            if (sourceValue == "null")
            {
                input.PrimaryContactId = null;
            }
            else if (sourceValue == "reference")
            {
                var replacement = new Contact { LastName = "Replacement synthetic contact" };
                replacement.Id = orgAdminService.Create(replacement);
                expected = replacement.ToEntityReference();
                input.PrimaryContactId = expected;
            }

            tree.Execute(input, TimeSpan.Zero, orgAdminService, null, null);

            var update = Assert.IsType<Entity>(tree.Variables["CreatedEntities(\"primaryEntity#Temp\")"]);
            Assert.True(update.Contains("primarycontactid"));
            Assert.Equal(expected, update.GetAttributeValue<EntityReference>("primarycontactid"));
            var retrieved = orgAdminService.Retrieve(Account.EntityLogicalName, account.Id, new ColumnSet("primarycontactid"));
            Assert.Equal(expected, retrieved.GetAttributeValue<EntityReference>("primarycontactid"));
        }
    }
}
