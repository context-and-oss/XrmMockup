using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;

namespace DG.Tools.XrmMockup.Internal
{
    internal static class QueryExpressionAggregation
    {
        private sealed class AggregateColumn
        {
            internal string EntityName;
            internal string SourceName;
            internal XrmAttributeExpression Expression;
        }

        internal static bool IsAggregate(QueryExpression query)
        {
            return GetColumns(query).Any(c => c.Expression.AggregateType != XrmAggregateType.None || c.Expression.HasGroupBy);
        }

        private static IEnumerable<AggregateColumn> GetColumns(QueryExpression query)
        {
            return GetColumns(query.ColumnSet, query.EntityName, null)
                .Concat(query.LinkEntities.SelectMany(GetLinkColumns));
        }

        private static IEnumerable<AggregateColumn> GetLinkColumns(LinkEntity link)
        {
            return GetColumns(link.Columns, link.LinkToEntityName, link.EntityAlias)
                .Concat(link.LinkEntities.SelectMany(GetLinkColumns));
        }

        private static IEnumerable<AggregateColumn> GetColumns(ColumnSet columns, string entityName, string alias)
        {
            if (columns == null) return Enumerable.Empty<AggregateColumn>();
            return columns.AttributeExpressions.Select(expression => new AggregateColumn
            {
                EntityName = entityName,
                SourceName = alias == null ? expression.AttributeName : alias + "." + expression.AttributeName,
                Expression = expression
            });
        }

        internal static void Validate(QueryExpression query)
        {
            ValidateColumnSet(query.ColumnSet);
            foreach (var link in query.LinkEntities) ValidateLinkColumns(link);
            var columns = GetColumns(query).ToArray();
            var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in columns)
            {
                var expression = column.Expression;
                if (string.IsNullOrWhiteSpace(expression.Alias) || !aliases.Add(expression.Alias))
                    throw new FaultException("Aggregate columns must have unique, non-empty aliases.");
                if (expression.HasGroupBy != (expression.AggregateType == XrmAggregateType.None))
                    throw new FaultException("Each aggregate column must specify an aggregate type or be a group-by column.");
                if (expression.DateTimeGrouping != XrmDateTimeGrouping.None)
                    throw new NotImplementedException("Date grouping in aggregate queries is not supported.");
            }
        }

        internal static IEnumerable<Entity> Aggregate(QueryExpression query, IEnumerable<Entity> source)
        {
            var columns = GetColumns(query).ToArray();
            var rows = source.ToArray();
            var groupColumns = columns.Where(c => c.Expression.HasGroupBy).ToArray();
            // Without grouping, even an empty input yields one row (with a count of zero).
            var groups = groupColumns.Length == 0
                ? new[] { rows.AsEnumerable() }
                : rows.GroupBy(row => groupColumns.Select(c => ValueConverter.ConvertToComparableObject(GetValue(row, c))).ToArray(),
                    new GroupKeyComparer()).Select(group => group.AsEnumerable());

            IEnumerable<Entity> results = groups.Select(group =>
            {
                var groupRows = group.ToArray();
                var result = new Entity(query.EntityName);
                foreach (var column in columns)
                {
                    var value = column.Expression.HasGroupBy
                        ? GetValue(groupRows[0], column)
                        : Calculate(groupRows, column);
                    if (value != null)
                        result[column.Expression.Alias] = new AliasedValue(column.EntityName, column.Expression.AttributeName, value);
                }
                return result;
            });

            IOrderedEnumerable<Entity> ordered = null;
            foreach (var order in query.Orders)
            {
                var alias = string.IsNullOrEmpty(order.Alias) ? order.AttributeName : order.Alias;
                Func<Entity, object> selector = row => row.Attributes.TryGetValue(alias, out var value)
                    ? ValueConverter.ConvertToComparableObject(value) : null;
                ordered = ordered == null
                    ? (order.OrderType == OrderType.Ascending ? results.OrderBy(selector) : results.OrderByDescending(selector))
                    : (order.OrderType == OrderType.Ascending ? ordered.ThenBy(selector) : ordered.ThenByDescending(selector));
            }
            return ordered ?? results;
        }

        private static void ValidateColumnSet(ColumnSet columns)
        {
            if (columns != null && (columns.AllColumns || columns.Columns.Count > 0))
                throw new FaultException("Aggregate queries must select columns using AttributeExpressions only.");
        }

        private static void ValidateLinkColumns(LinkEntity link)
        {
            ValidateColumnSet(link.Columns);
            foreach (var nested in link.LinkEntities) ValidateLinkColumns(nested);
        }

        private static object GetValue(Entity row, AggregateColumn column)
        {
            if (!row.Attributes.TryGetValue(column.SourceName, out var value)) return null;
            return value is AliasedValue aliased ? aliased.Value : value;
        }

        private static object Calculate(Entity[] rows, AggregateColumn column)
        {
            if (column.Expression.AggregateType == XrmAggregateType.Count) return rows.Length;
            var values = rows.Select(row => GetValue(row, column)).Where(value => value != null).ToArray();
            if (column.Expression.AggregateType == XrmAggregateType.CountColumn) return values.Length;
            if (values.Length == 0) return null;

            var comparable = values.Select(ValueConverter.ConvertToComparableObject).ToArray();
            switch (column.Expression.AggregateType)
            {
                case XrmAggregateType.Min:
                case XrmAggregateType.Max:
                    var sorted = values.OrderBy(ValueConverter.ConvertToComparableObject);
                    return column.Expression.AggregateType == XrmAggregateType.Min ? sorted.First() : sorted.Last();
                case XrmAggregateType.Sum:
                case XrmAggregateType.Avg:
                    var total = comparable.Sum(value => Convert.ToDecimal(value, CultureInfo.InvariantCulture));
                    if (column.Expression.AggregateType == XrmAggregateType.Avg) total /= values.Length;
                    if (values[0] is Money) return new Money(total);
                    if (values[0] is double) return (double)total;
                    if (column.Expression.AggregateType == XrmAggregateType.Avg) return total;
                    return Convert.ChangeType(total, comparable[0].GetType(), CultureInfo.InvariantCulture);
                default:
                    throw new NotImplementedException($"Aggregate type '{column.Expression.AggregateType}' is not supported.");
            }
        }

        private sealed class GroupKeyComparer : IEqualityComparer<object[]>
        {
            public bool Equals(object[] x, object[] y)
            {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(object[] values)
            {
                unchecked
                {
                    return values.Aggregate(17, (hash, value) => hash * 31 + (value?.GetHashCode() ?? 0));
                }
            }
        }
    }
}
