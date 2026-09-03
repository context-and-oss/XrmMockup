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
            var template = new Template
            {
                Title = "Registration",
                Subject = subject,
                Body = body,
                IsPersonal = false,
                LanguageCode = 1033
            };
            // Dataverse returns templatetypecode as the logical name, but the test metadata gives
            // the attribute an option set, so the mock only stores the integer object type code
            // (see DbAttributeTypeMap). The generated string property therefore cannot be used.
            template["templatetypecode"] = ObjectTypeCode(boundTo);
            template.Id = orgAdminService.Create(template);
            return template;
        }

        // Dataverse wraps the merged body in this envelope: LF line breaks, trailing newline.
        private static string HtmlEnvelope(string body) =>
            "<html>\n<head>\n" +
            "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">\n" +
            "</head>\n<body>\n" + body + "\n</body>\n</html>\n";

        private SendEmailFromTemplateRequest BuildRequest(Template template, Contact regarding) =>
            BuildRequest(BuildEmail(regarding), template, regarding);

        private SendEmailFromTemplateRequest BuildRequest(Email target, Template template, Contact regarding) =>
            new SendEmailFromTemplateRequest
            {
                Target = target,
                TemplateId = template.Id,
                RegardingId = regarding.Id,
                RegardingType = Contact.EntityLogicalName
            };

        private Email RetrieveEmail(Guid id, params string[] columns) =>
            orgAdminService.Retrieve(Email.EntityLogicalName, id, new ColumnSet(columns)).ToEntity<Email>();

        [Fact]
        public void TestSendEmailFromTemplateCreatesAndSendsEmail()
        {
            var contact = CreateRecipient();
            var template = CreateContactTemplate();
            var target = BuildEmail(contact);

            var response = orgAdminUIService.Execute(BuildRequest(target, template, contact)) as SendEmailFromTemplateResponse;

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

            // The caller's entity is left as it was; Dataverse merges into its own copy.
            Assert.Equal("Test Email From Template", target.Subject);
            Assert.False(target.Contains("description"));
            Assert.False(target.Contains("regardingobjectid"));
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

            var email = RetrieveEmail(response.Id, "subject", "description");

            // The template replaces the caller's subject, and the body is fully determined by it.
            // The envelope matches Dataverse byte for byte; the markup inside does not, since
            // Dataverse re-serialises it (lower-cased tags, line breaks around block elements).
            Assert.Equal("Thank you for registering with us", email.Subject);
            Assert.Equal(HtmlEnvelope("<P>Dear Smith, your e-mail is smith@test.com.</P>"), email.Description);
        }

        [Fact]
        public void TestSendEmailFromTemplateMergesSenderFields()
        {
            orgAdminService.Update(new SystemUser { Id = crm.AdminUser.Id, FirstName = "Sender" });

            var contact = CreateRecipient("sender@test.com");
            var template = CreateContactTemplate(SubjectXslt, SenderBodyXslt);

            var response = orgAdminUIService.Execute(BuildRequest(template, contact)) as SendEmailFromTemplateResponse;
            Assert.NotNull(response);

            var email = RetrieveEmail(response.Id, "description");

            Assert.Equal(HtmlEnvelope("From: Sender"), email.Description);
        }

        [Fact]
        public void TestSendEmailFromTemplateSenderWinsOverRegardingUser()
        {
            // Regarding record and sender compete for the systemuser key in the render context.
            // Dataverse merges the sender and leaves regardingobjectid empty, since the lookup
            // cannot target a systemuser.
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

            var email = RetrieveEmail(response.Id, "description", "regardingobjectid");

            Assert.Equal(HtmlEnvelope("From: Caller"), email.Description);
            Assert.Null(email.RegardingObjectId);
        }

        [Fact]
        public void TestSendEmailFromTemplateSendsFromCallerWhenSenderIsMissing()
        {
            var contact = CreateRecipient("nosender@test.com");
            var template = CreateContactTemplate();
            var target = BuildEmail(contact);
            target.from = null;

            var response = orgAdminUIService.Execute(BuildRequest(target, template, contact)) as SendEmailFromTemplateResponse;
            Assert.NotNull(response);

            var email = RetrieveEmail(response.Id, "from", "statuscode");

            Assert.Equal(crm.AdminUser.Id, Assert.Single(email.from).PartyId.Id);
            Assert.Equal(email_statuscode.PendingSend, email.StatusCode);
        }

        [Fact]
        public void TestSendEmailFromTemplateValidatesRequest()
        {
            // All guards raise FaultException, so each case asserts the message to prove the guard
            // it names is the one that fired. The messages are Dataverse's own.
            var id = Guid.NewGuid();

            Assert.Equal("Template id should be set.", Assert.Throws<FaultException>(() =>
                orgAdminUIService.Execute(new SendEmailFromTemplateRequest
                { Target = new Email(), RegardingId = id, RegardingType = Contact.EntityLogicalName })).Message);

            Assert.Equal("Required field 'Target' is missing for RequestName='SendEmailFromTemplate'", Assert.Throws<FaultException>(() =>
                orgAdminUIService.Execute(new SendEmailFromTemplateRequest
                { TemplateId = id, RegardingId = id, RegardingType = Contact.EntityLogicalName })).Message);

            Assert.Equal("Cannot merge 2 Business entities of different types. Current Entity Type: contact, Entity To Merge Type: email", Assert.Throws<FaultException>(() =>
                orgAdminUIService.Execute(new SendEmailFromTemplateRequest
                { Target = new Contact(), TemplateId = id, RegardingId = id, RegardingType = Contact.EntityLogicalName })).Message);

            Assert.Equal("Object id should be set.", Assert.Throws<FaultException>(() =>
                orgAdminUIService.Execute(new SendEmailFromTemplateRequest
                { Target = new Email(), TemplateId = id, RegardingType = Contact.EntityLogicalName })).Message);

            Assert.Equal("Required field 'RegardingType' is missing for RequestName='SendEmailFromTemplate'", Assert.Throws<FaultException>(() =>
                orgAdminUIService.Execute(new SendEmailFromTemplateRequest
                { Target = new Email(), TemplateId = id, RegardingId = id })).Message);

            Assert.Equal("Expected non-empty string.", Assert.Throws<FaultException>(() =>
                orgAdminUIService.Execute(new SendEmailFromTemplateRequest
                { Target = new Email(), TemplateId = id, RegardingId = id, RegardingType = "" })).Message);
        }

        [Fact]
        public void TestSendEmailFromTemplateThrowsWhenReferencedRecordDoesNotExist()
        {
            var contact = CreateRecipient();
            var template = CreateContactTemplate();

            var missingTemplate = Assert.Throws<FaultException>(() =>
                orgAdminUIService.Execute(new SendEmailFromTemplateRequest
                {
                    Target = BuildEmail(contact),
                    TemplateId = Guid.NewGuid(),
                    RegardingId = contact.Id,
                    RegardingType = Contact.EntityLogicalName
                }));
            Assert.Contains("template With Id =", missingTemplate.Message);

            var missingRegarding = Assert.Throws<FaultException>(() =>
                orgAdminUIService.Execute(new SendEmailFromTemplateRequest
                {
                    Target = BuildEmail(contact),
                    TemplateId = template.Id,
                    RegardingId = Guid.NewGuid(),
                    RegardingType = Contact.EntityLogicalName
                }));
            Assert.Contains("contact With Id =", missingRegarding.Message);
        }

        [Fact]
        public void TestSendEmailFromTemplateThrowsWhenTemplateTypeMismatch()
        {
            var contact = CreateRecipient();
            var template = CreateTemplate(SubjectXslt, BodyXslt, Account.EntityLogicalName);
            var expected = $"Template type is incorrect for given objectType {ObjectTypeCode("contact")} != {ObjectTypeCode("account")} template.templatetypecode";

            var ex = Assert.Throws<FaultException>(() => orgAdminUIService.Execute(BuildRequest(template, contact)));
            Assert.Equal(expected, ex.Message);

            // The type is checked before the regarding record is looked up, as in Dataverse.
            var missing = Assert.Throws<FaultException>(() => orgAdminUIService.Execute(new SendEmailFromTemplateRequest
            {
                Target = BuildEmail(contact),
                TemplateId = template.Id,
                RegardingId = Guid.NewGuid(),
                RegardingType = Contact.EntityLogicalName
            }));
            Assert.Equal(expected, missing.Message);
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
