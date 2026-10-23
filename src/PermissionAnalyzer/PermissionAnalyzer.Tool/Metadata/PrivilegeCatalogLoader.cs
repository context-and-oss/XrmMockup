using System.Runtime.Serialization;
using DG.Tools.XrmMockup;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Metadata;

/// <summary>
/// Reads <c>Metadata/PrivilegeCatalog.xml</c>, written by the metadata tool: every privilege by name, and the execute
/// privileges of the custom APIs.
/// </summary>
internal static class PrivilegeCatalogLoader
{
    public const string FileName = "PrivilegeCatalog.xml";

    /// <summary>
    /// The privileges and custom APIs, sorted by name, or <c>null</c> when the metadata folder has no catalog.
    /// </summary>
    public static (IReadOnlyList<PrivilegeDefinition> Privileges, IReadOnlyList<CustomApiDefinition> CustomApis)? Load(string metadataDirectory)
    {
        var path = Path.Combine(metadataDirectory, FileName);
        if (!File.Exists(path)) return null;

        PrivilegeCatalog catalog;
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            catalog = new DataContractSerializer(typeof(PrivilegeCatalog)).ReadObject(stream) as PrivilegeCatalog
                ?? throw new ToolException($"Not a privilege catalog: {path}");
        }
        return (Convert(catalog.Privileges ?? []), Convert(catalog.CustomApis ?? []));
    }

    internal static IReadOnlyList<PrivilegeDefinition> Convert(IEnumerable<MetaPrivilege> privileges) =>
        privileges
            .Where(p => !string.IsNullOrEmpty(p.Name))
            .Select(p => new PrivilegeDefinition(
                p.Name,
                RoleLoader.ToPrivilege(p.AccessRight),
                (p.Tables ?? []).Select(t => t.ToLowerInvariant()).Distinct(StringComparer.Ordinal).OrderBy(t => t, StringComparer.Ordinal).ToList()))
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToList();

    internal static IReadOnlyList<CustomApiDefinition> Convert(IEnumerable<MetaCustomApi> customApis) =>
        customApis
            .Where(a => !string.IsNullOrEmpty(a.UniqueName))
            .Select(a => new CustomApiDefinition(a.UniqueName, string.IsNullOrEmpty(a.ExecutePrivilegeName) ? null : a.ExecutePrivilegeName))
            .OrderBy(a => a.UniqueName, StringComparer.Ordinal)
            .ToList();
}
