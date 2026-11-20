using System.Linq;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestCascade : UnitTestBase
    {
        SystemUser user1;
        Account acc1;
        Account acc2;
        Contact contact;

        public TestCascade(XrmMockupFixture fixture) : base(fixture)
        {
            user1 = crm.CreateUser(orgGodService, crm.RootBusinessUnit, SecurityRoles.XrmMockupTestUser).ToEntity<SystemUser>();

            acc1 = new Account()
            {
                Name = "Parent Account"
            };
            acc1.Id = orgAdminUIService.Create(acc1);

            acc2 = new Account()
            {
                Name = "Account",
                ParentAccountId = acc1.ToEntityReference()
            };
            acc2.Id = orgAdminUIService.Create(acc2);

            contact = new Contact()
            {
                FirstName = "Child Contact",
                LastName = "Test",
                ParentCustomerId = acc2.ToEntityReference()
            };
            contact.Id = orgAdminUIService.Create(contact);
        }

        [Fact]
        public void TestCascadeAssignParentToChildren()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedAccount1 = context.AccountSet.FirstOrDefault(x => x.Id == acc1.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedAccount1.OwnerId.Id);
                var fetchedAccount2 = context.AccountSet.FirstOrDefault(x => x.Id == acc2.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedAccount2.OwnerId.Id);
                var fetchedContact = context.ContactSet.FirstOrDefault(x => x.Id == contact.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedContact.OwnerId.Id);
            }

            var req = new AssignRequest()
            {
                Assignee = user1.ToEntityReference(),
                Target = acc1.ToEntityReference()
            };
            orgAdminUIService.Execute(req);

            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedAccount1 = context.AccountSet.FirstOrDefault(x => x.Id == acc1.Id);
                Assert.Equal(user1.Id, fetchedAccount1.OwnerId.Id);
                var fetchedAccount2 = context.AccountSet.FirstOrDefault(x => x.Id == acc2.Id);
                Assert.Equal(user1.Id, fetchedAccount2.OwnerId.Id);
                var fetchedContact = context.ContactSet.FirstOrDefault(x => x.Id == contact.Id);
                Assert.Equal(user1.Id, fetchedContact.OwnerId.Id);
            }
        }

        [Fact]
        public void TestCascadeAssignParentToChildAndParent()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedAccount1 = context.AccountSet.FirstOrDefault(x => x.Id == acc1.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedAccount1.OwnerId.Id);
                var fetchedAccount2 = context.AccountSet.FirstOrDefault(x => x.Id == acc2.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedAccount2.OwnerId.Id);
                var fetchedContact = context.ContactSet.FirstOrDefault(x => x.Id == contact.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedContact.OwnerId.Id);
            }

            var req = new AssignRequest()
            {
                Assignee = user1.ToEntityReference(),
                Target = acc2.ToEntityReference()
            };
            orgAdminUIService.Execute(req);

            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedAccount1 = context.AccountSet.FirstOrDefault(x => x.Id == acc1.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedAccount1.OwnerId.Id);
                var fetchedAccount2 = context.AccountSet.FirstOrDefault(x => x.Id == acc2.Id);
                Assert.Equal(user1.Id, fetchedAccount2.OwnerId.Id);
                var fetchedContact = context.ContactSet.FirstOrDefault(x => x.Id == contact.Id);
                Assert.Equal(user1.Id, fetchedContact.OwnerId.Id);
            }
        }

        [Fact]
        public void TestCascadeAssignChildToParents()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedAccount1 = context.AccountSet.FirstOrDefault(x => x.Id == acc1.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedAccount1.OwnerId.Id);
                var fetchedAccount2 = context.AccountSet.FirstOrDefault(x => x.Id == acc2.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedAccount2.OwnerId.Id);
                var fetchedContact = context.ContactSet.FirstOrDefault(x => x.Id == contact.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedContact.OwnerId.Id);
            }

            var req = new AssignRequest()
            {
                Assignee = user1.ToEntityReference(),
                Target = contact.ToEntityReference()
            };
            orgAdminUIService.Execute(req);

            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedAccount1 = context.AccountSet.FirstOrDefault(x => x.Id == acc1.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedAccount1.OwnerId.Id);
                var fetchedAccount2 = context.AccountSet.FirstOrDefault(x => x.Id == acc2.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedAccount2.OwnerId.Id);
                var fetchedContact = context.ContactSet.FirstOrDefault(x => x.Id == contact.Id);
                Assert.Equal(user1.Id, fetchedContact.OwnerId.Id);
            }
        }

        [Fact]
        public void TestCascadeDeleteParentToChildren()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedAccount1 = context.AccountSet.FirstOrDefault(x => x.Id == acc1.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedAccount1.OwnerId.Id);
                var fetchedAccount2 = context.AccountSet.FirstOrDefault(x => x.Id == acc2.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedAccount2.OwnerId.Id);
                var fetchedContact = context.ContactSet.FirstOrDefault(x => x.Id == contact.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedContact.OwnerId.Id);
            }

            orgAdminUIService.Delete(Account.EntityLogicalName, acc1.Id);

            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedAccount1 = context.AccountSet.FirstOrDefault(x => x.Id == acc1.Id);
                Assert.Null(fetchedAccount1);
                var fetchedAccount2 = context.AccountSet.FirstOrDefault(x => x.Id == acc2.Id);
                Assert.NotNull(fetchedAccount2);
                Assert.Null(fetchedAccount2.ParentAccountId);
                var fetchedContact = context.ContactSet.FirstOrDefault(x => x.Id == contact.Id);
                Assert.NotNull(fetchedContact);
            }
        }

        [Fact]
        public void TestCascadeDeleteParentToChildAndParent()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedAccount1 = context.AccountSet.FirstOrDefault(x => x.Id == acc1.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedAccount1.OwnerId.Id);
                var fetchedAccount2 = context.AccountSet.FirstOrDefault(x => x.Id == acc2.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedAccount2.OwnerId.Id);
                var fetchedContact = context.ContactSet.FirstOrDefault(x => x.Id == contact.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedContact.OwnerId.Id);
            }

            orgAdminUIService.Delete(Account.EntityLogicalName, acc2.Id);

            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedAccount1 = context.AccountSet.FirstOrDefault(x => x.Id == acc1.Id);
                Assert.NotNull(fetchedAccount1);
                var fetchedAccount2 = context.AccountSet.FirstOrDefault(x => x.Id == acc2.Id);
                Assert.Null(fetchedAccount2);
                var fetchedContact = context.ContactSet.FirstOrDefault(x => x.Id == contact.Id);
                Assert.Null(fetchedContact);
            }
        }

        [Fact]
        public void TestCascadeDeleteChildToParents()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedAccount1 = context.AccountSet.FirstOrDefault(x => x.Id == acc1.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedAccount1.OwnerId.Id);
                var fetchedAccount2 = context.AccountSet.FirstOrDefault(x => x.Id == acc2.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedAccount2.OwnerId.Id);
                var fetchedContact = context.ContactSet.FirstOrDefault(x => x.Id == contact.Id);
                Assert.Equal(crm.AdminUser.Id, fetchedContact.OwnerId.Id);
            }

            orgAdminUIService.Delete(Contact.EntityLogicalName, contact.Id);

            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchedAccount1 = context.AccountSet.FirstOrDefault(x => x.Id == acc1.Id);
                Assert.NotNull(fetchedAccount1);
                var fetchedAccount2 = context.AccountSet.FirstOrDefault(x => x.Id == acc2.Id);
                Assert.NotNull(fetchedAccount2);
                var fetchedContact = context.ContactSet.FirstOrDefault(x => x.Id == contact.Id);
                Assert.Null(fetchedContact);
            }
        }
    }
}
