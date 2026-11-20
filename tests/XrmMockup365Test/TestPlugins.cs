using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk;
using Xunit;
using TestPluginAssembly365.Plugins.LegacyDaxif;
using DG.Some.Namespace;

namespace DG.XrmMockupTest
{
    public class TestPlugins : UnitTestBase
    {
        public TestPlugins(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestImpersonatingUserIds()
        {
            var user1 = crm.CreateUser(orgAdminService, crm.RootBusinessUnit, SecurityRoles.XrmMockupTestUser); 
            var user2 = crm.CreateUser(orgAdminService, crm.RootBusinessUnit, SecurityRoles.XrmMockupTestUser);

            var user1Service = crm.CreateOrganizationService(user1.Id);

            //create a note as user 1;
            var note = new Entity("annotation");
            note["notetext"] = "test note";
            note.Id = user1Service.Create(note);

            var user2Service = crm.CreateOrganizationService(user2.Id);
            //edit note as user 2 - should not be allowed

            //create a note as user 1;
            var note2 = new Entity("annotation");
            note2["notetext"] = "test note2";
            note2.Id = user2Service.Create(note2);

           // var editNote2 = new Entity("annotation") { Id = note2.Id };
           // editNote2["notetext"] = note2.Id.ToString();
           // user1Service.Update(editNote2); //this shuold trigger the plugin running as admin to update note 2

            var editNote = new Entity("annotation") { Id = note.Id };
            editNote["notetext"] = note2.Id.ToString();
            user1Service.Update(editNote); //this shuold trigger the plugin running as admin to update note 2

            var checkNote = user2Service.Retrieve("annotation", note2.Id, new ColumnSet("notetext"));
            Assert.Equal("updated by admin plugin", checkNote.GetAttributeValue<string>("notetext"));
        }

        [Fact]
        public void TestImages()
        {
            // Testing that plugins not registered with DAXIF still have access to pre and post images during update.
            var createdAccount = new Account()
            {
                Name = "ChangeMePlease"
            };
            createdAccount.Id = orgAdminUIService.Create(createdAccount);

            orgAdminService.Update(createdAccount);
            var retrievedAccount = Account.Retrieve(orgAdminService, createdAccount.Id, x => x.Name);
            // The update plugin isn't run or the name it updates doesn't match what we are expecting!
            Assert.Equal("NameIsModified", retrievedAccount.Name);

            orgAdminUIService.Delete(Account.EntityLogicalName, createdAccount.Id);
        }

        [Fact]
        public void TestDirectIPluginImplementationPreOp()
        {
            // Testing that plugins which implement IPlugin directly are loaded and called as expected
            var createdContact = new Contact()
            {
                FirstName = "ChangeMePlease"
            };
            createdContact.Id = orgAdminUIService.Create(createdContact);

            var retrievedContact = Contact.Retrieve(orgAdminService, createdContact.Id, x => x.FirstName);
            Assert.Equal("NameIsModified", retrievedContact.FirstName);

            orgAdminUIService.Delete(Contact.EntityLogicalName, createdContact.Id);
        }
        [Fact]
        public void TestDirectIPluginImplementationPostOp()
        {
            // Testing that plugins which implement IPlugin directly are loaded and called as expected
            var createdContact = new Contact()
            {
                FirstName = "ChangeMePleasePostOp"
            };
            createdContact.Id = orgAdminUIService.Create(createdContact);

            var retrievedContact = Contact.Retrieve(orgAdminService, createdContact.Id, x => x.FirstName);
            Assert.Equal("NameIsModifiedPostOp", retrievedContact.FirstName);

            orgAdminUIService.Delete(Contact.EntityLogicalName, createdContact.Id);
        }

        [Fact]
        public void TestSystemAttributesAddedToTargetForPostOperationStepPlugins()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var con = new Contact
                {
                    FirstName = "CheckSystemAttributes"
                };
                con.Id = orgAdminUIService.Create(con);

                con = Contact.Retrieve(orgAdminService, con.Id, x => x.LastName, x => x.CreatedOn);
                Assert.True(!string.IsNullOrEmpty(con.LastName));
                Assert.Equal(con.CreatedOn.ToString(), con.LastName);
            }
        }

