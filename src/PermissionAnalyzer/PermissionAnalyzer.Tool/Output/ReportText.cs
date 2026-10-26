using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// The lines the text and Markdown reports share: the counts, what a filter matches and the result.
/// </summary>
internal static class ReportText
{
    /// <summary>
    /// What was analyzed. A requirement is counted once per entry point or method that needs it; the tables merge
    /// them. The counts are of the whole analysis, whatever is shown.
    /// </summary>
    public static string Totals(Manifest manifest)
    {
        var requirements = manifest.AllRequirements.ToList();
        var globalRequirements = manifest.AllGlobalRequirements.ToList();
        var unresolvedCount = manifest.EntryPoints.Sum(e => e.Unresolved.Count) + manifest.Unreached.Sum(m => m.Unresolved.Count);
        var tables = requirements.Select(r => r.Table).Distinct(StringComparer.Ordinal).Count();
        var tablePrivileges = requirements.Select(r => (r.Table, r.Privilege)).Distinct().Count();
        var globalPrivileges = globalRequirements.Select(r => r.Privilege).Distinct(StringComparer.Ordinal).Count();
        return
            $"{Count(manifest.EntryPoints.Count, "entry point")}, {Count(manifest.Unreached.Count, "unreached method")}, " +
            $"{Count(tables, "table")}, {Count(tablePrivileges, "table privilege")} " +
            $"({Count(requirements.Count, "requirement")} counted per entry point and method), " +
            (globalPrivileges > 0 ? $"{Count(globalPrivileges, "privilege")} not tied to a table, " : "") +
            $"{unresolvedCount} unresolved";
    }

    /// <summary>
    /// What the filter matches, of every requirement.
    /// </summary>
    public static string Filtered(Manifest manifest, ReportFilter filter)
    {
        var all = manifest.AllRequirements.Count() + manifest.AllGlobalRequirements.Count();
        var matching = manifest.AllRequirements.Count(filter.Matches) + manifest.AllGlobalRequirements.Count(filter.Matches);
        return $"Filtered by {filter}: {matching} of {Count(all, "requirement")} match";
    }

    /// <summary>
    /// What became of each requirement.
    /// </summary>
    public static string Result(CoverageSummary summary)
    {
        var parts = new List<string> { $"{summary.Granted} granted", $"{summary.Missing} missing" };
        void Add(int count, string text)
        {
            if (count > 0) parts.Add($"{count} {text}");
        }
        Add(summary.NotGoverned, "not governed");
        Add(summary.Suppressed, "suppressed");
        Add(summary.System, "as SYSTEM");
        return "Result: " + string.Join(", ", parts);
    }

    /// <summary>
    /// What the result doesn't cover, so a pass isn't read as proof that the roles are enough; <c>null</c> when
    /// everything was checked.
    /// </summary>
    public static string? NotChecked(CoverageSummary summary)
    {
        var parts = new List<string>();
        if (summary.NotChecked > 0) parts.Add($"{Count(summary.NotChecked, "requirement")} without roles");
        if (summary.Unresolved > 0)
        {
            var details = new List<string>();
            if (summary.UnresolvedNoRule > 0) details.Add($"{Count(summary.UnresolvedNoRule, "request")} without a rule");
            if (summary.UnresolvedCustomApi > 0) details.Add($"{Count(summary.UnresolvedCustomApi, "custom API call")}");
            var other = summary.Unresolved - summary.UnresolvedNoRule - summary.UnresolvedCustomApi;
            if (other > 0) details.Add($"{other} with an unknown table or request");
            parts.Add($"{Count(summary.Unresolved, "unresolved operation")} ({string.Join(", ", details)})");
        }
        if (summary.Unverifiable > 0)
        {
            parts.Add($"{Count(summary.Unverifiable, "privilege")} not tied to a table (the role metadata predates them; regenerate it)");
        }
        return parts.Count > 0 ? "Not checked: " + string.Join(", ", parts) : null;
    }

    public static string Count(int count, string noun) => $"{count} {noun}{(count == 1 ? "" : "s")}";
}
