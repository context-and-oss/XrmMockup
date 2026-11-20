using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestOrderByQueryExpression : UnitTestBase
    {
        public TestOrderByQueryExpression(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void When_ordering_by_money_fields_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var contact1 = new Contact()
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Fred",
                    LastName = "Bloggs",
                    AnnualIncome = 12345m,
                    TransactionCurrencyId = crm.BaseCurrency
                };
                var contact2 = new Contact()
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Jo",
                    LastName = "Bloggs",
                    AnnualIncome = 678910m,
                    TransactionCurrencyId = crm.BaseCurrency
                };
                crm.PopulateWith(contact1, contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true)
                };
                qry.AddOrder("annualincome", OrderType.Ascending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).AnnualIncome;

                Assert.Equal(12345m, firstResultValue.Value);
            }
        }

        [Fact]
        public void When_ordering_by_money_fields_descending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var contact1 = new Contact
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Fred",
                    LastName = "Bloggs",
                    AnnualIncome = 12345m,
                    TransactionCurrencyId = crm.BaseCurrency
                };

                var contact2 = new Contact
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Jo",
                    LastName = "Bloggs",
                    AnnualIncome = 678910m,
                    TransactionCurrencyId = crm.BaseCurrency
                };

                crm.PopulateWith(contact1, contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true)
                };
                qry.AddOrder("annualincome", OrderType.Descending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).AnnualIncome;

                Assert.Equal(678910M, firstResultValue.Value);
            }
        }

        [Fact]
        public void When_ordering_by_entity_reference_fields_ascending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                // Migrated from Lead -> Account and Contact.OriginatingLeadId -> Contact.ParentCustomerId
                // (Lead removed). This still exercises ordering by an EntityReference (lookup) field and
                // verifies the referenced record's Name is populated on the result.
                var account1 = new Account
                {
                    Name = "Jordi"
                };
                account1.Id = orgAdminService.Create(account1);

                var account2 = new Account
                {
                    Name = "Skuba"
                };
                account2.Id = orgAdminService.Create(account2);

                var contact1 = new Contact
                {
                    FirstName = "Fred",
                    LastName = "Bloggs",
                    ParentCustomerId = account1.ToEntityReference()
                };
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact
                {
                    FirstName = "Jo",
                    LastName = "Bloggs",
                    ParentCustomerId = account2.ToEntityReference()
                };
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true)
                };
                qry.AddOrder("parentcustomerid", OrderType.Ascending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).ParentCustomerId;

                Assert.Equal("Jordi", firstResultValue.Name);
            }
        }

        [Fact]
        public void When_ordering_by_entity_reference_fields_descending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                // Migrated from Lead -> Account and Contact.OriginatingLeadId -> Contact.ParentCustomerId
                // (Lead removed). Ordering by an EntityReference (lookup) field, descending.
                var account1 = new Account
                {
                    Name = "Jordi"
                };
                account1.Id = orgAdminService.Create(account1);

                var account2 = new Account
                {
                    Name = "Skuba"
                };
                account2.Id = orgAdminService.Create(account2);

                var contact1 = new Contact
                {
                    FirstName = "Fred",
                    LastName = "Bloggs",
                    ParentCustomerId = account1.ToEntityReference()
                };
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact
                {
                    FirstName = "Jo",
                    LastName = "Bloggs",
                    ParentCustomerId = account2.ToEntityReference()
                };
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true)
                };
                qry.AddOrder("parentcustomerid", OrderType.Descending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).ParentCustomerId;

                Assert.Equal("Skuba", firstResultValue.Name);
            }
        }

        [Fact]
        public void When_ordering_by_optionsetvalue_fields_ascending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var contact1 = new Contact
                {
                    FirstName = "Fred",
                    GenderCode = contact_gendercode.Male
                };
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact
                {
                    FirstName = "Jo",
                    GenderCode = contact_gendercode.Female
                };
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true)
                };
                qry.AddOrder("gendercode", OrderType.Ascending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).GenderCode;

                Assert.Equal(contact_gendercode.Male, firstResultValue.Value);
            }
        }

        [Fact]
        public void When_ordering_by_optionsetvalue_fields_descending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var contact1 = new Contact
                {
                    FirstName = "Fred",
                    GenderCode = contact_gendercode.Male
                };
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact
                {
                    FirstName = "Jo",
                    GenderCode = contact_gendercode.Female
                };
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true)
                };
                qry.AddOrder("gendercode", OrderType.Descending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).GenderCode;

                Assert.Equal(contact_gendercode.Female, firstResultValue.Value);
            }
        }

        [Fact]
        public void When_ordering_by_int_fields_ascending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var contact1 = new Contact
                {
                    FirstName = "Fred",
                    NumberOfChildren = 2
                };
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact
                {
                    FirstName = "Jo",
                    NumberOfChildren = 5
                };
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true)
                };
                qry.AddOrder("numberofchildren", OrderType.Ascending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).NumberOfChildren;

                Assert.Equal(2, firstResultValue.Value);
            }
        }

        [Fact]
        public void When_ordering_by_int_fields_descending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var contact1 = new Contact
                {
                    FirstName = "Fred",
                    NumberOfChildren = 2
                };
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact
                {
                    FirstName = "Jo",
                    NumberOfChildren = 5
                };
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true)
                };
                qry.AddOrder("numberofchildren", OrderType.Descending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).NumberOfChildren;

                Assert.Equal(5, firstResultValue.Value);
            }
        }

        [Fact]
        public void When_ordering_by_datetime_ascending_fields_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var now = DateTime.Now;
                var contact1 = new Contact
                {
                    FirstName = "Fred",
                    LastOnHoldTime = now
                };
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact
                {
                    FirstName = "Jo",
                    LastOnHoldTime = now.AddDays(1)
                };
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true)
                };
                qry.AddOrder("lastonholdtime", OrderType.Ascending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).LastOnHoldTime;

                Assert.Equal(now, firstResultValue.Value);
            }
        }

        [Fact]
        public void When_ordering_by_datetime_descending_fields_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var now = DateTime.Now;
                var contact1 = new Contact
                {
                    FirstName = "Fred",
                    LastOnHoldTime = now
                };
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact
                {
                    FirstName = "Jo",
                    LastOnHoldTime = now.AddDays(1)
                };
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true)
                };
                qry.AddOrder("lastonholdtime", OrderType.Descending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).LastOnHoldTime;

                Assert.Equal(now.AddDays(1), firstResultValue.Value);
            }
        }

        [Fact]
        public void When_ordering_by_guid_fields_ascending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var g1 = new Guid(1, 2, 3, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
                var g2 = new Guid(2, 2, 3, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
                var contact1 = new Contact
                {
                    FirstName = "Fred",
                    Address1_AddressId = g1
                };
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact
                {
                    FirstName = "Jo",
                    Address1_AddressId = g2
                };
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true)
                };
                qry.AddOrder("address1_addressid", OrderType.Ascending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).Address1_AddressId;

                Assert.Equal(g1, firstResultValue.Value);
            }
        }

        [Fact]
        public void When_ordering_by_guid_fields_descending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var g1 = new Guid(1, 2, 3, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
                var g2 = new Guid(2, 2, 3, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
                var contact1 = new Contact
                {
                    FirstName = "Fred",
                    Address1_AddressId = g1
                };
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact
                {
                    FirstName = "Jo",
                    Address1_AddressId = g2
                };
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true)
                };
                qry.AddOrder("address1_addressid", OrderType.Descending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).Address1_AddressId;

                Assert.Equal(g2, firstResultValue.Value);
            }
        }

        [Fact]
        public void When_ordering_by_decimal_fields_ascending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var contact1 = new Contact
                {
                    FirstName = "Fred"
                };
                contact1["exchangerate"] = 20m;
                contact1.Id = orgGodService.Create(contact1);

                var contact2 = new Contact
                {
                    FirstName = "Jo"
                };
                contact2["exchangerate"] = 50m;
                contact2.Id = orgGodService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true)
                };
                qry.AddOrder("exchangerate", OrderType.Ascending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact)["exchangerate"];

                Assert.Equal(20m, firstResultValue);
            }
        }

        [Fact]
        public void When_ordering_by_decimal_fields_descending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var contact1 = new Contact
                {
                    FirstName = "Fred"
                };
                contact1["exchangerate"] = 20m;
                contact1.Id = orgGodService.Create(contact1);

                var contact2 = new Contact
                {
                    FirstName = "Jo"
                };
                contact2["exchangerate"] = 50m;
                contact2.Id = orgGodService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true)
                };
                qry.AddOrder("exchangerate", OrderType.Descending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact)["exchangerate"];

                Assert.Equal(50m, firstResultValue);
            }
        }

        [Fact]
        public void When_ordering_by_double_fields_ascending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var contact1 = new Contact
                {
                    FirstName = "Fred",
                    Address1_Latitude = 2
                };
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact
                {
                    FirstName = "Jo",
                    Address1_Latitude = 5
                };
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true)
                };
                qry.AddOrder("address1_latitude", OrderType.Ascending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).Address1_Latitude;

                Assert.Equal(2, firstResultValue.Value);
            }
        }

        [Fact]
        public void When_ordering_by_double_fields_descending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var contact1 = new Contact
                {
                    FirstName = "Fred",
                    Address1_Latitude = 2
                };
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact
                {
                    FirstName = "Jo",
                    Address1_Latitude = 5
                };
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true)
                };
                qry.AddOrder("address1_latitude", OrderType.Descending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).Address1_Latitude;

                Assert.Equal(5, firstResultValue.Value);
            }
        }

        [Fact]
        public void When_ordering_by_boolean_fields_ascending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var contact1 = new Contact
                {
                    FirstName = "Fred",
                    DoNotEMail = false
                };
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact
                {
                    FirstName = "Jo",
                    DoNotEMail = true
                };
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true)
                };
                qry.AddOrder("donotemail", OrderType.Ascending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).DoNotEMail;

                Assert.False(firstResultValue.Value);
            }
        }

        [Fact]
        public void When_ordering_by_boolean_fields_descending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var contact1 = new Contact
                {
                    FirstName = "Fred",
                    DoNotEMail = false
                };
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact
                {
                    FirstName = "Jo",
                    DoNotEMail = true
                };
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true)
                };
                qry.AddOrder("donotemail", OrderType.Descending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).DoNotEMail;

                Assert.True(firstResultValue.Value);
            }
        }

        [Fact]
        public void When_ordering_by_2_columns_simultaneously_right_result_is_returned_asc_desc()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var account11 = new Account() { Name = "11", ImportSequenceNumber = 1, NumberOfEmployees = 1 };
                var account12 = new Account() { Name = "12", ImportSequenceNumber = 1, NumberOfEmployees = 2 };
                var account21 = new Account() { Name = "21", ImportSequenceNumber = 2, NumberOfEmployees = 1 };
                var account22 = new Account() { Name = "22", ImportSequenceNumber = 2, NumberOfEmployees = 2 };
                var account31 = new Account() { Name = "31", ImportSequenceNumber = 3, NumberOfEmployees = 1 };
                var account32 = new Account() { Name = "32", ImportSequenceNumber = 3, NumberOfEmployees = 2 };

                crm.PopulateWith(account11, account12, account21, account22, account31, account32);

                QueryExpression query = new QueryExpression()
                {
                    EntityName = "account",
                    ColumnSet = new ColumnSet(true),
                    Orders =
                    {
                    new OrderExpression("importsequencenumber", OrderType.Ascending),
                    new OrderExpression("numberofemployees", OrderType.Descending)
                }
                };

                EntityCollection ec = orgAdminService.RetrieveMultiple(query);
                var names = ec.Entities.Select(e => e.ToEntity<Account>().Name).ToList();

                Assert.True(names[0].Equals("12"), "Test 12 failed");
                Assert.True(names[1].Equals("11"), "Test 11 failed");
                Assert.True(names[2].Equals("22"), "Test 22 failed");
                Assert.True(names[3].Equals("21"), "Test 21 failed");
                Assert.True(names[4].Equals("32"), "Test 32 failed");
                Assert.True(names[5].Equals("31"), "Test 31 failed");
            }
        }

        [Fact]
        public void When_ordering_by_2_columns_simultaneously_right_result_is_returned_asc_asc()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var account11 = new Account() { Name = "11", ImportSequenceNumber = 1, NumberOfEmployees = 1 };
                var account12 = new Account() { Name = "12", ImportSequenceNumber = 1, NumberOfEmployees = 2 };
                var account21 = new Account() { Name = "21", ImportSequenceNumber = 2, NumberOfEmployees = 1 };
                var account22 = new Account() { Name = "22", ImportSequenceNumber = 2, NumberOfEmployees = 2 };
                var account31 = new Account() { Name = "31", ImportSequenceNumber = 3, NumberOfEmployees = 1 };
                var account32 = new Account() { Name = "32", ImportSequenceNumber = 3, NumberOfEmployees = 2 };

                crm.PopulateWith(account11, account12, account21, account22, account31, account32);

                QueryExpression query = new QueryExpression()
                {
                    EntityName = "account",
                    ColumnSet = new ColumnSet(true),
                    Orders =
                    {
                    new OrderExpression("importsequencenumber", OrderType.Ascending),
                    new OrderExpression("numberofemployees", OrderType.Ascending)
                }
                };

                EntityCollection ec = orgAdminService.RetrieveMultiple(query);
                var names = ec.Entities.Select(e => e.ToEntity<Account>().Name).ToList();
                Assert.True(names[0].Equals("11"), "Test 11 failed");
                Assert.True(names[1].Equals("12"), "Test 12 failed");
                Assert.True(names[2].Equals("21"), "Test 21 failed");
                Assert.True(names[3].Equals("22"), "Test 22 failed");
                Assert.True(names[4].Equals("31"), "Test 31 failed");
                Assert.True(names[5].Equals("32"), "Test 32 failed");
            }
        }

        [Fact]
        public void When_ordering_by_2_columns_simultaneously_right_result_is_returned_desc_desc()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var account11 = new Account() { Name = "11", ImportSequenceNumber = 1, NumberOfEmployees = 1 };
                var account12 = new Account() { Name = "12", ImportSequenceNumber = 1, NumberOfEmployees = 2 };
                var account21 = new Account() { Name = "21", ImportSequenceNumber = 2, NumberOfEmployees = 1 };
                var account22 = new Account() { Name = "22", ImportSequenceNumber = 2, NumberOfEmployees = 2 };
                var account31 = new Account() { Name = "31", ImportSequenceNumber = 3, NumberOfEmployees = 1 };
                var account32 = new Account() { Name = "32", ImportSequenceNumber = 3, NumberOfEmployees = 2 };

                crm.PopulateWith(account11, account12, account21, account22, account31, account32);

                QueryExpression query = new QueryExpression()
                {
                    EntityName = "account",
                    ColumnSet = new ColumnSet(true),
                    Orders =
                    {
                    new OrderExpression("importsequencenumber", OrderType.Descending),
                    new OrderExpression("numberofemployees", OrderType.Descending)
                }
                };

                EntityCollection ec = orgAdminService.RetrieveMultiple(query);
                var names = ec.Entities.Select(e => e.ToEntity<Account>().Name).ToList();

                Assert.True(names[0].Equals("32"), "Test 32 failed");
                Assert.True(names[1].Equals("31"), "Test 31 failed");
                Assert.True(names[2].Equals("22"), "Test 22 failed");
                Assert.True(names[3].Equals("21"), "Test 21 failed");
                Assert.True(names[4].Equals("12"), "Test 12 failed");
                Assert.True(names[5].Equals("11"), "Test 11 failed");
            }
        }

        [Fact]
        public void When_ordering_by_2_columns_simultaneously_right_result_is_returned_desc_asc()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var account11 = new Account() { Name = "11", ImportSequenceNumber = 1, NumberOfEmployees = 1 };
                var account12 = new Account() { Name = "12", ImportSequenceNumber = 1, NumberOfEmployees = 2 };
                var account21 = new Account() { Name = "21", ImportSequenceNumber = 2, NumberOfEmployees = 1 };
                var account22 = new Account() { Name = "22", ImportSequenceNumber = 2, NumberOfEmployees = 2 };
                var account31 = new Account() { Name = "31", ImportSequenceNumber = 3, NumberOfEmployees = 1 };
                var account32 = new Account() { Name = "32", ImportSequenceNumber = 3, NumberOfEmployees = 2 };

                crm.PopulateWith(account11, account12, account21, account22, account31, account32);

                QueryExpression query = new QueryExpression()
                {
                    EntityName = "account",
                    ColumnSet = new ColumnSet(true),
                    Orders =
                    {
                    new OrderExpression("importsequencenumber", OrderType.Descending),
                    new OrderExpression("numberofemployees", OrderType.Ascending)
                }
                };

                EntityCollection ec = orgAdminService.RetrieveMultiple(query);
                var names = ec.Entities.Select(e => e.ToEntity<Account>().Name).ToList();

                Assert.True(names[0].Equals("31"), "Test 31 failed");
                Assert.True(names[1].Equals("32"), "Test 32 failed");
                Assert.True(names[2].Equals("21"), "Test 21 failed");
                Assert.True(names[3].Equals("22"), "Test 22 failed");
                Assert.True(names[4].Equals("11"), "Test 11 failed");
                Assert.True(names[5].Equals("12"), "Test 12 failed");
            }
        }

        [Fact]
        public void When_ordering_by_3_columns_simultaneously_right_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var account111 = new Account() { Name = "111", ImportSequenceNumber = 1, NumberOfEmployees = 1, AccountNumber = "1" };
                var account112 = new Account() { Name = "112", ImportSequenceNumber = 1, NumberOfEmployees = 1, AccountNumber = "2" };
                var account121 = new Account() { Name = "121", ImportSequenceNumber = 1, NumberOfEmployees = 2, AccountNumber = "1" };
                var account122 = new Account() { Name = "122", ImportSequenceNumber = 1, NumberOfEmployees = 2, AccountNumber = "2" };
                var account211 = new Account() { Name = "211", ImportSequenceNumber = 2, NumberOfEmployees = 1, AccountNumber = "1" };
                var account212 = new Account() { Name = "212", ImportSequenceNumber = 2, NumberOfEmployees = 1, AccountNumber = "2" };
                var account221 = new Account() { Name = "221", ImportSequenceNumber = 2, NumberOfEmployees = 2, AccountNumber = "1" };
                var account222 = new Account() { Name = "222", ImportSequenceNumber = 2, NumberOfEmployees = 2, AccountNumber = "2" };

                crm.PopulateWith(account211, account221, account112, account122, account222, account212, account121, account111);

                QueryExpression query = new QueryExpression()
                {
                    EntityName = "account",
                    ColumnSet = new ColumnSet(true),
                    Orders =
            {
                new OrderExpression("importsequencenumber", OrderType.Ascending),
                new OrderExpression("numberofemployees", OrderType.Ascending),
                new OrderExpression("accountnumber", OrderType.Ascending)
            }
                };

                EntityCollection ec = orgAdminService.RetrieveMultiple(query);
                var names = ec.Entities.Select(e => e.ToEntity<Account>().Name).ToList();

                Assert.True(names[0].Equals("111"), "test 111 failed");
                Assert.True(names[1].Equals("112"), "test 112 failed");
                Assert.True(names[2].Equals("121"), "test 121 failed");
                Assert.True(names[3].Equals("122"), "test 122 failed");
                Assert.True(names[4].Equals("211"), "test 211 failed");
                Assert.True(names[5].Equals("212"), "test 221 failed");
                Assert.True(names[6].Equals("221"), "test 212 failed");
                Assert.True(names[7].Equals("222"), "test 222 failed");
            }
        }

        [Fact]
        public void When_ordering_by_2_columns_nulled_column_is_handled()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var account11 = new Account() { Name = "11", ImportSequenceNumber = 1, NumberOfEmployees = 1 };
                var account12 = new Account() { Name = "12", ImportSequenceNumber = 1, NumberOfEmployees = 2 };
                var account21 = new Account() { Name = "21", ImportSequenceNumber = 2, NumberOfEmployees = 1 };
                var account2null = new Account() { Name = "2null", ImportSequenceNumber = 2 }; // Explicitly not setting numberofemployees

                crm.PopulateWith(account11, account12, account21, account2null);

                QueryExpression query = new QueryExpression()
                {
                    EntityName = "account",
                    ColumnSet = new ColumnSet(true),
                    Orders =
                    {
                        new OrderExpression ("importsequencenumber", OrderType.Ascending),
                        new OrderExpression ("numberofemployees", OrderType.Ascending)
                    }
                };

                EntityCollection ec = orgAdminService.RetrieveMultiple(query);
                var names = ec.Entities.Select(e => e.ToEntity<Account>().Name).ToList();

                Assert.True(names[0].Equals("11"), "test 11 failed");
                Assert.True(names[1].Equals("12"), "test 12 failed");
                Assert.True(names[2].Equals("2null"), "test 2null failed"); 
                Assert.True(names[3].Equals("21"), "test 21 failed");
            }
        }
    }
}
