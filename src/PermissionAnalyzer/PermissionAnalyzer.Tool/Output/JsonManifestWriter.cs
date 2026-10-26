using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// The manifest as JSON, for diffing in CI and for other tools. Privileges and identities keep their names
/// (<c>AppendTo</c>, <c>ApplicationUser</c>); a basis and a coverage status are written in kebab case
/// (<c>all-implementations</c>, <c>not-checked</c>). A privilege not tied to a table is a requirement without a
/// table, with the privilege name as its privilege. Coverage properties are only written with coverage. A filter
/// leaves out the requirements and unresolved operations it doesn't match, the entry points and methods left without
/// any and the missing privileges no matching requirement is missing; the result counts stay those of the whole
/// analysis.
/// </summary>
internal sealed class JsonManifestWriter(ReportFilter? filter = null) : IManifestWriter
{
    private readonly ReportFilter _filter = filter ?? ReportFilter.None;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        NewLine = "\n",
        Converters =
        {
            new JsonStringEnumConverter<Privilege>(),
            new JsonStringEnumConverter<RunsAsKind>(),
            new JsonStringEnumConverter<EntryPointKind>(),
            new JsonStringEnumConverter<PrivilegeDepth>(),
            new JsonStringEnumConverter<CoverageStatus>(JsonNamingPolicy.KebabCaseLower),
            new JsonStringEnumConverter<RequirementBasis>(JsonNamingPolicy.KebabCaseLower),
        },
    };

    public void Write(Manifest manifest, CoverageReport? coverage, TextWriter writer)
    {
        (IReadOnlyList<MissingPrivilege> Missing, IReadOnlyList<MissingGlobalPrivilege> MissingGlobal)? missing = coverage is null ? null : _filter.Missing(manifest, coverage);
        var document = new ManifestDocument(
            manifest.EntryPoints.Where(e => _filter.IsEmpty || _filter.Matches(e)).Select(e => new EntryPointDocument(
                e.Id,
                e.Kind,
                e.Type,
                e.Assembly,
                e.Trigger is { } trigger ? new TriggerDocument(trigger.Message, trigger.Table, trigger.Stage, trigger.Binding, trigger.FilteredAttributes) : null,
                e.ImpersonatingUserId,
                Declaration(e.Declaration),
                Requirements(e, coverage?.For(e)),
                Unresolved(e.Unresolved),
                Assumptions(e.Assumptions),
                e.Triggers.Count > 0 ? e.Triggers : null,
                Roles(coverage?.For(e))))
                .ToList(),
            manifest.Unreached.Where(m => _filter.IsEmpty || _filter.Matches(m)).Select(m => new MethodDocument(
                m.Id,
                Declaration(m.Declaration),
                Requirements(m, coverage?.For(m)),
                Unresolved(m.Unresolved),
                Assumptions(m.Assumptions),
                Roles(coverage?.For(m))))
                .ToList(),
            manifest.NotRoleGoverned,
            missing?.Missing.Select(m => Missing(m.Table, m.Privilege.ToString(), m.RunsAs, m.Roles, m.In))
                .Concat(missing.Value.MissingGlobal.Select(m => Missing(null, m.Privilege, m.RunsAs, m.Roles, m.In)))
                .ToList(),
            coverage?.Summary is { } summary
                ? new ResultDocument(summary.Granted, summary.NotGoverned, summary.Missing, summary.Suppressed, summary.NotChecked, summary.System, summary.Unresolved, summary.UnresolvedNoRule, summary.UnresolvedCustomApi, summary.UnresolvedWorkflow, summary.Unverifiable, summary.Incomplete)
                : null);

        writer.Write(JsonSerializer.Serialize(document, Options));
        writer.Write('\n');
    }

    private IReadOnlyList<RequirementDocument> Requirements(IManifestSection section, SectionCoverage? coverage) =>
        PrivilegeNeed.Of(section, coverage, _filter).Select(n => Requirement(n.Table, n.PrivilegeName, n.RunsAs, n.Basis, n.Sources, n.Coverage)).ToList();

    private static RequirementDocument Requirement(string? table, string privilege, RunsAs runsAs, RequirementBasis basis, IReadOnlyList<RequirementSource> sources, RequirementCoverage? coverage) =>
        new(table, privilege, runsAs.Kind, runsAs.UserId, Assumed(runsAs), basis, sources.Select(Source).ToList(), Coverage(coverage));

    /// <summary>
    /// <c>true</c> for an assumed identity, else left out of the JSON.
    /// </summary>
    private static bool? Assumed(RunsAs runsAs) => runsAs.Assumed ? true : null;

    private static MissingDocument Missing(string? table, string privilege, RunsAs runsAs, RoleSet roles, IReadOnlyList<string> @in) =>
        new(table, privilege, runsAs.Kind, runsAs.UserId, roles.Names, roles.Origin, @in);

    private static CoverageDocument? Coverage(RequirementCoverage? result) =>
        result is null ? null : new CoverageDocument(result.Status, result.Depth, result.Reason);

    private static IReadOnlyList<IdentityRolesDocument>? Roles(SectionCoverage? coverage) =>
        coverage?.Identities.Select(i => new IdentityRolesDocument(i.RunsAs.Kind, i.RunsAs.UserId, i.Roles?.Names, i.Roles?.Origin, i.Note)).ToList();

    private IReadOnlyList<UnresolvedDocument> Unresolved(IEnumerable<UnresolvedOperation> unresolved) =>
        unresolved.Where(_filter.Matches).Select(u => new UnresolvedDocument(
            u.Operation,
            u.Privileges,
            u.Reason,
            u.RunsAs.Kind,
            u.RunsAs.UserId,
            Assumed(u.RunsAs),
            Source(u.Source)))
            .ToList();

    private static IReadOnlyList<AssumptionDocument> Assumptions(IEnumerable<Assumption> assumptions) =>
        assumptions.Select(a => new AssumptionDocument(a.Reason, Source(a.Source))).ToList();

    private static SourceDocument Source(RequirementSource source) =>
        new(source.Location.File, source.Location.Line, source.Location.Column, source.Path);

    private static LocationDocument? Declaration(SourceLocation? location) =>
        location is null ? null : new LocationDocument(location.File, location.Line, location.Column);

    private sealed record ManifestDocument(
        IReadOnlyList<EntryPointDocument> EntryPoints,
        IReadOnlyList<MethodDocument> Unreached,
        IReadOnlyList<string>? NotRoleGoverned,
        IReadOnlyList<MissingDocument>? Missing,
        ResultDocument? Result);

    private sealed record MissingDocument(
        string? Table,
        string Privilege,
        RunsAsKind RunsAs,
        string? UserId,
        IReadOnlyList<string> Roles,
        string Origin,
        IReadOnlyList<string> In);

    private sealed record EntryPointDocument(
        string Id,
        EntryPointKind Kind,
        string Type,
        string? Assembly,
        TriggerDocument? Trigger,
        string? ImpersonatingUserId,
        LocationDocument? Declaration,
        IReadOnlyList<RequirementDocument> Requirements,
        IReadOnlyList<UnresolvedDocument> Unresolved,
        IReadOnlyList<AssumptionDocument> Assumptions,
        IReadOnlyList<string>? Triggers,
        IReadOnlyList<IdentityRolesDocument>? Roles);

    private sealed record TriggerDocument(string? Message, string? Table, string? Stage, string? Binding, IReadOnlyList<string>? FilteredAttributes);

    private sealed record MethodDocument(
        string Id,
        LocationDocument? Declaration,
        IReadOnlyList<RequirementDocument> Requirements,
        IReadOnlyList<UnresolvedDocument> Unresolved,
        IReadOnlyList<AssumptionDocument> Assumptions,
        IReadOnlyList<IdentityRolesDocument>? Roles);

    private sealed record RequirementDocument(
        string? Table,
        string Privilege,
        RunsAsKind RunsAs,
        string? UserId,
        bool? Assumed,
        RequirementBasis Basis,
        IReadOnlyList<SourceDocument> Sources,
        CoverageDocument? Coverage);

    private sealed record CoverageDocument(CoverageStatus Status, PrivilegeDepth? Depth, string? Reason);

    private sealed record IdentityRolesDocument(RunsAsKind RunsAs, string? UserId, IReadOnlyList<string>? Roles, string? Origin, string? Note);

    private sealed record ResultDocument(
        int Granted,
        int NotGoverned,
        int Missing,
        int Suppressed,
        int NotChecked,
        int System,
        int Unresolved,
        int UnresolvedNoRule,
        int UnresolvedCustomApi,
        int UnresolvedWorkflow,
        int Unverifiable,
        bool Incomplete);

    private sealed record UnresolvedDocument(
        string Operation,
        IReadOnlyList<Privilege> Privileges,
        string Reason,
        RunsAsKind RunsAs,
        string? UserId,
        bool? Assumed,
        SourceDocument Source);

    private sealed record AssumptionDocument(string Reason, SourceDocument Source);

    private sealed record SourceDocument(string File, int Line, int Column, IReadOnlyList<string> Path);

    private sealed record LocationDocument(string File, int Line, int Column);
}
