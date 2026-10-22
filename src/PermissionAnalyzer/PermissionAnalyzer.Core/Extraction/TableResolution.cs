using XrmMockup.PermissionAnalyzer.Core.Graph;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Extraction;

/// <summary>
/// The tables a value can refer to. A value can resolve partly: <c>flag ? "account" : name</c> resolves to
/// <c>account</c> plus a reason for the parameter. An empty resolution carries no information yet, such as
/// <c>new Entity()</c> before its <c>LogicalName</c> is set.
/// </summary>
/// <remarks>
/// A value that comes from a method parameter is kept as a <see cref="TableSlot"/>, so the call graph can fill
/// it in from the argument at each call site. Where no call site binds it, the slot's reason is reported. A value
/// returned by a method in the solution is kept as a <see cref="ReturnedTables"/>, resolved from the method's
/// return values in the context of the call.
/// </remarks>
internal sealed class TableResolution
{
    private readonly SortedSet<string> _tables = new(StringComparer.Ordinal);
    private readonly SortedSet<string> _reasons = new(StringComparer.Ordinal);
    private readonly SortedSet<TableSlot> _slots = new();
    private readonly SortedSet<ReturnedTables> _returns = new();

    public IReadOnlyCollection<string> Tables => _tables;

    /// <summary>
    /// Why part of the value couldn't be resolved, one entry per distinct cause.
    /// </summary>
    public IReadOnlyCollection<string> Reasons => _reasons;

    /// <summary>
    /// Parameters the value depends on, resolved per call site.
    /// </summary>
    public IReadOnlyCollection<TableSlot> Slots => _slots;

    /// <summary>
    /// Calls whose return value the value depends on, resolved per call context.
    /// </summary>
    public IReadOnlyCollection<ReturnedTables> Returns => _returns;

    /// <summary>
    /// Set when nothing in the value depends on the call context: no parameter and no method's return value.
    /// </summary>
    public bool IsBound => _slots.Count == 0 && _returns.Count == 0;

    public bool IsEmpty => _tables.Count == 0 && _reasons.Count == 0 && IsBound;

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

    public static TableResolution Returned(ReturnedTables returned)
    {
        var result = new TableResolution();
        result._returns.Add(returned);
        return result;
    }

    public TableResolution Add(TableResolution other)
    {
        _tables.UnionWith(other._tables);
        _reasons.UnionWith(other._reasons);
        _slots.UnionWith(other._slots);
        _returns.UnionWith(other._returns);
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
        string.Join("|", _tables) + "?" + string.Join("|", _reasons) + "$" + string.Join("|", _slots) + "^" + string.Join("|", _returns);
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

    // A request, or a collection of requests, read for the tables it needs one privilege on (see RequestShapes)
    RequestCreate,
    RequestRead,
    RequestWrite,
    RequestDelete,
    RequestAppend,
    RequestAppendTo,
    RequestAssign,
    RequestShare,

    /// <summary>
    /// A request read for what can't be tied to a privilege: requests without a rule, or of unknown type.
    /// </summary>
    RequestOther,
}

/// <summary>
/// The shapes a request is read as: one per privilege, plus <see cref="TableShape.RequestOther"/>.
/// </summary>
internal static class RequestShapes
{
    public static IReadOnlyList<TableShape> All { get; } =
        [.. Enum.GetValues(typeof(Privilege)).Cast<Privilege>().Select(For), TableShape.RequestOther];

    public static TableShape For(Privilege privilege) => TableShape.RequestCreate + (int)privilege;

    /// <summary>
    /// The privilege a request shape is read for; <c>null</c> for <see cref="TableShape.RequestOther"/> and the
    /// other shapes.
    /// </summary>
    public static Privilege? PrivilegeOf(TableShape shape) =>
        shape is >= TableShape.RequestCreate and < TableShape.RequestOther ? (Privilege)(shape - TableShape.RequestCreate) : null;

    public static bool IsRequest(TableShape shape) => shape >= TableShape.RequestCreate;
}

/// <summary>
/// A table that depends on parameter <see cref="Ordinal"/>, or on a type parameter of a generic method (see
/// <see cref="TypeParameterOrdinal"/>) of <see cref="Method"/>, read as <see cref="Shape"/>.
/// <see cref="Reason"/> is reported when no call site provides the argument; an empty reason reports nothing, for
/// a request read for one privilege, whose reason is reported once under <see cref="TableShape.RequestOther"/>.
/// </summary>
internal sealed record TableSlot(string Method, int Ordinal, TableShape Shape, string Reason) : IComparable<TableSlot>
{
    /// <summary>
    /// The ordinal a method's type parameter is bound under. Negative, so it never clashes with a parameter: a
    /// call binds type argument <c>i</c> as if it were argument <c>-1 - i</c>, read as <see cref="TableShape.Entity"/>.
    /// </summary>
    public static int TypeParameterOrdinal(int ordinal) => -1 - ordinal;

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

/// <summary>
/// The tables of the value <see cref="Call"/> returns, read as <see cref="Shape"/>, such as a query built by a
/// helper method. <see cref="Reason"/> is reported when the method isn't in the call graph or returns nothing known;
/// an empty reason reports nothing, as for <see cref="TableSlot"/>.
/// </summary>
internal sealed record ReturnedTables(CallFact Call, TableShape Shape, string Reason) : IComparable<ReturnedTables>
{
    public int CompareTo(ReturnedTables? other)
    {
        if (other is null) return 1;
        var result = Call.Source.CompareTo(other.Call.Source);
        if (result == 0) result = string.CompareOrdinal(Call.Target, other.Call.Target);
        if (result == 0) result = Shape.CompareTo(other.Shape);
        return result != 0 ? result : string.CompareOrdinal(Reason, other.Reason);
    }

    public override string ToString() => $"{Call.Target}@{Call.Source}:{Shape}";
}
