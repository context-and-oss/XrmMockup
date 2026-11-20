using System.Linq;
using System.ServiceModel;
using System;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestAddListMembersListRequestHandler : ListMembershipRequestHandlerTestBase
    {
        public TestAddListMembersListRequestHandler(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void HandlerIsRegisteredExactlyOnce()
        {
            Assert.Single(engine.RequestHandlers.Where(handler => handler.HandlesRequest("AddListMembersList")));
        }

        [Fact]
        public void BulkMembershipDeduplicatesAndValidatesAllMembersFirst()
        {
            var list = List(); var first = Contact(); var second = Contact();
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new AddListMembersListRequest { ListId = list, MemberIds = new[] { first, Guid.NewGuid() } }));
            Assert.Empty(All("listmember").Entities);
            orgAdminService.Execute(new AddListMembersListRequest { ListId = list, MemberIds = new[] { first, first, second } });
            Assert.Equal(2, All("listmember").Entities.Count);
        }

        [Fact]
        public void BulkMembershipRequiresMemberIds()
        {
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new AddListMembersListRequest { ListId = List() }));
            Assert.Empty(All("listmember").Entities);
        }
    }
}
