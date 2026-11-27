using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using XrmMockup.PermissionAnalyzer;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;
using AnalysisDepth = XrmMockup.PermissionAnalyzer.Core.Model.PrivilegeDepth;
using CrmDepth = Microsoft.Crm.Sdk.Messages.PrivilegeDepth;

namespace DG.Tools.XrmMockup
{
    public abstract partial class XrmMockupBase
    {
        /// <summary>Checks static privileges for combined role IDs. Does not execute requests or prove record-level access.</summary>
        public PermissionCheckResult CheckPermissions(PermissionAnalysis analysis, PermissionScope scope, params Guid[] securityRoles)
        {
            return Core.CheckPermissions(analysis, scope, securityRoles);
        }

        /// <summary>Checks the user's current direct and team-derived roles, without executing requests.</summary>
        public PermissionCheckResult CheckPermissions(PermissionAnalysis analysis, PermissionScope scope, EntityReference user)
        {
            return Core.CheckPermissions(analysis, scope, user);
        }
    }

    internal partial class Core
    {
        public PermissionCheckResult CheckPermissions(PermissionAnalysis analysis, PermissionScope scope, params Guid[] securityRoles)
        {
            if (securityRoles == null) throw new ArgumentNullException(nameof(securityRoles));
            if (securityRoles.Length == 0) throw new ArgumentException("Supply at least one security role ID.", nameof(securityRoles));
            var current = security.GetSecurityRoles().ToDictionary(r => r.RoleId);
            var roles = securityRoles.Distinct().Select(id => current.TryGetValue(id, out var role) ? role
                : throw new ArgumentException($"Unknown security role ID: {id}.", nameof(securityRoles))).ToList();
            return CheckPermissions(analysis, scope, roles, "explicit security role IDs");
        }

        public PermissionCheckResult CheckPermissions(PermissionAnalysis analysis, PermissionScope scope, EntityReference user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (user.LogicalName != "systemuser" || user.Id == Guid.Empty || !security.HasLocalUser(user.Id))
                throw new ArgumentException($"Invalid local system user: {user.LogicalName} {user.Id}.", nameof(user));
            return CheckPermissions(analysis, scope, security.GetPermissionCheckRoles(user.Id), $"user {user.Id} (direct and team roles)", security.GetPermissionCheckRoles(user.Id, directOnly: true));
        }

        private PermissionCheckResult CheckPermissions(PermissionAnalysis analysis, PermissionScope scope, IEnumerable<SecurityRole> roles, string origin, IEnumerable<SecurityRole> directRoles = null)
        {
            if (analysis == null) throw new ArgumentNullException(nameof(analysis));
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            var allRoles = security.GetSecurityRoles();
            var steps = (metadata.Plugins ?? new List<MetaPlugin>()).Concat(settings.IPluginMetadata ?? new MetaPlugin[0]);
            var context = new PermissionMetadata
            {
                Workflows = workflowManager.PermissionCheckWorkflows.Select(PermissionWorkflow).Where(w => w != null).ToList(),
                RoleGovernedTables = allRoles.SelectMany(r => (r.Privileges ?? new Dictionary<string, Dictionary<AccessRights, RolePrivilege>>()).Keys).Distinct().ToList(),
                Lookups = metadata.EntityMetadata.Values.SelectMany(e => (e.Attributes ?? new AttributeMetadata[0]).OfType<LookupAttributeMetadata>()
                    .Where(a => a.Targets != null && a.Targets.Length > 0).Select(a => new LookupColumn(e.LogicalName, a.LogicalName, a.Targets))).ToList(),
                PluginSteps = steps.Where(p => !string.IsNullOrEmpty(p.AssemblyName) && !string.IsNullOrEmpty(p.MessageName))
                    .Select(p => new PluginStepRegistration(p.AssemblyName, p.PluginTypeAssemblyName, p.MessageName,
                        string.IsNullOrEmpty(p.PrimaryEntity) || p.PrimaryEntity == "none" ? null : p.PrimaryEntity,
                        p.Stage, p.ImpersonatingUserId?.ToString(), string.IsNullOrWhiteSpace(p.FilteredAttributes) ? null : p.FilteredAttributes.Split(',').Select(a => a.Trim().ToLowerInvariant()).Where(a => a.Length > 0).ToList(), p.Mode == 1)).ToList()
            };
            var manifest = analysis.Analyze(scope, context);
            if (manifest.EntryPoints.Count == 0) throw new ArgumentException($"No registered entry point matches {scope}.", nameof(scope));
            var users = new Dictionary<string, RoleSet>(StringComparer.OrdinalIgnoreCase);
            // Never substitute the primary caller's roles for a separately impersonated identity.
            foreach (var identity in manifest.AllRequirements.Select(r => r.RunsAs).Concat(manifest.AllGlobalRequirements.Select(r => r.RunsAs)))
            {
                if (identity.Kind == RunsAsKind.User && Guid.TryParse(identity.UserId, out var id) && security.HasLocalUser(id))
                    users[identity.UserId] = RoleSet.Combine(security.GetPermissionCheckRoles(id).Select(PermissionRole), $"user {id} (direct and team roles)", security.GetPermissionCheckRoles(id, directOnly: true).Select(PermissionRole));
            }
            var primaryRoles = RoleSet.Combine(roles.Select(PermissionRole), origin, (directRoles ?? roles).Select(PermissionRole));
            var coverage = CoverageEvaluator.EvaluateForPermissionCheck(manifest, new CoverageOptions(primaryRoles, users));
            return new PermissionCheckResult(manifest, coverage, primaryRoles);
        }

