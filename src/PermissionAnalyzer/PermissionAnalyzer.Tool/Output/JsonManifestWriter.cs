using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>
/// The manifest as JSON, for diffing in CI and for other tools. Privileges keep their Dataverse names
/// (<c>AppendTo</c>); a basis is written in kebab case (<c>all-implementations</c>).
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
            new JsonStringEnumConverter<RequirementBasis>(JsonNamingPolicy.KebabCaseLower),
        },
    };

    public void Write(Manifest manifest, TextWriter writer)
    {
        var document = new ManifestDocument(
            manifest.Methods.Select(m => new MethodDocument(
                m.Id,
                m.Declaration is { } declaration ? Location(declaration) : null,
                m.Requirements.Select(r => new RequirementDocument(r.Table, r.Privilege, r.Basis, r.Sources.Select(Location).ToList())).ToList(),
                m.Unresolved.Select(u => new UnresolvedDocument(u.Operation, u.Privileges, u.Reason, Location(u.Source))).ToList()))
                .ToList(),
            manifest.NotRoleGoverned);

        writer.Write(JsonSerializer.Serialize(document, Options));
        writer.Write('\n');
    }

    private static LocationDocument Location(SourceLocation location) => new(location.File, location.Line, location.Column);

    private sealed record ManifestDocument(IReadOnlyList<MethodDocument> Methods, IReadOnlyList<string>? NotRoleGoverned);

    private sealed record MethodDocument(
        string Id,
        LocationDocument? Declaration,
        IReadOnlyList<RequirementDocument> Requirements,
        IReadOnlyList<UnresolvedDocument> Unresolved);

    private sealed record RequirementDocument(string Table, Privilege Privilege, RequirementBasis Basis, IReadOnlyList<LocationDocument> Sources);

    private sealed record UnresolvedDocument(string Operation, IReadOnlyList<Privilege> Privileges, string Reason, LocationDocument Source);

    private sealed record LocationDocument(string File, int Line, int Column);
}
