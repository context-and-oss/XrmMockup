using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using WorkflowExecuter;
using Xunit;

namespace DG.XrmMockupTest
{
    // The classic calculated-field engine concatenates strings via the WorkflowExecuter.Concat node.
    // Dataverse permits a calculated field that references a non-string column inside Concat (the
    // platform stringifies the value before joining), so the node must accept any operand type, not
    // only strings. Before the fix, the (string) cast threw InvalidCastException, which the Retrieve
    // / RetrieveMultiple handlers re-raised because they execute every calculated field on each row
    // regardless of whether it was selected.
    public class TestCalculatedFields
    {
        // Regression for #364: a decimal result does not imply decimal operands.
        // Margin % = (Margin * 100) / Total Discounted Price; both fields are Money.
        [Fact]
        public void Arithmetic_CalculatesDecimalMarginPercentageFromMoneyFields()
        {
            var variables = new Dictionary<string, object>
            {
                ["margin"] = new Money(37.5m),
                ["hundred"] = 100,
                ["totalDiscountedPrice"] = new Money(150m),
            };
            var multiply = new Arithmetic(new[] { new[] { "margin", "hundred" } },
                "Decimal", "marginTimesHundred", "Multiply");
            var divide = new Arithmetic(new[] { new[] { "marginTimesHundred", "totalDiscountedPrice" } },
                "Decimal", "marginPercentage", "Divide");

            multiply.Execute(ref variables, TimeSpan.Zero, null, null, null);
            divide.Execute(ref variables, TimeSpan.Zero, null, null, null);

            Assert.Equal(3750m, Assert.IsType<decimal>(variables["marginTimesHundred"]));
            Assert.Equal(25m, Assert.IsType<decimal>(variables["marginPercentage"]));
        }

        [Theory]
        [InlineData("Add", true, true, 15)]
        [InlineData("Add", true, false, 15)]
        [InlineData("Add", false, true, 15)]
        [InlineData("Add", false, false, 15)]
        [InlineData("Subtract", true, true, 10)]
        [InlineData("Subtract", true, false, 10)]
        [InlineData("Subtract", false, true, 10)]
        [InlineData("Subtract", false, false, 10)]
        [InlineData("Multiply", true, true, 31.25)]
        [InlineData("Multiply", true, false, 31.25)]
        [InlineData("Multiply", false, true, 31.25)]
        [InlineData("Multiply", false, false, 31.25)]
        [InlineData("Divide", true, true, 5)]
        [InlineData("Divide", true, false, 5)]
        [InlineData("Divide", false, true, 5)]
        [InlineData("Divide", false, false, 5)]
        public void Arithmetic_DecimalOutputAcceptsMoneyAndDecimalOperands(
            string method, bool leftIsMoney, bool rightIsMoney, double expected)
        {
            var node = new Arithmetic(new[] { new[] { "left", "right" } }, "Decimal", "result", method);
            var variables = new Dictionary<string, object>
            {
                ["left"] = leftIsMoney ? (object)new Money(12.5m) : 12.5m,
                ["right"] = rightIsMoney ? (object)new Money(2.5m) : 2.5m,
            };

            node.Execute(ref variables, TimeSpan.Zero, null, null, null);

            Assert.Equal(Convert.ToDecimal(expected), Assert.IsType<decimal>(variables["result"]));
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void Arithmetic_DecimalOutputPreservesNullHandling(bool leftIsNull, bool rightIsNull)
        {
            var node = new Arithmetic(new[] { new[] { "left", "right" } }, "Decimal", "result", "Add");
            var variables = new Dictionary<string, object>
            {
                ["left"] = leftIsNull ? null : new Money(12.5m),
                ["right"] = rightIsNull ? null : new Money(12.5m),
            };

            node.Execute(ref variables, TimeSpan.Zero, null, null, null);

            if (leftIsNull && rightIsNull)
                Assert.Null(variables["result"]);
            else
                Assert.Equal(12.5m, Assert.IsType<decimal>(variables["result"]));
        }

        [Fact]
        public void Arithmetic_DecimalOutputStillConvertsOtherNumericOperands()
        {
            var node = new Arithmetic(new[] { new[] { "left", "right" } }, "Decimal", "result", "Add");
            var variables = new Dictionary<string, object>
            {
                ["left"] = 12.5d,
                ["right"] = 2,
            };

            node.Execute(ref variables, TimeSpan.Zero, null, null, null);

            Assert.Equal(14.5m, Assert.IsType<decimal>(variables["result"]));
        }

        [Theory]
        [InlineData("Add", 15)]
        [InlineData("Subtract", 10)]
        [InlineData("Multiply", 31.25)]
        [InlineData("Divide", 5)]
        public void Arithmetic_MoneyOutputRemainsMoney(string method, double expected)
        {
            var node = new Arithmetic(new[] { new[] { "left", "right" } }, "Money", "result", method);
            var variables = new Dictionary<string, object>
            {
                ["left"] = new Money(12.5m),
                ["right"] = new Money(2.5m),
            };

            node.Execute(ref variables, TimeSpan.Zero, null, null, null);

            Assert.Equal(Convert.ToDecimal(expected), Assert.IsType<Money>(variables["result"]).Value);
        }

        [Fact]
        public void Concat_MixesStringAndInt()
        {
            var node = new Concat(new[] { new[] { "a", "b", "c" } }, "result");
            var variables = new Dictionary<string, object>
            {
                ["a"] = "Order-",
                ["b"] = 42,
                ["c"] = " (priority)",
            };

            node.Execute(ref variables, TimeSpan.Zero, null, null, null);

            Assert.Equal("Order-42 (priority)", variables["result"]);
        }

        [Fact]
        public void Concat_MixesStringAndDecimal()
        {
            var node = new Concat(new[] { new[] { "a", "b" } }, "result");
            var variables = new Dictionary<string, object>
            {
                ["a"] = "Total: ",
                ["b"] = 12.5m,
            };

            node.Execute(ref variables, TimeSpan.Zero, null, null, null);

            Assert.Equal("Total: 12.5", variables["result"]);
        }

        [Fact]
        public void Concat_TreatsNullAsEmpty()
        {
            var node = new Concat(new[] { new[] { "a", "b", "c" } }, "result");
            var variables = new Dictionary<string, object>
            {
                ["a"] = "Start",
                ["b"] = null,
                ["c"] = "End",
            };

            node.Execute(ref variables, TimeSpan.Zero, null, null, null);

            Assert.Equal("StartEnd", variables["result"]);
        }
    }
}
