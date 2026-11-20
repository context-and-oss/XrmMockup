using System.Runtime.InteropServices;

namespace XrmMockup.PermissionAnalyzer.Tool.Workspace;

/// <summary>
/// The .NET Framework reference assemblies for projects that target .NET Framework without referencing the
/// <c>Microsoft.NETFramework.ReferenceAssemblies</c> package, such as old-style plugin projects. Windows has them
/// from the Developer Packs; elsewhere they come from the <c>Microsoft.NETFramework.ReferenceAssemblies.net*</c>
/// packages in the NuGet cache, whose framework folders are linked into one root for <c>TargetFrameworkRootPath</c>.
/// </summary>
internal static class FrameworkReferences
{
    public const string Property = "TargetFrameworkRootPath";

    private const string PackagePrefix = "microsoft.netframework.referenceassemblies.net";

    /// <summary>
    /// The MSBuild properties that make the reference assemblies found, or none when they don't need to be set.
    /// </summary>
    public static Dictionary<string, string> Properties()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || Environment.GetEnvironmentVariable(Property) is { Length: > 0 }) return new Dictionary<string, string>();
        return Root(PackagesDirectory()) is { } root ? new Dictionary<string, string> { [Property] = root } : new Dictionary<string, string>();
    }

    /// <summary>
    /// A folder with every <c>.NETFramework/v*</c> version found in the packages under <paramref name="packages"/>,
    /// from the latest version of each package, or <c>null</c> when there are none.
    /// </summary>
    internal static string? Root(string packages, string? output = null)
    {
        if (!Directory.Exists(packages)) return null;

        var frameworks = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var directories = Directory.GetDirectories(packages)
            .Where(d => Path.GetFileName(d).StartsWith(PackagePrefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(d => d, StringComparer.Ordinal);
        foreach (var package in directories)
        {
            var latest = Directory.GetDirectories(package)
                .Select(d => (Path: d, Version: Version.TryParse(Path.GetFileName(d), out var version) ? version : null))
                .Where(v => v.Version is not null)
                .OrderByDescending(v => v.Version)
                .Select(v => Path.Combine(v.Path, "build", ".NETFramework"))
                .FirstOrDefault(Directory.Exists);
            if (latest is null) continue;

            foreach (var framework in Directory.GetDirectories(latest))
            {
                frameworks.TryAdd(Path.GetFileName(framework), framework);
            }
        }
        if (frameworks.Count == 0) return null;

        var root = output ?? Path.Combine(Path.GetTempPath(), "xrmmockup-permissions", "framework-references");
        var target = Path.Combine(root, ".NETFramework");
        try
        {
            Directory.CreateDirectory(target);
            foreach (var (name, path) in frameworks)
            {
                var link = Path.Combine(target, name);
                var info = new DirectoryInfo(link);
                if (info is { Exists: true, LinkTarget: { } existing } && existing == path) continue;
                // File.Delete also removes a link whose target is gone, such as a package version cleared from the cache
                if (info.LinkTarget is not null) File.Delete(link);
                else if (info.Exists) Directory.Delete(link);
                Directory.CreateSymbolicLink(link, path);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Projects then fail to load with a message that says what is missing
            return null;
        }
        return root;
    }

    private static string PackagesDirectory() =>
        Environment.GetEnvironmentVariable("NUGET_PACKAGES") is { Length: > 0 } packages
            ? packages
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
}
