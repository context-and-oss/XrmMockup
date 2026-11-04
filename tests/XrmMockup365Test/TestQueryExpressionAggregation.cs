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
    }
}
