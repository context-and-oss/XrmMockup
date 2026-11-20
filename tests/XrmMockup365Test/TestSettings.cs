using System;
using Microsoft.Xrm.Sdk;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestSettings : UnitTestBase
    {
        public TestSettings(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestNoExceptionRequest()
        {
            using (var context = new Xrm(orgAdminUIService))
            {

                var req = new OrganizationRequest("WrongRequestThatFails");
                try
                {
                    orgAdminUIService.Execute(req);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<NotImplementedException>(e);
                }

                req = new OrganizationRequest("TestWrongRequest");
                orgAdminUIService.Execute(req);
            }
        }
    }
}
