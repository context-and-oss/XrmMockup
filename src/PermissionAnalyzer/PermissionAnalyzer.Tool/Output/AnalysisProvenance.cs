using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using XrmMockup.PermissionAnalyzer.Tool.Options;
using XrmMockup.PermissionAnalyzer.Tool.Workspace;

namespace XrmMockup.PermissionAnalyzer.Tool.Output;

/// <summary>Stable inputs only. Authentication and live lookup timestamps are deliberately excluded.</summary>
internal sealed record AnalysisProvenance(string Target, IReadOnlyList<LoadedProject> Projects, IReadOnlyList<string> Exclusions,
    string ConfigurationHash, IReadOnlyList<InputFingerprint> Metadata)
{
    public static AnalysisProvenance Create(AnalysisSettings settings, PermissionsConfiguration permissions,
        IReadOnlyList<LoadedProject> projects, bool noRoles)
    {
        var root = Path.GetDirectoryName(settings.Target)!;
        string Relative(string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
        // Role assignments belong to coverage, not the role-free requirement artifact.
        var effective = noRoles ? permissions with { Principals = [], Users = [], Suppress = [] } : permissions;
        var configurationHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalJson(effective)))).ToLowerInvariant();
        var files = settings.MetadataDirectory is { } directory
            ? Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                .Where(f => !noRoles || !Path.GetRelativePath(directory, f).Replace('\\', '/').StartsWith("SecurityRoles/", StringComparison.Ordinal))
                .OrderBy(Relative, StringComparer.Ordinal)
                .Select(f => new InputFingerprint(Relative(f), Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f))).ToLowerInvariant())).ToList()
            : [];
        return new AnalysisProvenance(Relative(settings.Target), projects, settings.ExcludeProjects.Distinct(StringComparer.Ordinal).OrderBy(e => e, StringComparer.Ordinal).ToList(), configurationHash, files);
    }

    private static string CanonicalJson(PermissionsConfiguration configuration)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(configuration));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) Write(document.RootElement, writer);
        return Encoding.UTF8.GetString(stream.ToArray());

        static void Write(JsonElement value, Utf8JsonWriter writer)
        {
            if (value.ValueKind == JsonValueKind.Object)
            {
                writer.WriteStartObject();
                foreach (var property in value.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                { writer.WritePropertyName(property.Name); Write(property.Value, writer); }
                writer.WriteEndObject();
            }
            else if (value.ValueKind == JsonValueKind.Array)
            {
                writer.WriteStartArray();
                foreach (var item in value.EnumerateArray()) Write(item, writer);
                writer.WriteEndArray();
            }
            else value.WriteTo(writer);
        }
    }
}

internal sealed record InputFingerprint(string Path, string Sha256);
