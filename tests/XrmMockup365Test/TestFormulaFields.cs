using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.PowerFx.Types;
using Microsoft.Xrm.Sdk;
using System;
using System.Globalization;
using System.Linq;
using Xunit;

using TTask = System.Threading.Tasks.Task;

namespace DG.XrmMockupTest
{
    public class TestFormulaFields : UnitTestBase
    {
        private readonly IOrganizationServiceFactory serviceFactory;

        public TestFormulaFields(XrmMockupFixture fixture) : base(fixture) {
            serviceFactory = new UnitTestOrganizationServiceFactory(this);
        }

        [Theory]
        [InlineData("123", "1 + 1", "2")]
        [InlineData("123", "name & \" - \" & accountnumber", "Test - 123")]
        [InlineData("123", "Concatenate(name, \" - \", Text(accountnumber))", "Test - 123")]
        [InlineData("123", "If(Decimal(accountnumber) > 0, name & \" - \" & accountnumber, \"No Account Number\")", "Test - 123")]
        [InlineData("0", "If(Decimal(accountnumber) > 0, name & \" - \" & accountnumber, \"No Account Number\")", "No Account Number")]
        public async TTask CanEvaluateExpressionOnEntity(string accountNumber, string formula, string expected)
        {
            var evaluator = new FormulaFieldEvaluator(serviceFactory);

            var account = Create(new Account { Name = "Test", AccountNumber = accountNumber });
            var result = await evaluator.Evaluate(formula, account, TimeSpan.Zero);
            Assert.Equal(expected, result.ToString());
        }

        [Fact]
        public async TTask CanEvaluateExpressionWithRelatedEntity()
        {
            var evaluator = new FormulaFieldEvaluator(serviceFactory);

            var adminUserId = Guid.Parse("3b961284-cd7a-4fa3-af7e-89802e88dd5c");
            var parent = Create(new ctx_parent { ctx_Name = "Fluffy", OwnerId = new EntityReference(SystemUser.EntityLogicalName, adminUserId) });
            var child = Create(new ctx_child { ctx_ParentId = new EntityReference(ctx_parent.EntityLogicalName, parent.Id), ctx_Name = "Meatballs" });

            // ctx_TrimLeft is a Power Fx formula column defined as Mid(ctx_name, 3).
            var result = await evaluator.Evaluate("\"Test: \" & Mid(ctx_name, 3)", parent, TimeSpan.Zero);
            Assert.Equal("Test: uffy", result);

            result = await evaluator.Evaluate("If(ctx_ParentId.ctx_name = \"Fluffy\", \"Test: \" & ctx_ParentId.ctx_name & \" eats \" & ctx_name, \"New telegraph, who dis?\")", child, TimeSpan.Zero);
            Assert.Equal("Test: Fluffy eats Meatballs", result);
        }

        [Theory]
        [ClassData(typeof(FunctionCases))]
        public async TTask CanEvaluateFunction(string formula, object expected)
        {
            var targetCulture = new CultureInfo("en-US");
            var originalUICulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = targetCulture;
                var evaluator = new FormulaFieldEvaluator(serviceFactory);
                var result = await evaluator.Evaluate(formula, new Account(), TimeSpan.Zero);

                if (result is ErrorValue error && expected is string errorString)
                {
                    var errorMessage = string.Join("\n", error.Errors.Select(e => e.Message));
                    Assert.Contains(errorString, errorMessage);
                }
                else if (result is DateTime resultDateTime)
                {
                    var expectedDateTime = (expected is DateTime dt)
                        ? dt
                        : (expected is "DateTime.Now")
                            ? DateTime.Now
                            : throw new InvalidOperationException("Expected value is not a DateTime or 'DateTime.Now'");
                    Assert.Equal(expectedDateTime, resultDateTime, TimeSpan.FromSeconds(1));
                }
                else
                {
                    Assert.Equal(expected, result);
                }
            }
            finally
            {
                CultureInfo.CurrentUICulture = originalUICulture;
            }
        }

