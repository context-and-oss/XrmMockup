using DG.Tools.XrmMockup;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Metadata;

/// <summary>
/// Turns the plugin steps that <see cref="MetadataFileLoader"/> reads into registrations.
/// </summary>
internal static class PluginStepLoader
{
    // The mode of an sdkmessageprocessingstep: 0 is synchronous, 1 asynchronous
    private const int AsynchronousMode = 1;

    /// <summary>
    /// The plugin steps with a plugin type and message, sorted by type, message, table and stage.
    /// </summary>
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
