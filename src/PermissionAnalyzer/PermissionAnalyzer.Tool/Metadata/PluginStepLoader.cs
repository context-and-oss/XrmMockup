using System.Runtime.Serialization;
using System.Xml;
using DG.Tools.XrmMockup;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Metadata;

/// <summary>
/// Reads the plugin steps in <c>Metadata/Metadata.xml</c>, written by the metadata tool. Only the
/// <c>Plugins</c> element is read, since the file also holds all entity metadata.
/// </summary>
internal static class PluginStepLoader
{
    public const string MetadataFile = "Metadata.xml";

    // The mode of an sdkmessageprocessingstep: 0 is synchronous, 1 asynchronous
    private const int AsynchronousMode = 1;

    // The data contract namespace of the XrmMockup metadata files
    internal const string Namespace = "http://schemas.datacontract.org/2004/07/DG.Tools.XrmMockup";

    /// <summary>
    /// Every plugin step in the metadata folder. Empty when there's no metadata file or it has no steps.
    /// </summary>
    public static IReadOnlyList<PluginStepRegistration> Load(string metadataDirectory)
    {
        var path = Path.Combine(metadataDirectory, MetadataFile);
        if (!File.Exists(path)) return [];

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreWhitespace = true });
        if (!reader.ReadToFollowing("Plugins", Namespace)) return [];

        var serializer = new DataContractSerializer(typeof(List<MetaPlugin>), "Plugins", Namespace);
        var plugins = serializer.ReadObject(reader) as List<MetaPlugin> ?? [];
        return Convert(plugins);
    }

    internal static IReadOnlyList<PluginStepRegistration> Convert(IEnumerable<MetaPlugin> plugins) =>
        plugins
            .Where(p => !string.IsNullOrEmpty(p.AssemblyName) && !string.IsNullOrEmpty(p.MessageName))
            .Select(p => new PluginStepRegistration(
                p.AssemblyName,
                string.IsNullOrEmpty(p.PluginTypeAssemblyName) ? null : p.PluginTypeAssemblyName,
                p.MessageName,
                string.IsNullOrEmpty(p.PrimaryEntity) || p.PrimaryEntity == "none" ? null : p.PrimaryEntity,
                p.Stage,
                p.ImpersonatingUserId?.ToString(),
                FilteredAttributes(p.FilteredAttributes),
                p.Mode == AsynchronousMode))
            .OrderBy(s => s.TypeName, StringComparer.Ordinal)
            .ThenBy(s => s.Message, StringComparer.Ordinal)
            .ThenBy(s => s.Table, StringComparer.Ordinal)
            .ThenBy(s => s.Stage)
            .ToList();

    /// <summary>
    /// The comma-separated columns a step is filtered on, lowercased and sorted; <c>null</c> for none.
    /// </summary>
    private static IReadOnlyList<string>? FilteredAttributes(string? attributes)
    {
        var columns = (attributes ?? "")
            .Split(',')
            .Select(a => a.Trim().ToLowerInvariant())
            .Where(a => a.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(a => a, StringComparer.Ordinal)
            .ToList();
        return columns.Count > 0 ? columns : null;
    }
}
