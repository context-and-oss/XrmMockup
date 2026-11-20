using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.ServiceModel;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestQueryExpressionAggregation : UnitTestBase
    {
        private readonly Guid[] accountIds;

        public TestQueryExpressionAggregation(XrmMockupFixture fixture) : base(fixture)
        {
            accountIds = new[]
            {
                orgAdminService.Create(new Account { Name = "Aggregate A", Address1_City = "A", NumberOfEmployees = 10, Revenue = 10 }),
                orgAdminService.Create(new Account { Name = "Aggregate B", Address1_City = "A", NumberOfEmployees = 20, Revenue = 20 }),
                orgAdminService.Create(new Account { Name = "Aggregate C", Address1_City = "B" })
            };
        }

        private QueryExpression CreateQuery(string attribute, string alias, XrmAggregateType type)
        {
            var query = new QueryExpression(Account.EntityLogicalName) { ColumnSet = new ColumnSet(false) };
            query.ColumnSet.AttributeExpressions.Add(new XrmAttributeExpression
            {
                AttributeName = attribute,
                Alias = alias,
                AggregateType = type
            });
            query.Criteria.AddCondition("accountid", ConditionOperator.In, accountIds.Cast<object>().ToArray());
            return query;
        }

        [Fact]
        public void CountReturnsOnlyAnAliasedValueAfterFiltering()
        {
            var query = CreateQuery("statuscode", "Count", XrmAggregateType.Count);
            query.Criteria.AddCondition("statuscode", ConditionOperator.Equal, 1);
            query.Criteria.AddCondition("address1_city", ConditionOperator.Equal, "A");

            var result = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);

            Assert.Equal(Guid.Empty, result.Id);
            var attribute = Assert.Single(result.Attributes);
            Assert.Equal("Count", attribute.Key);
            var value = Assert.IsType<AliasedValue>(attribute.Value);
            Assert.Equal(Account.EntityLogicalName, value.EntityLogicalName);
            Assert.Equal("statuscode", value.AttributeLogicalName);
            Assert.Equal(2, Assert.IsType<int>(value.Value));
        }

        [Theory]
        [InlineData(XrmAggregateType.Count)]
        [InlineData(XrmAggregateType.CountColumn)]
        public void EmptyInputReturnsZero(XrmAggregateType type)
        {
            var query = CreateQuery("numberofemployees", "Count", type);
            query.Criteria.AddCondition("accountid", ConditionOperator.Equal, Guid.NewGuid());

            var result = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);

            Assert.Single(result.Attributes);
            Assert.Equal(0, result.GetAttributeValue<AliasedValue>("Count").Value);
        }

        [Fact]
        public void CountCountsRowsWhileCountColumnExcludesNulls()
        {
            var query = CreateQuery("numberofemployees", "Rows", XrmAggregateType.Count);
            query.ColumnSet.AttributeExpressions.Add(new XrmAttributeExpression
            {
                AttributeName = "numberofemployees", Alias = "Values", AggregateType = XrmAggregateType.CountColumn
            });

            var result = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);

            Assert.Equal(2, result.Attributes.Count);
            Assert.Equal(3, result.GetAttributeValue<AliasedValue>("Rows").Value);
            Assert.Equal(2, result.GetAttributeValue<AliasedValue>("Values").Value);
        }

        [Theory]
        [InlineData(XrmAggregateType.Sum, 30)]
        [InlineData(XrmAggregateType.Avg, 15)]
        [InlineData(XrmAggregateType.Min, 10)]
        [InlineData(XrmAggregateType.Max, 20)]
        public void NumericAggregatesExcludeNulls(XrmAggregateType type, int expected)
        {
            var query = CreateQuery("numberofemployees", "Value", type);
            var result = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);

            Assert.Single(result.Attributes);
            Assert.Equal(expected, Convert.ToDecimal(result.GetAttributeValue<AliasedValue>("Value").Value));
        }

        [Theory]
        [InlineData(XrmAggregateType.Sum, 30)]
        [InlineData(XrmAggregateType.Avg, 15)]
        [InlineData(XrmAggregateType.Min, 10)]
        [InlineData(XrmAggregateType.Max, 20)]
        public void MoneyAggregatesPreserveMoneyType(XrmAggregateType type, int expected)
        {
            var query = CreateQuery("revenue", "Value", type);
            var result = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);

            Assert.Equal(expected, Assert.IsType<Money>(result.GetAttributeValue<AliasedValue>("Value").Value).Value);
        }

        [Fact]
        public void TopCountLimitsAggregateResultsNotInputRows()
        {
            var query = CreateQuery("statuscode", "Count", XrmAggregateType.Count);
            query.TopCount = 1;

            var result = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);

            Assert.Equal(3, result.GetAttributeValue<AliasedValue>("Count").Value);
        }

        [Fact]
        public void GroupingAndAliasOrderingAreAppliedBeforeTopCount()
        {
            var query = CreateQuery("statuscode", "Count", XrmAggregateType.Count);
            query.ColumnSet.AttributeExpressions.Add(new XrmAttributeExpression
            {
                AttributeName = "address1_city", Alias = "City", HasGroupBy = true
            });
            query.Orders.Add(new OrderExpression("address1_city", OrderType.Descending) { Alias = "City" });
            query.TopCount = 1;

            var result = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);

            Assert.Equal(2, result.Attributes.Count);
            Assert.Equal("B", result.GetAttributeValue<AliasedValue>("City").Value);
            Assert.Equal(1, result.GetAttributeValue<AliasedValue>("Count").Value);
        }

        [Fact]
        public void LinkedAttributeCanBeAggregatedWithoutReturningLinkedRows()
        {
            orgAdminService.Create(new Contact { LastName = "Aggregate contact A", ParentCustomerId = new EntityReference(Account.EntityLogicalName, accountIds[0]) });
            orgAdminService.Create(new Contact { LastName = "Aggregate contact B", ParentCustomerId = new EntityReference(Account.EntityLogicalName, accountIds[0]) });
            var query = CreateQuery("statuscode", "Accounts", XrmAggregateType.Count);
            var link = query.AddLink(Contact.EntityLogicalName, "accountid", "parentcustomerid");
            link.EntityAlias = "contacts";
            link.Columns.AttributeExpressions.Add(new XrmAttributeExpression
            {
                AttributeName = "contactid", Alias = "Contacts", AggregateType = XrmAggregateType.Count
            });

            var result = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);

            Assert.Equal(2, result.Attributes.Count);
            Assert.Equal(2, result.GetAttributeValue<AliasedValue>("Contacts").Value);
            Assert.Equal(Contact.EntityLogicalName, result.GetAttributeValue<AliasedValue>("Contacts").EntityLogicalName);
        }

        [Fact]
        public void CountHonorsReadPermissions()
        {
            var regularQuery = new QueryExpression(Account.EntityLogicalName) { ColumnSet = new ColumnSet(false) };
            regularQuery.Criteria.AddCondition("accountid", ConditionOperator.In, accountIds.Cast<object>().ToArray());
            var visibleCount = testUser1Service.RetrieveMultiple(regularQuery).Entities.Count;
            var aggregateQuery = CreateQuery("statuscode", "Count", XrmAggregateType.Count);

            var result = Assert.Single(testUser1Service.RetrieveMultiple(aggregateQuery).Entities);

            Assert.Equal(visibleCount, result.GetAttributeValue<AliasedValue>("Count").Value);
        }

        [Fact]
        public void LinkedOnlyAggregateIsRecognized()
        {
            orgAdminService.Create(new Contact { LastName = "Aggregate contact", ParentCustomerId = new EntityReference(Account.EntityLogicalName, accountIds[0]) });
            var query = CreateQuery("statuscode", "Unused", XrmAggregateType.Count);
            query.ColumnSet.AttributeExpressions.Clear();
            var link = query.AddLink(Contact.EntityLogicalName, "accountid", "parentcustomerid");
            link.Columns.AttributeExpressions.Add(new XrmAttributeExpression
            {
                AttributeName = "contactid", Alias = "Count", AggregateType = XrmAggregateType.Count
            });

            var result = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);

            Assert.Single(result.Attributes);
            Assert.Equal(1, result.GetAttributeValue<AliasedValue>("Count").Value);
        }

        [Fact]
        public void GroupedEmptyInputReturnsNoRows()
        {
            var query = CreateQuery("statuscode", "Count", XrmAggregateType.Count);
            query.ColumnSet.AttributeExpressions.Add(new XrmAttributeExpression
            {
                AttributeName = "address1_city", Alias = "City", HasGroupBy = true
            });
            query.Criteria.AddCondition("accountid", ConditionOperator.Equal, Guid.NewGuid());

            Assert.Empty(orgAdminService.RetrieveMultiple(query).Entities);
        }

        [Fact]
        public void PagingLimitDoesNotLimitCountInput()
        {
            var query = CreateQuery("statuscode", "Count", XrmAggregateType.Count);
            query.PageInfo.Count = 1;
            query.PageInfo.ReturnTotalRecordCount = true;

            var collection = orgAdminService.RetrieveMultiple(query);
            var result = Assert.Single(collection.Entities);

            Assert.Equal(3, result.GetAttributeValue<AliasedValue>("Count").Value);
            Assert.Equal(1, collection.TotalRecordCount);
        }

        [Fact]
        public void AveragePreservesFractionalValues()
        {
            orgAdminService.Update(new Account(accountIds[1]) { NumberOfEmployees = 21 });
            var query = CreateQuery("numberofemployees", "Average", XrmAggregateType.Avg);

            var result = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);

            Assert.Equal(15.5m, result.GetAttributeValue<AliasedValue>("Average").Value);
        }

        [Theory]
        [InlineData("ctx_amountcalcclassic")]
        [InlineData("ctx_amountcalc")]
        public void ComputedAttributesAreEvaluatedBeforeAggregation(string attribute)
        {
            var first = orgAdminService.Create(new ctx_parent { ctx_Name = "Aggregate parent A", ctx_Amount = 30 });
            var second = orgAdminService.Create(new ctx_parent { ctx_Name = "Aggregate parent B", ctx_Amount = 20 });
            var query = new QueryExpression(ctx_parent.EntityLogicalName) { ColumnSet = new ColumnSet(false) };
            query.Criteria.AddCondition("ctx_parentid", ConditionOperator.In, first, second);
            query.ColumnSet.AttributeExpressions.Add(new XrmAttributeExpression
            {
                AttributeName = attribute, Alias = "Sum", AggregateType = XrmAggregateType.Sum
            });

            var result = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);
            var value = result.GetAttributeValue<AliasedValue>("Sum").Value;

            Assert.Equal(1000m, value is Money money ? money.Value : Assert.IsType<decimal>(value));
        }

        [Fact]
        public void DuplicateAliasesAreRejected()
        {
            var query = CreateQuery("statuscode", "Count", XrmAggregateType.Count);
            query.ColumnSet.AttributeExpressions.Add(new XrmAttributeExpression
            {
                AttributeName = "numberofemployees", Alias = "Count", AggregateType = XrmAggregateType.CountColumn
            });

            Assert.Throws<FaultException>(() => orgAdminService.RetrieveMultiple(query));
        }

        [Fact]
        public void AllColumnsCannotBeUsedInAggregateQueries()
        {
            var query = CreateQuery("statuscode", "Count", XrmAggregateType.Count);
            query.ColumnSet.AllColumns = true;

            Assert.Throws<FaultException>(() => orgAdminService.RetrieveMultiple(query));
        }

        [Theory]
        [InlineData(XrmAggregateType.Count, 0)]
        [InlineData(XrmAggregateType.Sum, 0)]
        [InlineData(XrmAggregateType.Count, 1)]
        [InlineData(XrmAggregateType.Sum, 1)]
        [InlineData(XrmAggregateType.Count, 2)]
        [InlineData(XrmAggregateType.Sum, 2)]
        public void InvalidExpressionAttributesAreRejectedEvenWithNoMatchingRows(XrmAggregateType type, int depth)
        {
            // "revenue" is valid on the primary account, but not either linked entity.
            var query = CreateQuery(depth == 0 ? "missingattribute" : "revenue", "Value", type);
            if (depth > 0)
            {
                var expression = query.ColumnSet.AttributeExpressions.Single();
                query.ColumnSet.AttributeExpressions.Clear();
                var link = query.AddLink(Contact.EntityLogicalName, "accountid", "parentcustomerid");
                if (depth == 2) link = link.AddLink(ctx_parent.EntityLogicalName, "contactid", "ctx_contactid");
                link.Columns.AttributeExpressions.Add(expression);
            }
            query.Criteria.AddCondition("accountid", ConditionOperator.Equal, Guid.NewGuid());

            var exception = Assert.Throws<FaultException<OrganizationServiceFault>>(() => orgAdminService.RetrieveMultiple(query));

            Assert.Contains(depth == 0 ? "missingattribute" : "revenue", exception.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("account.revenue")]
        [InlineData("Revenue")]
        public void ExpressionAttributesMustBeValidLogicalNames(string attribute)
        {
            var query = CreateQuery(attribute, "Count", XrmAggregateType.Count);

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => orgAdminService.RetrieveMultiple(query));
        }

        [Fact]
        public void InvalidGroupByAttributeIsRejected()
        {
            var query = CreateQuery("statuscode", "Count", XrmAggregateType.Count);
            query.ColumnSet.AttributeExpressions.Add(new XrmAttributeExpression
            {
                AttributeName = "missingattribute", Alias = "Group", HasGroupBy = true
            });

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => orgAdminService.RetrieveMultiple(query));
        }

        [Theory]
        [InlineData(false, OrderType.Ascending)]
        [InlineData(false, OrderType.Descending)]
        [InlineData(true, OrderType.Ascending)]
        [InlineData(true, OrderType.Descending)]
        public void LookupGroupsHaveNamesAndAreOrderedByNameBeforeTopCount(bool linked, OrderType order)
        {
            // GUID ordering deliberately disagrees with name ordering.
            var alpha = new Account(new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff")) { Name = "Alpha" };
            var zulu = new Account(new Guid("00000001-0000-0000-0000-000000000000")) { Name = "Zulu" };
            orgAdminService.Create(alpha);
            orgAdminService.Create(zulu);
            var first = orgAdminService.Create(new Contact { LastName = "Alpha contact", ParentCustomerId = alpha.ToEntityReference() });
            var second = orgAdminService.Create(new Contact { LastName = "Zulu contact", ParentCustomerId = zulu.ToEntityReference() });
            var query = new QueryExpression(linked ? Account.EntityLogicalName : Contact.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(false), TopCount = 1
            };
            ColumnSet columns;
            if (linked)
            {
                query.Criteria.AddCondition("accountid", ConditionOperator.In, alpha.Id, zulu.Id);
                columns = query.AddLink(Contact.EntityLogicalName, "accountid", "parentcustomerid").Columns;
            }
            else
            {
                query.Criteria.AddCondition("contactid", ConditionOperator.In, first, second);
                columns = query.ColumnSet;
            }
            columns.AttributeExpressions.Add(new XrmAttributeExpression
            {
                AttributeName = "contactid", Alias = "Count", AggregateType = XrmAggregateType.Count
            });
            columns.AttributeExpressions.Add(new XrmAttributeExpression
            {
                AttributeName = "parentcustomerid", Alias = "Customer", HasGroupBy = true
            });
            query.Orders.Add(new OrderExpression("parentcustomerid", order) { Alias = "Customer" });

            var result = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);
            var customer = Assert.IsType<EntityReference>(result.GetAttributeValue<AliasedValue>("Customer").Value);

            Assert.Equal(order == OrderType.Ascending ? alpha.Id : zulu.Id, customer.Id);
            Assert.Equal(order == OrderType.Ascending ? "Alpha" : "Zulu", customer.Name);
        }

        [Fact]
        public void StringGroupKeysAreCaseInsensitive()
        {
            orgAdminService.Update(new Account(accountIds[0]) { Address1_City = "London" });
            orgAdminService.Update(new Account(accountIds[1]) { Address1_City = "london" });
            orgAdminService.Update(new Account(accountIds[2]) { Address1_City = "LONDON" });
            var query = CreateQuery("statuscode", "Count", XrmAggregateType.Count);
            query.ColumnSet.AttributeExpressions.Add(new XrmAttributeExpression
            {
                AttributeName = "address1_city", Alias = "City", HasGroupBy = true
            });
            // Also exercise composite keys, with a null component shared by all accounts.
            query.ColumnSet.AttributeExpressions.Add(new XrmAttributeExpression
            {
                AttributeName = "address2_city", Alias = "OtherCity", HasGroupBy = true
            });

            var result = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);

            Assert.Equal(3, result.GetAttributeValue<AliasedValue>("Count").Value);
            Assert.Equal("LONDON", Assert.IsType<string>(result.GetAttributeValue<AliasedValue>("City").Value).ToUpperInvariant());
        }

        private QueryExpression CreateLinkedParentQuery(string attribute, XrmAggregateType type, bool nested,
            EntityReference secondCurrency = null)
        {
            var query = CreateQuery("statuscode", "Unused", XrmAggregateType.Count);
            query.ColumnSet.AttributeExpressions.Clear();
            LinkEntity link;
            EntityReference contact = null;
            if (nested)
            {
                var id = orgAdminService.Create(new Contact { LastName = "Aggregate parent contact", ParentCustomerId = new EntityReference(Account.EntityLogicalName, accountIds[0]) });
                contact = new EntityReference(Contact.EntityLogicalName, id);
                link = query.AddLink(Contact.EntityLogicalName, "accountid", "parentcustomerid")
                    .AddLink(ctx_parent.EntityLogicalName, "contactid", "ctx_contactid");
            }
            else
            {
                link = query.AddLink(ctx_parent.EntityLogicalName, "accountid", "ctx_accountid");
            }
            var first = orgAdminService.Create(new ctx_parent
            {
                ctx_Name = "First aggregate parent", ctx_Amount = 30,
                ctx_AccountId = new EntityReference(Account.EntityLogicalName, accountIds[0]), ctx_ContactId = contact
            });
            var second = orgAdminService.Create(new ctx_parent
            {
                ctx_Name = "Second aggregate parent", ctx_Amount = 20,
                ctx_AccountId = new EntityReference(Account.EntityLogicalName, accountIds[0]), ctx_ContactId = contact,
                TransactionCurrencyId = secondCurrency
            });
            link.LinkCriteria.AddCondition("ctx_parentid", ConditionOperator.In, first, second);
            link.Columns.AttributeExpressions.Add(new XrmAttributeExpression
            {
                AttributeName = attribute, Alias = "Value", AggregateType = type
            });
            return query;
        }

        [Theory]
        [InlineData("ctx_amountcalcclassic", XrmAggregateType.Sum, false)]
        [InlineData("ctx_amountcalcclassic", XrmAggregateType.CountColumn, false)]
        [InlineData("ctx_amountcalc", XrmAggregateType.Sum, false)]
        [InlineData("ctx_amountcalc", XrmAggregateType.CountColumn, false)]
        [InlineData("ctx_amountcalcclassic", XrmAggregateType.Sum, true)]
        [InlineData("ctx_amountcalcclassic", XrmAggregateType.CountColumn, true)]
        [InlineData("ctx_amountcalc", XrmAggregateType.Sum, true)]
        [InlineData("ctx_amountcalc", XrmAggregateType.CountColumn, true)]
        public void LinkedComputedColumnsAreEvaluatedBeforeAggregation(string attribute, XrmAggregateType type, bool nested)
        {
            var query = CreateLinkedParentQuery(attribute, type, nested);

            var result = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);
            var value = result.GetAttributeValue<AliasedValue>("Value").Value;

            Assert.Equal(type == XrmAggregateType.CountColumn ? 2m : 1000m,
                value is Money money ? money.Value : Convert.ToDecimal(value));
        }

        private TransactionCurrency CreateHalfRateCurrency()
        {
            var currency = new TransactionCurrency { ExchangeRate = 0.5m, CurrencyPrecision = 2 };
            currency.Id = orgAdminService.Create(currency);
            return currency;
        }

        [Theory]
        [InlineData(XrmAggregateType.Sum, 35)]
        [InlineData(XrmAggregateType.Avg, 17.5)]
        [InlineData(XrmAggregateType.Min, 15)]
        [InlineData(XrmAggregateType.Max, 20)]
        public void MoneyAggregatesUseBaseCurrencyAndHistoricalRates(XrmAggregateType type, double expected)
        {
            var currency = CreateHalfRateCurrency();
            orgAdminService.Update(new Account(accountIds[0]) { Revenue = 15 });
            orgAdminService.Update(new Account(accountIds[1]) { Revenue = 10, TransactionCurrencyId = currency.ToEntityReference() });
            // Updating the currency itself must not change the historical value on the account.
            orgAdminService.Update(new TransactionCurrency(currency.Id) { ExchangeRate = 0.25m });
            var query = CreateQuery("revenue", "Value", type);

            var result = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);

            Assert.Equal((decimal)expected, Assert.IsType<Money>(result.GetAttributeValue<AliasedValue>("Value").Value).Value);
        }

        [Fact]
        public void ExplicitBaseCurrencyColumnIsNotConvertedTwice()
        {
            var currency = CreateHalfRateCurrency();
            orgAdminService.Update(new Account(accountIds[0]) { Revenue = 10 });
            orgAdminService.Update(new Account(accountIds[1]) { Revenue = 10, TransactionCurrencyId = currency.ToEntityReference() });
            var query = CreateQuery("revenue_base", "Value", XrmAggregateType.Sum);

            var result = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);

            Assert.Equal(30m, Assert.IsType<Money>(result.GetAttributeValue<AliasedValue>("Value").Value).Value);
        }

        [Theory]
        [InlineData(XrmAggregateType.Sum, 70, false)]
        [InlineData(XrmAggregateType.Avg, 35, false)]
        [InlineData(XrmAggregateType.Min, 30, false)]
        [InlineData(XrmAggregateType.Max, 40, false)]
        [InlineData(XrmAggregateType.Sum, 70, true)]
        [InlineData(XrmAggregateType.Avg, 35, true)]
        [InlineData(XrmAggregateType.Min, 30, true)]
        [InlineData(XrmAggregateType.Max, 40, true)]
        public void LinkedMoneyAggregatesUseTheLinkedRecordsCurrency(XrmAggregateType type, int expected, bool nested)
        {
            var currency = CreateHalfRateCurrency();
            var query = CreateLinkedParentQuery("ctx_amount", type, nested, currency.ToEntityReference());

            var result = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);

            Assert.Equal(expected, Assert.IsType<Money>(result.GetAttributeValue<AliasedValue>("Value").Value).Value);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LinkedComputedMoneyUsesTheLinkedExchangeRate(bool nested)
        {
            var currency = CreateHalfRateCurrency();
            var query = CreateLinkedParentQuery("ctx_amountcalcclassic", XrmAggregateType.Sum, nested, currency.ToEntityReference());

            var result = Assert.Single(orgAdminService.RetrieveMultiple(query).Entities);

            // Normalize the computed values actually projected by the classic workflow engine.
            var link = query.LinkEntities.Single();
            if (nested) link = link.LinkEntities.Single();
            var expected = link.LinkCriteria.Conditions.Single().Values.Cast<Guid>()
                .Select(id => orgAdminService.Retrieve(ctx_parent.EntityLogicalName, id,
                    new ColumnSet("ctx_amountcalcclassic", "exchangerate")))
                .Sum(parent => parent.GetAttributeValue<Money>("ctx_amountcalcclassic").Value /
                    parent.GetAttributeValue<decimal>("exchangerate"));
            Assert.Equal(expected, Assert.IsType<Money>(result.GetAttributeValue<AliasedValue>("Value").Value).Value);
        }
    }
}
