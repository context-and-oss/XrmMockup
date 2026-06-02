using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.ServiceModel;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestSendEmailFromTemplate : UnitTestBase
    {
        public TestSendEmailFromTemplate(XrmMockupFixture fixture) : base(fixture) { }

        private Email BuildEmail(Contact recipient)
        {
            return new Email
            {
                From = new ActivityParty[]
                {
                    new ActivityParty { PartyId = crm.AdminUser }
                },
                To = new ActivityParty[]
                {
                    new ActivityParty { PartyId = recipient.ToEntityReference() }
                },
                Subject = "Test Email From Template",
            };
        }

        private int ObjectTypeCode(string logicalName)
        {
            var response = (RetrieveEntityResponse)orgAdminService.Execute(new RetrieveEntityRequest
            {
                LogicalName = logicalName,
                EntityFilters = EntityFilters.Entity
            });
            return response.EntityMetadata.ObjectTypeCode.Value;
        }

        private Template CreateContactTemplate()
        {
            var template = new Template
            {
                Title = "Registration",
                Subject = SubjectXslt,
                Body = BodyXslt,
                IsPersonal = false,
                LanguageCode = 1033
            };
            // templatetypecode is an OptionSet-backed EntityName attribute, so XrmMockup stores it
            // as the entity's integer object type code rather than the logical-name string.
            template["templatetypecode"] = ObjectTypeCode(Contact.EntityLogicalName);
            template.Id = orgAdminService.Create(template);
            return template;
        }

        [Fact]
        public void TestSendEmailFromTemplateCreatesAndSendsEmail()
        {
            var contact = new Contact
            {
                FirstName = "Test",
                EMailAddress1 = "test@test.com"
            };
            contact.Id = orgAdminUIService.Create(contact);

            var template = CreateContactTemplate();

            var request = new SendEmailFromTemplateRequest
            {
                Target = BuildEmail(contact),
                TemplateId = template.Id,
                RegardingId = contact.Id,
                RegardingType = Contact.EntityLogicalName
            };

            var response = orgAdminUIService.Execute(request) as SendEmailFromTemplateResponse;

            Assert.NotNull(response);
            Assert.NotEqual(Guid.Empty, response.Id);

            using (var context = new Xrm(orgAdminUIService))
            {
                var email = context.EmailSet.FirstOrDefault(e => e.Id == response.Id);
                Assert.NotNull(email);
                Assert.Equal(EmailState.Completed, email.StateCode);
                Assert.Equal(Email_StatusCode.PendingSend, email.StatusCode);
                Assert.Equal(contact.Id, email.RegardingObjectId.Id);
            }
        }

        // The subject/body below are XSLT stylesheets in the same shape Dataverse stores them in.
        private const string SubjectXslt =
            "<?xml version=\"1.0\" ?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
            "<xsl:output method=\"text\" indent=\"no\" /><xsl:template match=\"/data\">" +
            "<![CDATA[Thank you for registering with us]]></xsl:template></xsl:stylesheet>";

        private const string BodyXslt =
            "<?xml version=\"1.0\" ?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
            "<xsl:output method=\"text\" indent=\"no\"/><xsl:template match=\"/data\">" +
            "<![CDATA[<P>Dear ]]><xsl:choose><xsl:when test=\"contact/lastname\"><xsl:value-of select=\"contact/lastname\" /></xsl:when><xsl:otherwise>Valued Customer</xsl:otherwise></xsl:choose>" +
            "<![CDATA[, your e-mail is ]]><xsl:choose><xsl:when test=\"contact/emailaddress1\"><xsl:value-of select=\"contact/emailaddress1\" /></xsl:when><xsl:otherwise></xsl:otherwise></xsl:choose>" +
            "<![CDATA[.</P>]]></xsl:template></xsl:stylesheet>";

        [Fact]
        public void TestSendEmailFromTemplateRendersTemplateContent()
        {
            var template = CreateContactTemplate();

            var contact = new Contact
            {
                FirstName = "Test",
                LastName = "Smith",
                EMailAddress1 = "smith@test.com"
            };
            contact.Id = orgAdminUIService.Create(contact);

            var request = new SendEmailFromTemplateRequest
            {
                Target = BuildEmail(contact),
                TemplateId = template.Id,
                RegardingId = contact.Id,
                RegardingType = Contact.EntityLogicalName
            };

            var response = orgAdminUIService.Execute(request) as SendEmailFromTemplateResponse;
            Assert.NotNull(response);
            Assert.NotEqual(Guid.Empty, response.Id);

            var email = orgAdminService
                .Retrieve(Email.EntityLogicalName, response.Id, new ColumnSet("subject", "description", "statecode"))
                .ToEntity<Email>();

            // Subject and body were rendered from the template, overriding the caller's subject.
            Assert.Equal("Thank you for registering with us", email.Subject);
            Assert.Contains("Dear", email.Description);
            Assert.Contains("Smith", email.Description);
            Assert.Contains("smith@test.com", email.Description);
            Assert.Equal(EmailState.Completed, email.StateCode);
        }

        [Fact]
        public void TestSendEmailFromTemplateThrowsWhenTemplateIdMissing()
        {
            var contact = new Contact { FirstName = "Test", EMailAddress1 = "test@test.com" };
            contact.Id = orgAdminUIService.Create(contact);

            var request = new SendEmailFromTemplateRequest
            {
                Target = BuildEmail(contact),
                RegardingId = contact.Id,
                RegardingType = Contact.EntityLogicalName
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(request));
        }

        [Fact]
        public void TestSendEmailFromTemplateThrowsWhenRegardingMissing()
        {
            var contact = new Contact { FirstName = "Test", EMailAddress1 = "test@test.com" };
            contact.Id = orgAdminUIService.Create(contact);

            var request = new SendEmailFromTemplateRequest
            {
                Target = BuildEmail(contact),
                TemplateId = Guid.NewGuid()
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(request));
        }

        [Fact]
        public void TestSendEmailFromTemplateThrowsWhenTemplateDoesNotExist()
        {
            var contact = new Contact { FirstName = "Test", EMailAddress1 = "test@test.com" };
            contact.Id = orgAdminUIService.Create(contact);

            var request = new SendEmailFromTemplateRequest
            {
                Target = BuildEmail(contact),
                TemplateId = Guid.NewGuid(),
                RegardingId = contact.Id,
                RegardingType = Contact.EntityLogicalName
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(request));
        }

        [Fact]
        public void TestSendEmailFromTemplateThrowsWhenTemplateTypeMismatch()
        {
            var contact = new Contact { FirstName = "Test", EMailAddress1 = "test@test.com" };
            contact.Id = orgAdminUIService.Create(contact);

            // Template is bound to 'account' but the regarding record is a contact.
            var template = new Template
            {
                Title = "Mismatch",
                Subject = SubjectXslt,
                Body = BodyXslt
            };
            template["templatetypecode"] = ObjectTypeCode(Account.EntityLogicalName);
            template.Id = orgAdminService.Create(template);

            var request = new SendEmailFromTemplateRequest
            {
                Target = BuildEmail(contact),
                TemplateId = template.Id,
                RegardingId = contact.Id,
                RegardingType = Contact.EntityLogicalName
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(request));
        }
    }
}
