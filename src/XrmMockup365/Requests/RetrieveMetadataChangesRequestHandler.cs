using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tools.XrmMockup.Database;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;

namespace DG.Tools.XrmMockup
{
    internal class RetrieveMetadataChangesRequestHandler : RequestHandler
    {
        internal RetrieveMetadataChangesRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RetrieveMetadataChanges") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<RetrieveMetadataChangesRequest>(orgRequest);
            var query = request.Query;

            IEnumerable<EntityMetadata> results = metadata.EntityMetadata.Values;

            // Apply filter criteria if provided
            if (query?.Criteria?.Conditions != null && query.Criteria.Conditions.Count > 0)
            {
                foreach (var condition in query.Criteria.Conditions)
                {
                    results = ApplyCondition(results, condition);
                }
            }

            var resp = new RetrieveMetadataChangesResponse();
            var col = new EntityMetadataCollection();
            col.AddRange(results);
            resp.Results["EntityMetadata"] = col;

            return resp;
        }

        private static IEnumerable<EntityMetadata> ApplyCondition(
            IEnumerable<EntityMetadata> entities,
            MetadataConditionExpression condition)
        {
            switch (condition.PropertyName)
            {
                case "LogicalName":
                    return ApplyStringCondition(entities, e => e.LogicalName, condition);
                case "MetadataId":
                    return ApplyGuidCondition(entities, e => e.MetadataId, condition);
                case "ObjectTypeCode":
                    return ApplyIntCondition(entities, e => e.ObjectTypeCode, condition);
                default:
                    return entities; // Unknown property, return all
            }
        }

        private static IEnumerable<EntityMetadata> ApplyStringCondition(
            IEnumerable<EntityMetadata> entities,
            Func<EntityMetadata, string> selector,
            MetadataConditionExpression condition)
        {
            var values = GetStringValues(condition.Value);
            switch (condition.ConditionOperator)
            {
                case MetadataConditionOperator.Equals:
                    return entities.Where(e => string.Equals(selector(e), condition.Value?.ToString(), StringComparison.OrdinalIgnoreCase));
                case MetadataConditionOperator.NotEquals:
                    return entities.Where(e => !string.Equals(selector(e), condition.Value?.ToString(), StringComparison.OrdinalIgnoreCase));
                case MetadataConditionOperator.In:
                    return entities.Where(e => values.Contains(selector(e)));
                case MetadataConditionOperator.NotIn:
                    return entities.Where(e => !values.Contains(selector(e)));
                default:
                    return entities;
            }
        }

        private static IEnumerable<EntityMetadata> ApplyGuidCondition(
            IEnumerable<EntityMetadata> entities,
            Func<EntityMetadata, Guid?> selector,
            MetadataConditionExpression condition)
        {
            var values = GetGuidValues(condition.Value);
            switch (condition.ConditionOperator)
            {
                case MetadataConditionOperator.Equals:
                    return entities.Where(e => selector(e) == GetGuidValue(condition.Value));
                case MetadataConditionOperator.NotEquals:
                    return entities.Where(e => selector(e) != GetGuidValue(condition.Value));
                case MetadataConditionOperator.In:
                    return entities.Where(e => values.Contains(selector(e) ?? Guid.Empty));
                case MetadataConditionOperator.NotIn:
                    return entities.Where(e => !values.Contains(selector(e) ?? Guid.Empty));
                default:
                    return entities;
            }
        }

        private static IEnumerable<EntityMetadata> ApplyIntCondition(
            IEnumerable<EntityMetadata> entities,
            Func<EntityMetadata, int?> selector,
            MetadataConditionExpression condition)
        {
            var values = GetIntValues(condition.Value);
            switch (condition.ConditionOperator)
            {
                case MetadataConditionOperator.Equals:
                    return entities.Where(e => selector(e) == GetIntValue(condition.Value));
                case MetadataConditionOperator.NotEquals:
                    return entities.Where(e => selector(e) != GetIntValue(condition.Value));
                case MetadataConditionOperator.In:
                    return entities.Where(e => values.Contains(selector(e) ?? 0));
                case MetadataConditionOperator.NotIn:
                    return entities.Where(e => !values.Contains(selector(e) ?? 0));
                default:
                    return entities;
            }
        }

        private static HashSet<string> GetStringValues(object value)
        {
            if (value is string[] stringArray)
                return new HashSet<string>(stringArray, StringComparer.OrdinalIgnoreCase);
            if (value is IEnumerable<string> stringEnumerable)
                return new HashSet<string>(stringEnumerable, StringComparer.OrdinalIgnoreCase);
            if (value is string singleString)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { singleString };
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private static Guid? GetGuidValue(object value)
        {
            if (value is Guid guid) return guid;
            if (value is string str && Guid.TryParse(str, out var parsed)) return parsed;
            return null;
        }

        private static HashSet<Guid> GetGuidValues(object value)
        {
            if (value is Guid[] guidArray)
                return new HashSet<Guid>(guidArray);
            if (value is IEnumerable<Guid> guidEnumerable)
                return new HashSet<Guid>(guidEnumerable);
            if (value is Guid singleGuid)
                return new HashSet<Guid> { singleGuid };
            return new HashSet<Guid>();
        }

        private static int? GetIntValue(object value)
        {
            if (value is int i) return i;
            if (value is string str && int.TryParse(str, out var parsed)) return parsed;
            return null;
        }

        private static HashSet<int> GetIntValues(object value)
        {
            if (value is int[] intArray)
                return new HashSet<int>(intArray);
            if (value is IEnumerable<int> intEnumerable)
                return new HashSet<int>(intEnumerable);
            if (value is int singleInt)
                return new HashSet<int> { singleInt };
            return new HashSet<int>();
        }
    }
}
