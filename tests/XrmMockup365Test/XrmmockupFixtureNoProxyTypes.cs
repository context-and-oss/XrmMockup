using DG.Some.Namespace;
using DG.Tools.XrmMockup;
using XrmPluginCore;
using System;

public class XrmMockupFixtureNoProxyTypes : IDisposable
{
    // Shared settings instances to ensure metadata cache hits
    private static XrmMockupSettings _sharedSettings;
    private static readonly object _settingsLock = new object();
    
    public XrmMockupSettings Settings => _sharedSettings;

    public XrmMockupFixtureNoProxyTypes()
    {
        lock (_settingsLock)
        {
            if (_sharedSettings == null)
            {
                _sharedSettings = new XrmMockupSettings
                {
                    BasePluginTypes = new Type[] { typeof(Plugin), typeof(PluginNonDaxif) },
                    CodeActivityInstanceTypes = new Type[] { typeof(AccountWorkflowActivity) },
                    EnableProxyTypes = false,
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
        }
    };
}