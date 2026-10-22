using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// The manifest for reading in a terminal: requirements per entry point, the methods no entry point reaches,
/// then the privileges per table. With coverage, each requirement is marked granted (✔) or missing (✘), with the
/// roles each identity was checked against.
/// </summary>
internal sealed class TextManifestWriter : IManifestWriter
{
    public void Write(Manifest manifest, CoverageReport? coverage, TextWriter writer)
    {
        var requirements = manifest.AllRequirements.ToList();
        var tableWidth = requirements.Select(r => r.Table.Length).DefaultIfEmpty(0).Max();
        var privilegeWidth = Enum.GetNames<Privilege>().Max(n => n.Length);
        var runsAsWidth = requirements.Select(r => r.RunsAs.ToString().Length).DefaultIfEmpty(0).Max();
        var coverageWidth = coverage is null ? 0 : manifest.EntryPoints.SelectMany(e => e.Requirements.Select(r => Label(coverage.For(e).For(r))))
            .Concat(manifest.Unreached.SelectMany(m => m.Requirements.Select(r => Label(coverage.For(m).For(r)))))
            .Select(l => l.Length).DefaultIfEmpty(0).Max();

        void Section(string header, IReadOnlyList<Requirement> sectionRequirements, IReadOnlyList<UnresolvedOperation> unresolved, IReadOnlyList<Assumption> assumptions, SectionCoverage? sectionCoverage)
        {
            writer.Write(header + "\n");
            if (sectionCoverage is not null)
            {
                foreach (var identity in sectionCoverage.Identities)
                {
                    var roles = identity.Roles is { } set ? set.ToString() : identity.Note;
                    writer.Write($"  roles for {identity.RunsAs}: {roles}\n");
                }
            }

            foreach (var requirement in sectionRequirements)
            {
                var basis = requirement.Basis == RequirementBasis.Direct ? "" : $"  ({Kebab(requirement.Basis)})";
                var sources = string.Join(", ", requirement.Sources.Select(Source));
                var row = $"{requirement.Table.PadRight(tableWidth)}  {requirement.Privilege.ToString().PadRight(privilegeWidth)}  " +
                    $"{requirement.RunsAs.ToString().PadRight(runsAsWidth)}  ";
                if (sectionCoverage?.For(requirement) is { } result)
                {
                    var reason = result.Status == CoverageStatus.Suppressed && result.Reason is { } text ? $"  suppressed: {text}" : "";
                    writer.Write($"  {Mark(result.Status)} {row}{Label(result).PadRight(coverageWidth)}  {sources}{basis}{reason}\n");
                }
                else
                {
                    writer.Write($"  {row}{sources}{basis}\n");
                }
            }

            if (sectionCoverage?.Impersonation is { } impersonation)
            {
                writer.Write(
                    $"  ! {ImpersonationCheck.Privilege} as {impersonation.RunsAs}: unverifiable, the role metadata has no global privileges. " +
                    "Setting CallerId needs it\n");
            }

            foreach (var operation in unresolved)
            {
                var privileges = operation.Privileges.Count > 0 ? $" [{string.Join(", ", operation.Privileges)}]" : "";
                writer.Write($"  ? {operation.Operation}{privileges} as {operation.RunsAs}: {operation.Reason}  {Source(operation.Source)}\n");
            }

            foreach (var assumption in assumptions)
            {
                writer.Write($"  ~ {assumption.Reason}  {Source(assumption.Source)}\n");
            }
            writer.Write('\n');
        }

        foreach (var entryPoint in manifest.EntryPoints)
        {
            var header = entryPoint.Declaration is { } declaration ? $"{entryPoint.Id}  {declaration}" : entryPoint.Id;
            if (entryPoint.ImpersonatingUserId is { } user) header += $"  (runs as user {user})";
            if (entryPoint.Triggers.Count > 0) header += $"\n  fires: {string.Join(", ", entryPoint.Triggers)}";
            Section(header, entryPoint.Requirements, entryPoint.Unresolved, entryPoint.Assumptions, coverage?.For(entryPoint));
        }

        if (manifest.Unreached.Count > 0)
        {
            writer.Write("Not reached from any entry point\n\n");
            foreach (var method in manifest.Unreached)
            {
                var header = method.Declaration is { } declaration ? $"{method.Id}  {declaration}" : method.Id;
                Section(header, method.Requirements, method.Unresolved, method.Assumptions, coverage?.For(method));
            }
        }

        var byTable = requirements
            .GroupBy(r => r.Table)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();
        if (byTable.Count > 0)
        {
            writer.Write("Privileges per table\n");
            foreach (var table in byTable)
            {
                var privileges = table.Select(r => r.Privilege).Distinct().OrderBy(p => p);
                writer.Write($"  {table.Key.PadRight(tableWidth)}  {string.Join(", ", privileges)}\n");
            }
            writer.Write('\n');
        }

        if (manifest.NotRoleGoverned is { Count: > 0 } notGoverned)
        {
            writer.Write($"Not in any security role, so XrmMockup allows every operation: {string.Join(", ", notGoverned)}\n\n");
        }

        // A requirement is counted once per entry point or method that needs it; the table above merges them
        var unresolvedCount = manifest.EntryPoints.Sum(e => e.Unresolved.Count) + manifest.Unreached.Sum(m => m.Unresolved.Count);
        var tablePrivileges = byTable.Sum(t => t.Select(r => r.Privilege).Distinct().Count());
        writer.Write(
            $"{Count(manifest.EntryPoints.Count, "entry point")}, {Count(manifest.Unreached.Count, "unreached method")}, " +
            $"{Count(byTable.Count, "table")}, {Count(tablePrivileges, "table privilege")} " +
            $"({Count(requirements.Count, "requirement")} counted per entry point and method), {unresolvedCount} unresolved\n");

        if (coverage is not null)
        {
            writer.Write(Result(coverage.Summary) + "\n");
            Missing(coverage.Missing, writer);
            if (NotChecked(coverage.Summary) is { } notChecked) writer.Write(notChecked + "\n");
        }
    }

