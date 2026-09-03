using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using Xunit;

namespace DG.XrmMockupTest
{
    /// <summary>
    /// Unit tests for the XSLT-based template rendering used by SendEmailFromTemplate. The
    /// stylesheets below are the actual subject/body of the built-in "Thank you for registering
    /// with us" contact template in Dataverse.
    /// </summary>
    public class TestEmailTemplateRenderer
    {
        private const string RealSubjectXslt =
            "<?xml version=\"1.0\" ?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
            "<xsl:output method=\"text\" indent=\"no\" /><xsl:template match=\"/data\">" +
            "<![CDATA[Thank you for registering with us]]></xsl:template></xsl:stylesheet>";

        private const string RealBodyXslt =
            "<?xml version=\"1.0\" ?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
            "<xsl:output method=\"text\" indent=\"no\"/><xsl:template match=\"/data\">" +
            "<![CDATA[<P>Dear ]]><xsl:choose><xsl:when test=\"contact/salutation\"><xsl:value-of select=\"contact/salutation\" /></xsl:when><xsl:otherwise></xsl:otherwise></xsl:choose>" +
            "<![CDATA[ ]]><xsl:choose><xsl:when test=\"contact/lastname\"><xsl:value-of select=\"contact/lastname\" /></xsl:when><xsl:otherwise>Valued Customer</xsl:otherwise></xsl:choose>" +
            "<![CDATA[  ,</P><P>Name: ]]><xsl:choose><xsl:when test=\"systemuser/fullname\"><xsl:value-of select=\"systemuser/fullname\" /></xsl:when><xsl:otherwise></xsl:otherwise></xsl:choose>" +
            "<![CDATA[<BR>Street Address: ]]><xsl:choose><xsl:when test=\"contact/address1_line1\"><xsl:value-of select=\"contact/address1_line1\" /></xsl:when><xsl:otherwise>No Address Provided</xsl:otherwise></xsl:choose>" +
            "<![CDATA[<BR>E-mail Address: ]]><xsl:choose><xsl:when test=\"contact/emailaddress1\"><xsl:value-of select=\"contact/emailaddress1\" /></xsl:when><xsl:otherwise></xsl:otherwise></xsl:choose>" +
            "<![CDATA[</P>]]></xsl:template></xsl:stylesheet>";

        private const string LookupXslt =
            "<?xml version=\"1.0\" ?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
            "<xsl:output method=\"text\" indent=\"no\"/><xsl:template match=\"/data\">" +
            "<![CDATA[company=]]><xsl:value-of select=\"contact/parentcustomerid\" /></xsl:template></xsl:stylesheet>";

        // Well-formed XML, but 'contact/' is not a valid XPath expression.
        private const string MalformedXslt =
            "<?xml version=\"1.0\" ?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
            "<xsl:output method=\"text\" /><xsl:template match=\"/data\">" +
            "<xsl:value-of select=\"contact/\" /></xsl:template></xsl:stylesheet>";

        [Fact]
        public void RendersStaticSubject()
        {
            var entities = new Dictionary<string, Entity>
            {
                ["contact"] = new Entity("contact")
            };

            var result = EmailTemplateRenderer.Render(RealSubjectXslt, entities);

            Assert.Equal("Thank you for registering with us", result);
        }

        [Fact]
        public void RendersBodyWithRegardingAndSenderValues()
        {
            var contact = new Entity("contact")
            {
                ["salutation"] = "Mr",
                ["lastname"] = "Smith",
                ["address1_line1"] = "123 Main St",
                ["emailaddress1"] = "smith@test.com"
            };
            var user = new Entity("systemuser")
            {
                ["fullname"] = "Admin User"
            };

            var entities = new Dictionary<string, Entity>
            {
                ["contact"] = contact,
                ["systemuser"] = user
            };

            var result = EmailTemplateRenderer.Render(RealBodyXslt, entities);

            // "MrSmith" has no space because XSLT strips the whitespace-only text node the
            // stylesheet places between the two values. Dataverse renders it the same way.
            Assert.Contains("Dear MrSmith", result);
            Assert.Contains("Name: Admin User", result);
            Assert.Contains("Street Address: 123 Main St", result);
            Assert.Contains("E-mail Address: smith@test.com", result);
        }

        [Fact]
        public void UsesXsltDefaultsWhenAttributesMissing()
        {
            // contact has neither lastname nor address1_line1 -> the <xsl:otherwise> defaults apply.
            var entities = new Dictionary<string, Entity>
            {
                ["contact"] = new Entity("contact") { ["salutation"] = "Ms" }
            };

            var result = EmailTemplateRenderer.Render(RealBodyXslt, entities);

            Assert.Contains("Dear Ms", result);
            Assert.Contains("Valued Customer", result);
            Assert.Contains("Street Address: No Address Provided", result);
        }

        [Fact]
        public void ReturnsPlainTextTemplatesUnchanged()
        {
            var entities = new Dictionary<string, Entity>();

            var result = EmailTemplateRenderer.Render("Just plain text", entities);

            Assert.Equal("Just plain text", result);
        }

        [Fact]
        public void ReturnsNullForNullInput()
        {
            Assert.Null(EmailTemplateRenderer.Render(null, new Dictionary<string, Entity>()));
        }

        [Fact]
        public void ThrowsWhenStylesheetDoesNotCompile()
        {
            Assert.Throws<FaultException>(
                () => EmailTemplateRenderer.Render(MalformedXslt, new Dictionary<string, Entity>()));
        }

        [Fact]
        public void MergesLookupAsRecordId()
        {
            var accountId = new Guid("3c2e0869-d1a6-f111-b8de-70a8a57d382b");
            var entities = new Dictionary<string, Entity>
            {
                // The name is populated to prove it is deliberately not what gets merged.
                ["contact"] = new Entity("contact")
                {
                    ["parentcustomerid"] = new EntityReference("account", accountId) { Name = "Probe Account A/S" }
                }
            };

            var result = EmailTemplateRenderer.Render(LookupXslt, entities);

            Assert.Equal("company={3C2E0869-D1A6-F111-B8DE-70A8A57D382B}", result);
        }
    }
}
