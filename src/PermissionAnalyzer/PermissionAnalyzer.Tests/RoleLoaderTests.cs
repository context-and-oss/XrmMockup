using System.Runtime.Serialization;
using DG.Tools.XrmMockup;
using Microsoft.Crm.Sdk.Messages;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Metadata;
using CrmDepth = Microsoft.Crm.Sdk.Messages.PrivilegeDepth;
using RolePrivilege = DG.Tools.XrmMockup.RolePrivilege;
using PrivilegeDepth = XrmMockup.PermissionAnalyzer.Core.Model.PrivilegeDepth;

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

    [Fact]
    public void GlobalPrivilegesAreConvertedByName()
    {
        var role = RoleLoader.Convert(new SecurityRole
        {
            Name = "Integration",
            Privileges = [],
            GlobalPrivileges = new() { ["prvActOnBehalfOfAnotherUser"] = new RolePrivilege { PrivilegeDepth = CrmDepth.Global } },
        });

        Assert.Equal(PrivilegeDepth.Organization, role.GlobalPrivileges!["PRVACTONBEHALFOFANOTHERUSER"]);
    }

    [Fact]
    public void RoleFromOlderMetadataHasNoGlobalPrivileges()
    {
        Assert.Null(RoleLoader.Convert(new SecurityRole { Name = "Old", Privileges = [] }).GlobalPrivileges);
    }

    [Fact]
    public void PrivilegeCatalogIsLoadedFromTheMetadataFolder()
    {
        var directory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            using (var stream = File.Create(Path.Combine(directory, PrivilegeCatalogLoader.FileName)))
            {
                new DataContractSerializer(typeof(PrivilegeCatalog)).WriteObject(stream, new PrivilegeCatalog
                {
                    Privileges =
                    [
                        new MetaPrivilege { Name = "prvReadctx_Order", AccessRight = AccessRights.ReadAccess, Tables = ["ctx_order"] },
                        new MetaPrivilege { Name = "prvActOnBehalfOfAnotherUser", AccessRight = AccessRights.None, Tables = [] },
                    ],
                    CustomApis = [new MetaCustomApi { UniqueName = "ctx_Ping" }],
                });
            }

            var (privileges, customApis) = PrivilegeCatalogLoader.Load(directory)!.Value;

            Assert.Equal(
                ["prvActOnBehalfOfAnotherUser  ", "prvReadctx_Order Read ctx_order"],
                privileges.Select(p => $"{p.Name} {p.Privilege} {string.Join(",", p.Tables)}"));
            Assert.Equal(new CustomApiDefinition("ctx_Ping", null), Assert.Single(customApis));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void MissingPrivilegeCatalogGivesNull()
    {
        Assert.Null(PrivilegeCatalogLoader.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
    }
}
