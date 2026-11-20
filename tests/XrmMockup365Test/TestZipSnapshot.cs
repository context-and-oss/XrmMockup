using System;
using System.Collections.Generic;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.Tools.XrmMockup;
using Xunit;
using System.ServiceModel;

namespace DG.XrmMockupTest
{
    public class TestZipSnapshot : UnitTestBase
    {
        public TestZipSnapshot(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestTakeSnapshot()
        {
            var contact = new Contact()
            {
                FirstName = "John"
            };
            contact.Id = orgAdminService.Create(contact);

            crm.TakeZipSnapshot("testfile");

            var dbContact = Contact.Retrieve(orgAdminService, contact.Id);
            Assert.Equal(contact.FirstName, dbContact.FirstName);

            crm.ResetEnvironment();

            var ex = Assert.Throws<FaultException>(() => Contact.Retrieve(orgAdminService, contact.Id));
            Assert.Equal(ex.Message, $"The record of type '{contact.LogicalName}' with id '{contact.Id}' does not exist. If you use hard-coded records from CRM, then make sure you create those records before retrieving them.");

            crm.RestoreZipSnapshot("testfile");

            var dbContact2 = Contact.Retrieve(orgAdminService, contact.Id);
            Assert.Equal(contact.FirstName, dbContact2.FirstName);
        }

        [Fact]
        public void TestMultipleSnapshot()
        {
            var contactJ = new Contact()
            {
                FirstName = "John"
            };
            contactJ.Id = orgAdminService.Create(contactJ);

            crm.TakeZipSnapshot("testfile1");
            crm.ResetEnvironment();

            var contactP = new Contact()
            {
                FirstName = "Peter"
            };
            contactP.Id = orgAdminService.Create(contactP);

            crm.TakeZipSnapshot("testfile2");
            crm.ResetEnvironment();
            crm.RestoreZipSnapshot("testfile1");

            var dbContactJ = Contact.Retrieve(orgAdminService, contactJ.Id);
            Assert.Equal(contactJ.FirstName, dbContactJ.FirstName);

            try
            {
                Contact.Retrieve(orgAdminService, contactP.Id);
            }
            catch (Exception ex)
            {
                Assert.Equal(ex.Message, $"The record of type '{contactP.LogicalName}' with id '{contactP.Id}' does not exist. If you use hard-coded records from CRM, then make sure you create those records before retrieving them.");
            }

            crm.RestoreZipSnapshot("testfile2");

            var dbContactP = Contact.Retrieve(orgAdminService, contactP.Id);
            Assert.Equal(contactP.FirstName, dbContactP.FirstName);

            try
            {
                Contact.Retrieve(orgAdminService, contactJ.Id);
            }
            catch (Exception ex)
            {
                Assert.Equal(ex.Message, $"The record of type '{contactJ.LogicalName}' with id '{contactJ.Id}' does not exist. If you use hard-coded records from CRM, then make sure you create those records before retrieving them.");
            }
        }

        [Fact]
        public void TestMultipleSnapshotIncremental()
        {
            var contactJ = new Contact()
            {
                FirstName = "John"
            };
            contactJ.Id = orgAdminService.Create(contactJ);

            crm.TakeZipSnapshot("testfile1");

            var contactUpd = new Contact(contactJ.Id)
            {
                LastName = "Smith"
            };
            orgAdminService.Update(contactUpd);

            crm.TakeZipSnapshot("testfile2");

            crm.ResetEnvironment();
            crm.RestoreZipSnapshot("testfile1");

            var dbContactJ = Contact.Retrieve(orgAdminService, contactJ.Id);
            Assert.Equal(contactJ.FirstName, dbContactJ.FirstName);
            Assert.Null(contactJ.LastName);

            crm.ResetEnvironment();
            crm.RestoreZipSnapshot("testfile2");

            var dbContactJ2 = Contact.Retrieve(orgAdminService, contactJ.Id);
            Assert.Equal(contactJ.FirstName, dbContactJ2.FirstName);
            Assert.Equal(contactUpd.LastName, dbContactJ2.LastName);
        }

        [Fact]
        public void TestTakeActionAfterRestore()
        {
            var contactJ = new Contact()
            {
                FirstName = "John"
            };
            contactJ.Id = orgAdminService.Create(contactJ);

            var testUser = new SystemUser()
            {
                LastName = "test",
                BusinessUnitId = crm.RootBusinessUnit
            };
            testUser.Id = crm.CreateUser(orgAdminService, testUser, SecurityRoles.SystemAdministrator).Id;

            crm.TakeZipSnapshot("testfile");
            crm.ResetEnvironment();
            crm.RestoreZipSnapshot("testfile");

            // Create Contact

            var contactP = new Contact()
            {
                FirstName = "Peter"
            };
            contactP.Id = orgAdminService.Create(contactP);

            var dbContactJ = Contact.Retrieve(orgAdminService, contactJ.Id);
            Assert.Equal(contactJ.FirstName, dbContactJ.FirstName);

            var dbContactP = Contact.Retrieve(orgAdminService, contactP.Id);
            Assert.Equal(contactP.FirstName, dbContactP.FirstName);

            // Create Account

            var acc = new Account()
            {
                Name = "test"
            };
            acc.Id = orgAdminUIService.Create(acc);

            var accFetch = Account.Retrieve(orgAdminService, acc.Id);
            Assert.Equal(acc.Name, accFetch.Name);

            // Set Owner
            var ownerUpd = new Contact(contactP.Id)
            {
                OwnerId = testUser.ToEntityReference(),
            };
            orgAdminService.Update(ownerUpd);
        }
    }
}
