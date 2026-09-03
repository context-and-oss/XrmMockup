using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.ServiceModel;
using System.Xml;
using System.Xml.Xsl;

namespace DG.Tools.XrmMockup
{
    /// <summary>
    /// Renders Dynamics e-mail template content. In Dataverse a template's <c>subject</c> and
    /// <c>body</c> attributes are XSLT stylesheets (method="text") that transform a
    /// <c>&lt;data&gt;</c> document built from the records the e-mail draws from (the regarding
    /// record and the sending user). This reproduces that mechanism so the merged text matches
    /// what the platform produces.
    /// </summary>
    internal static class EmailTemplateRenderer
    {
        /// <param name="templateField">The raw template attribute value.</param>
        /// <param name="entitiesByLogicalName">
        /// The records available to the template, keyed by logical name. Each becomes a child of
        /// <c>&lt;data&gt;</c> (e.g. <c>&lt;contact&gt;</c>, <c>&lt;systemuser&gt;</c>) with one
        /// element per populated attribute.
        /// </param>
        /// <returns>The merged text, or the value unchanged if it is not a stylesheet.</returns>
        /// <exception cref="FaultException">The value is a stylesheet but does not compile or apply.</exception>
        public static string Render(string templateField, IReadOnlyDictionary<string, Entity> entitiesByLogicalName)
        {
            if (string.IsNullOrWhiteSpace(templateField))
                return templateField;

            // Real Dataverse templates are XSLT. Anything else is treated as literal text.
            if (templateField.IndexOf("xsl:stylesheet", StringComparison.OrdinalIgnoreCase) < 0)
                return templateField;

            try
            {
                var transform = new XslCompiledTransform();
                using (var stringReader = new StringReader(templateField))
                using (var xsltReader = XmlReader.Create(stringReader))
                {
                    // A template is data, so it gets no script blocks, no document() and no
                    // resolver for xsl:import/include - a mock run must stay offline.
                    transform.Load(xsltReader, XsltSettings.Default, null);
                }

                using (var writer = new StringWriter(CultureInfo.InvariantCulture))
                {
                    transform.Transform(BuildDataDocument(entitiesByLogicalName), null, writer);
                    return writer.ToString();
                }
            }
            catch (Exception e) when (e is XmlException || e is XsltException)
            {
                // Returning the raw stylesheet instead would put markup in the sent e-mail.
                throw new FaultException($"The e-mail template could not be rendered: {e.Message}");
            }
        }

        /// <summary>
        /// Builds the <c>&lt;data&gt;</c> document the stylesheet selects its merge values from.
        /// </summary>
        private static XmlDocument BuildDataDocument(IReadOnlyDictionary<string, Entity> entitiesByLogicalName)
        {
            var document = new XmlDocument();
            var dataElement = document.CreateElement("data");
            document.AppendChild(dataElement);

            if (entitiesByLogicalName == null)
                return document;

            foreach (var pair in entitiesByLogicalName)
            {
                if (pair.Value == null || string.IsNullOrEmpty(pair.Key))
                    continue;

                var entityElement = document.CreateElement(pair.Key);
                dataElement.AppendChild(entityElement);

                foreach (var attribute in pair.Value.Attributes)
                {
                    var text = AttributeToString(attribute.Value);
                    if (string.IsNullOrEmpty(text))
                        continue;

                    var attributeElement = document.CreateElement(attribute.Key);
                    attributeElement.InnerText = text;
                    entityElement.AppendChild(attributeElement);
                }
            }

            return document;
        }

        /// <summary>
        /// Flattens an attribute to the text a stylesheet's <c>xsl:value-of</c> would select.
        /// </summary>
        private static string AttributeToString(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case string s:
                    return s;
                case EntityReference reference:
                    // Dataverse merges a lookup as the record id in registry format (verified
                    // against a live org), not as the display name.
                    return reference.Id.ToString("B").ToUpperInvariant();
                case OptionSetValue optionSet:
                    return optionSet.Value.ToString(CultureInfo.InvariantCulture);
                case Money money:
                    return money.Value.ToString(CultureInfo.InvariantCulture);
                case bool boolean:
                    return boolean ? "True" : "False";
                case DateTime dateTime:
                    return dateTime.ToString(CultureInfo.InvariantCulture);
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return value.ToString();
            }
        }
    }
}
