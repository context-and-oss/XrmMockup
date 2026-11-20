using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WorkflowExecuter {
    internal static class Utility {
        internal static string TrimEdge(this string value) {
            return value.Substring(1, value.Length - 2);
        }

        internal static int GetDiffMonths(DateTime from, DateTime to) {
            if (from > to) return GetDiffMonths(to, from);
            var monthDiff = Math.Abs((to.Year * 12 + (to.Month - 1)) - (from.Year * 12 + (from.Month - 1)));
            return monthDiff - (from.AddMonths(monthDiff) > to || to.Day < from.Day ? 1 : 0);
        }

        internal static int GetDiffYears(DateTime from, DateTime to) {
            if (from > to) return GetDiffYears(to, from);
            return (to.Year - from.Year - 1) +
                (((to.Month > from.Month) ||
                ((to.Month == from.Month) && (to.Day >= from.Day))) ? 1 : 0);
        }

        internal static object ToCorrectType(this string variable, Dictionary<string, object> variables, TimeSpan timeOffset) {

            if (variable.Contains("New Entity(")) {
                var regex = new Regex(@"\(.+\)");
                var logicalname = regex.Match(variable).Value.Replace("\"", "").TrimEdge();
                return new Entity(logicalname);
            }

            if (variable.Contains(".Add(")) {
                var regex = new Regex(@"\(.+\)");
                var date = (DateTime)regex.Match(variable).Value.TrimEdge().ToCorrectType(variables, timeOffset);
                var timespan = (XrmTimeSpan)variable.Split('.')[0].ToCorrectType(variables, timeOffset);
                return date.AddXrmTimeSpan(timespan);
            }

            if (variable.Contains(".Subtract(")) {
                var regex = new Regex(@"\(.+\)");
                var date = (DateTime)regex.Match(variable).Value.TrimEdge().ToCorrectType(variables, timeOffset);
                var timespan = (XrmTimeSpan)variable.Split('.')[0].ToCorrectType(variables, timeOffset);
                return date.SubtractXrmTimeSpan(timespan);
            }

            if (variable.Contains(".ToString()")) {
                var variableName = variable.Replace(".ToString()", "");
                return variables[variableName].ToString();
            }

            if (variable.Contains(".ToUniversalTime()")) {
                var time = (DateTime)ToCorrectType(variable.Replace(".ToUniversalTime()", ""), variables, timeOffset);
                return time.ToUniversalTime();
            }

            if (variable.Contains("DirectCast(")) {
                var regex = new Regex(@"\(.+\)");
                var parameters = regex.Match(variable).Value.TrimEdge().Split(',').Select(s => s.Trim()).ToArray();
                var toBeTypedVariable = variables[parameters[0]];
                switch (parameters[1]) {
                    case "Microsoft.Xrm.Sdk.EntityReference":
                        if (toBeTypedVariable is EntityReference) {
                            return toBeTypedVariable;
                        }

                        if (toBeTypedVariable is Entity) {
                            return (toBeTypedVariable as Entity).ToEntityReference();
                        }

                        throw new NotImplementedException("Unknown type, direct cast to type entityreference");

                    case "System.DateTime":
                        return (DateTime)toBeTypedVariable;

                    default:
                        throw new NotImplementedException($"Unknown cast type '{parameters[1]}' in direct cast in workflow");
                }
            }

            if (variable == "DateTime.MaxValue") {
                return DateTime.MaxValue;
            }

            if (variable == "DateTime.MinValue") {
                return DateTime.MinValue;
            }

            if (variable == "DateTime.UtcNow") {
                return DateTime.UtcNow.Add(timeOffset);
            }

            if (variables.ContainsKey(variable)) {
                return variables[variable];
            }

            return null;
        }

        internal static DateTime AddXrmTimeSpan(this DateTime date, XrmTimeSpan timespan) {
            date = date.AddDays(timespan.Days);
            date = date.AddHours(timespan.Hours);
            date = date.AddMinutes(timespan.Minutes);
            date = date.AddYears(timespan.Years);
            date = date.AddMonths(timespan.Months);
            return date;
        }

        internal static DateTime SubtractXrmTimeSpan(this DateTime date, XrmTimeSpan timespan) {
            date = date.AddDays(-timespan.Days);
            date = date.AddHours(-timespan.Hours);
            date = date.AddMinutes(-timespan.Minutes);
            date = date.AddYears(-timespan.Years);
            date = date.AddMonths(-timespan.Months);
            return date;
        }
    }


}
