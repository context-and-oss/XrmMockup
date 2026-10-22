using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Coverage;

/// <summary>
/// The roles one identity is checked against, combined the way XrmMockup combines a principal's roles: the union of
/// their privileges, with the highest depth winning.
/// </summary>
public sealed class RoleSet
{
    private readonly Dictionary<string, Dictionary<Privilege, PrivilegeDepth>> _privileges;

    private RoleSet(IReadOnlyList<string> names, string origin, Dictionary<string, Dictionary<Privilege, PrivilegeDepth>> privileges)
    {
        Names = names;
        Origin = origin;
        _privileges = privileges;
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
        foreach (var role in roles)
        {
            names.Add(role.Name);
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
        return new RoleSet(names.ToList(), origin, privileges);
    }

    /// <summary>
    /// The depth the roles grant the privilege at, or <c>null</c> when none of them grants it.
    /// </summary>
    public PrivilegeDepth? Depth(string table, Privilege privilege) =>
        _privileges.TryGetValue(table, out var granted) && granted.TryGetValue(privilege, out var depth) ? depth : null;

    public override string ToString() => $"{string.Join(", ", Names)} ({Origin})";
}
