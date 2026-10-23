using System.Runtime.Serialization;
using System.Xml;
using Microsoft.Xrm.Sdk.Metadata;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Metadata;

/// <summary>
/// Reads the lookup columns in the entity metadata of <c>Metadata/Metadata.xml</c>, and of the other
/// <c>*Metadata.xml</c> files that XrmMockup merges into it. Only the <c>EntityMetadata</c> element is read.
/// </summary>
internal static class LookupLoader
{
    /// <summary>
    /// Every lookup column with the tables it can refer to, sorted by table and column. Empty when there's no
    /// metadata file.
    /// </summary>
    public static IReadOnlyList<LookupColumn> Load(string metadataDirectory)
    {
        if (!Directory.Exists(metadataDirectory)) return [];

        var serializer = new DataContractSerializer(typeof(Dictionary<string, EntityMetadata>), "EntityMetadata", PluginStepLoader.Namespace);
        var entities = new List<EntityMetadata>();
        foreach (var file in Directory.GetFiles(metadataDirectory, "*" + PluginStepLoader.MetadataFile).OrderBy(f => f, StringComparer.Ordinal))
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreWhitespace = true });
            if (!reader.ReadToFollowing("EntityMetadata", PluginStepLoader.Namespace)) continue;
            if (serializer.ReadObject(reader) is Dictionary<string, EntityMetadata> metadata) entities.AddRange(metadata.Values);
        }
        return Convert(entities);
    }

    internal static IReadOnlyList<LookupColumn> Convert(IEnumerable<EntityMetadata> entities) =>
        entities
            .Where(e => !string.IsNullOrEmpty(e.LogicalName))
            .SelectMany(e => (e.Attributes ?? [])
                .OfType<LookupAttributeMetadata>()
                .Where(a => !string.IsNullOrEmpty(a.LogicalName) && a.Targets is { Length: > 0 })
                .Select(a => new LookupColumn(e.LogicalName, a.LogicalName, a.Targets.OrderBy(t => t, StringComparer.Ordinal).ToList())))
            .GroupBy(c => (c.Table, c.Column))
            .Select(g => g.First())
            .OrderBy(c => c.Table, StringComparer.Ordinal)
            .ThenBy(c => c.Column, StringComparer.Ordinal)
            .ToList();
}
