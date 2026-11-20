using System;
using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestRetrieveVersion : UnitTestBase
    {
        public TestRetrieveVersion(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestRetrieveVersionAll()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var version = (RetrieveVersionResponse)orgAdminUIService.Execute(new RetrieveVersionRequest());
                Assert.True(8 <= Int32.Parse(version.Version.Substring(0, 1)));
            }
        }
    }

}