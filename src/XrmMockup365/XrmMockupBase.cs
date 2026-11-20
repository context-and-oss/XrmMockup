using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using System.IO;
using System.Diagnostics;
using System.Collections.ObjectModel;
using DG.Tools.XrmMockup.Internal;

namespace DG.Tools.XrmMockup
{
    using Privileges = Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>;

    /// <summary>
    /// A Mockup of a CRM instance
    /// </summary>
    public abstract partial class XrmMockupBase {

        /// <summary>
        /// AdminUser for the Mockup instance
        /// </summary>
        public EntityReference AdminUser {
            get {
                return Core.AdminUserRef;
            }
        }

        /// <summary>
        /// AdminUser for the Mockup instance
        /// </summary>
        public EntityReference RootBusinessUnit {
            get {
                return Core.RootBusinessUnitRef;
            }
        }

        /// <summary>
        /// Base currency for the Mockup instance
        /// </summary>
        public EntityReference BaseCurrency {
            get {
                return Core.BaseCurrency;
            }
        }


        private bool HasProxyTypes = false;
        private Core Core;
        private MockupServiceProviderAndFactory ServiceFactory;

        private readonly Dictionary<string, long> timers;
        public IReadOnlyDictionary<string, long> Timers => new ReadOnlyDictionary<string, long>(timers);

        /// <summary>
        /// A snapshot of the trace messages emitted to <see cref="ITracingService"/> during
        /// plugin, workflow and custom API execution, allowing tests to assert on them.
        /// <para>Only populated when using the default tracing service factory, i.e. when a
        /// custom <see cref="XrmMockupSettings.TracingServiceFactory"/> has not been supplied.</para>
        /// </summary>
        public IReadOnlyList<string> TraceLog =>
            (Core.TracingServiceFactory as TracingServiceFactory)?.TraceLog ?? Array.Empty<string>();

        /// <summary>
        /// Clears all collected trace messages from the <see cref="TraceLog"/>.
        /// </summary>
        public void ClearTraceLog() =>
            (Core.TracingServiceFactory as TracingServiceFactory)?.Clear();

        /// <summary>
        /// The trace messages emitted during plugin execution, grouped by the plugin execution
        /// that produced them, mirroring the plugintracelog records of a real Dynamics 365
        /// environment. Each entry carries the invoking type, message, primary entity, depth,
        /// correlation id, mode, timing, the ordered list of trace messages and any exception.
        /// <para>Internal XrmMockup system plugins are excluded.</para>
        /// </summary>
        public IReadOnlyList<PluginTraceLog> PluginTraceLog => Core.PluginTraceLogs;

        /// <summary>
        /// Clears all collected <see cref="PluginTraceLog"/> entries.
        /// </summary>
        public void ClearPluginTraceLog() => Core.ClearPluginTraceLogs();

        protected XrmMockupSettings Settings { get; }
        protected MetadataSkeleton Metadata { get; }
        protected List<Entity> Workflows { get; }
        protected List<SecurityRole> SecurityRoles { get; }

        /// <summary>
        /// Create a new XrmMockup instance
        /// </summary>
        protected XrmMockupBase(XrmMockupSettings settings, MetadataSkeleton metadata = null, List<Entity> workflows = null, List<SecurityRole> securityRoles = null) {
            timers = new Dictionary<string, long>();
            Settings = settings;

            Stopwatch stopwatch = Stopwatch.StartNew();
            var metadataDirectory = settings.MetadataDirectoryPath ?? "../../Metadata/";
            Metadata = metadata ?? Utility.GetMetadata(metadataDirectory);
            timers[nameof(Utility.GetMetadata)] = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            Workflows = workflows ?? Utility.GetWorkflows(metadataDirectory);
            timers[nameof(Utility.GetWorkflows)] = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            SecurityRoles = securityRoles ?? Utility.GetSecurityRoles(metadataDirectory);
            timers[nameof(Utility.GetSecurityRoles)] = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            Core = new Core(settings, Metadata, Workflows, SecurityRoles);
            timers[nameof(Core)] = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            ServiceFactory = new MockupServiceProviderAndFactory(Core);
            timers[nameof(ServiceFactory)] = stopwatch.ElapsedMilliseconds;

            if (settings.EnableProxyTypes == true) {
                stopwatch.Restart();
                EnableProxyTypes();
                timers[nameof(EnableProxyTypes)] = stopwatch.ElapsedMilliseconds;
            }

            stopwatch.Stop();
        }

