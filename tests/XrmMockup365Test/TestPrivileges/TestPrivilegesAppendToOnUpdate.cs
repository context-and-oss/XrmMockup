using System;
using Microsoft.Crm.Sdk.Messages;
using System.Collections.Generic;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestPrivilegesAppendToOnUpdate : UnitTestBase
    {
        SystemUser UserBURoot;
        SystemUser UserBULvl11;
        SystemUser UserBULvl12;
        SystemUser UserBULvl2;

        public TestPrivilegesAppendToOnUpdate(XrmMockupFixture fixture) : base(fixture)
        {
            crm.DisableRegisteredPlugins(true);
            var buLevel1 = orgAdminService.Create(new BusinessUnit() { ParentBusinessUnitId = crm.RootBusinessUnit, Name = "Business Level 1" });
            var buLevel2 = orgAdminService.Create(new BusinessUnit() { ParentBusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel1), Name = "Business Level 2" });

            UserBURoot = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userRoot@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, crm.RootBusinessUnit.Id) }) as SystemUser;
            UserBULvl11 = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userBULvl11@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel1) }) as SystemUser;
            UserBULvl12 = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userBULvl12@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel1) }) as SystemUser;
            UserBULvl2 = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userBULvl2@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel2) }) as SystemUser;

            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                            { AccessRights.WriteAccess, PrivilegeDepth.Global },
                            { AccessRights.AppendAccess, PrivilegeDepth.Global },
                        }
                    },
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                        }
                    }
                });
        }

        /// <summary>
        /// Test user append to account on basic level
        /// </summary>
        [Fact]
        public void TestAppendToOnUpdateUserLevel()
        {
            // add account and contact to database
            var accId = Guid.NewGuid();
            var contactId = Guid.NewGuid();
            AddAccountToDatabase(accId, UserBULvl11.ToEntityReference());
            AddContactToDatabase(contactId, UserBULvl11.ToEntityReference());

            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);

            // test append account without privilege
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
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
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendToAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try append account with basic privilege
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }

            // Clear PrimaryContactId
            orgGodService.Update(new Account(accId) { PrimaryContactId = null });

            // Add account append privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendToAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try append account with local privilege
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }

            // Clear PrimaryContactId
            orgGodService.Update(new Account(accId) { PrimaryContactId = null });

            // Add account append privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendToAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try append account with deep privilege
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }

            // Clear PrimaryContactId
            orgGodService.Update(new Account(accId) { PrimaryContactId = null });

            // Add account append privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendToAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try append account with global privilege
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }
        }

        /// <summary>
        /// Test user append to account on local level
        /// </summary>
        [Fact]
        public void TestAppendToOnUpdateBULevel()
        {
            // add account and contact to database
            var accId = Guid.NewGuid();
            var contactId = Guid.NewGuid();
            AddAccountToDatabase(accId, UserBULvl12.ToEntityReference());
            AddContactToDatabase(contactId, UserBULvl12.ToEntityReference());

            // test append account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
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
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendToAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try append account with basic privilege
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
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
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendToAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try append account with local privilege
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }

            // Clear PrimaryContactId
            orgGodService.Update(new Account(accId) { PrimaryContactId = null });

            // Add account append privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendToAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try append account with deep privilege
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }

            // Clear PrimaryContactId
            orgGodService.Update(new Account(accId) { PrimaryContactId = null });

            // Add account append privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendToAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try append account with global privilege
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }
        }

        /// <summary>
        /// Test user append to account on deep level
        /// </summary>
        [Fact]
        public void TestAppendToOnUpdateBUChildLevel()
        {
            // add account and contact to database
            var accId = Guid.NewGuid();
            var contactId = Guid.NewGuid();
            AddAccountToDatabase(accId, UserBULvl2.ToEntityReference());
            AddContactToDatabase(contactId, UserBULvl2.ToEntityReference());

            // test append account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
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
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendToAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try append account with basic privilege
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
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
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendToAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try append account with local privilege
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
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
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendToAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try append account with deep privilege
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }

            // Add account append privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendToAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try append account with global privilege
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }
        }

        /// <summary>
        /// Test user append to account on global level
        /// </summary>
        [Fact]
        public void TestAppendToOnUpdateGlobalLevel()
        {
            // add account and contact to database
            var accId = Guid.NewGuid();
            var contactId = Guid.NewGuid();
            AddAccountToDatabase(accId, UserBURoot.ToEntityReference());
            AddContactToDatabase(contactId, UserBURoot.ToEntityReference());

            // test append account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
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
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendToAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try append account with basic privilege
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
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
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendToAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try append account with local privilege
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
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
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendToAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try append account with deep privilege
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
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
                    { Contact.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.AppendToAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try append account with global privilege
            try
            {
                userOrg.Update(new Account(accId) { PrimaryContactId = new EntityReference(Contact.EntityLogicalName, contactId) });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.Equal(contactId, account.PrimaryContactId.Id);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to append");
            }
        }

        private void AddAccountToDatabase(Guid accId, EntityReference owner)
        {
            crm.PopulateWith(new Account(accId)
            {
                Name = "Account",
                OwnerId = owner,
            });
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
    }
}
