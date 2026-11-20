using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System.Linq;
using System.ServiceModel;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestSendEmail : UnitTestBase
    {
        public TestSendEmail(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestSendEmailRequestSuccessWhenIssueSendTrue()
        {
            var contact = new Contact
            {
                FirstName = "Test",
                EMailAddress1 = "test@test.com"
            };
            contact.Id = orgAdminUIService.Create(contact);

            var email = new Email
            {
                from = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = crm.AdminUser
                    }
                },
                to = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = contact.ToEntityReference()
                    }
                },
                Subject = "Test Email",
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id,
                IssueSend = true
            };

            var response = orgAdminUIService.Execute(sendEmailRequest) as SendEmailResponse;
            Assert.NotNull(response);
            Assert.Equal(email.Subject, response.Subject);

            using (var context = new Xrm(orgAdminUIService))
            {
                var retrievedEmail = context.EmailSet.FirstOrDefault();
                Assert.Equal(email_statecode.Completed, retrievedEmail.StateCode);
                Assert.Equal(email_statuscode.PendingSend, retrievedEmail.StatusCode);
            }
        }

        [Fact]
        public void TestSendEmailRequestSuccessWhenIssueSendFalse()
        {
            var contact = new Contact
            {
                FirstName = "Test",
                EMailAddress1 = "test@test.com"
            };
            contact.Id = orgAdminUIService.Create(contact);

            var email = new Email
            {
                from = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = crm.AdminUser
                    }
                },
                to = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = contact.ToEntityReference()
                    }
                },
                Subject = "Test Email",
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id
            };

            var response = orgAdminUIService.Execute(sendEmailRequest) as SendEmailResponse;
            Assert.NotNull(response);
            Assert.Equal(email.Subject, response.Subject);

            using (var context = new Xrm(orgAdminUIService))
            {
                var retrievedEmail = context.EmailSet.FirstOrDefault();
                Assert.Equal(email_statecode.Completed, retrievedEmail.StateCode);
                Assert.Equal(email_statuscode.Sent, retrievedEmail.StatusCode);
            }
        }

        [Fact]
        public void TestSendEmailRequestSuccessWithUnresolvedRecipient()
        {
            var email = new Email
            {
                from = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = crm.AdminUser
                    }
                },
                to = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        AddressUsed = "test@test.com"
                    }
                },
                Subject = "Test Email",
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id,
                IssueSend = true
            };

            var response = orgAdminUIService.Execute(sendEmailRequest) as SendEmailResponse;
            Assert.NotNull(response);
            Assert.Equal(email.Subject, response.Subject);

            using (var context = new Xrm(orgAdminUIService))
            {
                var retrievedEmail = context.EmailSet.FirstOrDefault();
                Assert.Equal(email_statecode.Completed, retrievedEmail.StateCode);
                Assert.Equal(email_statuscode.PendingSend, retrievedEmail.StatusCode);
            }
        }

        [Fact]
        public void TestSendEmailRequestSuccessWhenEmailStatusFailed()
        {
            var contact = new Contact
            {
                FirstName = "Test",
                EMailAddress1 = "test@test.com"
            };
            contact.Id = orgAdminUIService.Create(contact);

            var email = new Email
            {
                from = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = crm.AdminUser
                    }
                },
                to = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = contact.ToEntityReference()
                    }
                },
                Subject = "Test Email",
                StatusCode = email_statuscode.Failed
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id,
                IssueSend = true
            };

            var response = orgAdminUIService.Execute(sendEmailRequest) as SendEmailResponse;
            Assert.NotNull(response);
            Assert.Equal(email.Subject, response.Subject);

            using (var context = new Xrm(orgAdminUIService))
            {
                var retrievedEmail = context.EmailSet.FirstOrDefault();
                Assert.Equal(email_statecode.Completed, retrievedEmail.StateCode);
                Assert.Equal(email_statuscode.PendingSend, retrievedEmail.StatusCode);
            }
        }

        [Fact]
        public void TestSendEmailRequestFailsWhenTryingToSendAgain()
        {
            var contact = new Contact
            {
                FirstName = "Test",
                EMailAddress1 = "test@test.com"
            };
            contact.Id = orgAdminUIService.Create(contact);

            var email = new Email
            {
                from = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = crm.AdminUser
                    }
                },
                to = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = contact.ToEntityReference()
                    }
                },
                Subject = "Test Email",
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id,
                IssueSend = true
            };

            orgAdminUIService.Execute(sendEmailRequest);

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailRequest));
        }

        [Fact]
        public void TestSendEmailRequestFailsWhenEmailDirectionCodeIncoming()
        {
            var email = new Email
            {
                from = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = crm.AdminUser
                    }
                },
                to = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        AddressUsed = "test@test.com"
                    }
                },
                Subject = "Test Email",
                DirectionCode = false
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id,
                IssueSend = true
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailRequest));
        }

        [Fact]
        public void TestSendEmailRequestFailsWhenNoSender()
        {
            var email = new Email();
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailRequest));
        }

        [Fact]
        public void TestSendEmailRequestFailsWhenSenderNotCreated()
        {
            var contact = new Contact
            {
                FirstName = "Test",
                EMailAddress1 = "test@test.com"
            };

            var email = new Email
            {
                from = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = contact.ToEntityReference()
                    }
                }
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailRequest));
        }

        [Fact]
        public void TestSendEmailRequestFailsWhenSenderUnresolved()
        {
            var email = new Email
            {
                from = new ActivityParty[]
                {
                    new ActivityParty()
                    {
                        AddressUsed = "test@test.com"
                    }
                }
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailRequest));
        }

        [Fact]
        public void TestSendEmailRequestFailsWhenMoreThanOneSender()
        {
            var email = new Email
            {
                from = new ActivityParty[]
                {
                    new ActivityParty(),
                    new ActivityParty(),
                }
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailRequest));
        }

        [Fact]
        public void TestSendEmailRequestFailsWhenNoRecipients()
        {
            var email = new Email
            {
                from = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = crm.AdminUser
                    }
                }
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailRequest));
        }

        [Fact]
        public void TestSendEmailRequestFailsWhenRecipientInvalid()
        {
            var email = new Email
            {
                from = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = crm.AdminUser
                    }
                },
                to = new ActivityParty[]
                {
                    new ActivityParty()
                }
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailRequest));
        }

        [Fact]
        public void TestSendEmailRequestFailsWhenRecipientHasNoEmailAddress1()
        {
            var contact = new Contact
            {
                FirstName = "Test"
            };
            contact.Id = orgAdminUIService.Create(contact);

            var email = new Email
            {
                from = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = crm.AdminUser
                    }
                },
                to = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = contact.ToEntityReference()
                    }
                },
                Subject = "Test Email",
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailRequest));
        }

        [Fact]
        public void TestSendEmailRequestSuccessWithSystemUserRecipient()
        {
            var email = new Email
            {
                from = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = crm.AdminUser
                    }
                },
                to = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = testUser1.ToEntityReference()
                    }
                },
                Subject = "Test Email",
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id,
                IssueSend = true
            };

            var response = orgAdminUIService.Execute(sendEmailRequest) as SendEmailResponse;
            Assert.NotNull(response);
            Assert.Equal(email.Subject, response.Subject);
        }

        [Fact]
        public void TestSendEmailRequestFailsWhenSystemUserRecipientHasNoEmailAddress()
        {
            var userWithNoEmail = new SystemUser
            {
                BusinessUnitId = crm.RootBusinessUnit,
                IsLicensed = true
            };
            userWithNoEmail = crm.CreateUser(orgAdminService, userWithNoEmail, new System.Guid[0]).ToEntity<SystemUser>();

            var email = new Email
            {
                from = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = crm.AdminUser
                    }
                },
                to = new ActivityParty[]
                {
                    new ActivityParty
                    {
                        PartyId = userWithNoEmail.ToEntityReference()
                    }
                },
                Subject = "Test Email",
            };
            email.Id = orgAdminUIService.Create(email);

            var sendEmailRequest = new SendEmailRequest
            {
                EmailId = email.Id
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(sendEmailRequest));
        }
    }
}
