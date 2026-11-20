using DG.Some.Namespace.Test;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestSyncAsyncPlugin : UnitTestBase
    {
        Account account;
        string oldAccountName;
        string newAccountName;

        Contact personel;
        string oldFirstName;
        string newFirstName;

        public TestSyncAsyncPlugin(XrmMockupFixture fixture) : base(fixture) { }
        /* Tests for Sync and Async plugins. Relevant plugins can be found in folder "SyncAsyncTest" */

        /*-------------------------------- Order of plugin declaration --------------------------
         * Registration order of plugins should not interfere with execution. Sync should
         * always execute before Async. */
        [Fact]
        public void Test4ASyncAndSyncPluginFailsWhenAsyncAppliesFirst()
        {
            //OrderOfPluginDeclaration -> Register ASync first, then Sync 
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary,
                typeof(Test4PluginASync),
                typeof(Test4PluginSync));

            oldFirstName = "Something";
            newFirstName = oldFirstName + "Sync1" + "ASync2";

            personel = new Contact()
            {
                FirstName = oldFirstName
            };

            personel.Id = orgAdminService.Create(personel);

            var personelUpd = new Contact(personel.Id)
            {
                EMailAddress1 = "something@test.dk"
            };

            orgAdminService.Update(personelUpd);

            var retrievedPersonel = Contact.Retrieve(orgAdminService, personel.Id, x => x.FirstName);

            Assert.Equal(newFirstName, retrievedPersonel.FirstName);
        }

        [Fact]
        public void Test4SyncAndAsyncPluginSucceedsWhenSyncAppliesFirst()
        {
            //OrderOfPluginDeclaration -> Register Sync first, then ASync 
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary,
                typeof(Test4PluginSync),
                typeof(Test4PluginASync));

            string oldFirstName = "Something";
            string newFirstName = oldFirstName + "Sync1" + "ASync2";

            var personel = new Contact()
            {
                FirstName = oldFirstName
            };

            personel.Id = orgAdminService.Create(personel);

            var personelUpd = new Contact(personel.Id)
            {
                EMailAddress1 = "something@test.dk"
            };

            orgAdminService.Update(personelUpd);

            var retrievedPersonel = Contact.Retrieve(orgAdminService, personel.Id, x => x.FirstName);

            Assert.Equal(newFirstName, retrievedPersonel.FirstName);
        }

        /* Plugin with lowest executionOrder should execute first. If no order given, it is in order of declaration. 
           Sync however always trigger before ASync */
        [Fact]
        public void Test5TwoSyncPluginsLowestExecutionOrderExecutesFirst()
        {
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary,
                typeof(Sync2WithExecutionOrder),
                typeof(Sync1WithExecutionOrder));

            string oldFirstName = "Something";
            string newFirstName = oldFirstName + ", Sync1" + ", Sync2";

            var personel = new Contact()
            {
                FirstName = oldFirstName
            };

            personel.Id = orgAdminService.Create(personel);

            var personelUpd = new Contact(personel.Id)
            {
                EMailAddress1 = "something@test.dk"
            };

            orgAdminService.Update(personelUpd);

            var retrievedPersonel = Contact.Retrieve(orgAdminService, personel.Id, x => x.FirstName);

            Assert.Equal(newFirstName, retrievedPersonel.FirstName);
        }


        //Sync Should trigger first regardless of execution order
        [Fact]
        public void Test5TwoSyncAndAsyncWithLowerExecutionOrderSucceedsWhenSyncsTriggerFirst()
        {
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary,
               typeof(Sync2WithExecutionOrder),
               typeof(ASyncWithExecutionOrder),
               typeof(Sync1WithExecutionOrder));

            string oldFirstName = "Something";
            string newFirstName = oldFirstName + ", Sync1" + ", Sync2" + ", Async";

            var personel = new Contact()
            {
                FirstName = oldFirstName
            };

            personel.Id = orgAdminService.Create(personel);

            var personelUpd = new Contact(personel.Id)
            {
                EMailAddress1 = "something@test.dk"
            };

            orgAdminService.Update(personelUpd);

            var retrievedPersonel = Contact.Retrieve(orgAdminService, personel.Id, x => x.FirstName);

            Assert.Equal(newFirstName, retrievedPersonel.FirstName);
        }



        [Fact]
        public void Test6SyncPluginCallsSyncAndAsyncPlugNoExecutionOrder()
        {
            //Sync plugin calls another Sync and Async plugin. Sync executes first.
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary,
                typeof(Test6Plugin1Sync),
                typeof(Test6Plugin2Async),
                typeof(Test6Plugin3Sync));

            string oldFirstName = "Something";
            string newFirstName = oldFirstName + "Sync1" + "Sync3" + "ASync2";

            var personel = new Contact()
            {
                FirstName = oldFirstName
            };

            personel.Id = orgAdminService.Create(personel);

            var personelUpd = new Contact(personel.Id)
            {
                EMailAddress1 = "something@test.dk"
            };

            orgAdminService.Update(personelUpd);

            var retrievedPersonel = Contact.Retrieve(orgAdminService, personel.Id, x => x.FirstName);

            Assert.Equal(newFirstName, retrievedPersonel.FirstName);
        }

        //--------------------------------------------- Test plugin execution order --------------------------------------------

        [Fact]
        public void Test1Sync1Sync2PostPluginSucceedsWhenOnlySync2Applies()
        {
            /* Two Sync plugins trigger of same emailAddress1 change  - the one with the highest execution order is the only one that executes*/
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary,
                typeof(Test1Plugin1),
                typeof(Test1Plugin2));

            oldAccountName = "Test";
            newAccountName = oldAccountName + "Sync2";

            account = new Account()
            {
                Name = oldAccountName,
            };
            account.Id = orgAdminService.Create(account);

            var accountUpd = new Account(account.Id)
            {
                EMailAddress1 = "trigger@valid.dk"
            };

            orgAdminService.Update(accountUpd);

            var retrievedAccount = Account.Retrieve(orgAdminService, account.Id, x => x.Name);

            Assert.Equal(newAccountName, retrievedAccount.Name);
        }

        [Fact]
        public void Test2ASync1ASync2PostPluginSucceedsWhenOnlyASync2Applies()
        {
            /* Same as previous but with Async plugins*/
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary,
                typeof(Test2Plugin1),
                typeof(Test2Plugin2));

            oldAccountName = "Test";
            newAccountName = oldAccountName + "ASync2";

            account = new Account()
            {
                Name = oldAccountName,
            };
            account.Id = orgAdminService.Create(account);

            var accountUpd = new Account(account.Id)
            {
                EMailAddress1 = "trigger@valid.dk"
            };

            orgAdminService.Update(accountUpd);

            var retrievedAccount = Account.Retrieve(orgAdminService, account.Id, x => x.Name);

            Assert.Equal(newAccountName, retrievedAccount.Name);
        }

        [Fact]
        public void Test3Sync1AndAsync2PostPluginSucceedsWhenBothApplies()
        {
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary,
                typeof(Test3Plugin1),
                typeof(Test3Plugin2));

            oldAccountName = "Test";
            newAccountName = oldAccountName + "Sync1" + "ASync2";

            account = new Account()
            {
                Name = oldAccountName,
            };
            account.Id = orgAdminService.Create(account);

            var accountUpd = new Account(account.Id)
            {
                EMailAddress1 = "trigger@valid.dk"
            };

            orgAdminService.Update(accountUpd);

            var retrievedAccount = Account.Retrieve(orgAdminService, account.Id, x => x.Name);

            Assert.Equal(newAccountName, retrievedAccount.Name);
        }

        [Fact]
        public void Test7Sync1TriggersAsync2Sync3PostPluginSucceedsWhenSync3AndAsync2Applies()
        {
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary,
                typeof(Test7Plugin1),
                typeof(Test7Plugin2),
                typeof(Test7Plugin3));

            oldAccountName = "Test";
            newAccountName = oldAccountName + "Sync3ASync2";

            account = new Account()
            {
                Name = oldAccountName,
            };
            account.Id = orgAdminService.Create(account);

            var accountUpd = new Account(account.Id)
            {
                EMailAddress1 = "trigger@valid.dk"
            };

            orgAdminService.Update(accountUpd);

            var retrievedAccount = Account.Retrieve(orgAdminService, account.Id, x => x.Name);

            Assert.Equal(newAccountName, retrievedAccount.Name);
        }


        [Fact]
        public void Test8Async2Sync1TriggersSync3SucceedsWhenAllApplies()
        {
            crm.RegisterAdditionalPlugins(Tools.XrmMockup.PluginRegistrationScope.Temporary,
                typeof(Test8Plugin1),
                typeof(Test8Plugin2),
                typeof(Test8Plugin3));

            oldAccountName = "Test";
            newAccountName = oldAccountName + "Sync1" + "Sync3" + "ASync2";

            account = new Account()
            {
                Name = oldAccountName,
            };
            account.Id = orgAdminService.Create(account);

            var accountUpd = new Account(account.Id)
            {
                EMailAddress1 = "trigger@valid.dk"
            };

            orgAdminService.Update(accountUpd);

            var retrievedAccount = Account.Retrieve(orgAdminService, account.Id, x => x.Name);

            Assert.Equal(newAccountName, retrievedAccount.Name);
        }
    }
}