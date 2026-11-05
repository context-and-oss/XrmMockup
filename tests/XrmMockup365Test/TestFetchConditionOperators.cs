using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
namespace DG.XrmMockupTest
{
    public class TestFetchConditionOperators : UnitTestBase
    {
        public TestFetchConditionOperators(XrmMockupFixture fixture) : base(fixture)
        {
            CreateAccount(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item1 });
            CreateAccount(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item2 });
            CreateAccount(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item3 });
            CreateAccount(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item4 });
            CreateAccount(new List<Account_dg_Multiselect> { Account_dg_Multiselect.checkmark });
            CreateAccount(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item });
            CreateAccount(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item1, Account_dg_Multiselect.Item2 });
            CreateAccount(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item1, Account_dg_Multiselect.Item3 });
            CreateAccount(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item1, Account_dg_Multiselect.Item4 });
            CreateAccount(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item2, Account_dg_Multiselect.Item3 });
            CreateAccount(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item2, Account_dg_Multiselect.Item4 });
            CreateAccount(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item3, Account_dg_Multiselect.Item4 });
            CreateAccount(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item1, Account_dg_Multiselect.Item2, Account_dg_Multiselect.Item3 });
            CreateAccount(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item1, Account_dg_Multiselect.Item2, Account_dg_Multiselect.Item4 });
            CreateAccount(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item1, Account_dg_Multiselect.Item3, Account_dg_Multiselect.Item4 });
            CreateAccount(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item2, Account_dg_Multiselect.Item3, Account_dg_Multiselect.Item4 });
            CreateAccount(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item1, Account_dg_Multiselect.Item2, Account_dg_Multiselect.Item3, Account_dg_Multiselect.Item4 });

            void CreateAccount(List<Account_dg_Multiselect> list)
            {
                for (int i = 0; i < 2; i++)
                {
                    list.Reverse();
                    var account = new Account();
                    account.dg_Multiselect = list;
                    account.Id = orgAdminUIService.Create(account);
                }
            }
        }

        private class MultiselectCases : TheoryData<List<Account_dg_Multiselect>>
        {
            public MultiselectCases()
            {
                Add(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item1 });
                Add(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item2 });
                Add(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item3 });
                Add(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item4 });
                Add(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item1, Account_dg_Multiselect.Item2 });
                Add(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item1, Account_dg_Multiselect.Item3 });
                Add(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item1, Account_dg_Multiselect.Item4 });
                Add(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item2, Account_dg_Multiselect.Item3 });
                Add(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item2, Account_dg_Multiselect.Item4 });
                Add(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item3, Account_dg_Multiselect.Item4 });
                Add(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item1, Account_dg_Multiselect.Item2, Account_dg_Multiselect.Item3 });
                Add(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item1, Account_dg_Multiselect.Item2, Account_dg_Multiselect.Item4 });
                Add(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item1, Account_dg_Multiselect.Item3, Account_dg_Multiselect.Item4 });
                Add(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item2, Account_dg_Multiselect.Item3, Account_dg_Multiselect.Item4 });
                Add(new List<Account_dg_Multiselect> { Account_dg_Multiselect.Item1, Account_dg_Multiselect.Item2, Account_dg_Multiselect.Item3, Account_dg_Multiselect.Item4 });
            }
        }

        [Theory]
        [ClassData(typeof(MultiselectCases))]
        public void TestMultiselectConditionOperatorIn(List<Account_dg_Multiselect> optionSetValueCollection)
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var optionSetValues = optionSetValueCollection.Select(_ => (int)_).ToList();
                for (int i = 0; i < 2; i++)
                {
                    optionSetValues.Reverse();
                    string values = string.Empty;
                    foreach (var value in optionSetValues)
                    {
                        values += $"<value>{value}</value>";
                    }

                    var fetchXml =
                    $@"<fetch mapping='logical' version='1.0'>
                        <entity name='account'>
                            <filter>
                                <condition attribute='dg_multiselect' operator='in'>
                                    {values}
                                </condition>
                            </filter>
                        </entity>
                    </fetch>";
                    EntityCollection result = Fetch(fetchXml);
                    Assert.Equal(2, result.Entities.Count);
                }
            }
        }

        [Theory]
        [ClassData(typeof(MultiselectCases))]
        public void TestMultiselectConditionOperatorNotIn(List<Account_dg_Multiselect> optionSetValueCollection)
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchXmlCount =
                    $@"<fetch mapping='logical' version='1.0'>
                        <entity name='account'>
                            <attribute name='name' />
                        </entity>
                    </fetch>";
                var count = Fetch(fetchXmlCount).Entities.Count;
                var optionSetValues = optionSetValueCollection.Select(_ => (int)_).ToList();
                for (int i = 0; i < 2; i++)
                {
                    optionSetValues.Reverse();
                    string values = string.Empty;
                    foreach (var value in optionSetValues)
                    {
                        values += $"<value>{value}</value>";
                    }

                    var fetchXml =
                    $@"<fetch mapping='logical' version='1.0'>
                        <entity name='account'>
                            <filter>
                                <condition attribute='dg_multiselect' operator='not-in'>
                                    {values}
                                </condition>
                            </filter>
                        </entity>
                    </fetch>";
                    EntityCollection result = Fetch(fetchXml);
                    Assert.Equal(count - 2, result.Entities.Count);
                }
            }
        }

        [Theory]
        [InlineData("olderthan-x-years", 365 * 1440, 1, true)]
        [InlineData("olderthan-x-years", 365 * 1440, 3, false)]
        [InlineData("olderthan-x-months", 30 * 1440, 1, true)]
        [InlineData("olderthan-x-months", 30 * 1440, 3, false)]
        [InlineData("olderthan-x-weeks", 7 * 1440, 1, true)]
        [InlineData("olderthan-x-weeks", 7 * 1440, 3, false)]
        [InlineData("olderthan-x-days", 1440, 1, true)]
        [InlineData("olderthan-x-days", 1440, 3, false)]
        [InlineData("olderthan-x-hours", 60, 1, true)]
        [InlineData("olderthan-x-hours", 60, 3, false)]
        [InlineData("olderthan-x-minutes", 1, 1, true)]
        [InlineData("olderthan-x-minutes", 1, 3, false)]
        public void TestFetchConditionOperatorsTheoryOlderThanX(string conditionOperator, int minutes, int x, bool hasHit)
        {
            // Migrated from Opportunity.EstimatedCloseDate -> Account.LastOnHoldTime (Opportunity removed);
            // this exercises generic FetchXML date condition operators on any settable datetime field.
            orgAdminUIService.Create(
                new Account()
                {
                    LastOnHoldTime = DateTime.UtcNow.AddMinutes(-(2 * minutes))
                });

            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchXml1 =
                    $@"<fetch mapping='logical' version='1.0'>
                        <entity name='account'>
                            <filter>
                                <condition attribute='lastonholdtime' operator='{conditionOperator}' value='{x}' />
                            </filter>
                        </entity>
                    </fetch>";
                EntityCollection result1 = Fetch(fetchXml1);
                if (hasHit)
                {
                    Assert.Single(result1.Entities);
                }
                else
                {
                    Assert.Empty(result1.Entities);
                }
            }
        }

        [Theory]
        [InlineData("yesterday", 0, false)]
        [InlineData("yesterday", -1, true)]
        [InlineData("yesterday", -2, false)]
        [InlineData("today", -1, false)]
        [InlineData("today", 0, true)]
        [InlineData("today", 1, false)]
        [InlineData("tomorrow", 0, false)]
        [InlineData("tomorrow", 1, true)]
        [InlineData("tomorrow", 2, false)]
        public void TestFetchConditionOperatorsTheoryYesterdayTodayTomorrowX(string conditionOperator, int days, bool hasHit)
        {
            // Migrated from Opportunity.EstimatedCloseDate -> Account.LastOnHoldTime (Opportunity removed).
            orgAdminUIService.Create(
                new Account()
                {
                    LastOnHoldTime = DateTime.UtcNow.AddDays(days)
                });

            using (var context = new Xrm(orgAdminUIService))
            {
                var fetchXml1 =
                    $@"<fetch mapping='logical' version='1.0'>
                        <entity name='account'>
                            <filter>
                                <condition attribute='lastonholdtime' operator='{conditionOperator}' />
                            </filter>
                        </entity>
                    </fetch>";
                EntityCollection result1 = Fetch(fetchXml1);
                if (hasHit)
                {
                    Assert.Single(result1.Entities);
                }
                else
                {
                    Assert.Empty(result1.Entities);
                }
            }
        }

        private EntityCollection Fetch(string fetchXml2)
        {
            var conversionResponse2 = (FetchXmlToQueryExpressionResponse)orgAdminUIService.Execute(new FetchXmlToQueryExpressionRequest { FetchXml = fetchXml2 });
            QueryExpression queryExpression2 = conversionResponse2.Query;
            EntityCollection result2 = orgAdminUIService.RetrieveMultiple(queryExpression2);
            return result2;
        }
    }
}
