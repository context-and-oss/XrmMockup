using System.ServiceModel;
using System;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestExecuteTransactionRequestHandler : RequestHandlerTestBase
    {
        public TestExecuteTransactionRequestHandler(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void ChoiceInsertionIsRolledBackWithFailedTransaction()
        {
            var request = new ExecuteTransactionRequest
            {
                Requests = new OrganizationRequestCollection
                {
                    new InsertOptionValueRequest { EntityLogicalName = "account", AttributeLogicalName = "preferredcontactmethodcode", Label = new Label("Rolled back", 1033), Value = 100000789 },
                    new DeleteRequest { Target = new EntityReference("account", Guid.NewGuid()) }
                }
            };
            Assert.Throws<FaultException>(() => orgAdminService.Execute(request));
            Assert.DoesNotContain(((PicklistAttributeMetadata)AccountAttribute("preferredcontactmethodcode")).OptionSet.Options, o => o.Value == 100000789);
        }
    }
}
