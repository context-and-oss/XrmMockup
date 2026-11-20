extern alias XrmMockupLib;

using XrmMockupLib::DG.Tools.XrmMockup;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Fixtures;

public sealed class XrmMockupFactory
{
    private static readonly XrmMockupSettings? sharedSettings;
    private static readonly Lock settingsLock = new();

    static XrmMockupFactory()
    {
        lock (settingsLock)
        {
            sharedSettings ??= new XrmMockupSettings
            {
                BasePluginTypes = [],
                CodeActivityInstanceTypes = [],
                EnableProxyTypes = true,
                IncludeAllWorkflows = true,
                MetadataDirectoryPath = GetMetadataPath()
            };
        }
    }

    public static XrmMockup365 CreateMockup() => XrmMockup365.GetInstance(sharedSettings);

    private static string GetMetadataPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var paths = new[]
        {
            // Source directory relative path (when running from bin/Debug/net10.0)
            Path.Combine(currentDir, "..", "..", "..", "TestMetadata"),
            // Output directory (if copied)
            Path.Combine(currentDir, "TestMetadata")
        };
        foreach (var p in paths)
        {
            var full = Path.GetFullPath(p);
            if (Directory.Exists(full))
                return full;
        }

        throw new DirectoryNotFoundException($"TestMetadata directory not found. Searched: {string.Join(", ", paths.Select(Path.GetFullPath))}");
    }
}
