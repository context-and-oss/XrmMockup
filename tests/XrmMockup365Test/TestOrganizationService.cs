using System;
using System.ServiceModel;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestOrganizationService : UnitTestBase
    {
        public TestOrganizationService(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestOrgSvcWithNonExistentUser()
        {
            try
            {
                crm.CreateOrganizationService(Guid.NewGuid());
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }
    }
}
