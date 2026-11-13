using DG.Tools.XrmMockup.Database;
using DG.Tools.XrmMockup.Internal;
using DG.Tools.XrmMockup.Serialization;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    using AccessDepthRight = Dictionary<AccessRights, PrivilegeDepth>;
    using Privileges = Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>;

    internal class Security
    {

        private static Guid SYSTEMADMIN_ROLE_TEMPLATE = new Guid("627090ff-40a3-4053-8790-584edc5be201");
        private Core Core;
        private MetadataSkeleton Metadata;

        private Dictionary<Guid, SecurityRole> SecurityRoles;
        private Dictionary<Guid, Guid> SecurityRoleMapping;
        private Dictionary<EntityReference, Dictionary<EntityReference, AccessRights>> Shares;
        private Dictionary<Guid, Privileges> PrinciplePrivilages = new Dictionary<Guid, Privileges>();
        private XrmDb db; 
        private List<Guid> addedRoles;

        internal Security(Core core, MetadataSkeleton metadata, List<SecurityRole> SecurityRoles, XrmDb db)
        {
            this.Core = core;
            this.Metadata = metadata;
            this.db = db;
            this.SecurityRoles = SecurityRoles.ToDictionary(s => s.RoleId, s => s);
            this.addedRoles = new List<Guid>();
            Initialize();
        }

        private void Initialize()
        {
            this.PrinciplePrivilages = new Dictionary<Guid, Privileges>();
            this.SecurityRoleMapping = new Dictionary<Guid, Guid>();
            this.Shares = new Dictionary<EntityReference, Dictionary<EntityReference, AccessRights>>();
        }

        internal void AddRolesForBusinessUnit(XrmDb db, EntityReference businessUnit)
        {
            AddRoleTemplates(db);

            foreach (var sr in SecurityRoles.Values)
            {
                var roleMeta = Metadata.EntityMetadata.GetMetadata("role");
                var role = new Entity("role")
                {
                    Id = Guid.NewGuid()
                };
                role[roleMeta.PrimaryIdAttribute] = role.Id;
                role["businessunitid"] = businessUnit;
                role["name"] = sr.Name;
                if (sr.RoleTemplateId != Guid.Empty)
                {
                    role["roletemplateid"] = new EntityReference("roletemplate", sr.RoleTemplateId);
                }
                role["createdby"] = Core.AdminUserRef;
                role["createdon"] = DateTime.UtcNow.Add(Core.TimeOffset);
                role["modifiedby"] = Core.AdminUserRef;
                role["modifiedon"] = DateTime.UtcNow.Add(Core.TimeOffset);
                db.Add(role);
                SecurityRoleMapping.Add(role.Id, sr.RoleId);
            }
        }

        internal void AddRoleTemplates(XrmDb db)
        {
            //role templates are actually uniques across all business units so don't try adding more than once.
            var allRoleTemplates = db.GetDBEntityRows("roletemplate").Select(x => x.ToEntity());

            foreach (var sr in SecurityRoles.Values.Where(x => x.RoleTemplateId != Guid.Empty).GroupBy(x => x.RoleTemplateId).Select(x => x.Key))
            {
                if (!allRoleTemplates.Any(x => x.Id == sr))
                {
                    var roleTemplate = new Entity("roletemplate")
                    {
                        Id = sr
                    };
                    db.Add(roleTemplate);
                }
            }
        }

        internal void InitializeSecurityRoles(XrmDb db)
        {
            AddRolesForBusinessUnit(db, Core.RootBusinessUnitRef);
            SetSecurityRole(Core.AdminUserRef,
                SecurityRoles
                .Where(s => s.Value.RoleTemplateId == SYSTEMADMIN_ROLE_TEMPLATE)
                .Select(s => s.Value.RoleId)
                .ToArray());
        }

        private void CheckSharingAccess(Entity entity, PrincipalAccess principalAccess, EntityReference userRef)
        {
            if (!HasPermission(entity, AccessRights.ShareAccess, userRef))
            {
                throw new FaultException($"Trying to share entity '{entity.LogicalName}'" +
                    $", but the calling user with id '{userRef.Id}' does not have share access for that entity");
            }
            if (principalAccess.AccessMask.GetAccessRights().Any(r => !HasPermission(entity, r, userRef)))
            {
                throw new FaultException($"Trying to share entity '{entity.LogicalName}'" +
                    $", but the calling user with id '{userRef.Id}' does not have the privileges it tries to share");
            }
        }

        internal void GrantAccess(EntityReference target, PrincipalAccess principalAccess, EntityReference userRef)
        {
            var entity = Core.GetDbEntityWithRelatedEntities(target, EntityRole.Referenced, userRef);
            CheckSharingAccess(entity, principalAccess, userRef);

            if (!Shares.ContainsKey(target))
            {
                Shares.Add(target, new Dictionary<EntityReference, AccessRights>());
            }

            if (Shares[target].ContainsKey(principalAccess.Principal))
            {
                throw new FaultException($"Trying to share record with logicalname '{target.LogicalName}' and id '{target.Id}' with " +
                    $"'{principalAccess.Principal.LogicalName}' with id '{principalAccess.Principal.Id}'" +
                    $", but the record was already shared with the {principalAccess.Principal.LogicalName}");
            }

            Shares[target].Add(principalAccess.Principal, principalAccess.AccessMask);
        }

        internal void ModifyAccess(EntityReference target, PrincipalAccess principalAccess, EntityReference userRef)
        {
            var entity = Core.GetDbEntityWithRelatedEntities(target, EntityRole.Referenced, userRef);
            CheckSharingAccess(entity, principalAccess, userRef);

            if (Shares.ContainsKey(target) && Shares[target].ContainsKey(principalAccess.Principal))
            {
                Shares[target][principalAccess.Principal] = principalAccess.AccessMask;
            }

        }
        internal void RevokeAccess(EntityReference target, EntityReference revokee, EntityReference userRef)
        {
            var entity = Core.GetDbEntityWithRelatedEntities(target, EntityRole.Referenced, userRef);
            if (!HasPermission(entity, AccessRights.ShareAccess, userRef))
            {
                throw new FaultException($"Trying to share entity '{entity.LogicalName}'" +
                    $", but the calling user with id '{userRef.Id}' does not have share access for that entity");
            }
            if (Shares.ContainsKey(target) && Shares[target].ContainsKey(revokee))
            {
                Shares[target].Remove(revokee);
            }
        }
        internal PrincipalAccess[] GetSharedPrincipalsAndAccess(EntityReference target)
        {
            // Return only explicit shares, not ownership, team membership or role-derived rights.
            var entity = db.GetEntity(target);
            if (!Shares.TryGetValue(entity.ToEntityReference(), out var shares)) return Array.Empty<PrincipalAccess>();
            return shares.Select(share => new PrincipalAccess
            {
                Principal = new EntityReference(share.Key.LogicalName, share.Key.Id),
                AccessMask = share.Value
            }).ToArray();
        }

        internal AccessRights GetAccessRights(EntityReference target, EntityReference userRef)
        {
            var entity = Core.GetDbRow(target);
            return GetAccessRights(entity.ToEntity(), userRef);
        }

        internal AccessRights GetAccessRights(Entity target, EntityReference userRef)
        {
            var ret = AccessRights.None;
            ret |= HasPermission(target, AccessRights.CreateAccess, userRef) ? AccessRights.CreateAccess : AccessRights.None;
            ret |= HasPermission(target, AccessRights.ReadAccess, userRef) ? AccessRights.ReadAccess : AccessRights.None;
            ret |= HasPermission(target, AccessRights.WriteAccess, userRef) ? AccessRights.WriteAccess : AccessRights.None;
            ret |= HasPermission(target, AccessRights.DeleteAccess, userRef) ? AccessRights.DeleteAccess : AccessRights.None;
            ret |= HasPermission(target, AccessRights.AppendAccess, userRef) ? AccessRights.AppendAccess : AccessRights.None;
            ret |= HasPermission(target, AccessRights.AppendToAccess, userRef) ? AccessRights.AppendToAccess : AccessRights.None;
            ret |= HasPermission(target, AccessRights.AssignAccess, userRef) ? AccessRights.AssignAccess : AccessRights.None;
            ret |= HasPermission(target, AccessRights.ShareAccess, userRef) ? AccessRights.ShareAccess : AccessRights.None;
            return ret;
        }

        internal void CascadeOwnerUpdate(Entity dbEntity, EntityReference userRef, EntityReference ownerRef)
        {
            // Cascade like Assign, but with UpdateRequests
            foreach (var relatedEntities in Core.GetDbEntityWithRelatedEntities(dbEntity.ToEntityReference(), EntityRole.Referenced, userRef).RelatedEntities)
            {
                var relationshipMeta = Metadata.EntityMetadata.GetMetadata(dbEntity.LogicalName).OneToManyRelationships.FirstOrDefault(r => r.SchemaName == relatedEntities.Key.SchemaName);
                if (relationshipMeta == null) continue;

                var req = new UpdateRequest();
                switch (relationshipMeta.CascadeConfiguration.Assign)
                {
                    case CascadeType.Cascade:

                        var refEntityMetadata = Metadata.EntityMetadata[relationshipMeta.ReferencingEntity];
                        if (refEntityMetadata.OwnershipType != OwnershipTypes.BusinessOwned)
                        {
                            foreach (var relatedEntity in relatedEntities.Value.Entities)
                            {
                                req.Target = new Entity(relatedEntity.LogicalName, relatedEntity.Id);
                                req.Target.Attributes["ownerid"] = ownerRef;
                                Core.Execute(req, userRef, null);
                            }
                        }
                        break;

                    case CascadeType.Active:
                        foreach (var relatedEntity in relatedEntities.Value.Entities)
                        {
                            if (Core.GetDbRow(relatedEntity.ToEntityReference()).ToEntity().GetAttributeValue<OptionSetValue>("statecode")?.Value == 0)
                            {
                                req.Target = new Entity(relatedEntity.LogicalName, relatedEntity.Id);
                                req.Target.Attributes["ownerid"] = ownerRef;
                                Core.Execute(req, userRef, null);
                            }
                        }
                        break;

                    case CascadeType.UserOwned:
                        foreach (var relatedEntity in relatedEntities.Value.Entities)
                        {
                            var currentOwner = dbEntity.Attributes["ownerid"] as EntityReference;
                            if (Core.GetDbRow(relatedEntity.ToEntityReference()).ToEntity().GetAttributeValue<EntityReference>("ownerid")?.Id == currentOwner.Id)
                            {
                                req.Target = new Entity(relatedEntity.LogicalName, relatedEntity.Id);
                                req.Target.Attributes["ownerid"] = ownerRef;
                                Core.Execute(req, userRef, null);
                            }
                        }
                        break;
                }
            }
        }

        internal void CheckAssignPermission(Entity entity, EntityReference assignee, EntityReference userRef)
        {
            if (!HasPermission(entity, AccessRights.AssignAccess, userRef))
            {
                throw new FaultException($"Trying to assign '{assignee.Id}' to entity '{entity.LogicalName}'" +
                    $", but calling user with id '{userRef.Id}' does not have assign access for that entity");
            }

            if (!HasPermission(entity, AccessRights.WriteAccess, userRef))
            {
                throw new FaultException($"Trying to assign '{assignee.Id}' to entity '{entity.LogicalName}'" +
                     $", but calling user with id '{userRef.Id}' does not have write access for that entity");
            }
        }

        internal void ResetEnvironment(XrmDb db)
        {
            Initialize();
            //remove any roles added on the fly
            foreach (var role in addedRoles)
            {
                SecurityRoles.Remove(role);
            }
            addedRoles = new List<Guid>();
            this.db = db;
            InitializeSecurityRoles(db);
        }

        internal Privileges GetPrincipalPrivilege(Guid principleId)
        {
            if (!PrinciplePrivilages.Keys.Contains(principleId))
            {
                return null;
            }
            return PrinciplePrivilages[principleId];
        }

        private AccessDepthRight JoinAccess(AccessDepthRight adr1, AccessDepthRight adr2)
        {
            var newAdr = adr1.ToDictionary(x => x.Key, x => x.Value);
            var intersect = adr1.Keys.Intersect(adr2.Keys);
            foreach(var access in intersect)
            {
                var d1 = adr1[access];
                var d2 = adr2[access];
                newAdr.Remove(access);
                newAdr.Add(access, (PrivilegeDepth) Math.Max(((int) d1), ((int) d2)));
            }

            foreach (var access in adr2.Keys.Except(intersect))
            {
                newAdr.Add(access, adr2[access]);
            }

            return newAdr;
        }

        private Privileges JoinPrivileges(Privileges privilege1, Dictionary<string, Dictionary<AccessRights, RolePrivilege>> privileges2)
        {
            Privileges privilage2Modified = privileges2.Select(x =>
                new KeyValuePair<string, AccessDepthRight>(
                    x.Key,
                    x.Value.Select(y =>
                        new KeyValuePair<AccessRights, PrivilegeDepth>(y.Key, y.Value.PrivilegeDepth))
                        .ToDictionary(v => v.Key, v => v.Value)))
                .ToDictionary(v => v.Key, v => v.Value);

            return JoinPrivileges(privilege1, privilage2Modified);
        }

        private Privileges JoinPrivileges(Privileges privilege1, Privileges privilege2)
        {
            var newPriv = privilege1.ToDictionary(x => x.Key, x => x.Value.ToDictionary(y => y.Key, y => y.Value));
            var intersection = privilege1.Keys.Intersect(privilege2.Keys);
            foreach(var logicalName in intersection)
            {
                var a1 = privilege1[logicalName];
                var a2 = privilege2[logicalName];
                var aJoined = JoinAccess(a1, a2);
                newPriv.Remove(logicalName);
                newPriv.Add(logicalName, aJoined);
            }
            foreach (var logicalName in privilege2.Keys.Except(intersection))
            {
                newPriv.Add(logicalName, privilege2[logicalName]);
            }
            return newPriv;
        }

        internal void AddPrinciplePrivileges(Guid principleId, Privileges privileges)
        {
            // TODO: Handle basic privilege of the user does not have access yet.
            var currentPrivileges = new Privileges();
            if (PrinciplePrivilages.ContainsKey(principleId))
            {
                currentPrivileges = PrinciplePrivilages[principleId];
            }

            // Combine users current privilege with the securityRoles
            currentPrivileges = JoinPrivileges(currentPrivileges, privileges);

            // Update the principles privileges;
            if (PrinciplePrivilages.ContainsKey(principleId))
            {
                PrinciplePrivilages.Remove(principleId);
            }
            PrinciplePrivilages.Add(principleId, currentPrivileges);
        } 

        private void AddPrinciplePrivileges(Guid principleId, Guid[] roles)
        {
            if (!roles.Any())
            {
                return;
            }

            // Get users current privilgees
            var currentPrivileges = new Privileges();
            if (PrinciplePrivilages.ContainsKey(principleId))
            {
                currentPrivileges = PrinciplePrivilages[principleId];
            }

            // Combine users current privilege with the securityRoles
            foreach (var roleId in roles)
            {
                if (!SecurityRoles.TryGetValue(roleId, out var securityRole))
                {
                    throw new MockupException(
                        $"Security role with id '{roleId}' was not found. " +
                        $"Ensure the security role exists in the metadata or has been added via AddSecurityRole.");
                }

                currentPrivileges = JoinPrivileges(currentPrivileges, securityRole.Privileges);
            }

            // Update the principles privileges;
            if (PrinciplePrivilages.ContainsKey(principleId))
            {
                PrinciplePrivilages.Remove(principleId);
            }
            PrinciplePrivilages.Add(principleId, currentPrivileges);
        }

        internal void SetSecurityRole(EntityReference entRef, Guid[] secRoles)
        {
            if (!secRoles.Any())
            {
                return;
            }

            AddPrinciplePrivileges(entRef.Id, secRoles);
            var user = Core.GetDbRow(entRef).ToEntity();
            var relationship = entRef.LogicalName == LogicalNames.SystemUser ? new Relationship("systemuserroles_association") : new Relationship("teamroles_association");
            var roles = secRoles
                .Select(sr => SecurityRoleMapping.Where(srm => srm.Value == sr).Select(srm => srm.Key))
                .Select(roleGuids =>
                    Core.GetDbTable("role")
                    .Select(x => x.ToEntity())
                    .Where(r => roleGuids.Contains(r.Id))
                )
                .Select(roleEntities =>
                    roleEntities.First(e => e.GetAttributeValue<EntityReference>("businessunitid").Id == user.GetAttributeValue<EntityReference>("businessunitid").Id))
                .Select(r => r.ToEntityReference());

            var assocHandler = Core.RequestHandlers.Find(x => x is AssociateRequestHandler);
            assocHandler.Execute(new AssociateRequest
            {
                RelatedEntities = new EntityReferenceCollection(roles.ToList()),
                Relationship = relationship,
                Target = entRef
            }, Core.AdminUserRef);
        }

        internal HashSet<SecurityRole> GetSecurityRoles(EntityReference caller)
        {
            var securityRoles = new HashSet<SecurityRole>();
            var relationship = caller.LogicalName == LogicalNames.SystemUser ? new Relationship("systemuserroles_association") : new Relationship("teamroles_association");
            var callerEntity = Core.GetDbEntityWithRelatedEntities(caller, EntityRole.Referenced, Core.AdminUserRef, null, relationship);
            if (callerEntity == null) return securityRoles;
            var roles = callerEntity.RelatedEntities.ContainsKey(relationship) ? callerEntity.RelatedEntities[relationship] : new EntityCollection();
            foreach (var role in roles.Entities)
            {
                securityRoles.Add(SecurityRoles[SecurityRoleMapping[role.Id]]);
            }
            return securityRoles;
        }

        private bool IsMemberOfTeam(EntityReference team, EntityReference user)
        {
            return Core.GetDbTable(LogicalNames.TeamMembership)
                .Select(x => x.ToEntity())
                .Where(tm => tm.GetAttributeValue<Guid>("systemuserid") == user.Id && tm.GetAttributeValue<Guid>("teamid") == team.Id)
                .Any();
        }

        private bool IsInBusinessUnit(EntityReference owner, Guid businessunitId)
        {
            var usersInBusinessUnit = Core.GetDbTable(LogicalNames.SystemUser).Select(x => x.ToEntity()).Where(u => u.GetAttributeValue<EntityReference>("businessunitid")?.Id == businessunitId);
            if (usersInBusinessUnit.Any(u => owner.Id == u.Id))
            {
                return true;
            }
            else
            {
                var teamsInBusinessUnit = Core.GetDbTable(LogicalNames.Team).Select(x => x.ToEntity()).Where(u => u.GetAttributeValue<EntityReference>("businessunitid")?.Id == businessunitId);
                return teamsInBusinessUnit.Any(u => owner.Id == u.Id);
            }
        }

        private bool IsInBusinessUnitTree(EntityReference owner, Guid businessunitId)
        {
            if (IsInBusinessUnit(owner, businessunitId)) return true;

            var childBusinessUnits = Core.GetDbTable(LogicalNames.BusinessUnit).Select(x => x.ToEntity()).Where(b => b.GetAttributeValue<EntityReference>("parentbusinessunitid")?.Id == businessunitId);
            return childBusinessUnits.Any(b => IsInBusinessUnitTree(owner, b.Id));
        }

        internal bool HasTeamMemberPermission(Entity entity, AccessRights access, EntityReference caller)
        {
            if (caller.LogicalName != LogicalNames.SystemUser)
                return false;

            // Owner-team check only applies to owned entities. Business-owned and
            // organization-owned entities (e.g. systemuser) have no ownerid, but the caller
            // may still be granted access through a security role assigned to one of their
            // teams, so we must still enumerate team memberships below.
            if (entity.Attributes.ContainsKey("ownerid"))
            {
                var owner = entity.GetAttributeValue<EntityReference>("ownerid");

                // Check if owner is a team, and if user is member of that team, then check access for that team
                if (owner.LogicalName == LogicalNames.Team && IsMemberOfTeam(owner, caller))
                {
                    return HasPermission(entity, access, owner);
                }
            }

            // Check if any teams that the user is a member of have access 
            var teamIds = Core.GetDbTable(LogicalNames.TeamMembership)
            .Select(x => x.ToEntity())
            .Where(tm => tm.GetAttributeValue<Guid>("systemuserid") == caller.Id)
            .Select(tm => tm.GetAttributeValue<Guid>("teamid"))
            .ToList();

            return teamIds.Any(teamId => HasPermission(entity, access, new EntityReference(LogicalNames.Team, teamId)));
        }

        internal bool HasCallerPermission(Entity entity, AccessRights access, EntityReference caller)
        {
            var privilages = GetPrincipalPrivilege(caller.Id);

            if (privilages == null)
                return false;

            if (!privilages.ContainsKey(entity.LogicalName))
                return false;

            if (!privilages[entity.LogicalName].ContainsKey(access))
                return false;

            var maxRole  = privilages[entity.LogicalName][access];

            // if max privelege depth is global, then caller can access all entities
            if (maxRole == PrivilegeDepth.Global) return true;

            // RecordFilter privilege depth is not properly handled, and are instead handled as Global
            if (maxRole == PrivilegeDepth.RecordFilter) return true;

            // sets owner of entity to caller if there is no owner already
            if (access == AccessRights.CreateAccess && !entity.Attributes.ContainsKey("ownerid"))
            {
                entity["ownerid"] = caller;
            }

            if (!entity.Attributes.ContainsKey("ownerid"))
            {
                // User/team-owned records always carry an ownerid. Records without one are
                // business-owned or organization-owned (e.g. systemuser, team, businessunit),
                // whose access is scoped by the owning business unit rather than an owner.
                // Without this, holding a non-global privilege (e.g. AppendTo on systemuser)
                // would always be denied because the ownerid-based checks below cannot apply.
                return HasBusinessScopedPermission(entity, maxRole, caller);
            }

            var owner = entity.GetAttributeValue<EntityReference>("ownerid");

            // if the caller is the owner of the entity, he has access
            if (owner.Id == caller.Id) return true;

            var callerRow = Core.GetDbRow(caller);

            if (maxRole == PrivilegeDepth.Local)
            {
                // if the owner-user or the owner-team is in the same BU as the caller, he has access
                if (IsInBusinessUnit(owner, callerRow.GetColumn<DbRow>("businessunitid").Id))
                    return true;
            }

            if (maxRole == PrivilegeDepth.Deep)
            {
                // if the owner-user or the owner-team is in the same BU, or sub BU's, as the caller, he has access
                if (IsInBusinessUnitTree(owner, callerRow.GetColumn<DbRow>("businessunitid").Id))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines access for entities that have no ownerid (business-owned or
        /// organization-owned entities). Organization-owned entities have a single
        /// (organization) privilege scope, so any granted privilege applies to every
        /// record. Business-owned entities are scoped by their owning business unit.
        /// </summary>
        private bool HasBusinessScopedPermission(Entity entity, PrivilegeDepth maxRole, EntityReference caller)
        {
            var ownershipType = Metadata.EntityMetadata.GetMetadata(entity.LogicalName).OwnershipType;

            // Organization-owned entities have a single (organization) privilege scope,
            // so holding the privilege at any depth grants access to every record.
            if (ownershipType.HasValue && ownershipType.Value.HasFlag(OwnershipTypes.OrganizationOwned))
                return true;

            // User (Basic) access is ownership-based: per Dataverse it grants records the user
            // owns plus records shared with them. Business-owned records (systemuser, team,
            // businessunit) have no user owner, so the only such record a caller "owns" is their
            // own (e.g. their own systemuser record). Shares are handled by HasSharePermission.
            if (maxRole == PrivilegeDepth.Basic)
                return entity.Id == caller.Id;

            var recordBu = GetOwningBusinessUnit(entity);

            // Fail closed: a business-owned record whose owning business unit can't be resolved
            // must not be widened to every caller. In practice this is always resolvable (the
            // record, or its stored row, carries a business unit); denying here only guards
            // against a malformed record or an unexpectedly ownerless business-owned table.
            if (recordBu == null) return false;

            var callerRow = Core.GetDbRowOrNull(caller);
            if (callerRow == null) return false;

            var callerBu = callerRow.GetColumn<DbRow>("businessunitid")?.Id;
            if (callerBu == null) return false;

            switch (maxRole)
            {
                // Business Unit (Local): records whose owning business unit is the caller's.
                case PrivilegeDepth.Local:
                    return recordBu.Value == callerBu.Value;

                // Parent: Child (Deep): the caller's business unit and all subordinate units.
                case PrivilegeDepth.Deep:
                    return IsBusinessUnitInTree(recordBu.Value, callerBu.Value);

                default:
                    return false;
            }
        }

        private Guid? GetOwningBusinessUnit(Entity entity)
        {
            if (entity.LogicalName == LogicalNames.BusinessUnit)
                return entity.Id;

            var bu = entity.GetAttributeValue<EntityReference>("businessunitid")
                     ?? entity.GetAttributeValue<EntityReference>("owningbusinessunit");
            if (bu != null) return bu.Id;

            // The supplied entity may be a partial projection (e.g. a RetrieveMultiple result
            // that didn't select the business unit column), so resolve from the stored record
            // rather than trusting the instance - a missing column must not silently widen access.
            if (entity.Id == Guid.Empty) return null;

            var row = Core.GetDbRowOrNull(entity.ToEntityReference());
            if (row == null) return null;

            return GetBusinessUnitColumnId(row, "businessunitid")
                   ?? GetBusinessUnitColumnId(row, "owningbusinessunit");
        }

        private static Guid? GetBusinessUnitColumnId(DbRow row, string attribute)
        {
            // DbRow's indexer throws for attributes absent from the table's metadata, so guard first.
            if (!row.AttributeMetadata.ContainsKey(attribute)) return null;

            switch (row[attribute])
            {
                case DbRow dbRow: return dbRow.Id;
                case EntityReference reference: return reference.Id;
                default: return null;
            }
        }

        private bool IsBusinessUnitInTree(Guid businessUnitId, Guid rootBusinessUnitId)
        {
            if (businessUnitId == rootBusinessUnitId) return true;

            var childBusinessUnits = Core.GetDbTable(LogicalNames.BusinessUnit)
                .Select(x => x.ToEntity())
                .Where(b => b.GetAttributeValue<EntityReference>("parentbusinessunitid")?.Id == rootBusinessUnitId);

            return childBusinessUnits.Any(b => IsBusinessUnitInTree(businessUnitId, b.Id));
        }

        internal bool HasSharePermission(Entity entity, AccessRights access, EntityReference caller)
        {
            var entityRef = entity.ToEntityReference();
            return (Shares.ContainsKey(entityRef) &&
                Shares[entityRef].ContainsKey(caller) &&
                Shares[entityRef][caller].HasFlag(access));
        }

        internal bool HasPermission(EntityReference entityReference, AccessRights access, EntityReference caller)
        {
            return HasPermission(Core.GetDbRow(entityReference).ToEntity(), access, caller);
        }

        internal bool HasPermission(Entity entity, AccessRights access, EntityReference caller)
        {
            // check if system has no security roles for this entity: it is a case with linkentities which have no security roles
            if (!SecurityRoles.Any(s => s.Value.Privileges.Any(p => p.Key == entity.LogicalName)))
            {
                return true;
            }

            if (caller.Id == Core.AdminUserRef.Id) return true;

            // check if the caller has the permissions needed for access
            if (HasCallerPermission(entity, access, caller)) return true;

            // check if any of the Teams that the caller is a member of has access
            if (HasTeamMemberPermission(entity, access, caller)) return true;

            // check if any of the Teams that the caller is a member of has access
            if (HasAccessTeamMemberPermission(entity, access, caller)) return true;

            // check if there are any shares of the entity with the caller
            if (HasSharePermission(entity, access, caller)) return true;

            // not part of HasPermissions check - TODO: move to Utility: SetOwner
            var parentChangeRelationships = Metadata.EntityMetadata.GetMetadata(entity.LogicalName).ManyToOneRelationships
                .Where(r => r.CascadeConfiguration.Reparent == CascadeType.Cascade || r.CascadeConfiguration.Reparent == CascadeType.Active)
                .Where(r => entity.Attributes.ContainsKey(r.ReferencingAttribute));

            //cope with entity[pcr.ReferencingAttribute] being null
            foreach (var pcr in parentChangeRelationships)
            {
                if (entity.Contains(pcr.ReferencingAttribute) && entity[pcr.ReferencingAttribute] != null)
                {
                    var refEntity = Core.GetDbRowOrNull(new EntityReference(pcr.ReferencedEntity, Utility.GetGuidFromReference(entity[pcr.ReferencingAttribute])));
                    if (refEntity != null)
                    {
                        // Business-owned / organization-owned parents (e.g. systemuser) have no
                        // ownerid, so there is no owner to match the caller against.
                        var refOwner = refEntity.GetColumn<DbRow>("ownerid");
                        if (refOwner != null && refOwner.Id == caller.Id)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public Security Clone()
        {
            var s = new Security(this.Core, this.Metadata, this.SecurityRoles.Values.ToList(),this.db)
            {
                SecurityRoleMapping = this.SecurityRoleMapping.ToDictionary(x => x.Key, x => x.Value),
                Shares = this.Shares.ToDictionary(x => x.Key, x => x.Value.ToDictionary(y => y.Key, y => y.Value)),
                addedRoles = new List<Guid>(this.addedRoles)
            };
            s.PrinciplePrivilages = this.PrinciplePrivilages.ToDictionary(x => x.Key, x => x.Value.ToDictionary(y => y.Key, y => y.Value));
            return s;
        }

        public SecurityModelDTO ToSerializableDTO()
        {
            var jsonObj = new SecurityModelDTO
            {
                AddedRoles = this.addedRoles,
                PrinciplePrivilages = this.PrinciplePrivilages,
                SecurityRoleMapping = this.SecurityRoleMapping,
                Shares = this.Shares.ToDictionary(x => new EntityReferenceDTO { Id = x.Key.Id, LogicalName = x.Key.LogicalName}, x => x.Value.ToDictionary(y => new EntityReferenceDTO { Id = y.Key.Id, LogicalName = y.Key.LogicalName}, y => y.Value))
            };
            return jsonObj;
        }
        internal static Security RestoreSerializableDTO(Security current, SecurityModelDTO model)
        {
            var s = new Security(current.Core, current.Metadata, current.SecurityRoles.Values.ToList(), current.db)
            {
                SecurityRoleMapping = model.SecurityRoleMapping.ToDictionary(x => x.Key, x => x.Value),
                Shares = model.Shares.ToDictionary(x => new EntityReference { Id = x.Key.Id, LogicalName = x.Key.LogicalName }, x => x.Value.ToDictionary(y => new EntityReference { Id = y.Key.Id, LogicalName = y.Key.LogicalName }, y => y.Value)),
                addedRoles = new List<Guid>(model.AddedRoles)
            };
            s.PrinciplePrivilages = model.PrinciplePrivilages.ToDictionary(x => x.Key, x => x.Value.ToDictionary(y => y.Key, y => y.Value));
            return s;
        }

        internal SecurityRole GetSecurityRole(string name)
        {
            return SecurityRoles.Single(x => x.Value.Name == name).Value;
        }

        internal List<SecurityRole> GetSecurityRoles()
        {
            return SecurityRoles.Select(x => x.Value).ToList();
        }

        internal void AddSecurityRole(SecurityRole role)
        {
            SecurityRoles.Add(role.RoleId, role);
            addedRoles.Add(role.RoleId);
            AddRoleToDb(this.db, role, Core.RootBusinessUnitRef);
        }

        internal void AddRoleToDb(XrmDb db, SecurityRole newRole, EntityReference businessUnit)
        {
            var roleMeta = Metadata.EntityMetadata.GetMetadata("role");
            var role = new Entity("role")
            {
                Id = newRole.RoleId
            };
            role[roleMeta.PrimaryIdAttribute] = role.Id;
            role["businessunitid"] = businessUnit;
            role["name"] = newRole.Name;
            role["roletemplateid"] = newRole.RoleTemplateId;
            role["createdby"] = Core.AdminUserRef;
            role["createdon"] = DateTime.UtcNow.Add(Core.TimeOffset);
            role["modifiedby"] = Core.AdminUserRef;
            role["modifiedon"] = DateTime.UtcNow.Add(Core.TimeOffset);
            db.Add(role);
            SecurityRoleMapping.Add(role.Id, newRole.RoleId);
        }

        internal Entity GetAccessTeam(Guid teamTemplateId, Guid recordId)
        {
            return Core.GetDbTable("team")
                        .Select(x => x.ToEntity())
                        .Where(x => x.GetAttributeValue<OptionSetValue>("teamtype").Value == 1)
                        .Where(x => x.Attributes.Contains("teamtemplateid") && x.GetAttributeValue<EntityReference>("teamtemplateid").Id == teamTemplateId)
                        .Where(x => x.GetAttributeValue<EntityReference>("regardingobjectid").Id == recordId)
                        .SingleOrDefault();
        }
        internal IEnumerable<Entity> GetAccessTeams(Guid recordId)
        {
            return Core.GetDbTable("team")
                        .Select(x => x.ToEntity())
                        .Where(x => x.GetAttributeValue<OptionSetValue>("teamtype").Value == 1)
                        .Where(x => (x.GetAttributeValue<EntityReference>("regardingobjectid").Id == recordId));
        }
        internal Entity AddAccessTeam(Guid teamTemplateId, EntityReference record)
        {
            //create the team
            var team = new Entity("team");
            team.Id = Guid.NewGuid();
            team["teamtype"] = new OptionSetValue(1);
            team["teamtemplateid"] = new EntityReference("teamtemplate", teamTemplateId);
            team["regardingobjectid"] = record;
            team["name"] = record.Id.ToString() + "+" + teamTemplateId.ToString();
            db.Add(team);

            return GetAccessTeam(teamTemplateId, record.Id);
        }

        internal Entity GetTeamMembership(Guid teamId, Guid systemuserId)
        {
            return Core.GetDbTable("teammembership")
                        .Select(x => x.ToEntity())
                        .Where(x => x.GetAttributeValue<Guid>("teamid") == teamId)
                        .Where(x => x.GetAttributeValue<Guid>("systemuserid") == systemuserId)
                        .SingleOrDefault();
        }
        internal Entity GetPOA(Guid principalId, Guid objectId)
        {
            return Core.GetDbTable("principalobjectaccess")
                        .Select(x => x.ToEntity())
                        .Where(x => x.GetAttributeValue<Guid>("principalid") == principalId)
                        .Where(x => x.GetAttributeValue<Guid>("objectid") == objectId)
                        .SingleOrDefault();
        }

        internal Entity AddTeamMembership(Guid teamId, Guid systemuserId)
        {
            var teammembership = new Entity("teammembership");
            teammembership.Id = Guid.NewGuid();
            teammembership["teamid"] = teamId;
            teammembership["systemuserid"] = systemuserId;
            db.Add(teammembership);

            return GetTeamMembership(teamId, systemuserId);
        }
        internal Entity AddPOA(Guid principalId, EntityReference objectId, int accessRightsMask)
        {
            var poa = new Entity("principalobjectaccess");
            poa.Id = Guid.NewGuid();
            poa["principalid"] = principalId;
            poa["objectid"] = objectId.Id;
            poa["accessrightsmask"] = accessRightsMask;
            poa["objecttypecode"] = objectId.LogicalName;

            db.Add(poa);

            return GetPOA(principalId, objectId.Id);
        }
        internal void UpdatePOAMask(Guid poaId, int accessRightsMask)
        {
            var existing = db.GetDbRow("principalobjectaccess", poaId).ToEntity();
            existing["accessrightsmask"] = existing.GetAttributeValue<int>("accessrightsmask") | accessRightsMask;
            db.Update(existing);
        }
        internal void OverwritePOAMask(Guid poaId, int accessRightsMask)
        {
            var existing = db.GetDbRow("principalobjectaccess", poaId).ToEntity();
            existing["accessrightsmask"] = accessRightsMask;
            db.Update(existing);
        }

        internal bool HasAccessTeamMemberPermission(Entity entity, AccessRights access, EntityReference caller)
        {
            //check if an access team exists for this entity
            var accessTeams = Core.GetDbTable(LogicalNames.Team)
                              .Select(x => x.ToEntity())
                              .Where(x => x.GetAttributeValue<OptionSetValue>("teamtype").Value == 1)
                              .Where(x => x.GetAttributeValue<EntityReference>("regardingobjectid").Id == entity.Id);

            if (!accessTeams.Any())
                return false;

            var accessTeamMemberships = Core.GetDbTable(LogicalNames.TeamMembership)
                                        .Select(x => x.ToEntity())
                                        .Where(x => x.GetAttributeValue<Guid>("systemuserid") == caller.Id)
                                        .Where(x => accessTeams.Select(y => y.Id).Contains(x.GetAttributeValue<Guid>("teamid")));

            if (!accessTeamMemberships.Any())
                return false;

            foreach (var atm in accessTeamMemberships)
            {
                var poa = GetPOA(atm.GetAttributeValue<Guid>("systemuserid"), entity.Id);
                if (poa != null)
                {
                    var mask = poa.GetAttributeValue<int>("accessrightsmask");
                    //check if the mask covers the requested access right
                    if ((mask & (int)access) > 0)
                        return true;
                }
            }

            return false;
        }
    }
}