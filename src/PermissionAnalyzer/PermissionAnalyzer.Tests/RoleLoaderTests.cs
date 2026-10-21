using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Metadata;

namespace XrmMockup.PermissionAnalyzer.Tests;

public class RoleLoaderTests
{
    private static string MetadataDirectory([System.Runtime.CompilerServices.CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "..", "tests", "XrmMockup365Test", "Metadata"));

    [Fact]
    public void LoadsTheTestSecurityRolesSortedByName()
    {
        var roles = RoleLoader.Load(MetadataDirectory());

        Assert.NotEmpty(roles);
        Assert.Equal(roles.Select(r => r.Name).OrderBy(n => n, StringComparer.Ordinal), roles.Select(r => r.Name));
        Assert.All(roles, r => Assert.NotEmpty(r.Privileges));
        Assert.Contains(roles, r => r.Privileges.Values.Any(p => p.ContainsKey(Privilege.Read)));
    }

    [Fact]
    public void MissingFolderGivesNoRoles()
    {
        Assert.Empty(RoleLoader.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
    }
}
