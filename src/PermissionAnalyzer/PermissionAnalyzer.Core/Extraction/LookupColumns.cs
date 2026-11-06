using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Extraction;

/// <summary>
/// The tables each lookup column can refer to, from the metadata. Table and column names are matched ignoring case.
/// </summary>
internal sealed class LookupColumns
{
    private readonly Dictionary<string, SortedSet<string>> _targets = new(StringComparer.OrdinalIgnoreCase);

    public LookupColumns(IEnumerable<LookupColumn>? columns = null)
    {
        foreach (var column in columns ?? [])
        {
            var key = Key(column.Table, column.Column);
            if (!_targets.TryGetValue(key, out var targets)) _targets[key] = targets = new SortedSet<string>(StringComparer.Ordinal);
            targets.UnionWith(column.Targets.Where(t => t.Length > 0).Select(t => t.ToLowerInvariant()));
        }
    }

    /// <summary>
    /// The tables <paramref name="column"/> on <paramref name="table"/> can refer to; empty when the metadata
    /// doesn't know the column.
    /// </summary>
    public IReadOnlyCollection<string> TargetsOf(string table, string column) =>
        _targets.TryGetValue(Key(table, column), out var targets) ? targets : [];

    private static string Key(string table, string column) => table + "\n" + column;
}
