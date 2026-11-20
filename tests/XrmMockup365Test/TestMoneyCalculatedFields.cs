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
    public class TestMoneyCalculatedFields
    {
        private const string PrimaryEntity = "InputEntities(\"primaryEntity\")";
        private const string WholeNumberField = "ns_simplecalculatedfieldtest";
        private const string DecimalField = "ns_decimalfield";
        private const string MappingField = "ns_simplecalculatedmoneyfield";
        private const string ArithmeticField = "ns_calculatedmoneyfield";

        [Theory]
        [InlineData("100", false)]
        [InlineData("-100", false)]
        [InlineData("0", false)]
        [InlineData("1", false)]
        [InlineData("-1", false)]
        [InlineData("100.49", true)]
        [InlineData("-100.50", true)]
        [InlineData("1.01", true)]
        [InlineData("0", true)]
        [InlineData("0.50", true)]
        public void MappingNumberToMoneyWrapsTheValueWithoutRounding(string amount, bool decimalSource)
        {
            var tree = ParseCalculation("NumberToMoney.xml", decimalSource);
            var account = AccountWithNumber(amount, decimalSource);

            tree.Execute(account, TimeSpan.Zero, null, null, null, suppressWrites: true);

            var result = Assert.IsAssignableFrom<Entity>(tree.Variables[PrimaryEntity]);
            Assert.Equal(ParseDecimal(amount), Assert.IsType<Money>(result[MappingField]).Value);
        }

        [Theory]
        [InlineData("100", false)]
        [InlineData("-100", false)]
        [InlineData("0", false)]
        [InlineData("1", false)]
        [InlineData("-1", false)]
        [InlineData("100.49", true)]
        [InlineData("-100.50", true)]
        [InlineData("1.01", true)]
        [InlineData("0", true)]
        [InlineData("0.50", true)]
        public void MoneyArithmeticAcceptsWholeAndDecimalSourcesAndPreservesFractions(string amount, bool decimalSource)
        {
            // The exported calculation is (Number / 2) * 2, with Money targets even
            // for the CreateCrmType activities whose parameter specifies Integer.
            var tree = ParseCalculation("NumberArithmeticToMoney.xml", decimalSource);
            var account = AccountWithNumber(amount, decimalSource);

            tree.Execute(account, TimeSpan.Zero, null, null, null, suppressWrites: true);

            Assert.Equal(ParseDecimal(amount) / 2,
                Assert.IsType<Money>(tree.Variables["SetAttributeValueStep4_3"]).Value);
            var result = Assert.IsAssignableFrom<Entity>(tree.Variables[PrimaryEntity]);
            Assert.Equal(ParseDecimal(amount), Assert.IsType<Money>(result[ArithmeticField]).Value);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MappingEmptyNumberToMoneyRemainsEmpty(bool explicitNull)
        {
            var tree = ParseCalculation("NumberToMoney.xml");
            var account = new Account { Id = Guid.NewGuid() };
            if (explicitNull)
                account[WholeNumberField] = null;

            tree.Execute(account, TimeSpan.Zero, null, null, null, suppressWrites: true);

            var result = Assert.IsAssignableFrom<Entity>(tree.Variables[PrimaryEntity]);
            Assert.Null(result[MappingField]);
        }

        [Theory]
        [InlineData("100.50", 101, false)]
        [InlineData("-100.50", -101, false)]
        [InlineData("100.50", 101, true)]
        [InlineData("-100.50", -101, true)]
        public void MoneyCalculationUsesTheAlreadyRoundedWholeNumberResult(
            string amount, int wholeNumber, bool arithmetic)
        {
            // Evaluate the source calculated field before the dependent Money field.
            var account = new Account { Id = Guid.NewGuid() };
            account["ns_moneyfield"] = new Money(ParseDecimal(amount));
            var wholeNumberTree = ParseCalculation("MoneyToWholeNumber.xml");
            wholeNumberTree.Execute(account, TimeSpan.Zero, null, null, null, suppressWrites: true);
            var source = Assert.IsAssignableFrom<Entity>(wholeNumberTree.Variables[PrimaryEntity]);
            Assert.Equal(wholeNumber, Assert.IsType<int>(source[WholeNumberField]));
            var tree = ParseCalculation(arithmetic ? "NumberArithmeticToMoney.xml" : "NumberToMoney.xml");

            tree.Execute(source, TimeSpan.Zero, null, null, null, suppressWrites: true);

            var result = Assert.IsAssignableFrom<Entity>(tree.Variables[PrimaryEntity]);
            Assert.Equal((decimal)wholeNumber,
                Assert.IsType<Money>(result[arithmetic ? ArithmeticField : MappingField]).Value);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WrappingNumberAsMoneyDoesNotChangeItsValueWithAnExchangeRate(bool decimalSource)
        {
            var account = new Account();
            var value = decimalSource ? (object)100.50m : 100;
            var variables = new Dictionary<string, object>
            {
                ["account"] = account,
                ["value"] = value,
                ["ExchangeRate"] = 3m,
            };
            var node = new SetEntityProperty(MappingField, "account", "account", "value", "Money");

            node.Execute(ref variables, TimeSpan.Zero, null, null, null);

            Assert.Equal(Convert.ToDecimal(value), Assert.IsType<Money>(account[MappingField]).Value);
        }

        [Fact]
        public void AssigningExistingMoneyPreservesExistingExchangeRateBehavior()
        {
            var account = new Account();
            var variables = new Dictionary<string, object>
            {
                ["account"] = account,
                ["value"] = new Money(100.50m),
                ["ExchangeRate"] = 3m,
            };
            var node = new SetEntityProperty(MappingField, "account", "account", "value", "Money");

            node.Execute(ref variables, TimeSpan.Zero, null, null, null);

            Assert.Equal(301.50m, Assert.IsType<Money>(account[MappingField]).Value);
        }

        private static WorkflowTree ParseCalculation(string fileName, bool decimalSource = false)
        {
            var xaml = File.ReadAllText(Path.Combine("..", "..", "..", "Metadata", "CalculatedFields", fileName));
            // Change only the source attribute for the decimal-column variant; preserve all
            // operators, target types, and the output attribute from the captured definition.
            if (decimalSource)
                xaml = xaml.Replace($"Attribute=\"{WholeNumberField}\"", $"Attribute=\"{DecimalField}\"");
            return WorkflowConstructor.ParseCalculated(xaml, "account.moneyRegression");
        }

        private static Account AccountWithNumber(string amount, bool decimalSource)
        {
            var value = ParseDecimal(amount);
            var account = new Account { Id = Guid.NewGuid() };
            account[decimalSource ? DecimalField : WholeNumberField] = decimalSource ? (object)value : (int)value;
            return account;
        }

        private static decimal ParseDecimal(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
    }
}
