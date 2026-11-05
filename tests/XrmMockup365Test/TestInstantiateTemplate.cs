using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Linq;
using System.ServiceModel;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestInstantiateTemplate : UnitTestBase
    {
        public TestInstantiateTemplate(XrmMockupFixture fixture) : base(fixture) { }

        // Stylesheets in the shape Dataverse stores template subject/body in.
        private const string SubjectXslt =
            "<?xml version=\"1.0\" ?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
            "<xsl:output method=\"text\" indent=\"no\" /><xsl:template match=\"/data\">" +
            "<![CDATA[Thank you for registering with us]]></xsl:template></xsl:stylesheet>";

        private const string BodyXslt =
            "<?xml version=\"1.0\" ?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">" +
            "<xsl:output method=\"text\" indent=\"no\"/><xsl:template match=\"/data\">" +
            "<![CDATA[Dear ]]><xsl:value-of select=\"contact/lastname\" />" +
            "<![CDATA[, from ]]><xsl:value-of select=\"systemuser/firstname\" /></xsl:template></xsl:stylesheet>";

        private int ObjectTypeCode(string logicalName)
        {
            var response = (RetrieveEntityResponse)orgAdminService.Execute(new RetrieveEntityRequest
            {
                LogicalName = logicalName,
                EntityFilters = EntityFilters.Entity
            });
            return response.EntityMetadata.ObjectTypeCode.Value;
        }

        private Template CreateTemplate(string boundTo)
        {
            var template = new Template
            {
                Title = "Registration",
                Subject = SubjectXslt,
                Body = BodyXslt,
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

        private Contact CreateContact()
        {
            var contact = new Contact { FirstName = "Test", LastName = "Andersen" };
            contact.Id = orgAdminUIService.Create(contact);
            return contact;
        }

        // Dataverse wraps the merged body in this envelope: LF line breaks, trailing newline.
        private static string HtmlEnvelope(string body) =>
            "<html>\n<head>\n" +
            "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">\n" +
            "</head>\n<body>\n" + body + "\n</body>\n</html>\n";

        private Entity Instantiate(Template template, Guid objectId, string objectType = Contact.EntityLogicalName)
        {
            var response = (InstantiateTemplateResponse)orgAdminUIService.Execute(new InstantiateTemplateRequest
            {
                TemplateId = template.Id,
                ObjectId = objectId,
                ObjectType = objectType
            });
            return Assert.Single(response.EntityCollection.Entities);
        }

        [Fact]
        public void TestInstantiateTemplateMergesTemplateContent()
        {
            orgAdminService.Update(new SystemUser { Id = crm.AdminUser.Id, FirstName = "Sender" });

            var contact = CreateContact();
            var template = CreateTemplate(Contact.EntityLogicalName);

            var email = Instantiate(template, contact.Id);

            // Merged against both the target record and the calling user.
            Assert.Equal("Thank you for registering with us", email.GetAttributeValue<string>("subject"));
            Assert.Equal(HtmlEnvelope("Dear Andersen, from Sender"), email.GetAttributeValue<string>("description"));
        }

        [Fact]
        public void TestInstantiateTemplateReturnsUnsavedContentOnly()
        {
            var contact = CreateContact();
            var template = CreateTemplate(Contact.EntityLogicalName);

            var email = Instantiate(template, contact.Id);

            Assert.Equal(Email.EntityLogicalName, email.LogicalName);
            Assert.Equal(new[] { "description", "subject" }, email.Attributes.Keys.OrderBy(k => k).ToArray());
            Assert.Equal(Guid.Empty, email.Id);

            using (var context = new Xrm(orgAdminService))
            {
                Assert.Empty(context.EmailSet.ToList());
            }
        }

        [Fact]
        public void TestInstantiateTemplateValidatesRequest()
        {
            // All guards raise FaultException, so each case asserts the message to prove the guard
            // it names is the one that fired. The messages are Dataverse's own.
            var id = Guid.NewGuid();

            Assert.Equal("Template id should be set.", Assert.Throws<FaultException>(() =>
                orgAdminUIService.Execute(new InstantiateTemplateRequest
                { ObjectId = id, ObjectType = Contact.EntityLogicalName })).Message);

            Assert.Equal("Object id should be set.", Assert.Throws<FaultException>(() =>
                orgAdminUIService.Execute(new InstantiateTemplateRequest
                { TemplateId = id, ObjectType = Contact.EntityLogicalName })).Message);

            Assert.Equal("Required field 'ObjectType' is missing for RequestName='InstantiateTemplate'", Assert.Throws<FaultException>(() =>
                orgAdminUIService.Execute(new InstantiateTemplateRequest
                { TemplateId = id, ObjectId = id })).Message);

            Assert.Equal("Expected non-empty string.", Assert.Throws<FaultException>(() =>
                orgAdminUIService.Execute(new InstantiateTemplateRequest
                { TemplateId = id, ObjectId = id, ObjectType = "" })).Message);
        }

        [Fact]
        public void TestInstantiateTemplateThrowsWhenReferencedRecordDoesNotExist()
        {
            var contact = CreateContact();
            var template = CreateTemplate(Contact.EntityLogicalName);

            var missingTemplate = Assert.Throws<FaultException>(() =>
                orgAdminUIService.Execute(new InstantiateTemplateRequest
                {
                    TemplateId = Guid.NewGuid(),
                    ObjectId = contact.Id,
                    ObjectType = Contact.EntityLogicalName
                }));
            Assert.Contains("template With Id =", missingTemplate.Message);

            var missingObject = Assert.Throws<FaultException>(() => Instantiate(template, Guid.NewGuid()));
            Assert.Contains("contact With Id =", missingObject.Message);
        }

        [Fact]
        public void TestInstantiateTemplateThrowsWhenTemplateTypeMismatch()
        {
            var contact = CreateContact();
            var template = CreateTemplate(Account.EntityLogicalName);

            var ex = Assert.Throws<FaultException>(() => Instantiate(template, contact.Id));

            Assert.Equal(
                $"Template type is incorrect for given objectType {ObjectTypeCode("contact")} != {ObjectTypeCode("account")} template.templatetypecode",
                ex.Message);
        }
    }
}
