using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestRetrieveMultiple : UnitTestBase
    {
        Account account1;
        Account account2;
        Account account3;
        Account account4;

        Contact contact1;
        Contact contact2;
        Contact contact3;
        Contact contact4;

        ctx_parent p1;
        ctx_parent p2;
        ctx_parent p3;
        ctx_parent p4;

        public TestRetrieveMultiple(XrmMockupFixture fixture) : base(fixture)
        {
            account1 = new Account();
            account2 = new Account();
            account3 = new Account();
            account4 = new Account();

            account1.Name = "account1";
            account2.Name = "account2";
            account3.Name = "account3";
            account4.Name = "account4";

            account1.Address1_City = "Virum";
            account2.Address1_City = "Virum";
            account3.Address1_City = "Lyngby";
            account4.Address1_City = "Lyngby";

            account1.Address1_PostalCode = "MK111DW";
            account2.Address1_PostalCode = "OX449NG";
            account3.Address1_PostalCode = "S103AY";
            account4.Address1_PostalCode = "SY71SW";

            account1.DoNotEMail = true;

            // account_accountratingcode has only DefaultValue in this environment, so ordering by it
            // can't distinguish records; use Revenue (distinct, orderable) instead. account2's value is
            // lower than account1's to preserve the original ascending order (account2 first).
            // NumberOfEmployees is intentionally left unset on all accounts so the null-filter tests
            // (TestRetrieveMultipleFilterNullOrGreaterThan etc.) see no records with a value.
            account1.Revenue = 2;
            account2.Revenue = 1;

            account1.Id = orgAdminUIService.Create(account1);
            account2.Id = orgAdminUIService.Create(account2);
            account3.Id = orgAdminUIService.Create(account3);
            account4.Id = orgAdminUIService.Create(account4);

            contact1 = new Contact();
            contact2 = new Contact();
            contact3 = new Contact();
            contact4 = new Contact();

            contact1.LastName = "contact1";
            contact2.LastName = "contact2";
            contact3.LastName = "contact3";
            contact4.LastName = "contact4";

            contact1.Id = orgAdminUIService.Create(contact1);
            contact2.Id = orgAdminUIService.Create(contact2);
            contact3.Id = orgAdminUIService.Create(contact3);
            contact4.Id = orgAdminUIService.Create(contact4);

            var rand = new Random();
            p1 = new ctx_parent()
            {
                ctx_Name = "Some contact lead " + rand.Next(0, 1000),
                ctx_ContactId = contact1.ToEntityReference(),
                ctx_Industrycode = ctx_parent_ctx_industrycode.Accounting,
                ctx_Postalcode = account1.Address1_PostalCode
            };
            p2 = new ctx_parent()
            {
                ctx_Name = "Some contact lead " + rand.Next(0, 1000),
                ctx_ContactId = contact2.ToEntityReference()
            };
            p3 = new ctx_parent()
            {
                ctx_Name = "Some new lead " + rand.Next(0, 1000),
                ctx_DateValue = new DateTime(2025, 9, 29, 7, 28, 0, DateTimeKind.Local)
            };
            p4 = new ctx_parent()
            {
                ctx_Name = "Some new lead " + rand.Next(0, 1000),
                ctx_Score = 100,
            };

            p1.Id = orgAdminUIService.Create(p1);
            p2.Id = orgAdminUIService.Create(p2);
            p3.Id = orgAdminUIService.Create(p3);
            p4.Id = orgAdminUIService.Create(p4);
        }

        [Fact]
        public void TestInnerJoin()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var accountId = account1.Id;

                var query =
                    from acc in context.AccountSet
                    join lead in context.ctx_parentSet on acc.AccountId equals lead.ctx_AccountId.Id
                    where acc.AccountId == accountId
                    select new { acc.Name, lead.ctx_Name };

                var result = query.AsEnumerable().Where(x => x.ctx_Name.StartsWith("Some"));
                Assert.Equal(2, result.Count());

                foreach (var res in result)
                {
                    Assert.Equal(account1.Name, res.Name);
                    Assert.StartsWith("Some", res.ctx_Name);
                }
            }
        }

        [Fact]
        public void TestFilterOnJoin()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from con in context.ContactSet
                    join lead in context.ctx_parentSet
                    on con.ContactId equals lead.ctx_ContactId.Id
                    where lead.ctx_Name.StartsWith("Some") && con.LastName.StartsWith("contact")
                    select new { con.LastName, lead.ctx_Name };

                var result = query.ToArray();
                Assert.Equal(2, result.Count());
            }
        }

        [Fact]
        public void TestAllColumns()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from con in context.ContactSet
                    where con.ContactId == contact1.Id
                    select con;

                var result = query.First();
                Assert.Equal(contact1.LastName, result.LastName);
            }
        }

        [Fact]
        public void TestFilterOnOptionSet()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                contact1.SetState(orgAdminUIService, contact_statecode.Inactive);

                var query =
                    from con in context.ContactSet
                    where con.StateCode == contact_statecode.Inactive
                    select con;

                var result = query.First();
                Assert.Equal(contact1.LastName, result.LastName);
            }
        }

        [Fact]
        public void TestOrderingJoin()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var res = account1.Id;

                var query =
                    from acc in context.AccountSet
                    join lead in context.ctx_parentSet
                    on acc.AccountId equals lead.ctx_AccountId.Id
                    orderby acc.Name descending, acc.AccountId
                    where acc.Name.StartsWith("account")
                    select new { acc.Name, acc.AccountId, lead.ctx_Name };

                var result = query.AsEnumerable().Where(x => x.ctx_Name.StartsWith("Some"));
                Assert.Equal(8, result.Count());

                var ordered = result.OrderByDescending(x => x.Name).ThenBy(x => x.AccountId);
                Assert.Equal(ordered.Select(x => new { x.Name, x.AccountId }).ToList(), result.Select(x => new { x.Name, x.AccountId }).ToList());
            }
        }

        [Fact]
        public void BusinessUnitChange()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var rootBu = context.BusinessUnitSet.FirstOrDefault();

                var anotherBusinessUnit = new BusinessUnit();
                anotherBusinessUnit["name"] = "Business unit name";
                anotherBusinessUnit.Id = orgAdminUIService.Create(anotherBusinessUnit);

                var user = crm.CreateUser(orgAdminUIService, anotherBusinessUnit.ToEntityReference(), SecurityRoles.SystemAdministrator) as SystemUser;

                // Create and check account user/bu
                var acc = new Account();
                acc.Id = orgAdminUIService.Create(acc);

                acc = orgAdminUIService.Retrieve<Account>(acc.Id);
                Assert.Equal(acc.OwnerId.Id, crm.AdminUser.Id);

                // Update and check new account user/bu
                var upd = new Account(acc.Id)
                {
                    OwnerId = user.ToEntityReference()
                };
                orgAdminUIService.Update(upd);

                acc = orgAdminUIService.Retrieve<Account>(acc.Id);
                Assert.Equal(acc.OwnerId.Id, user.Id);
                Assert.Equal(acc.OwningBusinessUnit.Id, user.BusinessUnitId.Id);
            }
        }

        // Removed: ContextJoinTest / ContextLoadDifferentEntitiesTest. They exercised the XrmContext
        // lazy-load API (context.Load / LoadEnumeration) and account self-parent reverse-nav properties.
        // That API is generated code owned by XrmContext — verifying it belongs in XrmContext's own test
        // suite, not here (this suite tests XrmMockup's engine behavior). The regenerated context no
        // longer emits those helpers, so there is nothing for XrmMockup to exercise.

        [Fact]
        public void TestLeftJoin()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    (from con in context.ContactSet
                     join lead in context.ctx_parentSet
                     on con.ContactId equals lead.ctx_ContactId.Id into ls
                     from lead in ls.DefaultIfEmpty()
                     select new { con.ContactId, lead.ctx_Name })
                    .ToList();

                Assert.Equal(4, query.Count);

                foreach (var r in query)
                {
                    Assert.True(r.ctx_Name == null && (r.ContactId == contact3.Id || r.ContactId == contact4.Id) ||
                        r.ctx_Name.StartsWith("Some contact lead") && (r.ContactId == contact1.Id || r.ContactId == contact2.Id));
                }
            }
        }

        [Fact]
        public void TestNotAnyJoin()
        {
            // Test NotAny join operator - should return contacts that do NOT have any associated leads
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("contactid", "lastname")
            };

            var linkEntity = new LinkEntity()
            {
                LinkToEntityName = "ctx_parent",
                LinkToAttributeName = "ctx_contactid",
                LinkFromEntityName = "contact",
                LinkFromAttributeName = "contactid",
                Columns = new ColumnSet(false),
                EntityAlias = "ctx_parent",
                JoinOperator = JoinOperator.NotAny,
            };

            query.LinkEntities.Add(linkEntity);

            var result = orgAdminService.RetrieveMultiple(query).Entities.Cast<Contact>().ToList();

            // Should return contact3 and contact4 since they have no associated leads
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Id == contact3.Id);
            Assert.Contains(result, x => x.Id == contact4.Id);
            Assert.DoesNotContain(result, x => x.Id == contact1.Id);
            Assert.DoesNotContain(result, x => x.Id == contact2.Id);
        }

        [Fact]
        public void TestNotAnyJoinFetchXml()
        {
            // Test NotAny join operator via FetchXML - should return contacts that do NOT have any associated leads
            var fetchXml = @"
                <fetch>
                    <entity name='contact'>
                        <attribute name='contactid' />
                        <attribute name='lastname' />
                        <link-entity name='ctx_parent' from='ctx_contactid' to='contactid' link-type='notany' />
                    </entity>
                </fetch>";

            var fetchExpr = new FetchExpression(fetchXml);
            var result = orgAdminService.RetrieveMultiple(fetchExpr).Entities.Cast<Contact>().ToList();

            // Should return contact3 and contact4 since they have no associated leads
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Id == contact3.Id);
            Assert.Contains(result, x => x.Id == contact4.Id);
            Assert.DoesNotContain(result, x => x.Id == contact1.Id);
            Assert.DoesNotContain(result, x => x.Id == contact2.Id);
        }

        [Fact]
        public void TestNestedJoins()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var res = contact1.Id;

                EntityReferenceCollection relatedEntities = new EntityReferenceCollection
                {
                    new EntityReference(Account.EntityLogicalName, account1.Id),
                    new EntityReference(Account.EntityLogicalName, account2.Id)
                };


                orgAdminUIService.Associate(
                    Contact.EntityLogicalName, contact1.Id, new Relationship("account_primary_contact"), relatedEntities);

                var query =
                    from con in context.ContactSet
                    join acc in context.AccountSet
                    on con.ContactId equals acc.PrimaryContactId.Id
                    join lead in context.ctx_parentSet
                    on acc.AccountId equals lead.ctx_AccountId.Id
                    where con.Id == res
                    select new { con.LastName, acc.Name, lead.ctx_Name };

                var result = query.AsEnumerable().Where(x => x.ctx_Name.StartsWith("Some"));
                Assert.Equal(4, result.Count());

                foreach (var r in result)
                {
                    Assert.Equal(contact1.LastName, r.LastName);
                    Assert.True(account1.Name == r.Name || account2.Name == r.Name);
                    Assert.StartsWith("Some", r.ctx_Name);
                }
            }
        }

        [Fact]
        public void TestNestedJoinsLinkedSelectOnId()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var res = contact1.Id;

                EntityReferenceCollection relatedEntities = new EntityReferenceCollection
                {
                    new EntityReference(Account.EntityLogicalName, account1.Id),
                    new EntityReference(Account.EntityLogicalName, account2.Id)
                };


                orgAdminUIService.Associate(
                    Contact.EntityLogicalName, contact1.Id, new Relationship("account_primary_contact"), relatedEntities);

                var query =
                    from con in context.ContactSet
                    join acc in context.AccountSet
                    on con.ContactId equals acc.PrimaryContactId.Id
                    join lead in context.ctx_parentSet
                    on acc.AccountId equals lead.ctx_AccountId.Id
                    where con.Id == res
                    select new { con.Id, con.LastName, acc.Name, lead.ctx_Name };

                var result = query.AsEnumerable().Where(x => x.ctx_Name.StartsWith("Some"));
                Assert.Equal(4, result.Count());

                foreach (var r in result)
                {
                    Assert.Equal(contact1.LastName, r.LastName);
                    Assert.True(account1.Name == r.Name || account2.Name == r.Name);
                    Assert.StartsWith("Some", r.ctx_Name);
                }
            }
        }

        [Fact]
        public void TestFilter()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from acc in context.AccountSet
                    where acc.Address1_City == "Virum"
                    select new { acc.Name };


                Assert.Equal(2, query.AsEnumerable().Count());

                foreach (var r in query)
                {
                    Assert.True(account1.Name == r.Name || account2.Name == r.Name);
                }
            }
        }

        [Fact]
        public void TestOrderByOtherAttributes()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from acc in context.AccountSet
                    where acc.Address1_City == "Virum"
                    orderby acc.Revenue
                    select new { acc.AccountId };

                var result = query.ToArray();
                Assert.Equal(2, result.Length);
                Assert.Equal(account2.Id, result[0].AccountId);
                Assert.Equal(account1.Id, result[1].AccountId);
            }
        }

        [Fact]
        public void TestOrderByOtherAttributesDescending()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from acc in context.AccountSet
                    where acc.Address1_City == "Virum"
                    orderby acc.Revenue descending
                    select new { acc.AccountId };

                var result = query.ToArray();
                Assert.Equal(2, result.Length);
                Assert.Equal(account1.Id, result[0].AccountId);
                Assert.Equal(account2.Id, result[1].AccountId);
            }
        }

        [Fact]
        public void TestFetchOrderByOtherAttributes()
        {
            var conversionResponse = (FetchXmlToQueryExpressionResponse)orgAdminUIService.Execute(new FetchXmlToQueryExpressionRequest
            {
                FetchXml = $@"<fetch>
                    <entity name='account'>
                        <filter>
                            <condition attribute='address1_city' operator='eq' value='Virum'/>
                        </filter>
                        <order attribute='revenue' />
                    </entity>
                </fetch>"
            });
            EntityCollection result = orgAdminUIService.RetrieveMultiple(conversionResponse.Query);
            Assert.Equal(2, result.Entities.Count);
            Assert.Equal(account2.Id, result.Entities[0].GetAttributeValue<Guid>("accountid"));
            Assert.Equal(account1.Id, result.Entities[1].GetAttributeValue<Guid>("accountid"));
        }

        [Fact]
        public void TestFetchOrderByOtherAttributesDescending()
        {
            var conversionResponse = (FetchXmlToQueryExpressionResponse)orgAdminUIService.Execute(new FetchXmlToQueryExpressionRequest
            {
                FetchXml = $@"<fetch>
                    <entity name='account'>
                        <filter>
                            <condition attribute='address1_city' operator='eq' value='Virum'/>
                        </filter>
                        <order attribute='revenue' descending='true'/>
                    </entity>
                </fetch>"
            });
            EntityCollection result = orgAdminUIService.RetrieveMultiple(conversionResponse.Query);
            Assert.Equal(2, result.Entities.Count);
            Assert.Equal(account1.Id, result.Entities[0].GetAttributeValue<Guid>("accountid"));
            Assert.Equal(account2.Id, result.Entities[1].GetAttributeValue<Guid>("accountid"));
        }

        [Fact]
        public void TestOrderByLambdaSyntax()
        {
            orgAdminService.Update(new Account(account1.Id) { NumberOfEmployees = 10 });
            orgAdminService.Update(new Account(account2.Id) { NumberOfEmployees = 20 });

            using (var context = new Xrm(orgAdminUIService))
            {
                var query = context.AccountSet
                        .Where(acc => acc.Address1_City == "Virum")
                        .OrderBy(acc => acc.NumberOfEmployees)
                        .Select(acc => new { acc.AccountId });

                var result = query.ToArray();
                Assert.Equal(2, result.Length);
                Assert.Equal(account1.Id, result[0].AccountId);
                Assert.Equal(account2.Id, result[1].AccountId);
            }
        }

        [Fact]
        public void TestOrderByDescendingLambdaSyntax()
        {
            orgAdminService.Update(new Account(account1.Id) { NumberOfEmployees = 10 });
            orgAdminService.Update(new Account(account2.Id) { NumberOfEmployees = 20 });

            using (var context = new Xrm(orgAdminUIService))
            {
                var query = context.AccountSet
                        .Where(acc => acc.Address1_City == "Virum")
                        .OrderByDescending(acc => acc.NumberOfEmployees)
                        .Select(acc => new { acc.AccountId });

                var result = query.ToArray();
                Assert.Equal(2, result.Length);
                Assert.Equal(account2.Id, result[0].AccountId);
                Assert.Equal(account1.Id, result[1].AccountId);
            }
        }


        [Fact]
        public void RetrieveMultipleNotEqualsNullCheck()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var acc = new Account() { };
                acc.Id = orgAdminService.Create(acc);

                var contact1 = new Contact()
                {
                    ParentCustomerId = acc.ToEntityReference()
                };
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact()
                {
                    DoNotEMail = true,
                    ParentCustomerId = acc.ToEntityReference()
                };
                contact2.Id = orgAdminService.Create(contact2);


                var isTrue = context.ContactSet.Where(x => x.ParentCustomerId.Id == acc.Id && x.DoNotEMail == true).ToList();

                Assert.Single(isTrue);

                var isNotTrue = context.ContactSet.Where(x => x.ParentCustomerId.Id == acc.Id && x.DoNotEMail != true).ToList();
                Assert.Single(isNotTrue);
            }
        }

        [Fact]
        public void TestRetrieveMultipleFilterNullOr()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from acc in context.AccountSet
                    where acc.Address1_Country == null || acc.Name == "account1"
                    select acc;

                var result = query.ToList();
                Assert.Equal(4, result.Count);
                var accResult = result.FirstOrDefault();
                Assert.Equal("account1", accResult.Name);
            }
        }

        [Fact]
        public void TestRetrieveMultipleFilterNullAnd()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from acc in context.AccountSet
                    where acc.Address1_Country == null && acc.Name == "account1"
                    select acc;

                var result = query.ToList();
                Assert.Single(result);
                var accResult = result.FirstOrDefault();
                Assert.Equal("account1", accResult.Name);
            }
        }

        [Fact]
        public void TestRetrieveMultipleFilterGreaterThan5UnderlyingNull()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from acc in context.AccountSet
                    where acc.NumberOfEmployees >= 5
                    select acc;

                var result = query.ToList();
                Assert.Empty(result);
            }
        }

        [Fact]
        public void TestRetrieveMultipleFilterNullOrGreaterThan()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from acc in context.AccountSet
                    where acc.NumberOfEmployees != null || acc.NumberOfEmployees >= 5
                    select acc;

                var result = query.ToList();
                Assert.Empty(result);
            }
        }

        [Fact]
        public void TestRetrieveMultipleFilterOrWithNullValue()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var query =
                    from acc in context.AccountSet
                    where acc.NumberOfEmployees == null && acc.NumberOfEmployees >= 5
                    select acc;

                var result = query.ToList();
                Assert.Empty(result);
            }
        }

        [Fact]
        public void TestContextWhere()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var userId = context.SystemUserSet.First().Id;
                //Currently returns null. Should return the same record.
                var reFetched = context.SystemUserSet.Single(a => a.Id == userId);
                Assert.NotNull(reFetched);
            }
        }

        [Fact]
        public void TestQueryExpressionIn()
        {
            var query = new QueryExpression("ctx_parent")
            {
                ColumnSet = new ColumnSet(true)
            };

            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("ctx_contactid", ConditionOperator.In, new Guid[] { contact1.Id, contact2.Id, Guid.NewGuid() }));

            query.Criteria = filter;

            var res = orgAdminService.RetrieveMultiple(query).Entities.Cast<ctx_parent>();
            Assert.Equal(2, res.Count());
            Assert.Contains(res, x => x.Id == p1.Id);
            Assert.Contains(res, x => x.Id == p2.Id);
            // Dropped the Description assertions: they verified the LeadRetrieveMultiplePlugin (now
            // disabled) rewriting the first row's description, and ctx_parent has no Description field.
        }

        [Fact]
        public void TestQueryExpressionInGuidAndState()
        {
            var query = new QueryExpression("ctx_parent")
            {
                Distinct = true,
                ColumnSet = new ColumnSet("ctx_name", "ctx_contactid", "statecode"),
            };

            var filter = new FilterExpression();
            filter.Conditions.Add(new ConditionExpression("ctx_contactid", ConditionOperator.In, new Guid[] { contact1.Id, contact2.Id, Guid.NewGuid() }));

            var statecodes = new object[] { (int)ctx_parent_statecode.Active };
            filter.Conditions.Add(new ConditionExpression("statecode", ConditionOperator.In, statecodes));

            query.Criteria.Filters.Add(filter);

            var res = orgAdminService
                .RetrieveMultiple(query)
                .Entities
                .Cast<ctx_parent>()
                .ToList();

            Assert.Equal(2, res.Count);
            Assert.Contains(res, x => x.Id == p1.Id);
            Assert.Contains(res, x => x.Id == p2.Id);
        }

        [Fact]
        public void TestFetchExpressionIn()
        {
            var fetchXml =
                $@"<fetch>
                    <entity name='ctx_parent'>
                        <attribute name='ctx_contactid' />
                        <filter>
                            <condition attribute='ctx_contactid' operator='in'>
                                <value>{contact1.Id}</value>
                                <value>{contact2.Id}</value>
                            </condition>
                        </filter>
                    </entity>
                </fetch>";
            var res = orgAdminService.RetrieveMultiple(new FetchExpression(fetchXml)).Entities.Cast<ctx_parent>();
            Assert.Equal(2, res.Count());
            Assert.Contains(res, x => x.Id == p1.Id);
            Assert.Contains(res, x => x.Id == p2.Id);
            // Dropped the Description assertions: they verified the LeadRetrieveMultiplePlugin (now
            // disabled) rewriting the first row's description, and ctx_parent has no Description field.
        }

        [Fact]
        public void TestFetchExpressionInGuidAndState()
        {
            var fetchXml =
                $@"<fetch distinct='true'>
                    <entity name='ctx_parent'>
                        <attribute name='ctx_name' />
                        <attribute name='ctx_contactid' />
                        <attribute name='statecode' />
                        <filter>
                            <condition attribute='ctx_contactid' operator='in'>
                                <value>{contact1.Id}</value>
                                <value>{contact2.Id}</value>
                            </condition>
                            <condition attribute='statecode' operator='in'>
                                <value>{(int)ctx_parent_statecode.Active}</value>
                            </condition>
                        </filter>
                    </entity>
                </fetch>";

            var res = orgAdminService
                .RetrieveMultiple(new FetchExpression(fetchXml))
                .Entities
                .Cast<ctx_parent>()
                .ToList();

            Assert.Equal(2, res.Count);
            Assert.Contains(res, x => x.Id == p1.Id);
            Assert.Contains(res, x => x.Id == p2.Id);
        }

        [Fact]
        public void TestQueryExpressionInEmpty()
        {
            var query = new QueryExpression("ctx_parent")
            {
                ColumnSet = new ColumnSet(true)
            };

            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("ctx_contactid", ConditionOperator.In, new Guid[] { }));

            query.Criteria = filter;

            var res = orgAdminService.RetrieveMultiple(query).Entities.Cast<ctx_parent>();
            Assert.Empty(res);
        }

        [Fact]
        public void TestQueryExpressionNotIn()
        {
            var query = new QueryExpression("ctx_parent")
            {
                ColumnSet = new ColumnSet(true)
            };

            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("ctx_contactid", ConditionOperator.NotIn, new Guid[] { contact1.Id, contact2.Id, Guid.NewGuid() }));

            query.Criteria = filter;

            var res = orgAdminService.RetrieveMultiple(query).Entities.Cast<ctx_parent>();
            Assert.True(!res.Any(x => x.Id == p1.Id));
            Assert.True(!res.Any(x => x.Id == p2.Id));
        }

        [Fact]
        public void TestFetchExpressionNotIn()
        {
            var fetchXml =
                $@"<fetch>
                    <entity name='ctx_parent'>
                        <attribute name='ctx_contactid' />
                        <filter>
                            <condition attribute='ctx_contactid' operator='not-in'>
                                <value>{contact1.Id}</value>
                                <value>{contact2.Id}</value>
                            </condition>
                        </filter>
                    </entity>
                </fetch>";
            var res = orgAdminService.RetrieveMultiple(new FetchExpression(fetchXml)).Entities.Cast<ctx_parent>();
            Assert.True(!res.Any(x => x.Id == p1.Id));
            Assert.True(!res.Any(x => x.Id == p2.Id));
        }

        [Fact]
        public void TestQueryExpressionNotInEmpty()
        {
            var leadCount = 0;
            using (var context = new Xrm(orgAdminUIService))
            {
                leadCount = context.ctx_parentSet.Select(x => x.ctx_parentId).ToList().Count();
            }

            var query = new QueryExpression("ctx_parent")
            {
                ColumnSet = new ColumnSet(true)
            };

            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("ctx_contactid", ConditionOperator.NotIn, new Guid[] { }));

            query.Criteria = filter;

            var res = orgAdminService.RetrieveMultiple(query).Entities.Cast<ctx_parent>();
            Assert.Equal(leadCount, res.Count());
        }

        [Fact]
        public void TestQueryExpressionLinkEntity()
        {
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("lastname")
            };
            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("lastname", ConditionOperator.Like, "contact%"));
            query.Criteria = filter;

            var linkEntity = new LinkEntity()
            {
                LinkToEntityName = "ctx_parent",
                LinkToAttributeName = "ctx_contactid",
                LinkFromEntityName = "contact",
                LinkFromAttributeName = "contactid",
                Columns = new ColumnSet("ctx_name"),
                EntityAlias = "ctx_parent"
            };
            var linkFilter = new FilterExpression(LogicalOperator.And);
            linkFilter.AddCondition(new ConditionExpression("ctx_parent", "ctx_name", ConditionOperator.Like, "Some%"));
            linkEntity.LinkCriteria = linkFilter;

            query.LinkEntities.Add(linkEntity);

            var res = orgAdminService.RetrieveMultiple(query).Entities;
            Assert.Equal(2, res.Count());
        }

        [Fact]
        public void TestQueryExpressionMultipleLinkEntitiesAreAndJoined()
        {
            // Two sibling links, each with its own LinkCriteria, must be combined with AND
            // (cross join) semantics like real Dataverse. Previously each link was evaluated
            // independently and the results unioned, so a parent matching only one link still
            // came back.
            var accA = new Account { Name = "ReproA", Address1_City = "ReproCityA" };
            var accB = new Account { Name = "ReproB", Address1_City = "ReproCityB" };
            accA.Id = orgAdminService.Create(accA);
            accB.Id = orgAdminService.Create(accB);

            var conX = new Contact { LastName = "ReproX" };
            var conY = new Contact { LastName = "ReproY" };
            conX.Id = orgAdminService.Create(conX);
            conY.Id = orgAdminService.Create(conY);

            // pAX matches both link criteria; pAY matches only the account link; pBX only the contact link.
            var pAX = new ctx_parent { ctx_Name = "pAX", ctx_AccountId = accA.ToEntityReference(), ctx_ContactId = conX.ToEntityReference() };
            var pAY = new ctx_parent { ctx_Name = "pAY", ctx_AccountId = accA.ToEntityReference(), ctx_ContactId = conY.ToEntityReference() };
            var pBX = new ctx_parent { ctx_Name = "pBX", ctx_AccountId = accB.ToEntityReference(), ctx_ContactId = conX.ToEntityReference() };
            pAX.Id = orgAdminService.Create(pAX);
            pAY.Id = orgAdminService.Create(pAY);
            pBX.Id = orgAdminService.Create(pBX);

            var query = new QueryExpression("ctx_parent") { ColumnSet = new ColumnSet("ctx_name") };

            var accountLink = query.AddLink("account", "ctx_accountid", "accountid");
            accountLink.EntityAlias = "acc";
            accountLink.Columns = new ColumnSet("address1_city");
            accountLink.LinkCriteria.AddCondition("address1_city", ConditionOperator.Equal, "ReproCityA");

            var contactLink = query.AddLink("contact", "ctx_contactid", "contactid");
            contactLink.EntityAlias = "con";
            contactLink.Columns = new ColumnSet("lastname");
            contactLink.LinkCriteria.AddCondition("lastname", ConditionOperator.Equal, "ReproX");

            var res = orgAdminService.RetrieveMultiple(query).Entities;

            Assert.Single(res);
            var match = res[0];
            Assert.Equal(pAX.Id, match.Id);
            Assert.Equal("pAX", match.GetAttributeValue<string>("ctx_name"));
            // The cross-join must carry the aliased columns from BOTH links onto the single result row.
            Assert.Equal("ReproCityA", match.GetAttributeValue<AliasedValue>("acc.address1_city")?.Value);
            Assert.Equal("ReproX", match.GetAttributeValue<AliasedValue>("con.lastname")?.Value);
        }

        [Fact]
        public void TestQueryExpressionSiblingLinkDoesNotBreakOtherLinkCriteria()
        {
            // Regression for the reported bug: adding a second sibling link must not relax the
            // first link's criteria. Here no parent satisfies both criteria simultaneously, so
            // the result must be empty (it previously returned the OR of the two links).
            var accA = new Account { Name = "ReproC", Address1_City = "ReproCityC" };
            accA.Id = orgAdminService.Create(accA);

            var conY = new Contact { LastName = "ReproZ" };
            conY.Id = orgAdminService.Create(conY);

            // Matches the account link only; its contact does not match the contact link.
            var p = new ctx_parent { ctx_Name = "pOnlyAccount", ctx_AccountId = accA.ToEntityReference(), ctx_ContactId = conY.ToEntityReference() };
            p.Id = orgAdminService.Create(p);

            var query = new QueryExpression("ctx_parent") { ColumnSet = new ColumnSet("ctx_name") };

            var accountLink = query.AddLink("account", "ctx_accountid", "accountid");
            accountLink.LinkCriteria.AddCondition("address1_city", ConditionOperator.Equal, "ReproCityC");

            var contactLink = query.AddLink("contact", "ctx_contactid", "contactid");
            contactLink.LinkCriteria.AddCondition("lastname", ConditionOperator.Equal, "NoSuchLastName");

            var res = orgAdminService.RetrieveMultiple(query).Entities;

            Assert.Empty(res);
        }


        [Fact]
        public void TestQueryExpressionLinkEntityNoSetEntityNameAndAlias()
        {
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("lastname")
            };
            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("lastname", ConditionOperator.Like, "contact%"));
            query.Criteria = filter;

            var linkEntity = new LinkEntity()
            {
                LinkToEntityName = "ctx_parent",
                LinkToAttributeName = "ctx_contactid",
                LinkFromEntityName = "contact",
                LinkFromAttributeName = "contactid",
                Columns = new ColumnSet("ctx_name"),
            };
            var linkFilter = new FilterExpression(LogicalOperator.And);
            linkFilter.AddCondition(new ConditionExpression("ctx_name", ConditionOperator.Like, "Some%"));
            linkEntity.LinkCriteria = linkFilter;

            query.LinkEntities.Add(linkEntity);

            var res = orgAdminService.RetrieveMultiple(query).Entities;
            Assert.Equal(2, res.Count());
        }

        [Fact]
        public void TestQueryExpressionLinkEntityNotEqualExcludesNull()
        {
            // Migrated from lead -> ctx_parent: p1 (-> contact1) has ctx_postalcode = "MK111DW";
            // p2 (-> contact2) has none (null). A NotEqual link-criteria must exclude the contact whose
            // linked ctx_parent has a null value, matching real Dataverse (NULL <> value -> row excluded).
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("lastname")
            };
            query.Criteria = new FilterExpression(LogicalOperator.And);
            query.Criteria.AddCondition(new ConditionExpression("lastname", ConditionOperator.Like, "contact%"));

            var linkEntity = new LinkEntity()
            {
                LinkToEntityName = "ctx_parent",
                LinkToAttributeName = "ctx_contactid",
                LinkFromEntityName = "contact",
                LinkFromAttributeName = "contactid",
                Columns = new ColumnSet("ctx_postalcode"),
                EntityAlias = "ctxp"
            };
            linkEntity.LinkCriteria = new FilterExpression(LogicalOperator.And);
            linkEntity.LinkCriteria.AddCondition(new ConditionExpression("ctxp", "ctx_postalcode", ConditionOperator.NotEqual, "ZZZ"));
            query.LinkEntities.Add(linkEntity);

            var res = orgAdminService.RetrieveMultiple(query).Entities;
            Assert.Single(res);
            Assert.Equal("contact1", res[0].GetAttributeValue<string>("lastname"));
        }

        [Fact]
        public void TestQueryExpressionNotEqualExcludesNull()
        {
            // Migrated from lead -> ctx_parent: only p1 has a postal code; the others are null.
            // NotEqual must exclude the null rows.
            var query = new QueryExpression("ctx_parent") { ColumnSet = new ColumnSet(true) };
            query.Criteria.AddCondition(new ConditionExpression("ctx_postalcode", ConditionOperator.NotEqual, "ZZZ"));

            var res = orgAdminService.RetrieveMultiple(query).Entities.Cast<ctx_parent>().ToList();
            Assert.Single(res);
            Assert.Equal(p1.Id, res[0].Id);
        }

        [Fact]
        public void TestQueryExpressionNotInExcludesNull()
        {
            var query = new QueryExpression("ctx_parent") { ColumnSet = new ColumnSet(true) };
            query.Criteria.AddCondition(new ConditionExpression("ctx_postalcode", ConditionOperator.NotIn, new[] { "AAA", "BBB" }));

            var res = orgAdminService.RetrieveMultiple(query).Entities.Cast<ctx_parent>().ToList();
            Assert.Single(res);
            Assert.Equal(p1.Id, res[0].Id);
        }

        [Fact]
        public void TestQueryExpressionDoesNotBeginWithExcludesNull()
        {
            var query = new QueryExpression("ctx_parent") { ColumnSet = new ColumnSet(true) };
            query.Criteria.AddCondition(new ConditionExpression("ctx_postalcode", ConditionOperator.DoesNotBeginWith, "ZZ"));

            var res = orgAdminService.RetrieveMultiple(query).Entities.Cast<ctx_parent>().ToList();
            Assert.Single(res);
            Assert.Equal(p1.Id, res[0].Id);
        }

        [Fact]
        public void TestQueryExpressionLinkEntityNoSetEntityName()
        {
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("lastname")
            };
            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("lastname", ConditionOperator.Like, "contact%"));
            query.Criteria = filter;

            var linkEntity = new LinkEntity()
            {
                LinkToEntityName = "ctx_parent",
                LinkToAttributeName = "ctx_contactid",
                LinkFromEntityName = "contact",
                LinkFromAttributeName = "contactid",
                Columns = new ColumnSet("ctx_name"),
                EntityAlias = "ctx_parent"
            };
            var linkFilter = new FilterExpression(LogicalOperator.And);
            linkFilter.AddCondition(new ConditionExpression("ctx_name", ConditionOperator.Like, "Some%"));
            linkEntity.LinkCriteria = linkFilter;

            query.LinkEntities.Add(linkEntity);

            var res = orgAdminService.RetrieveMultiple(query).Entities;
            Assert.Equal(2, res.Count());
        }

        [Fact]
        public void RetrieveMultipleWithLinkEntitiesReturnDistinctResults()
        {
            var lead = new ctx_parent()
            {
                ctx_Name = "Lead",
                ctx_ContactId = contact1.ToEntityReference()
            };
            orgAdminService.Create(lead);

            var linkEntity = new LinkEntity
            {
                LinkToEntityName = "ctx_parent",
                LinkToAttributeName = "ctx_contactid",
                LinkFromEntityName = "contact",
                LinkFromAttributeName = "contactid",
                Columns = new ColumnSet(false),
                EntityAlias = "contact",
                JoinOperator = JoinOperator.LeftOuter,
            };

            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression("lastname", ConditionOperator.Equal, contact1.LastName));

            var query = new QueryExpression("contact")
            {
                Distinct = true,
                ColumnSet = new ColumnSet(),
                LinkEntities = { linkEntity }
            };

            var res = orgAdminService.RetrieveMultiple(query).Entities;

            var distinctIdCount = res.Select(x => x.Id).Distinct().Count();

            Assert.Equal(distinctIdCount, res.Count);
        }

        [Fact]
        public void RetrieveMultipleWithQueryByAttribute()
        {
            var result = orgAdminUIService.RetrieveMultiple(new QueryByAttribute
            {
                EntityName = Account.EntityLogicalName,
                Attributes = { "name" },
                Values = { account3.Name }
            });
            Assert.Single(result.Entities);
        }

        [Fact]
        public void RetrieveMultipleWithAliasedNullAttribute()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var result = (from lead in context.ctx_parentSet
                              join contact in context.ContactSet on lead.ctx_ContactId.Id equals contact.Id
                              select new { lead.ctx_Name, contact.FirstName, contact.LastName, contact.FullName })
                          .ToList();
                Assert.Equal(2, result.Count);
            }
        }

        [Fact]
        public void TestLINQContains()
        {
            var account = new Account()
            {
                Name = "NotIn",

            };
            account.Id = orgAdminService.Create(account);

            var searchstring = "a";
            using (var xrm = new Xrm(orgAdminService))
            {
                var query =
                    from c in xrm.AccountSet
                    where c.Name.Contains(searchstring)
                    select new
                    {
                        c.Name
                    };
                var queryList = query.ToList();
                Assert.True(queryList.Count > 0);
            }

            using (var xrm = new Xrm(orgAdminService))
            {
                var query =
                    from c in xrm.AccountSet
                    where !c.Name.Contains(searchstring)
                    select new
                    {
                        c.AccountId
                    };
                var queryNotA = query.FirstOrDefault();
                Assert.Equal(account.Id, queryNotA.AccountId);
            }
        }

        [Fact]
        public void RetrieveMultipleTotalRecordCount()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var query = new QueryExpression("account")
                {
                    ColumnSet = new ColumnSet(true),
                    PageInfo = new PagingInfo() { ReturnTotalRecordCount = true }
                };

                var res = orgAdminService.RetrieveMultiple(query);

                Assert.True(query.PageInfo.ReturnTotalRecordCount);
                Assert.Equal(4, res.TotalRecordCount);
            }
        }

        [Fact]
        public void RetrieveMultipleTotalRecordCountDefault()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var query = new QueryExpression("account")
                {
                    ColumnSet = new ColumnSet(true)
                };

                var res = orgAdminService.RetrieveMultiple(query);

                Assert.False(query.PageInfo.ReturnTotalRecordCount);
                Assert.Equal(-1, res.TotalRecordCount);
            }
        }

        [Fact]
        public void RetrieveMultipleBeginsWith()
        {
            var query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet(true)
            };
            query.Criteria.AddCondition("address1_postalcode", ConditionOperator.BeginsWith, "MK11");
            var res = orgAdminService.RetrieveMultiple(query);
            Assert.Single(res.Entities);

            query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet(true)
            };
            query.Criteria.AddCondition("address1_postalcode", ConditionOperator.DoesNotBeginWith, "MK11");
            res = orgAdminService.RetrieveMultiple(query);
            Assert.Equal(3, res.Entities.Count);

            query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet(true)
            };
            query.Criteria.AddCondition("address1_postalcode", ConditionOperator.EndsWith, "1DW");
            res = orgAdminService.RetrieveMultiple(query);
            Assert.Single(res.Entities);

            query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet(true)
            };
            query.Criteria.AddCondition("address1_postalcode", ConditionOperator.DoesNotEndWith, "1DW");
            res = orgAdminService.RetrieveMultiple(query);
            Assert.Equal(3, res.Entities.Count);
        }

        [Fact]
        public void TestCaseSensitivity()
        {
            var c = new Contact();
            c.FirstName = "MATT";
            orgAdminService.Create(c);
            var q = new QueryExpression("contact");
            q.Criteria.AddCondition("firstname", ConditionOperator.Equal, "matt");
            q.ColumnSet = new ColumnSet(true);
            var res = orgAdminService.RetrieveMultiple(q);

            Assert.Equal("MATT", res.Entities.Single().GetAttributeValue<string>("firstname"));
        }

        [Fact]
        public void TestRetrieveMultipleFailWithNonExistentAttribute()
        {
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("x")
            };

            Assert.Throws<AggregateException>(() => orgAdminService.RetrieveMultiple(query));
        }

        [Fact]
        public void TestRetrieveMultipleTopCount()
        {
            var query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("accountid"),
                TopCount = 1
            };

            var res = orgAdminService.RetrieveMultiple(query);

            Assert.Single(res.Entities);
        }

        [Fact]
        public void TestRetrieveMultipleTake()
        {
            using (var context = new Xrm(orgAdminService))
            {
                Assert.Single(context.AccountSet
                    .Select(a => a.AccountId)
                    .Take(1)
                    .ToList());
            }
        }

        [Fact]
        public void TestRetrieveMultipleSkip()
        {
            using (var context = new Xrm(orgAdminService))
            {
                Assert.Empty(context.AccountSet
                    .Select(a => a.AccountId)
                    .Skip(4)
                    .ToList());

                Assert.Single(context.AccountSet
                    .Select(a => a.AccountId)
                    .Skip(3)
                    .ToList());
            }
        }

        [Fact]
        public void TestColumnComparison()
        {
            var sameName = new Contact()
            {
                FirstName = "Some strange name",
                LastName = "Some strange name",
            };
            sameName.Id = orgAdminService.Create(sameName);

            var retrieved = Contact.Retrieve(orgAdminService, sameName.Id, x => x.FirstName, x => x.LastName);
            Assert.Equal(sameName.FirstName, retrieved.FirstName);

            using (var context = new Xrm(orgAdminService))
            {
                var query = new QueryExpression(Contact.EntityLogicalName)
                {
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression(
                                Contact.EntityLogicalName,
                                Contact.GetColumnName(x => x.FirstName),
                                ConditionOperator.Equal,
                                true,
                                Contact.GetColumnName(x => x.LastName)),
                        }
                    }
                };

                var res = orgAdminService.RetrieveMultiple(query);
                var entities = res.Entities.Select(x => x.ToEntity<Contact>()).ToList();
                Assert.Single(entities);
                Assert.Equal(sameName.Id, entities[0].Id);
            }
        }

        // Removed: TestFormulaFieldEvaluated. It tested dg_animal's cross-entity string-composition
        // calculated field (dg_AnimalOwner, referencing the owner's name) which ctx_parent can't
        // reproduce; PowerFx formula evaluation in RetrieveMultiple is already covered by
        // TestMoney.TestCalculatedIsSetRetrieveMultiple.

        [Fact]
        public void TestQueryExpressionEqualString()
        {
            // Test string equality
            var queryString = new QueryExpression("ctx_parent")
            {
                ColumnSet = new ColumnSet("ctx_name", "ctx_postalcode")
            };
            queryString.Criteria.AddCondition("ctx_postalcode", ConditionOperator.Equal, "MK111DW");
            var stringResult = orgAdminService.RetrieveMultiple(queryString);
            Assert.Single(stringResult.Entities);
            Assert.Equal(p1.Id, stringResult.Entities[0].Id);
        }

        [Fact]
        public void TestQueryExpressionEqualInt()
        {
            // Test int equality
            var queryInt = new QueryExpression("ctx_parent")
            {
                ColumnSet = new ColumnSet("ctx_parentid", "ctx_score")
            };
            queryInt.Criteria.AddCondition("ctx_score", ConditionOperator.Equal, 100);
            var intResult = orgAdminService.RetrieveMultiple(queryInt);
            Assert.Single(intResult.Entities);
            Assert.Equal(p4.Id, intResult.Entities[0].Id);
        }

        [Fact]
        public void TestQueryExpressionEqualGuid()
        {
            // Test Guid equality (using ctx_parentid)
            var queryGuid = new QueryExpression("ctx_parent")
            {
                ColumnSet = new ColumnSet("ctx_parentid", "ctx_name")
            };
            queryGuid.Criteria.AddCondition("ctx_parentid", ConditionOperator.Equal, p1.Id);
            var guidResult = orgAdminService.RetrieveMultiple(queryGuid);
            Assert.Single(guidResult.Entities);
            Assert.Equal(p1.Id, guidResult.Entities[0].Id);
        }

        [Fact]
        public void TestQueryExpressionDateTimeEqual()
        {
            // Test DateTime equality (using ctx_datevalue)
            var queryDateTime = new QueryExpression("ctx_parent")
            {
                ColumnSet = new ColumnSet("ctx_parentid", "ctx_name")
            };
            queryDateTime.Criteria.AddCondition("ctx_datevalue", ConditionOperator.Equal, new DateTime(2025, 9, 29, 7, 28, 0));
            var dateTimeResult = orgAdminService.RetrieveMultiple(queryDateTime);
            Assert.Single(dateTimeResult.Entities);
            Assert.Equal(p3.Id, dateTimeResult.Entities[0].Id);
        }

        [Fact]
        public void TestQueryExpressionEqualOptionSet()
        {
            // Test industry code (enum)
            var queryIndustry = new QueryExpression("ctx_parent")
            {
                ColumnSet = new ColumnSet("ctx_parentid", "ctx_industrycode")
            };
            queryIndustry.Criteria.AddCondition("ctx_industrycode", ConditionOperator.Equal, (int)ctx_parent_ctx_industrycode.Accounting);
            var industryResult = orgAdminService.RetrieveMultiple(queryIndustry);
            Assert.Single(industryResult.Entities);
            Assert.Equal(p1.Id, industryResult.Entities[0].Id);
        }

        [Fact]
        public void TestFetchXmlEqualString()
        {
            // Test string equality using FetchXML
            var fetchXml = @"<fetch>
                <entity name='ctx_parent'>
                    <attribute name='ctx_name' />
                    <attribute name='ctx_postalcode' />
                    <filter>
                        <condition attribute='ctx_postalcode' operator='eq' value='MK111DW' />
                    </filter>
                </entity>
            </fetch>";
            var stringResult = orgAdminService.RetrieveMultiple(new FetchExpression(fetchXml));
            Assert.Single(stringResult.Entities);
            Assert.Equal(p1.Id, stringResult.Entities[0].Id);
        }

        [Fact]
        public void TestFetchXmlEqualInt()
        {
            // Test int equality using FetchXML
            var fetchXml = @"<fetch>
                <entity name='ctx_parent'>
                    <attribute name='ctx_parentid' />
                    <attribute name='ctx_score' />
                    <filter>
                        <condition attribute='ctx_score' operator='eq' value='100' />
                    </filter>
                </entity>
            </fetch>";
            var intResult = orgAdminService.RetrieveMultiple(new FetchExpression(fetchXml));
            Assert.Single(intResult.Entities);
            Assert.Equal(p4.Id, intResult.Entities[0].Id);
        }

        [Fact]
        public void TestFetchXmlEqualGuid()
        {
            // Test Guid equality using FetchXML (using ctx_parentid)
            var fetchXml = $@"<fetch>
                <entity name='ctx_parent'>
                    <attribute name='ctx_parentid' />
                    <attribute name='ctx_name' />
                    <filter>
                        <condition attribute='ctx_parentid' operator='eq' value='{p1.Id}' />
                    </filter>
                </entity>
            </fetch>";
            var guidResult = orgAdminService.RetrieveMultiple(new FetchExpression(fetchXml));
            Assert.Single(guidResult.Entities);
            Assert.Equal(p1.Id, guidResult.Entities[0].Id);
        }

        [Fact]
        public void TestFetchXmlDateTimeEqual()
        {
            // Test DateTime equality using FetchXML (using ctx_datevalue)
            var dt = new DateTime(2025, 9, 29, 7, 28, 0, DateTimeKind.Local);
            var fetchXml = $@"<fetch>
                <entity name='ctx_parent'>
                    <attribute name='ctx_parentid' />
                    <attribute name='ctx_name' />
                    <filter>
                        <condition attribute='ctx_datevalue' operator='eq' value='{dt:O}' />
                    </filter>
                </entity>
            </fetch>";
            var dateTimeResult = orgAdminService.RetrieveMultiple(new FetchExpression(fetchXml));
            Assert.Single(dateTimeResult.Entities);
            Assert.Equal(p3.Id, dateTimeResult.Entities[0].Id);
        }

        [Fact]
        public void TestFetchXmlEqualOptionSet()
        {
            // Test industry code (enum) using FetchXML
            var fetchXml = $@"<fetch>
                <entity name='ctx_parent'>
                    <attribute name='ctx_parentid' />
                    <attribute name='ctx_industrycode' />
                    <filter>
                        <condition attribute='ctx_industrycode' operator='eq' value='{(int)ctx_parent_ctx_industrycode.Accounting}' />
                    </filter>
                </entity>
            </fetch>";
            var industryResult = orgAdminService.RetrieveMultiple(new FetchExpression(fetchXml));
            Assert.Single(industryResult.Entities);
            Assert.Equal(p1.Id, industryResult.Entities[0].Id);
        }

        [Fact]
        public void TestRetrieveMultipleFullNameAfterCreate()
        {
            // Arrange: Create a Contact with firstname and lastname
            var contact = new Contact
            {
                FirstName = "John",
                LastName = "Doe"
            };
            contact.Id = orgAdminService.Create(contact);

            // Act: Use RetrieveMultiple with QueryExpression to get the contact
            var query = new QueryExpression(Contact.EntityLogicalName)
            {
                ColumnSet = new ColumnSet("firstname", "lastname", "fullname")
            };
            query.Criteria.AddCondition("contactid", ConditionOperator.Equal, contact.Id);

            var result = orgAdminService.RetrieveMultiple(query);

            // Assert: fullname should be "John Doe"
            Assert.Single(result.Entities);
            var retrievedContact = result.Entities[0].ToEntity<Contact>();
            Assert.Equal("John Doe", retrievedContact.FullName);
        }

        [Fact]
        public void TestRetrieveMultipleFullNameAfterPartialUpdateFirstName()
        {
            // Arrange: Create a Contact with firstname and lastname
            var contact = new Contact
            {
                FirstName = "John",
                LastName = "Doe"
            };
            contact.Id = orgAdminService.Create(contact);

            // Act: Update ONLY firstname (create a new Entity with just the Id and firstname, no lastname)
            var updateContact = new Contact(contact.Id)
            {
                FirstName = "Jane"
            };
            orgAdminService.Update(updateContact);

            // Use RetrieveMultiple to get the contact
            var query = new QueryExpression(Contact.EntityLogicalName)
            {
                ColumnSet = new ColumnSet("firstname", "lastname", "fullname")
            };
            query.Criteria.AddCondition("contactid", ConditionOperator.Equal, contact.Id);

            var result = orgAdminService.RetrieveMultiple(query);

            // Assert: fullname should be "Jane Doe" (not just "Jane")
            Assert.Single(result.Entities);
            var retrievedContact = result.Entities[0].ToEntity<Contact>();
            Assert.Equal("Jane", retrievedContact.FirstName);
            Assert.Equal("Doe", retrievedContact.LastName);
            Assert.Equal("Jane Doe", retrievedContact.FullName);
        }

        [Fact]
        public void TestRetrieveMultipleFullNameAfterPartialUpdateLastName()
        {
            // Arrange: Create a Contact with firstname and lastname
            var contact = new Contact
            {
                FirstName = "John",
                LastName = "Doe"
            };
            contact.Id = orgAdminService.Create(contact);

            // Act: Update ONLY lastname (create a new Entity with just the Id and lastname, no firstname)
            var updateContact = new Contact(contact.Id)
            {
                LastName = "Smith"
            };
            orgAdminService.Update(updateContact);

            // Use RetrieveMultiple to get the contact
            var query = new QueryExpression(Contact.EntityLogicalName)
            {
                ColumnSet = new ColumnSet("firstname", "lastname", "fullname")
            };
            query.Criteria.AddCondition("contactid", ConditionOperator.Equal, contact.Id);

            var result = orgAdminService.RetrieveMultiple(query);

            // Assert: fullname should be "John Smith"
            Assert.Single(result.Entities);
            var retrievedContact = result.Entities[0].ToEntity<Contact>();
            Assert.Equal("John", retrievedContact.FirstName);
            Assert.Equal("Smith", retrievedContact.LastName);
            Assert.Equal("John Smith", retrievedContact.FullName);
        }

        [Fact]
        public void TestRetrieveMultipleFullNameAfterPartialUpdateOtherField()
        {
            // Arrange: Create a Contact with firstname and lastname
            var contact = new Contact
            {
                FirstName = "John",
                LastName = "Doe"
            };
            contact.Id = orgAdminService.Create(contact);

            // Act: Update ONLY lastname (create a new Entity with just the Id and lastname, no firstname)
            var updateContact = new Contact(contact.Id)
            {
                Description = "Updated description"
            };
            orgAdminService.Update(updateContact);

            // Use RetrieveMultiple to get the contact
            var query = new QueryExpression(Contact.EntityLogicalName)
            {
                ColumnSet = new ColumnSet("fullname")
            };
            query.Criteria.AddCondition("contactid", ConditionOperator.Equal, contact.Id);

            var result = orgAdminService.RetrieveMultiple(query);

            // Assert: fullname should be "John Smith"
            Assert.Single(result.Entities);
            var retrievedContact = result.Entities[0].ToEntity<Contact>();
            Assert.Equal("John Doe", retrievedContact.FullName);
        }

        [Fact]
        public void TestRetrieveMultipleEntityReferenceNamePopulated()
        {
            // Arrange: Create entities with a lookup relationship
            var parentAccount = new Account
            {
                Name = "Parent Company ABC"
            };
            parentAccount.Id = orgAdminService.Create(parentAccount);

            var childAccount = new Account
            {
                Name = "Child Company",
                ParentAccountId = parentAccount.ToEntityReference()
            };
            childAccount.Id = orgAdminService.Create(childAccount);

            // Act: Use RetrieveMultiple with QueryExpression to get the child account
            var query = new QueryExpression(Account.EntityLogicalName)
            {
                ColumnSet = new ColumnSet("name", "parentaccountid")
            };
            query.Criteria.AddCondition("accountid", ConditionOperator.Equal, childAccount.Id);

            var result = orgAdminService.RetrieveMultiple(query);

            // Assert: EntityReference.Name should be populated
            Assert.Single(result.Entities);
            var retrievedAccount = result.Entities[0].ToEntity<Account>();
            Assert.NotNull(retrievedAccount.ParentAccountId);
            Assert.Equal(parentAccount.Id, retrievedAccount.ParentAccountId.Id);
            Assert.Equal("Parent Company ABC", retrievedAccount.ParentAccountId.Name);
        }

        [Fact]
        public void TestRetrieveMultipleEntityReferenceNamePopulatedWithLinkEntities()
        {
            // Arrange: Create entities with relationships
            var referencedAccount = new Account
            {
                Name = "Referenced Account XYZ"
            };
            referencedAccount.Id = orgAdminService.Create(referencedAccount);

            var testContact = new Contact
            {
                FirstName = "Test",
                LastName = "Person",
                ParentCustomerId = referencedAccount.ToEntityReference()
            };
            testContact.Id = orgAdminService.Create(testContact);

            var testLead = new ctx_parent
            {
                ctx_Name = "Test Lead for LinkEntity",
                ctx_ContactId = testContact.ToEntityReference()
            };
            testLead.Id = orgAdminService.Create(testLead);

            // Act: Use RetrieveMultiple with LinkEntity
            var query = new QueryExpression(ctx_parent.EntityLogicalName)
            {
                ColumnSet = new ColumnSet("ctx_name", "ctx_contactid")
            };
            query.Criteria.AddCondition("ctx_parentid", ConditionOperator.Equal, testLead.Id);

            var linkEntity = new LinkEntity
            {
                LinkFromEntityName = ctx_parent.EntityLogicalName,
                LinkFromAttributeName = "ctx_contactid",
                LinkToEntityName = Contact.EntityLogicalName,
                LinkToAttributeName = "contactid",
                JoinOperator = JoinOperator.Inner,
                Columns = new ColumnSet(false),
                EntityAlias = "linkedContact"
            };
            query.LinkEntities.Add(linkEntity);

            var result = orgAdminService.RetrieveMultiple(query);

            // Assert: EntityReference.Name should be populated on the main entity's lookup field
            Assert.Single(result.Entities);
            var retrievedLead = result.Entities[0].ToEntity<ctx_parent>();
            Assert.NotNull(retrievedLead.ctx_ContactId);
            Assert.Equal(testContact.Id, retrievedLead.ctx_ContactId.Id);
            Assert.Equal("Test Person", retrievedLead.ctx_ContactId.Name);
        }

        [Fact]
        public void TestRetrieveMultipleAliasedEntityReferenceNamePopulated()
        {
            // Arrange: Create entities with relationships where the linked entity has a lookup field
            var referencedAccount = new Account
            {
                Name = "Ultimate Parent Account"
            };
            referencedAccount.Id = orgAdminService.Create(referencedAccount);

            var testContact = new Contact
            {
                FirstName = "Aliased",
                LastName = "Contact",
                ParentCustomerId = referencedAccount.ToEntityReference()
            };
            testContact.Id = orgAdminService.Create(testContact);

            var testLead = new ctx_parent
            {
                ctx_Name = "Lead for Aliased Test",
                ctx_ContactId = testContact.ToEntityReference()
            };
            testLead.Id = orgAdminService.Create(testLead);

            // Act: Use RetrieveMultiple with LinkEntity and include the lookup column from the linked entity
            var query = new QueryExpression(ctx_parent.EntityLogicalName)
            {
                ColumnSet = new ColumnSet("ctx_name")
            };
            query.Criteria.AddCondition("ctx_parentid", ConditionOperator.Equal, testLead.Id);

            var linkEntity = new LinkEntity
            {
                LinkFromEntityName = ctx_parent.EntityLogicalName,
                LinkFromAttributeName = "ctx_contactid",
                LinkToEntityName = Contact.EntityLogicalName,
                LinkToAttributeName = "contactid",
                JoinOperator = JoinOperator.Inner,
                Columns = new ColumnSet("parentcustomerid"),
                EntityAlias = "linkedContact"
            };
            query.LinkEntities.Add(linkEntity);

            var result = orgAdminService.RetrieveMultiple(query);

            // Assert: EntityReference.Name inside AliasedValue should be populated
            Assert.Single(result.Entities);
            var retrievedEntity = result.Entities[0];

            // Get the aliased value for the linked entity's lookup field
            var aliasedValue = retrievedEntity.GetAttributeValue<AliasedValue>("linkedContact.parentcustomerid");
            Assert.NotNull(aliasedValue);
            Assert.IsType<EntityReference>(aliasedValue.Value);

            var aliasedEntityRef = (EntityReference)aliasedValue.Value;
            Assert.Equal(referencedAccount.Id, aliasedEntityRef.Id);
            Assert.Equal("Ultimate Parent Account", aliasedEntityRef.Name);
        }

        [Fact]
        public void TestRetrieveMultipleEntityReferenceNamePopulatedUsingLinq()
        {
            // Arrange: Create entities with lookup relationship
            var parentAccount = new Account
            {
                Name = "LINQ Test Parent Account"
            };
            parentAccount.Id = orgAdminService.Create(parentAccount);

            var childAccount = new Account
            {
                Name = "LINQ Test Child Account",
                ParentAccountId = parentAccount.ToEntityReference()
            };
            childAccount.Id = orgAdminService.Create(childAccount);

            // Act: Use LINQ query to retrieve the child account
            using (var context = new Xrm(orgAdminService))
            {
                var query = from acc in context.AccountSet
                            where acc.AccountId == childAccount.Id
                            select new { acc.Name, acc.ParentAccountId };

                var results = query.ToList();

                // Assert: EntityReference.Name should be populated
                Assert.Single(results);
                var retrievedAccount = results[0];
                Assert.NotNull(retrievedAccount.ParentAccountId);
                Assert.Equal(parentAccount.Id, retrievedAccount.ParentAccountId.Id);
                Assert.Equal("LINQ Test Parent Account", retrievedAccount.ParentAccountId.Name);
            }
        }

        [Fact]
        public void TestRetrieveMultipleEntityReferenceNamePopulatedWithFetchXml()
        {
            // Arrange: Create entities with lookup relationship
            var parentAccount = new Account
            {
                Name = "FetchXml Parent Account"
            };
            parentAccount.Id = orgAdminService.Create(parentAccount);

            var childAccount = new Account
            {
                Name = "FetchXml Child Account",
                ParentAccountId = parentAccount.ToEntityReference()
            };
            childAccount.Id = orgAdminService.Create(childAccount);

            // Act: Use FetchXML to retrieve the child account
            var fetchXml = $@"<fetch>
                <entity name='account'>
                    <attribute name='name' />
                    <attribute name='parentaccountid' />
                    <filter>
                        <condition attribute='accountid' operator='eq' value='{childAccount.Id}' />
                    </filter>
                </entity>
            </fetch>";

            var result = orgAdminService.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert: EntityReference.Name should be populated
            Assert.Single(result.Entities);
            var retrievedAccount = result.Entities[0].ToEntity<Account>();
            Assert.NotNull(retrievedAccount.ParentAccountId);
            Assert.Equal(parentAccount.Id, retrievedAccount.ParentAccountId.Id);
            Assert.Equal("FetchXml Parent Account", retrievedAccount.ParentAccountId.Name);
        }

        [Fact]
        public void TestRetrieveMultipleFilterOnInvalidAttributeFails()
        {
            var query = new QueryExpression(Account.EntityLogicalName)
            {
                ColumnSet = new ColumnSet("name")
            };
            query.Criteria.AddCondition("nonexistentattribute", ConditionOperator.Equal, "whatever");

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(
                () => orgAdminService.RetrieveMultiple(query));
            Assert.Equal(
                "'Account' entity doesn't contain attribute with Name = 'nonexistentattribute' and NameMapping = 'Logical' (look up attribute by name is case-sensitive)",
                ex.Message);
        }

        [Fact]
        public void TestRetrieveMultipleFilterAttributeLookupIsCaseSensitive()
        {
            // "Name" (capitalised) is not the logical name; the lookup must be case-sensitive and fault.
            var query = new QueryExpression(Account.EntityLogicalName)
            {
                ColumnSet = new ColumnSet("name")
            };
            query.Criteria.AddCondition("Name", ConditionOperator.Equal, "account1");

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(
                () => orgAdminService.RetrieveMultiple(query));
            Assert.Equal(
                "'Account' entity doesn't contain attribute with Name = 'Name' and NameMapping = 'Logical' (look up attribute by name is case-sensitive)",
                ex.Message);
        }

        [Fact]
        public void TestRetrieveMultipleFilterOnValidAttributeSucceeds()
        {
            var account = new Account { Name = "ValidAttributeFilterAccount" };
            account.Id = orgAdminService.Create(account);

            var query = new QueryExpression(Account.EntityLogicalName)
            {
                ColumnSet = new ColumnSet("name")
            };
            query.Criteria.AddCondition("name", ConditionOperator.Equal, "ValidAttributeFilterAccount");

            var result = orgAdminService.RetrieveMultiple(query);
            Assert.Single(result.Entities);
        }

        [Fact]
        public void TestRetrieveMultipleFetchXmlFilterOnInvalidAttributeFails()
        {
            var fetchXml = @"<fetch>
                <entity name='account'>
                    <attribute name='name' />
                    <filter>
                        <condition attribute='nonexistentattribute' operator='eq' value='x' />
                    </filter>
                </entity>
            </fetch>";

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(
                () => orgAdminService.RetrieveMultiple(new FetchExpression(fetchXml)));
            Assert.Equal(
                "'Account' entity doesn't contain attribute with Name = 'nonexistentattribute' and NameMapping = 'Logical' (look up attribute by name is case-sensitive)",
                ex.Message);
        }

        [Fact]
        public void TestRetrieveMultipleLinkEntityFilterOnInvalidAttributeFails()
        {
            var query = new QueryExpression(Account.EntityLogicalName)
            {
                ColumnSet = new ColumnSet("name")
            };
            var link = query.AddLink(Contact.EntityLogicalName, "primarycontactid", "contactid");
            link.LinkCriteria.AddCondition("nonexistentattribute", ConditionOperator.Equal, "x");

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(
                () => orgAdminService.RetrieveMultiple(query));
            Assert.Equal(
                "'Contact' entity doesn't contain attribute with Name = 'nonexistentattribute' and NameMapping = 'Logical' (look up attribute by name is case-sensitive)",
                ex.Message);
        }

        [Fact]
        public void TestRetrieveMultipleLinkEntityWithEmptyAliasFilterOnInvalidAttributeFails()
        {
            // An explicit empty alias is not replaced by alias-filling; the link criteria must still
            // be validated against the linked entity's metadata.
            var query = new QueryExpression(Account.EntityLogicalName)
            {
                ColumnSet = new ColumnSet("name")
            };
            var link = query.AddLink(Contact.EntityLogicalName, "primarycontactid", "contactid");
            link.EntityAlias = "";
            link.LinkCriteria.AddCondition("nonexistentattribute", ConditionOperator.Equal, "x");

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(
                () => orgAdminService.RetrieveMultiple(query));
            Assert.Equal(
                "'Contact' entity doesn't contain attribute with Name = 'nonexistentattribute' and NameMapping = 'Logical' (look up attribute by name is case-sensitive)",
                ex.Message);
        }
    }
}