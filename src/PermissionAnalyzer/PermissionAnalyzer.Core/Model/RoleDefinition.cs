namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// A security role from the XrmMockup metadata: the privileges it grants per table, with their depth.
/// </summary>
public sealed class RoleDefinition(string name, IReadOnlyDictionary<string, IReadOnlyDictionary<Privilege, PrivilegeDepth>> privileges)
{
    public string Name { get; } = name;

    /// <summary>
    /// Granted privileges keyed by table logical name.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<Privilege, PrivilegeDepth>> Privileges { get; } = privileges;
}
