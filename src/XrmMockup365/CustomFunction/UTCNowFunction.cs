using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using System;

namespace DG.Tools.XrmMockup.CustomFunction
{
    internal class UTCNowFunction : ReflectionFunction
    {
        private readonly TimeSpan timeOffset;

        public UTCNowFunction(TimeSpan timeOffset)
            : base("UTCNow", FormulaType.DateTime)
        {
            this.timeOffset = timeOffset;
        }
        public DateTimeValue Execute()
        {
            var utcNow = DateTime.UtcNow.Add(timeOffset);
            return FormulaValue.New(utcNow);
        }
    }
}