        /// <summary>
        /// Create a new XrmMockup instance using cached static metadata
        /// </summary>
        protected XrmMockupBase(XrmMockupSettings settings, StaticMetadataCache staticCache) {
            timers = new Dictionary<string, long>();
            Settings = settings;
            Metadata = staticCache.Metadata;
            Workflows = staticCache.Workflows;
            SecurityRoles = staticCache.SecurityRoles;

            Stopwatch stopwatch = Stopwatch.StartNew();
            Core = new Core(settings, staticCache);
            timers[nameof(Core)] = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            ServiceFactory = new MockupServiceProviderAndFactory(Core);
            timers[nameof(ServiceFactory)] = stopwatch.ElapsedMilliseconds;

            if (settings.EnableProxyTypes == true) {
                stopwatch.Restart();
                // Proxy types are already set up in the static cache, just mark as enabled
                HasProxyTypes = true;
                timers[nameof(EnableProxyTypes)] = stopwatch.ElapsedMilliseconds;
            }

            stopwatch.Stop();
        }

        /// <summary>
        /// Enable early-bound types from the given context
        /// </summary>
        private void EnableProxyTypes() {
            if (HasProxyTypes) return;

            if (Settings.Assemblies?.Any() ?? false)
            {
                foreach (var assembly in Settings.Assemblies)
                {
                    Core.EnableProxyTypes(assembly);
                }
                HasProxyTypes = true;
            }
            else
            {
                List<string> exclude = new List<string> {
                    "Microsoft.Xrm.Sdk.dll",
                    "Microsoft.Crm.Sdk.Proxy.dll"
                };

                var regex = new Regex("^XrmMockup.*\\.dll$");
                var assemblies = new List<Assembly>();
                var addedAssemblies = new HashSet<string>();

                var exeAsm = AppDomain.CurrentDomain.GetAssemblies();
                assemblies.AddRange(exeAsm);
                foreach (var name in exeAsm.Select(x => x.FullName))
                {
                    addedAssemblies.Add(name);
                }

                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                foreach (string dll in Directory.GetFiles(path, "*.dll"))
                {
                    var asm = Assembly.LoadFrom(dll);
                    if (addedAssemblies.Contains(asm.FullName)) continue;

                    assemblies.Add(asm);
                    addedAssemblies.Add(asm.FullName);
                }

                var useableAssemblies =
                    assemblies
                    .Where(asm => asm.CustomAttributes.Any(attr => attr.AttributeType.Name.Equals("ProxyTypesAssemblyAttribute")))
                    .Where(asm => !exclude.Contains(asm.ManifestModule.Name) && !regex.IsMatch(asm.ManifestModule.Name))
                    .ToList();

                if (useableAssemblies?.Any() == true)
                {
                    foreach (var asm in useableAssemblies)
                    {
                        Core.EnableProxyTypes(asm);
                    }
                    HasProxyTypes = true;
                }
            }
        }

        /// <summary>
        /// Resets the local test data
        /// </summary>
        public void ResetEnvironment() {
            Core.ResetEnvironment();
            ClearTraceLog();
        }


        internal OrganizationResponse Execute(OrganizationRequest request, EntityReference userRef, PluginContext pluginContext) {
            return Core.Execute(request, userRef, pluginContext);
        }

        /// <summary>
        /// Increase the local time that mockup uses by an offset of the current time
        /// </summary>
        /// <param name="offset"></param>
        public void AddTime(TimeSpan offset) {
            Core.AddTime(offset);
        }

        /// <summary>
        /// Increase the local time that mockup uses by an offset of the current time
        /// </summary>
        /// <param name="days"></param>
        /// <param name="hours"></param>
        /// <param name="minutes"></param>
        /// <param name="seconds"></param>
        public void AddTime(int days, int hours, int minutes, int seconds) {
            AddTime(new TimeSpan(days, hours, minutes, seconds));
        }

        /// <summary>
        /// Increase the local time that mockup uses by an offset of the current time
        /// </summary>
        /// <param name="days"></param>
        /// <param name="hours"></param>
        /// <param name="minutes"></param>
        public void AddTime(int days, int hours, int minutes) {
            AddTime(new TimeSpan(days, hours, minutes, 0));
        }

        /// <summary>
        /// Increase the local time that mockup uses by an offset of the current time
        /// </summary>
        /// <param name="days"></param>
        /// <param name="hours"></param>
        public void AddTime(int days, int hours) {
            AddTime(new TimeSpan(days, hours, 0, 0));
        }

        /// <summary>
        /// Increase the local time that mockup uses by an offset of the current time
        /// </summary>
        /// <param name="days"></param>
        public void AddTime(int days) {
            AddTime(new TimeSpan(days, 0, 0, 0));
        }

        /// <summary>
        /// Increase the local time that mockup uses by an offset of days
        /// </summary>
        /// <param name="days"></param>
        public void AddDays(int days) {
            AddTime(new TimeSpan(days, 0, 0, 0));
        }

