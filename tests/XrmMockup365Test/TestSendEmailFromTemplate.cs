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

        // Stylesheets in the shape Dataverse stores template subject/body in.
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

        private const string SenderBodyXslt =
            "<?xml version=\"1.0\" ?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
            "<xsl:output method=\"text\" indent=\"no\"/><xsl:template match=\"/data\">" +
            "<![CDATA[From: ]]><xsl:value-of select=\"systemuser/firstname\" /></xsl:template></xsl:stylesheet>";

        // Well-formed XML, but 'contact/' is not a valid XPath expression.
        private const string MalformedXslt =
            "<?xml version=\"1.0\" ?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
            "<xsl:output method=\"text\" /><xsl:template match=\"/data\">" +
            "<xsl:value-of select=\"contact/\" /></xsl:template></xsl:stylesheet>";

        private Contact CreateRecipient(string email = "test@test.com")
        {
            var contact = new Contact { FirstName = "Test", EMailAddress1 = email };
            contact.Id = orgAdminUIService.Create(contact);
            return contact;
        }

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
            var template = NewTemplate(subject, body);
            // Set late-bound: the generated TemplateTypeCode is a string and cannot hold the
            // integer object type code the database stores.
            template["templatetypecode"] = ObjectTypeCode(boundTo);
            template.Id = orgAdminService.Create(template);
            return template;
        }

        private static Template NewTemplate(string subject, string body) => new Template
        {
            Title = "Registration",
            Subject = subject,
            Body = body,
            IsPersonal = false,
            LanguageCode = 1033
        };

        private SendEmailFromTemplateRequest BuildRequest(Template template, Contact regarding) =>
            new SendEmailFromTemplateRequest
            {
                Target = BuildEmail(regarding),
                TemplateId = template.Id,
                RegardingId = regarding.Id,
                RegardingType = Contact.EntityLogicalName
            };

        [Fact]
        public void TestSendEmailFromTemplateCreatesAndSendsEmail()
        {
            var contact = CreateRecipient();
            var template = CreateContactTemplate();

            var response = orgAdminUIService.Execute(BuildRequest(template, contact)) as SendEmailFromTemplateResponse;

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

            var response = orgAdminUIService.Execute(BuildRequest(template, contact)) as SendEmailFromTemplateResponse;
            Assert.NotNull(response);

            var email = orgAdminService
                .Retrieve(Email.EntityLogicalName, response.Id, new ColumnSet("subject", "description"))
                .ToEntity<Email>();

            // The template replaces the caller's subject, and the body is fully determined by it.
            Assert.Equal("Thank you for registering with us", email.Subject);
            Assert.Equal("<P>Dear Smith, your e-mail is smith@test.com.</P>", email.Description);
        }

        [Fact]
        public void TestSendEmailFromTemplateMergesSenderFields()
        {
            orgAdminService.Update(new SystemUser { Id = crm.AdminUser.Id, FirstName = "Sender" });

            var contact = CreateRecipient("sender@test.com");
            var template = CreateContactTemplate(SubjectXslt, SenderBodyXslt);

            var response = orgAdminUIService.Execute(BuildRequest(template, contact)) as SendEmailFromTemplateResponse;
            Assert.NotNull(response);

            var email = orgAdminService
                .Retrieve(Email.EntityLogicalName, response.Id, new ColumnSet("description"))
                .ToEntity<Email>();

            Assert.Equal("From: Sender", email.Description);
        }

        [Fact]
        public void TestSendEmailFromTemplateRegardingUserWinsOverSender()
        {
            // Regarding record and sender compete for the systemuser key in the render context.
            orgAdminService.Update(new SystemUser { Id = crm.AdminUser.Id, FirstName = "Caller" });
            orgAdminService.Update(new SystemUser { Id = testUser1.Id, FirstName = "Regarding" });

            var contact = CreateRecipient("user@test.com");
            var template = CreateTemplate(SubjectXslt, SenderBodyXslt, SystemUser.EntityLogicalName);

            var response = orgAdminUIService.Execute(new SendEmailFromTemplateRequest
            {
                Target = BuildEmail(contact),
                TemplateId = template.Id,
                RegardingId = testUser1.Id,
                RegardingType = SystemUser.EntityLogicalName
            }) as SendEmailFromTemplateResponse;
            Assert.NotNull(response);

            var email = orgAdminService
                .Retrieve(Email.EntityLogicalName, response.Id, new ColumnSet("description"))
                .ToEntity<Email>();

            Assert.Equal("From: Regarding", email.Description);
        }

        [Fact]
        public void TestSendEmailFromTemplateRendersPlainTextTemplate()
        {
            var contact = CreateRecipient("plain@test.com");
            var template = CreateContactTemplate("Plain subject", "Plain body text");

            var response = orgAdminUIService.Execute(BuildRequest(template, contact)) as SendEmailFromTemplateResponse;
            Assert.NotNull(response);

            var email = orgAdminService
                .Retrieve(Email.EntityLogicalName, response.Id, new ColumnSet("subject", "description"))
                .ToEntity<Email>();

            Assert.Equal("Plain subject", email.Subject);
            Assert.Equal("Plain body text", email.Description);
        }

        [Fact]
        public void TestSendEmailFromTemplateSendsWhenTemplateHasNoTypeCode()
        {
            var contact = CreateRecipient("notype@test.com");

            // Without a templatetypecode there is nothing to compare the regarding type against,
            // so the send proceeds rather than faulting.
            var template = NewTemplate(SubjectXslt, BodyXslt);
            template.Id = orgAdminService.Create(template);

            var response = orgAdminUIService.Execute(BuildRequest(template, contact)) as SendEmailFromTemplateResponse;

            Assert.NotNull(response);
            Assert.NotEqual(Guid.Empty, response.Id);
        }

        [Fact]
        public void TestSendEmailFromTemplateThrowsWhenTemplateIdMissing()
        {
            var ex = Assert.Throws<FaultException>(() => orgAdminUIService.Execute(new SendEmailFromTemplateRequest
            {
                Target = new Email(),
                RegardingId = Guid.NewGuid(),
                RegardingType = Contact.EntityLogicalName
            }));

            Assert.Equal("Template id should be set.", ex.Message);
        }

        [Fact]
        public void TestSendEmailFromTemplateThrowsWhenTargetMissing()
        {
            var ex = Assert.Throws<FaultException>(() => orgAdminUIService.Execute(new SendEmailFromTemplateRequest
            {
                TemplateId = Guid.NewGuid(),
                RegardingId = Guid.NewGuid(),
                RegardingType = Contact.EntityLogicalName
            }));

            Assert.Equal("Target email is missing.", ex.Message);
        }

        [Fact]
        public void TestSendEmailFromTemplateThrowsWhenTargetIsNotAnEmail()
        {
            var ex = Assert.Throws<FaultException>(() => orgAdminUIService.Execute(new SendEmailFromTemplateRequest
            {
                Target = new Contact(),
                TemplateId = Guid.NewGuid(),
                RegardingId = Guid.NewGuid(),
                RegardingType = Contact.EntityLogicalName
            }));

            Assert.Equal("Target must be an email entity.", ex.Message);
        }

        [Fact]
        public void TestSendEmailFromTemplateThrowsWhenRegardingIdMissing()
        {
            var ex = Assert.Throws<FaultException>(() => orgAdminUIService.Execute(new SendEmailFromTemplateRequest
            {
                Target = new Email(),
                TemplateId = Guid.NewGuid(),
                RegardingType = Contact.EntityLogicalName
            }));

            Assert.Equal("Regarding id should be set.", ex.Message);
        }

        [Fact]
        public void TestSendEmailFromTemplateThrowsWhenRegardingTypeMissing()
        {
            var ex = Assert.Throws<FaultException>(() => orgAdminUIService.Execute(new SendEmailFromTemplateRequest
            {
                Target = new Email(),
                TemplateId = Guid.NewGuid(),
                RegardingId = Guid.NewGuid()
            }));

            Assert.Equal("Regarding type should be set.", ex.Message);
        }

        [Fact]
        public void TestSendEmailFromTemplateThrowsWhenTemplateDoesNotExist()
        {
            var contact = CreateRecipient();

            var ex = Assert.Throws<FaultException>(() => orgAdminUIService.Execute(new SendEmailFromTemplateRequest
            {
                Target = BuildEmail(contact),
                TemplateId = Guid.NewGuid(),
                RegardingId = contact.Id,
                RegardingType = Contact.EntityLogicalName
            }));

            Assert.Contains("template With Id =", ex.Message);
        }

        [Fact]
        public void TestSendEmailFromTemplateThrowsWhenRegardingDoesNotExist()
        {
            var contact = CreateRecipient();
            var template = CreateContactTemplate();

            var ex = Assert.Throws<FaultException>(() => orgAdminUIService.Execute(new SendEmailFromTemplateRequest
            {
                Target = BuildEmail(contact),
                TemplateId = template.Id,
                RegardingId = Guid.NewGuid(),
                RegardingType = Contact.EntityLogicalName
            }));

            Assert.Contains("contact With Id =", ex.Message);
        }

        [Fact]
        public void TestSendEmailFromTemplateThrowsWhenTemplateTypeMismatch()
        {
            var contact = CreateRecipient();
            var template = CreateTemplate(SubjectXslt, BodyXslt, Account.EntityLogicalName);

            var ex = Assert.Throws<FaultException>(() => orgAdminUIService.Execute(BuildRequest(template, contact)));

            Assert.Contains("template type does not match", ex.Message);
        }

        [Fact]
        public void TestSendEmailFromTemplateThrowsWhenTemplateDoesNotCompile()
        {
            var contact = CreateRecipient("broken@test.com");
            var template = CreateContactTemplate(SubjectXslt, MalformedXslt);

            var ex = Assert.Throws<FaultException>(() => orgAdminUIService.Execute(BuildRequest(template, contact)));

            // The render fault must surface, not be swallowed somewhere in the pipeline.
            Assert.Contains("could not be rendered", ex.Message);
        }

        [Fact]
        public void TestSendEmailFromTemplateThrowsWhenCallerCannotReadTemplate()
        {
            var contact = CreateRecipient("denied@test.com");
            var template = CreateContactTemplate();

            var ex = Assert.Throws<FaultException>(() => testUser1Service.Execute(BuildRequest(template, contact)));

            Assert.Contains("does not have permission to read entity 'template'", ex.Message);
        }
    }
}
