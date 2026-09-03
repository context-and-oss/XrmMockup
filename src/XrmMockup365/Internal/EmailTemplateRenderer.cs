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
    /// Renders an e-mail template's subject or body. Dataverse stores these as XSLT stylesheets
    /// that transform a <c>&lt;data&gt;</c> document built from the records the e-mail draws from.
    /// Values that are not stylesheets are literal text and pass through unchanged.
    /// </summary>
    internal static class EmailTemplateRenderer
    {
        /// <param name="entitiesByLogicalName">
        /// Records the stylesheet may select from. Keys become element names, so a contact keyed
        /// "contact" is addressed as <c>contact/lastname</c>.
        /// </param>
        public static string Render(string templateField, IReadOnlyDictionary<string, Entity> entitiesByLogicalName)
        {
            if (string.IsNullOrWhiteSpace(templateField))
                return templateField;

            if (templateField.IndexOf("xsl:stylesheet", StringComparison.OrdinalIgnoreCase) < 0)
                return templateField;

            try
            {
                var transform = new XslCompiledTransform();
                using (var stringReader = new StringReader(templateField))
                using (var xsltReader = XmlReader.Create(stringReader))
                {
                    // A template is untrusted data: no scripts, no document(), and a null resolver
                    // so xsl:import cannot pull a mock run onto the network.
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
                // Falling back to the raw value would mail the stylesheet markup to the recipient.
                throw new FaultException($"The e-mail template could not be rendered: {e.Message}");
            }
        }

        /// <summary>Builds the document the stylesheet selects its merge values from.</summary>
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

                    // An empty element and a missing one differ to the stylesheet: xsl:when treats
                    // a missing node as false and falls through to its xsl:otherwise default.
                    if (string.IsNullOrEmpty(text))
                        continue;

                    var attributeElement = document.CreateElement(attribute.Key);
                    attributeElement.InnerText = text;
                    entityElement.AppendChild(attributeElement);
                }
            }

            return document;
        }

        /// <summary>Flattens an attribute to the text the stylesheet will select.</summary>
        private static string AttributeToString(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case string s:
                    return s;
                case EntityReference reference:
                    // Dataverse merges a lookup as the record id, not the display name.
                    return reference.Id.ToString("B").ToUpperInvariant();
                case OptionSetValue optionSet:
                    // Likewise the raw value rather than the option label.
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
