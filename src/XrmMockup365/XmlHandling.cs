using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using System.Linq;
using System.Xml.Linq;

namespace DG.Tools.XrmMockup {
    internal static class XmlHandling {

        public static QueryExpression FetchXmlToQueryExpression(string fetchXml) {
            var query = new QueryExpression();
            var fetch = XElement.Parse(fetchXml);
            var entity = fetch.Element("entity");
            var logicalName = entity.Attribute("name");
            var page = fetch.Attribute("page");
            var count = fetch.Attribute("count");

            query.EntityName = logicalName.Value;

            query.ColumnSet = new ColumnSet();
            if (entity.Element("all-attributes") != null) {
                query.ColumnSet.AllColumns = true;
            }
            foreach (var column in entity.Elements("attribute")) {
                query.ColumnSet.Columns.Add(column.Attribute("name").Value);
            }

            if (entity.Element("filter") != null) {
                query.Criteria = FilterExpFromXml(entity.Element("filter").ToString());
            }

            var top = fetch.Attribute("top");
            if (top != null) query.TopCount = int.Parse(top.Value);
            if (count != null || page != null) {
                query.PageInfo = new PagingInfo {
                    PageNumber = page == null ? 1 : int.Parse(page.Value),
                    Count = count == null ? 5000 : int.Parse(count.Value),
                    PagingCookie = (string)fetch.Attribute("paging-cookie")
                };
                if (query.PageInfo.PageNumber < 1 || query.PageInfo.Count < 1)
                    throw new FaultException("FetchXML page and count must be positive.");
            }
            query.PageInfo.ReturnTotalRecordCount = (string)fetch.Attribute("returntotalrecordcount") == "true";

            foreach (var order in entity.Elements("order")) {
                var orderExp = new OrderExpression() {
                    AttributeName = order.Attribute("attribute").Value,
                    OrderType = order.Attribute("descending") == null || order.Attribute("descending").Value == "false" ? OrderType.Ascending : OrderType.Descending
                };
                query.Orders.Add(orderExp);
            }

            foreach (var linkEntity in entity.Elements("link-entity")) {
                query.LinkEntities.Add(LinkEntityFromXml(logicalName.Value, linkEntity.ToString()));
            }

            return query;
        }

        public static FilterExpression FilterExpFromXml(string filterXml, string currentEntity = null) {
            var filter = XElement.Parse(filterXml);
            var filterExp = new FilterExpression();

            if (filter.Attribute("type") == null || filter.Attribute("type").Value == "and") {
                filterExp.FilterOperator = LogicalOperator.And;
            } else if (filter.Attribute("type").Value == "or") {
                filterExp.FilterOperator = LogicalOperator.Or;
            }

            foreach (var condition in filter.Elements("condition")) {
                var attr = condition.Attribute("attribute").Value;
                var entityName = condition.Attribute("entityname")?.Value ?? currentEntity;
                var op = Mappings.ConditionalOperator[condition.Attribute("operator").Value];
                object[] values = null;
                if (condition.HasElements) {
                    values = condition.Elements("value")
                        .Select(v => v.Value)
                        .ToArray();
                }
                else if (condition.Attribute("value") != null) { 
                    values = new[] { condition.Attribute("value").Value };
                }
                filterExp.AddCondition(entityName, attr, op, values);
            }

            foreach (var subFilter in filter.Elements("filter")) {
                filterExp.AddFilter(FilterExpFromXml(subFilter.ToString(), currentEntity));
            }

            return filterExp;
        }

        public static LinkEntity LinkEntityFromXml(string parentLogicalName, string linkXml) {
            var link = XElement.Parse(linkXml);
            var joinOperator = link.Attribute("link-type");

            var linkEntity = new LinkEntity() {
                EntityAlias = link.Attribute("alias")?.Value ?? link.Attribute("name").Value,
                LinkFromEntityName = parentLogicalName,
                LinkFromAttributeName = link.Attribute("to").Value,
                LinkToEntityName = link.Attribute("name").Value,
                LinkToAttributeName = link.Attribute("from").Value,
                Columns = new ColumnSet()
            };

            if (link.Element("all-attributes") != null) {
                linkEntity.Columns.AllColumns = true;
            }

            foreach (var column in link.Elements("attribute")) {
                linkEntity.Columns.Columns.Add(column.Attribute("name").Value);
            }

            if (joinOperator == null || joinOperator.Value == "inner") {
                linkEntity.JoinOperator = JoinOperator.Inner;
            } else if (joinOperator.Value == "outer") {
                linkEntity.JoinOperator = JoinOperator.LeftOuter;
            } else if (joinOperator.Value == "natural") {
                linkEntity.JoinOperator = JoinOperator.Natural;
            } else if (joinOperator.Value == "notany") {
                linkEntity.JoinOperator = JoinOperator.NotAny;
            }

            if (link.Element("filter") != null) {
                linkEntity.LinkCriteria = FilterExpFromXml(link.Element("filter").ToString(), linkEntity.EntityAlias);
            }

            foreach (var subLink in link.Elements("link-entity")) {
                linkEntity.LinkEntities.Add(LinkEntityFromXml(parentLogicalName, subLink.ToString()));
            }

            return linkEntity;


        }
    }
}
