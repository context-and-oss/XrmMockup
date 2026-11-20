using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using System;

namespace DG.Tools.XrmMockup.CustomFunction
{
    internal class UTCTodayFunction : ReflectionFunction
    {
        private readonly TimeSpan timeOffset;

        public UTCTodayFunction(TimeSpan timeOffset)
            : base("UTCToday", FormulaType.DateTime)
        {
            this.timeOffset = timeOffset;
        }

        public DateTimeValue Execute()
        {
            var utcNow = DateTime.UtcNow.Add(timeOffset);
            return FormulaValue.New(utcNow.Date);
        }
    }
}
