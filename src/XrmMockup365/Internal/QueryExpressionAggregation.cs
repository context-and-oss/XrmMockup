using DG.Tools.XrmMockup.Database;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Metadata;
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
            internal string ExchangeRateSourceName;
            internal AttributeMetadata Metadata;
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
                ExchangeRateSourceName = alias == null ? "exchangerate" : alias + ".exchangerate",
                Expression = expression
            });
        }

        internal static void Validate(QueryExpression query, IDictionary<string, EntityMetadata> entityMetadata)
        {
            ValidateColumnSet(query.ColumnSet);
            foreach (var link in query.LinkEntities) ValidateLinkColumns(link);
            var columns = GetColumns(query).ToArray();
            var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in columns)
            {
                var expression = column.Expression;
                if (!entityMetadata.TryGetValue(column.EntityName, out var entity) ||
                    !entity.Attributes.Any(attribute => attribute.LogicalName == expression.AttributeName))
                {
                    var entityName = entity?.SchemaName ?? column.EntityName;
                    throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(),
                        $"'{entityName}' entity doesn't contain attribute with Name = '{expression.AttributeName}' and NameMapping = 'Logical' (look up attribute by name is case-sensitive)");
                }
                if (string.IsNullOrWhiteSpace(expression.Alias) || !aliases.Add(expression.Alias))
                    throw new FaultException("Aggregate columns must have unique, non-empty aliases.");
                if (expression.HasGroupBy != (expression.AggregateType == XrmAggregateType.None))
                    throw new FaultException("Each aggregate column must specify an aggregate type or be a group-by column.");
                if (expression.DateTimeGrouping != XrmDateTimeGrouping.None)
                    throw new NotImplementedException("Date grouping in aggregate queries is not supported.");
            }
        }

        internal static IEnumerable<Entity> Aggregate(QueryExpression query, IEnumerable<Entity> source, XrmDb db, IDictionary<string, EntityMetadata> entityMetadata)
        {
            var columns = GetColumns(query).ToArray();
            foreach (var column in columns)
                column.Metadata = entityMetadata[column.EntityName].Attributes.First(attribute => attribute.LogicalName == column.Expression.AttributeName);
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
                // Lookup names must be available both in the returned aliases and before ordering.
                Utility.PopulateEntityReferenceNames(result, db);
                return result;
            });

            IOrderedEnumerable<Entity> ordered = null;
            foreach (var order in query.Orders)
            {
                var alias = string.IsNullOrEmpty(order.Alias) ? order.AttributeName : order.Alias;
                Func<Entity, object> selector = row => row.Attributes.TryGetValue(alias, out var value)
                    ? Utility.GetComparableAttribute(value is AliasedValue aliased ? aliased.Value : value) : null;
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
            return GetValue(row, column.SourceName);
        }

        private static object GetValue(Entity row, string sourceName)
        {
            if (!row.Attributes.TryGetValue(sourceName, out var value)) return null;
            return value is AliasedValue aliased ? aliased.Value : value;
        }

        private static object Calculate(Entity[] rows, AggregateColumn column)
        {
            if (column.Expression.AggregateType == XrmAggregateType.Count) return rows.Length;
            var values = rows.Select(row => GetValue(row, column)).Where(value => value != null).ToArray();
            if (column.Expression.AggregateType == XrmAggregateType.CountColumn) return values.Length;
            if (values.Length == 0) return null;

            values = rows.Select(row => NormalizeMoney(row, column)).Where(value => value != null).ToArray();
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

        private static object NormalizeMoney(Entity row, AggregateColumn column)
        {
            var value = GetValue(row, column);
            if (!(value is Money money) || column.Expression.AttributeName.EndsWith("_base", StringComparison.Ordinal))
                return value;

            // Persisted base values retain the record's historical rate and currency precision.
            // Computed money was just projected, so a stored base value could be stale or absent.
            var isComputed = column.Metadata.SourceType == (int)SourceType.CalculatedAttribute ||
                column.Metadata.SourceType == (int)SourceType.FormulaAttribute;
            if (!isComputed && GetValue(row, column.SourceName + "_base") is Money baseMoney)
                return baseMoney;

            // A calculated zero may exist on a row with no transaction currency at all.
            if (money.Value == 0) return new Money(0);
            if (!(GetValue(row, column.ExchangeRateSourceName) is decimal rate) || rate <= 0)
                throw new FaultException($"Cannot aggregate money column '{column.SourceName}' without a positive exchange rate.");
            return new Money(money.Value / rate);
        }

        private sealed class GroupKeyComparer : IEqualityComparer<object[]>
        {
            public bool Equals(object[] x, object[] y)
            {
                if (x.Length != y.Length) return false;
                for (var i = 0; i < x.Length; i++)
                {
                    var equal = x[i] is string first && y[i] is string second
                        ? StringComparer.OrdinalIgnoreCase.Equals(first, second)
                        : object.Equals(x[i], y[i]);
                    if (!equal) return false;
                }
                return true;
            }

            public int GetHashCode(object[] values)
            {
                unchecked
                {
                    return values.Aggregate(17, (hash, value) => hash * 31 + (value is string text ? StringComparer.OrdinalIgnoreCase.GetHashCode(text) : value?.GetHashCode() ?? 0));
                }
            }
        }
    }
}
