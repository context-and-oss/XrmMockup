using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// The manifest for reading in a terminal: requirements per entry point, the methods no entry point reaches,
/// then the privileges per table and the privileges not tied to a table. With coverage, each requirement is marked
/// granted (✔) or missing (✘), with the roles each identity was checked against. Below <see cref="Verbosity.Full"/>,
/// sources are shown without their call paths; at <see cref="Verbosity.Normal"/> only what needs attention is shown,
/// and a last line says what was left out. <see cref="Verbosity.Minimal"/> and <see cref="Verbosity.Compact"/> replace
/// the sections with the privileges to change (<see cref="RoleChanges"/>). A filter narrows the sections, the
/// privileges per table and the missing privileges; the counts and the result stay those of the whole analysis.
/// </summary>
internal sealed class TextManifestWriter(ReportOptions? options = null) : IManifestWriter
{
    private readonly ReportOptions _options = options ?? ReportOptions.Default;

    /// <summary>
    /// The sources shown for a requirement below <see cref="Verbosity.Full"/>.
    /// </summary>
    private const int ShortSources = 3;

    /// <summary>
    /// The privilege column of a privilege not tied to a table, whose name is in the table column.
    /// </summary>
    private const string GlobalColumn = "global";

    public void Write(Manifest manifest, CoverageReport? coverage, TextWriter writer)
    {
        var filter = _options.Filter;
        var verbosity = _options.Verbosity;
        var hiddenRows = 0;
        var hiddenAssumptions = 0;
        var trimmed = false;

        bool Shown(RequirementCoverage? result)
        {
            var shown = verbosity > Verbosity.Normal || result is null
                || result.Status is CoverageStatus.Missing or CoverageStatus.NotChecked or CoverageStatus.Unverifiable;
            if (!shown) hiddenRows++;
            return shown;
        }

        SectionView View(string header, IReadOnlyList<string> triggers, IManifestSection section)
        {
            var sectionCoverage = coverage?.For(section);
            var rows = PrivilegeNeed.Of(section, sectionCoverage, filter)
                .Select(n => new RowView(n.Name, n.TablePrivilege?.ToString() ?? GlobalColumn, n.RunsAs, n.Basis, n.Sources, n.Coverage))
                .Where(r => Shown(r.Coverage))
                .ToList();
            var assumptions = section.Assumptions;
            var shownUnresolved = section.Unresolved.Where(filter.Matches).ToList();
            var detailed = verbosity > Verbosity.Normal;
            // Every section is listed, as before, unless only what needs attention or matches a filter is shown
            var shown = (detailed && filter.IsEmpty) || rows.Count + shownUnresolved.Count > 0;
            if (!detailed && (shown || filter.IsEmpty)) hiddenAssumptions += assumptions.Count;
            if (detailed && triggers.Count > 0) header += $"\n  fires: {string.Join(", ", triggers)}";
            return new SectionView(header, rows, shownUnresolved, detailed ? assumptions : [], sectionCoverage, shown);
        }

        var entryPoints = manifest.EntryPoints.Select(entryPoint =>
        {
            var header = entryPoint.Declaration is { } declaration ? $"{entryPoint.Id}  {declaration}" : entryPoint.Id;
            if (entryPoint.ImpersonatingUserId is { } user) header += $"  (runs as user {user})";
            return View(header, entryPoint.Triggers, entryPoint);
        }).Where(s => s.Shown).ToList();
        var unreached = manifest.Unreached.Select(method =>
        {
            var header = method.Declaration is { } declaration ? $"{method.Id}  {declaration}" : method.Id;
            return View(header, [], method);
        }).Where(s => s.Shown).ToList();

        var rows = entryPoints.Concat(unreached).SelectMany(s => s.Rows).ToList();
        var tableWidth = rows.Select(r => r.Table.Length).DefaultIfEmpty(0).Max();
        var privilegeWidth = Enum.GetNames<Privilege>().Append(GlobalColumn).Max(n => n.Length);
        var runsAsWidth = rows.Select(r => r.RunsAs.ToString().Length).DefaultIfEmpty(0).Max();
        var coverageWidth = rows.Select(r => r.Coverage is { } result ? Label(result).Length : 0).DefaultIfEmpty(0).Max();

        string Sources(IReadOnlyList<RequirementSource> sources)
        {
            if (verbosity == Verbosity.Full) return string.Join(", ", sources.Select(Source));

            var locations = sources.Select(s => s.Location).Distinct().ToList();
            if (locations.Count > ShortSources || sources.Any(s => s.Path.Count > 1)) trimmed = true;
            var text = string.Join(", ", locations.Take(ShortSources));
            return locations.Count > ShortSources ? $"{text} and {locations.Count - ShortSources} more" : text;
        }

        void Row(RowView row)
        {
            var basis = row.Basis == RequirementBasis.Direct ? "" : $"  ({BasisNames.Of(row.Basis)})";
            var sources = Sources(row.Sources);
            var text = $"{row.Table.PadRight(tableWidth)}  {row.Privilege.PadRight(privilegeWidth)}  {row.RunsAs.ToString().PadRight(runsAsWidth)}  ";
            if (row.Coverage is { } result)
            {
                var reason = result.Status == CoverageStatus.Suppressed && result.Reason is { } why ? $"  suppressed: {why}" : "";
                writer.Write($"  {Mark(result.Status)} {text}{Label(result).PadRight(coverageWidth)}  {sources}{basis}{reason}\n");
            }
            else
            {
                writer.Write($"  {text}{sources}{basis}\n");
            }
        }

        void Section(SectionView section)
        {
            writer.Write(section.Header + "\n");
            if (section.Coverage is not null)
            {
                foreach (var identity in section.Coverage.Identities)
                {
                    var roles = identity.Roles is { } set ? set.ToString() : identity.Note;
                    writer.Write($"  roles for {identity.RunsAs}: {roles}\n");
                }
            }

            foreach (var row in section.Rows)
            {
                Row(row);
            }

            foreach (var operation in section.Unresolved)
            {
                writer.Write($"  ? {operation.Operation}{UnresolvedSites.PrivilegeSuffix(operation)} as {operation.RunsAs}: {operation.Reason}  {Sources([operation.Source])}\n");
            }

            foreach (var assumption in section.Assumptions)
            {
                writer.Write($"  ~ {assumption.Reason}  {Sources([assumption.Source])}\n");
            }
            writer.Write('\n');
        }

        if (verbosity <= Verbosity.Compact)
        {
            Needs(manifest, coverage, filter, verbosity == Verbosity.Compact, writer);
            entryPoints = [];
            unreached = [];
        }

        foreach (var section in entryPoints)
        {
            Section(section);
        }

        if (unreached.Count > 0)
        {
            writer.Write("Not reached from any entry point\n\n");
            foreach (var section in unreached)
            {
                Section(section);
            }
        }

        // With roles, the missing privileges below say what to change, so the summary of everything needed is detail
        if (verbosity > Verbosity.Normal || (coverage is null && verbosity == Verbosity.Normal))
        {
            var byTable = manifest.AllRequirements
                .Where(filter.Matches)
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

            var shownGlobal = manifest.AllGlobalRequirements.Where(filter.Matches).Select(r => r.Privilege).Distinct(StringComparer.Ordinal).OrderBy(p => p, StringComparer.Ordinal).ToList();
            if (shownGlobal.Count > 0)
            {
                writer.Write($"Privileges not tied to a table: {string.Join(", ", shownGlobal)}\n\n");
            }
        }

        if (verbosity > Verbosity.Compact && manifest.NotRoleGoverned?.Where(filter.MatchesTable).ToList() is { Count: > 0 } notGoverned)
        {
            writer.Write($"Not in any security role, so XrmMockup allows every operation: {string.Join(", ", notGoverned)}\n\n");
        }

        writer.Write(Totals(manifest) + "\n");

        if (!filter.IsEmpty)
        {
            writer.Write(Filtered(manifest, filter) + "\n");
        }

        if (coverage is not null)
        {
            writer.Write(Result(coverage.Summary) + "\n");
            var (missing, missingGlobal) = filter.Missing(manifest, coverage);
            // Below normal, the blocks above already say what to change
            if (verbosity > Verbosity.Compact) Missing(missing, missingGlobal, writer);
            if (NotChecked(coverage.Summary) is { } notChecked) writer.Write(notChecked + "\n");
        }

        if (verbosity <= Verbosity.Compact)
        {
            var which = verbosity == Verbosity.Minimal ? "entry points, " : "";
            var adds = verbosity == Verbosity.Minimal ? $"{Name(Verbosity.Compact)} adds the entry points, {Name(Verbosity.Normal)} the sources" : $"{Name(Verbosity.Normal)} adds the sources";
            writer.Write($"Not shown: {which}sources and requirements with nothing to fix. {CliOptions.Verbosity.Primary} {adds}, {Name(Verbosity.Detailed)} everything\n");
        }
        else if (NotShown(hiddenRows, hiddenAssumptions, trimmed) is { } notShown)
        {
            writer.Write(notShown + "\n");
        }
    }

