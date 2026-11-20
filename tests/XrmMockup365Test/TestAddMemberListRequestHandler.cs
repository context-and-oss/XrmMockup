using System.Linq;
using System.ServiceModel;
using System;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestAddMemberListRequestHandler : ListMembershipRequestHandlerTestBase
    {
        public TestAddMemberListRequestHandler(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void HandlerIsRegisteredExactlyOnce()
        {
            Assert.Single(engine.RequestHandlers.Where(handler => handler.HandlesRequest("AddMemberList")));
        }

        [Fact]
        public void AddMemberReturnsMembershipIdAndDoesNotDuplicate()
        {
            var list = List(); var contact = Contact();
            var response = (AddMemberListResponse)orgAdminService.Execute(new AddMemberListRequest { ListId = list, EntityId = contact });
            Assert.NotEqual(Guid.Empty, response.Id);
            Assert.Equal(contact, Read("listmember", response.Id).GetAttributeValue<EntityReference>("entityid").Id);
            Assert.Equal(response.Id, ((AddMemberListResponse)orgAdminService.Execute(new AddMemberListRequest { ListId = list, EntityId = contact })).Id);
            Assert.Single(All("listmember").Entities);
        }

        [Fact]
        public void MarketingListRejectsDynamicListsAndWrongMemberType()
        {
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new AddMemberListRequest { ListId = List(true), EntityId = Contact() }));
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new AddMemberListRequest { ListId = List(), EntityId = orgAdminService.Create(new Account()) }));
            Assert.Empty(All("listmember").Entities);
        }

        [Fact]
        public void MembershipSupportsUniqueidentifierIntersectColumns()
        {
            var entity = CloneMetadata(metadata.EntityMetadata["listmember"]);
            var attributes = entity.Attributes.Where(a => a.LogicalName != "entityid").Concat(new[] { Attribute("accountid", "entityid") }).ToArray();
            Set(entity, "Attributes", attributes);
            metadata.EntityMetadata["listmember"] = entity;
            var member = Contact();
            var response = (AddMemberListResponse)orgAdminService.Execute(new AddMemberListRequest { ListId = List(), EntityId = member });
            Assert.Equal(member, Read("listmember", response.Id).GetAttributeValue<Guid>("entityid"));
        }

        [Theory]
        [InlineData(1, "account")]
        [InlineData(2, "contact")]
        [InlineData(4, "lead")]
        public void MembershipSupportsEveryAllowedEntityType(int code, string name)
        {
            var list = Create("list", ("createdfromcode", new OptionSetValue(code)));
            var member = Create(name);
            orgAdminService.Execute(new AddMemberListRequest { ListId = list, EntityId = member });
            Assert.Single(All("listmember").Entities);
        }

        [Fact]
        public void LockedMarketingListCannotBeChanged()
        {
            var list = List();
            orgAdminService.Update(new Entity("list", list) { ["lockstatus"] = true });
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new AddMemberListRequest { ListId = list, EntityId = Contact() }));
            Assert.Empty(All("listmember").Entities);
        }
    }
}
