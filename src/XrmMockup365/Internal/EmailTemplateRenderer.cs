using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        /// <summary>
        /// Renders a single template field (subject or body).
        /// </summary>
        /// <param name="templateField">The raw template attribute value (an XSLT stylesheet in Dataverse).</param>
        /// <param name="entitiesByLogicalName">
        /// The records available to the template, keyed by logical name. Each becomes a child of
        /// <c>&lt;data&gt;</c> (e.g. <c>&lt;contact&gt;</c>, <c>&lt;systemuser&gt;</c>) with one
        /// element per populated attribute.
        /// </param>
        /// <returns>
        /// The merged text. If the field is not an XSLT stylesheet, or the transform fails, the
        /// raw value is returned unchanged so plain-text templates still work.
        /// </returns>
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
                    // Default XsltSettings: scripts and the document() function are disabled.
                    transform.Load(xsltReader);
                }

                var dataDocument = BuildDataDocument(entitiesByLogicalName);

                using (var writer = new StringWriter(CultureInfo.InvariantCulture))
                {
                    transform.Transform(dataDocument, null, writer);
                    return writer.ToString();
                }
            }
            catch (Exception e) when (e is XmlException || e is XsltException)
            {
                // Not valid XSLT - fall back to the raw value rather than failing the send.
                return templateField;
            }
        }

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

        private static string AttributeToString(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case string s:
                    return s;
                case EntityReference reference:
                    return reference.Name;
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
