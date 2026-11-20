using System.IO;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestResponseName : RequestHandlerTestBase
    {
        public TestResponseName(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TypedRequestReturnsNamedTypedResponseWithResults()
        {
            var response = Assert.IsType<WhoAmIResponse>(orgAdminService.Execute(new WhoAmIRequest()));

            Assert.Equal("WhoAmI", response.ResponseName);
            Assert.Equal(crm.AdminUser.Id, response.UserId);
            Assert.Equal(crm.RootBusinessUnit.Id, response.BusinessUnitId);
        }

        [Fact]
        public void GenericRequestReturnsNamedTypedResponseWithResults()
        {
            var response = Assert.IsType<WhoAmIResponse>(
                orgAdminService.Execute(new OrganizationRequest("WhoAmI")));

            Assert.Equal("WhoAmI", response.ResponseName);
            Assert.Equal(crm.AdminUser.Id, response.UserId);
            Assert.Equal(crm.RootBusinessUnit.Id, response.BusinessUnitId);
        }

        [Fact]
        public void AssignDispatchReturnsNamedTypedResponse()
        {
            var contact = new Contact();
            contact.Id = orgAdminService.Create(contact);
            var stored = orgAdminService.Retrieve(Contact.EntityLogicalName, contact.Id, new ColumnSet("ownerid"));
            var response = Assert.IsType<AssignResponse>(orgAdminService.Execute(new AssignRequest
            {
                Target = contact.ToEntityReference(),
                Assignee = stored.GetAttributeValue<EntityReference>("ownerid")
            }));

            Assert.Equal("Assign", response.ResponseName);
        }

        [Fact]
        public void SetStateDispatchReturnsNamedTypedResponse()
        {
            var contact = new Contact();
            contact.Id = orgAdminService.Create(contact);
            var stored = orgAdminService.Retrieve(Contact.EntityLogicalName, contact.Id,
                new ColumnSet("statecode", "statuscode"));
            var response = Assert.IsType<SetStateResponse>(orgAdminService.Execute(new SetStateRequest
            {
                EntityMoniker = contact.ToEntityReference(),
                State = stored.GetAttributeValue<OptionSetValue>("statecode"),
                Status = stored.GetAttributeValue<OptionSetValue>("statuscode")
            }));

            Assert.Equal("SetState", response.ResponseName);
        }

        [Fact]
        public void ExecuteMultipleNamesOuterAndNestedResponses()
        {
            var response = Assert.IsType<ExecuteMultipleResponse>(orgAdminService.Execute(new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings { ReturnResponses = true },
                Requests = new OrganizationRequestCollection
                {
                    new WhoAmIRequest(),
                    new OrganizationRequest("WhoAmI")
                }
            }));

            Assert.Equal("ExecuteMultiple", response.ResponseName);
            Assert.Equal(2, response.Responses.Count);
            foreach (var item in response.Responses)
            {
                Assert.Null(item.Fault);
                var nested = Assert.IsType<WhoAmIResponse>(item.Response);
                Assert.Equal("WhoAmI", nested.ResponseName);
                Assert.Equal(crm.AdminUser.Id, nested.UserId);
            }
        }

        [Theory]
        [InlineData(null, "ResponseNameTest")]
        [InlineData("", "ResponseNameTest")]
        [InlineData("ExplicitResponseName", "ExplicitResponseName")]
        [InlineData(" ", " ")]
        public void HandlerResponseIsNamedOnlyWhenNullOrEmpty(string originalName, string expectedName)
        {
            // Use a name different from the request to detect unconditional overwrites.
            var handlerResponse = new OrganizationResponse { ResponseName = originalName };
            handlerResponse.Results["Value"] = 42;
            engine.RequestHandlers.Add(new FixedResponseHandler(engine, handlerResponse));

            var response = orgAdminService.Execute(new OrganizationRequest("ResponseNameTest"));

            Assert.Same(handlerResponse, response);
            Assert.Equal(expectedName, response.ResponseName);
            Assert.Equal(42, response.Results["Value"]);
        }

        [Fact]
        public void NullHandlerResponseRemainsNull()
        {
            engine.RequestHandlers.Add(new FixedResponseHandler(engine, null));

            Assert.Null(orgAdminService.Execute(new OrganizationRequest("ResponseNameTest")));
        }

        [Theory]
        [InlineData("dg_CreateAccount")]
        [InlineData("dg_LegacyCreateAccountApi")]
        public void CustomApiResponseIsNamedAndRetainsOutput(string requestName)
        {
            var response = orgAdminService.Execute(new OrganizationRequest(requestName)
            {
                Parameters = { { "Name", "Response name test account" } }
            });

            Assert.Equal(requestName, response.ResponseName);
            Assert.Equal("Response name test account", response.Results["Name"]);
        }

        [Fact]
        public void CustomActionResponseIsNamedAndRetainsOutput()
        {
            crm.AddWorkflow(Path.Combine("../../..", "Metadata", "OtherWorkflows", "Fullaction.xml"));
            var contact = new Contact();
            contact.Id = orgAdminService.Create(contact);
            var response = orgAdminService.Execute(new OrganizationRequest("Full action")
            {
                Parameters =
                {
                    { "StringInput", "Response name test" },
                    { "Target", contact.ToEntityReference() }
                }
            });

            Assert.Equal("Full action", response.ResponseName);
            Assert.Equal("Response name test", response.Results["Output"]);
        }

        [Fact]
        public void ExceptionFreeRequestReturnsNamedEmptyResponse()
        {
            var response = orgAdminService.Execute(new OrganizationRequest("TestWrongRequest"));

            Assert.Equal("TestWrongRequest", response.ResponseName);
            Assert.Empty(response.Results);
        }

        private sealed class FixedResponseHandler : RequestHandler
        {
            private readonly OrganizationResponse response;

            internal FixedResponseHandler(Core core, OrganizationResponse response)
                : base(core, null, null, null, "ResponseNameTest")
            {
                this.response = response;
            }

            internal override OrganizationResponse Execute(OrganizationRequest request, EntityReference userRef)
                => response;
        }
    }
}
