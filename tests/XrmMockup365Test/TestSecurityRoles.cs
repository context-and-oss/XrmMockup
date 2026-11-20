using System;
using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestSecurityRoles : UnitTestBase
    {
        public TestSecurityRoles(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestQueryOnLinkedRoleTemplate()
        {
            // All MS Dynamics CRM instances share the same System Admin role GUID.
            // Hence, we can hardode it as this will not represent a security issue
            Guid adminId = new Guid("627090FF-40A3-4053-8790-584EDC5BE201");

            var businessunit = new BusinessUnit();
            businessunit["name"] = "business unit name 1";
            businessunit.Id = orgAdminUIService.Create(businessunit);

            var user = new SystemUser();
            user["businessunitid"] = businessunit.ToEntityReference();
            var adminuser = crm.CreateUser(orgAdminService, user, SecurityRoles.SystemAdministrator);

            var q = new QueryExpression("role");
            q.Criteria.AddCondition("roletemplateid", ConditionOperator.Equal, adminId);
            var link = q.AddLink("systemuserroles", "roleid", "roleid");
            link.LinkCriteria.AddCondition("systemuserid", ConditionOperator.Equal, adminuser.Id);
            Assert.True( orgAdminService.RetrieveMultiple(q).Entities.Count > 0 );
        }

        [Fact]
        public void TestCreateSecurity()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var orgUser = new SystemUser();
                orgUser.Id = orgAdminUIService.Create(orgUser);

                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 1";
                businessunit.Id = orgAdminUIService.Create(businessunit);

                var scheduler = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.XrmMockupTestUser);
                var schedulerService = crm.CreateOrganizationService(scheduler.Id);

                try
                {
                    // XrmMockupTestUser has no account-create privilege (originally tested with Lead, now removed).
                    schedulerService.Create(new Account());
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }

                schedulerService.Create(new Contact());

                try
                {
                    var contact = new Contact
                    {
                        OwnerId = orgUser.ToEntityReference()
                    };
                    schedulerService.Create(contact);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }
            }
        }

        [Fact]
        public void TestAssignSecurityUser()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 2";
                businessunit.Id = orgAdminUIService.Create(businessunit);
                var orgUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator);
                var service = crm.CreateOrganizationService(orgUser.Id);
                var contact = new Contact();
                contact.Id = service.Create(contact);

                var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.XrmMockupTestUser);
                service = crm.CreateOrganizationService(user.Id);

                var req = new AssignRequest();
                try
                {
                    req.Assignee = user.ToEntityReference();
                    req.Target = contact.ToEntityReference();
                    service.Execute(req);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }
                var usersContact = new Contact();
                usersContact.Id = service.Create(usersContact);
                req.Assignee = orgUser.ToEntityReference();
                req.Target = usersContact.ToEntityReference();
                service.Execute(req);
            }
        }

        [Fact]
        public void TestAssignSecurityTeam()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 3";
                businessunit.Id = orgAdminUIService.Create(businessunit);
                var orgUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator);
                var service = crm.CreateOrganizationService(orgUser.Id);
                var contact = new Contact();
                contact.Id = service.Create(contact);

                var team = crm.CreateTeam(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.XrmMockupTestNoContactAccess);

                var req = new AssignRequest();
                try
                {
                    req.Assignee = team.ToEntityReference();
                    req.Target = contact.ToEntityReference();
                    service.Execute(req);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }
                var teamAdmin = crm.CreateTeam(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator);
                var adminContact = new Contact();
                adminContact.Id = service.Create(adminContact);
                req.Assignee = teamAdmin.ToEntityReference();
                req.Target = adminContact.ToEntityReference();
                service.Execute(req);
            }
        }

        [Fact]
        public void TestUpdateSecurity()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 4";
                businessunit.Id = orgAdminUIService.Create(businessunit);
                var orgUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator);
                var service = crm.CreateOrganizationService(orgUser.Id);
                var bus = new ctx_parent();
                bus.Id = service.Create(bus);

                var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.XrmMockupTestUser);
                service = crm.CreateOrganizationService(user.Id);

                try
                {
                    bus.ctx_Name = "new sadas";
                    service.Update(bus);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }

                var usersBus = new ctx_parent();
                usersBus.Id = service.Create(usersBus);
                usersBus.ctx_Name = "sadsa";
                service.Update(usersBus);
            }
        }

        [Fact]
        public void TestDeleteSecurity()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 5";
                businessunit.Id = orgAdminUIService.Create(businessunit);
                var orgUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator);
                var service = crm.CreateOrganizationService(orgUser.Id);
                var bus = new ctx_parent();
                bus.Id = service.Create(bus);

                var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.XrmMockupTestUser);
                service = crm.CreateOrganizationService(user.Id);

                try
                {
                    service.Delete(bus.LogicalName, bus.Id);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }

                var usersBus = new ctx_parent();
                usersBus.Id = service.Create(usersBus);
                service.Delete(usersBus.LogicalName, usersBus.Id);
            }
        }

        [Fact]
        public void TestParentChangeCascading()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 6";
                businessunit.Id = orgAdminUIService.Create(businessunit);
                var parentUser = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.XrmMockupTestUser);
                var service = crm.CreateOrganizationService(parentUser.Id);
                var bus = new ctx_parent();
                bus.Id = service.Create(bus);

                var parentChild = new ctx_child
                {
                    ctx_ParentId = bus.ToEntityReference()
                };
                parentChild.Id = service.Create(parentChild);

                var otherChild = new ctx_child();
                otherChild.Id = orgAdminUIService.Create(otherChild);

                try
                {
                    service.Retrieve(otherChild.LogicalName, otherChild.Id, new ColumnSet(true));
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }

                otherChild.ctx_ParentId = bus.ToEntityReference();
                orgAdminUIService.Update(otherChild);

                service.Retrieve(otherChild.LogicalName, otherChild.Id, new ColumnSet(true));
            }
        }

        [Fact]
        public void TestCreateUserWithNonExistentSecurityRoleThrowsMockupException()
        {
            var nonExistentRoleId = Guid.NewGuid();

            var ex = Assert.Throws<MockupException>(() =>
                crm.CreateUser(orgAdminUIService, crm.RootBusinessUnit, nonExistentRoleId));

            Assert.Contains(nonExistentRoleId.ToString(), ex.Message);
            Assert.Contains("was not found", ex.Message);
        }
    }

}