    /// <summary>
    /// Each missing privilege once, with the roles that lack it and where it is needed, so it doesn't have to be
    /// collected from the sections above.
    /// </summary>
    private static void Missing(IReadOnlyList<MissingPrivilege> missing, TextWriter writer)
    {
        if (missing.Count == 0) return;

        var tableWidth = missing.Max(m => m.Table.Length);
        var privilegeWidth = missing.Max(m => m.Privilege.ToString().Length);
        var runsAsWidth = missing.Max(m => m.RunsAs.ToString().Length);
        var rolesWidth = missing.Max(m => m.Roles.ToString().Length);
        writer.Write("Missing privileges:\n");
        foreach (var privilege in missing)
        {
            writer.Write(
                $"  ✘ {privilege.Table.PadRight(tableWidth)}  {privilege.Privilege.ToString().PadRight(privilegeWidth)}  " +
                $"{privilege.RunsAs.ToString().PadRight(runsAsWidth)}  {privilege.Roles.ToString().PadRight(rolesWidth)}  " +
                $"in {string.Join(", ", privilege.In)}\n");
        }
    }

    /// <summary>
    /// What became of each requirement.
    /// </summary>
    private static string Result(CoverageSummary summary)
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
    private static string? NotChecked(CoverageSummary summary)
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
        if (summary.Unverifiable > 0) parts.Add($"{ImpersonationCheck.Privilege} for {Count(summary.Unverifiable, "entry point")}");
        return parts.Count > 0 ? "Not checked: " + string.Join(", ", parts) : null;
    }

    private static string Mark(CoverageStatus status) => status switch
    {
        CoverageStatus.Granted or CoverageStatus.NotGoverned => "✔",
        CoverageStatus.Missing => "✘",
        CoverageStatus.Suppressed => "-",
        _ => "·",
    };

    /// <summary>
    /// The granted depth, or what else became of the requirement.
    /// </summary>
    private static string Label(RequirementCoverage coverage) => coverage.Status switch
    {
        CoverageStatus.Granted => coverage.Depth.ToString()!,
        CoverageStatus.NotGoverned => "not governed",
        CoverageStatus.Missing => "missing",
        CoverageStatus.Suppressed => "suppressed",
        CoverageStatus.NotChecked => "not checked",
        _ => "SYSTEM",
    };

    /// <summary>
    /// A source location, with the call path when the operation is in a called method.
    /// </summary>
    private static string Source(RequirementSource source) =>
        source.Path.Count > 1 ? $"{source.Location} ({string.Join(" → ", source.Path)})" : source.Location.ToString();

    private static string Kebab(RequirementBasis basis) => basis switch
    {
        RequirementBasis.AllImplementations => "all-implementations",
        RequirementBasis.AllLookupTargets => "all-lookup-targets",
        _ => basis.ToString().ToLowerInvariant(),
    };

    private static string Count(int count, string noun) => $"{count} {noun}{(count == 1 ? "" : "s")}";
}
