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

    public void Write(Manifest manifest, CoverageReport? coverage, TextWriter writer) => new Report(_options, manifest, coverage, writer).Write();

    /// <summary>
    /// One report being written, with what it has left out so far.
    /// </summary>
    private sealed class Report(ReportOptions options, Manifest manifest, CoverageReport? coverage, TextWriter writer)
    {
        private readonly ReportFilter _filter = options.Filter;
        private readonly Verbosity _verbosity = options.Verbosity;
        private int _hiddenRows;
        private int _hiddenAssumptions;
        private bool _trimmed;
        private int _tableWidth;
        private int _privilegeWidth;
        private int _runsAsWidth;
        private int _coverageWidth;

        public void Write()
        {
            if (_verbosity <= Verbosity.Compact) Needs(manifest, coverage, _filter, _verbosity == Verbosity.Compact, writer);
            else Sections();

            // With roles, the missing privileges below say what to change, so the summary of everything needed is detail
            if (_verbosity > Verbosity.Normal || (coverage is null && _verbosity == Verbosity.Normal)) PrivilegesPerTable();

            if (_verbosity > Verbosity.Compact && manifest.NotRoleGoverned?.Where(_filter.MatchesTable).ToList() is { Count: > 0 } notGoverned)
            {
                writer.Write($"Not in any security role, so XrmMockup allows every operation: {string.Join(", ", notGoverned)}\n\n");
            }

            writer.Write(ReportText.Totals(manifest) + "\n");

            if (!_filter.IsEmpty)
            {
                writer.Write(ReportText.Filtered(manifest, _filter) + "\n");
            }

            if (coverage is not null)
            {
                writer.Write(ReportText.Result(coverage.Summary) + "\n");
                var (missing, missingGlobal) = _filter.Missing(manifest, coverage);
                // Below normal, the blocks above already say what to change
                if (_verbosity > Verbosity.Compact) Missing(missing, missingGlobal, writer);
                if (ReportText.NotChecked(coverage.Summary) is { } notChecked) writer.Write(notChecked + "\n");
            }

            if (_verbosity <= Verbosity.Compact)
            {
                var which = _verbosity == Verbosity.Minimal ? "entry points, " : "";
                var adds = _verbosity == Verbosity.Minimal ? $"{Name(Verbosity.Compact)} adds the entry points, {Name(Verbosity.Normal)} the sources" : $"{Name(Verbosity.Normal)} adds the sources";
                writer.Write($"Not shown: {which}sources and requirements with nothing to fix. {CliOptions.Verbosity.Primary} {adds}, {Name(Verbosity.Detailed)} everything\n");
            }
            else if (NotShown(_hiddenRows, _hiddenAssumptions, _trimmed) is { } notShown)
            {
                writer.Write(notShown + "\n");
            }
        }

        /// <summary>
        /// The entry points, then the methods no entry point reaches.
        /// </summary>
        private void Sections()
        {
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
            _tableWidth = rows.Select(r => r.Table.Length).DefaultIfEmpty(0).Max();
            _privilegeWidth = Enum.GetNames<Privilege>().Append(GlobalColumn).Max(n => n.Length);
            _runsAsWidth = rows.Select(r => r.RunsAs.ToString().Length).DefaultIfEmpty(0).Max();
            _coverageWidth = rows.Select(r => r.Coverage is { } result ? Label(result).Length : 0).DefaultIfEmpty(0).Max();

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
        }

        private SectionView View(string header, IReadOnlyList<string> triggers, IManifestSection section)
        {
            var sectionCoverage = coverage?.For(section);
            var rows = PrivilegeNeed.Of(section, sectionCoverage, _filter)
                .Select(n => new RowView(n.Name, n.TablePrivilege?.ToString() ?? GlobalColumn, n.RunsAs, n.Basis, n.Sources, n.Coverage))
                .Where(r => Shown(r.Coverage))
                .ToList();
            var assumptions = section.Assumptions;
            var shownUnresolved = section.Unresolved.Where(_filter.Matches).ToList();
            var detailed = _verbosity > Verbosity.Normal;
            // Every section is listed, as before, unless only what needs attention or matches a filter is shown
            var shown = (detailed && _filter.IsEmpty) || rows.Count + shownUnresolved.Count > 0;
            if (!detailed && (shown || _filter.IsEmpty)) _hiddenAssumptions += assumptions.Count;
            if (detailed && triggers.Count > 0) header += $"\n  fires: {string.Join(", ", triggers)}";
            return new SectionView(header, rows, shownUnresolved, detailed ? assumptions : [], sectionCoverage, shown);
        }

        private bool Shown(RequirementCoverage? result)
        {
            var shown = _verbosity > Verbosity.Normal || result is null
                || result.Status is CoverageStatus.Missing or CoverageStatus.NotChecked or CoverageStatus.Unverifiable;
            if (!shown) _hiddenRows++;
            return shown;
        }

        private void Section(SectionView section)
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

        private void Row(RowView row)
        {
            var basis = row.Basis == RequirementBasis.Direct ? "" : $"  ({BasisNames.Of(row.Basis)})";
            var sources = Sources(row.Sources);
            var text = $"{row.Table.PadRight(_tableWidth)}  {row.Privilege.PadRight(_privilegeWidth)}  {row.RunsAs.ToString().PadRight(_runsAsWidth)}  ";
            if (row.Coverage is { } result)
            {
                var reason = result.Status == CoverageStatus.Suppressed && result.Reason is { } why ? $"  suppressed: {why}" : "";
                writer.Write($"  {Mark(result.Status)} {text}{Label(result).PadRight(_coverageWidth)}  {sources}{basis}{reason}\n");
            }
            else
            {
                writer.Write($"  {text}{sources}{basis}\n");
            }
        }

        private string Sources(IReadOnlyList<RequirementSource> sources)
        {
            if (_verbosity == Verbosity.Full) return string.Join(", ", sources.Select(Source));

            var locations = sources.Select(s => s.Location).Distinct().ToList();
            if (locations.Count > ShortSources || sources.Any(s => s.Path.Count > 1)) _trimmed = true;
            var text = string.Join(", ", locations.Take(ShortSources));
            return locations.Count > ShortSources ? $"{text} and {locations.Count - ShortSources} more" : text;
        }

        private void PrivilegesPerTable()
        {
            var byTable = manifest.AllRequirements
                .Where(_filter.Matches)
                .GroupBy(r => r.Table)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToList();
            if (byTable.Count > 0)
            {
                writer.Write("Privileges per table\n");
                foreach (var table in byTable)
                {
                    var privileges = table.Select(r => r.Privilege).Distinct().OrderBy(p => p);
                    writer.Write($"  {table.Key.PadRight(_tableWidth)}  {string.Join(", ", privileges)}\n");
                }
                writer.Write('\n');
            }

            var shownGlobal = manifest.AllGlobalRequirements.Where(_filter.Matches).Select(r => r.Privilege).Distinct(StringComparer.Ordinal).OrderBy(p => p, StringComparer.Ordinal).ToList();
            if (shownGlobal.Count > 0)
            {
                writer.Write($"Privileges not tied to a table: {string.Join(", ", shownGlobal)}\n\n");
            }
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

    private static string Name(Verbosity verbosity) => verbosity.ToString().ToLowerInvariant();

    /// <summary>
    /// What a lower verbosity left out, and the verbosity that shows it; <c>null</c> when nothing was.
    /// </summary>
    private static string? NotShown(int requirements, int assumptions, bool trimmed)
    {
        var parts = new List<string>();
        if (requirements > 0) parts.Add($"{ReportText.Count(requirements, "requirement")} with nothing to fix");
        if (assumptions > 0) parts.Add(ReportText.Count(assumptions, "assumption"));
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
}
