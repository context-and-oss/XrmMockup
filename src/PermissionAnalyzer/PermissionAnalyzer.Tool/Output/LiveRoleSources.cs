using System.Globalization;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>Keep live provenance visible even in a passing or filtered report. Offline reports are unchanged.</summary>
internal static class LiveRoleSources
{
    public static IReadOnlyList<RoleSet>? Sets(Manifest manifest, CoverageReport? coverage)
    {
        if (coverage is null) return null;
        var sources = manifest.Sections
            .SelectMany(section => coverage.For(section).Identities)
            .Select(identity => identity.Roles)
            .Where(roles => roles?.LiveSource is not null)
            .Cast<RoleSet>()
            .Distinct()
            .OrderBy(roles => roles.Origin, StringComparer.Ordinal)
            .ToList();
        return sources.Count == 0 ? null : sources;
    }

    /// <summary>Machine-readable reports always retain complete provenance, independently of text detail.</summary>
    public static IReadOnlyList<string>? Of(Manifest manifest, CoverageReport? coverage) =>
        Sets(manifest, coverage)?.Select(roles =>
        {
            var source = roles.LiveSource!;
            var assignments = source.Assignments.Select(a => $"{a.RoleName}: {Assignment(a, true)} (root role ID: {a.RootRoleId:D})");
            return $"{string.Join(", ", roles.Names)} ({roles.Origin} ({source.UserId:D}); environment {source.EnvironmentUrl}; " +
                $"roles read {Timestamp(source)}; {string.Join("; ", assignments)})";
        }).ToList();

    public static IEnumerable<string> Lines(RoleSet roles, Detail detail)
    {
        var source = roles.LiveSource!;
        var detailed = detail >= Detail.Detailed;
        var label = roles.Origin.StartsWith(CliOptions.CallerUser.Primary + ":", StringComparison.Ordinal) ? "Caller user" : "User";
        yield return $"{label}: {source.UserName}" + (detailed ? $" (ID: {source.UserId:D})" : "");
        if (detailed)
        {
            yield return $"  Environment: {source.EnvironmentUrl}";
            yield return $"  Roles read: {Timestamp(source)}";
        }
        foreach (var role in source.Assignments.GroupBy(a => (a.RoleName, a.RootRoleId)))
        {
            if (!detailed)
            {
                var assignments = role.Select(a => Assignment(a, false)).Distinct(StringComparer.Ordinal);
                yield return $"  {role.Key.RoleName}: {string.Join("; ", assignments)}";
            }
            else
            {
                yield return $"  {role.Key.RoleName} (root role ID: {role.Key.RootRoleId:D})";
                foreach (var assignment in role) yield return $"    {Assignment(assignment, true)}";
            }
        }
    }

    private static string Assignment(LiveRoleAssignment assignment, bool detailed) => assignment.TeamId is { } teamId
        ? $"team '{assignment.TeamName}'" + (detailed ? $" (ID: {teamId:D})" : "")
        : "direct";

    private static string Timestamp(LiveRoleSource source) => source.ReadAt.ToString("O", CultureInfo.InvariantCulture);
}