    /// <summary>
    /// The <see cref="RoleChanges"/>, each followed at <see cref="Verbosity.Compact"/> by the entry points that need
    /// its privileges.
    /// </summary>
    private static void Needs(Manifest manifest, CoverageReport? coverage, ReportFilter filter, bool byEntryPoint, TextWriter writer)
    {
        foreach (var block in RoleChanges.Of(manifest, coverage, filter))
        {
            writer.Write($"{block.Heading}:\n");
            Tables(block.Tables, "  ");
            if (byEntryPoint)
            {
                writer.Write("\n  By entry point:\n");
                foreach (var group in block.EntryPoints)
                {
                    writer.Write('\n');
                    foreach (var id in group.Ids)
                    {
                        writer.Write($"    {id}\n");
                    }
                    Tables(group.Tables, "      ");
                }
            }
            writer.Write('\n');
        }

        void Tables(IReadOnlyList<TableChange> tables, string indent)
        {
            var width = tables.Max(t => t.Table.Length);
            foreach (var table in tables)
            {
                writer.Write($"{indent}{table.Table.PadRight(width)}  {(table.IsGlobal ? GlobalColumn : string.Join(", ", table.Privileges))}\n");
            }
        }
    }

    /// <summary>
    /// What was analyzed. A requirement is counted once per entry point or method that needs it; the tables merge
    /// them. The counts are of the whole analysis, whatever is shown.
    /// </summary>
    internal static string Totals(Manifest manifest)
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
    internal static string Filtered(Manifest manifest, ReportFilter filter)
    {
        var all = manifest.AllRequirements.Count() + manifest.AllGlobalRequirements.Count();
        var matching = manifest.AllRequirements.Count(filter.Matches) + manifest.AllGlobalRequirements.Count(filter.Matches);
        return $"Filtered by {filter}: {matching} of {Count(all, "requirement")} match";
    }

