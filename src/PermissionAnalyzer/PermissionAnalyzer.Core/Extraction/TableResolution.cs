namespace XrmMockup.PermissionAnalyzer.Core.Extraction;

/// <summary>
/// The tables a value can refer to. A value can resolve partly: <c>flag ? "account" : name</c> resolves to
/// <c>account</c> plus a reason for the parameter. An empty resolution carries no information yet, such as
/// <c>new Entity()</c> before its <c>LogicalName</c> is set.
/// </summary>
/// <remarks>
/// A value that comes from a method parameter is kept as a <see cref="TableSlot"/>, so the call graph can fill
/// it in from the argument at each call site. Where no call site binds it, the slot's reason is reported.
/// </remarks>
internal sealed class TableResolution
{
    private readonly SortedSet<string> _tables = new(StringComparer.Ordinal);
    private readonly SortedSet<string> _reasons = new(StringComparer.Ordinal);
    private readonly SortedSet<TableSlot> _slots = new();

    public IReadOnlyCollection<string> Tables => _tables;

    /// <summary>
    /// Why part of the value couldn't be resolved, one entry per distinct cause.
    /// </summary>
    public IReadOnlyCollection<string> Reasons => _reasons;

    /// <summary>
    /// Parameters the value depends on, resolved per call site.
    /// </summary>
    public IReadOnlyCollection<TableSlot> Slots => _slots;

    public bool IsEmpty => _tables.Count == 0 && _reasons.Count == 0 && _slots.Count == 0;

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

    public static TableResolution Slot(TableSlot slot)
    {
        var result = new TableResolution();
        result._slots.Add(slot);
        return result;
    }

    public TableResolution Add(TableResolution other)
    {
        _tables.UnionWith(other._tables);
        _reasons.UnionWith(other._reasons);
        _slots.UnionWith(other._slots);
        return this;
    }

    /// <summary>
    /// Turns "no information" into an explicit reason, so a final result is never silently empty.
    /// </summary>
    public TableResolution OrUnresolved(string reason) => IsEmpty ? Unresolved(reason) : this;

    /// <summary>
    /// A stable text form, used to tell call contexts apart.
    /// </summary>
    public override string ToString() =>
        string.Join("|", _tables) + "?" + string.Join("|", _reasons) + "$" + string.Join("|", _slots);
}

/// <summary>
/// How a parameter value is read for its table, because a string can be a table name or FetchXML, and a query
/// can count its linked tables or only its main table.
/// </summary>
internal enum TableShape
{
    Name,
    Entity,
    Query,
    QueryMainTable,
    Fetch,
    FetchMainTable,
    EntityReferences,
    Records,
}

/// <summary>
/// A table that depends on parameter <see cref="Ordinal"/> of <see cref="Method"/>, read as <see cref="Shape"/>.
/// <see cref="Reason"/> is reported when no call site provides the argument.
/// </summary>
internal sealed record TableSlot(string Method, int Ordinal, TableShape Shape, string Reason) : IComparable<TableSlot>
{
    public int CompareTo(TableSlot? other)
    {
        if (other is null) return 1;
        var result = string.CompareOrdinal(Method, other.Method);
        if (result == 0) result = Ordinal.CompareTo(other.Ordinal);
        if (result == 0) result = Shape.CompareTo(other.Shape);
        return result != 0 ? result : string.CompareOrdinal(Reason, other.Reason);
    }

    public override string ToString() => $"{Method}#{Ordinal}:{Shape}";
}
