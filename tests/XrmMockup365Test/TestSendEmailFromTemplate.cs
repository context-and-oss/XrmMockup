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

        // Merges a field from the sending systemuser rather than the regarding record.
        private const string SenderBodyXslt =
            "<?xml version=\"1.0\" ?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
            "<xsl:output method=\"text\" indent=\"no\"/><xsl:template match=\"/data\">" +
            "<![CDATA[From: ]]><xsl:value-of select=\"systemuser/firstname\" /></xsl:template></xsl:stylesheet>";

        // Well-formed XML, but 'contact/' is not a valid XPath expression.
        private const string MalformedXslt =
            "<?xml version=\"1.0\" ?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
            "<xsl:output method=\"text\" /><xsl:template match=\"/data\">" +
            "<xsl:value-of select=\"contact/\" /></xsl:template></xsl:stylesheet>";

        private Email BuildEmail(Contact recipient)
        {
            return new Email
            {
                from = new ActivityParty[]
                {
                    new ActivityParty { PartyId = crm.AdminUser }
                },
                to = new ActivityParty[]
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

        private Template CreateContactTemplate() => CreateContactTemplate(SubjectXslt, BodyXslt);

        private Template CreateContactTemplate(string subject, string body) =>
            CreateTemplate(subject, body, Contact.EntityLogicalName);

        private Template CreateTemplate(string subject, string body, string boundTo)
        {
            var template = new Template
            {
                Title = "Registration",
                Subject = subject,
                Body = body,
                IsPersonal = false,
                LanguageCode = 1033
            };
            // Stored as the entity's integer object type code - see ValidateTemplateType.
            template["templatetypecode"] = ObjectTypeCode(boundTo);
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
                Assert.Equal(email_statecode.Completed, email.StateCode);
                Assert.Equal(email_statuscode.PendingSend, email.StatusCode);
                Assert.Equal(contact.Id, email.RegardingObjectId.Id);
            }
        }

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
            Assert.Equal(email_statecode.Completed, email.StateCode);
        }

        [Fact]
        public void TestSendEmailFromTemplateMergesSenderFields()
        {
            // Give the user that orgAdminUIService runs as a known, mergeable value, then verify
            // it flows through the sender side of the render context (not the regarding record).
            orgAdminService.Update(new Entity("systemuser", crm.AdminUser.Id) { ["firstname"] = "Sender" });

            var contact = new Contact { FirstName = "Test", EMailAddress1 = "sender@test.com" };
            contact.Id = orgAdminUIService.Create(contact);

            var template = CreateContactTemplate(SubjectXslt, SenderBodyXslt);

            var request = new SendEmailFromTemplateRequest
            {
                Target = BuildEmail(contact),
                TemplateId = template.Id,
                RegardingId = contact.Id,
                RegardingType = Contact.EntityLogicalName
            };

            var response = orgAdminUIService.Execute(request) as SendEmailFromTemplateResponse;
            Assert.NotNull(response);

            var email = orgAdminService
                .Retrieve(Email.EntityLogicalName, response.Id, new ColumnSet("description"))
                .ToEntity<Email>();

            Assert.Equal("From: Sender", email.Description);
        }

        [Fact]
        public void TestSendEmailFromTemplateRegardingUserWinsOverSender()
        {
            // Regarding record and sender share the systemuser key in the render context. The
            // regarding user must win, otherwise a user template silently merges the caller.
            orgAdminService.Update(new Entity("systemuser", crm.AdminUser.Id) { ["firstname"] = "Caller" });
            orgAdminService.Update(new Entity("systemuser", testUser1.Id) { ["firstname"] = "Regarding" });

            var contact = new Contact { FirstName = "Test", EMailAddress1 = "user@test.com" };
            contact.Id = orgAdminUIService.Create(contact);

            var template = CreateTemplate(SubjectXslt, SenderBodyXslt, "systemuser");

            var request = new SendEmailFromTemplateRequest
            {
                Target = BuildEmail(contact),
                TemplateId = template.Id,
                RegardingId = testUser1.Id,
                RegardingType = "systemuser"
            };

            var response = orgAdminUIService.Execute(request) as SendEmailFromTemplateResponse;
            Assert.NotNull(response);

            var email = orgAdminService
                .Retrieve(Email.EntityLogicalName, response.Id, new ColumnSet("description"))
                .ToEntity<Email>();

            Assert.Equal("From: Regarding", email.Description);
        }

        [Fact]
        public void TestSendEmailFromTemplateRendersPlainTextTemplate()
        {
            var contact = new Contact { FirstName = "Test", EMailAddress1 = "plain@test.com" };
            contact.Id = orgAdminUIService.Create(contact);

            // Not every template is an XSLT stylesheet; literal text must pass through unchanged.
            var template = CreateContactTemplate("Plain subject", "Plain body text");

            var request = new SendEmailFromTemplateRequest
            {
                Target = BuildEmail(contact),
                TemplateId = template.Id,
                RegardingId = contact.Id,
                RegardingType = Contact.EntityLogicalName
            };

            var response = orgAdminUIService.Execute(request) as SendEmailFromTemplateResponse;
            Assert.NotNull(response);

            var email = orgAdminService
                .Retrieve(Email.EntityLogicalName, response.Id, new ColumnSet("subject", "description"))
                .ToEntity<Email>();

            Assert.Equal("Plain subject", email.Subject);
            Assert.Equal("Plain body text", email.Description);
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

            var template = CreateTemplate(SubjectXslt, BodyXslt, Account.EntityLogicalName);

            var request = new SendEmailFromTemplateRequest
            {
                Target = BuildEmail(contact),
                TemplateId = template.Id,
                RegardingId = contact.Id,
                RegardingType = Contact.EntityLogicalName
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(request));
        }

        [Fact]
        public void TestSendEmailFromTemplateThrowsWhenTemplateDoesNotCompile()
        {
            var contact = new Contact { FirstName = "Test", EMailAddress1 = "broken@test.com" };
            contact.Id = orgAdminUIService.Create(contact);

            // A broken stylesheet must fault, not put raw XSLT markup in the sent e-mail.
            var template = CreateContactTemplate(SubjectXslt, MalformedXslt);

            var request = new SendEmailFromTemplateRequest
            {
                Target = BuildEmail(contact),
                TemplateId = template.Id,
                RegardingId = contact.Id,
                RegardingType = Contact.EntityLogicalName
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(request));
        }

        [Fact]
        public void TestSendEmailFromTemplateThrowsWhenCallerCannotReadTemplate()
        {
            var contact = new Contact { FirstName = "Test", EMailAddress1 = "denied@test.com" };
            contact.Id = orgAdminUIService.Create(contact);

            var template = CreateContactTemplate();

            var request = new SendEmailFromTemplateRequest
            {
                Target = BuildEmail(contact),
                TemplateId = template.Id,
                RegardingId = contact.Id,
                RegardingType = Contact.EntityLogicalName
            };

            var ex = Assert.Throws<FaultException>(() => testUser1Service.Execute(request));
            Assert.Contains("template", ex.Message);
        }
    }
}
