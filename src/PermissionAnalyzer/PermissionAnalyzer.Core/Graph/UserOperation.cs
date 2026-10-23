using XrmMockup.PermissionAnalyzer.Core.Extraction;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Graph;

/// <summary>
/// An operation a user makes outside the solution's code, such as updating a record in an app, given with
/// <c>--operation</c>. It needs the privileges of its request rule on its table as the caller, and fires the plugin
/// steps of the rule's messages, setting <see cref="Columns"/> when they are given.
/// </summary>
internal sealed record UserOperation(string Message, string Table, IReadOnlyList<string>? Columns)
{
    /// <summary>
    /// Where the requirements of a user operation are reported, as there is no code for it.
    /// </summary>
    public static readonly SourceLocation Location = new("--operation", 0, 0);

    public string Display => $"{Message} {Table}";

    /// <summary>
    /// A call graph node with the operation's accesses, to evaluate on its own.
    /// </summary>
    public NodeSummary Node(string id, RequestRules rules, LookupColumns lookups)
    {
        var node = new NodeSummary(id, id, Display, null, false, null, null);
        if (rules.Find(Message) is not { } rule)
        {
            // A message without a rule, such as a custom API, may fire steps on the table
            node.Accesses.Add(Access(TableResolution.Unresolved(RequestReasons.NoRule(Message)).Add(TableResolution.Fires(Message, TableResolution.Table(Table), ColumnSet()))));
            return node;
        }

        var members = new OperationMembers(this, RuleApplication.PrimaryMember(rule), lookups);
        foreach (var shape in RequestShapes.All)
        {
            var tables = RuleApplication.Apply(Message, rule, members, shape);
            node.Accesses.Add(RequestShapes.PrivilegeOf(shape) is { } privilege ? Access(tables, privilege) : Access(tables));
        }
        return node;
    }

    private TableResolution? ColumnSet() => Columns?.Aggregate(TableResolution.Empty, (result, column) => result.Add(TableResolution.Table(column)));

    private AccessFact Access(TableResolution tables, params Privilege[] privileges) => new(Message, Location, privileges, tables, "", Service);

    private static IReadOnlyList<ServiceOrigin> Service { get; } = [new FixedOrigin(RunsAs.Caller)];

    /// <summary>
    /// The members of a user operation. Its table and columns are those of the member its steps fire on, usually
    /// <c>Target</c>; the values of the other members aren't known.
    /// </summary>
    private sealed class OperationMembers(UserOperation operation, string? primary, LookupColumns lookups) : IRequestMembers
    {
        public TableResolution Tables(string message, string member) =>
            member == primary ? TableResolution.Table(operation.Table) : TableResolution.Unresolved($"{member} of the {message} request isn't known");

        public TableResolution? Columns(string member) => member == primary ? operation.ColumnSet() : null;

        // The choices a user makes, such as whether qualifying a lead creates an account, aren't known, so each applies
        public bool MayBeSet(string member) => true;

        public IReadOnlyList<Privilege> Rights(string path, out TableResolution reasons)
        {
            reasons = TableResolution.Unresolved($"the rights granted in {path} aren't known");
            return [];
        }

        /// <summary>
        /// The table when a column set is a lookup, and the tables the lookups refer to: the table when there's
        /// one, every target as a lookup target when there are several.
        /// </summary>
        public TableResolution Lookups(string member, TableShape shape)
        {
            var result = TableResolution.Empty;
            if (member != primary || operation.Columns is null) return result;

            var targets = operation.Columns.Select(c => lookups.TargetsOf(operation.Table, c)).Where(t => t.Count > 0).ToList();
            if (targets.Count == 0) return result;
            if (shape == TableShape.LookupHolder) return TableResolution.Table(operation.Table);

            foreach (var target in targets)
            {
                foreach (var name in target) result.Add(target.Count == 1 ? TableResolution.Table(name) : TableResolution.LookupTarget(name));
            }
            return result;
        }

        public TableResolution Requests(string member, TableShape shape) =>
            RequestShapes.Other(shape, $"the requests in {member} of the {operation.Message} request aren't known");

        public TableResolution Bypass(string message, out PluginBypass bypass)
        {
            bypass = PluginBypass.None;
            return TableResolution.Empty;
        }
    }
}
