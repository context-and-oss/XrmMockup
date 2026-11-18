using DG.Tools.XrmMockup;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmMockup.MetadataGenerator.Core.Connection;

namespace XrmMockup.MetadataGenerator.Core.Readers;

/// <summary>
/// Reads security role definitions from Dataverse.
/// </summary>
internal sealed class SecurityRoleReader(
    IOrganizationServiceProvider serviceProvider,
    ILogger<SecurityRoleReader> logger) : ISecurityRoleReader
{
    private const string Privilege = "privilege";
    private const string RolePrivileges = "roleprivileges";
    private const string Role = "role";
    private const string PrivilegeOtc = "privilegeobjecttypecodes";

    private readonly IOrganizationService _service = serviceProvider.Service;

    public async Task<Dictionary<Guid, SecurityRole>> GetSecurityRolesAsync(
        Guid rootBusinessUnitId,
        string[] solutions,
        string[]? securityRoles,
        bool allSecurityRoles,
        CancellationToken ct = default)
    {
        logger.LogInformation("Getting security roles");

        return await Task.Run(() =>
        {
            // Get role IDs to filter by
            var roleIdsToInclude = GetRoleIdsToInclude(solutions, securityRoles, allSecurityRoles, rootBusinessUnitId);

            // Empty set means explicitly no roles wanted — early return
            if (roleIdsToInclude is { Count: 0 })
            {
                logger.LogInformation("No security roles to retrieve");
                return [];
            }

            // Query all required data
            var privileges = _service.RetrieveAll(new QueryExpression(Privilege)
            {
                ColumnSet = new ColumnSet("privilegeid", "name", "accessright", "canbeglobal", "canbedeep", "canbelocal", "canbebasic")
            });

            var privilegeOTCs = _service.RetrieveAll(new QueryExpression(PrivilegeOtc)
            {
                ColumnSet = new ColumnSet("privilegeid", "objecttypecode")
            });

            var rolePrivileges = _service.RetrieveAll(new QueryExpression(RolePrivileges)
            {
                ColumnSet = new ColumnSet("privilegeid", "roleid", "privilegedepthmask")
            });

            var roleList = _service.RetrieveAll(GetRoleQuery(roleIdsToInclude));

            // Join: roleprivileges inner join roles
            // Use GetAttributeValue for defensive access in case parentrootroleid is missing
            var rolePrivilegeJoinRole =
                from roleprivilege in rolePrivileges
                join role in roleList on (Guid)roleprivilege["roleid"] equals GetParentRootRoleId(role)
                where ((EntityReference)role["businessunitid"]).Id.Equals(rootBusinessUnitId) &&
                      (int)roleprivilege["privilegedepthmask"] != 0
                select new { roleprivilege, role };

            // Join: privileges left outer join privilegeOTCs
            var privilegesJoinPrivilegeOTCs =
                from privilege in privileges
                join privilegeOTC in privilegeOTCs on (Guid)privilege["privilegeid"] equals ((EntityReference)privilegeOTC["privilegeid"]).Id into res
                from privilegeOTC in res.DefaultIfEmpty()
                select new { privilege, privilegeOTC };

            // Full join: pp left outer join rpr
            var entities =
                from pp in privilegesJoinPrivilegeOTCs
                join rpr in rolePrivilegeJoinRole on (Guid)pp.privilege["privilegeid"] equals (Guid)rpr.roleprivilege["privilegeid"] into res
                from rpr in res.DefaultIfEmpty()
                select new { pp, rpr };

            // Build security roles
            var roles = new Dictionary<Guid, SecurityRole>();
            var roleNameCounters = new Dictionary<string, int>();

            var validSecurityRoles = entities.Where(e =>
                e.pp?.privilege != null &&
                (e.pp?.privilegeOTC?.Contains("objecttypecode")).GetValueOrDefault() &&
                (e.rpr?.roleprivilege?.Contains("roleid")).GetValueOrDefault() &&
                (e.rpr?.role?.Contains("name")).GetValueOrDefault());

            foreach (var e in validSecurityRoles)
            {
                var entityName = (string)e.pp.privilegeOTC!["objecttypecode"];
                var global = entityName == PrivilegeReader.NoTable;
                var privilegeName = e.pp.privilege!.GetAttributeValue<string>("name");
                if (global && string.IsNullOrEmpty(privilegeName)) continue;

                var rp = ToRolePrivilege(e.pp.privilege!, e.rpr!.roleprivilege);
                if (!global && rp.AccessRight == AccessRights.None) continue;

                var roleId = (Guid)e.rpr.roleprivilege["roleid"];
                if (!roles.TryGetValue(roleId, out var roleValue))
                {
                    var name = (string)e.rpr.role["name"];

                    // Always mitigate duplicate names
                    if (roleNameCounters.TryGetValue(name, out var count))
                    {
                        if (logger.IsEnabled(LogLevel.Warning))
                        {
                            logger.LogWarning("Duplicate security role name detected: \"{Name}\". If this is not on purpose, contact Microsoft Support.", name);
                        }

                        name += $"_{++count}";
                        roleNameCounters[name] = count;
                    }
                    else
                    {
                        roleNameCounters[name] = 1;
                    }

                    roleValue = new SecurityRole
                    {
                        Name = name,
                        RoleId = roleId,
                        Privileges = [],
                        GlobalPrivileges = []
                    };
                    roles[roleId] = roleValue;

                    if (e.rpr.role.Attributes.ContainsKey("roletemplateid"))
                    {
                        roles[roleId].RoleTemplateId = ((EntityReference)e.rpr.role["roletemplateid"]).Id;
                    }
                }

                // Privileges that aren't tied to a table are kept apart, so XrmMockup's table checks don't see them
                if (global)
                {
                    roleValue.GlobalPrivileges.TryAdd(privilegeName, rp);
                    continue;
                }

                if (!roleValue.Privileges.TryGetValue(entityName, out var value))
                {
                    value = [];
                    roleValue.Privileges.Add(entityName, value);
                }

                value.TryAdd(rp.AccessRight, rp);
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Retrieved {Count} security roles", roles.Count);
            }
            return roles;
        }, ct);
    }

    private static DG.Tools.XrmMockup.RolePrivilege ToRolePrivilege(Entity privilege, Entity rolePrivilege)
    {
        return new DG.Tools.XrmMockup.RolePrivilege
        {
            CanBeGlobal = privilege.GetAttributeValue<bool>("canbeglobal"),
            CanBeDeep = privilege.GetAttributeValue<bool>("canbedeep"),
            CanBeLocal = privilege.GetAttributeValue<bool>("canbelocal"),
            CanBeBasic = privilege.GetAttributeValue<bool>("canbebasic"),
            AccessRight = privilege.GetAttributeValue<AccessRights>("accessright"),
            PrivilegeDepth = PrivilegeDepthToEnum((int)rolePrivilege["privilegedepthmask"])
        };
    }

    private static PrivilegeDepth PrivilegeDepthToEnum(int privilegeDepth)
    {
        return privilegeDepth switch
        {
            1 => PrivilegeDepth.Basic,
            2 => PrivilegeDepth.Local,
            4 => PrivilegeDepth.Deep,
            8 => PrivilegeDepth.Global,
            16 => PrivilegeDepth.RecordFilter,
            _ => throw new NotImplementedException($"Unknown privilege depth mask: {privilegeDepth}")
        };
    }

    /// <summary>
    /// Gets the parent root role ID from a role entity.
    /// Falls back to the role's own ID if parentrootroleid is not present.
    /// </summary>
    private static Guid GetParentRootRoleId(Entity role)
    {
        var parentRootRole = role.GetAttributeValue<EntityReference>("parentrootroleid");
        if (parentRootRole != null)
            return parentRootRole.Id;

        // Fallback: use the role's own ID (for root roles or when parentrootroleid is not populated)
        return role.Id;
    }

    /// <summary>
    /// Gets the set of role IDs to include based on solutions, named roles, and allSecurityRoles flag.
    /// Returns null if no filtering is needed (all roles should be returned).
    /// Returns an empty set if explicitly no roles are wanted.
    /// </summary>
    private HashSet<Guid>? GetRoleIdsToInclude(string[] solutions, string[]? securityRoles, bool allSecurityRoles, Guid rootBusinessUnitId)
    {
        // allSecurityRoles overrides everything — return all roles
        if (allSecurityRoles)
            return null;

        // No solutions and no configured roles → all roles (backward compat)
        if (solutions.Length == 0 && securityRoles is null)
            return null;

        var roleIds = new HashSet<Guid>();

        if (solutions.Length > 0)
        {
            roleIds = GetRoleIdsFromSolutions(solutions);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Found {Count} roles in specified solutions", roleIds.Count);
            }
        }

        if (securityRoles is { Length: > 0 })
        {
            var namedRoleIds = GetRoleIdsByName(securityRoles, rootBusinessUnitId);
            roleIds.UnionWith(namedRoleIds);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Found {Count} roles by name", namedRoleIds.Count);
            }
        }

        return roleIds;
    }

    /// <summary>
    /// Gets role IDs that are part of the specified solutions.
    /// </summary>
    private HashSet<Guid> GetRoleIdsFromSolutions(string[] solutions)
    {
        var query = new QueryExpression(Role)
        {
            ColumnSet = new ColumnSet("roleid"),
            Distinct = true
        };

        var solutionComponentQuery = new LinkEntity(
            Role, "solutioncomponent",
            "roleid", "objectid", JoinOperator.Inner)
        {
            Columns = new ColumnSet(),
            LinkCriteria = new FilterExpression()
        };
        solutionComponentQuery.LinkCriteria.AddCondition("componenttype", ConditionOperator.Equal, 20);
        query.LinkEntities.Add(solutionComponentQuery);

        var solutionQuery = new LinkEntity(
            "solutioncomponent", "solution",
            "solutionid", "solutionid", JoinOperator.Inner)
        {
            Columns = new ColumnSet(),
            LinkCriteria = new FilterExpression()
        };
        solutionQuery.LinkCriteria.AddCondition("uniquename", ConditionOperator.In, solutions.Cast<object>().ToArray());
        solutionComponentQuery.LinkEntities.Add(solutionQuery);

        var results = _service.RetrieveMultiple(query);
        return results.Entities.Select(e => e.Id).ToHashSet();
    }

    /// <summary>
    /// Gets role IDs by their names in the root business unit.
    /// </summary>
    private HashSet<Guid> GetRoleIdsByName(string[] roleNames, Guid rootBusinessUnitId)
    {
        if (roleNames.Length == 0)
            return [];

        var query = new QueryExpression(Role)
        {
            ColumnSet = new ColumnSet("roleid", "name")
        };
        query.Criteria.AddCondition("name", ConditionOperator.In, roleNames.Cast<object>().ToArray());
        query.Criteria.AddCondition("businessunitid", ConditionOperator.Equal, rootBusinessUnitId);

        var results = _service.RetrieveMultiple(query);

        if (results.Entities.Count < roleNames.Length)
        {
            var foundNames = results.Entities
                .Select(e => e.GetAttributeValue<string>("name"))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingNames = roleNames.Where(n => !foundNames.Contains(n));
            logger.LogWarning("Security roles not found: {MissingRoles}", string.Join(", ", missingNames));
        }

        return [.. results.Entities.Select(e => e.Id)];
    }

    /// <summary>
    /// Builds the role query, optionally filtering by role IDs.
    /// </summary>
    private static QueryExpression GetRoleQuery(HashSet<Guid>? roleIdsToInclude)
    {
        var query = new QueryExpression(Role)
        {
            ColumnSet = new ColumnSet("parentrootroleid", "name", "roleid", "roletemplateid", "businessunitid")
        };

        if (roleIdsToInclude is not null && roleIdsToInclude.Count > 0)
        {
            query.Criteria.AddCondition("roleid", ConditionOperator.In, roleIdsToInclude.Cast<object>().ToArray());
        }

        return query;
    }
}
