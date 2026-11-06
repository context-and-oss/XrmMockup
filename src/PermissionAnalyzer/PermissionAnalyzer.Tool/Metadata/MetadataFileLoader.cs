using System.Runtime.Serialization;
using System.Xml;
using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk.Metadata;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Metadata;

/// <summary>
/// What the analysis uses of <c>Metadata/Metadata.xml</c>, written by the metadata tool, and of the other
/// <c>*Metadata.xml</c> files that XrmMockup merges into it.
/// </summary>
/// <param name="Lookups">The lookup columns, from the entity metadata of every file.</param>
/// <param name="PluginSteps">The plugin steps, from <c>Metadata.xml</c>.</param>
internal sealed record MetadataFileContents(IReadOnlyList<LookupColumn> Lookups, IReadOnlyList<PluginStepRegistration> PluginSteps);

/// <summary>
/// Reads the metadata files in one pass each. A file can be hundreds of megabytes, almost all of it entity metadata,
/// so only the <c>EntityMetadata</c> and <c>Plugins</c> elements are deserialized and the rest is skipped.
/// </summary>
internal static class MetadataFileLoader
{
    public const string MetadataFile = "Metadata.xml";

    // The data contract namespace of the XrmMockup metadata files
    internal const string Namespace = "http://schemas.datacontract.org/2004/07/DG.Tools.XrmMockup";

    private const string EntityMetadataElement = "EntityMetadata";
    private const string PluginsElement = "Plugins";

    /// <summary>
    /// The lookup columns and plugin steps in the metadata folder. Empty when there's no metadata file.
    /// </summary>
    public static MetadataFileContents Load(string metadataDirectory)
    {
        if (!Directory.Exists(metadataDirectory)) return new([], []);

        var entitySerializer = new DataContractSerializer(typeof(Dictionary<string, EntityMetadata>), EntityMetadataElement, Namespace);
        var pluginSerializer = new DataContractSerializer(typeof(List<MetaPlugin>), PluginsElement, Namespace);
        // As in XrmMockup, the main file goes first and an entity that an earlier file has is ignored
        var entities = new Dictionary<string, EntityMetadata>();
        var plugins = new List<MetaPlugin>();
        var files = Directory.GetFiles(metadataDirectory, "*" + MetadataFile)
            .OrderBy(f => Path.GetFileName(f) == MetadataFile ? 0 : 1)
            .ThenBy(f => f, StringComparer.Ordinal);
        foreach (var file in files)
        {
            // Plugin steps only come from the main file, as in XrmMockup
            var withPlugins = Path.GetFileName(file) == MetadataFile;
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreWhitespace = true });
            if (reader.MoveToContent() != XmlNodeType.Element || reader.IsEmptyElement) continue;

            reader.ReadStartElement();
            bool readEntities = false, readPlugins = !withPlugins;
            while (!(readEntities && readPlugins) && reader.MoveToContent() == XmlNodeType.Element)
            {
                if (reader.NamespaceURI != Namespace)
                {
                    reader.Skip();
                }
                else if (reader.LocalName == EntityMetadataElement)
                {
                    if (entitySerializer.ReadObject(reader) is Dictionary<string, EntityMetadata> metadata)
                    {
                        foreach (var entity in metadata) entities.TryAdd(entity.Key, entity.Value);
                    }
                    readEntities = true;
                }
                else if (reader.LocalName == PluginsElement && withPlugins)
                {
                    if (pluginSerializer.ReadObject(reader) is List<MetaPlugin> steps) plugins.AddRange(steps);
                    readPlugins = true;
                }
                else
                {
                    reader.Skip();
                }
            }
        }
        return new(LookupLoader.Convert(entities.Values), PluginStepLoader.Convert(plugins));
    }
}
