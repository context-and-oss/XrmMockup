using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using WorkflowExecuter;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestWholeNumberCalculatedFields
    {
        private const string PrimaryEntity = "InputEntities(\"primaryEntity\")";
        private const string MoneyField = "ns_moneyfield";
        private const string MappingField = "ns_simplecalculatedfieldtest";
        private const string ArithmeticField = "ns_calculatedfieldtest";

        // These fixtures are the classic calculated-field XAML exported from Dataverse.
        // The expected values were verified against that environment, including negative ties.
        [Theory]
        [InlineData("100.49", 100)]
        [InlineData("100.50", 101)]
        [InlineData("100.51", 101)]
        [InlineData("-100.49", -100)]
        [InlineData("-100.50", -101)]
        [InlineData("-100.51", -101)]
        [InlineData("0.49", 0)]
        [InlineData("-0.49", 0)]
        [InlineData("0.50", 1)]
        [InlineData("-0.50", -1)]
        [InlineData("101.50", 102)]
        [InlineData("-101.50", -102)]
        [InlineData("0", 0)]
        public void MappingMoneyToWholeNumberRoundsMidpointsAwayFromZero(string amount, int expected)
        {
            var tree = ParseCalculation("MoneyToWholeNumber.xml");
            var account = AccountWithMoney(amount);

            tree.Execute(account, TimeSpan.Zero, null, null, null, suppressWrites: true);

            var result = Assert.IsAssignableFrom<Entity>(tree.Variables[PrimaryEntity]);
            Assert.Equal(expected, Assert.IsType<int>(result[MappingField]));
        }

        [Fact]
        public void MappingMissingMoneyToWholeNumberRemainsEmptyOnRepeatedExecution()
        {
            var tree = ParseCalculation("MoneyToWholeNumber.xml");
            tree.Execute(AccountWithMoney("100.50"), TimeSpan.Zero, null, null, null, suppressWrites: true);
            var account = new Account { Id = Guid.NewGuid() };

            tree.Execute(account, TimeSpan.Zero, null, null, null, suppressWrites: true);

            var result = Assert.IsAssignableFrom<Entity>(tree.Variables[PrimaryEntity]);
            Assert.Null(result[MappingField]);
        }

        [Theory]
        [InlineData("1.01", 1)]
        [InlineData("-1.01", -1)]
        [InlineData("100.50", 101)]
        [InlineData("-100.50", -101)]
        public void WholeNumberCalculationPreservesFractionsUntilFinalAssignment(string amount, int expected)
        {
            // Every operator is tagged Int32 in the exported XAML, but (Money / 2) * 2
            // must not round or truncate the division result before multiplying it back.
            var tree = ParseCalculation("MoneyArithmeticToWholeNumber.xml");
            var account = AccountWithMoney(amount);

            tree.Execute(account, TimeSpan.Zero, null, null, null, suppressWrites: true);

            Assert.Equal(ParseDecimal(amount) / 2,
                Assert.IsType<decimal>(tree.Variables["SetAttributeValueStep4_3"]));
            var result = Assert.IsAssignableFrom<Entity>(tree.Variables[PrimaryEntity]);
            Assert.Equal(expected, Assert.IsType<int>(result[ArithmeticField]));
        }

        [Theory]
        [InlineData("100.49", 100)]
        [InlineData("100.50", 101)]
        [InlineData("-100.50", -101)]
        public void AssigningDecimalToWholeNumberRoundsOnlyAtAssignment(string value, int expected)
        {
            var account = new Account();
            var variables = new Dictionary<string, object>
            {
                ["account"] = account,
                ["value"] = ParseDecimal(value),
            };
            var node = new SetEntityProperty(MappingField, "account", "account", "value", "Int32");

            node.Execute(ref variables, TimeSpan.Zero, null, null, null);

            Assert.Equal(expected, Assert.IsType<int>(account[MappingField]));
        }

        [Fact]
        public void MappingMoneyToWholeNumberDoesNotApplyCurrencyExchangeRate()
        {
            var account = new Account();
            var variables = new Dictionary<string, object>
            {
                ["account"] = account,
                ["value"] = new Money(100.50m),
                ["ExchangeRate"] = 3m,
            };
            var node = new SetEntityProperty(MappingField, "account", "account", "value", "Int32");

            node.Execute(ref variables, TimeSpan.Zero, null, null, null);

            Assert.Equal(101, Assert.IsType<int>(account[MappingField]));
        }

        [Theory]
        [InlineData(12, 5, 2)]
        [InlineData(-12, 5, -2)]
        public void OrdinaryWorkflowIntegerArithmeticKeepsExistingBehavior(int left, int right, int expected)
        {
            var variables = new Dictionary<string, object> { ["left"] = left, ["right"] = right };
            var node = new Arithmetic(new[] { new[] { "left", "right" } }, "Int32", "result", "Divide");

            node.Execute(ref variables, TimeSpan.Zero, null, null, null);

            Assert.Equal(expected, Assert.IsType<int>(variables["result"]));
        }

        private static WorkflowTree ParseCalculation(string fileName)
        {
            var xaml = File.ReadAllText(Path.Combine("..", "..", "..", "Metadata", "CalculatedFields", fileName));
            return WorkflowConstructor.ParseCalculated(xaml, "account.wholeNumberRegression");
        }

        private static Account AccountWithMoney(string amount)
        {
            var account = new Account { Id = Guid.NewGuid() };
            account[MoneyField] = new Money(ParseDecimal(amount));
            return account;
        }

        private static decimal ParseDecimal(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
    }
}