        /// <summary>
        /// Increase the local time that mockup uses by an offset of hours
        /// </summary>
        /// <param name="hours"></param>
        public void AddHours(int hours) {
            AddTime(new TimeSpan(hours, 0, 0));
        }

        /// <summary>
        /// Add a workflow from a serialized workflow entity
        /// </summary>
        /// <param name="path"></param>
        public void AddWorkflow(string path) {
            var workflow = Utility.GetWorkflow(path);
            Core.AddWorkflow(workflow);
        }

        /// <summary>
        /// Returns true if an entity exists in the database with the same id, and has at least the attributes of the parameter.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool ContainsEntity(Entity entity) {
            return Core.ContainsEntity(entity);
        }

        /// <summary>
        /// Adds entities directly into the database, without modifying them
        /// </summary>
        /// <param name="entities"></param>
        public void PopulateWith(params Entity[] entities) {
            Core.PopulateWith(entities);
        }

        /// <summary>
        /// Prefills the local database with data from the online service based on the query.
        /// Only works when OnlineDataServiceFactory or OnlineEnvironment is configured.
        /// </summary>
        /// <param name="query">The query to execute against the online service.</param>
        public void PrefillDBWithOnlineData(QueryExpression query) {
            Core.PrefillDBWithOnlineData(query);
        }

        /// <summary>
        /// Create a new user with a specific businessunit
        /// </summary>
        /// <param name="service"></param>
        /// <param name="businessUnit"></param>
        /// <param name="securityRoles"></param>
        /// <returns></returns>
        public Entity CreateUser(IOrganizationService service, EntityReference businessUnit, params Guid[] securityRoles) {
            var user = new Entity("systemuser");
            user["businessunitid"] = businessUnit;
            return CreateUser(service, user, securityRoles);
        }


        /// <summary>
        /// Create a new user from an entity. Remember to provide an existing businessunit in the entity.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="user"></param>
        /// <param name="securityRoles"></param>
        /// <returns></returns>
        public Entity CreateUser(IOrganizationService service, Entity user, params Guid[] securityRoles) {
            if (user.LogicalName != "systemuser") {
                throw new MockupException($"This method is only for creating users. You tried to create a '{user.LogicalName}'");
            }
            if (user.GetAttributeValue<EntityReference>("businessunitid") == null) {
                throw new MockupException("You tried to create a user with security roles, but did not specify a businessunit in the user's attributes");
            }
            user.Id = service.Create(user);
            Core.SetSecurityRoles(new EntityReference(LogicalNames.SystemUser, user.Id), securityRoles);
            return service.Retrieve(LogicalNames.SystemUser, user.Id, new ColumnSet(true));
        }

        /// <summary>
        /// Create a new owner team with a specific businessunit
        /// </summary>
        /// <param name="service"></param>
        /// <param name="businessUnit"></param>
        /// <param name="securityRoles"></param>
        /// <returns></returns>
        public Entity CreateTeam(IOrganizationService service, EntityReference businessUnit, params Guid[] securityRoles) {
            var team = new Entity(LogicalNames.Team);
            team["businessunitid"] = businessUnit;
            return CreateTeam(service, team, securityRoles);
        }

        /// <summary>
        /// Create a new owner team from an entity. Remember to provide an existing businessunit in the entity.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="team"></param>
        /// <param name="securityRoles"></param>
        /// <returns></returns>
        public Entity CreateTeam(IOrganizationService service, Entity team, params Guid[] securityRoles) {
            if (team.LogicalName != "team") {
                throw new MockupException($"This method is only for creating teams. You tried to create a '{team.LogicalName}'");
            }
            if (team.GetAttributeValue<EntityReference>("businessunitid") == null) {
                throw new MockupException("You tried to create a team with security roles, but did not specify a businessunit in the team's attributes");
            }
            team["teamtype"] = new OptionSetValue(0);
                team.Id = service.Create(team);
            Core.SetSecurityRoles(new EntityReference(LogicalNames.Team, team.Id), securityRoles);

            return service.Retrieve(LogicalNames.Team, team.Id, new ColumnSet(true));
        }

        /// <summary>
        /// Adds security roles to a given user or team
        /// </summary>
        /// <param name="priniple">User or Team</param>
        /// <param name="securityRoles">List of security role guids</param>
        public void AddSecurityRolesToPrinciple(EntityReference priniple, params Guid[] securityRoles)
        {
            Core.SetSecurityRoles(priniple, securityRoles);
        }

        /// <summary>
        /// Add users to a team
        /// </summary>
        /// <param name="team"></param>
        /// <param name="users"></param>
        public void AddUsersToTeam(EntityReference team, params EntityReference[] users)
        {
            var req = new AddMembersTeamRequest
            {
                TeamId = team.Id,
                MemberIds = users.Select(x => x.Id).ToArray()
            };
            Core.Execute(req, AdminUser);
        }

