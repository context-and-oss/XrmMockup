namespace XrmMockup.PermissionAnalyzer.Core.Extraction;

/// <summary>
/// The tables a value can refer to. A value can resolve partly: <c>flag ? "account" : name</c> resolves to
/// <c>account</c> plus a reason for the parameter. An empty resolution carries no information yet, such as
/// <c>new Entity()</c> before its <c>LogicalName</c> is set.
/// </summary>
internal sealed class TableResolution
{
    private readonly SortedSet<string> _tables = new(StringComparer.Ordinal);
    private readonly SortedSet<string> _reasons = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> Tables => _tables;

    /// <summary>
    /// Why part of the value couldn't be resolved, one entry per distinct cause.
    /// </summary>
    public IReadOnlyCollection<string> Reasons => _reasons;

    public bool IsEmpty => _tables.Count == 0 && _reasons.Count == 0;

    public static TableResolution Empty => new();

    public static TableResolution Table(string logicalName)
    {
        var result = new TableResolution();
        result._tables.Add(logicalName.ToLowerInvariant());
        return result;
    }

    public static TableResolution Unresolved(string reason)
    {
        var result = new TableResolution();
        result._reasons.Add(reason);
        return result;
    }

    public TableResolution Add(TableResolution other)
    {
        _tables.UnionWith(other._tables);
        _reasons.UnionWith(other._reasons);
        return this;
    }

    /// <summary>
    /// Turns "no information" into an explicit reason, so a final result is never silently empty.
    /// </summary>
    public TableResolution OrUnresolved(string reason) => IsEmpty ? Unresolved(reason) : this;
}
