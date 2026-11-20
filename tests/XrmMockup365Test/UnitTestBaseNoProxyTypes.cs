using System;
using Microsoft.Xrm.Sdk;
using DG.Tools.XrmMockup;
using Xunit;

namespace DG.XrmMockupTest
{
    public class UnitTestBaseNoProxyTypes : IClassFixture<XrmMockupFixtureNoProxyTypes>
    {
        protected IOrganizationService orgAdminUIService;
        protected IOrganizationService orgAdminService;
        protected IOrganizationService orgGodService;

        protected XrmMockup365 crm;

        public UnitTestBaseNoProxyTypes(XrmMockupFixtureNoProxyTypes fixture)
        {
            // Each test gets its own completely fresh instance
            crm = XrmMockup365.GetInstance(fixture.Settings);
            orgAdminUIService = crm.GetAdminService(new MockupServiceSettings(true, false, MockupServiceSettings.Role.UI));
            orgGodService = crm.GetAdminService(new MockupServiceSettings(false, true, MockupServiceSettings.Role.SDK));
            orgAdminService = crm.GetAdminService();
        }

        public void Dispose()
        {
            // No need to reset environment since each test has its own instance
            // The instance will be garbage collected automatically
        }
    }
}
