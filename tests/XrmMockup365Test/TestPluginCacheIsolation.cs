using DG.Some.Namespace;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace DG.XrmMockupTest
{
    // Regression guard for the static plugin-registration cache. Two XrmMockupSettings with the SAME
    // BasePluginTypes but DIFFERENT IPluginMetadata must NOT share a cache entry — otherwise the plugin
    // registrations leak between them depending on which instance is constructed first, which surfaces
    // as order-dependent failures (passing locally, failing in CI). See GeneratePluginCacheKey.
    public class TestPluginCacheIsolation : IClassFixture<XrmMockupFixture>
    {
        private readonly XrmMockupSettings _baseSettings;
        public TestPluginCacheIsolation(XrmMockupFixture fixture) { _baseSettings = fixture.Settings; }

        private XrmMockupSettings With(MetaPlugin[] ipm) => new XrmMockupSettings
        {
            BasePluginTypes = _baseSettings.BasePluginTypes,
            BaseCustomApiTypes = _baseSettings.BaseCustomApiTypes,
            CodeActivityInstanceTypes = _baseSettings.CodeActivityInstanceTypes,
            EnableProxyTypes = _baseSettings.EnableProxyTypes,
            IncludeAllWorkflows = _baseSettings.IncludeAllWorkflows,
            ExceptionFreeRequests = _baseSettings.ExceptionFreeRequests,
            MetadataDirectoryPath = _baseSettings.MetadataDirectoryPath,
            IPluginMetadata = ipm,
        };

        [Fact]
        public void SameBaseTypesDifferentPluginMetadataDoNotShareCache()
        {
            // Same base plugin types; one settings registers ContactIPluginDirectPreOp, the other none.
            var withoutPlugin = With(new MetaPlugin[0]);
            var withPlugin = With(new[]
            {
                new MetaPlugin
                {
                    PluginTypeAssemblyName = "TestPluginAssembly365",
                    AssemblyName = "DG.Some.Namespace.ContactIPluginDirectPreOp",
                    MessageName = "Create",
                    PrimaryEntity = "contact",
                    Rank = 10,
                    Stage = 20
                }
            });

            // Construct the no-plugin instance FIRST: pre-fix this poisoned the shared (base-types-only)
            // cache key, so the with-plugin instance reused it and the plugin never fired.
            var crmWithout = XrmMockup365.GetInstance(withoutPlugin);
            var crmWith = XrmMockup365.GetInstance(withPlugin);

            var svcWithout = crmWithout.GetAdminService();
            var idA = svcWithout.Create(new Contact { FirstName = "ChangeMePlease" });
            var a = (Contact)svcWithout.Retrieve(Contact.EntityLogicalName, idA, new ColumnSet("firstname"));
            Assert.Equal("ChangeMePlease", a.FirstName); // no plugin registered -> unchanged

            var svcWith = crmWith.GetAdminService();
            var idB = svcWith.Create(new Contact { FirstName = "ChangeMePlease" });
            var b = (Contact)svcWith.Retrieve(Contact.EntityLogicalName, idB, new ColumnSet("firstname"));
            Assert.Equal("NameIsModified", b.FirstName); // plugin registered -> modified
        }
    }
}
