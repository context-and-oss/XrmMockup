using System;
using System.Collections.Generic;
using System.Text;

#if DATAVERSE_SERVICE_CLIENT
namespace Microsoft.Xrm.Sdk.Workflow
{
    //
    // Summary:
    //     Represents a time interval.
    [Serializable]
    public struct XrmTimeSpan
    {
        //
        // Summary:
        //     Represents the zero XrmTimeSpan value. This field is read-only.
        public static readonly XrmTimeSpan Zero = new XrmTimeSpan(0, 0, 0, 0, 0);

        //
        // Summary:
        //     Gets the number of whole years represented by the current XrmTimeSpan structure.
        //
        // Returns:
        //     Type: Int32 The number of whole years represented by the current XrmTimeSpan
        //     structure.
        public int Years { get; set; }

        //
        // Summary:
        //     Gets the number of whole months represented by the current XrmTimeSpan structure.
        //
        // Returns:
        //     Type: Int32 The number of whole months represented by the current XrmTimeSpan
        //     structure.
        public int Months { get; set; }

        //
        // Summary:
        //     Gets the number of whole days represented by the current XrmTimeSpan structure.
        //
        // Returns:
        //     Type: Int32 The number of whole days represented by the current XrmTimeSpan structure.
        public int Days { get; set; }

        //
        // Summary:
        //     Gets the number of whole hours represented by the current XrmTimeSpan structure.
        //
        // Returns:
        //     Type: Int32 The number of whole hours represented by the current XrmTimeSpan
        //     structure.
        public int Hours { get; set; }

        //
        // Summary:
        //     Gets the number of whole minutes represented by the current XrmTimeSpan structure.
        //
        // Returns:
        //     Type: Int32 The number of whole minutes represented by the current XrmTimeSpan
        //     structure.
        public int Minutes { get; set; }

        //
        // Summary:
        //     Initializes a new instance of the CrmTimeSpan class setting the time span.
        //
        // Parameters:
        //   value:
        //     Type: TimeSpan. Specifies the time span.
        public XrmTimeSpan(TimeSpan value)
        {
            Years = 0;
            Months = 0;
            Days = value.Days;
            Hours = value.Days;
            Minutes = value.Days;
        }

        //
        // Summary:
        //     Initializes a new instance of the XrmTimeSpan class setting the days, hours and
        //     minutes.
        //
        // Parameters:
        //   days:
        //     Type: Int32. Specifies the days for the time span.
        //
        //   hours:
        //     Type: Int32. Specifies the hours for the time span.
        //
        //   minutes:
        //     Type: Int32. Specifies the minutes for the time span.
        public XrmTimeSpan(int days, int hours, int minutes)
        {
            Years = 0;
            Months = 0;
            Days = days;
            Hours = hours;
            Minutes = minutes;
        }

        //
        // Summary:
        //     Initializes a new instance of the XrmTimeSpan class setting the years, months,
        //     days, hours and minutes.
        //
        // Parameters:
        //   years:
        //     Type: Int32. Specifies the years for the time span.
        //
        //   months:
        //     Type: Int32. Specifies the months for the time span.
        //
        //   days:
        //     Type: Int32. Specifies the days for the time span.
        //
        //   hours:
        //     Type: Int32. Specifies the hours for the time span.
        //
        //   minutes:
        //     Type: Int32. Specifies the minutes for the time span.
        public XrmTimeSpan(int years, int months, int days, int hours, int minutes)
        {
            Years = years;
            Months = months;
            Days = days;
            Hours = hours;
            Minutes = minutes;
        }

        //
        // Summary:
        //     Creates an instance of a XrmTimeSpan class setting the days, hours and minutes.
        //
        // Parameters:
        //   days:
        //     Type: Int32. Specifies the days for the time span.
        //
        //   hours:
        //     Type: Int32. Specifies the hours for the time span.
        //
        //   minutes:
        //     Type: Int32. Specifies the minutes for the time span.
        //
        // Returns:
        //     Type: Microsoft.Xrm.Sdk.Workflow.XrmTimeSpan The instance of a XrmTimeSpan class
        //     where the days, hours, and minutes are set.
        public static XrmTimeSpan CreateXrmTimeSpan(int days, int hours, int minutes)
        {
            return new XrmTimeSpan(days, hours, minutes);
        }

        //
        // Summary:
        //     Creates an instance of a XrmTimeSpan class setting the years, months, days, hours
        //     and minutes.
        //
        // Parameters:
        //   years:
        //     Type: Int32. Specifies the years for the time span.
        //
        //   months:
        //     Type: Int32. Specifies the months for the time span.
        //
        //   days:
        //     Type: Int32. Specifies the days for the time span.
        //
        //   hours:
        //     Type: Int32. Specifies the hours for the time span.
        //
        //   minutes:
        //     Type: Int32. Specifies the minutes for the time span.
        //
        // Returns:
        //     Type: Microsoft.Xrm.Sdk.Workflow.XrmTimeSpan The instance of a XrmTimeSpan class
        //     where the years, months, days, hours, and minutes are set.
        public static XrmTimeSpan CreateXrmTimeSpan(int years, int months, int days, int hours, int minutes)
        {
            return new XrmTimeSpan(years, months, days, hours, minutes);
        }

