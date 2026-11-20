using System;
using Microsoft.Xrm.Sdk;
using DG.Tools.XrmMockup;
using Xunit;
using System.Collections.Generic;
using Microsoft.Crm.Sdk.Messages;
using System.Linq;

#if DATAVERSE_SERVICE_CLIENT
using Microsoft.PowerPlatform.Dataverse.Client;
#endif

namespace DG.XrmMockupTest
{
    public abstract class ServiceWrapper
    {
#if DATAVERSE_SERVICE_CLIENT
        public IOrganizationServiceAsync2 orgAdminUIService { get; protected set; }
        public IOrganizationServiceAsync2 orgAdminService { get; protected set; }
        public IOrganizationServiceAsync2 orgGodService { get; protected set; }

        public IOrganizationServiceAsync2 testUser1Service { get; protected set; }
        public IOrganizationServiceAsync2 testUser2Service { get; protected set; }
        public IOrganizationServiceAsync2 testUser3Service { get; protected set; }
        public IOrganizationServiceAsync2 testUser4Service { get; protected set; }
#else
        public IOrganizationService orgAdminUIService { get; protected set; }
        public IOrganizationService orgAdminService { get; protected set; }
        public IOrganizationService orgGodService { get; protected set; }

        public IOrganizationService testUser1Service { get; protected set; }
        public IOrganizationService testUser2Service { get; protected set; }
        public IOrganizationService testUser3Service { get; protected set; }
        public IOrganizationService testUser4Service { get; protected set; }
#endif

        public Entity testUser1 { get; protected set; }
        public Entity testUser2 { get; protected set; }
        public Entity testUser3 { get; protected set; }
        public Entity testUser4 { get; protected set; }
        public Entity testUser5 { get; protected set; }
    }

    public class UnitTestOrganizationServiceFactory : IOrganizationServiceFactory
    {
        public UnitTestOrganizationServiceFactory(ServiceWrapper services)
        {
            Services = services;
        }

        private ServiceWrapper Services { get; }

        public IOrganizationService CreateOrganizationService(Guid? userId)
        {
            if (userId == null || userId == Guid.Empty)
            {
                return Services.orgAdminService;
            }
            else if (userId == Services.testUser1.Id)
            {
                return Services.testUser1Service;
            }
            else if (userId == Services.testUser2.Id)
            {
                return Services.testUser2Service;
            }
            else if (userId == Services.testUser3.Id)
            {
                return Services.testUser3Service;
            }
            else if (userId == Services.testUser4.Id)
            {
                return Services.testUser4Service;
            }
            else
            {
                throw new ArgumentException($"Unknown userId: {userId}");
            }
        }
    }

    public class UnitTestBase : ServiceWrapper, IClassFixture<XrmMockupFixture>
    {
        private static DateTime _startTime { get; set; }

        protected Entity contactWriteAccessTeamTemplate;

        protected XrmMockup365 crm;

        public UnitTestBase(XrmMockupFixture fixture)
        {
            // Each test gets its own completely fresh instance
            crm = XrmMockup365.GetInstance(fixture.Settings);
            
            orgAdminUIService = crm.GetAdminService(new MockupServiceSettings(true, false, MockupServiceSettings.Role.UI));
            orgGodService = crm.GetAdminService(new MockupServiceSettings(false, true, MockupServiceSettings.Role.SDK));
            orgAdminService = crm.GetAdminService();

            //create an admin user to run our impersonating user plugins as
            var adminUser = new Entity("systemuser") { Id = Guid.Parse("3b961284-cd7a-4fa3-af7e-89802e88dd5c") };
            adminUser["businessunitid"] = crm.RootBusinessUnit;
            adminUser["internalemailaddress"] = "test@test.com";
            adminUser["islicensed"] = true;
            adminUser["firstname"] = "Admin";
            adminUser["lastname"] = "User";
            adminUser["fullname"] = "Admin User";

            adminUser = crm.CreateUser(orgAdminService, adminUser, SecurityRoles.SystemAdministrator);
            
            InitialiseAccessTeamConfiguration();
        }

