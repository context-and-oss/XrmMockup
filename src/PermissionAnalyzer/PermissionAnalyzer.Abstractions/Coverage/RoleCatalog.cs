using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Coverage;

/// <summary>
/// The security roles in the metadata, found by name.
/// </summary>
public sealed class RoleCatalog(IReadOnlyList<RoleDefinition> roles)
{
    public IReadOnlyList<RoleDefinition> Roles { get; } = roles;

    /// <summary>
    /// The roles with this name: an exact match, else a match ignoring case. Every business unit can have a copy
    /// of a role, so there can be several. Empty when no role has the name.
    /// </summary>
    public IReadOnlyList<RoleDefinition> Find(string name)
    {
        var exact = Roles.Where(r => r.Name == name).ToList();
        return exact.Count > 0 ? exact : Roles.Where(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// The role names closest to <paramref name="name"/>, for an error message: names containing it first, then by
    /// edit distance, ignoring case. Names that are too different aren't suggested.
    /// </summary>
    public IReadOnlyList<string> Suggest(string name, int count = 3)
    {
        var wanted = name.ToLowerInvariant();
        var maxDistance = Math.Max(2, wanted.Length / 3);
        return Roles
            .Select(r => r.Name)
            .Distinct(StringComparer.Ordinal)
            .Select(n => (Name: n, Lower: n.ToLowerInvariant()))
            .Select(n => (n.Name, Contains: wanted.Length > 0 && (n.Lower.Contains(wanted) || wanted.Contains(n.Lower)), Distance: Distance(wanted, n.Lower)))
            .Where(n => n.Contains || n.Distance <= maxDistance)
            .OrderBy(n => n.Contains ? 0 : 1)
            .ThenBy(n => n.Distance)
            .ThenBy(n => n.Name, StringComparer.Ordinal)
            .Take(count)
            .Select(n => n.Name)
            .ToList();
    }

    private static int Distance(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) previous[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var substitution = previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1);
                current[j] = Math.Min(substitution, Math.Min(previous[j], current[j - 1]) + 1);
            }
            (previous, current) = (current, previous);
        }
        return previous[b.Length];
    }
}