        [Fact]
        public void TestSystemFieldsAbsentFromPreStageTargetOnCreate()
        {
            // Matches Dataverse: system-managed fields the caller did not set must not appear in the
            // Target during the PreValidation/PreOperation stages.
            var contact = new Contact { FirstName = ContactSystemFieldsProbePlugin.Marker };
            contact.Id = orgAdminService.Create(contact);

            var probed = Contact.Retrieve(orgAdminService, contact.Id,
                x => x.Address1_Line1, x => x.Address1_Line2);

            // Empty flag strings are normalised to null on create, confirming no system fields were present.
            Assert.True(string.IsNullOrEmpty(probed.Address1_Line1)); // PreValidation Target
            Assert.True(string.IsNullOrEmpty(probed.Address1_Line2)); // PreOperation Target
        }

        [Fact]
        public void TestCallerSetOwnerVisibleInPreStageTargetOnCreate()
        {
            // A system field the caller DID set stays visible in the pre-stage Target.
            var contact = new Contact
            {
                FirstName = ContactSystemFieldsProbePlugin.Marker,
                OwnerId = testUser1.ToEntityReference(),
            };
            contact.Id = orgAdminService.Create(contact);

            var probed = Contact.Retrieve(orgAdminService, contact.Id,
                x => x.Address1_Line1, x => x.Address1_Line2);

            Assert.Contains("ownerid", probed.Address1_Line1);         // PreValidation Target
            Assert.Contains("ownerid", probed.Address1_Line2);         // PreOperation Target
            Assert.DoesNotContain("createdon", probed.Address1_Line1); // still resolved later
        }

        [Fact]
        public void TestSystemFieldsPresentInPostImageAndPostTargetOnCreate()
        {
            var contact = new Contact { FirstName = ContactSystemFieldsProbePlugin.Marker };
            contact.Id = orgAdminService.Create(contact);

            var probed = Contact.Retrieve(orgAdminService, contact.Id,
                x => x.Address1_Line3, x => x.Address1_City);

            foreach (var field in new[]
                { "ownerid", "createdon", "createdby", "modifiedon", "modifiedby", "statecode", "statuscode" })
            {
                Assert.Contains(field, probed.Address1_Line3); // post-image
                Assert.Contains(field, probed.Address1_City);  // post-operation Target
            }
        }

        [Fact]
        public void TestPluginTrigger()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var acc = new Account();
                orgAdminUIService.Create(acc);

