using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class ContextTest : UnitTestBase
    {
        public ContextTest(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestAddObject()
        {
            // Migrated from Lead -> Contact (Lead removed from environment); this is a generic
            // context add/SaveChanges test, so any available entity works.
            using (var context = new Xrm(orgAdminUIService))
            {
                context.AddObject(new Contact());
                context.SaveChanges();
                Assert.NotNull(context.ContactSet.FirstOrDefault());
                context.ClearChanges();
            }
        }

        [Fact]
        public void TestContextIntersectEntity()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var acc1 = orgAdminUIService.Create(new Account());
                var acc2 = orgAdminUIService.Create(new Account());
                var acc3 = orgAdminUIService.Create(new Account());
                var con = orgAdminUIService.Create(new Contact());

                var relatedAccounts = new EntityReferenceCollection
                {
                    new EntityReference(Account.EntityLogicalName, acc1),
                    new EntityReference(Account.EntityLogicalName, acc2),
                    new EntityReference(Account.EntityLogicalName, acc3)
                };

                Relationship relationship = new Relationship("ctx_account_contact");

                orgAdminUIService.Associate(Contact.EntityLogicalName, con, relationship, relatedAccounts);

                // There is no early-bound intersect-entity set for ctx_account_contact, so assert the
                // association via a QueryExpression over account with a LinkEntity to the intersect entity
                // filtering on the contact id.
                var query = new QueryExpression(Account.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(false)
                };
                var link = query.AddLink("ctx_account_contact", "accountid", "accountid");
                link.LinkCriteria.AddCondition("contactid", ConditionOperator.Equal, con);

                var associatedAccounts = orgAdminUIService.RetrieveMultiple(query);
                Assert.Equal(3, associatedAccounts.Entities.Count);
            }
        }


        [Fact]
        public void TestUpdateObject()
        {
            // Migrated from Lead -> Contact (Lead removed from environment); generic update-via-context test.
            using (var context = new Xrm(orgAdminUIService))
            {
                var contact = new Contact
                {
                    FirstName = "Before"
                };
                orgAdminUIService.Create(contact);
                var contactFromContext = context.ContactSet.First();
                contactFromContext.FirstName = "After";
                context.SaveChanges();

                Assert.Equal(contact.FirstName, context.ContactSet.First().FirstName);
                context.ClearChanges();

                context.Attach(contactFromContext);
                context.UpdateObject(contactFromContext);
                context.SaveChanges();

                Assert.Equal(contactFromContext.FirstName, context.ContactSet.First().FirstName);
                context.ClearChanges();
            }
        }
    }
}
