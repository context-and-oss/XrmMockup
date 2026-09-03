using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using Xunit;

namespace DG.XrmMockupTest
{
    /// <summary>
    /// Tests the XSLT rendering behind SendEmailFromTemplate. The subject and body below are the
    /// actual stylesheets of the built-in "Thank you for registering with us" contact template.
    /// </summary>
    public class TestEmailTemplateRenderer
    {
        private const string SubjectXslt =
            "<?xml version=\"1.0\" ?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
            "<xsl:output method=\"text\" indent=\"no\" /><xsl:template match=\"/data\">" +
            "<![CDATA[Thank you for registering with us]]></xsl:template></xsl:stylesheet>";

        private const string BodyXslt =
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

        private const string ScalarsXslt =
            "<?xml version=\"1.0\" ?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
            "<xsl:output method=\"text\" indent=\"no\"/><xsl:template match=\"/data\">" +
            "<![CDATA[option=]]><xsl:value-of select=\"contact/gendercode\" />" +
            "<![CDATA[|money=]]><xsl:value-of select=\"contact/creditlimit\" />" +
            "<![CDATA[|flag=]]><xsl:value-of select=\"contact/donotemail\" />" +
            "<![CDATA[|date=]]><xsl:value-of select=\"contact/birthdate\" />" +
            "<![CDATA[|int=]]><xsl:value-of select=\"contact/numberofchildren\" />" +
            "<![CDATA[|decimal=]]><xsl:value-of select=\"contact/exchangerate\" /></xsl:template></xsl:stylesheet>";

        // Well-formed XML, but 'contact/' is not a valid XPath expression.
        private const string MalformedXslt =
            "<?xml version=\"1.0\" ?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
            "<xsl:output method=\"text\" /><xsl:template match=\"/data\">" +
            "<xsl:value-of select=\"contact/\" /></xsl:template></xsl:stylesheet>";

        private static Dictionary<string, Entity> Context(Entity contact, Entity user = null)
        {
            var entities = new Dictionary<string, Entity> { ["contact"] = contact };
            if (user != null)
                entities["systemuser"] = user;
            return entities;
        }

        [Fact]
        public void RendersStaticSubject()
        {
            var result = EmailTemplateRenderer.Render(SubjectXslt, Context(new Entity("contact")));

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
            var user = new Entity("systemuser") { ["fullname"] = "Admin User" };

            var result = EmailTemplateRenderer.Render(BodyXslt, Context(contact, user));

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
            var contact = new Entity("contact") { ["salutation"] = "Ms" };

            var result = EmailTemplateRenderer.Render(BodyXslt, Context(contact));

            Assert.Contains("Dear Ms", result);
            Assert.Contains("Valued Customer", result);
            Assert.Contains("Street Address: No Address Provided", result);
        }

        [Fact]
        public void UsesXsltDefaultsWhenAttributeIsEmpty()
        {
            // An empty value must be omitted from the document, not written as an empty element,
            // or xsl:when would match it and suppress the default.
            var contact = new Entity("contact") { ["salutation"] = "Ms", ["lastname"] = "" };

            var result = EmailTemplateRenderer.Render(BodyXslt, Context(contact));

            Assert.Contains("Valued Customer", result);
        }

        [Fact]
        public void MergesLookupAsRecordId()
        {
            var accountId = new Guid("3c2e0869-d1a6-f111-b8de-70a8a57d382b");
            // The name is populated to prove it is deliberately not what gets merged.
            var contact = new Entity("contact")
            {
                ["parentcustomerid"] = new EntityReference("account", accountId) { Name = "Probe Account A/S" }
            };

            var result = EmailTemplateRenderer.Render(LookupXslt, Context(contact));

            Assert.Equal("company={3C2E0869-D1A6-F111-B8DE-70A8A57D382B}", result);
        }

        [Fact]
        public void MergesScalarAttributesInvariantly()
        {
            var contact = new Entity("contact")
            {
                ["gendercode"] = new OptionSetValue(1),
                ["creditlimit"] = new Money(1234.5m),
                ["donotemail"] = true,
                ["birthdate"] = new DateTime(2026, 1, 2, 3, 4, 5),
                ["numberofchildren"] = 42,
                ["exchangerate"] = 3.5m
            };

            var result = EmailTemplateRenderer.Render(ScalarsXslt, Context(contact));

            Assert.Equal(
                "option=1|money=1234.5|flag=True|date=01/02/2026 03:04:05|int=42|decimal=3.5",
                result);
        }

        [Fact]
        public void ReturnsPlainTextTemplatesUnchanged()
        {
            var result = EmailTemplateRenderer.Render("Just plain text", new Dictionary<string, Entity>());

            Assert.Equal("Just plain text", result);
        }

        [Fact]
        public void ReturnsNullForNullInput()
        {
            Assert.Null(EmailTemplateRenderer.Render(null, new Dictionary<string, Entity>()));
        }

        [Fact]
        public void RendersWithoutAnyRecords()
        {
            Assert.Equal("Thank you for registering with us", EmailTemplateRenderer.Render(SubjectXslt, null));
        }

        [Fact]
        public void ThrowsWhenStylesheetDoesNotCompile()
        {
            Assert.Throws<FaultException>(
                () => EmailTemplateRenderer.Render(MalformedXslt, new Dictionary<string, Entity>()));
        }
    }
}
