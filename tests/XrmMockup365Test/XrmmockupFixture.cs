using DG.Some.Namespace;
using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk;
using XrmPluginCore;
using System;
using TestPluginAssembly365.Plugins.LegacyDaxif;
using TestPluginAssembly365.Plugins.ServiceBased;

public class XrmMockupFixture : IDisposable
{
    // Shared settings instance to ensure metadata cache hits
    private static XrmMockupSettings _sharedSettings;
    private static readonly object _settingsLock = new object();

    public XrmMockupSettings Settings => _sharedSettings;

    public XrmMockupFixture()
    {
        lock (_settingsLock)
        {
            if (_sharedSettings == null)
            {
                _sharedSettings = new XrmMockupSettings
                {
                    BasePluginTypes = new Type[] { typeof(Plugin), typeof(PluginNonDaxif), typeof(LegacyPlugin), typeof(DIPlugin) },
                    BaseCustomApiTypes = new[] { new Tuple<string, Type>("dg", typeof(Plugin)), new Tuple<string, Type>("dg", typeof(LegacyCustomApi)) },
                    CodeActivityInstanceTypes = new Type[] { typeof(AccountWorkflowActivity) },
                    EnableProxyTypes = true,
                    IncludeAllWorkflows = false,
                    ExceptionFreeRequests = new string[] { "TestWrongRequest" },
                    MetadataDirectoryPath = GetMetadataPath(),
                    IPluginMetadata = metaPlugins
                };
            }
        }
    }

    public void Dispose()
    {
    }

    private static string GetMetadataPath()
    {
        // Find the test project directory by looking for the Metadata folder
        var currentDir = System.IO.Directory.GetCurrentDirectory();
        var metadataPath = System.IO.Path.Combine(currentDir, "Metadata");
        
        if (System.IO.Directory.Exists(metadataPath))
        {
            return metadataPath;
        }
        
        // If not found in current directory, try relative paths from bin output
        var testProjectPaths = new[]
        {
            System.IO.Path.Combine(currentDir, "..", "..", "..", "Metadata"),
            System.IO.Path.Combine(currentDir, "Metadata"),
            "Metadata"
        };
        
        foreach (var path in testProjectPaths)
        {
            var fullPath = System.IO.Path.GetFullPath(path);
            if (System.IO.Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }
        
        throw new System.IO.DirectoryNotFoundException($"Could not find Metadata directory. Searched in: {currentDir}");
    }

    private static MetaPlugin[] metaPlugins = new MetaPlugin[]
    {
        new MetaPlugin()
        {
            PluginTypeAssemblyName = "TestPluginAssembly365",
            AssemblyName = "DG.Some.Namespace.ContactIPluginDirectPreOp",
            MessageName = "Create",
            PrimaryEntity = "contact",
            Rank = 10,
            Stage = 20 //pre op as it only updates a field name
        },
        new MetaPlugin()
        {
            PluginTypeAssemblyName = "TestPluginAssembly365",
            AssemblyName = "DG.Some.Namespace.ContactIPluginDirectPostOp",
            MessageName = "Create",
            PrimaryEntity = "contact",
            Rank = 10,
            Stage = 40 //pre op as it only updates a field name
        },
        new MetaPlugin()
        {
            PluginTypeAssemblyName = "TestPluginAssembly365",
            AssemblyName = "DG.Some.Namespace.AccountPluginExecutionContext7PreOp",
            MessageName = "Create",
            PrimaryEntity = "account",
            Rank = 10,
            Stage = 20
        },
        // Non-DAXIF plugins that, in the original environment, were registered via a separate plugin
        // solution captured in Metadata.xml. That solution isn't part of the leaned environment, so we
        // register their steps here instead (TestUpdateBase / TestImages depend on them).
        new MetaPlugin()
        {
            PluginTypeAssemblyName = "TestPluginAssembly365",
            AssemblyName = "DG.Delegate.TSTOnboarding.Plugins.AccountCurrencyBase",
            MessageName = "Update",
            PrimaryEntity = "account",
            Rank = 10,
            Stage = 40 // post-operation; appends "UpdateBase" to the name
        },
        new MetaPlugin()
        {
            PluginTypeAssemblyName = "TestPluginAssembly365",
            AssemblyName = "DG.Delegate.TSTOnboarding.Plugins.AccountBothImagePlugin",
            MessageName = "Update",
            PrimaryEntity = "account",
            Rank = 10,
            Stage = 40, // post-operation; requires both a pre- and post-image
            Images = new System.Collections.Generic.List<MetaImage>
            {
                // ImageType.Both (2) populates both PreEntityImages and PostEntityImages.
                new MetaImage { Name = "image", EntityAlias = "image", ImageType = 2 }
            }
        },
        // BusTicketSync (non-DAXIF) fires on Associate/Disassociate of the ctx_parent_child N:N and
        // stamps ctx_parent.ctx_Amount (25/26). Associate/Disassociate steps must be registered on
        // AnyEntity (empty PrimaryEntity) — the plugin itself filters on the relationship schema name.
        new MetaPlugin()
        {
            PluginTypeAssemblyName = "TestPluginAssembly365",
            AssemblyName = "DG.NotAKeyWord.SSPOnBoarding.Plugins.BusTicketSync",
            MessageName = "Associate",
            PrimaryEntity = "",
            Rank = 10,
            Stage = 40
        },
        new MetaPlugin()
        {
            PluginTypeAssemblyName = "TestPluginAssembly365",
            AssemblyName = "DG.NotAKeyWord.SSPOnBoarding.Plugins.BusTicketSync",
            MessageName = "Disassociate",
            PrimaryEntity = "",
            Rank = 10,
            Stage = 40
        },
        // Late-bound re-creation of the old IncidentDeleteAllRelatedResolutionsOnClose plugin: on an
        // incident statecode change (Active -> not-Active) for the test incident, deletes related
        // incidentresolution records. Needs pre+post images on statecode (and title for the gate).
        new MetaPlugin()
        {
            PluginTypeAssemblyName = "TestPluginAssembly365",
            AssemblyName = "DG.Delegate.TSTOnboarding.Plugins.IncidentDeleteResolutionsOnClose",
            MessageName = "Update",
            PrimaryEntity = "incident",
            Rank = 10,
            Stage = 40,
            FilteredAttributes = "statecode",
            Images = new System.Collections.Generic.List<MetaImage>
            {
                new MetaImage { Name = "PreImage", EntityAlias = "PreImage", ImageType = 0, Attributes = "statecode,title" }, // PreImage
                new MetaImage { Name = "PostImage", EntityAlias = "PostImage", ImageType = 1, Attributes = "statecode" },     // PostImage
            }
        },
        // Late-bound re-creation of the old OpportunityWinLose plugin: stamps the opportunity's
        // description with "SetFromWinLose" on the Win and Lose messages (post-operation).
        new MetaPlugin()
        {
            PluginTypeAssemblyName = "TestPluginAssembly365",
            AssemblyName = "DG.Delegate.TSTOnboarding.Plugins.OpportunityWinLoseDescription",
            MessageName = "Win",
            PrimaryEntity = "opportunity",
            Rank = 10,
            Stage = 40
        },
        new MetaPlugin()
        {
            PluginTypeAssemblyName = "TestPluginAssembly365",
            AssemblyName = "DG.Delegate.TSTOnboarding.Plugins.OpportunityWinLoseDescription",
            MessageName = "Lose",
            PrimaryEntity = "opportunity",
            Rank = 10,
            Stage = 40
        }
    };
}