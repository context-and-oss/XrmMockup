using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Extraction;
using XrmMockup.PermissionAnalyzer.Core.Graph;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Output;

namespace XrmMockup.PermissionAnalyzer.Tool.Snapshots;

/// <summary>
/// A complete extraction, not a filtered report. Schema changes include changes to extraction/evaluation
/// semantics: bump CurrentSchemaVersion when a saved graph can no longer be evaluated equivalently.
/// Role definitions are an optional offline baseline, never cached coverage or live assignments.
/// </summary>
internal sealed record AnalysisSnapshot(
    string Kind,
    int SchemaVersion,
    string AnalyzerVersion,
    string SourceRoot,
    AnalysisProvenance Inputs,
    ExtractedAnalysis Graph,
    IReadOnlyList<UnresolvedOperation> Diagnostics,
    IReadOnlyList<SnapshotRole>? Roles)
{
    internal const int CurrentSchemaVersion = 1;
    private const string SnapshotKind = "xrmmockup-permissions-snapshot";

    internal static AnalysisSnapshot Create(ManifestBuilder builder, AnalysisProvenance inputs,
        IReadOnlyList<UnresolvedOperation> diagnostics, IReadOnlyList<RoleDefinition>? roles,
        string version, string sourceRoot) => new(SnapshotKind, CurrentSchemaVersion, version, sourceRoot,
            inputs, builder.Extracted(), diagnostics.OrderBy(d => d.Source.Location)
                .ThenBy(d => d.Operation, StringComparer.Ordinal).ThenBy(d => d.Reason, StringComparer.Ordinal).ToList(),
            roles?.OrderBy(r => r.Name, StringComparer.Ordinal).ThenBy(r => r.RoleId)
                .Select(SnapshotRole.Of).ToList());

    internal IReadOnlyList<RoleDefinition>? RoleDefinitions() => Roles?.Select(r => r.Definition()).ToList();

    internal static AnalysisSnapshot Read(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(path), new JsonDocumentOptions { MaxDepth = 256 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("kind", out var kind) || kind.GetString() != SnapshotKind)
                throw new ToolException("Not a permission analysis snapshot. Create one with --save-snapshot; a JSON report cannot be replayed.");
            if (!root.TryGetProperty("schemaVersion", out var version) || !version.TryGetInt32(out var number) || number != CurrentSchemaVersion)
                throw new ToolException($"Unsupported permission snapshot schema. This analyzer supports version {CurrentSchemaVersion}; recreate the snapshot with this analyzer.");
            ValidateArrays(root);
            return root.Deserialize<AnalysisSnapshot>(Options) ?? throw new JsonException("Empty snapshot");
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            throw new ToolException($"Cannot read permission snapshot '{path}': {ex.Message}");
        }
    }

    internal async Task SaveAsync(string path, CancellationToken cancellationToken)
    {
        // Sort every dictionary/property ordinally, independently of serializer insertion order or culture.
        var element = JsonSerializer.SerializeToElement(this, Options);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, NewLine = "\n" }))
            Canonical(element, writer);
        var bytes = stream.ToArray();
        // Write atomically so a cancelled/failed capture doesn't destroy an existing snapshot.
        var fullPath = Path.GetFullPath(path);
        var temporary = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, Encoding.UTF8.GetString(bytes) + "\n", new UTF8Encoding(false), cancellationToken);
            File.Move(temporary, fullPath, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static void ValidateArrays(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Null) throw new JsonException("Snapshot arrays cannot contain null facts");
                ValidateArrays(item);
            }
        else if (value.ValueKind == JsonValueKind.Object)
            foreach (var property in value.EnumerateObject()) ValidateArrays(property.Value);
    }

    private static void Canonical(JsonElement value, Utf8JsonWriter writer)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            foreach (var property in value.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
            { writer.WritePropertyName(property.Name); Canonical(property.Value, writer); }
            writer.WriteEndObject();
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            foreach (var item in value.EnumerateArray()) Canonical(item, writer);
            writer.WriteEndArray();
        }
        else value.WriteTo(writer);
    }

    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(info =>
        {
            if (info.Type != typeof(ServiceOrigin)) return;
            info.PolymorphismOptions = new JsonPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "origin",
                DerivedTypes =
                {
                    new JsonDerivedType(typeof(FixedOrigin), "fixed"),
                    new JsonDerivedType(typeof(ParameterOrigin), "parameter"),
                    new JsonDerivedType(typeof(FieldOrigin), "field"),
                    new JsonDerivedType(typeof(InjectedOrigin), "injected"),
                    new JsonDerivedType(typeof(ReturnedOrigin), "returned"),
                    new JsonDerivedType(typeof(UnknownOrigin), "unknown"),
                }
            };
        });
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = resolver,
            // Canonical sorting can put a discriminator after a normal property.
            AllowOutOfOrderMetadataProperties = true,
            RespectRequiredConstructorParameters = true,
            RespectNullableAnnotations = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            MaxDepth = 256,
            Converters = { new JsonStringEnumConverter(allowIntegerValues: false), new NodeConverter(), new TypeConverter(), new TableConverter() }
        };
    }

    private sealed record NodeData(string Id, string Member, string Display, string? Type, bool IsConstructor,
        SourceLocation? Declaration, SourceLocation? MemberDeclaration, IReadOnlyList<AccessFact> Accesses,
        IReadOnlyList<CallFact> Calls, IReadOnlyList<string> Lambdas, IReadOnlyList<IReadOnlyList<ServiceOrigin>> Returns,
        IReadOnlyDictionary<TableShape, TableResolution> ReturnTables, IReadOnlyList<FieldWrite> FieldWrites);

    private sealed class NodeConverter : JsonConverter<NodeSummary>
    {
        public override NodeSummary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var data = JsonSerializer.Deserialize<NodeData>(ref reader, options) ?? throw new JsonException("Missing node");
            var node = new NodeSummary(data.Id, data.Member, data.Display, data.Type, data.IsConstructor, data.Declaration, data.MemberDeclaration);
            node.Accesses.AddRange(data.Accesses); node.Calls.AddRange(data.Calls); node.Lambdas.UnionWith(data.Lambdas);
            node.Returns.AddRange(data.Returns); node.FieldWrites.AddRange(data.FieldWrites);
            foreach (var pair in data.ReturnTables) node.ReturnTables.Add(pair.Key, pair.Value);
            return node;
        }
        public override void Write(Utf8JsonWriter writer, NodeSummary node, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, new NodeData(node.Id, node.Member, node.Display, node.Type, node.IsConstructor,
                node.Declaration, node.MemberDeclaration,
                // Fact order is meaningful when evaluation reaches its visit/depth limits. Keep it
                // exactly as extracted, like constructor field writes; never change it during replay.
                node.Accesses, node.Calls, node.Lambdas.ToList(), node.Returns,
                node.ReturnTables, node.FieldWrites), options);
    }

    private sealed record TypeData(string Id, bool IsAbstract, bool IsInterface, IReadOnlyList<string> Ancestors,
        IReadOnlyDictionary<string, string> Implementations, IReadOnlyDictionary<string, IReadOnlyList<int>> AncestorTypeArguments,
        IReadOnlyList<string> Constructors, IReadOnlyList<FieldWrite> Initializers);

    private sealed class TypeConverter : JsonConverter<TypeSummary>
    {
        public override TypeSummary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var data = JsonSerializer.Deserialize<TypeData>(ref reader, options) ?? throw new JsonException("Missing type");
            var type = new TypeSummary(data.Id, data.IsAbstract, data.IsInterface, data.Ancestors);
            foreach (var pair in data.Implementations) type.Implementations.Add(pair.Key, pair.Value);
            foreach (var pair in data.AncestorTypeArguments) type.AncestorTypeArguments.Add(pair.Key, pair.Value);
            type.Constructors.UnionWith(data.Constructors); type.Initializers.AddRange(data.Initializers);
            return type;
        }
        public override void Write(Utf8JsonWriter writer, TypeSummary type, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, new TypeData(type.Id, type.IsAbstract, type.IsInterface,
                type.Ancestors.OrderBy(a => a, StringComparer.Ordinal).ToList(), type.Implementations,
                type.AncestorTypeArguments, type.Constructors.ToList(), type.Initializers), options);
    }

    private sealed record TableData(IReadOnlyCollection<string> Tables, IReadOnlyCollection<string> Reasons,
        IReadOnlyCollection<string> LookupTargets, IReadOnlyCollection<TableSlot> Slots, IReadOnlyCollection<ReturnedTables> Returns,
        IReadOnlyDictionary<string, TableResolution> Fired, IReadOnlyCollection<GlobalNeed> Globals,
        IReadOnlyCollection<string> Assumptions, TableResolution? Columns, PluginBypass Bypass);

    private sealed class TableConverter : JsonConverter<TableResolution>
    {
        public override TableResolution Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var data = JsonSerializer.Deserialize<TableData>(ref reader, options) ?? throw new JsonException("Missing table resolution");
            return TableResolution.Restore(data.Tables, data.Reasons, data.LookupTargets, data.Slots, data.Returns,
                data.Fired, data.Globals, data.Assumptions, data.Columns, data.Bypass);
        }
        public override void Write(Utf8JsonWriter writer, TableResolution table, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, new TableData(table.Tables, table.Reasons, table.LookupTargets,
                table.Slots, table.Returns, table.Fired, table.Globals, table.Assumptions, table.Columns, table.Bypass), options);
    }
}

internal sealed record SnapshotRole(string Name, Guid? RoleId,
    IReadOnlyDictionary<string, IReadOnlyDictionary<Privilege, PrivilegeDepth>> Privileges,
    IReadOnlyDictionary<string, PrivilegeDepth>? GlobalPrivileges)
{
    internal static SnapshotRole Of(RoleDefinition role) => new(role.Name, role.RoleId, role.Privileges, role.GlobalPrivileges);
    internal RoleDefinition Definition() => new(Name, Privileges, GlobalPrivileges, RoleId);
}
