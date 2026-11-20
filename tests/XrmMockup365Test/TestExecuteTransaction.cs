using System;
using System.Linq;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using System.ServiceModel;

namespace DG.XrmMockupTest
{
    public class TestExecuteTransaction : UnitTestBase
    {
        public TestExecuteTransaction(XrmMockupFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public void TestTransactionSuccessful()
        {
            var request = new ExecuteTransactionRequest
            {
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest
                    {
                        Target = new Entity("account")
                        {
                            ["name"] = "TestAccount"
                        }
                    },
                    new CreateRequest
                    {
                        Target = new Entity("contact")
                        {
                            ["firstname"] = "Test",
                            ["lastname"] = "Contact"
                        }
                    }
                }
            };

            var response = (ExecuteTransactionResponse)orgAdminService.Execute(request);
            Assert.NotNull(response);
            Assert.Equal(2, response.Responses.Count);
            Assert.All(response.Responses, r => Assert.IsType<CreateResponse>(r));
            Assert.All(response.Responses, r => Assert.True(((CreateResponse)r).id != Guid.Empty));
        }

        [Fact]
        public void TestTransactionFailed()
        {
            var request = new ExecuteTransactionRequest
            {
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest
                    {
                        Target = new Entity("account")
                        {
                            ["name"] = "TestAccount"
                        }
                    },
                    new CreateRequest
                    {
                        Target = new Entity("contact")
                        {
                            ["firstname"] = 123456789,
                        }
                    }
                }
            };

            Assert.Throws<InvalidCastException>(() => orgAdminService.Execute(request));
            
            using (var context = new Xrm(orgAdminService))
            {
                var accounts = context.AccountSet.ToList();
                var contacts = context.ContactSet.ToList();
                Assert.Empty(accounts);
                Assert.Empty(contacts);
            }
        }
    }
}