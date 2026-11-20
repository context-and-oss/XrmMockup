using System;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestReferences : UnitTestBase
    {
        public TestReferences(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestCreateCircularReferenceSelf()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var id = Guid.NewGuid();
                var acc = new Account(id)
                {
                    ParentAccountId = new EntityReference(Account.EntityLogicalName, id)
                };
                try
                {
                    orgAdminUIService.Create(acc);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }
            }
        }


        [Fact]
        public void TestUpdateCircularReference()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var id = Guid.NewGuid();
                var acc = new Account(id);
                orgAdminUIService.Create(acc);

                acc.ParentAccountId = new EntityReference(Account.EntityLogicalName, id);
                try
                {
                    orgAdminUIService.Update(acc);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }

                acc.ParentAccountId = null;
                orgAdminUIService.Update(acc);
            }
        }
    }

}