        private static WorkflowRegistration PermissionWorkflow(Entity entity)
        {
            var table = entity.GetAttributeValue<string>("primaryentity");
            if (string.IsNullOrEmpty(table) || table == "none") return null;
            var create = entity.GetAttributeValue<bool?>("triggeroncreate") == true;
            var delete = entity.GetAttributeValue<bool?>("triggerondelete") == true;
            var columns = (entity.GetAttributeValue<string>("triggeronupdateattributelist") ?? "").Split(',')
                .Select(c => c.Trim().ToLowerInvariant()).Where(c => c.Length > 0).Distinct().OrderBy(c => c, StringComparer.Ordinal).ToList();
            if (!create && !delete && columns.Count == 0) return null;
            var realTime = entity.GetAttributeValue<OptionSetValue>("mode")?.Value == 1;
            var asCaller = realTime && entity.GetAttributeValue<OptionSetValue>("runas")?.Value == 1;
            return new WorkflowRegistration(entity.GetAttributeValue<string>("name") ?? entity.Id.ToString(), table,
                create, delete, columns.Count == 0 ? null : columns, realTime,
                asCaller ? null : entity.GetAttributeValue<EntityReference>("ownerid")?.Id.ToString());
        }

        private static RoleDefinition PermissionRole(SecurityRole role)
        {
            var privileges = new Dictionary<string, IReadOnlyDictionary<Privilege, AnalysisDepth>>();
            foreach (var table in role.Privileges ?? new Dictionary<string, Dictionary<AccessRights, RolePrivilege>>())
            {
                var rights = new Dictionary<Privilege, AnalysisDepth>();
                foreach (var right in table.Value)
                {
                    Privilege? mapped = null;
                    switch (right.Key)
                    {
                        case AccessRights.CreateAccess: mapped = Privilege.Create; break;
                        case AccessRights.ReadAccess: mapped = Privilege.Read; break;
                        case AccessRights.WriteAccess: mapped = Privilege.Write; break;
                        case AccessRights.DeleteAccess: mapped = Privilege.Delete; break;
                        case AccessRights.AppendAccess: mapped = Privilege.Append; break;
                        case AccessRights.AppendToAccess: mapped = Privilege.AppendTo; break;
                        case AccessRights.AssignAccess: mapped = Privilege.Assign; break;
                        case AccessRights.ShareAccess: mapped = Privilege.Share; break;
                    }
                    if (mapped.HasValue) rights[mapped.Value] = PermissionDepth(right.Value.PrivilegeDepth);
                }
                privileges[table.Key.ToLowerInvariant()] = rights;
            }
            var globals = role.GlobalPrivileges == null ? null : role.GlobalPrivileges.ToDictionary(p => p.Key, p => PermissionDepth(p.Value.PrivilegeDepth));
            return new RoleDefinition(role.Name, privileges, globals);
        }

        private static AnalysisDepth PermissionDepth(CrmDepth depth)
        {
            switch (depth)
            {
                case CrmDepth.Basic: return AnalysisDepth.User;
                case CrmDepth.Local: return AnalysisDepth.BusinessUnit;
                case CrmDepth.Deep: return AnalysisDepth.ParentChildBusinessUnit;
                default: return AnalysisDepth.Organization;
            }
        }
    }
}
