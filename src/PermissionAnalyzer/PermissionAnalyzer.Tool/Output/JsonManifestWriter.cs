using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// The manifest as JSON, for diffing in CI and for other tools. Privileges and identities keep their names
/// (<c>AppendTo</c>, <c>ApplicationUser</c>); a basis and a coverage status are written in kebab case
/// (<c>all-implementations</c>, <c>not-checked</c>). Coverage properties are only written with coverage.
/// </summary>
internal sealed class JsonManifestWriter : IManifestWriter
{
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
        var document = new ManifestDocument(
            manifest.EntryPoints.Select(e => new EntryPointDocument(
                e.Id,
                e.Kind,
                e.Type,
                e.Assembly,
                e.Trigger is { } trigger ? new TriggerDocument(trigger.Message, trigger.Table, trigger.Stage, trigger.Binding, trigger.FilteredAttributes) : null,
                e.ImpersonatingUserId,
                Declaration(e.Declaration),
                Requirements(e.Requirements, coverage?.For(e)),
                Unresolved(e.Unresolved),
                Assumptions(e.Assumptions),
                e.Triggers.Count > 0 ? e.Triggers : null,
                Roles(coverage?.For(e)),
                Impersonation(coverage?.For(e))))
                .ToList(),
            manifest.Unreached.Select(m => new MethodDocument(
                m.Id,
                Declaration(m.Declaration),
                Requirements(m.Requirements, coverage?.For(m)),
                Unresolved(m.Unresolved),
                Assumptions(m.Assumptions),
                Roles(coverage?.For(m)),
                Impersonation(coverage?.For(m))))
                .ToList(),
            manifest.NotRoleGoverned,
            coverage?.Missing.Select(m => new MissingDocument(m.Table, m.Privilege, m.RunsAs.Kind, m.RunsAs.UserId, m.Roles.Names, m.Roles.Origin, m.In)).ToList(),
            coverage?.Summary is { } summary
                ? new ResultDocument(summary.Granted, summary.NotGoverned, summary.Missing, summary.Suppressed, summary.NotChecked, summary.System, summary.Unresolved, summary.UnresolvedNoRule, summary.UnresolvedCustomApi, summary.Unverifiable, summary.Incomplete)
                : null);

        writer.Write(JsonSerializer.Serialize(document, Options));
        writer.Write('\n');
    }

    private static IReadOnlyList<RequirementDocument> Requirements(IEnumerable<Requirement> requirements, SectionCoverage? coverage) =>
        requirements.Select(r => new RequirementDocument(
            r.Table,
            r.Privilege,
            r.RunsAs.Kind,
            r.RunsAs.UserId,
            r.RunsAs.Assumed ? true : null,
            r.Basis,
            r.Sources.Select(Source).ToList(),
            coverage?.For(r) is { } result ? new CoverageDocument(result.Status, result.Depth, result.Reason) : null))
            .ToList();

    private static IReadOnlyList<IdentityRolesDocument>? Roles(SectionCoverage? coverage) =>
        coverage?.Identities.Select(i => new IdentityRolesDocument(i.RunsAs.Kind, i.RunsAs.UserId, i.Roles?.Names, i.Roles?.Origin, i.Note)).ToList();

    private static ImpersonationDocument? Impersonation(SectionCoverage? coverage) =>
        coverage?.Impersonation is { } impersonation
            ? new ImpersonationDocument(ImpersonationCheck.Privilege, impersonation.RunsAs.Kind, impersonation.RunsAs.Assumed ? true : null, "unverifiable")
            : null;

    private static IReadOnlyList<UnresolvedDocument> Unresolved(IEnumerable<UnresolvedOperation> unresolved) =>
        unresolved.Select(u => new UnresolvedDocument(
            u.Operation,
            u.Privileges,
            u.Reason,
            u.RunsAs.Kind,
            u.RunsAs.UserId,
            u.RunsAs.Assumed ? true : null,
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
        string Table,
        Privilege Privilege,
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
        IReadOnlyList<IdentityRolesDocument>? Roles,
        ImpersonationDocument? Impersonation);

    private sealed record TriggerDocument(string? Message, string? Table, string? Stage, string? Binding, IReadOnlyList<string>? FilteredAttributes);

    private sealed record MethodDocument(
        string Id,
        LocationDocument? Declaration,
        IReadOnlyList<RequirementDocument> Requirements,
        IReadOnlyList<UnresolvedDocument> Unresolved,
        IReadOnlyList<AssumptionDocument> Assumptions,
        IReadOnlyList<IdentityRolesDocument>? Roles,
        ImpersonationDocument? Impersonation);

    private sealed record RequirementDocument(
        string Table,
        Privilege Privilege,
        RunsAsKind RunsAs,
        string? UserId,
        bool? Assumed,
        RequirementBasis Basis,
        IReadOnlyList<SourceDocument> Sources,
        CoverageDocument? Coverage);

    private sealed record CoverageDocument(CoverageStatus Status, PrivilegeDepth? Depth, string? Reason);

    private sealed record IdentityRolesDocument(RunsAsKind RunsAs, string? UserId, IReadOnlyList<string>? Roles, string? Origin, string? Note);

    private sealed record ImpersonationDocument(string Privilege, RunsAsKind RunsAs, bool? Assumed, string Status);

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
