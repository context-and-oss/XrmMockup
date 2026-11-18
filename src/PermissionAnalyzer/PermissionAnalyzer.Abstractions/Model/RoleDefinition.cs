namespace XrmMockup.PermissionAnalyzer.Core.Model;

/// <summary>
/// A security role from the XrmMockup metadata: the privileges it grants per table, with their depth, and the
/// privileges not tied to a table that it grants.
/// </summary>
public sealed class RoleDefinition(
    string name,
    IReadOnlyDictionary<string, IReadOnlyDictionary<Privilege, PrivilegeDepth>> privileges,
    IReadOnlyDictionary<string, PrivilegeDepth>? globalPrivileges = null)
{
    public string Name { get; } = name;

    /// <summary>
    /// Granted privileges keyed by table logical name.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<Privilege, PrivilegeDepth>> Privileges { get; } = privileges;

    /// <summary>
    /// Granted privileges not tied to a table, such as <c>prvActOnBehalfOfAnotherUser</c>, keyed by name.
    /// <c>null</c> when the role was downloaded by a metadata generator that didn't download them.
    /// </summary>
    public IReadOnlyDictionary<string, PrivilegeDepth>? GlobalPrivileges { get; } = globalPrivileges;
}
