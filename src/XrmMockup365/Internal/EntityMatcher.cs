using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.Tools.XrmMockup.Internal
{
    internal class EntityMatcher
    {
        public static bool MatchesCriteria(Entity row, FilterExpression criteria)
        {
            if (criteria.FilterOperator == LogicalOperator.And)
                return criteria.Filters.All(f =>
                    MatchesCriteria(row, f)) &&
                    criteria.Conditions.All(c => EvaluateCondition(row, c));
            else
                return criteria.Filters.Any(f =>
                    MatchesCriteria(row, f)) ||
                    criteria.Conditions.Any(c => EvaluateCondition(row, c));
        }

        private static bool EvaluateCondition(Entity row, ConditionExpression condition)
        {
            object attr = null;
            switch (condition)
            {
                case var c when condition.CompareColumns == true:
                    if (!row.Contains(condition.AttributeName))
                        return false;
                    var columnAttr = condition.Values.FirstOrDefault() as string;
                    if (columnAttr == null)
                        throw new NotImplementedException("CompareColumns only supports string values");
                    if (!row.Contains(columnAttr))
                        return false;

                    return Matches(row[condition.AttributeName], c.Operator, new[] { row[columnAttr] });

                case var c when condition.AttributeName == null:
                    return Matches(row.Id, condition.Operator, condition.Values);

                case var c when condition.EntityName != null:
                    var key = $"{condition.EntityName}.{condition.AttributeName}";
                    if (row != null && row.Contains(key))
                    {
                        attr = row[key];
                    }
                    else if (row != null && row.Contains(condition.AttributeName))
                    {
                        attr = row[condition.AttributeName];
                    }
                    break;

                default:
                    if (row.Contains(condition.AttributeName))
                    {
                        attr = row[condition.AttributeName];
                    }
                    break;
            }

            attr = ValueConverter.ConvertToComparableObject(attr);
            var values = condition.Values.Select(ValueConverter.ConvertToComparableObject);
            return Matches(attr, condition.Operator, values);
        }

        private static bool Matches(object attr, ConditionOperator op, IEnumerable<object> values)
        {
            switch (op)
            {
                case ConditionOperator.Null:
                    return attr == null;

                case ConditionOperator.NotNull:
                    return attr != null;

                case ConditionOperator.Equal:
                    return IsEqual(attr, values);

                case ConditionOperator.NotEqual:
                    // Dataverse excludes null-valued records for "not" conditions (NULL <> value is unknown).
                    if (attr == null) return false;
                    return !Matches(attr, ConditionOperator.Equal, values);

                case ConditionOperator.GreaterThan:
                case ConditionOperator.GreaterEqual:
                case ConditionOperator.LessEqual:
                case ConditionOperator.LessThan:
                    return Compare((IComparable)attr, op, (IComparable)ValueConverter.ConvertTo(values.First(), attr?.GetType()));

                case ConditionOperator.NotLike:
                    if (attr == null) return false;
                    return !Matches(attr, ConditionOperator.Like, values);

                case ConditionOperator.Like:
                    if (attr == null)
                        return false;
                    var sAttr = (string)attr;
                    var pattern = (string)values.First();
                    if (pattern.First() == '%' && pattern.Last() == '%')
                    {
                        return sAttr.Contains(pattern.Substring(1, pattern.Length - 2));
                    }
                    else if (pattern.First() == '%')
                    {
                        return sAttr.EndsWith(pattern.Substring(1));
                    }
                    else if (pattern.Last() == '%')
                    {
                        return sAttr.StartsWith(pattern.Substring(0, pattern.Length - 1));
                    }
                    else
                    {
                        throw new NotImplementedException($"The like matching for '{pattern}' has not been implemented yet");
                    }

                case ConditionOperator.NextXYears:
                case ConditionOperator.OlderThanXYears:
                case ConditionOperator.OlderThanXWeeks:
                case ConditionOperator.OlderThanXMonths:
                case ConditionOperator.OlderThanXDays:
                case ConditionOperator.OlderThanXHours:
                case ConditionOperator.OlderThanXMinutes:
                case ConditionOperator.Yesterday:
                case ConditionOperator.Today:
                case ConditionOperator.Tomorrow:
                    return DateTimeCompareNow(attr, op, values);

                case ConditionOperator.In:
                    if (values != null && attr is OptionSetValueCollection optionSetValueCollection)
                    {
                        return string.Join(",", values.OrderBy(_ => _)) == string.Join(",", optionSetValueCollection.ToList().Select(_ => _.Value).OrderBy(_ => _));
                    }
                    return values.Contains(ValueConverter.ConvertTo(attr, values.FirstOrDefault()?.GetType()));

                case ConditionOperator.NotIn:
                    // Only exclude nulls when there are values to compare; an empty NotIn matches all rows.
                    if (attr == null && values.Any()) return false;
                    return !Matches(attr, ConditionOperator.In, values);

                case ConditionOperator.BeginsWith:
                    if (attr == null) return false;

                    if (attr.GetType() == typeof(string))
                    {
                        return (attr as string).StartsWith((string)ValueConverter.ConvertTo(values.First(), attr?.GetType()), StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        throw new NotImplementedException($"The ConditionOperator '{op}' is not valid for anything other than string yet.");
                    }

                case ConditionOperator.DoesNotBeginWith:
                    if (attr == null) return false;
                    return !Matches(attr, ConditionOperator.BeginsWith, values);

                case ConditionOperator.EndsWith:
                    if (attr == null) return false;

                    if (attr.GetType() == typeof(string))
                    {
                        return (attr as string).EndsWith((string)ValueConverter.ConvertTo(values.First(), attr?.GetType()), StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        throw new NotImplementedException($"The ConditionOperator '{op}' is not valid for anything other than string yet.");
                    }

                case ConditionOperator.DoesNotEndWith:
                    if (attr == null) return false;
                    return !Matches(attr, ConditionOperator.EndsWith, values);
                default:
                    throw new NotImplementedException($"The ConditionOperator '{op}' has not been implemented yet.");
            }
        }

        private static bool IsEqual(object attr, IEnumerable<object> values)
        {
            if (attr == null) return false;

            if (attr is string attrStr)
            {
                var valueString = (string)ValueConverter.ConvertTo(values.First(), attr?.GetType());
                return attrStr.Equals(valueString, StringComparison.OrdinalIgnoreCase);
            }
            else if (attr is DateTime attrDate)
            {
                var valueDate = (DateTime)ValueConverter.ConvertTo(values.First(), attr?.GetType());
                return DateTime.Equals(valueDate.ToUniversalTime(), attrDate.ToUniversalTime());
            }
            else
            {
                var converted = ValueConverter.ConvertTo(values.First(), attr?.GetType());
                return Equals(converted, attr);
            }
        }

        private static bool DateTimeCompareNow(object attr, ConditionOperator op, IEnumerable<object> values)
        {
            var now = DateTime.UtcNow;
            var nowDate = now.Date;
            var date = attr is DateTime dateTime ? dateTime : DateTime.Parse((string)attr);

            switch (op)
            {
                case ConditionOperator.NextXYears:
                    return now.Date <= date.Date && date.Date <= now.AddYears(int.Parse((string)values.First())).Date;
                case ConditionOperator.OlderThanXYears:
                    return date <= now.AddYears(-int.Parse((string)values.First()));
                case ConditionOperator.OlderThanXWeeks:
                    return date <= now.AddDays(-7 * int.Parse((string)values.First()));
                case ConditionOperator.OlderThanXMonths:
                    return date <= now.AddMonths(-int.Parse((string)values.First()));
                case ConditionOperator.OlderThanXDays:
                    return date <= now.AddDays(-int.Parse((string)values.First()));
                case ConditionOperator.OlderThanXHours:
                    return date <= now.AddHours(-int.Parse((string)values.First()));
                case ConditionOperator.OlderThanXMinutes:
                    return date <= now.AddMinutes(-int.Parse((string)values.First()));
                case ConditionOperator.Yesterday:
                    return date.Date == nowDate.AddDays(-1).Date;
                case ConditionOperator.Today:
                    return date.Date == nowDate;
                case ConditionOperator.Tomorrow:
                    return date.Date == nowDate.AddDays(1).Date;

                default:
                    throw new MockupException("Invalid operator: " + op);
            }
        }

        public static bool Compare(IComparable attr, ConditionOperator op, IComparable value)
        {
            // if at least one of the two compare values are null. Then compare returns null
            if (attr == null || value == null)
                return false;
            switch (op)
            {
                case ConditionOperator.GreaterEqual:
                    return attr.CompareTo(value) >= 0;
                case ConditionOperator.GreaterThan:
                    return attr.CompareTo(value) > 0;
                case ConditionOperator.LessEqual:
                    return attr.CompareTo(value) <= 0;
                case ConditionOperator.LessThan:
                    return attr.CompareTo(value) < 0;

                default:
                    throw new MockupException("Invalid state.");
            }
        }
    }
}
