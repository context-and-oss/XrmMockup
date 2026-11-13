using System.Linq;
using System.ServiceModel;
using System;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestRetrieveSharedPrincipalsAndAccessRequestHandler : RequestHandlerTestBase
    {
        public TestRetrieveSharedPrincipalsAndAccessRequestHandler(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void HandlerIsRegisteredExactlyOnce()
        {
            Assert.Single(engine.RequestHandlers.Where(handler => handler.HandlesRequest("RetrieveSharedPrincipalsAndAccess")));
        }

        [Fact]
        public void SharedPrincipalsOnlyIncludeExplicitSharesAndTrackModifyRevoke()
        {
            var account = new Account(); account.Id = orgAdminService.Create(account);
            var request = new RetrieveSharedPrincipalsAndAccessRequest { Target = account.ToEntityReference() };
            Assert.Empty(((RetrieveSharedPrincipalsAndAccessResponse)orgAdminService.Execute(request)).PrincipalAccesses);
            orgAdminService.Execute(new GrantAccessRequest { Target = account.ToEntityReference(), PrincipalAccess = new PrincipalAccess { Principal = testUser1.ToEntityReference(), AccessMask = AccessRights.ReadAccess } });
            var shares = ((RetrieveSharedPrincipalsAndAccessResponse)orgAdminService.Execute(request)).PrincipalAccesses;
            Assert.Single(shares);
            Assert.Equal(testUser1.Id, shares[0].Principal.Id);
            Assert.Equal(AccessRights.ReadAccess, shares[0].AccessMask);
            orgAdminService.Execute(new ModifyAccessRequest { Target = account.ToEntityReference(), PrincipalAccess = new PrincipalAccess { Principal = testUser1.ToEntityReference(), AccessMask = AccessRights.ReadAccess | AccessRights.WriteAccess } });
            Assert.Equal(AccessRights.ReadAccess | AccessRights.WriteAccess, ((RetrieveSharedPrincipalsAndAccessResponse)orgAdminService.Execute(request)).PrincipalAccesses[0].AccessMask);
            orgAdminService.Execute(new RevokeAccessRequest { Target = account.ToEntityReference(), Revokee = testUser1.ToEntityReference() });
            Assert.Empty(((RetrieveSharedPrincipalsAndAccessResponse)orgAdminService.Execute(request)).PrincipalAccesses);
        }

        [Fact]
        public void SharedPrincipalsRejectMissingTargetAndUnknownRecord()
        {
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new RetrieveSharedPrincipalsAndAccessRequest()));
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new RetrieveSharedPrincipalsAndAccessRequest { Target = new EntityReference("account", Guid.NewGuid()) }));
        }
    }
}
