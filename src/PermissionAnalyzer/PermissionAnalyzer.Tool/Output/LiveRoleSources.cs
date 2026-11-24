using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>Keep live provenance visible even in a passing or filtered report. Offline reports are unchanged.</summary>
internal static class LiveRoleSources
{
    public static IReadOnlyList<string>? Of(Manifest manifest, CoverageReport? coverage)
    {
        if (coverage is null) return null;
        var sources = manifest.EntryPoints.Cast<IManifestSection>().Concat(manifest.Unreached)
            .SelectMany(section => coverage.For(section).Identities)
            .Select(identity => identity.Roles)
            .Where(roles => roles is not null && (roles.Origin.StartsWith(CliOptions.User.Primary + ":", StringComparison.Ordinal) ||
                roles.Origin.StartsWith(CliOptions.CallerUser.Primary + ":", StringComparison.Ordinal)))
            .Select(roles => roles!.ToString())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
        return sources.Count == 0 ? null : sources;
    }
}
