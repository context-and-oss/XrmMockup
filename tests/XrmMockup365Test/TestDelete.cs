using System;
using System.ServiceModel;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestDelete : UnitTestBase
    {
        public TestDelete(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void DeleteTest()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var guid = orgAdminUIService.Create(new Contact());

                var firstRetrieve = orgAdminUIService.Retrieve<Contact>(guid, null);
                Assert.NotNull(firstRetrieve);

                orgAdminUIService.Delete(Contact.EntityLogicalName, guid);

                try
                {
                    orgAdminUIService.Retrieve<Contact>(guid, null);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }
            }
        }

        [Fact]
        public void TestDeleteNonExistingEntity()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                try
                {
                    orgAdminUIService.Delete(Contact.EntityLogicalName, Guid.NewGuid());
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }
            }
        }
    }
}
