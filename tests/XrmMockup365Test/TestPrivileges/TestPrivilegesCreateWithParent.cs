using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Xunit;
using RolePrivilege = DG.Tools.XrmMockup.RolePrivilege;

namespace DG.XrmMockupTest
{
    public class TestPrivilegesCreateWithParent : UnitTestBase
    {
        public TestPrivilegesCreateWithParent(XrmMockupFixture fixture) : base(fixture)
        {
            crm.DisableRegisteredPlugins(true);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ParentCreatePrivilegeDoesNotGrantChildCreate(bool throughTeam)
        {
            var role = CreateRole();
            EntityReference owner;
            var user = CreateCaller(role, throughTeam, out owner);
            var service = crm.CreateOrganizationService(user.Id);
            var parent = new Account { Name = "Owned parent", OwnerId = owner };
            parent.Id = service.Create(parent);

            // Account -> Contact has cascading Reparent in the checked-in metadata.
            var child = new Contact { LastName = "Child", ParentCustomerId = parent.ToEntityReference() };
            var error = Assert.Throws<FaultException>(() => service.Create(child));
            Assert.Contains("does not have Create access", error.Message);
            Assert.Throws<FaultException>(() => service.Execute(new CreateRequest { Target = child }));
            using (var context = new Xrm(orgAdminService))
                Assert.Empty(context.ContactSet.Where(c => c.ParentCustomerId != null && c.ParentCustomerId.Id == parent.Id).ToList());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ExplicitChildCreatePrivilegeStillAllowsCreate(bool throughTeam)
        {
            var role = CreateRole(PrivilegeDepth.Global);
            EntityReference owner;
            var user = CreateCaller(role, throughTeam, out owner);
            var service = crm.CreateOrganizationService(user.Id);
            var parent = new Account { Name = "Owned parent", OwnerId = owner };
            parent.Id = service.Create(parent);

            var childId = service.Create(new Contact { LastName = "Child", ParentCustomerId = parent.ToEntityReference() });
            var child = orgAdminService.Retrieve(Contact.EntityLogicalName, childId, new ColumnSet("parentcustomerid", "ownerid")).ToEntity<Contact>();
            Assert.Equal(parent.Id, child.ParentCustomerId.Id);
            Assert.Equal(user.Id, child.OwnerId.Id);
        }

        [Fact]
        public void ParentOwnershipDoesNotExpandChildCreateDepth()
        {
            var role = CreateRole(PrivilegeDepth.Basic);
            EntityReference owner;
            var user = CreateCaller(role, false, out owner);
            var service = crm.CreateOrganizationService(user.Id);
            var parent = new Account { Name = "Owned parent" };
            parent.Id = service.Create(parent);
            var otherUser = crm.CreateUser(orgAdminService, crm.RootBusinessUnit, role.RoleId);

            var error = Assert.Throws<FaultException>(() => service.Create(new Contact
            {
                LastName = "Other owner's child", ParentCustomerId = parent.ToEntityReference(), OwnerId = otherUser.ToEntityReference()
            }));
            Assert.Contains("does not have Create access", error.Message);

            // Basic Create still permits a child owned by the caller.
            Assert.NotEqual(Guid.Empty, service.Create(new Contact { LastName = "Owned child", ParentCustomerId = parent.ToEntityReference() }));
        }

        private SecurityRole CreateRole(PrivilegeDepth? childCreate = null)
        {
            var role = crm.CloneSecurityRole("XrmMockup Test Access Team");
            role.Privileges.Clear();
            role.Privileges[Account.EntityLogicalName] = new Dictionary<AccessRights, RolePrivilege>
            {
                { AccessRights.CreateAccess, new RolePrivilege { AccessRight = AccessRights.CreateAccess, PrivilegeDepth = PrivilegeDepth.Global } },
                { AccessRights.ReadAccess, new RolePrivilege { AccessRight = AccessRights.ReadAccess, PrivilegeDepth = PrivilegeDepth.Global } },
                { AccessRights.AppendToAccess, new RolePrivilege { AccessRight = AccessRights.AppendToAccess, PrivilegeDepth = PrivilegeDepth.Global } }
            };
            role.Privileges[Contact.EntityLogicalName] = new Dictionary<AccessRights, RolePrivilege>
            {
                { AccessRights.ReadAccess, new RolePrivilege { AccessRight = AccessRights.ReadAccess, PrivilegeDepth = PrivilegeDepth.Global } },
                { AccessRights.AppendAccess, new RolePrivilege { AccessRight = AccessRights.AppendAccess, PrivilegeDepth = PrivilegeDepth.Global } }
            };
            if (childCreate.HasValue)
                role.Privileges[Contact.EntityLogicalName][AccessRights.CreateAccess] = new RolePrivilege { AccessRight = AccessRights.CreateAccess, PrivilegeDepth = childCreate.Value };
            crm.AddSecurityRole(role);
            return role;
        }

        private SystemUser CreateCaller(SecurityRole role, bool throughTeam, out EntityReference owner)
        {
            var user = crm.CreateUser(orgAdminService, crm.RootBusinessUnit, throughTeam ? new Guid[0] : new[] { role.RoleId }).ToEntity<SystemUser>();
            owner = user.ToEntityReference();
            if (throughTeam)
            {
                var team = crm.CreateTeam(orgAdminService, new Team { Name = "Parent owners", BusinessUnitId = crm.RootBusinessUnit }, role.RoleId);
                orgAdminService.Execute(new AddMembersTeamRequest { TeamId = team.Id, MemberIds = new[] { user.Id } });
                owner = team.ToEntityReference();
            }
            return user;
        }
    }
}