        /// <summary>
        /// Remove users from a team
        /// </summary>
        /// <param name="team"></param>
        /// <param name="users"></param>
        public void RemoveUsersFromTeam(EntityReference team, params EntityReference[] users)
        {
            var req = new RemoveMembersTeamRequest
            {
                TeamId = team.Id,
                MemberIds = users.Select(x => x.Id).ToArray()
            };
            Core.Execute(req, AdminUser);
        }

        /// <summary>
        /// Takes a snapshot of the XrmMockup database
        /// </summary>
        /// <param name="snapshotName"></param>
        public void TakeSnapshot(string snapshotName)
        {
            Core.TakeSnapshot(snapshotName);
        }

        /// <summary>
        /// Retore the XrmMockup database from a snapshot
        /// </summary>
        /// <param name="snapshotName"></param>
        public void RestoreToSnapshot(string snapshotName)
        {
            Core.RestoreToSnapshot(snapshotName);
        }

        /// <summary>
        /// Takes a snapshot of the XrmMockup database data and saves it zip compressed
        /// </summary>
        /// <param name="filename">Filename used for the ZIP archive. Extenstion will be replaced with .zip</param>
        public void TakeZipSnapshot(string filename)
        {
            var json = Core.TakeJsonSnapshot();
            Utility.ZipCompressString(filename, json);
        }

        /// <summary>
        /// Retore the XrmMockup database data from json data saved in a zip archive
        /// </summary>
        /// <param name="filename">Filename of the ZIP archive to be restored</param>
        public void RestoreZipSnapshot(string filename)
        {
            var json = Utility.ZipUncompressString(filename);
            Core.RestoreJsonSnapshot(json);
        }

        /// <summary>
        /// Delete a stored snapshot
        /// </summary>
        /// <param name="snapshotName"></param>
        public void DeleteSnapshot(string snapshotName)
        {
            Core.DeleteSnapshot(snapshotName);
        }

        /// <summary>
        /// Disables triggering of registered plugins. Does not include temporarily plugins. Is set to false when <see cref="ResetEnvironment"/> is called.
        /// </summary>
        /// <param name="include"></param>
        public void DisableRegisteredPlugins(bool include)
        {
            Core.DisableRegisteredPlugins(include);
        }

        /// <summary>
        /// Register additional plugins to be triggered in addition to the existing plugins in the current environment.
        /// Plugins registered temporarily will be deleted when <see cref="ResetEnvironment"/> is called.
        /// </summary>
        /// <param name="scope">The scope of the plugin registration</param>
        /// <param name="basePluginTypes"></param>
        public void RegisterAdditionalPlugins(PluginRegistrationScope scope, params Type[] basePluginTypes)
        {
            Core.RegisterAdditionalPlugins(basePluginTypes, scope);
        }

        /// <summary>
        /// Returns the privilege of the given principle
        /// </summary>
        /// <param name="principleId">Guid of user or team</param>
        /// <returns>A dictionary of entities where each entity contains a dictionary over access rights and privilege depth for the given principle</returns>
        public Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>> GetPrivilege(Guid principleId)
        {
            return Core.GetPrivilege(principleId);
        }
        
        /// <summary>
        /// Checks if a principle has the given access right to an entity
        /// </summary>
        /// <param name="entityRef">Entity to check against</param>
        /// <param name="access">Access to check with</param>
        /// <param name="principleRef">User or team</param>
        /// <returns>If the given principle has permission to 'access' the entity</returns>
        public bool HasPermission(EntityReference entityRef, AccessRights access, EntityReference principleRef)
        {
            return Core.HasPermission(entityRef, access, principleRef);
        }

        /// <summary>
        /// Add entity privileges to the given principle ontop of any existing privileges and security roles
        /// </summary>
        /// <param name="principleRef">EntityReference of a user or team</param>
        /// <param name="privileges">A dictionary of entities where each entity contains a dictionary over access rights and privilege depth</param>
        internal void AddPrivileges(EntityReference principleRef, Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>> privileges)
        {
            Core.AddPrivileges(principleRef, privileges);
        }

        public SecurityRole GetSecurityRole(string roleName)
        {
            return Core.GetSecurityRole(roleName);
        }
        public SecurityRole CloneSecurityRole(string roleName)
        {
            return Core.GetSecurityRole(roleName).Clone();
        }
        public void AddSecurityRole(SecurityRole role)
        {
            Core.AddSecurityRole(role);
        }

        public void ResetTable(string tableName)
        {
            Core.ResetTable(tableName);
        }
    }
}
