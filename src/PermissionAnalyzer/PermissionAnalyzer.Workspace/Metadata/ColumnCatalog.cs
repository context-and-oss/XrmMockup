using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace XrmMockup.PermissionAnalyzer.Tool.Metadata;

/// <summary>Presentation-only metadata, not extraction rules or evidence of record-level access.</summary>
internal sealed record ColumnDefinition(string LogicalName, string? DisplayName);
internal sealed record TableColumnCatalog(string Table, string? DisplayName, IReadOnlyList<ColumnDefinition> Columns);

internal static class ColumnCatalog
{
    internal static IReadOnlyList<TableColumnCatalog> Convert(IEnumerable<EntityMetadata> entities) => entities
        .Where(e => !string.IsNullOrWhiteSpace(e.LogicalName) && e.Attributes is not null)
        .Select(e => new TableColumnCatalog(e.LogicalName, Label(e.DisplayName), e.Attributes!
            .Where(a => !string.IsNullOrWhiteSpace(a.LogicalName))
            .GroupBy(a => a.LogicalName, StringComparer.Ordinal)
            .Select(g => new ColumnDefinition(g.Key, Label(g.First().DisplayName)))
            .OrderBy(c => c.LogicalName, StringComparer.Ordinal).ToList()))
        .GroupBy(t => t.Table, StringComparer.Ordinal).Select(g => g.First())
        .OrderBy(t => t.Table, StringComparer.Ordinal).ToList();

    // An external catalog replaces whole tables, never blends stale and current columns.
    // Tables absent from the supplied metadata retain their snapshot catalog.
    internal static IReadOnlyList<TableColumnCatalog> Merge(IReadOnlyList<TableColumnCatalog>? baseline,
        IReadOnlyList<TableColumnCatalog> supplied) => (baseline ?? []).Concat(supplied)
        .GroupBy(t => t.Table, StringComparer.Ordinal).Select(g => g.Last())
        .OrderBy(t => t.Table, StringComparer.Ordinal).ToList();

    private static string? Label(Label? label) => label?.UserLocalizedLabel?.Label
        ?? label?.LocalizedLabels?.OrderBy(l => l.LanguageCode).ThenBy(l => l.Label, StringComparer.Ordinal)
            .Select(l => l.Label).FirstOrDefault();
}
