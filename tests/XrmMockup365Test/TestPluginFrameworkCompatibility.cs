using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Versioning;
using DG.Some.Namespace;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk.Query;
using TestPluginAssembly462;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestPluginFrameworkCompatibility : IClassFixture<XrmMockupFixture>
    {
        private readonly XrmMockupSettings _baseSettings;

        public TestPluginFrameworkCompatibility(XrmMockupFixture fixture)
        {
            _baseSettings = fixture.Settings;
        }

        [Fact]
        public void PluginAssembliesTargetExpectedFrameworks()
        {
            Assert.Equal(".NETFramework,Version=v4.6.2",
                typeof(ContactPreOperationPlugin).Assembly.GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName);
#if DATAVERSE_SERVICE_CLIENT
            const string currentFramework = ".NETCoreApp,Version=v10.0";
#else
            const string currentFramework = ".NETFramework,Version=v4.8";
#endif
            Assert.Equal(currentFramework,
                typeof(ContactIPluginDirectPreOp).Assembly.GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName);
            Assert.Equal(currentFramework,
                typeof(XrmMockup365).Assembly.GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Net462PluginExecutesAlongsideCurrentFrameworkPlugin(bool registerLegacyPlugin)
        {
            var currentPluginType = typeof(ContactIPluginDirectPreOp);
            var legacyPluginType = typeof(ContactPreOperationPlugin);
            var plugins = new List<MetaPlugin> { CreateRegistration(currentPluginType, 10) };
            if (registerLegacyPlugin)
            {
                plugins.Add(CreateRegistration(legacyPluginType, 20));
            }

            // Keep registrations local to this instance; do not alter the shared fixture.
            var crm = XrmMockup365.GetInstance(new XrmMockupSettings
            {
                MetadataDirectoryPath = _baseSettings.MetadataDirectoryPath,
                EnableProxyTypes = true,
                IncludeAllWorkflows = false,
                BasePluginTypes = new[] { currentPluginType, legacyPluginType },
                IPluginMetadata = plugins.ToArray()
            });
            var service = crm.GetAdminService();
            var id = service.Create(new Contact { FirstName = "ChangeMePlease", LastName = "Original" });
            var contact = service.Retrieve(Contact.EntityLogicalName, id,
                new ColumnSet("firstname", "lastname")).ToEntity<Contact>();

            Assert.Equal("NameIsModified", contact.FirstName);
            Assert.Equal(registerLegacyPlugin ? "ModifiedByNet462Plugin" : "Original", contact.LastName);
        }

        private static MetaPlugin CreateRegistration(Type pluginType, int rank)
        {
            return new MetaPlugin
            {
                PluginTypeAssemblyName = pluginType.Assembly.GetName().Name,
                AssemblyName = pluginType.FullName,
                MessageName = "Create",
                PrimaryEntity = Contact.EntityLogicalName,
                Rank = rank,
                Stage = 20
            };
        }
    }
}
