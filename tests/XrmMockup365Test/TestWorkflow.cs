using System;
using System.Linq;
using Microsoft.Xrm.Sdk.Query;
using System.IO;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Microsoft.Xrm.Sdk;

namespace DG.XrmMockupTest
{
    public class TestWorkflow : UnitTestBase
    {
        public TestWorkflow(XrmMockupFixture fixture) : base(fixture) { }

        //[Fact]
        public void TestWorkflowWhichUpdatesParent()
        {
            crm.DisableRegisteredPlugins(true);
            crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "IncrementNumberofChildren.xml"));

            var parent = new Entity("mock_parent");
            parent.Id = orgAdminService.Create(parent);

            var child = new Entity("mock_child");
            child["mock_parentid"] = parent.ToEntityReference();
            child.Id = orgAdminService.Create(child);

            var checkParent = orgAdminService.Retrieve("mock_parent", parent.Id, new ColumnSet(true));
            Assert.Equal(1, checkParent.GetAttributeValue<int>("mock_numberofchildren"));
        }

        [Fact]
        public void TestCreateWorkflow()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "OtherWorkflows", "Activeworkflow.xml"));
                var acc = new Account()
                {
                    Name = "Wap"
                };
                acc.Id = orgAdminUIService.Create(acc);

                var retrieved = context.AccountSet.Where(x => x.AccountId == acc.Id).FirstOrDefault();
                Assert.Equal(acc.Name + "setFromCodeActivity", retrieved.Name);
            }
        }

        [Fact]
        public void TestStringAdd()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "OtherWorkflows", "AddStringWorkflow.xml"));
                var acc = new Account()
                {
                    Name = "WapAdd"
                };
                acc.Id = orgAdminUIService.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.Equal(acc.Name + " - " + acc.Name + " ", retrieved.Name);
            }
        }

        [Fact]
        public void TestOtherwiseBranchingWorkflow()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "OtherWorkflows", "OtherwiseWorkflow.xml"));
                var otherwise = new Account()
                {
                    Name = "Otherwise"
                };
                otherwise.Id = orgAdminUIService.Create(otherwise);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, otherwise.Id, new ColumnSet(true)) as Account;
                Assert.Equal("SetFromOtherwise", retrieved.Name);


                var thenThen = new Account()
                {
                    Name = "ThenThen"
                };
                thenThen.Id = orgAdminUIService.Create(thenThen);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, thenThen.Id, new ColumnSet(true)) as Account;
                Assert.Equal("SetFromThenThen", retrieved.Name);

                var thenOtherwise = new Account
                {
                    Name = "ThenOtherwise"
                };
                thenOtherwise.Id = orgAdminUIService.Create(thenOtherwise);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, thenOtherwise.Id, new ColumnSet(true)) as Account;
                Assert.Equal("SetFromThenOtherwise", retrieved.Name);
            }
        }

        [Fact]
        public void TestTimeWorkflow()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "OtherWorkflows", "WaitingWorkflow.xml"));

                // The workflow's create step references this hard-coded currency (the original
                // environment's base currency, "Dansk krone"), and its wait branch only resumes when
                // the account's currency name equals "Dansk krone". Create that currency and put the
                // account on it so the wait condition is satisfiable in the leaned environment.
                var currency = new TransactionCurrency
                {
                    Id = new Guid("b26a2fe0-0154-e611-80dc-c4346bad0190"),
                    CurrencyName = "Dansk krone",
                    CurrencySymbol = "kr.",
                    ExchangeRate = 1m,
                    CurrencyPrecision = 2
                };
                orgAdminService.Create(currency);

                var acc = new Account { TransactionCurrencyId = currency.ToEntityReference() };
                acc.Id = orgAdminUIService.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.Null(retrieved.Name);

                acc.Name = "Some name";
                orgAdminUIService.Update(acc);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.Equal(acc.Name + acc.Name, retrieved.Name);

                crm.AddDays(1);
                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.NotEqual("qwerty", retrieved.Description);

                crm.AddDays(2);
                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.Equal("qwerty", retrieved.Description);
            }
        }


        [Fact]
        public void TestRelatedNullWorkflow()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "OtherWorkflows", "RelatedWorkflow.xml"));
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 1";
                businessunit.Id = orgAdminUIService.Create(businessunit);

                var user = crm.CreateUser(orgAdminUIService,
                    new SystemUser() { LastName = null, BusinessUnitId = businessunit.ToEntityReference() }, SecurityRoles.SystemAdministrator) as SystemUser;
                var service = crm.CreateOrganizationService(user.Id);
                var con = new Contact()
                {
                    LastName = "SomeLastname"
                };
                con.Id = service.Create(con);

                var acc = new Account
                {
                    Name = "Related",
                    PrimaryContactId = con.ToEntityReference()
                };
                acc.Id = service.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.Equal(user.LastName + " - " + con.LastName + " ", retrieved.Name);
            }
        }

        [Fact]
        public void TestRelatedWorkflow()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "OtherWorkflows", "RelatedWorkflow.xml"));
                var businessunit = new BusinessUnit();
                businessunit["name"] = "business unit name 2";
                businessunit.Id = orgAdminUIService.Create(businessunit);

                var user = crm.CreateUser(orgAdminUIService,
                    new SystemUser() { LastName = "UserLastName", BusinessUnitId = businessunit.ToEntityReference() }, SecurityRoles.SystemAdministrator) as SystemUser;
                var service = crm.CreateOrganizationService(user.Id);
                var con = new Contact
                {
                    LastName = "SomeLastname"
                };
                con.Id = service.Create(con);

                var acc = new Account
                {
                    Name = "Related",
                    PrimaryContactId = con.ToEntityReference()
                };
                acc.Id = service.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.Equal(user.LastName + " - " + con.LastName + " ", retrieved.Name);
            }
        }

        [Fact]
        public void TestOwningBusinessUnitInvalidNameWorkflow()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "OtherWorkflows", "OwningBUWorkflow.xml"));
                var businessunit = new BusinessUnit
                {
                    Name = "something"
                };
                businessunit.Id = orgAdminUIService.Create(businessunit);

                var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator) as SystemUser;
                var service = crm.CreateOrganizationService(user.Id);

                var acc = new Account
                {
                    Name = "Some name"
                };
                acc.Id = service.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.Equal(acc.Name, retrieved.Name);
            }
        }

        [Fact]
        public void TestOwningBusinessUnitValidNameWorkflow()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "OtherWorkflows", "OwningBUWorkflow.xml"));
                var businessunit = new BusinessUnit
                {
                    Name = "delegatelab4"
                };
                businessunit.Id = orgAdminUIService.Create(businessunit);

                var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator) as SystemUser;
                var service = crm.CreateOrganizationService(user.Id);

                var acc = new Account
                {
                    Name = "Some name"
                };
                acc.Id = service.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.Equal("SetOwningBU", retrieved.Name);
            }
        }

        [Fact]
        public void TestMyConditionWorkflow()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "OtherWorkflows", "MyConditionWorkflow.xml"));
                var businessunit = new BusinessUnit
                {
                    Name = "deledevelopmentasd"
                };
                businessunit.Id = orgAdminUIService.Create(businessunit);

                var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator) as SystemUser;
                var service = crm.CreateOrganizationService(user.Id);

                var acc = new Account
                {
                    Name = "Some name",
                    Revenue = 3000,
                    DoNotFax = true,
                    DoNotBulkPostalMail = false
                };
                acc.Id = service.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.Equal("MyCondition", retrieved.Name);
            }
        }

        [Fact]
        public void TestSendMailWorkflow()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                crm.AddWorkflow(Path.Combine("../../..", "Metadata", "OtherWorkflows", "SendMailWorkflow.xml"));

                // The workflow sets the created email's transactioncurrencyid to a currency the
                // original environment shipped by default. The leaned environment's default currency
                // has a different id, so create the hard-coded one the workflow references.
                orgAdminService.Create(new TransactionCurrency
                {
                    Id = new Guid("b26a2fe0-0154-e611-80dc-c4346bad0190"),
                    CurrencyName = "Workflow Currency",
                    CurrencySymbol = "$",
                    ExchangeRate = 1m,
                    CurrencyPrecision = 2
                });

                var businessunit = new BusinessUnit
                {
                    Name = "delegatelab4"
                };
                businessunit.Id = orgAdminUIService.Create(businessunit);

                crm.CreateUser(orgAdminService,
                    new SystemUser()
                    {
                        Id = new Guid("9732d6d5-8e46-44e1-b408-32d3801c5724"),
                        FirstName = "Kaspar",
                        LastName = "Bøgh Christensen",
                        BusinessUnitId = crm.RootBusinessUnit
                    }, SecurityRoles.SystemAdministrator);

                var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator) as SystemUser;
                var service = crm.CreateOrganizationService(user.Id);

                var acc = new Account
                {
                    Name = "Some name"
                };
                acc.Id = service.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;

                var email = orgAdminService.RetrieveMultiple(new QueryExpression
                {
                    EntityName = Email.EntityLogicalName,
                    ColumnSet = new ColumnSet(true)
                }).Entities.FirstOrDefault() as Email;

                Assert.NotNull(email);
                Assert.Equal("&lt;span&gt;&lt;span&gt;Some kind of email&lt;/span&gt;&lt;/span&gt;", email.Description);
                Assert.Equal("Something", email.Subject);
                Assert.Equal(retrieved.ToEntityReference(), email.RegardingObjectId);
            }
        }

        /* DISABLED: these (already non-[Fact]) workflow type-conversion tests operate on the Lead entity
           and Lead-targeted workflow definitions, both removed from the environment. Migrating them would
           require re-authoring the workflow XML against an available entity. Retained for reference.
        //[Fact]
        public void TestClear()
        {
            crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestClear.xml"));
            var service = crm.GetAdminService();
            var lead = new Lead
            {
                Subject = "test"
            };

            lead.Id = service.Create(lead);

            var retrieved = orgAdminUIService.Retrieve(Lead.EntityLogicalName, lead.Id, new ColumnSet(true)) as Lead;
            Assert.Null(retrieved.Subject);
        }

        //[Fact]
        public void TestTypeConvertingOptionsetToString()
        {
            crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestTypeConvertingOptionsetToString.xml"));
            var service = crm.GetAdminService();
            var lead = new Lead
            {
                LeadSourceCode = Lead_LeadSourceCode.TradeShow
            };

            lead.Id = service.Create(lead);

            var retrieved = orgAdminUIService.Retrieve(Lead.EntityLogicalName, lead.Id, new ColumnSet(true)) as Lead;
            Assert.Equal("Trade Show", retrieved.Description);
        }

        //[Fact]
        public void TestTypeConvertingEntityToString()
        {
            crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestTypeConvertingEntityToString.xml"));
            var service = crm.GetAdminService();
            var acc = new Account
            {
                Name = "Contoso Corp."
            };
            acc.Id = service.Create(acc);
            var lead = new Lead
            {
                CustomerId = acc.ToEntityReference()
            };

            lead.Id = service.Create(lead);

            var retrieved = orgAdminUIService.Retrieve(Lead.EntityLogicalName, lead.Id, new ColumnSet(true)) as Lead;
            Assert.Equal(acc.Name, retrieved.Description);
        }

        //[Fact]
        public void TestTypeConvertingMoneyToString()
        {
            crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestTypeConvertingMoneyToString.xml"));
            var service = crm.GetAdminService();
            var lead = new Lead
            {
                Revenue = 997.98m
            };

            lead.Id = service.Create(lead);

            var retrieved = orgAdminUIService.Retrieve(Lead.EntityLogicalName, lead.Id, new ColumnSet(true)) as Lead;
            // TODO.. we should somewhere declare formating and currency should be from record
            //Assert.Equal("997,98 zł", retrieved.Description);
            Assert.Equal($"{lead.Revenue:C}", retrieved.Description);
        }

        //[Fact]
        public void TestTypeConvertingIntToString()
        {
            crm.AddWorkflow(Path.Combine("../../..", "Metadata", "Workflows", "TestTypeConvertingIntToString.xml"));
            var service = crm.GetAdminService();
            var lead = new Lead
            {
                NumberOfEmployees = 11111
            };

            lead.Id = service.Create(lead);

            var retrieved = orgAdminUIService.Retrieve(Lead.EntityLogicalName, lead.Id, new ColumnSet(true)) as Lead;
            // TODO.. we should somewhere declare formating
            Assert.Equal($"{lead.NumberOfEmployees:N0}", retrieved.Description);
        }
        */
    }
}
