using Microsoft.PowerFx;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;
using System;
using System.Globalization;

namespace DG.Tools.XrmMockup.CustomFunction
{
    internal class ISOWeekNumFunction : ReflectionFunction
    {
        private readonly TimeSpan timeOffset;

        public ISOWeekNumFunction(TimeSpan timeOffset)
            : base("ISOWeekNum", FormulaType.Decimal, FormulaType.DateTime)
        {
            this.timeOffset = timeOffset;
        }
        public DecimalValue Execute(DateTimeValue date)
        {
            var utcDate = date?.GetConvertedValue(TimeZoneInfo.Utc);
            if (utcDate == null || utcDate.Value <= DateTime.MinValue)
                throw new CustomFunctionErrorException("Invalid date or time value", ErrorKind.InvalidArgument);

            utcDate = utcDate.Value.Add(timeOffset);

#if DATAVERSE_SERVICE_CLIENT
            var weekNumber = ISOWeek.GetWeekOfYear(utcDate.Value);
#else
            // .NET Framework does not have ISOWeek class
            // Implementing ISO 8601 week date algorithm
            // https://learn.microsoft.com/en-us/archive/blogs/shawnste/iso-8601-week-of-year-format-in-microsoft-net

            var dayOfWeek = utcDate.Value.DayOfWeek;
            if (dayOfWeek >= DayOfWeek.Monday && dayOfWeek <= DayOfWeek.Wednesday)
            {
                utcDate = utcDate.Value.AddDays(3);
            }

            var weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(utcDate.Value, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
#endif

            return FormulaValue.New(weekNumber);
        }
    }
}
