using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestFetchXmlToQueryExpression : UnitTestBase
    {
        string accountName;
        Guid _contact1Id;
        Guid _contact2Id;
        Guid _childAccount1Id;
        Guid _childAccount2Id;

        public TestFetchXmlToQueryExpression(XrmMockupFixture fixture) : base(fixture)
        {
            accountName = "Litware, Inc.";
            // Create an account.
            var acc = new Account
            {
                Name = accountName,
                Address1_StateOrProvince = "Colorado"
            };
            acc.Id = orgAdminUIService.Create(acc);

            // Create the two contacts.
            _contact1Id = orgAdminUIService.Create(
                new Contact()
                {
                    FirstName = "Ben",
                    LastName = "Andrews",
                    EMailAddress1 = "sample@example.com",
                    Address1_City = "Redmond",
                    Address1_StateOrProvince = "WA",
                    Address1_Telephone1 = "(206)555-5555",
                    ParentCustomerId = acc.ToEntityReference()
                });

            _contact2Id = orgAdminUIService.Create(
                new Contact()
                {
                    FirstName = "Colin",
                    LastName = "Wilcox",
                    EMailAddress1 = "sample@example.com",
                    Address1_City = "Bellevue",
                    Address1_StateOrProvince = "WA",
                    Address1_Telephone1 = "(425)555-5555",
                    ParentCustomerId = acc.ToEntityReference()
                });

            // Migrated from Opportunity -> child Account (Opportunity removed). The two "child" accounts
            // reference the main account via ParentAccountId (replacing Opportunity.CustomerId) and carry a
            // settable date in LastOnHoldTime (replacing EstimatedCloseDate). The nested-link query below
            // walks child account -> parent account -> contact, exercising the same FetchXML features.
            _childAccount1Id = orgAdminUIService.Create(
                new Account()
                {
                    Name = "Litware, Inc. Opportunity 1",
                    LastOnHoldTime = DateTime.Now.AddMonths(6),
                    ParentAccountId = acc.ToEntityReference()
                });

            _childAccount2Id = orgAdminUIService.Create(
                new Account()
                {
                    Name = "Litware, Inc. Opportunity 2",
                    LastOnHoldTime = DateTime.Now.AddYears(4),
                    ParentAccountId = acc.ToEntityReference()
                });
        }

        [Fact]
        public void TestFetchXmlToQueryExpressionFromXml()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                // Create a Fetch query that we will convert into a query expression.
                var fetchXml =
                    @"<fetch mapping='logical' version='1.0'>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='lastonholdtime' operator='next-x-years' value='3' />
                            <condition attribute='lastonholdtime' operator='gt' value='2000-01-01 00:00:00' />
                        </filter>
                        <link-entity name='account' from='accountid' to='parentaccountid'>
                            <link-entity name='contact' from='parentcustomerid' to='accountid'>
                                <attribute name='firstname' />
                                <filter>
                                    <condition attribute='address1_city' operator='eq' value='Bellevue' />
                                    <condition attribute='address1_stateorprovince' operator='eq' value='WA' />
                                </filter>
                            </link-entity>
                        </link-entity>
                    </entity>
                </fetch>";

                var conversionResponse = (FetchXmlToQueryExpressionResponse)orgAdminUIService.Execute(
                    new FetchXmlToQueryExpressionRequest { FetchXml = fetchXml });

                // Use the newly converted query expression to make a retrieve multiple
                // request to Microsoft Dynamics CRM.
                QueryExpression queryExpression = conversionResponse.Query;

                EntityCollection result = orgAdminUIService.RetrieveMultiple(queryExpression);

                Assert.Single(result.Entities);
                var entity = result.Entities.First();

                Assert.True(entity.Attributes.ContainsKey("accountid"));
                Assert.Equal(_childAccount1Id, entity.Attributes["accountid"]);

                Assert.True(entity.Attributes.ContainsKey("name"));
                Assert.Equal("Litware, Inc. Opportunity 1", entity.Attributes["name"]);

                Assert.True(entity.Attributes.ContainsKey("contact.firstname"));
                Assert.True(entity.Attributes["contact.firstname"] is AliasedValue);
                Assert.Equal("Colin", entity.GetAttributeValue<AliasedValue>("contact.firstname").Value);
            }
        }

        [Fact]
        public void TestFetchXmlToQueryExpressionFromExpr()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                // Create a Fetch Expression
                var fetchXml =
                    @"<fetch mapping='logical' version='1.0'>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='lastonholdtime' operator='next-x-years' value='3' />
                            <condition attribute='lastonholdtime' operator='gt' value='2000-01-01 00:00:00' />
                        </filter>
                        <link-entity name='account' from='accountid' to='parentaccountid'>
                            <link-entity name='contact' from='parentcustomerid' to='accountid'>
                                <attribute name='firstname' />
                                <filter>
                                    <condition attribute='address1_city' operator='eq' value='Bellevue' />
                                    <condition attribute='address1_stateorprovince' operator='eq' value='WA' />
                                </filter>
                            </link-entity>
                        </link-entity>
                    </entity>
                </fetch>";

                var fetchExpr = new FetchExpression(fetchXml);

                EntityCollection result = orgAdminUIService.RetrieveMultiple(fetchExpr);

                Assert.Single(result.Entities);
                var entity = result.Entities.First();

                Assert.True(entity.Attributes.ContainsKey("accountid"));
                Assert.Equal(_childAccount1Id, entity.Attributes["accountid"]);

                Assert.True(entity.Attributes.ContainsKey("name"));
                Assert.Equal("Litware, Inc. Opportunity 1", entity.Attributes["name"]);

                Assert.True(entity.Attributes.ContainsKey("contact.firstname"));
                Assert.True(entity.Attributes["contact.firstname"] is AliasedValue);
                Assert.Equal("Colin", entity.GetAttributeValue<AliasedValue>("contact.firstname").Value);
            }
        }
    }
}
