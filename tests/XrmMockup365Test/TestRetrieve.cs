using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using System.ServiceModel;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.Tools.XrmMockup;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestRetrieve : UnitTestBase
    {
        public TestRetrieve(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestReferenceHasPrimaryAttribute()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var id1 = this.orgAdminUIService.Create(new Account() { Name = "MLJ UnitTest" });
                var id2 = this.orgAdminUIService.Create(new Account() { Name = "MLJ UnitTest2" });

                var acc1a = new Account(id1)
                {
                    ParentAccountId = new EntityReference(Account.EntityLogicalName, id2)
                };
                this.orgAdminUIService.Update(acc1a);

                var retrieved = this.orgAdminUIService.Retrieve(Account.EntityLogicalName, id1,
                    new ColumnSet("accountid", "parentaccountid")).ToEntity<Account>();
                Assert.NotNull(retrieved.ParentAccountId);
                Assert.Equal(retrieved.Id, id1);

            }
        }

        [Fact]
        public void TestLookupFormattedValues()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var id1 = this.orgAdminUIService.Create(new Account() { Name = "MLJ UnitTest" });
                var id2 = this.orgAdminUIService.Create(new Account() { Name = "MLJ UnitTest2" });

                var acc1a = new Account(id1)
                {
                    ParentAccountId = new EntityReference(Account.EntityLogicalName, id2)
                };
                this.orgAdminUIService.Update(acc1a);

                var retrieved = this.orgAdminUIService.Retrieve(Account.EntityLogicalName, id1,
                    new ColumnSet("accountid", "parentaccountid")).ToEntity<Account>();
                Assert.NotNull(retrieved.ParentAccountId);
                Assert.Equal("MLJ UnitTest2", retrieved.FormattedValues["parentaccountid"]);
            }
        }

        [Fact]
        public void TestRetrieveHasId()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var accountName = "Litware, Inc.";
                var _accountId = orgAdminUIService.Create(
                new Account
                {
                    Name = accountName,
                    Address1_StateOrProvince = "Colorado"
                });

                var entity = (Account)orgAdminUIService.Retrieve(Account.EntityLogicalName, _accountId, new ColumnSet(true));
                Assert.Equal(_accountId, entity.Id);
                entity = (Account)orgAdminUIService.Retrieve(Account.EntityLogicalName, _accountId, new ColumnSet("name"));
                Assert.Equal(_accountId, entity.Id);

                // Substituted calendar (not in the leaned environment) with ctx_parent: same check —
                // an entity created with no attributes, retrieved with ColumnSet(true) and with a
                // single lookup-only column, still has its Id populated on the returned entity.
                var calId = orgAdminUIService.Create(new Entity("ctx_parent"));
                var cal = orgAdminUIService.Retrieve("ctx_parent", calId, new ColumnSet(true));
                Assert.Equal(calId, cal.Id);
                cal = orgAdminUIService.Retrieve("ctx_parent", calId, new ColumnSet("createdby"));
                Assert.Equal(calId, cal.Id);

            }
        }

        [Fact]
        public void TestRetrieveWithNullColumnset()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var account = new Account();
                account.Id = orgAdminUIService.Create(account);

                try
                {
                    orgAdminUIService.Retrieve(Account.EntityLogicalName, account.Id, null);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }


            }
        }


        [Fact]
        public void TestRetrieveRelatedEntities()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var accountName = "Litware, Inc.";
                var _accountId = orgAdminUIService.Create(
                new Account
                {
                    Name = accountName,
                    Address1_StateOrProvince = "Colorado"
                });

                // Create the two contacts.
                orgAdminUIService.Create(
                    new Contact()
                    {
                        FirstName = "Ben",
                        LastName = "Andrews",
                        EMailAddress1 = "sample@example.com",
                        Address1_City = "Redmond",
                        Address1_StateOrProvince = "WA",
                        Address1_Telephone1 = "(206)555-5555",
                        ParentCustomerId = new EntityReference
                        {
                            Id = _accountId,
                            LogicalName = Account.EntityLogicalName
                        }
                    });


                orgAdminUIService.Create(
                    new Contact()
                    {
                        FirstName = "Alan",
                        LastName = "Wilcox",
                        EMailAddress1 = "sample@example.com",
                        Address1_City = "Bellevue",
                        Address1_StateOrProvince = "WA",
                        Address1_Telephone1 = "(425)555-5555",
                        ParentCustomerId = new EntityReference
                        {
                            Id = _accountId,
                            LogicalName = Account.EntityLogicalName
                        }
                    });


                //create the query expression object
                QueryExpression query = new QueryExpression
                {
                    EntityName = "contact",
                    ColumnSet = new ColumnSet(true)
                };

                //create the relationship object
                Relationship relationship = new Relationship();

                //add the condition where you can retrieve only the account related active contacts
                query.Criteria = new FilterExpression();
                query.Criteria.AddCondition(new ConditionExpression("address1_city", ConditionOperator.Equal, "Bellevue"));

                // name of relationship between account & contact
                relationship.SchemaName = "contact_customer_accounts";

                //create relationshipQueryCollection Object
                RelationshipQueryCollection relatedEntity = new RelationshipQueryCollection
                {
                    { relationship, query }
                };

                //create the retrieve request object
                RetrieveRequest request = new RetrieveRequest
                {
                    RelatedEntitiesQuery = relatedEntity,
                    ColumnSet = new ColumnSet("accountid"),
                    Target = new EntityReference { Id = _accountId, LogicalName = "account" }
                };

                //execute the request
                RetrieveResponse response = (RetrieveResponse)orgAdminUIService.Execute(request);

                Assert.Single(response.Entity.RelatedEntities);
                var collection = response.Entity.RelatedEntities.Values.First();
                Assert.Single(collection.Entities);
                var entity = collection.Entities.First();
                Assert.True(entity.Attributes.ContainsKey("firstname"));
                Assert.Equal("Alan", entity.Attributes["firstname"]);

            }
        }

        [Fact]
        public void TestFetchMoneyAttribute()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                // Migrated from Invoice -> Account (Invoice not available); Invoice.TotalAmount -> Account.Revenue.
                // Exercises retrieving a money attribute with/without its transactioncurrencyid column.
                var invoice = new Account()
                {
                    Name = "test",
                    Revenue = 10m,
                };
                invoice.Id = orgGodService.Create(invoice);

                var retrievedSucceeds = orgAdminService.Retrieve(Account.EntityLogicalName, invoice.Id, new ColumnSet("revenue", "transactioncurrencyid")) as Account;
                Assert.NotNull(retrievedSucceeds.Revenue);
                Assert.Equal(10m, retrievedSucceeds.Revenue.Value);

                var retrievedFails = orgAdminService.Retrieve(Account.EntityLogicalName, invoice.Id, new ColumnSet("revenue")) as Account;
                Assert.NotNull(retrievedSucceeds.Revenue);
                Assert.Equal(10m, retrievedSucceeds.Revenue.Value);
            }
        }

        [Fact]
        public void TestFormattedValuesRetrieveMultiple()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                // Migrated from Invoice -> Account (Invoice not available); Invoice.PriorityCode ->
                // Account.AccountRatingCode (its DefaultValue option is labelled "Default Value").
                var invoice = new Account()
                {
                    Name = "test",
                    AccountRatingCode = account_accountratingcode.DefaultValue,
                };
                invoice.Id = orgAdminUIService.Create(invoice);

                var retrieved = context.AccountSet.FirstOrDefault();
                Assert.Equal("Default Value", retrieved.FormattedValues["accountratingcode"]);
            }
        }

        [Fact]
        public void TestFormattedValuesRetrieve()
        {
            // Migrated from Invoice -> Account (Invoice not available); Invoice.PriorityCode ->
            // Account.AccountRatingCode (its DefaultValue option is labelled "Default Value").
            var invoice = new Account()
            {
                Name = "test",
                AccountRatingCode = account_accountratingcode.DefaultValue,
            };
            invoice.Id = orgAdminUIService.Create(invoice);

            var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, invoice.Id, new ColumnSet(true));
            Assert.Equal("Default Value", retrieved.FormattedValues["accountratingcode"]);
        }

        [Fact]
        public void TestRetrieveInvalidAttributeFails()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var id1 = this.orgAdminUIService.Create(new Account() { Name = "SomeName" });
                var attr = "invalidatttributeaaaaaaaaaaaa";
                try
                {
                    orgAdminUIService.Retrieve(Account.EntityLogicalName, id1,
                        new ColumnSet(attr)).ToEntity<Account>();
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<MockupException>(e);
                    Assert.Equal($"'account' entity doesn't contain attribute with Name = '{attr}'", e.Message);
                }

            }
        }


        [Fact]
        public void TestEmptyCalculatedFieldss()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var id1 = this.orgAdminUIService.Create(new ctx_parent());
                ctx_parent.Retrieve(orgAdminService, id1, x => x.ctx_AmountCalc);
            }
        }

        [Fact]
        public void TestFormulaFields()
        {
            var adminUserId = Guid.Parse("3b961284-cd7a-4fa3-af7e-89802e88dd5c");
            var parentId = orgAdminService.Create(new ctx_parent
            {
                ctx_Name = "Fluffy",
                ctx_Amount = 5m,
                OwnerId = new EntityReference(SystemUser.EntityLogicalName, adminUserId)
            });

            // Power Fx formula columns must be evaluated on Retrieve:
            // ctx_TrimLeft = Mid(ctx_name, 3)  => ctx_Name without the first 2 chars
            // ctx_AmountCalc = ctx_amount * 20
            var parent = ctx_parent.Retrieve(orgAdminService, parentId, x => x.ctx_TrimLeft, x => x.ctx_AmountCalc);
            Assert.Equal("Fluffy".Substring(2), parent.ctx_TrimLeft);
            Assert.Equal(5m * 20, parent.ctx_AmountCalc);
        }

        [Fact]
        public void TestRetrieveUserByFullName()
        {
            var user = new Entity("systemuser");
            user["businessunitid"] = crm.RootBusinessUnit;
            user["firstname"] = "Matt";
            crm.CreateUser(orgAdminService, user, SecurityRoles.XrmMockupTestReadOnly);

            var q = new QueryExpression("systemuser");
            q.Criteria.AddCondition("fullname", ConditionOperator.Equal, "Matt");
            var users = orgAdminService.RetrieveMultiple(q);
            Assert.Single(users.Entities);
        }

        [Fact]
        public void TestRetrievePlugin()
        {
            var accountId = orgAdminService.Create(new Contact { LastName = "Test" });

            orgAdminUIService.Update(new Contact(accountId)
            {
                StateCode = contact_statecode.Inactive,
                StatusCode = contact_statuscode.Inactive
            });

            Assert.Throws<InvalidPluginExecutionException>(() => Contact.Retrieve(orgAdminService, accountId, x => x.LastName));
        }
    }
}
