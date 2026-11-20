using Microsoft.PowerFx;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;
using System;

namespace DG.Tools.XrmMockup.CustomFunction
{
    internal class IsUTCTodayFunction : ReflectionFunction
    {
        private readonly TimeSpan timeOffset;

        public IsUTCTodayFunction(TimeSpan timeOffset)
            : base("IsUTCToday", FormulaType.Boolean, FormulaType.DateTime)
        {
            this.timeOffset = timeOffset;
        }
        public BooleanValue Execute(DateTimeValue date)
        {
            var utcDate = date?.GetConvertedValue(TimeZoneInfo.Utc);
            if (utcDate == null || utcDate <= DateTime.MinValue)
                throw new CustomFunctionErrorException("Invalid date or time value", ErrorKind.InvalidArgument);

            var utcToday = DateTime.UtcNow.Add(timeOffset).Date;
            return FormulaValue.New(utcDate.Value.Date == utcToday);
        }
    }
}
