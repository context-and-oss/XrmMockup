using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// The manifest as JSON, for diffing in CI and for other tools. Privileges and identities keep their names
/// (<c>AppendTo</c>, <c>ApplicationUser</c>); a basis is written in kebab case (<c>all-implementations</c>).
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
            new JsonStringEnumConverter<RequirementBasis>(JsonNamingPolicy.KebabCaseLower),
        },
    };

    public void Write(Manifest manifest, TextWriter writer)
    {
        var document = new ManifestDocument(
            manifest.EntryPoints.Select(e => new EntryPointDocument(
                e.Id,
                e.Kind,
                e.Type,
                e.Assembly,
                e.Trigger is { } trigger ? new TriggerDocument(trigger.Message, trigger.Table, trigger.Stage, trigger.Binding) : null,
                e.ImpersonatingUserId,
                Declaration(e.Declaration),
                Requirements(e.Requirements),
                Unresolved(e.Unresolved),
                Assumptions(e.Assumptions)))
                .ToList(),
            manifest.Unreached.Select(m => new MethodDocument(
                m.Id,
                Declaration(m.Declaration),
                Requirements(m.Requirements),
                Unresolved(m.Unresolved),
                Assumptions(m.Assumptions)))
                .ToList(),
            manifest.NotRoleGoverned);

        writer.Write(JsonSerializer.Serialize(document, Options));
        writer.Write('\n');
    }

    private static IReadOnlyList<RequirementDocument> Requirements(IEnumerable<Requirement> requirements) =>
        requirements.Select(r => new RequirementDocument(
            r.Table,
            r.Privilege,
            r.RunsAs.Kind,
            r.RunsAs.UserId,
            r.RunsAs.Assumed ? true : null,
            r.Basis,
            r.Sources.Select(Source).ToList()))
            .ToList();

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
        IReadOnlyList<string>? NotRoleGoverned);

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
        IReadOnlyList<AssumptionDocument> Assumptions);

    private sealed record TriggerDocument(string? Message, string? Table, string? Stage, string? Binding);

    private sealed record MethodDocument(
        string Id,
        LocationDocument? Declaration,
        IReadOnlyList<RequirementDocument> Requirements,
        IReadOnlyList<UnresolvedDocument> Unresolved,
        IReadOnlyList<AssumptionDocument> Assumptions);

    private sealed record RequirementDocument(
        string Table,
        Privilege Privilege,
        RunsAsKind RunsAs,
        string? UserId,
        bool? Assumed,
        RequirementBasis Basis,
        IReadOnlyList<SourceDocument> Sources);

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
