using System;
using Microsoft.Crm.Sdk.Messages;
using System.Collections.Generic;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestPrivilegesAppendOnCreate : UnitTestBase
    {
        SystemUser UserBURoot;
        SystemUser UserBULvl11;
        SystemUser UserBULvl12;
        SystemUser UserBULvl2;

        public TestPrivilegesAppendOnCreate(XrmMockupFixture fixture) : base(fixture)
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
                            { AccessRights.CreateAccess, PrivilegeDepth.Global },
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                        }
                    },
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendToAccess, PrivilegeDepth.Global },
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            AddGlobalCreateAccessOnAccount(UserBURoot.ToEntityReference());
            AddGlobalCreateAccessOnAccount(UserBULvl12.ToEntityReference());
            AddGlobalCreateAccessOnAccount(UserBULvl2.ToEntityReference());
        }

        /// <summary>
        /// Test user append on basic level
        /// </summary>
        [Fact]
        public void TestAppendOnCreateUserLevel()
        {
            // contact to database
            var contactId = Guid.NewGuid();
            AddContactToDatabase(contactId, UserBULvl11.ToEntityReference());

            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);

            // test append account without privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account non",
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                throw new XunitException("User should not be able to append");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to append");
            }
            catch (Exception) { }

            // Add account append privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendAccess, PrivilegeDepth.Basic },
                        }
                    },
                });

            // try append account with basic privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account basic",
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
                Assert.Equal("Account basic", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }

            // Add account append privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendAccess, PrivilegeDepth.Local },
                        }
                    },
                });

            // try append account with local privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account local",
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
                Assert.Equal("Account local", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }

            // Add account append privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendAccess, PrivilegeDepth.Deep },
                        }
                    },
                });

            // try append account with deep privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account deep",
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
                Assert.Equal("Account deep", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }

            // Add account append privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendAccess, PrivilegeDepth.Global },
                        }
                    },
                });

            // try append account with global privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account global",
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
                Assert.Equal("Account global", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }
        }

        /// <summary>
        /// Test user append on local level
        /// </summary>
        [Fact]
        public void TestAppendOnCreateBULevel()
        {
            // contact to database
            var contactId = Guid.NewGuid();
            AddContactToDatabase(contactId, UserBULvl12.ToEntityReference());

            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);

            // test append account without privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account non",
                    OwnerId = UserBULvl12.ToEntityReference(),
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                throw new XunitException("User should not be able to append");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to append");
            }
            catch (Exception) { }

            // Add account append privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendAccess, PrivilegeDepth.Basic },
                        }
                    },
                });

            // try append account with basic privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account basic",
                    OwnerId = UserBULvl12.ToEntityReference(),
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                throw new XunitException("User should not be able to append");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to append");
            }
            catch (Exception) { }

            // Add account append privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendAccess, PrivilegeDepth.Local },
                        }
                    },
                });

            // try append account with local privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account local",
                    OwnerId = UserBULvl12.ToEntityReference(),
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
                Assert.Equal("Account local", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }

            // Add account append privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendAccess, PrivilegeDepth.Deep },
                        }
                    },
                });

            // try append account with deep privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account deep",
                    OwnerId = UserBULvl12.ToEntityReference(),
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
                Assert.Equal("Account deep", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }

            // Add account append privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendAccess, PrivilegeDepth.Global },
                        }
                    },
                });

            // try append account with global privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account global",
                    OwnerId = UserBULvl12.ToEntityReference(),
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
                Assert.Equal("Account global", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }
        }

        /// <summary>
        /// Test user append on deep level
        /// </summary>
        [Fact]
        public void TestAppendOnCreateBUChildlevel()
        {
            // contact to database
            var contactId = Guid.NewGuid();
            AddContactToDatabase(contactId, UserBULvl12.ToEntityReference());

            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);

            // test append account without privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account non",
                    OwnerId = UserBULvl2.ToEntityReference(),
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                throw new XunitException("User should not be able to append");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to append");
            }
            catch (Exception) { }

            // Add account append privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendAccess, PrivilegeDepth.Basic },
                        }
                    },
                });

            // try append account with basic privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account basic",
                    OwnerId = UserBULvl2.ToEntityReference(),
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                throw new XunitException("User should not be able to append");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to append");
            }
            catch (Exception) { }

            // Add account append privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendAccess, PrivilegeDepth.Local },
                        }
                    },
                });

            // try append account with local privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account local",
                    OwnerId = UserBULvl2.ToEntityReference(),
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                throw new XunitException("User should not be able to append");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to append");
            }
            catch (Exception) { }

            // Add account append privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendAccess, PrivilegeDepth.Deep },
                        }
                    },
                });

            // try append account with deep privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account deep",
                    OwnerId = UserBULvl2.ToEntityReference(),
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
                Assert.Equal("Account deep", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }

            // Add account append privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendAccess, PrivilegeDepth.Global },
                        }
                    },
                });

            // try append account with global privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account global",
                    OwnerId = UserBULvl2.ToEntityReference(),
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
                Assert.Equal("Account global", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }
        }

        /// <summary>
        /// Test user append on global level
        /// </summary>
        [Fact]
        public void TestAppendOnCreateGlobalLevel()
        {
            // contact to database
            var contactId = Guid.NewGuid();
            AddContactToDatabase(contactId, UserBURoot.ToEntityReference());

            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);

            // test append account without privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account non",
                    OwnerId = UserBURoot.ToEntityReference(),
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                throw new XunitException("User should not be able to append");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to append");
            }
            catch (Exception) { }

            // Add account append privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendAccess, PrivilegeDepth.Basic },
                        }
                    },
                });

            // try append account with basic privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account basic",
                    OwnerId = UserBURoot.ToEntityReference(),
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                throw new XunitException("User should not be able to append");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to append");
            }
            catch (Exception) { }

            // Add account append privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendAccess, PrivilegeDepth.Local },
                        }
                    },
                });

            // try append account with local privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account local",
                    OwnerId = UserBURoot.ToEntityReference(),
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                throw new XunitException("User should not be able to append");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to append");
            }
            catch (Exception) { }

            // Add account append privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendAccess, PrivilegeDepth.Deep },
                        }
                    },
                });

            // try append account with deep privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account deep",
                    OwnerId = UserBURoot.ToEntityReference(),
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                var account = Account.Retrieve(orgAdminService, accId);
                throw new XunitException("User should not be able to append");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to append");
            }
            catch (Exception) { }

            // Add account append privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendAccess, PrivilegeDepth.Global },
                        }
                    },
                });

            // try append account with global privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account global",
                    OwnerId = UserBURoot.ToEntityReference(),
                    PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId)
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
                Assert.Equal("Account global", account.Name);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }
        }

        private void AddContactToDatabase(Guid contactId, EntityReference owner)
        {
            crm.PopulateWith(new Contact(contactId)
            {
                FirstName = "Contact",
                LastName = "test",
                OwnerId = owner,
            });
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