        //
        // Summary:
        //     Creates an instance of a XrmTimeSpan class setting the time span members.
        //
        // Parameters:
        //   cts:
        //     Type: Microsoft.Xrm.Sdk.Workflow.XrmTimeSpan. Specifies the time span.
        //
        // Returns:
        //     Type: Microsoft.Xrm.Sdk.Workflow.XrmTimeSpan The instance of a XrmTimeSpan class
        //     where the time span members are set.
        public static XrmTimeSpan CreateXrmTimeSpan(XrmTimeSpan cts)
        {
            return new XrmTimeSpan(cts.Years, cts.Months, cts.Days, cts.Hours, cts.Minutes);
        }

        //
        // Summary:
        //     Returns a value indicating whether this instance is equal to a specified XrmTimeSpan
        //     object.
        //
        // Parameters:
        //   value:
        //     Type: Microsoft.Xrm.Sdk.Workflow.XrmTimeSpan. Specifies the XrmTimeSpan instance
        //     to test for equality with the current instance.
        //
        // Returns:
        //     Type: Boolean true if the value represents the same time interval as the current
        //     XrmTimeSpan structure; otherwise, false.
        public bool Equals(XrmTimeSpan value)
        {
            return value.Years == Years && value.Months == Months && value.Days == Days && value.Hours == Hours && value.Minutes == Minutes;
        }

        //
        // Summary:
        //     Returns a value indicating whether this instance is equal to a specified object.
        //
        // Parameters:
        //   obj:
        //     Type: Object. Specifies the object to test for equality with the current instance.
        //
        // Returns:
        //     Type: Boolean true if value is a XrmTimeSpan object that represents the same
        //     time interval as the current XrmTimeSpan structure; otherwise, false.
        public override bool Equals(object obj)
        {
            return Equals((XrmTimeSpan)obj);
        }

        //
        // Summary:
        //     Returns a hash code for this instance.
        //
        // Returns:
        //     Type: Int32 The hash code for this instance.
        public override int GetHashCode()
        {
            return Years ^ Months ^ Days ^ Hours ^ Minutes;
        }

        //
        // Summary:
        //     Indicates whether two XrmTimeSpan instances are equal.
        //
        // Parameters:
        //   span1:
        //     Type: Microsoft.Xrm.Sdk.Workflow.XrmTimeSpan. Specifies a XrmTimeSpan.
        //
        //   span2:
        //     Type: Microsoft.Xrm.Sdk.Workflow.XrmTimeSpan. Specifies a XrmTimeSpan.
        //
        // Returns:
        //     Type: Boolean true if the values of span1 and span2 are equal; otherwise, false.
        public static bool operator ==(XrmTimeSpan span1, XrmTimeSpan span2)
        {
            return span1.Equals(span2);
        }

        //
        // Summary:
        //     Indicates whether two XrmTimeSpan instances are not equal.
        //
        // Parameters:
        //   span1:
        //     Type: Microsoft.Xrm.Sdk.Workflow.XrmTimeSpan. Specifies a XrmTimeSpan.
        //
        //   span2:
        //     Type: Microsoft.Xrm.Sdk.Workflow.XrmTimeSpan. Specifies a XrmTimeSpan.
        //
        // Returns:
        //     Type: Boolean true if the values of span1 and span2 are not equal; otherwise,
        //     false.
        public static bool operator !=(XrmTimeSpan span1, XrmTimeSpan span2)
        {
            return !(span1 == span2);
        }

        //
        // Summary:
        //     Adds the specified date/time value to this instance.
        //
        // Parameters:
        //   value:
        //     Type: DateTime. A date/time value to add to the current instance value.
        //
        // Returns:
        //     Type: DateTime The resultant date and time value.
        public DateTime Add(DateTime value)
        {
            return value.AddYears(Years).AddMonths(Months).AddDays(Days)
                .AddHours(Hours)
                .AddMinutes(Minutes);
        }

        //
        // Summary:
        //     Subtracts the specified XrmTimeSpan from this instance.
        //
        // Parameters:
        //   value:
        //     Type: DateTime. Specifies a date/time value to subtract.
        //
        // Returns:
        //     Type: DateTime The value resulting when the passed value is subtracted from this
        //     instance.
        public DateTime Subtract(DateTime value)
        {
            return value.AddYears(-1 * Years).AddMonths(-1 * Months).AddDays(-1 * Days)
                .AddHours(-1 * Hours)
                .AddMinutes(-1 * Minutes);
        }
    }
}
#endif
