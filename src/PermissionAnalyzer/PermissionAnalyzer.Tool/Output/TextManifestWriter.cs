using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// The manifest for reading in a terminal: requirements per entry point, the methods no entry point reaches,
/// then the privileges per table and the privileges not tied to a table. With coverage, each requirement is marked
/// granted (✔) or missing (✘), with the roles each identity was checked against.
/// </summary>
internal sealed class TextManifestWriter : IManifestWriter
{
    public void Write(Manifest manifest, CoverageReport? coverage, TextWriter writer)
    {
        var requirements = manifest.AllRequirements.ToList();
        var globalRequirements = manifest.AllGlobalRequirements.ToList();
        var tableWidth = requirements.Select(r => r.Table.Length).Concat(globalRequirements.Select(r => r.Privilege.Length)).DefaultIfEmpty(0).Max();
        var privilegeWidth = Enum.GetNames<Privilege>().Append(GlobalColumn).Max(n => n.Length);
        var runsAsWidth = requirements.Select(r => r.RunsAs.ToString().Length).Concat(globalRequirements.Select(r => r.RunsAs.ToString().Length)).DefaultIfEmpty(0).Max();
        var coverageWidth = coverage is null ? 0 : manifest.EntryPoints
            .SelectMany(e => e.Requirements.Select(r => Label(coverage.For(e).For(r))).Concat(e.GlobalRequirements.Select(r => Label(coverage.For(e).For(r)))))
            .Concat(manifest.Unreached.SelectMany(m => m.Requirements.Select(r => Label(coverage.For(m).For(r))).Concat(m.GlobalRequirements.Select(r => Label(coverage.For(m).For(r))))))
            .Select(l => l.Length).DefaultIfEmpty(0).Max();

        void Row(string table, string privilege, RunsAs runsAs, RequirementBasis requirementBasis, IReadOnlyList<RequirementSource> requirementSources, RequirementCoverage? result)
        {
            var basis = requirementBasis == RequirementBasis.Direct ? "" : $"  ({Kebab(requirementBasis)})";
            var sources = string.Join(", ", requirementSources.Select(Source));
            var row = $"{table.PadRight(tableWidth)}  {privilege.PadRight(privilegeWidth)}  {runsAs.ToString().PadRight(runsAsWidth)}  ";
            if (result is not null)
            {
                var reason = result.Status == CoverageStatus.Suppressed && result.Reason is { } text ? $"  suppressed: {text}" : "";
                writer.Write($"  {Mark(result.Status)} {row}{Label(result).PadRight(coverageWidth)}  {sources}{basis}{reason}\n");
            }
            else
            {
                writer.Write($"  {row}{sources}{basis}\n");
            }
        }

        void Section(
            string header,
            IReadOnlyList<Requirement> sectionRequirements,
            IReadOnlyList<GlobalRequirement> sectionGlobalRequirements,
            IReadOnlyList<UnresolvedOperation> unresolved,
            IReadOnlyList<Assumption> assumptions,
            SectionCoverage? sectionCoverage)
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
                Row(requirement.Table, requirement.Privilege.ToString(), requirement.RunsAs, requirement.Basis, requirement.Sources, sectionCoverage?.For(requirement));
            }

            foreach (var requirement in sectionGlobalRequirements)
            {
                Row(requirement.Privilege, GlobalColumn, requirement.RunsAs, requirement.Basis, requirement.Sources, sectionCoverage?.For(requirement));
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
            Section(header, entryPoint.Requirements, entryPoint.GlobalRequirements, entryPoint.Unresolved, entryPoint.Assumptions, coverage?.For(entryPoint));
        }

        if (manifest.Unreached.Count > 0)
        {
            writer.Write("Not reached from any entry point\n\n");
            foreach (var method in manifest.Unreached)
            {
                var header = method.Declaration is { } declaration ? $"{method.Id}  {declaration}" : method.Id;
                Section(header, method.Requirements, method.GlobalRequirements, method.Unresolved, method.Assumptions, coverage?.For(method));
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

        var globalPrivileges = globalRequirements.Select(r => r.Privilege).Distinct(StringComparer.Ordinal).OrderBy(p => p, StringComparer.Ordinal).ToList();
        if (globalPrivileges.Count > 0)
        {
            writer.Write($"Privileges not tied to a table: {string.Join(", ", globalPrivileges)}\n\n");
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
            $"({Count(requirements.Count, "requirement")} counted per entry point and method), " +
            (globalPrivileges.Count > 0 ? $"{Count(globalPrivileges.Count, "privilege")} not tied to a table, " : "") +
            $"{unresolvedCount} unresolved\n");

        if (coverage is not null)
        {
            writer.Write(Result(coverage.Summary) + "\n");
            Missing(coverage.Missing, coverage.MissingGlobal, writer);
            if (NotChecked(coverage.Summary) is { } notChecked) writer.Write(notChecked + "\n");
        }
    }

    /// <summary>
    /// Each missing privilege once, with the roles that lack it and where it is needed, so it doesn't have to be
    /// collected from the sections above.
    /// </summary>
    private static void Missing(IReadOnlyList<MissingPrivilege> missing, IReadOnlyList<MissingGlobalPrivilege> missingGlobal, TextWriter writer)
    {
        var rows = missing
            .Select(m => (Table: m.Table, Privilege: m.Privilege.ToString(), m.RunsAs, m.Roles, m.In))
            .Concat(missingGlobal.Select(m => (Table: m.Privilege, Privilege: GlobalColumn, m.RunsAs, m.Roles, m.In)))
            .ToList();
        if (rows.Count == 0) return;

        var tableWidth = rows.Max(m => m.Table.Length);
        var privilegeWidth = rows.Max(m => m.Privilege.Length);
        var runsAsWidth = rows.Max(m => m.RunsAs.ToString().Length);
        var rolesWidth = rows.Max(m => m.Roles.ToString().Length);
        writer.Write("Missing privileges:\n");
        foreach (var privilege in rows)
        {
            writer.Write(
                $"  ✘ {privilege.Table.PadRight(tableWidth)}  {privilege.Privilege.PadRight(privilegeWidth)}  " +
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
        if (summary.Unverifiable > 0)
        {
            parts.Add($"{Count(summary.Unverifiable, "privilege")} not tied to a table (the role metadata predates them; regenerate it)");
        }
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
        CoverageStatus.Unverifiable => "unverifiable",
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
        RequirementBasis.FilterUnknown => "filter-unknown",
        RequirementBasis.ExecutePrivilege => "execute-privilege",
        _ => basis.ToString().ToLowerInvariant(),
    };

    /// <summary>
    /// The privilege column of a privilege not tied to a table, whose name is in the table column.
    /// </summary>
    private const string GlobalColumn = "global";

    private static string Count(int count, string noun) => $"{count} {noun}{(count == 1 ? "" : "s")}";
}
