using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Core.Extraction;

/// <summary>
/// The members of a request a rule reads: those set where the request is created in code, or those of an
/// operation a user makes, given with <c>--operation</c>.
/// </summary>
internal interface IRequestMembers
{
    /// <summary>
    /// The tables a member refers to, such as the table of <c>Target</c>.
    /// </summary>
    TableResolution Tables(string message, string member);

    /// <summary>
    /// The columns set on the entity a member holds, such as the <c>Target</c> of an <c>UpdateRequest</c>;
    /// <c>null</c> when they aren't known.
    /// </summary>
    TableResolution? Columns(string member);

    /// <summary>
    /// Whether a boolean member may be set, so a check that depends on it applies.
    /// </summary>
    bool MayBeSet(string member);

    /// <summary>
    /// The privileges of the access rights in the member at <paramref name="path"/>, such as
    /// <c>PrincipalAccess.AccessMask</c>, and in <paramref name="reasons"/> why some of them aren't known.
    /// </summary>
    IReadOnlyList<Privilege> Rights(string path, out TableResolution reasons);

    /// <summary>
    /// The tables of the lookups set on the entity a member holds, read as <see cref="TableShape.LookupHolder"/>
    /// or <see cref="TableShape.LookupTarget"/>.
    /// </summary>
    TableResolution Lookups(string member, TableShape shape);

    /// <summary>
    /// What the requests a member holds need, read as <paramref name="shape"/>.
    /// </summary>
    TableResolution Requests(string member, TableShape shape);

    /// <summary>
    /// The privileges the bypass parameters of the request need, and in <paramref name="bypass"/> the plugin steps
    /// they certainly skip.
    /// </summary>
    TableResolution Bypass(string message, out PluginBypass bypass);
}

/// <summary>
/// Reads a request by its rule, in one way for requests in code and operations users make.
/// </summary>
internal static class RuleApplication
{
    /// <summary>
    /// The tables <paramref name="message"/> needs the privilege of <paramref name="shape"/> on. The privileges a
    /// request needs by name, its assumptions and the steps it fires go with <see cref="TableShape.RequestOther"/>,
    /// so they are added once.
    /// </summary>
    public static TableResolution Apply(string message, ExpandedRule rule, IRequestMembers members, TableShape shape)
    {
        var result = TableResolution.Empty;
        foreach (var missing in rule.Missing) result.Add(RequestShapes.Other(shape, RequestReasons.NoRule(missing)));
        foreach (var nested in rule.Requests) result.Add(members.Requests(nested, shape));
        if (shape == TableShape.RequestOther)
        {
            foreach (var global in rule.GlobalPrivileges) result.Add(TableResolution.Global(global, message));
            foreach (var assumption in rule.Assumptions) result.Add(TableResolution.Assumed(assumption));
            result.Add(members.Bypass(message, out var bypass));
            result.Add(Fired(message, rule, members, bypass));
        }

        var privilege = RequestShapes.PrivilegeOf(shape);
        foreach (var check in rule.Checks)
        {
            if (check.When is { } when && !members.MayBeSet(when)) continue;

            var needed = privilege is { } p && check.Privileges.Contains(p);
            if (check.AccessMask is { } mask)
            {
                var rights = members.Rights(mask, out var reasons);
                if (shape == TableShape.RequestOther) result.Add(reasons);
                needed |= privilege is { } granted && rights.Contains(granted);
            }
            // A request that writes an entity, such as Create or Update, sets its lookups too
            if (shape is TableShape.RequestAppend or TableShape.RequestAppendTo && check.Member is { } written
                && (check.Privileges.Contains(Privilege.Create) || check.Privileges.Contains(Privilege.Write)))
            {
                result.Add(members.Lookups(written, shape == TableShape.RequestAppend ? TableShape.LookupHolder : TableShape.LookupTarget));
            }

            if (!needed) continue;

            if (check.Table is { } table)
            {
                result.Add(TableResolution.Table(table));
            }
            else if (check.Member is { } member)
            {
                result.Add(members.Tables(message, member));
            }
        }
        return result;
    }

    /// <summary>
    /// The member a request's plugin steps fire on: that of its first check on a member, usually <c>Target</c>.
    /// </summary>
    public static string? PrimaryMember(ExpandedRule rule) => rule.Checks.FirstOrDefault(c => c.Member is not null)?.Member;

    /// <summary>
    /// The plugin steps a request fires: those of each of its messages, on the table of its
    /// <see cref="PrimaryMember"/>, or else of its first check on a fixed table. A request whose checks name no
    /// table fires on no table.
    /// </summary>
    private static TableResolution Fired(string message, ExpandedRule rule, IRequestMembers members, PluginBypass bypass)
    {
        var tables = TableResolution.Empty;
        TableResolution? columns = null;
        if (PrimaryMember(rule) is { } member)
        {
            tables.Add(members.Tables(message, member));
            columns = members.Columns(member);
        }
        else if (rule.Checks.FirstOrDefault(c => c.Table is not null)?.Table is { } table)
        {
            tables.Add(TableResolution.Table(table));
        }

        return rule.Messages.Aggregate(TableResolution.Empty, (result, fired) => result.Add(TableResolution.Fires(fired, tables, columns, bypass)));
    }
}
