using DG.Tools.XrmMockup.Database;
using DG.Tools.XrmMockup.Internal;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace DG.Tools.XrmMockup
{
    internal class RetrieveMultipleRequestHandler : RequestHandler
    {
        internal RetrieveMultipleRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RetrieveMultiple") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<RetrieveMultipleRequest>(orgRequest);
            var queryExpr = request.Query as QueryExpression;
            var fetchExpr = request.Query as FetchExpression;
            var queryByAttr = request.Query as QueryByAttribute;
            if (fetchExpr != null)
            {
                queryExpr = XmlHandling.FetchXmlToQueryExpression(fetchExpr.Query);
            }
            else if (queryByAttr != null)
            {
                queryExpr = Utility.QueryByAttributeToQueryExpression(queryByAttr);
            }

            if (queryExpr.EntityName == null)
            {
                throw new FaultException("The 'RetrieveMultiple' method does not support entities of type 'none'");
            }

            FillAliasIfEmpty(queryExpr);

            var isAggregate = QueryExpressionAggregation.IsAggregate(queryExpr);
            if (isAggregate) QueryExpressionAggregation.Validate(queryExpr, metadata.EntityMetadata);

            // Reject up-front any filter that references an attribute that does not exist on the
            // targeted entity, mirroring Dataverse (which faults instead of silently ignoring the
            // condition). Run after FillAliasIfEmpty so link-criteria conditions carry their alias.
            ValidateFilterAttributes(queryExpr);

            db.PrefillDBWithOnlineData(queryExpr);
            // Create a snapshot for thread-safe enumeration during calculated field execution
            var rows = db.GetDBEntityRows(queryExpr.EntityName).ToList();

            var entityMetadata = metadata.EntityMetadata[queryExpr.EntityName];

            // Calculated fields must be projected onto each row before criteria matching, so the set
            // of "needed" calc columns is the union of the ColumnSet and any column referenced by the
            // filter/order. Formula fields are only used after filtering, so they only need to honour
            // the ColumnSet. A null set means "all columns" (e.g. ColumnSet(true) or an unbounded
            // query); when populated, unrequested computed columns are skipped to avoid paying their
            // workflow cost and to stop a broken calc field on one column from blocking every read.
            var calculatedNeeded = GetNeededAttributesForCalculatedFields(queryExpr);
            var formulaNeeded = GetRequestedAttributes(queryExpr.ColumnSet);

            var collection = new ConcurrentBag<KeyValuePair<DbRow, Entity>>();

            Parallel.ForEach(rows, row =>
            {
                var entity = row.ToEntity();

                // Calculated fields are evaluated on demand (never persisted); project them onto the
                // in-memory entity before criteria matching so filters can reference them.
                core.ExecuteCalculatedFields(row.Metadata, entity, calculatedNeeded);

                var toAdd = core.GetStronglyTypedEntity(entity, row.Metadata, null);

                if (queryExpr.LinkEntities.Count > 0)
                {
                    // Every top-level link must be satisfied simultaneously (AND / cross join),
                    // mirroring Dataverse inner-join semantics. Computing each link independently
                    // and unioning the results would instead OR the links together, letting a
                    // parent through if it matched any single link. Compute each link's aliased
                    // results, then cross-join them so a parent is only returned when all links
                    // match, with each result row carrying every link's aliased attributes.
                    var perLinkResults = queryExpr.LinkEntities
                        .Select(linkEntity => GetAliasedValuesFromLinkentity(linkEntity, entity, toAdd, db))
                        .ToList();
                    var matchingValues = CombineLinkResults(toAdd, perLinkResults)
                        .Where(e => EntityMatcher.MatchesCriteria(e, queryExpr.Criteria));
                    foreach (var m in matchingValues)
                    {
                        if (security.HasPermission(m, AccessRights.ReadAccess, userRef))
                        {
                            Utility.SetFormattedValues(db, m, entityMetadata);
                            collection.Add(new KeyValuePair<DbRow, Entity>(row, m));
                        }
                    }
                }
                else if (EntityMatcher.MatchesCriteria(toAdd, queryExpr.Criteria))
                {
                    if (security.HasPermission(toAdd, AccessRights.ReadAccess, userRef))
                    {
                        Utility.SetFormattedValues(db, toAdd, entityMetadata);
                        collection.Add(new KeyValuePair<DbRow, Entity>(row, toAdd));
                    }
                }
            });

            IEnumerable<Entity> entitiesToReturn;
            if (isAggregate)
            {
                // Aggregate the filtered, security-trimmed rows before projecting columns or
                // limiting the result set. Aggregate rows have aliases only, not record IDs.
                var aggregateRows = collection.OrderBy(x => x.Key.Sequence).Select(x => x.Value).ToArray();
                Parallel.ForEach(aggregateRows, entity => core.ExecuteFormulaFields(entityMetadata, entity, formulaNeeded).GetAwaiter().GetResult());
                entitiesToReturn = QueryExpressionAggregation.Aggregate(queryExpr, aggregateRows, db, metadata.EntityMetadata);
            }
            else
            {
                var orders = queryExpr.Orders;
                IOrderedEnumerable<KeyValuePair<DbRow, Entity>> tempSortedList;

                // TODO: Check the order that the orders are executed in is correct
                if (orders == null || orders.Count == 0)
                {
                    tempSortedList = collection.OrderBy(x => x.Key.Sequence);
                }
                else
                {
                    Func<KeyValuePair<DbRow, Entity>, object> selector = x => x.Value.Attributes.TryGetValue(orders[0].AttributeName, out var a) ? Utility.GetComparableAttribute(a) : null;
                    if (orders.First().OrderType == OrderType.Ascending)
                        tempSortedList = collection.OrderBy(selector);
                    else
                        tempSortedList = collection.OrderByDescending(selector);

                    foreach (var order in orders.Skip(1))
                    {
                        selector = (x => x.Value.Attributes.TryGetValue(order.AttributeName, out var a) ? Utility.GetComparableAttribute(a) : null);
                        if (order.OrderType == OrderType.Ascending)
                            tempSortedList = tempSortedList.ThenBy(selector);
                        else
                            tempSortedList = tempSortedList.ThenByDescending(selector);

                    }
                    tempSortedList = tempSortedList.ThenBy(x => x.Key.Sequence);
                }

                // Convert to array to lock-in the ordering (if we are ordering by something that doesn't exist in the columnset, the ordering will fail afterwards)
                var orderedEntities = tempSortedList.Select(x => x.Value).ToArray();

                // Calculate formula fields before we filter the fetched columns
                Parallel.ForEach(orderedEntities, entity => core.ExecuteFormulaFields(entityMetadata, entity, formulaNeeded).GetAwaiter().GetResult());

                // Refine and filter the columns
                entitiesToReturn = RefineEntityAttributes(orderedEntities, queryExpr.ColumnSet);

                // Populate EntityReference names after column filtering
                foreach (var entity in entitiesToReturn)
                {
                    Utility.PopulateEntityReferenceNames(entity, db);
                }

                if (queryExpr.Distinct)
                {
                    var uniqueIds = new HashSet<Guid>();
                    entitiesToReturn = entitiesToReturn.Where(entity => uniqueIds.Add(entity.Id));
                }
            }

            if (queryExpr.TopCount.HasValue || (queryExpr.PageInfo?.Count ?? 0) > 0)
            {
                entitiesToReturn = entitiesToReturn.Take(queryExpr.TopCount ?? queryExpr.PageInfo.Count); // QueryExpression TopCount or Linq query Take operator
            }

            var colToReturn = new EntityCollection(entitiesToReturn.ToList());

            // According to docs, should return -1 if ReturnTotalRecordCount set to false
            colToReturn.TotalRecordCount = queryExpr.PageInfo.ReturnTotalRecordCount ? colToReturn.Entities.Count : -1;

            var resp = new RetrieveMultipleResponse();

            resp.Results["EntityCollection"] = colToReturn;
            return resp;

        }

        private IEnumerable<Entity> RefineEntityAttributes(IEnumerable<Entity> entities, ColumnSet columnSet)
        {
            Parallel.ForEach(entities, entity =>
            {
                KeepAttributesAndAliasAttributes(entity, columnSet);
            });
            return entities;
        }

        private List<Entity> GetAliasedValuesFromLinkentity(LinkEntity linkEntity, Entity parent, Entity toAdd, XrmDb db)
        {
            var collection = new List<Entity>();
            var calculatedNeeded = GetNeededAttributesForCalculatedFields(linkEntity);
            var formulaNeeded = GetRequestedAttributes(linkEntity.Columns);
            foreach (var linkedRow in db[linkEntity.LinkToEntityName])
            {
                var linkedEntity = linkedRow.ToEntity();
                core.ExecuteCalculatedFields(linkedRow.Metadata, linkedEntity, calculatedNeeded);

                if (linkedEntity.Attributes.ContainsKey(linkEntity.LinkToAttributeName) &&
                    parent.Attributes.ContainsKey(linkEntity.LinkFromAttributeName))
                {
                    var linkedAttr = ValueConverter.ConvertToComparableObject(
                        linkedEntity.Attributes[linkEntity.LinkToAttributeName]);
                    var entAttr = ValueConverter.ConvertToComparableObject(
                            parent.Attributes[linkEntity.LinkFromAttributeName]);

                    if (linkedAttr.Equals(entAttr))
                    {
                        // Project requested formula fields before copying this link's aliases.
                        // Nested links follow the same path, so their aggregates see computed data too.
                        core.ExecuteFormulaFields(linkedRow.Metadata, linkedEntity, formulaNeeded).GetAwaiter().GetResult();
                        var aliasedEntity = GetEntityWithAliasAttributes(linkEntity.EntityAlias, toAdd,
                                metadata.EntityMetadata.GetMetadata(toAdd.LogicalName), linkedEntity.Attributes);

                        if (linkEntity.LinkEntities.Count > 0)
                        {
                            // Multiple nested links under this link must all be satisfied (AND /
                            // cross join), just like multiple top-level links. Unioning them would
                            // OR the nested links together.
                            var perNestedResults = new List<List<Entity>>();
                            foreach (var nestedLinkEntity in linkEntity.LinkEntities)
                            {
                                nestedLinkEntity.LinkFromEntityName = linkEntity.LinkToEntityName;
                                perNestedResults.Add(GetAliasedValuesFromLinkentity(
                                        nestedLinkEntity, linkedEntity, aliasedEntity, db));
                            }
                            collection.AddRange(CombineLinkResults(aliasedEntity, perNestedResults)
                                    .Where(e => EntityMatcher.MatchesCriteria(e, linkEntity.LinkCriteria)));
                        }
                        else if (EntityMatcher.MatchesCriteria(aliasedEntity, linkEntity.LinkCriteria))
                        {
                            collection.Add(aliasedEntity);
                        }

                    }
                }
            }
            if (linkEntity.JoinOperator == JoinOperator.LeftOuter && collection.Count == 0)
            {
                collection.Add(toAdd);
            }
            else if (linkEntity.JoinOperator == JoinOperator.NotAny && collection.Count == 0)
            {
                // NotAny join: return parent entity only if no matching linked entities were found
                collection.Add(toAdd);
            }
            else if (linkEntity.JoinOperator == JoinOperator.NotAny && collection.Count > 0)
            {
                // NotAny join: if matching linked entities were found, don't return anything
                collection.Clear();
            }
            return collection;
        }

        private Entity GetEntityWithAliasAttributes(string alias, Entity toAdd, EntityMetadata metadata, AttributeCollection attributes)
        {
            var parentClone = core.GetStronglyTypedEntity(toAdd, metadata, null);
            foreach (var attr in attributes.Keys)
            {
                parentClone.Attributes.Add(alias + "." + attr, new AliasedValue(alias, attr, attributes[attr]));
            }
            return parentClone;
        }

        // Cross-joins the per-link aliased result lists for sibling links into a single set of
        // combined rows. Each combined row is a clone of the base entity carrying the aliased
        // attributes of one pick from every link. If any link produced no rows (an inner link
        // with no match), the product is empty and the parent is dropped; LeftOuter/NotAny links
        // already contribute the parent itself, so outer joins keep the parent.
        private List<Entity> CombineLinkResults(Entity baseEntity, List<List<Entity>> perLinkResults)
        {
            IEnumerable<Entity> combined = new[] { baseEntity };
            foreach (var linkResults in perLinkResults)
            {
                combined = combined
                    .SelectMany(acc => linkResults.Select(linked => MergeAliasAttributes(acc, linked)))
                    .ToList();
            }
            return combined.ToList();
        }

        // Returns a clone of baseEntity (preserving any aliased attributes it already carries)
        // augmented with the aliased attributes from withAliases.
        private Entity MergeAliasAttributes(Entity baseEntity, Entity withAliases)
        {
            var merged = core.GetStronglyTypedEntity(baseEntity,
                    metadata.EntityMetadata.GetMetadata(baseEntity.LogicalName), null);
            foreach (var attr in withAliases.Attributes.Where(a => a.Key.Contains('.')))
            {
                if (!merged.Attributes.ContainsKey(attr.Key))
                {
                    merged.Attributes.Add(attr.Key, attr.Value);
                }
            }
            return merged;
        }

        private void KeepAttributesAndAliasAttributes(Entity entity, ColumnSet toKeep)
        {
            var clone = entity.CloneEntity(metadata.EntityMetadata.GetMetadata(entity.LogicalName), toKeep);
            if (toKeep != null && !toKeep.AllColumns)
                clone.Attributes.AddRange(entity.Attributes.Where(x => x.Key.Contains('.')));
            entity.Attributes.Clear();
            entity.Attributes.AddRange(clone.Attributes);
        }

        private void FillAliasIfEmpty(QueryExpression expression)
        {
            if (expression.LinkEntities.Count == 0)
                return;

            int depth = 1;
            foreach (var le in expression.LinkEntities)
            {
                FillAliasIfEmpty(le, ref depth);
            }
        }

        private void FillAliasIfEmpty(LinkEntity linkEntity, ref int linkCount)
        {
            if (linkEntity.EntityAlias == null)
            {
                linkEntity.EntityAlias = $"{linkEntity.LinkToEntityName}{linkCount}";
                linkCount++;
            }
            if (linkEntity.LinkCriteria != null)
            {
                FillEntityNameIfEmpty(linkEntity.LinkCriteria, linkEntity.EntityAlias);
            }
            if (linkEntity.LinkEntities.Count > 0)
            {
                foreach (var le in linkEntity.LinkEntities)
                {
                    FillAliasIfEmpty(le, ref linkCount);
                }
            }
        }
        private void FillEntityNameIfEmpty(FilterExpression filter, string alias)
        {
            foreach (var cond in filter.Conditions)
            {
                FillEntityNameIfEmpty(cond, alias);
            }
            foreach (var subFilter in filter.Filters)
            {
                FillEntityNameIfEmpty(subFilter, alias);
            }
        }

        private void FillEntityNameIfEmpty(ConditionExpression condition, string alias)
        {
            if (condition.EntityName != null)
            {
                return;
            }
            condition.EntityName = alias;
        }

        // Returns null when every column on the primary entity is in play (AllColumns / no
        // ColumnSet), otherwise the set of attribute names referenced anywhere a calc column could
        // be observed: ColumnSet, criteria, orders, and the link-from sides of inner links (where a
        // null on a calc column would silently change join semantics). When non-null, the Core can
        // skip evaluating any other calculated column for the row.
        private static ISet<string> GetNeededAttributesForCalculatedFields(QueryExpression query)
        {
            if (query.ColumnSet == null || query.ColumnSet.AllColumns) return null;

            var set = new HashSet<string>(GetRequestedAttributes(query.ColumnSet), StringComparer.OrdinalIgnoreCase);
            CollectFilterAttributes(query.Criteria, set);
            foreach (var order in query.Orders)
            {
                if (!string.IsNullOrEmpty(order.AttributeName)) set.Add(order.AttributeName);
            }
            foreach (var link in query.LinkEntities)
            {
                if (!string.IsNullOrEmpty(link.LinkFromAttributeName)) set.Add(link.LinkFromAttributeName);
            }
            return set;
        }

        private static ISet<string> GetNeededAttributesForCalculatedFields(LinkEntity link)
        {
            var requested = GetRequestedAttributes(link.Columns);
            if (requested == null) return null;
            var needed = new HashSet<string>(requested, StringComparer.OrdinalIgnoreCase);
            needed.Add(link.LinkToAttributeName);
            CollectFilterAttributes(link.LinkCriteria, needed);
            foreach (var nested in link.LinkEntities)
            {
                if (!string.IsNullOrEmpty(nested.LinkFromAttributeName)) needed.Add(nested.LinkFromAttributeName);
            }
            return needed;
        }

        private static ISet<string> GetRequestedAttributes(ColumnSet columnSet)
        {
            if (columnSet == null || columnSet.AllColumns) return null;
            return new HashSet<string>(columnSet.Columns.Concat(columnSet.AttributeExpressions.Select(expression => expression.AttributeName)), StringComparer.OrdinalIgnoreCase);
        }

        // Validates that every attribute referenced in the query's filters exists on the entity it
        // targets. Conditions carry an EntityName (the primary entity or a link alias) that decides
        // which entity's metadata the attribute is looked up against; the lookup is case-sensitive,
        // matching Dataverse.
        private void ValidateFilterAttributes(QueryExpression query)
        {
            // alias/logical-name -> metadata for the primary entity and every (nested) link entity.
            var scopes = new Dictionary<string, EntityMetadata>(StringComparer.Ordinal);
            if (metadata.EntityMetadata.TryGetValue(query.EntityName, out var primaryMeta))
            {
                scopes[query.EntityName] = primaryMeta;
            }
            foreach (var link in query.LinkEntities)
            {
                CollectLinkScopes(link, scopes);
            }

            ValidateFilterAttributes(query.Criteria, query.EntityName, scopes);
            foreach (var link in query.LinkEntities)
            {
                ValidateLinkCriteria(link, scopes);
            }
        }

        private void CollectLinkScopes(LinkEntity link, Dictionary<string, EntityMetadata> scopes)
        {
            var scopeKey = GetLinkScopeKey(link);
            if (!scopes.ContainsKey(scopeKey) &&
                metadata.EntityMetadata.TryGetValue(link.LinkToEntityName, out var meta))
            {
                scopes[scopeKey] = meta;
            }
            foreach (var nested in link.LinkEntities)
            {
                CollectLinkScopes(nested, scopes);
            }
        }

        private void ValidateLinkCriteria(LinkEntity link, Dictionary<string, EntityMetadata> scopes)
        {
            ValidateFilterAttributes(link.LinkCriteria, GetLinkScopeKey(link), scopes);
            foreach (var nested in link.LinkEntities)
            {
                ValidateLinkCriteria(nested, scopes);
            }
        }

        // FillAliasIfEmpty only assigns an alias when one is null, so a link whose EntityAlias is an
        // explicit empty string keeps it. Fall back to the linked entity's logical name in that case
        // so the link's criteria are still validated against the correct metadata.
        private static string GetLinkScopeKey(LinkEntity link)
        {
            return string.IsNullOrEmpty(link.EntityAlias) ? link.LinkToEntityName : link.EntityAlias;
        }

        private void ValidateFilterAttributes(FilterExpression filter, string defaultScope, Dictionary<string, EntityMetadata> scopes)
        {
            if (filter == null) return;

            foreach (var condition in filter.Conditions)
            {
                // A null/empty attribute name is a filter on the primary id, not a real column.
                if (string.IsNullOrEmpty(condition.AttributeName)) continue;

                var scopeKey = string.IsNullOrEmpty(condition.EntityName) ? defaultScope : condition.EntityName;
                // If the scope can't be resolved to a known entity we can't validate it; leave it be
                // rather than risk rejecting a legitimate query.
                if (scopeKey == null || !scopes.TryGetValue(scopeKey, out var meta)) continue;

                if (!Utility.IsValidAttribute(condition.AttributeName, meta))
                {
                    var entityName = string.IsNullOrEmpty(meta.SchemaName) ? meta.LogicalName : meta.SchemaName;
                    throw new FaultException<OrganizationServiceFault>(
                        new OrganizationServiceFault(),
                        $"'{entityName}' entity doesn't contain attribute with Name = '{condition.AttributeName}' and NameMapping = 'Logical' (look up attribute by name is case-sensitive)");
                }
            }

            foreach (var sub in filter.Filters)
            {
                ValidateFilterAttributes(sub, defaultScope, scopes);
            }
        }

        private static void CollectFilterAttributes(FilterExpression filter, HashSet<string> set)
        {
            if (filter == null) return;
            foreach (var cond in filter.Conditions)
            {
                if (!string.IsNullOrEmpty(cond.AttributeName)) set.Add(cond.AttributeName);
            }
            foreach (var sub in filter.Filters)
            {
                CollectFilterAttributes(sub, set);
            }
        }
    }
}