    private static string Name(Verbosity verbosity) => verbosity.ToString().ToLowerInvariant();

    /// <summary>
    /// What a lower verbosity left out, and the verbosity that shows it; <c>null</c> when nothing was.
    /// </summary>
    private static string? NotShown(int requirements, int assumptions, bool trimmed)
    {
        var parts = new List<string>();
        if (requirements > 0) parts.Add($"{Count(requirements, "requirement")} with nothing to fix");
        if (assumptions > 0) parts.Add(Count(assumptions, "assumption"));
        if (parts.Count > 0)
        {
            var full = trimmed ? $", {Name(Verbosity.Full)} also adds every source and call path" : "";
            return $"Not shown: {string.Join(", ", parts)}. {CliOptions.Verbosity.Primary} {Name(Verbosity.Detailed)} lists them{full}";
        }
        return trimmed ? $"Not shown: every source and call path. {CliOptions.Verbosity.Primary} {Name(Verbosity.Full)} adds them" : null;
    }

    /// <summary>
    /// A requirement or privilege not tied to a table as one row, the privilege name in the table column.
    /// </summary>
    private sealed record RowView(string Table, string Privilege, RunsAs RunsAs, RequirementBasis Basis, IReadOnlyList<RequirementSource> Sources, RequirementCoverage? Coverage);

    /// <summary>
    /// An entry point or unreached method with what is shown of it.
    /// </summary>
    private sealed record SectionView(
        string Header,
        IReadOnlyList<RowView> Rows,
        IReadOnlyList<UnresolvedOperation> Unresolved,
        IReadOnlyList<Assumption> Assumptions,
        SectionCoverage? Coverage,
        bool Shown);

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
    internal static string Result(CoverageSummary summary)
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
    internal static string? NotChecked(CoverageSummary summary)
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

    internal static string Count(int count, string noun) => $"{count} {noun}{(count == 1 ? "" : "s")}";
}