        [Theory]
        [ClassData(typeof(TextFunctionCases))]
        public async TTask CanEvaluateTextFunction(string formula, string expected)
        {
            var evaluator = new FormulaFieldEvaluator(serviceFactory);
            var result = await evaluator.Evaluate(formula, new Account(), TimeSpan.Zero);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("ISOWeekNum(Date(2025, 1, 1))", 1)] // Returns the ISO week number of a date/time value.
        [InlineData("IsUTCToday(DateTime(2025, 1, 1, 12, 30, 40))", false)] // Checks whether a date/time value is sometime today in Coordinated Universal Time (UTC).
        [InlineData("IsUTCToday(Now())", true)]
        [InlineData("UTCNow()", "DateTime.UtcNow")] // Returns the current date/time value in Coordinated Universal Time (UTC).
        [InlineData("UTCToday()", "DateTime.UtcNow.Date")] // Returns the current date-only value in Coordinated Universal Time (UTC).
        public async TTask CanEvaluateDataverseDateTimeCustomFunctions(string formula, object expected)
        {
            // Verify that the DataTime custom functions work as expected.
            // https://learn.microsoft.com/en-us/power-platform/power-fx/formula-reference-formula-columns

            var evaluator = new FormulaFieldEvaluator(serviceFactory);
            var result = await evaluator.Evaluate(formula, new Account(), TimeSpan.Zero);

            if (result is DateTime resultDateTime)
            {
                DateTime? expectedDateTime;
                if (expected is "DateTime.UtcNow")
                {
                    expectedDateTime = DateTime.UtcNow;
                }
                else if (expected is "DateTime.UtcNow.Date")
                {
                    expectedDateTime = DateTime.UtcNow.Date;
                }
                else if (expected is DateTime dt)
                {
                    expectedDateTime = dt;
                }
                else
                {
                    throw new Exception("Expected value is not a DateTime or 'DateTime.UtcNow'/'DateTime.UtcNow.Date'");
                }

                Assert.Equal(expectedDateTime.Value, resultDateTime, TimeSpan.FromSeconds(1));
            }
            else if (result is decimal resultDecimal && expected is int expectedInt)
            {
                Assert.Equal(expectedInt, resultDecimal);
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        [Fact]
        public async TTask UTCNowRespectsOffset()
        {
            var evaluator = new FormulaFieldEvaluator(serviceFactory);
            var result = await evaluator.Evaluate("UTCNow()", new Account(), TimeSpan.FromDays(1));
            var resultDateTime = (DateTime)result;
            Assert.Equal(DateTime.UtcNow.AddDays(1), resultDateTime, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async TTask UTCTodayRespectsOffset()
        {
            var evaluator = new FormulaFieldEvaluator(serviceFactory);
            var result = await evaluator.Evaluate("UTCToday()", new Account(), TimeSpan.FromDays(1));
            var resultDateTime = (DateTime)result;
            Assert.Equal(DateTime.UtcNow.Date.AddDays(1), resultDateTime, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async TTask IsUTCTodayRespectsOffset()
        {
            var evaluator = new FormulaFieldEvaluator(serviceFactory);
            var result = await evaluator.Evaluate("IsUTCToday(UTCNow())", new Account(), TimeSpan.FromDays(1));
            Assert.Equal(true, result);
        }
    }

    public class FunctionCases : TheoryData<string, object>
    {
        public FunctionCases()
        {
            // Absolute value of a number.
            Add("Abs(-10.5)", 10.5m);

            // Boolean logic AND. Returns true if all arguments are true. You can also use the && operator.
            Add("And(true, true)", true);
            Add("And(true, false)", false);
            Add("And(false, false)", false);
            Add("And(false, true)", false);
            Add("true && true", true);

            // Calculates the average of a table expression or a set of arguments.
            Add("Average(2, 6)", 4m);

            // Returns a blank value that can be used to insert a NULL value in a data source.
            Add("Blank()", null);

            // Translates a character code into a string.
            Add("Char(42)", "*");

            // Concatenates strings.
            Add("Concatenate(\"Hello,\", \" \", \"World!\")", "Hello, World!");

            // Adds days, months, quarters, or years to a date/time value.
            Add("DateAdd(Date(2025, 1, 1), 5, TimeUnit.Days)", new DateTime(2025, 1, 6));
            Add("DateAdd(Date(2025, 1, 1), 1, TimeUnit.Months)", new DateTime(2025, 2, 1));
            Add("DateAdd(Date(2025, 1, 1), 1, TimeUnit.Quarters)", new DateTime(2025, 4, 1));
            Add("DateAdd(Date(2025, 1, 1), 1, TimeUnit.Years)", new DateTime(2026, 1, 1));

            // Subtracts two date values, and shows the result in days, months, quarters, or years.
            Add("DateDiff(Date(2025, 2, 1), Date(2025, 1, 1), TimeUnit.Months)", -1m);

            // Retrieves the day portion of a date/time value.
            Add("Day(Date(2025, 1, 5))", 5m);

            // Converts a string to a decimal number.
            Add("Decimal(\"10.5\")", 10.5m);

            // Checks whether a text string ends with another text string.
            Add("EndsWith(\"some string\", \"string\")", true);
            Add("EndsWith(\"some string\", \"nooooo\")", false);

            // Returns e raised to a power.
            Add("Exp(0)", 1d);

            // Converts a string to a floating point number.
            Add("Float(\"10.5\")", 10.5d);

            // Returns the hour portion of a date/time value.
            Add("Hour(DateTime(2025, 1, 1, 12, 30, 40))", 12m);

            // Returns one value if a condition is true and another value if not.
            Add("If(true, \"Yes\", \"No\")", "Yes");
            Add("If(false, \"Yes\", \"No\")", "No");

            // Detects errors and provides an alternative value or takes action.
            Add("IfError(Error(\"FAULT\"), \"Caught!\")", "Caught!");

            // Rounds down to the nearest integer.
            Add("Int(10.5)", 10m);

            // Checks for a blank value.
            Add("IsBlank(Blank())", true);
            Add("IsBlank(\"Not blank\")", false);

            // Error()   : Create a custom error or pass through an error.
            // IsError() : Checks for an error.
            Add("IsError(Error(\"FAULT\"))", true);
            Add("IsError(\"No error\")", false);

            // Returns the left-most portion of a string.
            Add("Left(\"Hello, world!\", 5)", "Hello");

            // Returns the length of a string.
            Add("Len(\"Hello, world!\")", 13m);

            // Returns the natural log.
            Add("Ln(Exp(1))", 1d);
            Add("Ln(1)", 0d);
            Add("Ln(0)", "The function 'Ln' returned a non-finite number.");
            Add("Round(Ln(2), 2)", 0.69d);

            // Converts letters in a string of text to all lowercase.
            Add("Lower(\"HELLO, world\")", "hello, world");

            // Maximum value of a table expression or a set of arguments.
            Add("Max(1, 2, 3)", 3m);

            // Returns the middle portion of a string.
            Add("Mid(\"Hello, world!\", 6, 2)", ", ");

            // Minimum value of a table expression or a set of arguments.
            Add("Min(1, 2, 3)", 1m);

            // Retrieves the minute portion of a date/time value.
            Add("Minute(DateTime(2025, 1, 1, 12, 30, 40))", 30m);

            // Returns the remainder after a dividend is divided by a divisor.
            Add("Mod(42, 2)", 0m);
            Add("Mod(87, 14)", 3m);

            // Retrieves the month portion of a date/time value.
            Add("Month(DateTime(2025, 1, 1, 12, 30, 40))", 1m);

            // Boolean logic NOT. Returns true if its argument is false, and returns false if its argument is true. You can also use the ! operator.
            Add("Not(true)", false);
            Add("Not(false)", true);

            // Returns the current date/time value in the user's time zone.
            Add("Now()", "DateTime.Now");

            // Boolean logic OR. Returns true if any of its arguments are true. You can also use the || operator.
            Add("Or(true, false)", true);
            Add("Or(true, true)", true);
            Add("Or(false, true)", true);
            Add("Or(false, false)", false);
            Add("true || false", true);

            // Returns a number raised to a power. You can also use the ^ operator.
            Add("Power(10, 2)", 100d);
            Add("Power(2, 10)", 1024d);
            Add("2 ^ 10", 1024d);

            // Replaces part of a string with another string, by starting position of the string.
            Add("Replace(\"Hello, world!\", 1, 5, \"Goodbye\")", "Goodbye, world!");

            // Returns the right-most portion of a string.
            Add("Right(\"Hello, world!\", 6)", "world!");

            // Exp(): Returns e raised to a power.
            // Round(): Rounds to the closest number.
            Add("Round(Exp(2), 3)", 7.389d);
            Add("Round(Exp(2), 2)", 7.39d);

            // Rounds down to the largest previous number.
            Add("RoundDown(10.57443, 2)", 10.57m);
            Add("RoundDown(10.57443, 1)", 10.5m);
            Add("RoundDown(10.57443, 0)", 10m);

            // Rounds up to the smallest next number.
            Add("RoundUp(10.57443, 2)", 10.58m);
            Add("RoundUp(10.57443, 1)", 10.6m);
            Add("RoundUp(10.57443, 0)", 11m);

            // Retrieves the second portion of a date/time value.
            Add("Second(DateTime(2025, 1, 1, 12, 30, 40))", 40m);

            // Returns the square root of a number.
            Add("Sqrt(16)", 4d);

            // Checks if a text string begins with another text string.
            Add("StartsWith(\"Hello, world!\", \"Hello\")", true);
            Add("StartsWith(\"Hello, world!\", \"world!\")", false);

            // Replaces part of a string with another string, by matching strings.
            Add("Substitute(\"Hello, world!\", \"Hello\", \"Goodbye\")", "Goodbye, world!");

            // Calculates the sum of a table expression or a set of arguments.
            Add("Sum(1, 2, 3, 4, 5)", 15m);

            // Matches with a set of values and then evaluates a corresponding formula.
            Add("Switch(10, 10, \"Result 1\", 20, \"Result 2\", \"Result 3\")", "Result 1");
            Add("Switch(20, 10, \"Result 1\", 20, \"Result 2\", \"Result 3\")", "Result 2");
            Add("Switch(30, 10, \"Result 1\", 20, \"Result 2\", \"Result 3\")", "Result 3");

            // Removes extra spaces from the ends and interior of a string of text.
            Add("Trim(\"    Hello,    world!     \")", "Hello, world!");

            // Truncates the number to only the integer portion by removing any decimal portion.
            Add("Trunc(10.5)", 10m);

            // Removes extra spaces from the ends of a string of text only.
            Add("TrimEnds(\"    Hello,    world!     \")", "Hello,    world!");

            // Converts letters in a string of text to all uppercase.
            Add("Upper(\"Hello, WORLD!\")", "HELLO, WORLD!");

            // Converts a string to a number.
            Add("Value(10)", 10m);
            Add("Value(10.5)", 10.5m);
            Add("Value(10.575)", 10.575m);

            // Retrieves the weekday portion of a date/time value.
            Add("Weekday(DateTime(2025, 1, 1, 12, 30, 40))", 4m);

            // Returns the week number of a date/time value.
            Add("WeekNum(DateTime(2025, 1, 1, 12, 30, 40))", 1m);
            Add("WeekNum(DateTime(2025, 1, 1, 12, 30, 40), StartOfWeek.Sunday)", 1m);
            Add("WeekNum(DateTime(2025, 1, 1, 12, 30, 40), StartOfWeek.Monday)", 1m);

            // Retrieves the year portion of a date/time value.
            Add("Year(DateTime(2025, 1, 1, 12, 30, 40))", 2025m);
        }
    }

    public class TextFunctionCases : TheoryData<string, string>
    {
        public TextFunctionCases()
        {
            // Converts any value and formats a number or date/time value to a string of text.
            Add("Text(1234.59, \"####.#\")", "1234.6");
            Add("Text(8.9, \"#.000\")", "8.900");
            Add("Text(0.631, \"0.#\")", "0.6");
            Add("Text(12, \"#.0#\")", "12.0");
            Add("Text(1234.568, \"#.0#\")", "1234.57");
            Add("Text(12000, \"$ #,###\")", "$ 12,000");
            Add("Text(1200000, \"$ #,###\")", "$ 1,200,000");

            // TODO: Something funky is going on with the culture handling here.
            // TODO: Also, seems LongTime24 just... doesn't? It returns the same as LongTime.

            var testDateTime = new DateTime(2015, 11, 23, 14, 37, 47);
            Add("Text(DateTime(2015, 11, 23, 14, 37, 47), DateTimeFormat.LongDate)", testDateTime.ToString("D", CultureInfo.InvariantCulture));
            Add("Text(DateTime(2015, 11, 23, 14, 37, 47), DateTimeFormat.LongDateTime)", testDateTime.ToString("F", CultureInfo.InvariantCulture));
            Add("Text(DateTime(2015, 11, 23, 14, 37, 47), DateTimeFormat.LongTime24)", "14:37:47");
            Add("Text(DateTime(2015, 11, 23, 14, 37, 47), DateTimeFormat.ShortDate)", "11/23/2015");
            Add("Text(DateTime(2015, 11, 23, 14, 37, 47), \"d-mmm-yy\")", "23-Nov-15");

            // TODO: This errors out with invalid date time
            //Add("Text(1448318857 * 1000, \"mmm. dd, yyyy (hh:mm:ss AM/PM)\")", "Nov. 23, 2015 (02:47:37 PM)");

            // TODO: The in-format specifier doesn't seem to be supported on the parser currently
            //Add("Text(1234567, 89; \"[$-da-DK]# ###,## €\")", "1.234.567,89 €");
            Add("Text(1234567.89, \"#,###.## €\", \"da-DK\")", "1.234.567,89 €");
            Add("Text(Date(2016, 1, 31), \"dddd mmmm d\")", "Sunday January 31");
            Add("Text(Date(2016, 1, 31), \"dddd mmmm d\", \"es-ES\")", "domingo enero 31");

            Add("Text(1234567.89)", "1234567.89");

            // TODO: This also fails
            //Add("Text(DateTimeValue(\"01/04/2003\"))", "1/4/2003 12:00 AM");

            Add("Text(true)", "true");
            Add("Text(GUID(\"f8b10550-0f12-4f08-9aa3-bb10958bc3ff\"))", "f8b10550-0f12-4f08-9aa3-bb10958bc3ff");
        }
    }
}
