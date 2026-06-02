using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
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

        [Fact]
        public void TestSendEmailFromTemplateCreatesAndSendsEmail()
        {
            var contact = new Contact
            {
                FirstName = "Test",
                EMailAddress1 = "test@test.com"
            };
            contact.Id = orgAdminUIService.Create(contact);

            var request = new SendEmailFromTemplateRequest
            {
                Target = BuildEmail(contact),
                TemplateId = Guid.NewGuid(),
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
    }
}
