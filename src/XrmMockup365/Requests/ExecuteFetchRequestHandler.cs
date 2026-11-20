using System;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup.Database;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace DG.Tools.XrmMockup
{
    internal class ExecuteFetchRequestHandler : ServiceRequestHandler
    {
        internal ExecuteFetchRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "ExecuteFetch") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<ExecuteFetchRequest>(orgRequest);
            if (string.IsNullOrWhiteSpace(request.FetchXml)) throw new FaultException("FetchXml is required.");
            // Legacy ExecuteFetch uses entity names for implicit link aliases, unlike RetrieveMultiple.
            var implicitAliases = XDocument.Parse(request.FetchXml).Descendants("link-entity")
                .Where(link => link.Attribute("alias") == null)
                .Select(link => (string)link.Attribute("name")).Where(name => name != null).ToArray();
            var explicitAliases = XDocument.Parse(request.FetchXml).Descendants("link-entity")
                .Select(link => (string)link.Attribute("alias")).Where(name => name != null).ToArray();
            var result = RetrieveMultiple(new FetchExpression(request.FetchXml), userRef);
            var root = new XElement("resultset", new XAttribute("morerecords", result.MoreRecords ? "1" : "0"));
            if (result.PagingCookie != null) root.Add(new XAttribute("paging-cookie", result.PagingCookie));
            foreach (var entity in result.Entities)
            {
                var row = new XElement("result");
                foreach (var attribute in entity.Attributes)
                {
                    var value = attribute.Value is AliasedValue alias ? alias.Value : attribute.Value;
                    if (value == null) continue;
                    var key = attribute.Key;
                    var separator = key.IndexOf('.');
                    if (separator > 0)
                    {
                        var prefix = key.Substring(0, separator);
                        if (!explicitAliases.Contains(prefix))
                        {
                            var name = implicitAliases.FirstOrDefault(n => prefix.StartsWith(n, StringComparison.Ordinal) &&
                                int.TryParse(prefix.Substring(n.Length), out _));
                            if (name != null) key = name + key.Substring(separator);
                        }
                    }
                    var element = new XElement(key);
                    var formatted = entity.FormattedValues.Contains(attribute.Key) ? entity.FormattedValues[attribute.Key] : null;
                    if (value is EntityReference reference)
                    {
                        element.Value = reference.Id.ToString("D").ToUpperInvariant();
                        element.SetAttributeValue("dsc", "0");
                        element.SetAttributeValue("name", reference.Name ?? formatted ?? "");
                        element.SetAttributeValue("yomi", reference.Name ?? formatted ?? "");
                        if (metadata.EntityMetadata.TryGetValue(reference.LogicalName, out var em) && em.ObjectTypeCode.HasValue)
                            element.SetAttributeValue("type", em.ObjectTypeCode.Value);
                    }
                    else if (value is DateTime date)
                    {
                        element.Value = XmlConvert.ToString(date, XmlDateTimeSerializationMode.RoundtripKind);
                        element.SetAttributeValue("date", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                        element.SetAttributeValue("time", date.ToString("hh:mm tt", CultureInfo.InvariantCulture));
                    }
                    else if (value is Money money)
                    {
                        element.Value = money.Value.ToString(CultureInfo.InvariantCulture);
                        element.SetAttributeValue("formattedvalue", formatted ?? element.Value);
                    }
                    else if (value is OptionSetValue option)
                    {
                        element.Value = option.Value.ToString(CultureInfo.InvariantCulture);
                        element.SetAttributeValue("name", formatted ?? element.Value);
                        element.SetAttributeValue("formattedvalue", element.Value);
                    }
                    else if (value is bool boolean)
                    {
                        element.Value = boolean ? "1" : "0";
                        element.SetAttributeValue("name", formatted ?? boolean.ToString());
                    }
                    else if (value is OptionSetValueCollection options)
                        element.Value = string.Join(",", options.Select(o => o.Value.ToString(CultureInfo.InvariantCulture)));
                    else if (value is Guid guid) element.Value = guid.ToString("D").ToUpperInvariant();
                    else element.Value = Convert.ToString(value, CultureInfo.InvariantCulture);
                    row.Add(element);
                }
                root.Add(row);
            }
            var response = new ExecuteFetchResponse();
            response.Results["FetchXmlResult"] = root.ToString(SaveOptions.DisableFormatting);
            return response;
        }
    }
}
