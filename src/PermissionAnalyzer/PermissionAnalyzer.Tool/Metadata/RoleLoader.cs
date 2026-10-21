using System.Runtime.Serialization;
using DG.Tools.XrmMockup;
using Microsoft.Crm.Sdk.Messages;
using XrmMockup.PermissionAnalyzer.Core.Model;
using CrmDepth = Microsoft.Crm.Sdk.Messages.PrivilegeDepth;
using PrivilegeDepth = XrmMockup.PermissionAnalyzer.Core.Model.PrivilegeDepth;

namespace XrmMockup.PermissionAnalyzer.Tool.Metadata;

/// <summary>
/// Reads the security roles in <c>Metadata/SecurityRoles</c>, written by the metadata tool, the same way XrmMockup
/// does.
/// </summary>
internal static class RoleLoader
{
    public const string SecurityRolesFolder = "SecurityRoles";

    /// <summary>
    /// Every role in the metadata folder, sorted by name. Empty when the folder has no security roles.
    /// </summary>
    public static IReadOnlyList<RoleDefinition> Load(string metadataDirectory)
    {
        var directory = Path.Combine(metadataDirectory, SecurityRolesFolder);
        if (!Directory.Exists(directory)) return [];

        var serializer = new DataContractSerializer(typeof(SecurityRole));
        return Directory.GetFiles(directory, "*.xml")
            .Select(file =>
            {
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                var role = serializer.ReadObject(stream) as SecurityRole
                    ?? throw new ToolException($"Not a security role: {file}");
                return Convert(role);
            })
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .ToList();
    }

    internal static RoleDefinition Convert(SecurityRole role)
    {
        var privileges = new SortedDictionary<string, IReadOnlyDictionary<Privilege, PrivilegeDepth>>(StringComparer.Ordinal);
        foreach (var (table, rights) in role.Privileges ?? [])
        {
            var granted = new SortedDictionary<Privilege, PrivilegeDepth>();
            foreach (var (right, privilege) in rights)
            {
                if (ToPrivilege(right) is { } mapped) granted[mapped] = ToDepth(privilege.PrivilegeDepth);
            }
            privileges[table] = granted;
        }
        return new RoleDefinition(role.Name, privileges);
    }

    private static Privilege? ToPrivilege(AccessRights right) => right switch
    {
        AccessRights.CreateAccess => Privilege.Create,
        AccessRights.ReadAccess => Privilege.Read,
        AccessRights.WriteAccess => Privilege.Write,
        AccessRights.DeleteAccess => Privilege.Delete,
        AccessRights.AppendAccess => Privilege.Append,
        AccessRights.AppendToAccess => Privilege.AppendTo,
        AccessRights.AssignAccess => Privilege.Assign,
        AccessRights.ShareAccess => Privilege.Share,
        _ => null,
    };

    // XrmMockup treats RecordFilter like Global (Security.HasPermission), so it maps to Organization here too.
    private static PrivilegeDepth ToDepth(CrmDepth depth) => depth switch
    {
        CrmDepth.Basic => PrivilegeDepth.User,
        CrmDepth.Local => PrivilegeDepth.BusinessUnit,
        CrmDepth.Deep => PrivilegeDepth.ParentChildBusinessUnit,
        _ => PrivilegeDepth.Organization,
    };
}
