using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Coverage;

/// <summary>
/// The roles one identity is checked against, combined the way XrmMockup combines a principal's roles: the union of
/// their privileges, with the highest depth winning.
/// </summary>
public sealed class RoleSet
{
    private readonly Dictionary<string, Dictionary<Privilege, PrivilegeDepth>> _privileges;
    private readonly Dictionary<string, PrivilegeDepth>? _globalPrivileges;

    private RoleSet(
        IReadOnlyList<string> names,
        string origin,
        Dictionary<string, Dictionary<Privilege, PrivilegeDepth>> privileges,
        Dictionary<string, PrivilegeDepth>? globalPrivileges)
    {
        Names = names;
        Origin = origin;
        _privileges = privileges;
        _globalPrivileges = globalPrivileges;
    }

    /// <summary>
    /// Role names, sorted.
    /// </summary>
    public IReadOnlyList<string> Names { get; }

    /// <summary>
    /// Where the roles were given, such as <c>--role</c> or <c>Principals: Contoso.Functions</c>.
    /// </summary>
    public string Origin { get; }

    public static RoleSet Combine(IEnumerable<RoleDefinition> roles, string origin)
    {
        var names = new SortedSet<string>(StringComparer.Ordinal);
        var privileges = new Dictionary<string, Dictionary<Privilege, PrivilegeDepth>>(StringComparer.Ordinal);
        var globalPrivileges = new Dictionary<string, PrivilegeDepth>(StringComparer.OrdinalIgnoreCase);
        var globalKnown = true;
        foreach (var role in roles)
        {
            names.Add(role.Name);
            if (role.GlobalPrivileges is null) globalKnown = false;
            foreach (var global in role.GlobalPrivileges ?? new Dictionary<string, PrivilegeDepth>())
            {
                if (!globalPrivileges.TryGetValue(global.Key, out var depth) || global.Value > depth) globalPrivileges[global.Key] = global.Value;
            }

            foreach (var table in role.Privileges)
            {
                if (!privileges.TryGetValue(table.Key, out var granted))
                {
                    granted = new Dictionary<Privilege, PrivilegeDepth>();
                    privileges.Add(table.Key, granted);
                }

                foreach (var privilege in table.Value)
                {
                    if (!granted.TryGetValue(privilege.Key, out var depth) || privilege.Value > depth) granted[privilege.Key] = privilege.Value;
                }
            }
        }
        return new RoleSet(names.ToList(), origin, privileges, globalKnown ? globalPrivileges : null);
    }

    /// <summary>
    /// Every role has its privileges not tied to a table in the metadata, so <see cref="GlobalDepth"/> can be trusted.
    /// </summary>
    public bool KnowsGlobalPrivileges => _globalPrivileges is not null;

    /// <summary>
    /// The depth the roles grant the privilege at, or <c>null</c> when none of them grants it.
    /// </summary>
    public PrivilegeDepth? Depth(string table, Privilege privilege) =>
        _privileges.TryGetValue(table, out var granted) && granted.TryGetValue(privilege, out var depth) ? depth : null;

    /// <summary>
    /// The depth the roles grant a privilege not tied to a table at, by name, or <c>null</c> when none of them grants
    /// it or <see cref="KnowsGlobalPrivileges"/> is <c>false</c>.
    /// </summary>
    public PrivilegeDepth? GlobalDepth(string privilege) =>
        _globalPrivileges is not null && _globalPrivileges.TryGetValue(privilege, out var depth) ? depth : null;

    public override string ToString() => $"{string.Join(", ", Names)} ({Origin})";
}
