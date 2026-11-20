using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.ServiceModel;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestAlternateKeys : UnitTestBase
    {
        public TestAlternateKeys(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestAlternateKeysAll()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var attributes = new AttributeCollection {
                    { "name", "Burgers" },
                    { "address1_city", "Virum" }
                };
                orgAdminUIService.Create(new Account { Attributes = attributes });

                var keyAttributes = new KeyAttributeCollection {
                    { "name", "Burgers" },
                    { "address1_city", "Virum" }
                };
                var req = new RetrieveRequest
                {
                    Target = new EntityReference
                    {
                        LogicalName = Account.EntityLogicalName,
                        KeyAttributes = keyAttributes
                    }
                };

                var resp = orgAdminUIService.Execute(req) as RetrieveResponse;
                var entity = resp.Entity as Account;
                Assert.Equal("Burgers", entity.Name);
                Assert.Equal("Virum", entity.Address1_City);

                var newAttributes = new AttributeCollection {
                    { "name", "Toast" }
                };
                orgAdminUIService.Update(new Account { KeyAttributes = keyAttributes, Attributes = newAttributes });

                keyAttributes["name"] = "Toast";

                req = new RetrieveRequest
                {
                    Target = new EntityReference
                    {
                        LogicalName = Account.EntityLogicalName,
                        KeyAttributes = keyAttributes
                    }
                };
                resp = orgAdminUIService.Execute(req) as RetrieveResponse;
                var updatedEntity = resp.Entity as Account;
                Assert.Equal("Toast", updatedEntity.Name);
                Assert.Equal("Virum", updatedEntity.Address1_City);
            }
        }

        [Fact]
        public void TestAlternateKeyWithEntityReference()
        {
            var parentA = Create(new ctx_parent { ctx_Name = "Parent A" });
            var parentB = Create(new ctx_parent { ctx_Name = "Parent B" });

            // Same name, different parents.
            var childA = Create(new ctx_child
            {
                ctx_Name = "Meatballs",
                ctx_ParentId = parentA.ToEntityReference()
            });
            Create(new ctx_child
            {
                ctx_Name = "Meatballs",
                ctx_ParentId = parentB.ToEntityReference()
            });

            var req = new RetrieveRequest
            {
                Target = new EntityReference
                {
                    LogicalName = ctx_child.EntityLogicalName,
                    KeyAttributes = new KeyAttributeCollection
                    {
                        { "ctx_name",     "Meatballs" },
                        { "ctx_parentid", parentA.ToEntityReference() }
                    }
                },
                ColumnSet = new ColumnSet(true)
            };

            var retrieved = (orgAdminUIService.Execute(req) as RetrieveResponse).Entity as ctx_child;

            Assert.Equal(childA.Id, retrieved.Id);
            Assert.Equal(parentA.Id, retrieved.ctx_ParentId.Id);
        }

        [Fact]
        public void TestAlternateKeyWithGuidForLookup()
        {
            // Bare Guid works too. No logical name on it, so it matches on id alone.
            var parentA = Create(new ctx_parent { ctx_Name = "Parent A" });
            var parentB = Create(new ctx_parent { ctx_Name = "Parent B" });

            var childA = Create(new ctx_child
            {
                ctx_Name = "Meatballs",
                ctx_ParentId = parentA.ToEntityReference()
            });
            Create(new ctx_child
            {
                ctx_Name = "Meatballs",
                ctx_ParentId = parentB.ToEntityReference()
            });

            var req = new RetrieveRequest
            {
                Target = new EntityReference
                {
                    LogicalName = ctx_child.EntityLogicalName,
                    KeyAttributes = new KeyAttributeCollection
                    {
                        { "ctx_name",     "Meatballs" },
                        { "ctx_parentid", parentA.Id }
                    }
                },
                ColumnSet = new ColumnSet(true)
            };

            var retrieved = (orgAdminUIService.Execute(req) as RetrieveResponse).Entity as ctx_child;

            Assert.Equal(childA.Id, retrieved.Id);
            Assert.Equal(parentA.Id, retrieved.ctx_ParentId.Id);
        }

        [Fact]
        public void TestAlternateKeyWithString()
        {
            // Built at runtime, not an interned literal
            var wanted = 2800.ToString();

            Create(new ctx_parent { ctx_Name = "Alpha", ctx_Postalcode = "2830" });
            var target = Create(new ctx_parent { ctx_Name = "Alpha", ctx_Postalcode = "2800" });

            var req = new RetrieveRequest
            {
                Target = new EntityReference
                {
                    LogicalName = ctx_parent.EntityLogicalName,
                    KeyAttributes = new KeyAttributeCollection
                    {
                        { "ctx_name",       "Alpha" },
                        { "ctx_postalcode", wanted }
                    }
                },
                ColumnSet = new ColumnSet(true)
            };

            var retrieved = (orgAdminUIService.Execute(req) as RetrieveResponse).Entity as ctx_parent;

            Assert.Equal(target.Id, retrieved.Id);
            Assert.Equal("2800", retrieved.ctx_Postalcode);
        }

        [Fact]
        public void TestAlternateKeyWithWholeNumber()
        {
            Create(new ctx_parent { ctx_Name = "Counted", ctx_WholeNumber = 1 });
            var target = Create(new ctx_parent { ctx_Name = "Counted", ctx_WholeNumber = 4242 });

            var req = new RetrieveRequest
            {
                Target = new EntityReference
                {
                    LogicalName = ctx_parent.EntityLogicalName,
                    KeyAttributes = new KeyAttributeCollection
                    {
                        { "ctx_name",        "Counted" },
                        { "ctx_wholenumber", 4242 }
                    }
                },
                ColumnSet = new ColumnSet(true)
            };

            var retrieved = (orgAdminUIService.Execute(req) as RetrieveResponse).Entity as ctx_parent;

            Assert.Equal(target.Id, retrieved.Id);
            Assert.Equal(4242, retrieved.ctx_WholeNumber);
        }

        [Fact]
        public void TestAlternateKeyWithDecimal()
        {
            // Only writable Decimal column in the test metadata
            Create(new TransactionCurrency
            {
                CurrencyName = "Altkey A",
                CurrencySymbol = "A",
                ISOCurrencyCode = "AAA",
                ExchangeRate = 1.5m
            });
            var target = Create(new TransactionCurrency
            {
                CurrencyName = "Altkey B",
                CurrencySymbol = "B",
                ISOCurrencyCode = "BBB",
                ExchangeRate = 2.25m
            });

            var req = new RetrieveRequest
            {
                Target = new EntityReference
                {
                    LogicalName = TransactionCurrency.EntityLogicalName,
                    KeyAttributes = new KeyAttributeCollection { { "exchangerate", 2.25m } }
                },
                ColumnSet = new ColumnSet(true)
            };

            var retrieved = (orgAdminUIService.Execute(req) as RetrieveResponse).Entity as TransactionCurrency;

            Assert.Equal(target.Id, retrieved.Id);
            Assert.Equal(2.25m, retrieved.ExchangeRate);
        }

        [Fact]
        public void TestAlternateKeyWithDateTime()
        {
            var other = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var wanted = new DateTime(2026, 8, 28, 9, 30, 0, DateTimeKind.Utc);

            Create(new ctx_parent { ctx_Name = "Dated", ctx_DateValue = other });
            var target = Create(new ctx_parent { ctx_Name = "Dated", ctx_DateValue = wanted });

            var req = new RetrieveRequest
            {
                Target = new EntityReference
                {
                    LogicalName = ctx_parent.EntityLogicalName,
                    KeyAttributes = new KeyAttributeCollection
                    {
                        { "ctx_name",      "Dated" },
                        { "ctx_datevalue", wanted }
                    }
                },
                ColumnSet = new ColumnSet(true)
            };

            var retrieved = (orgAdminUIService.Execute(req) as RetrieveResponse).Entity as ctx_parent;

            Assert.Equal(target.Id, retrieved.Id);
            Assert.Equal(wanted, retrieved.ctx_DateValue);
        }

        [Fact]
        public void TestAlternateKeyWithOptionSet()
        {
            Create(new ctx_parent { ctx_Name = "Sorted", ctx_Industrycode = ctx_parent_ctx_industrycode.Accounting });
            var target = Create(new ctx_parent { ctx_Name = "Sorted", ctx_Industrycode = ctx_parent_ctx_industrycode.Consulting });

            var req = new RetrieveRequest
            {
                Target = new EntityReference
                {
                    LogicalName = ctx_parent.EntityLogicalName,
                    KeyAttributes = new KeyAttributeCollection
                    {
                        { "ctx_name",         "Sorted" },
                        { "ctx_industrycode", new OptionSetValue((int)ctx_parent_ctx_industrycode.Consulting) }
                    }
                },
                ColumnSet = new ColumnSet(true)
            };

            var byOptionSetValue = (orgAdminUIService.Execute(req) as RetrieveResponse).Entity as ctx_parent;

            Assert.Equal(target.Id, byOptionSetValue.Id);
            Assert.Equal(ctx_parent_ctx_industrycode.Consulting, byOptionSetValue.ctx_Industrycode);

            // Raw int works too, that is how it is stored
            req = new RetrieveRequest
            {
                Target = new EntityReference
                {
                    LogicalName = ctx_parent.EntityLogicalName,
                    KeyAttributes = new KeyAttributeCollection
                    {
                        { "ctx_name",         "Sorted" },
                        { "ctx_industrycode", (int)ctx_parent_ctx_industrycode.Consulting }
                    }
                },
                ColumnSet = new ColumnSet(true)
            };

            var byInt = (orgAdminUIService.Execute(req) as RetrieveResponse).Entity as ctx_parent;

            Assert.Equal(target.Id, byInt.Id);

            // Early-bound enum works too
            req = new RetrieveRequest
            {
                Target = new EntityReference
                {
                    LogicalName = ctx_parent.EntityLogicalName,
                    KeyAttributes = new KeyAttributeCollection
                    {
                        { "ctx_name",         "Sorted" },
                        { "ctx_industrycode", ctx_parent_ctx_industrycode.Consulting }
                    }
                },
                ColumnSet = new ColumnSet(true)
            };

            var byEnum = (orgAdminUIService.Execute(req) as RetrieveResponse).Entity as ctx_parent;

            Assert.Equal(target.Id, byEnum.Id);
        }

        [Fact]
        public void TestAlternateKeyStringIsCaseInsensitive()
        {
            // Text columns collate case-insensitively
            Create(new ctx_parent { ctx_Name = "Meatballs", ctx_Postalcode = "2830" });
            var target = Create(new ctx_parent { ctx_Name = "Meatballs", ctx_Postalcode = "2800" });

            var req = new RetrieveRequest
            {
                Target = new EntityReference
                {
                    LogicalName = ctx_parent.EntityLogicalName,
                    KeyAttributes = new KeyAttributeCollection
                    {
                        { "ctx_name",       "MEATBALLS" },
                        { "ctx_postalcode", "2800" }
                    }
                },
                ColumnSet = new ColumnSet(true)
            };

            var retrieved = (orgAdminUIService.Execute(req) as RetrieveResponse).Entity as ctx_parent;

            Assert.Equal(target.Id, retrieved.Id);

            // Same value, same result, through a query.
            var query = new QueryExpression(ctx_parent.EntityLogicalName) { ColumnSet = new ColumnSet(true) };
            query.Criteria.AddCondition("ctx_name", ConditionOperator.Equal, "MEATBALLS");
            query.Criteria.AddCondition("ctx_postalcode", ConditionOperator.Equal, "2800");

            var queried = orgAdminUIService.RetrieveMultiple(query).Entities;

            Assert.Single(queried);
            Assert.Equal(target.Id, queried[0].Id);
        }

        [Fact]
        public void TestAlternateKeyWithNullValueNeverMatches()
        {
            // Both rows leave ctx_postalcode unset, null would match an arbitrary one
            var first = Create(new ctx_parent { ctx_Name = "Nulled" });
            var second = Create(new ctx_parent { ctx_Name = "Nulled" });

            var target = new EntityReference
            {
                LogicalName = ctx_parent.EntityLogicalName,
                KeyAttributes = new KeyAttributeCollection
                {
                    { "ctx_name",       "Nulled" },
                    { "ctx_postalcode", null }
                }
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(new RetrieveRequest
            {
                Target = target,
                ColumnSet = new ColumnSet(true)
            }));

            // And nothing is silently updated instead
            Assert.Throws<FaultException>(() => orgAdminUIService.Update(new ctx_parent
            {
                KeyAttributes = target.KeyAttributes,
                ctx_Postalcode = "2800"
            }));

            Assert.Null(orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, first.Id, new ColumnSet(true)).ToEntity<ctx_parent>().ctx_Postalcode);
            Assert.Null(orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, second.Id, new ColumnSet(true)).ToEntity<ctx_parent>().ctx_Postalcode);
        }

        [Fact]
        public void TestAlternateKeysUpdateOnly()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var attributes = new AttributeCollection {
                    { "name", "Burgers" },
                    { "address1_city", "Virum" }
                };
                orgAdminUIService.Create(new Account { Attributes = attributes });

                var keyAttributes = new KeyAttributeCollection {
                    { "name", "Burgers" },
                    { "address1_city", "Virum" }
                };
                var req = new RetrieveRequest
                {
                    Target = new EntityReference
                    {
                        LogicalName = Account.EntityLogicalName,
                        KeyAttributes = keyAttributes
                    }
                };

                var resp = orgAdminUIService.Execute(req) as RetrieveResponse;
                
                var newAttributes = new AttributeCollection {
                    { "address1_line1", "Some street" }
                };
                orgAdminUIService.Update(new Account { KeyAttributes = keyAttributes, Attributes = newAttributes });

                req = new RetrieveRequest
                {
                    Target = new EntityReference
                    {
                        LogicalName = Account.EntityLogicalName,
                        KeyAttributes = keyAttributes
                    }
                };
                resp = orgAdminUIService.Execute(req) as RetrieveResponse;
                var updatedEntity = resp.Entity as Account;
                Assert.Equal("Some street", updatedEntity.Address1_Line1);
            }
        }

        [Fact]
        public void TestAlternateKeyUpsertResolvesExistingRecord()
        {
            var existing = Create(new ctx_parent { ctx_Name = "Upserted", ctx_WholeNumber = 4242 });

            var resp = orgAdminUIService.Execute(new UpsertRequest
            {
                Target = new ctx_parent
                {
                    KeyAttributes = new KeyAttributeCollection
                    {
                        { "ctx_name",        "Upserted" },
                        { "ctx_wholenumber", 4242 }
                    },
                    ctx_Postalcode = "2800"
                }
            }) as UpsertResponse;

            Assert.False(resp.RecordCreated);
            Assert.Equal(existing.Id, resp.Target.Id);
            Assert.Equal("2800", orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, existing.Id, new ColumnSet(true))
                .ToEntity<ctx_parent>().ctx_Postalcode);

            // Key matching nothing still creates
            resp = orgAdminUIService.Execute(new UpsertRequest
            {
                Target = new ctx_parent
                {
                    KeyAttributes = new KeyAttributeCollection
                    {
                        { "ctx_name",        "Upserted" },
                        { "ctx_wholenumber", 1 }
                    }
                }
            }) as UpsertResponse;

            Assert.True(resp.RecordCreated);
            Assert.NotEqual(existing.Id, resp.Target.Id);
        }

        // Migrated from Account.Retrieve_dg_name -> ctx_parent.Retrieve_ctx_NameKey. The provisioner
        // creates the ctx_NameKey alternate key (on ctx_name), so XrmContext generates this
        // retrieve-by-key helper. Verifies a record can be retrieved via its alternate key.
        [Fact]
        public void AltKeyRetrieveWithoutEntityTypeInDb()
        {
            var created = new ctx_parent { ctx_Name = "woop" };
            created.Id = orgAdminUIService.Create(created);

            var y = ctx_parent.Retrieve_ctx_NameKey(orgAdminUIService, "woop", x => x.ctx_Name);
            Assert.NotNull(y);
            Assert.Equal(created.Id, y.Id);
            Assert.Equal("woop", y.ctx_Name);
        }
    }
}
