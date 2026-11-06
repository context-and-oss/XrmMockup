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
    /// The roles to check against: the role options, else the <c>Principals</c> and <c>Users</c> settings.
    /// <c>null</c> to report without coverage, when there are none and <paramref name="requireRoles"/> isn't set,
    /// or only settings and no roles to look them up in. Unknown role names are an error that suggests the closest
    /// names.
    /// </summary>
    public static CoverageOptions? Create(
        IReadOnlyList<RoleDefinition>? roles,
        IReadOnlyList<string> roleNames,
        IReadOnlyList<string> callerRoleNames,
        PermissionsConfiguration configuration,
        bool requireRoles)
    {
        var given = roleNames.Count > 0 || callerRoleNames.Count > 0;
        if (!given && configuration.Principals.Count == 0 && configuration.Users.Count == 0)
        {
            if (!requireRoles) return null;
            throw new ToolException(
                $"No roles to check against. Pass {CliOptions.Role.Primary}, or map the application user's roles in " +
                $"{PermissionsConfiguration.SectionPath}:Principals or users' roles in {PermissionsConfiguration.SectionPath}:Users");
        }

        if (roles is null)
        {
            // Without metadata a plain run lists what the code needs; the missing metadata is already warned about
            if (!given && !requireRoles) return null;
            throw new ToolException($"No security roles loaded, so there is nothing to check against. Pass {CliOptions.Metadata.Primary} with a folder that has SecurityRoles");
        }

        var catalog = new RoleCatalog(roles);
        return new CoverageOptions
        {
            Roles = roleNames.Count > 0 ? Resolve(catalog, roleNames, CliOptions.Role.Primary, CliOptions.Role.Primary) : null,
            CallerRoles = callerRoleNames.Count > 0 ? Resolve(catalog, callerRoleNames, CliOptions.CallerRole.Primary, CliOptions.CallerRole.Primary) : null,
            // --role replaces the principals, so a stale mapping among them doesn't stop the run
            Principals = roleNames.Count > 0 ? new Dictionary<string, RoleSet>(StringComparer.OrdinalIgnoreCase) : configuration.Principals
                .OrderBy(p => p.Key, StringComparer.Ordinal)
                .ToDictionary(
                    p => p.Key,
                    p => Resolve(catalog, p.Value.Roles, $"Principals: {p.Key}", $"{PermissionsConfiguration.SectionPath}:Principals:{p.Key}"),
                    StringComparer.OrdinalIgnoreCase),
            Users = configuration.Users
                .OrderBy(u => u.Key, StringComparer.Ordinal)
                .ToDictionary(
                    u => UserId(u.Key),
                    u => Resolve(catalog, u.Value.Roles, $"Users: {u.Value.Name ?? u.Key}", $"{PermissionsConfiguration.SectionPath}:Users:{u.Key}"),
                    StringComparer.OrdinalIgnoreCase),
            Suppress = configuration.Suppress.Select(Suppression).ToList(),
        };
    }

    /// <summary>
    /// A <c>Users</c> key as the id the analysis writes, so braces and case don't matter.
    /// </summary>
    internal static string UserId(string key) =>
        Guid.TryParse(key, out var id)
            ? id.ToString("D")
            : throw new ToolException($"{PermissionsConfiguration.SectionPath}:Users:{key} isn't a user id. Use the systemuserid, such as 6f1c2a9e-0000-4000-8000-000000000001");

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

    internal static Suppression Suppression(SuppressionConfiguration suppression, int index)
    {
        var setting = $"{PermissionsConfiguration.SectionPath}:Suppress:{index}";
        if (string.IsNullOrWhiteSpace(suppression.Table))
        {
            // A privilege not tied to a table is suppressed by its name, such as prvActOnBehalfOfAnotherUser
            if (suppression.Privilege is { } name && !string.IsNullOrWhiteSpace(name) && RuleSetup.ParsePrivilege(name) is null)
            {
                return new Suppression("", null, suppression.Reason, name.Trim());
            }
            throw new ToolException($"{setting} has no Table. Leave it out only to suppress a privilege not tied to a table by its name, such as prvActOnBehalfOfAnotherUser");
        }

        Privilege? privilege = null;
        if (suppression.Privilege is { } text)
        {
            privilege = RuleSetup.ParsePrivilege(text)
                ?? throw new ToolException($"{setting} has an unknown Privilege '{text}'. Use one of {RuleSetup.PrivilegeNames}");
        }
        return new Suppression(suppression.Table.Trim().ToLowerInvariant(), privilege, suppression.Reason);
    }
}
