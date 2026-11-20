using System;
using System.IO;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestAction : UnitTestBase
    {
        public TestAction(XrmMockupFixture fixture) : base(fixture) { }

        // Removed: TestActionExecution. Its 'ActionTest' action created a Lead and set 16 Lead-specific
        // fields (leadqualitycode, salesstagecode, ...), so it can't be migrated without authoring a new
        // action — and action execution that produces output is already covered by TestActionParts.

        [Fact]
        public void TestActionParts()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                // The 'Full action' custom action (category=Action) was registered in the original
                // environment's metadata; the leaned environment doesn't contain it, so load its
                // definition from the regen-safe fixture folder. It only echoes its typed inputs back
                // as Output (no entity-specific logic), so it migrates as-is.
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "OtherWorkflows", "Fullaction.xml"));

                var stringInput = "A string";
                var datetimeInput = DateTime.Now;
                var boolInput = true;
                var decimalInput = 12.3m;
                var floatInput = 412.2f;
                var intInput = 12;
                var moneyInput = new Money(123.7m);
                var pickListInput = new OptionSetValue(3);

                var entity = new Contact();
                entity.Id = orgAdminUIService.Create(entity);

                var req = new OrganizationRequest("Full action");
                req["StringInput"] = stringInput;
                req["Target"] = entity.ToEntityReference();
                var resp = orgAdminUIService.Execute(req);
                Assert.True(resp.Results.ContainsKey("Output"));
                var output = resp["Output"] as string;
               Assert.Equal(stringInput, output);

                req = new OrganizationRequest("Full action");
                req["DateTimeInput"] = datetimeInput;
                req["Target"] = entity.ToEntityReference();
                resp = orgAdminUIService.Execute(req);
                Assert.True(resp.Results.ContainsKey("Output"));
                output = resp["Output"] as string;
               Assert.Equal(datetimeInput.ToString(), output);

                req = new OrganizationRequest("Full action");
                req["BoolInput"] = boolInput;
                req["Target"] = entity.ToEntityReference();
                resp = orgAdminUIService.Execute(req);
                Assert.True(resp.Results.ContainsKey("Output"));
                output = resp["Output"] as string;
               Assert.Equal(boolInput.ToString(), output);

                req = new OrganizationRequest("Full action");
                req["DecimalInput"] = decimalInput;
                req["Target"] = entity.ToEntityReference();
                resp = orgAdminUIService.Execute(req);
                Assert.True(resp.Results.ContainsKey("Output"));
                output = resp["Output"] as string;
               Assert.Equal(decimalInput.ToString(), output);

                req = new OrganizationRequest("Full action");
                req["FloatInput"] = floatInput;
                req["Target"] = entity.ToEntityReference();
                resp = orgAdminUIService.Execute(req);
                Assert.True(resp.Results.ContainsKey("Output"));
                output = resp["Output"] as string;
               Assert.Equal(floatInput.ToString(), output);

                req = new OrganizationRequest("Full action");
                req["IntegerInput"] = intInput;
                req["Target"] = entity.ToEntityReference();
                resp = orgAdminUIService.Execute(req);
                Assert.True(resp.Results.ContainsKey("Output"));
                output = resp["Output"] as string;
               Assert.Equal(intInput.ToString(), output);

                req = new OrganizationRequest("Full action");
                req["PicklistInput"] = pickListInput;
                req["Target"] = entity.ToEntityReference();
                resp = orgAdminUIService.Execute(req);
                Assert.True(resp.Results.ContainsKey("Output"));
                output = resp["Output"] as string;
               Assert.Equal(pickListInput.ToString(), output);

            }
        }
    }
}
