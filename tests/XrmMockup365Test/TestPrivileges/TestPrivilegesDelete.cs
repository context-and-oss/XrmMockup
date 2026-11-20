using System;
using Microsoft.Crm.Sdk.Messages;
using System.Collections.Generic;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestPrivilegesDelete : UnitTestBase
    {
        SystemUser UserBURoot;
        SystemUser UserBULvl11;
        SystemUser UserBULvl12;
        SystemUser UserBULvl2;

        public TestPrivilegesDelete(XrmMockupFixture fixture) : base(fixture)
        {
            crm.DisableRegisteredPlugins(true);
            var buLevel1 = orgAdminService.Create(new BusinessUnit() { ParentBusinessUnitId = crm.RootBusinessUnit, Name = "Business Level 1" });
            var buLevel2 = orgAdminService.Create(new BusinessUnit() { ParentBusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel1), Name = "Business Level 2" });

            UserBURoot = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userRoot@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, crm.RootBusinessUnit.Id) }) as SystemUser;
            UserBULvl11 = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userBULvl11@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel1) }) as SystemUser;
            UserBULvl12 = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userBULvl12@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel1) }) as SystemUser;
            UserBULvl2 = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userBULvl2@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel2) }) as SystemUser;
        }

        /// <summary>
        /// Test user delete account on basic level
        /// </summary>
        [Fact]
        public void TestDeleteUserLevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            AddAccountToDatabase(accId, UserBULvl11.ToEntityReference());

            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);

            // test delete account without privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                throw new XunitException("User should not be able to delete");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try delete account with basic privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to delete");
            }

            // Add account to database
            AddAccountToDatabase(accId, UserBULvl11.ToEntityReference());

            // Add account delete privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try delete account with local privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to delete");
            }

            // Add account to database
            AddAccountToDatabase(accId, UserBULvl11.ToEntityReference());

            // Add account delete privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try delete account with deep privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to delete");
            }

            // Add account to database
            AddAccountToDatabase(accId, UserBULvl11.ToEntityReference());

            // Add account delete privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try delete account with global privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to delete");
            }
        }

        /// <summary>
        /// Test user delete account on local level
        /// </summary>
        [Fact]
        public void TestDeleteBULevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            AddAccountToDatabase(accId, UserBULvl12.ToEntityReference());

            // test delete account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                throw new XunitException("User should not be able to delete");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { "account",
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try delete account with basic privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                throw new XunitException("User should not be able to delete");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try delete account with local privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to delete");
            }

            // Add account to database
            AddAccountToDatabase(accId, UserBULvl12.ToEntityReference());

            // Add account delete privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try delete account with deep privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to delete");
            }

            // Add account to database
            AddAccountToDatabase(accId, UserBULvl12.ToEntityReference());

            // Add account delete privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try delete account with global privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to delete");
            }
        }

        /// <summary>
        /// Test user delete account on deep level
        /// </summary>
        [Fact]
        public void TestDeleteBUChildLevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            AddAccountToDatabase(accId, UserBULvl2.ToEntityReference());

            // test delete account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                throw new XunitException("User should not be able to delete");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { "account",
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try delete account with basic privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                throw new XunitException("User should not be able to delete");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try delete account with local privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                throw new XunitException("User should not be able to delete");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try delete account with deep privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to delete");
            }

            // Add account to database
            AddAccountToDatabase(accId, UserBULvl2.ToEntityReference());

            // Add account delete privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try delete account with global privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to delete");
            }
        }

        /// <summary>
        /// Test user delete account on global level
        /// </summary>
        [Fact]
        public void TestDeleteGlobalLevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            AddAccountToDatabase(accId, UserBURoot.ToEntityReference());

            // test delete account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                throw new XunitException("User should not be able to delete");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { "account",
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try delete account with basic privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                throw new XunitException("User should not be able to delete");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try delete account with local privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                throw new XunitException("User should not be able to delete");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try delete account with deep privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                throw new XunitException("User should not be able to delete");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try delete account with global privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to delete");
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
    }
}
