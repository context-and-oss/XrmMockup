using System;
using System.ServiceModel;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace DG.XrmMockupTest
{
    public class TestValidateUnchangedLookups : UnitTestBase
    {
        public TestValidateUnchangedLookups(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestAppendTo()
        {
            var acc = new Account() { Name = "testaccount" };
            acc.Id = orgAdminService.Create(acc);
            var con = new Contact()
            {
                FirstName = "assign",
                ParentCustomerId = acc.ToEntityReference()
            };
            con.Id = orgAdminService.Create(con);

            // Create user with write and append, but not append to
            var minUser = crm.CreateUser(orgAdminService, crm.RootBusinessUnit);
            crm.AddPrivileges(
                minUser.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Global },
                            { AccessRights.AppendAccess, PrivilegeDepth.Global },
                        }
                    }
                });
            var userService = crm.CreateOrganizationService(minUser.Id);

            userService.Update(new Contact(con.Id)
            {
                FirstName = con.FirstName,
                ParentCustomerId = con.ParentCustomerId
            });
        }

        [Fact]
        public void TestCreateWithSystemUserLookupAppendTo()
        {
            // systemuser is a business-owned entity and has no ownerid. Creating a record with a
            // non-owner lookup to systemuser must succeed when the caller holds AppendTo on
            // systemuser, even when that privilege is granted below organization (global) depth.
            // The privilege is granted at Business Unit (Local) depth and the referenced user is
            // in the caller's business unit, matching how Dataverse scopes business-owned tables.
            var referencedUser = crm.CreateUser(orgAdminService, crm.RootBusinessUnit).ToEntity<SystemUser>();

            var minUser = crm.CreateUser(orgAdminService, crm.RootBusinessUnit);
            crm.AddPrivileges(
                minUser.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.CreateAccess, PrivilegeDepth.Global },
                            { AccessRights.AppendAccess, PrivilegeDepth.Global },
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                        }
                    },
                    { SystemUser.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Local },
                            { AccessRights.AppendToAccess, PrivilegeDepth.Local },
                        }
                    },
                });
            var userService = crm.CreateOrganizationService(minUser.Id);

            var con = new Contact { FirstName = "withref" };
            con["preferredsystemuserid"] = referencedUser.ToEntityReference();
            con.Id = userService.Create(con);

            Assert.NotEqual(Guid.Empty, con.Id);
        }

        [Fact]
        public void TestCreateWithSystemUserLookupAppendToViaTeamRole()
        {
            // Same scenario as TestCreateWithSystemUserLookupAppendTo, but the privileges are
            // granted through the caller's team rather than directly. systemuser is business-owned
            // and has no ownerid, so the team-membership check must still enumerate the caller's
            // teams instead of short-circuiting.
            var referencedUser = crm.CreateUser(orgAdminService, crm.RootBusinessUnit).ToEntity<SystemUser>();

            var minUser = crm.CreateUser(orgAdminService, crm.RootBusinessUnit);
            var team = crm.CreateTeam(orgAdminService, crm.RootBusinessUnit);
            crm.AddUsersToTeam(team.ToEntityReference(), minUser.ToEntityReference());
            crm.AddPrivileges(
                team.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.CreateAccess, PrivilegeDepth.Global },
                            { AccessRights.AppendAccess, PrivilegeDepth.Global },
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                        }
                    },
                    { SystemUser.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Local },
                            { AccessRights.AppendToAccess, PrivilegeDepth.Local },
                        }
                    },
                });
            var userService = crm.CreateOrganizationService(minUser.Id);

            var con = new Contact { FirstName = "withref-team" };
            con["preferredsystemuserid"] = referencedUser.ToEntityReference();
            con.Id = userService.Create(con);

            Assert.NotEqual(Guid.Empty, con.Id);
        }

        [Fact]
        public void TestCreateWithSystemUserLookupAppendToDeniedCrossBusinessUnit()
        {
            // With AppendTo on systemuser granted only at Business Unit (Local) depth, referencing
            // a user in a different business unit must be denied - matching Dataverse's scoping of
            // business-owned tables by owning business unit.
            var otherBu = new BusinessUnit { Name = "other bu", ParentBusinessUnitId = crm.RootBusinessUnit };
            otherBu.Id = orgAdminService.Create(otherBu);
            var referencedUser = crm.CreateUser(orgAdminService, otherBu.ToEntityReference()).ToEntity<SystemUser>();

            var minUser = crm.CreateUser(orgAdminService, crm.RootBusinessUnit);
            crm.AddPrivileges(
                minUser.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.CreateAccess, PrivilegeDepth.Global },
                            { AccessRights.AppendAccess, PrivilegeDepth.Global },
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                        }
                    },
                    { SystemUser.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Local },
                            { AccessRights.AppendToAccess, PrivilegeDepth.Local },
                        }
                    },
                });
            var userService = crm.CreateOrganizationService(minUser.Id);

            var con = new Contact { FirstName = "cross-bu" };
            con["preferredsystemuserid"] = referencedUser.ToEntityReference();

            Assert.Throws<FaultException>(() => userService.Create(con));
        }

        [Fact]
        public void TestAssign()
        {
            var acc = new Account() { Name = "testaccount" };
            acc.Id = orgAdminService.Create(acc);
            var con = new Contact()
            {
                FirstName = "assign"
            };
            con.Id = orgAdminService.Create(con);

            var contact = orgAdminService.Retrieve(con.LogicalName, con.Id, new ColumnSet(true)).ToEntity<Contact>();

            // Create user with write and append, but not assign
            var minUser = crm.CreateUser(orgAdminService, crm.RootBusinessUnit);
            crm.AddPrivileges(
                minUser.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Global },
                            { AccessRights.AppendAccess, PrivilegeDepth.Global },
                        }
                    }
                });
            var userService = crm.CreateOrganizationService(minUser.Id);

            userService.Update(new Contact(con.Id)
            {
                FirstName = contact.FirstName,
                OwnerId = contact.OwnerId
            });
        }
    }
}
