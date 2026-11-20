using DG.Some.Namespace.Test;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using System.IO;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestExecutionOrder : UnitTestBase
    {
        public TestExecutionOrder(XrmMockupFixture fixture) : base(fixture) { crm.DisableRegisteredPlugins(true); }

        [Fact]
        public void TestSyncWorkflowThenSyncPlugin()
        {
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary, typeof(Test1Plugin1));                             // Appends: pSync1
            crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test2", "Test2WorkflowSync1.xml"));    // Appends: Sync1

            var account = new Account()
            {
                Name = "Test",
            };
            account.Id = orgAdminService.Create(account);

            account.EMailAddress1 = "trigger@test.dk";
            orgAdminService.Update(account);

            var retrievedAccount = Account.Retrieve(orgAdminService, account.Id, x => x.Name);

            Assert.Equal(account.Name + "pSync1", retrievedAccount.Name);
        }

        [Fact]
        public void TestAsyncPluginThenAsyncWorkflow()
        {
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary, typeof(Test2Plugin1));                             // Appends: ASync1
            crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test1", "Test1WorkflowASync2.xml"));   // Appends: ASync2

            var account = new Account()
            {
                Name = "Test",
            };
            account.Id = orgAdminService.Create(account);

            account.EMailAddress1 = "trigger@test.dk";
            orgAdminService.Update(account);

            var retrievedAccount = Account.Retrieve(orgAdminService, account.Id, x => x.Name);

            Assert.Equal(account.Name + "ASync1ASync2", retrievedAccount.Name);
        }

        [Fact]
        public void TestSyncWorkflowThenAsyncPlugin()
        {
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary, typeof(Test2Plugin1));                             // Appends: ASync1
            crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test2", "Test2WorkflowSync1.xml"));    // Appends: Sync1

            var account = new Account()
            {
                Name = "Test",
            };
            account.Id = orgAdminService.Create(account);

            account.EMailAddress1 = "trigger@test.dk";
            orgAdminService.Update(account);

            var retrievedAccount = Account.Retrieve(orgAdminService, account.Id, x => x.Name);

            Assert.Equal(account.Name + "Sync1ASync1", retrievedAccount.Name);
        }

        [Fact]
        public void TestSyncPluginThenAsyncWorkflow()
        {
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary, typeof(Test1Plugin1));                             // Appends: pSync1
            crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test1", "Test1WorkflowASync2.xml"));   // Appends: ASync2

            var account = new Account()
            {
                Name = "Test",
            };
            account.Id = orgAdminService.Create(account);

            account.EMailAddress1 = "trigger@test.dk";
            orgAdminService.Update(account);

            var retrievedAccount = Account.Retrieve(orgAdminService, account.Id, x => x.Name);

            Assert.Equal(account.Name + "pSync1ASync2", retrievedAccount.Name);
        }

        [Fact]
        public void TestSyncPluginThenSyncWorkflow()
        {
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary, typeof(TestPlugin0));                              // Appends: Sync0
            crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestSyncAsyncWorkflows", "Test2", "Test2WorkflowSync1.xml"));    // Appends: Sync1

            var account = new Account()
            {
                Name = "Test",
            };
            account.Id = orgAdminService.Create(account);

            account.EMailAddress1 = "trigger@test.dk";
            orgAdminService.Update(account);

            var retrievedAccount = Account.Retrieve(orgAdminService, account.Id, x => x.Name);

            Assert.Equal(account.Name + "Sync1", retrievedAccount.Name);
        }
    }
}
