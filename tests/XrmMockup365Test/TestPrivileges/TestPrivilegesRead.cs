using System;
using Microsoft.Crm.Sdk.Messages;
using System.Collections.Generic;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestPrivilegesRead : UnitTestBase
    {
        SystemUser UserBURoot;
        SystemUser UserBULvl11;
        SystemUser UserBULvl12;
        SystemUser UserBULvl2;

        public TestPrivilegesRead(XrmMockupFixture fixture) : base(fixture)
        {
            var buLevel1 = orgAdminService.Create(new BusinessUnit() { ParentBusinessUnitId = crm.RootBusinessUnit, Name = "Business Level 1" });
            var buLevel2 = orgAdminService.Create(new BusinessUnit() { ParentBusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel1), Name = "Business Level 2" });

            UserBURoot = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userRoot@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, crm.RootBusinessUnit.Id) }) as SystemUser;
            UserBULvl11 = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userBULvl11@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel1) }) as SystemUser;
            UserBULvl12 = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userBULvl12@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel1) }) as SystemUser;
            UserBULvl2 = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userBULvl2@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel2) }) as SystemUser;
        }

        /// <summary>
        /// Test user read account on basic level
        /// </summary>
        [Fact]
        public void TestReadUserLevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            crm.PopulateWith(new Account(accId)
            {
                Name = "Account",
                OwnerId = UserBULvl11.ToEntityReference(),
            });

            // test read account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                throw new XunitException("User should not be able to read");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try read account with basic privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to read");
            }

            // Add account read privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try read account with local privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to read");
            }

            // Add account read privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try read account with deep privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to read");
            }

            // Add account read privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try read account with global privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to read");
            }
        }

        /// <summary>
        /// Test user read account on local level
        /// </summary>
        [Fact]
        public void TestReadBULevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            crm.PopulateWith(new Account(accId)
            {
                Name = "Account",
                OwnerId = UserBULvl12.ToEntityReference(),
            });

            // test read account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                throw new XunitException("User should not be able to read");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { "account",
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try read account with basic privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                throw new XunitException("User should not be able to read");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try read account with local privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to read");
            }

            // Add account read privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try read account with deep privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to read");
            }

            // Add account read privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try read account with global privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to read");
            }
        }

        /// <summary>
        /// Test user read account on deep level
        /// </summary>
        [Fact]
        public void TestReadBUChildLevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            crm.PopulateWith(new Account(accId)
            {
                Name = "Account",
                OwnerId = UserBULvl2.ToEntityReference(),
            });

            // test read account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                throw new XunitException("User should not be able to read");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { "account",
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try read account with basic privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                throw new XunitException("User should not be able to read");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try read account with local privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                throw new XunitException("User should not be able to read");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try read account with deep privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to read");
            }

            // Add account read privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try read account with global privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to read");
            }
        }

        /// <summary>
        /// Test user read account on global level
        /// </summary>
        [Fact]
        public void TestReadGlobalLevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            crm.PopulateWith(new Account(accId)
            {
                Name = "Account",
                OwnerId = UserBURoot.ToEntityReference(),
            });

            // test read account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                throw new XunitException("User should not be able to read");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { "account",
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try read account with basic privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                throw new XunitException("User should not be able to read");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try read account with local privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                throw new XunitException("User should not be able to read");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try read account with deep privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                throw new XunitException("User should not be able to read");
            }
            catch (XunitException)
            {
                throw new XunitException("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try read account with global privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                throw new XunitException("User should be able to read");
            }
        }
    }
}
