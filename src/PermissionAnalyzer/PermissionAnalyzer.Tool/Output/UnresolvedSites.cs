using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// The unresolved operations a filter matches, each call site once with every entry point and unreached method that
/// reaches it, sorted by location, operation and identity. Markdown and SARIF list call sites rather than sections.
/// </summary>
internal static class UnresolvedSites
{
    public static IReadOnlyList<UnresolvedSite> Of(Manifest manifest, ReportFilter filter)
    {
        var sites = new Dictionary<(SourceLocation Location, string Operation, string Privileges, RunsAs RunsAs, string Reason), (UnresolvedOperation Operation, List<Section> In)>();
        foreach (var section in manifest.Sections)
        {
            foreach (var operation in section.Unresolved.Where(filter.Matches))
            {
                var key = (operation.Source.Location, operation.Operation, string.Join(", ", operation.Privileges), operation.RunsAs, operation.Reason);
                if (!sites.TryGetValue(key, out var site)) sites.Add(key, site = (operation, []));
                site.In.Add(new Section(section.Id, section.Declaration));
            }
        }

        return sites
            .OrderBy(s => s.Key.Location)
            .ThenBy(s => s.Key.Operation, StringComparer.Ordinal)
            .ThenBy(s => s.Key.RunsAs)
            .ThenBy(s => s.Key.Privileges, StringComparer.Ordinal)
            .ThenBy(s => s.Key.Reason, StringComparer.Ordinal)
            .Select(s => new UnresolvedSite(s.Value.Operation, s.Value.In))
            .ToList();
    }

    /// <summary>
    /// The privileges an unresolved operation would need, such as <c> [Write, Delete]</c>; empty when they aren't known.
    /// </summary>
    public static string PrivilegeSuffix(UnresolvedOperation operation) =>
        operation.Privileges.Count > 0 ? $" [{string.Join(", ", operation.Privileges)}]" : "";
}

/// <param name="Operation">The operation, as found in the first section that reaches it.</param>
/// <param name="In">The entry points and unreached methods that reach it, in manifest order.</param>
internal sealed record UnresolvedSite(UnresolvedOperation Operation, IReadOnlyList<Section> In);

/// <summary>
/// An entry point or unreached method, by id, with where it is declared.
/// </summary>
internal sealed record Section(string Id, SourceLocation? Declaration);
