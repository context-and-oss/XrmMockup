using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Dataverse;

/// <summary>
/// Reads assignments, not privileges: coverage still uses the downloaded role definitions. Matching by root id
/// avoids confusing business-unit copies or unrelated roles with the same name. No analyzed code is executed.
/// </summary>
internal sealed class UserRoleResolver(IOrganizationService service)
{
    public RoleSet Resolve(string username, IReadOnlyList<RoleDefinition> metadataRoles, string environmentUrl,
        string option, DateTimeOffset readAt, CancellationToken cancellationToken = default)
    {
        var user = FindUser(username.Trim(), cancellationToken);
        var assignments = new List<(RoleDefinition Role, LiveRoleAssignment Source)>();
        foreach (var fromTeam in new[] { false, true })
        {
            foreach (var role in RetrieveAll(RoleQuery(user.Id, fromTeam), cancellationToken))
            {
                var rootId = role.GetAttributeValue<EntityReference>("parentrootroleid")?.Id ?? role.Id;
                var matches = metadataRoles.Where(r => r.RoleId == rootId).ToList();
                if (matches.Count != 1)
                    throw new ToolException($"Security role '{role.GetAttributeValue<string>("name")}' (root id {rootId:D}) " +
                        "is missing or duplicated in the metadata. Regenerate the metadata including all of this user's security roles.");

                var source = new LiveRoleAssignment(matches[0].Name, rootId,
                    fromTeam ? Alias<string>(role, "team.name") : null,
                    fromTeam ? Alias<Guid>(role, "team.teamid") : null);
                assignments.Add((matches[0], source));
            }
        }

        if (assignments.Count == 0)
            throw new ToolException($"Dataverse user '{username}' ({user.Id:D}) has no direct or team security roles.");

        var sources = assignments.Select(a => a.Source).Distinct()
            .OrderBy(a => a.RoleName, StringComparer.Ordinal)
            .ThenBy(a => a.RootRoleId)
            .ThenBy(a => a.TeamName, StringComparer.Ordinal)
            .ThenBy(a => a.TeamId)
            .ToList();
        return RoleSet.Combine(assignments.Select(a => a.Role), $"{option}: {username}",
            new LiveRoleSource(username, user.Id, environmentUrl, readAt.ToUniversalTime(), sources));
    }

    private Entity FindUser(string username, CancellationToken cancellationToken)
    {
        foreach (var attribute in new[] { "domainname", "internalemailaddress" })
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query = new QueryExpression("systemuser") { ColumnSet = new ColumnSet("systemuserid"), TopCount = 2 };
            query.Criteria.AddCondition(attribute, ConditionOperator.Equal, username);
            var users = service.RetrieveMultiple(query).Entities;
            if (users.Count > 1) throw new ToolException($"Dataverse user '{username}' is ambiguous ({attribute}). Use a unique UPN.");
            if (users.Count == 1) return users[0];
        }
        throw new ToolException($"Dataverse user '{username}' was not found by domainname or internalemailaddress.");
    }

    internal static QueryExpression RoleQuery(Guid userId, bool fromTeam)
    {
        var query = new QueryExpression("role") { ColumnSet = new ColumnSet("roleid", "name", "parentrootroleid") };
        query.AddOrder("roleid", OrderType.Ascending);
        if (!fromTeam)
        {
            var assignment = query.AddLink("systemuserroles", "roleid", "roleid");
            assignment.LinkCriteria.AddCondition("systemuserid", ConditionOperator.Equal, userId);
        }
        else
        {
            var assignment = query.AddLink("teamroles", "roleid", "roleid");
            var team = assignment.AddLink("team", "teamid", "teamid");
            team.EntityAlias = "team";
            team.Columns = new ColumnSet("teamid", "name");
            var membership = team.AddLink("teammembership", "teamid", "teamid");
            membership.LinkCriteria.AddCondition("systemuserid", ConditionOperator.Equal, userId);
            // No team-type filter: owner and Microsoft Entra group teams both contribute assignments.
        }
        return query;
    }

    private IEnumerable<Entity> RetrieveAll(QueryExpression query, CancellationToken cancellationToken)
    {
        query.PageInfo = new PagingInfo { PageNumber = 1, Count = 5000 };
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = service.RetrieveMultiple(query);
            foreach (var entity in page.Entities) yield return entity;
            if (!page.MoreRecords) yield break;
            query.PageInfo.PageNumber++;
            query.PageInfo.PagingCookie = page.PagingCookie;
        }
    }

    private static T Alias<T>(Entity entity, string name) => (T)entity.GetAttributeValue<AliasedValue>(name).Value;
}
