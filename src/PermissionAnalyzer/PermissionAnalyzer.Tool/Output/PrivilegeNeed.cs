using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// A requirement of an entry point or unreached method as the reports list it: a privilege on a table, or a privilege
/// not tied to a table, with what the roles make of it.
/// </summary>
/// <param name="Name">The table, or the name of a privilege not tied to a table.</param>
/// <param name="TablePrivilege">The privilege on <paramref name="Name"/>; <c>null</c> for a privilege not tied to a table.</param>
internal sealed record PrivilegeNeed(
    string Name,
    Privilege? TablePrivilege,
    RunsAs RunsAs,
    RequirementBasis Basis,
    IReadOnlyList<RequirementSource> Sources,
    RequirementCoverage? Coverage)
{
    public bool IsGlobal => TablePrivilege is null;

    /// <summary>
    /// The table; <c>null</c> for a privilege not tied to a table.
    /// </summary>
    public string? Table => IsGlobal ? null : Name;

    /// <summary>
    /// The privilege, such as <c>Write</c> or <c>prvMerge</c>.
    /// </summary>
    public string PrivilegeName => TablePrivilege?.ToString() ?? Name;

    /// <summary>
    /// The requirements of <paramref name="section"/> that <paramref name="filter"/> matches, those on tables first.
    /// </summary>
    public static IEnumerable<PrivilegeNeed> Of(IManifestSection section, SectionCoverage? coverage, ReportFilter filter) =>
        section.Requirements.Where(filter.Matches)
            .Select(r => new PrivilegeNeed(r.Table, r.Privilege, r.RunsAs, r.Basis, r.Sources, coverage?.For(r)))
            .Concat(section.GlobalRequirements.Where(filter.Matches)
                .Select(r => new PrivilegeNeed(r.Privilege, null, r.RunsAs, r.Basis, r.Sources, coverage?.For(r))));
}
