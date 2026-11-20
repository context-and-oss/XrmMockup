using System;
using Microsoft.Crm.Sdk.Messages;
using System.Collections.Generic;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestPrivilegesCreate : UnitTestBase
    {
        SystemUser UserBURoot;
        SystemUser UserBULvl11;
        SystemUser UserBULvl12;
        SystemUser UserBULvl2;

        public TestPrivilegesCreate(XrmMockupFixture fixture) : base(fixture)
        {
            crm.DisableRegisteredPlugins(true);
            var buLevel1 = orgAdminService.Create(new BusinessUnit() { ParentBusinessUnitId = crm.RootBusinessUnit, Name = "Business Level 1" });
            var buLevel2 = orgAdminService.Create(new BusinessUnit() { ParentBusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel1), Name = "Business Level 2" });

            UserBURoot = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userRoot@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, crm.RootBusinessUnit.Id) }) as SystemUser;
            UserBULvl11 = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userBULvl11@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel1) }) as SystemUser;
            UserBULvl12 = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userBULvl12@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel1) }) as SystemUser;
            UserBULvl2 = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userBULvl2@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel2) }) as SystemUser;

            // Add account append privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            AddGlobalCreateAccessOnAccount(UserBURoot.ToEntityReference());
            AddGlobalCreateAccessOnAccount(UserBULvl12.ToEntityReference());
            AddGlobalCreateAccessOnAccount(UserBULvl2.ToEntityReference());
        }

        /// <summary>
        /// Test create account with basic level privilege
        /// </summary>
        [Fact]
        public void TestCreateUserLevelPrivilege()
        {
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);

            // test create account without privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account non",
                });
                throw new XunitException("User should not be able to create");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to create");
            }
            catch (Exception) { }

            // Add account create privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.CreateAccess, PrivilegeDepth.Basic },
                        }
                    },
                });

            // try create account with basic privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account basic",
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(UserBULvl11.Id, account.OwnerId.Id);
                Assert.Equal("Account basic", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to create");
            }

            // try create account with local privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account local",
                    OwnerId = UserBULvl12.ToEntityReference(),
                });
                throw new XunitException("User should not be able to create");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to create");
            }
            catch (Exception) { }

            // try create account with deep privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account deep",
                    OwnerId = UserBULvl2.ToEntityReference(),
                });
                throw new XunitException("User should not be able to create");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to create");
            }
            catch (Exception) { }

            // try create account with global privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account global",
                    OwnerId = UserBURoot.ToEntityReference(),
                });
                throw new XunitException("User should not be able to create");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to create");
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Test create account with local level privilege
        /// </summary>
        [Fact]
        public void TestCreateBULevelPrivilege()
        {
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);

            // test create account without privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account non",
                });
                throw new XunitException("User should not be able to create");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to create");
            }
            catch (Exception) { }

            // Add account create privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.CreateAccess, PrivilegeDepth.Local },
                        }
                    },
                });

            // try create account with basic privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account basic",
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(UserBULvl11.Id, account.OwnerId.Id);
                Assert.Equal("Account basic", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to create");
            }

            // try create account with local privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account local",
                    OwnerId = UserBULvl12.ToEntityReference(),
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(UserBULvl12.Id, account.OwnerId.Id);
                Assert.Equal("Account local", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to create");
            }

            // try create account with deep privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account deep",
                    OwnerId = UserBULvl2.ToEntityReference(),
                });
                throw new XunitException("User should not be able to create");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to create");
            }
            catch (Exception) { }

            // try create account with global privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account global",
                    OwnerId = UserBURoot.ToEntityReference(),
                });
                throw new XunitException("User should not be able to create");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to create");
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Test create account with deep level privilege
        /// </summary>
        [Fact]
        public void TestCreateDeepLevelPrivilege()
        {
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);

            // test create account without privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account non",
                });
                throw new XunitException("User should not be able to create");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to create");
            }
            catch (Exception) { }

            // Add account create privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.CreateAccess, PrivilegeDepth.Deep },
                        }
                    },
                });

            // try create account with basic privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account basic",
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(UserBULvl11.Id, account.OwnerId.Id);
                Assert.Equal("Account basic", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to create");
            }

            // try create account with local privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account local",
                    OwnerId = UserBULvl12.ToEntityReference(),
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(UserBULvl12.Id, account.OwnerId.Id);
                Assert.Equal("Account local", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to create");
            }

            // try create account with deep privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account deep",
                    OwnerId = UserBULvl2.ToEntityReference(),
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(UserBULvl2.Id, account.OwnerId.Id);
                Assert.Equal("Account deep", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to create");
            }

            // try create account with global privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account global",
                    OwnerId = UserBURoot.ToEntityReference(),
                });
                throw new XunitException("User should not be able to create");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to create");
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Test create account with global level privilege
        /// </summary>
        [Fact]
        public void TestCreateGlobalLevelPrivilege()
        {
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);

            // test create account without privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account non",
                });
                throw new XunitException("User should not be able to create");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to create");
            }
            catch (Exception) { }

            // Add account create privilege with global level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.CreateAccess, PrivilegeDepth.Global },
                        }
                    },
                });

            // try create account with basic privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account basic",
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(UserBULvl11.Id, account.OwnerId.Id);
                Assert.Equal("Account basic", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to create");
            }

            // try create account with local privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account local",
                    OwnerId = UserBULvl12.ToEntityReference(),
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(UserBULvl12.Id, account.OwnerId.Id);
                Assert.Equal("Account local", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to create");
            }

            // try create account with deep privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account deep",
                    OwnerId = UserBULvl2.ToEntityReference(),
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(UserBULvl2.Id, account.OwnerId.Id);
                Assert.Equal("Account deep", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to create");
            }

            // try create account with global privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account global",
                    OwnerId = UserBURoot.ToEntityReference(),
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(UserBURoot.Id, account.OwnerId.Id);
                Assert.Equal("Account global", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to create");
            }
        }

        private void AddGlobalCreateAccessOnAccount(EntityReference user)
        {
            crm.AddPrivileges(
                user,
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.CreateAccess, PrivilegeDepth.Global },
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                        }
                    }
                });
        }
    }
}
