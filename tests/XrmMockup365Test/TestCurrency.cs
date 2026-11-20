using System;
using System.Linq;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;
using Xunit;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    // Migrated from the custom dg_bus entity to Account (dg_bus removed from environment):
    // dg_Ticketprice -> CreditLimit and dg_ticketprice_Base -> CreditLimit_Base (account's built-in
    // money field + its currency base field), dg_busState -> account_statecode. This preserves full coverage
    // of currency base-value calculation, exchange-rate handling and user/default currency selection.
    public class TestCurrency : UnitTestBase
    {
        public TestCurrency(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestBasesGetUpdated()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var currency = new TransactionCurrency()
                {
                    ExchangeRate = 0.5m,
                    CurrencyPrecision = 2
                };
                currency.Id = orgAdminUIService.Create(currency);

                // test currency gets set
                var bus = new Account
                {
                    TransactionCurrencyId = currency.ToEntityReference()
                };
                bus.Id = orgAdminUIService.Create(bus);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, bus.Id, new ColumnSet(true)) as Account;
                Assert.Equal(bus.TransactionCurrencyId, retrieved.TransactionCurrencyId);

                // test base value gets updated
                bus.CreditLimit = 100m;
                orgAdminUIService.Update(bus);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, bus.Id, new ColumnSet(true)) as Account;
                Assert.Equal(Math.Round(bus.CreditLimit.Value / currency.ExchangeRate.Value, currency.CurrencyPrecision.Value), retrieved.CreditLimit_Base);

                // test currency doesn't update before record gets updated
                var oldExchangeRate = currency.ExchangeRate;
                currency.ExchangeRate = 0.7m;
                orgAdminUIService.Update(currency);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, bus.Id, new ColumnSet(true)) as Account;
                Assert.Equal(Math.Round(bus.CreditLimit.Value / oldExchangeRate.Value, currency.CurrencyPrecision.Value), retrieved.CreditLimit_Base);

                // test base value gets updated when record field value changes
                bus.CreditLimit = 120m;
                orgAdminUIService.Update(bus);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, bus.Id, new ColumnSet(true)) as Account;
                Assert.Equal(Math.Round(bus.CreditLimit.Value / currency.ExchangeRate.Value, currency.CurrencyPrecision.Value), retrieved.CreditLimit_Base);

                // test base value gets updated when transactioncurrencyid changes
                var newCurrency = new TransactionCurrency
                {
                    ExchangeRate = 1m,
                    CurrencyPrecision = 3
                };
                newCurrency.Id = orgAdminUIService.Create(newCurrency);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, bus.Id, new ColumnSet(true)) as Account;
                Assert.Equal(Math.Round(bus.CreditLimit.Value / currency.ExchangeRate.Value, currency.CurrencyPrecision.Value), retrieved.CreditLimit_Base);

                bus.TransactionCurrencyId = newCurrency.ToEntityReference();
                orgAdminUIService.Update(bus);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, bus.Id, new ColumnSet(true)) as Account;
                Assert.Equal(Math.Round(bus.CreditLimit.Value / newCurrency.ExchangeRate.Value, newCurrency.CurrencyPrecision.Value), retrieved.CreditLimit_Base);

                // test base value gets updated when state of record changes
                oldExchangeRate = newCurrency.ExchangeRate;
                newCurrency.ExchangeRate = 0.3m;
                orgAdminUIService.Update(newCurrency);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, bus.Id, new ColumnSet(true)) as Account;
                Assert.Equal(Math.Round(bus.CreditLimit.Value / oldExchangeRate.Value, newCurrency.CurrencyPrecision.Value), retrieved.CreditLimit_Base);

                bus.SetState(orgAdminUIService, account_statecode.Inactive);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, bus.Id, new ColumnSet(true)) as Account;
                Assert.Equal(Math.Round(bus.CreditLimit.Value / newCurrency.ExchangeRate.Value, newCurrency.CurrencyPrecision.Value), retrieved.CreditLimit_Base);
            }
        }

        [Fact]
        public void TestExchangeRate()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var dollar = new TransactionCurrency
                {
                    ExchangeRate = 0.6m,
                    CurrencyPrecision = 3
                };
                var dollarId = orgAdminUIService.Create(dollar);
                dollar.Id = dollarId;

                var bus = new Account
                {
                    CreditLimit = 10m,
                    TransactionCurrencyId = dollar.ToEntityReference()
                };
                var busId = orgAdminUIService.Create(bus);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, busId, new ColumnSet(true)) as Account;
                Assert.True(retrieved.CreditLimit_Base.HasValue);
                Assert.Equal(Math.Round(bus.CreditLimit.Value / dollar.ExchangeRate.Value, dollar.CurrencyPrecision.Value), retrieved.CreditLimit_Base.Value);
            }
        }

        [Fact]
        public void TestExchangeIsSet()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var dollar = new TransactionCurrency
                {
                    ExchangeRate = 0.6m,
                    CurrencyPrecision = 3
                };
                var dollarId = orgAdminUIService.Create(dollar);
                dollar.Id = dollarId;

                var bus = new Account
                {
                    CreditLimit = 10m,
                    TransactionCurrencyId = dollar.ToEntityReference()
                };
                var busId = orgAdminUIService.Create(bus);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, busId, new ColumnSet(true)) as Account;
                Assert.True(retrieved.ExchangeRate.HasValue);
                Assert.Equal(dollar.ExchangeRate, retrieved.ExchangeRate);
            }
        }

        [Fact]
        public void TestSettingTransactionCurrency()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var bus = new Account();
                bus.Id = orgAdminUIService.Create(bus);
                var query = new QueryExpression(TransactionCurrency.EntityLogicalName)
                {
                    Criteria = new FilterExpression()
                };
                query.Criteria.AddCondition(new ConditionExpression("isocurrencycode", ConditionOperator.Equal, "DKK"));
                var resp = orgAdminUIService.RetrieveMultiple(query);
                var currency = resp.Entities.First().ToEntity<TransactionCurrency>();

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, bus.Id, new ColumnSet(true)) as Account;
                Assert.Equal(currency.ToEntityReference().Id, retrieved.TransactionCurrencyId.Id);

                bus.CreditLimit = 10m;
                orgAdminUIService.Update(bus);
                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, bus.Id, new ColumnSet(true)) as Account;
                Assert.Equal(currency.ToEntityReference().Id, retrieved.TransactionCurrencyId.Id);
            }
        }

        [Fact]
        public void TestUserCurrencyIsChosen()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var currentUser = orgAdminUIService.Retrieve(SystemUser.EntityLogicalName, crm.AdminUser.Id, new ColumnSet(true)).ToEntity<SystemUser>();
                var currency = new TransactionCurrency
                {
                    ExchangeRate = 0.5m,
                    CurrencyPrecision = 3
                };
                currency.Id = orgAdminUIService.Create(currency);
                currentUser.TransactionCurrencyId = currency.ToEntityReference();
                orgAdminUIService.Update(currentUser);

                var bus = new Account
                {
                    CreditLimit = 10m
                };
                bus.Id = orgAdminUIService.Create(bus);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, bus.Id, new ColumnSet(true)) as Account;
                Assert.Equal(currency.ToEntityReference(), retrieved.TransactionCurrencyId);

                bus.CreditLimit = 20m;
                orgAdminUIService.Update(bus);
                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, bus.Id, new ColumnSet(true)) as Account;
                Assert.Equal(currency.ToEntityReference(), retrieved.TransactionCurrencyId);
            }
        }

        [Fact]
        public void TestRetrieveExchangeRate()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var dollar = new TransactionCurrency
                {
                    ExchangeRate = 0.6m
                };
                dollar.Id = orgAdminUIService.Create(dollar);

                var request = new RetrieveExchangeRateRequest
                {
                    TransactionCurrencyId = dollar.Id
                };
                var response = orgAdminUIService.Execute(request) as RetrieveExchangeRateResponse;
                Assert.Equal(dollar.ExchangeRate, response.ExchangeRate);
            }
        }

        [Fact]
        public void TestRetriveExhangeRateFail()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var request = new RetrieveExchangeRateRequest
                {
                    TransactionCurrencyId = Guid.NewGuid()
                };
                try
                {
                    orgAdminUIService.Execute(request);
                    throw new XunitException();
                }
                catch (Exception e)
                {
                    Assert.IsType<FaultException>(e);
                }
            }
        }
    }
}
