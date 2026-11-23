#if DATAVERSE_SERVICE_CLIENT
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.Some.Namespace;
using XrmMockup.PermissionAnalyzer;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmPluginCore.Enums;
using Xunit;
using RolePrivilege = DG.Tools.XrmMockup.RolePrivilege;
using AppContext = System.AppContext;
using Operation = XrmPluginCore.Enums.EventOperation;
using Stage = XrmPluginCore.Enums.ExecutionStage;

namespace DG.XrmMockupTest
{
    public class TestPermissionAnalysis : UnitTestBase
    {
        private static readonly Lazy<Task<PermissionAnalysis>> Analysis = new Lazy<Task<PermissionAnalysis>>(() =>
            PermissionAnalysis.LoadAsync(Path.GetFullPath(Path.Combine(FindRoot(), "tests/TestPluginAssembly365/TestPluginAssembly365.csproj"))));

        public TestPermissionAnalysis(XrmMockupFixture fixture) : base(fixture) { }

        private static string FindRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "XrmMockup.slnx"))) directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found");
        }

        [Fact]
        public async System.Threading.Tasks.Task LoadsReusableSourceAndSelectsTypedPluginWithoutExecutingIt()
        {
            var analysis = await Analysis.Value;
            Assert.Equal("XrmMockup.PermissionAnalysis", System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(analysis.GetType().Assembly).Name);
            Assert.DoesNotContain(System.Runtime.Loader.AssemblyLoadContext.Default.Assemblies,
                a => a.GetName().Name == "XrmMockup.PermissionAnalyzer.Workspace");
            var result = crm.CheckPermissions(analysis, PermissionSelector.Plugin<AccountTagPlugin>(Operation.Create, Stage.PostOperation), SecurityRoles.XrmMockupTestAccessTeam);
            Assert.True(result.Passed);
            Assert.Equal("DG.Some.Namespace.AccountTagPlugin", Assert.Single(result.Manifest.EntryPoints).Type);
            result.ThrowIfFailed();
            Assert.Throws<ArgumentException>(() => crm.CheckPermissions(analysis, PermissionSelector.Plugin<AccountTagPlugin>(Operation.Delete), SecurityRoles.XrmMockupTestAccessTeam));
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateWithoutPluginsStillChecksDirectPrivilegeAndRoleChanges()
        {
            var analysis = await Analysis.Value;
            // Business unit has no registered source plugins, but Create is still required.
            var scope = PermissionSelector.Operation<BusinessUnit>(Operation.Create);
            var role = crm.CloneSecurityRole("XrmMockup Test Access Team");
            role.Privileges["businessunit"] = new Dictionary<AccessRights, RolePrivilege>();
            crm.AddSecurityRole(role);
            var first = crm.CheckPermissions(analysis, scope, role.RoleId);
            Assert.False(first.Passed);
            Assert.Contains(first.Manifest.AllRequirements, r => r.Table == "businessunit" && r.Privilege == XrmMockup.PermissionAnalyzer.Core.Model.Privilege.Create);
            role.Privileges["businessunit"][AccessRights.CreateAccess] = new RolePrivilege { PrivilegeDepth = Microsoft.Crm.Sdk.Messages.PrivilegeDepth.Basic, AccessRight = AccessRights.CreateAccess };
            Assert.True(crm.CheckPermissions(analysis, scope, role.RoleId).Passed);
            Assert.False(first.Passed); // Old results remain snapshots.
        }

        [Fact]
        public async System.Threading.Tasks.Task ParentOwnershipDoesNotOverrideMissingChildCreatePrivilege()
        {
            crm.DisableRegisteredPlugins(true);
            var analysis = await Analysis.Value;
            var role = crm.CloneSecurityRole("XrmMockup Test Access Team");
            role.Privileges[Contact.EntityLogicalName].Remove(AccessRights.CreateAccess);
            crm.AddSecurityRole(role);
            var user = crm.CreateUser(orgAdminService, crm.RootBusinessUnit, role.RoleId);
            var service = crm.CreateOrganizationService(user.Id);
            var parent = new Account { Name = "Owned parent", OwnerId = user.ToEntityReference() };
            parent.Id = orgGodService.Create(parent);
            var scope = PermissionSelector.Operation<Contact>(Operation.Create, c => c.ParentCustomerId);

            var denied = crm.CheckPermissions(analysis, scope, user.ToEntityReference());
            Assert.False(denied.Passed);
            Assert.Contains(denied.Coverage.Missing, missing => missing.Table == Contact.EntityLogicalName &&
                missing.Privilege == XrmMockup.PermissionAnalyzer.Core.Model.Privilege.Create);
            var error = Assert.Throws<System.ServiceModel.FaultException>(() => service.Create(new Contact
            {
                LastName = "Denied child", ParentCustomerId = parent.ToEntityReference()
            }));
            Assert.Contains("does not have Create access", error.Message);

            var childRole = crm.CloneSecurityRole("XrmMockup Test Access Team");
            childRole.Privileges.Clear();
            childRole.Privileges[Contact.EntityLogicalName] = new Dictionary<AccessRights, RolePrivilege>
            {
                { AccessRights.CreateAccess, new RolePrivilege { AccessRight = AccessRights.CreateAccess, PrivilegeDepth = Microsoft.Crm.Sdk.Messages.PrivilegeDepth.Basic } }
            };
            crm.AddSecurityRole(childRole);
            crm.AddSecurityRolesToPrinciple(user.ToEntityReference(), childRole.RoleId);
            var allowed = crm.CheckPermissions(analysis, scope, user.ToEntityReference());
            Assert.DoesNotContain(allowed.Coverage.Missing, missing => missing.Table == Contact.EntityLogicalName &&
                missing.Privilege == XrmMockup.PermissionAnalyzer.Core.Model.Privilege.Create);
            Assert.NotEqual(Guid.Empty, service.Create(new Contact { LastName = "Allowed child", ParentCustomerId = parent.ToEntityReference() }));
        }

        [Fact]
        public async System.Threading.Tasks.Task CombinedRolesAndCurrentUserTeamAssignments()
        {
            var analysis = await Analysis.Value;
            var scope = PermissionSelector.Operation<BusinessUnit>(Operation.Create);
            var denied = crm.CloneSecurityRole("XrmMockup Test Access Team");
            denied.Privileges["businessunit"] = new Dictionary<AccessRights, RolePrivilege>();
            crm.AddSecurityRole(denied);
            var allowed = denied.Clone();
            allowed.Privileges["businessunit"][AccessRights.CreateAccess] = new RolePrivilege { PrivilegeDepth = Microsoft.Crm.Sdk.Messages.PrivilegeDepth.Basic, AccessRight = AccessRights.CreateAccess };
            crm.AddSecurityRole(allowed);
            Assert.False(crm.CheckPermissions(analysis, scope, denied.RoleId).Passed);
            Assert.True(crm.CheckPermissions(analysis, scope, denied.RoleId, allowed.RoleId).Passed);
            var user = crm.CreateUser(orgAdminService, crm.RootBusinessUnit, denied.RoleId);
            Assert.False(crm.CheckPermissions(analysis, scope, user.ToEntityReference()).Passed);
            var team = crm.CreateTeam(orgAdminService, new Team { BusinessUnitId = crm.RootBusinessUnit }, allowed.RoleId);
            orgAdminService.Execute(new AddMembersTeamRequest { TeamId = team.Id, MemberIds = new[] { user.Id } });
            Assert.True(crm.CheckPermissions(analysis, scope, user.ToEntityReference()).Passed);
            orgAdminService.Execute(new RemoveMembersTeamRequest { TeamId = team.Id, MemberIds = new[] { user.Id } });
            Assert.False(crm.CheckPermissions(analysis, scope, user.ToEntityReference()).Passed);
            crm.AddSecurityRolesToPrinciple(user.ToEntityReference(), allowed.RoleId);
            Assert.True(crm.CheckPermissions(analysis, scope, user.ToEntityReference()).Passed);
        }

        [Fact]
        public async System.Threading.Tasks.Task ChecksDoNotChangeMockupState()
        {
            var analysis = await Analysis.Value;
            var user = crm.CreateUser(orgAdminService, crm.RootBusinessUnit, SecurityRoles.XrmMockupTestAccessTeam);
            var core = (Core)typeof(XrmMockupBase).GetField("Core", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(crm);
            var before = core.TakeJsonSnapshot();
            crm.CheckPermissions(analysis, PermissionSelector.Operation<BusinessUnit>(Operation.Create), user.ToEntityReference());
            crm.CheckPermissions(analysis, PermissionSelector.Plugin<AccountTagPlugin>(), SecurityRoles.XrmMockupTestAccessTeam);
            Assert.Equal(before, core.TakeJsonSnapshot());
        }

        [Fact]
        public async System.Threading.Tasks.Task NewlyRegisteredWorkflowMakesAnalysisIncompleteWithoutExecutingIt()
        {
            var analysis = await Analysis.Value;
            var scope = PermissionSelector.Operation<BusinessUnit>(Operation.Create);
            var core = (Core)typeof(XrmMockupBase).GetField("Core", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(crm);
            Assert.True(crm.CheckPermissions(analysis, scope, SecurityRoles.SystemAdministrator).Passed);
            core.AddWorkflow(new Entity("workflow", Guid.NewGuid())
            {
                ["name"] = "Static workflow boundary",
                ["category"] = new OptionSetValue(0),
                ["mode"] = new OptionSetValue(0),
                ["primaryentity"] = "businessunit",
                ["triggeroncreate"] = true
            });
            var result = crm.CheckPermissions(analysis, scope, SecurityRoles.SystemAdministrator);
            Assert.False(result.Passed);
            Assert.Contains(result.Manifest.EntryPoints.SelectMany(e => e.Unresolved), u => u.Reason.Contains("Static workflow boundary"));
            Assert.Throws<PermissionCheckException>(result.ThrowIfFailed);
        }

        [Fact]
        public void ImpersonatedUsersUseTheirOwnRolesAndUnknownIdentitiesFail()
        {
            var denied = crm.CloneSecurityRole("XrmMockup Test Access Team");
            denied.Privileges["businessunit"] = new Dictionary<AccessRights, RolePrivilege>();
            crm.AddSecurityRole(denied);
            var user = crm.CreateUser(orgAdminService, crm.RootBusinessUnit, denied.RoleId);
            var scope = PermissionSelector.Operation<BusinessUnit>(Operation.Create);
            var analysis = new IdentityAnalysis(user.Id);
            var result = crm.CheckPermissions(analysis, scope, SecurityRoles.SystemAdministrator);
            Assert.False(result.Passed); // The primary administrator cannot cover the impersonated user.
            var error = Assert.Throws<PermissionCheckException>(result.ThrowIfFailed);
            Assert.Contains(user.Id.ToString(), error.Message);
            Assert.Contains(denied.Name, error.Message);
            crm.AddSecurityRolesToPrinciple(user.ToEntityReference(), SecurityRoles.SystemAdministrator);
            Assert.True(crm.CheckPermissions(analysis, scope, denied.RoleId).Passed);
            var unknown = crm.CheckPermissions(new IdentityAnalysis(Guid.NewGuid()), scope, SecurityRoles.SystemAdministrator);
            Assert.False(unknown.Passed);
            Assert.True(unknown.Coverage.Summary.NotChecked > 0);
        }

        private sealed class IdentityAnalysis : PermissionAnalysis
        {
            private readonly Guid user;
            public IdentityAnalysis(Guid user) { this.user = user; }
            public override Manifest Analyze(PermissionScope scope, PermissionMetadata metadata)
            {
                var requirement = new Requirement("businessunit", XrmMockup.PermissionAnalyzer.Core.Model.Privilege.Create,
                    RunsAs.User(user.ToString()), RequirementBasis.Direct,
                    new[] { new RequirementSource(new SourceLocation("Impersonation.cs", 1, 1), new string[0]) });
                return new Manifest(new[] { new EntryPoint("operation:Create businessunit", EntryPointKind.Operation, "", null,
                    null, null, null, new[] { requirement }, new UnresolvedOperation[0], new Assumption[0]) }, new MethodManifest[0]);
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task ValidationAndSdkAttributeSelectors()
        {
            var analysis = await Analysis.Value;
            var scope = PermissionSelector.Operation<Account>(Operation.Update, a => a.Name, a => a.PrimaryContactId);
            Assert.Equal("account", scope.Table);
            Assert.Equal(new[] { "name", "primarycontactid" }, scope.Columns);
            Assert.Throws<ArgumentException>(() => PermissionSelector.Operation<Account>(Operation.Update, a => a.Name.Length));
            Assert.Throws<ArgumentException>(() => crm.CheckPermissions(analysis, scope, new Guid[0]));
            Assert.Throws<ArgumentException>(() => crm.CheckPermissions(analysis, scope, Guid.NewGuid()));
            Assert.Throws<ArgumentException>(() => crm.CheckPermissions(analysis, scope, new EntityReference("systemuser", Guid.NewGuid())));
            Assert.Throws<ArgumentException>(() => crm.CheckPermissions(analysis, scope, crm.RootBusinessUnit));
            var result = crm.CheckPermissions(analysis, PermissionSelector.Operation<BusinessUnit>("not_a_known_message"), SecurityRoles.XrmMockupTestAccessTeam);
            Assert.False(result.Passed);
            Assert.Throws<PermissionCheckException>(result.ThrowIfFailed);
        }
    }
}
#endif
