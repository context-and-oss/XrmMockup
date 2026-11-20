using Microsoft.Xrm.Sdk.Metadata;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Metadata;

/// <summary>
/// Turns the entity metadata that <see cref="MetadataFileLoader"/> reads into lookup columns.
/// </summary>
internal static class LookupLoader
{
    /// <summary>
    /// Every lookup column with the tables it can refer to, sorted by table and column.
    /// </summary>
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
