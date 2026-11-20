using System.Linq;
using System;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestInitializeFromRequestHandler : RequestHandlerTestBase
    {
        public TestInitializeFromRequestHandler(XrmMockupFixture fixture) : base(fixture)
        {
            AddTable("entitymap", Text("sourceentityname"), Text("targetentityname"));
            AddTable("attributemap", Lookup("entitymapid", "entitymap"), Text("sourceattributename"), Text("targetattributename"));
        }

        [Fact]
        public void HandlerIsRegisteredExactlyOnce()
        {
            Assert.Single(engine.RequestHandlers.Where(handler => handler.HandlesRequest("InitializeFrom")));
        }

        private Guid Mapping(string source, string target, string sourceAttribute, string targetAttribute)
        {
            var map = Create("entitymap", ("sourceentityname", source), ("targetentityname", target));
            Create("attributemap", ("entitymapid", new EntityReference("entitymap", map)), ("sourceattributename", sourceAttribute), ("targetattributename", targetAttribute));
            return map;
        }

        [Theory]
        [InlineData(TargetFieldType.All)]
        [InlineData(TargetFieldType.ValidForCreate)]
        [InlineData(TargetFieldType.ValidForUpdate)]
        [InlineData(TargetFieldType.ValidForRead)]
        public void InitializeFromMapsToUnsavedEntityAndSupportsEveryFieldType(TargetFieldType type)
        {
            Mapping("account", "contact", "name", "firstname");
            var source = new Account { Name = "Mapped name" }; source.Id = orgAdminService.Create(source);
            var response = (InitializeFromResponse)orgAdminService.Execute(new InitializeFromRequest { EntityMoniker = source.ToEntityReference(), TargetEntityName = "contact", TargetFieldType = type });
            Assert.Equal("contact", response.Entity.LogicalName);
            Assert.Equal(Guid.Empty, response.Entity.Id);
            Assert.Equal("Mapped name", response.Entity["firstname"]);
            Assert.Empty(All("contact").Entities);
        }

        [Fact]
        public void InitializeFromDoesNotCopyUnmappedOrUnavailableAttributes()
        {
            Mapping("account", "contact", "telephone1", "telephone1");
            var source = new Account { Name = "Not mapped" }; source.Id = orgAdminService.Create(source);
            var response = (InitializeFromResponse)orgAdminService.Execute(new InitializeFromRequest { EntityMoniker = source.ToEntityReference(), TargetEntityName = "contact", SkipParentalRelationshipMapping = true });
            Assert.Empty(response.Entity.Attributes);
        }

        [Fact]
        public void InitializeFromFiltersUnwritableMappedFields()
        {
            Mapping("account", "contact", "name", "firstname");
            var clone = CloneMetadata(metadata.EntityMetadata["contact"]);
            Set(clone.Attributes.First(a => a.LogicalName == "firstname"), "IsValidForCreate", false);
            metadata.EntityMetadata["contact"] = clone;
            var source = new Account { Name = "Mapped" }; source.Id = orgAdminService.Create(source);
            var response = (InitializeFromResponse)orgAdminService.Execute(new InitializeFromRequest { EntityMoniker = source.ToEntityReference(), TargetEntityName = "contact", TargetFieldType = TargetFieldType.ValidForCreate });
            Assert.False(response.Entity.Contains("firstname"));
        }

        [Fact]
        public void InitializeFromAddsParentalLookupUnlessExplicitlySkipped()
        {
            var source = new Account { Name = "Parent" }; source.Id = orgAdminService.Create(source);
            var request = new InitializeFromRequest { EntityMoniker = source.ToEntityReference(), TargetEntityName = "contact" };
            var result = ((InitializeFromResponse)orgAdminService.Execute(request)).Entity;
            Assert.Equal(source.Id, result.GetAttributeValue<EntityReference>("parentcustomerid").Id);
            request.SkipParentalRelationshipMapping = true;
            result = ((InitializeFromResponse)orgAdminService.Execute(request)).Entity;
            Assert.False(result.Contains("parentcustomerid"));
        }
    }
}
