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
        var table = TableResolution.Table(Table);
        var columns = Columns?.Aggregate(TableResolution.Empty, (result, column) => result.Add(TableResolution.Table(column)));

        if (rules.Find(Message) is not { } rule)
        {
            // A message without a rule, such as a custom API, may fire steps on the table
            node.Accesses.Add(Access(TableResolution.Unresolved(RequestReasons.NoRule(Message)).Add(TableResolution.Fires(Message, table, columns))));
            return node;
        }

        var fired = TableResolution.Empty;
        foreach (var message in rule.Messages) fired.Add(TableResolution.Fires(message, table, columns));
        node.Accesses.Add(Access(fired));

        foreach (var missing in rule.Missing) node.Accesses.Add(Access(TableResolution.Unresolved(RequestReasons.NoRule(missing))));
        foreach (var nested in rule.Requests)
        {
            node.Accesses.Add(Access(TableResolution.Unresolved($"the requests in {nested} of the {Message} request aren't known")));
        }

        // The table is the value of the member the steps fire on, usually Target; checks on other members aren't known
        var primary = rule.Checks.FirstOrDefault(c => c.Member is not null)?.Member;
        foreach (var check in rule.Checks)
        {
            var privileges = check.Privileges.ToArray();
            if (check.Table is { } fixedTable)
            {
                node.Accesses.Add(Access(TableResolution.Table(fixedTable), privileges));
            }
            else if (check.Member == primary)
            {
                node.Accesses.Add(Access(TableResolution.Table(Table), privileges));
                if (check.AccessMask is { } mask) node.Accesses.Add(Access(TableResolution.Unresolved($"the rights granted in {mask} aren't known")));
                if (Columns is not null && (privileges.Contains(Privilege.Create) || privileges.Contains(Privilege.Write))) node.Accesses.AddRange(Lookups(lookups));
            }
            else
            {
                node.Accesses.Add(Access(TableResolution.Unresolved($"{check.Member} of the {Message} request isn't known"), privileges));
            }
        }
        return node;
    }

    /// <summary>
    /// Append on the table when a column set is a lookup, and AppendTo on the tables the lookups refer to: the
    /// table when there's one, every target as a lookup target when there are several.
    /// </summary>
    private IEnumerable<AccessFact> Lookups(LookupColumns lookups)
    {
        var targets = Columns!.Select(c => lookups.TargetsOf(Table, c)).Where(t => t.Count > 0).ToList();
        if (targets.Count == 0) yield break;

        yield return Access(TableResolution.Table(Table), Privilege.Append);
        var referred = TableResolution.Empty;
        foreach (var target in targets)
        {
            foreach (var name in target) referred.Add(target.Count == 1 ? TableResolution.Table(name) : TableResolution.LookupTarget(name));
        }
        yield return new AccessFact(Message, Location, [Privilege.AppendTo], referred, "lookup value: ", Service);
    }

    private AccessFact Access(TableResolution tables, params Privilege[] privileges) => new(Message, Location, privileges, tables, "", Service);

    private static IReadOnlyList<ServiceOrigin> Service { get; } = [new FixedOrigin(RunsAs.Caller)];
}