                // AccountPostPlugin / LegacyAccountPlugin create ctx_parent records (migrated from Lead).
                var created = context.ctx_parentSet.ToList();
                Assert.True(created.Count > 0);
            }
        }

        [Fact]
        public void TestPluginChain()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var acc = new Account();
                var accid = orgAdminUIService.Create(acc);

                acc.Id = accid;
                acc.Fax = "1233213";

                Assert.Throws<FaultException>(() => orgAdminUIService.Update(acc));
            }
        }

        [Fact]
        public void TestUpdateBase()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var acc = new Account
                {
                    Name = "Some"
                };
                acc.Id = orgAdminUIService.Create(acc);

                var accUpd = new Account(acc.Id)
                {
                    MarketCap = 20m
                };
                orgAdminUIService.Update(accUpd);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.Equal(acc.Name + "UpdateBase", retrieved.Name);
            }
        }

        [Fact]
        public void TestAddedFields()
        {
            var fax = new Entity("fax");
            fax.Id = orgAdminService.Create(fax);
            Assert.False(fax.Contains("category"));

            orgAdminService.Update(fax);
            Assert.False(fax.Contains("isbilled"));
        }

        [Fact]
        public void TestExecutesLegacyDaxifPlugins_Create()
        {
            var account = new Account { Name = "TestAccount" };

            var id = orgAdminUIService.Create(account);

            var createdAccount = Account.Retrieve(orgAdminUIService, id);
            Assert.Equal("TestAccount", createdAccount.Name);

            // Check if the plugin executed (LegacyAccountPlugin creates a ctx_parent, migrated from Lead;
            // ParentAccountId -> ctx_AccountId, Subject -> ctx_Name).
            using (var xrm = new Xrm(orgAdminService))
            {
                var createdParent = xrm.ctx_parentSet.Single(p => p.ctx_AccountId != null && p.ctx_AccountId.Id == id && p.ctx_Name.StartsWith(nameof(LegacyAccountPlugin)));
                Assert.StartsWith(nameof(LegacyAccountPlugin) + " Create: Some new lead ", createdParent.ctx_Name);
            }
        }

        [Fact]
        public void TestExecutesLegacyDaxifPlugins_Update()
        {
            var account = new Account { Name = "TestAccount" };

            var id = orgAdminUIService.Create(account);
            account.Id = id;

            account.Name = "UpdatedAccount";
            orgAdminUIService.Update(account);

            var updatedAccount = Account.Retrieve(orgAdminUIService, id);
            Assert.Equal("UpdatedAccount", updatedAccount.Name);

            // Check if the plugin executed (LegacyAccountPlugin now creates Email records, migrated from
            // Lead): one on Create and one on Update, both regarding the account.
            using (var xrm = new Xrm(orgAdminService))
            {
                var createdParents = xrm.ctx_parentSet
                    .Where(p => p.ctx_AccountId != null
                        && p.ctx_AccountId.Id == id
                        && p.ctx_Name.StartsWith(nameof(LegacyAccountPlugin)))
                    .ToList();

                Assert.Collection(createdParents,
                    p => Assert.StartsWith(nameof(LegacyAccountPlugin) + " Create: Some new lead ", p.ctx_Name),
                    p => Assert.StartsWith(nameof(LegacyAccountPlugin) + " Update: Some new lead ", p.ctx_Name)
                );
            }
        }

        [Fact]
        public void TestDeleteTargetInPostUpdatePlugin()
        {
            crm.DisableRegisteredPlugins(true);
            crm.RegisterAdditionalPlugins(DG.Tools.XrmMockup.PluginRegistrationScope.Temporary, typeof(DG.Some.Namespace.AccountDeleteInPostPlugin));

            var account = new Account { Name = "DeleteMe" };
            account.Id = orgAdminService.Create(account);

            // Update triggers AccountDeleteInPostPlugin which deletes the account — should not throw
            var update = new Account { Id = account.Id, Name = "Updated" };
            orgAdminService.Update(update);

            // Verify the account was actually deleted by the plugin
            var retrieved = orgAdminService.RetrieveMultiple(
                new QueryExpression(Account.EntityLogicalName)
                {
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("accountid", ConditionOperator.Equal, account.Id)
                        }
                    }
                });
            Assert.Empty(retrieved.Entities);
        }

        [Fact]
        public void TestTagIsSharedVariableInAllStages()
        {
            crm.RegisterAdditionalPlugins(PluginRegistrationScope.Temporary, typeof(AccountTagPlugin));
            crm.ClearTraceLog();

            orgAdminService.Execute(new Microsoft.Xrm.Sdk.Messages.CreateRequest
            {
                Target = new Account { Name = "Tagged" },
                ["tag"] = "my tag",
            });

            Assert.Contains("Stage 10: tag=my tag, inInputParameters=False", crm.TraceLog);
            Assert.Contains("Stage 20: tag=my tag, inInputParameters=False", crm.TraceLog);
            Assert.Contains("Stage 40: tag=my tag, inInputParameters=False", crm.TraceLog);
        }
    }
}
