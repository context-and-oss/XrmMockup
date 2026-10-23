using System.Text.Json;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// The format of the report, by <c>--format</c>.
/// </summary>
internal enum ReportFormat
{
    Text,
    Json,
    Markdown,
    Sarif,
}

/// <summary>
/// How much of the text report to show.
/// </summary>
internal enum Verbosity
{
    /// <summary>
    /// Only what to change: per role set, the privileges it lacks, a line per table. No entry points or sources.
    /// The default.
    /// </summary>
    Minimal,

    /// <summary>
    /// Also, per role set, the entry points and methods that need its privileges, those that need the same ones
    /// listed together.
    /// </summary>
    Compact,

    /// <summary>
    /// What needs attention: requirements the roles don't grant or that weren't checked, and unresolved operations.
    /// Without roles, that is every requirement. Sources are shown without their call paths.
    /// </summary>
    Normal,

    /// <summary>
    /// Also the requirements the roles grant, assumptions, the steps an entry point fires and the privileges per table.
    /// </summary>
    Detailed,

    /// <summary>
    /// Everything, with every source and its call path.
    /// </summary>
    Full,
}

/// <summary>
/// What the report shows. Both apply after analysis, so they never change what is computed, the counts or the exit
/// code. JSON ignores <see cref="Verbosity"/>, so it is always complete, but is narrowed by <see cref="Filter"/>.
/// </summary>
internal sealed record ReportOptions(Verbosity Verbosity = Verbosity.Minimal, ReportFilter? Filter = null)
{
    public static ReportOptions Default { get; } = new();

    public ReportFilter Filter { get; init; } = Filter ?? ReportFilter.None;
}

/// <summary>
/// Narrows the report to some tables, privileges, identities or bases. Each list matches any of its values,
/// ignoring case, and an empty list matches everything. A privilege not tied to a table has no table, so a table
/// filter leaves it out; an unresolved operation has no known table or basis, so only the identity and privilege
/// filters apply to it.
/// </summary>
internal sealed class ReportFilter(
    IReadOnlyCollection<string> tables,
    IReadOnlyCollection<string> privileges,
    IReadOnlyCollection<RunsAsKind> identities,
    IReadOnlyCollection<RequirementBasis> bases)
{
    public static ReportFilter None { get; } = new([], [], [], []);

    public bool IsEmpty => tables.Count + privileges.Count + identities.Count + bases.Count == 0;

    public bool Matches(Requirement requirement) =>
        Has(tables, requirement.Table) && Has(privileges, requirement.Privilege.ToString()) && Identity(requirement.RunsAs) && Basis(requirement.Basis);

    public bool MatchesTable(string table) => Has(tables, table);

    public bool Matches(GlobalRequirement requirement) =>
        tables.Count == 0 && Has(privileges, requirement.Privilege) && Identity(requirement.RunsAs) && Basis(requirement.Basis);

    public bool Matches(UnresolvedOperation operation) =>
        Identity(operation.RunsAs) && (privileges.Count == 0 || operation.Privileges.Count == 0 || operation.Privileges.Any(p => Has(privileges, p.ToString())));

    /// <summary>
    /// The section has a requirement or unresolved operation the filter matches.
    /// </summary>
    public bool Matches(IManifestSection section) =>
        section.Requirements.Any(Matches) || section.GlobalRequirements.Any(Matches) || section.Unresolved.Any(Matches);

    /// <summary>
    /// The missing privileges of <paramref name="coverage"/> that some requirement the filter matches is missing,
    /// so that a basis filter, which a merged missing privilege doesn't have, narrows them too.
    /// </summary>
    public (IReadOnlyList<MissingPrivilege> Missing, IReadOnlyList<MissingGlobalPrivilege> MissingGlobal) Missing(Manifest manifest, CoverageReport coverage)
    {
        if (IsEmpty) return (coverage.Missing, coverage.MissingGlobal);

        var missing = manifest.Sections
            .SelectMany(s => PrivilegeNeed.Of(s, coverage.For(s), this))
            .Where(n => n.Coverage!.Status == CoverageStatus.Missing)
            .Select(n => (n.Name, n.TablePrivilege, RunsAs: n.RunsAs with { Assumed = false }))
            .ToHashSet();
        return (
            coverage.Missing.Where(m => missing.Contains((m.Table, m.Privilege, m.RunsAs))).ToList(),
            coverage.MissingGlobal.Where(m => missing.Contains((m.Privilege, null, m.RunsAs))).ToList());
    }

    /// <summary>
    /// The filter as its options, such as <c>--table account, contact; --basis direct</c>.
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string>();
        void Add<T>(string option, IEnumerable<T> values)
        {
            var list = values.Select(v => v is RequirementBasis basis ? BasisNames.Of(basis) : v!.ToString()!).ToList();
            if (list.Count > 0) parts.Add($"{option} {string.Join(", ", list)}");
        }
        Add(CliOptions.Table.Primary, tables);
        Add(CliOptions.Privilege.Primary, privileges);
        Add(CliOptions.RunsAs.Primary, identities);
        Add(CliOptions.Basis.Primary, bases);
        return string.Join("; ", parts);
    }

    private bool Identity(RunsAs runsAs) => identities.Count == 0 || identities.Contains(runsAs.Kind);

    private bool Basis(RequirementBasis basis) => bases.Count == 0 || bases.Contains(basis);

    private static bool Has(IReadOnlyCollection<string> values, string value) =>
        values.Count == 0 || values.Contains(value, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// A basis in kebab case, as the output and <c>--basis</c> write it: <c>all-implementations</c>.
/// </summary>
internal static class BasisNames
{
    public static string Of(RequirementBasis basis) => JsonNamingPolicy.KebabCaseLower.ConvertName(basis.ToString());

    public static IReadOnlyList<string> All { get; } = Enum.GetValues<RequirementBasis>().Select(Of).ToList();

    public static RequirementBasis? Parse(string name) =>
        Enum.GetValues<RequirementBasis>().Cast<RequirementBasis?>().FirstOrDefault(b => string.Equals(Of(b!.Value), name, StringComparison.OrdinalIgnoreCase));
}