        private void InitialiseAccessTeamConfiguration()
        {
            //create a new security role with basic level only on all contact privileges
            var accessTeamTestRole = crm.CloneSecurityRole("XrmMockup Test Access Team");
            accessTeamTestRole.Name = "AccessTeamTest";
            var contactPriv = accessTeamTestRole.Privileges["contact"];
            var newPriv = new Dictionary<AccessRights, DG.Tools.XrmMockup.RolePrivilege>();
            foreach (var priv in contactPriv)
            {
                var newP = priv.Value.Clone();
                newP.PrivilegeDepth = PrivilegeDepth.Basic;
                newPriv.Add(priv.Key, newP);
            }
            accessTeamTestRole.Privileges.Remove("contact");
            accessTeamTestRole.Privileges.Add("contact", newPriv);

            var accountPriv = accessTeamTestRole.Privileges["account"];
            newPriv = new Dictionary<AccessRights, DG.Tools.XrmMockup.RolePrivilege>();
            foreach (var priv in accountPriv)
            {
                var newP = priv.Value.Clone();
                newP.PrivilegeDepth = PrivilegeDepth.Basic;
                newPriv.Add(priv.Key, newP);
            }
            accessTeamTestRole.Privileges.Remove("account");
            accessTeamTestRole.Privileges.Add("account", newPriv);
            crm.AddSecurityRole(accessTeamTestRole);

            //create a new security role without share priv on contact
            var accessTeamTestRole2 = crm.CloneSecurityRole("XrmMockup Test Access Team");
            accessTeamTestRole2.Name = "AccessTeamTestNoShare";
            var contactPriv2 = accessTeamTestRole.Privileges["contact"];
            var newPriv2 = new Dictionary<AccessRights, DG.Tools.XrmMockup.RolePrivilege>();
            foreach (var priv in contactPriv.Where(x => x.Value.AccessRight != AccessRights.ShareAccess))
            {
                var newP = priv.Value.Clone();
                newP.PrivilegeDepth = PrivilegeDepth.Basic;
                newPriv2.Add(priv.Key, newP);
            }
            accessTeamTestRole2.Privileges.Remove("contact");
            accessTeamTestRole2.Privileges.Add("contact", newPriv2);
            crm.AddSecurityRole(accessTeamTestRole2);

            //create a new security role without write priv on contact
            var accessTeamTestRole3 = crm.CloneSecurityRole("XrmMockup Test Access Team");
            accessTeamTestRole3.Name = "AccessTeamTestNoWrite";
            var contactPriv3 = accessTeamTestRole.Privileges["contact"];
            var newPriv3 = new Dictionary<AccessRights, DG.Tools.XrmMockup.RolePrivilege>();
            foreach (var priv in contactPriv.Where(x => x.Value.AccessRight != AccessRights.WriteAccess))
            {
                var newP = priv.Value.Clone();
                newP.PrivilegeDepth = PrivilegeDepth.Basic;
                newPriv3.Add(priv.Key, newP);
            }
            accessTeamTestRole3.Privileges.Remove("contact");
            accessTeamTestRole3.Privileges.Add("contact", newPriv3);
            crm.AddSecurityRole(accessTeamTestRole3);

            //create some users with the new role
            var user = new Entity("systemuser");
            user["internalemailaddress"] = "camstestuser1@official.mod.uk";
            user["businessunitid"] = crm.RootBusinessUnit;
            user["islicensed"] = true;
            
            testUser1 = crm.CreateUser(orgAdminService, user, new Guid[] { crm.GetSecurityRole("AccessTeamTest").RoleId });
            testUser1Service = crm.CreateOrganizationService(testUser1.Id);

            var user2 = new Entity("systemuser");
            user2["internalemailaddress"] = "camstestuser2@official.mod.uk";
            user2["businessunitid"] = crm.RootBusinessUnit;
            user2["islicensed"] = true;
            testUser2 = crm.CreateUser(orgAdminService, user2, new Guid[] { crm.GetSecurityRole("AccessTeamTest").RoleId });
            testUser2Service = crm.CreateOrganizationService(testUser2.Id);

            var user3 = new Entity("systemuser");
            user3["internalemailaddress"] = "camstestuser3@official.mod.uk";
            user3["businessunitid"] = crm.RootBusinessUnit;
            user3["islicensed"] = true;
            testUser3 = crm.CreateUser(orgAdminService, user3, new Guid[] { crm.GetSecurityRole("AccessTeamTest").RoleId });
            testUser3Service = crm.CreateOrganizationService(testUser3.Id);

            var user4 = new Entity("systemuser");
            user4["internalemailaddress"] = "camstestuser4@official.mod.uk";
            user4["businessunitid"] = crm.RootBusinessUnit;
            user4["islicensed"] = true;
            testUser4 = crm.CreateUser(orgAdminService, user4, new Guid[] { crm.GetSecurityRole("AccessTeamTestNoShare").RoleId });
            testUser4Service = crm.CreateOrganizationService(testUser4.Id);

            var user5 = new Entity("systemuser");
            user5["internalemailaddress"] = "camstestuser4@official.mod.uk";
            user5["businessunitid"] = crm.RootBusinessUnit;
            user5["islicensed"] = true;
            testUser5 = crm.CreateUser(orgAdminService, user5, new Guid[] { crm.GetSecurityRole("AccessTeamTestNoWrite").RoleId });

            //create some access team templates
            CreateAccessTeamTemplate("TestWriteContact", 2, AccessRights.WriteAccess);
            CreateAccessTeamTemplate("TestReadContact", 2, AccessRights.ReadAccess);
            CreateAccessTeamTemplate("TestDeleteContact", 2, AccessRights.DeleteAccess);
            CreateAccessTeamTemplate("TestAppendContact", 2, AccessRights.AppendAccess);
            CreateAccessTeamTemplate("TestAssignContact", 2, AccessRights.AssignAccess);
            CreateAccessTeamTemplate("TestShareContact", 2, AccessRights.ShareAccess);
            CreateAccessTeamTemplate("TestAppendToAccount", 1, AccessRights.AppendToAccess);
            CreateAccessTeamTemplate("TestMultipleContact", 2, AccessRights.WriteAccess, AccessRights.ReadAccess, AccessRights.DeleteAccess);
        }

        private void CreateAccessTeamTemplate(string name, int objectTypeCode, params AccessRights[] access)
        {
            var contactWriteAccessTeamTemplate = new Entity("teamtemplate");
            contactWriteAccessTeamTemplate["teamtemplatename"] = name;
            contactWriteAccessTeamTemplate["objecttypecode"] = objectTypeCode;
            int mask = 0;
            //"OR" the access rights together to get the mask
            foreach (var a in access)
            {
                mask |= (int)a;
            }
            contactWriteAccessTeamTemplate["defaultaccessrightsmask"] = mask;
            contactWriteAccessTeamTemplate.Id = orgAdminService.Create(contactWriteAccessTeamTemplate);
        }

        protected TEntity Create<TEntity>(TEntity entity) where TEntity : Entity
        {
            return Create(orgAdminService, entity);
        }

        protected static TEntity Create<TEntity>(IOrganizationService service, TEntity entity) where TEntity : Entity
        {
            var id = service.Create(entity);
            return service.Retrieve(entity.LogicalName, id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true)).ToEntity<TEntity>();
        }

        public virtual void Dispose()
        {
            // No need to reset environment since each test has its own instance
            // The instance will be garbage collected automatically
        }
    }
}