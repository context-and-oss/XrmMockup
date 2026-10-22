using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Options;

namespace XrmMockup.PermissionAnalyzer.Tool.Commands;

/// <summary>
/// Turns role names from the command line and configuration into the role sets to check against.
/// </summary>
internal static class CoverageSetup
{
    /// <summary>
    /// The roles to check against, or <c>null</c> to report without coverage: when no role option is given and
    /// <paramref name="requireRoles"/> isn't set. Unknown role names are an error that suggests the closest names.
    /// </summary>
    public static CoverageOptions? Create(
        IReadOnlyList<RoleDefinition>? roles,
        IReadOnlyList<string> roleNames,
        IReadOnlyList<string> callerRoleNames,
        PermissionsConfiguration configuration,
        bool requireRoles)
    {
        var given = roleNames.Count > 0 || callerRoleNames.Count > 0;
        if (!given && !requireRoles) return null;

        if (!given && configuration.Principals.Count == 0 && configuration.Users.Count == 0)
        {
            throw new ToolException(
                $"No roles to check against. Pass {CliOptions.Role.Primary}, or map the application user's roles in " +
                "XrmMockup:Permissions:Principals or users' roles in XrmMockup:Permissions:Users");
        }

        if (roles is null)
        {
            throw new ToolException("No security roles loaded, so there is nothing to check against. Pass --metadata with a folder that has SecurityRoles");
        }

        var catalog = new RoleCatalog(roles);
        return new CoverageOptions
        {
            Roles = roleNames.Count > 0 ? Resolve(catalog, roleNames, CliOptions.Role.Primary, CliOptions.Role.Primary) : null,
            CallerRoles = callerRoleNames.Count > 0 ? Resolve(catalog, callerRoleNames, CliOptions.CallerRole.Primary, CliOptions.CallerRole.Primary) : null,
            Principals = configuration.Principals
                .OrderBy(p => p.Key, StringComparer.Ordinal)
                .ToDictionary(
                    p => p.Key,
                    p => Resolve(catalog, p.Value.Roles, $"Principals: {p.Key}", $"XrmMockup:Permissions:Principals:{p.Key}"),
                    StringComparer.OrdinalIgnoreCase),
            Users = configuration.Users
                .OrderBy(u => u.Key, StringComparer.Ordinal)
                .ToDictionary(
                    u => u.Key,
                    u => Resolve(catalog, u.Value.Roles, $"Users: {u.Value.Name ?? u.Key}", $"XrmMockup:Permissions:Users:{u.Key}"),
                    StringComparer.OrdinalIgnoreCase),
            Suppress = configuration.Suppress.Select(Suppression).ToList(),
        };
    }

    private static RoleSet Resolve(RoleCatalog catalog, IReadOnlyList<string> names, string origin, string setting)
    {
        if (names.Count == 0) throw new ToolException($"{setting} has no roles");

        var found = new List<RoleDefinition>();
        foreach (var name in names)
        {
            var matches = catalog.Find(name);
            if (matches.Count == 0)
            {
                var suggestions = catalog.Suggest(name);
                throw new ToolException(
                    $"Unknown security role '{name}' in {setting}." +
                    (suggestions.Count > 0 ? $" Did you mean {string.Join(", ", suggestions.Select(s => $"'{s}'"))}?" : ""));
            }
            found.AddRange(matches);
        }
        return RoleSet.Combine(found, origin);
    }

    private static Suppression Suppression(SuppressionConfiguration suppression, int index)
    {
        var setting = $"XrmMockup:Permissions:Suppress:{index}";
        if (string.IsNullOrWhiteSpace(suppression.Table)) throw new ToolException($"{setting} has no Table");

        Privilege? privilege = null;
        if (suppression.Privilege is { } text)
        {
            if (!Enum.TryParse<Privilege>(text, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            {
                throw new ToolException($"{setting} has an unknown Privilege '{text}'. Use one of {string.Join(", ", Enum.GetNames<Privilege>())}");
            }
            privilege = parsed;
        }
        return new Suppression(suppression.Table.Trim().ToLowerInvariant(), privilege, suppression.Reason);
    }
}
