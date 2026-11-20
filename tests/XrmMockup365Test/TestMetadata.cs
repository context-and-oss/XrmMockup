using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk.Metadata;
using Xunit;
using Xunit.Sdk;
using Microsoft.Xrm.Sdk.Metadata.Query;

namespace DG.XrmMockupTest
{
    public class TestMetadata : UnitTestBase
    {
        public TestMetadata(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestRetrieveOptionSet()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var optionRetrieved = orgAdminUIService.Execute(new RetrieveOptionSetRequest() { Name = "workflow_stage" }) as RetrieveOptionSetResponse;
                Assert.True(optionRetrieved.OptionSetMetadata.Name == "workflow_stage");

            }
        }

        [Fact]
        public void TestRetrieveAllOptionSets()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var optionsRetrieved = orgAdminUIService.Execute(new RetrieveAllOptionSetsRequest()) as RetrieveAllOptionSetsResponse;
                Assert.Contains(optionsRetrieved.OptionSetMetadata, x => x.Name == "workflow_stage");

            }
        }

        [Fact]
        public void TestSetttingAttributes()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var acc = new Account();
                acc.Id = orgAdminUIService.Create(acc);

                acc.Attributes["name"] = "Jon";
                orgAdminUIService.Update(acc);

                try
                {
                    acc.Attributes["illegalName"] = 1;
                    orgAdminUIService.Update(acc);
                    throw new XunitException("FaultException should have been thrown");
                }
                catch (FaultException)
                {
                }
                catch (Exception e)
                {
                    throw new XunitException(
                         string.Format("Unexpected exception of type {0} caught: {1}",
                                        e.GetType(), e.Message)
                    );
                }
            }
        }

        [Fact]
        public void TestCRURestrictions()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                // Migrated from account.opendeals_state (a read-only system rollup absent from the lean
                // metadata) to ctx_parent.ctx_totalallowance (also a rollup, hence read-only): a value
                // written on create/update is stripped, so it never round-trips. The non-readable-field
                // check stays on account.isprivate (still present in metadata).
                var parent = new ctx_parent();
                parent.Attributes.Add("ctx_totalallowance", new Money(22));
                parent.Id = orgAdminUIService.Create(parent);

                var retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, parent.Id, new ColumnSet("ctx_totalallowance")) as ctx_parent;
                Assert.NotEqual(22m, retrieved.ctx_TotalAllowance);

                orgAdminUIService.Update(parent);
                retrieved = orgAdminUIService.Retrieve(ctx_parent.EntityLogicalName, parent.Id, new ColumnSet("ctx_totalallowance")) as ctx_parent;
                Assert.NotEqual(22m, retrieved.ctx_TotalAllowance);

                var acc = new Account();
                acc.Id = orgAdminUIService.Create(acc);
                var accRetrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet("isprivate")) as Account;
                Assert.False(accRetrieved.Attributes.ContainsKey("isprivate"));
            }
        }


        [Fact]
        public void RetrieveEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.NotNull(resp);
            Assert.NotNull(resp.EntityMetadata);
            Assert.Equal(req.LogicalName, resp.EntityMetadata.LogicalName);
        }

        [Fact]
        public void RetrieveAllFilteredEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName,
                EntityFilters = EntityFilters.All
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.NotNull(resp.EntityMetadata.Privileges);
            Assert.NotNull(resp.EntityMetadata.OneToManyRelationships);
            Assert.NotNull(resp.EntityMetadata.ManyToManyRelationships);
            Assert.NotNull(resp.EntityMetadata.ManyToOneRelationships);
            Assert.NotNull(resp.EntityMetadata.Attributes);
        }


        [Fact]
        public void RetrieveAttributesFilteredEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName,
                EntityFilters = EntityFilters.Attributes
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.Null(resp.EntityMetadata.Privileges);
            Assert.Null(resp.EntityMetadata.OneToManyRelationships);
            Assert.Null(resp.EntityMetadata.ManyToManyRelationships);
            Assert.Null(resp.EntityMetadata.ManyToOneRelationships);
            Assert.NotNull(resp.EntityMetadata.Attributes);
        }


        [Fact]
        public void RetrievePrivilegesFilteredEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName,
                EntityFilters = EntityFilters.Privileges
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.NotNull(resp.EntityMetadata.Privileges);
            Assert.Null(resp.EntityMetadata.OneToManyRelationships);
            Assert.Null(resp.EntityMetadata.ManyToManyRelationships);
            Assert.Null(resp.EntityMetadata.ManyToOneRelationships);
            Assert.Null(resp.EntityMetadata.Attributes);
        }


        [Fact]
        public void RetrieveRelationshipsFilteredEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName,
                EntityFilters = EntityFilters.Relationships
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.Null(resp.EntityMetadata.Privileges);
            Assert.NotNull(resp.EntityMetadata.OneToManyRelationships);
            Assert.NotNull(resp.EntityMetadata.ManyToManyRelationships);
            Assert.NotNull(resp.EntityMetadata.ManyToOneRelationships);
            Assert.Null(resp.EntityMetadata.Attributes);
        }

        [Fact]
        public void RetrieveEntityFilteredEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName,
                EntityFilters = EntityFilters.Entity
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.Null(resp.EntityMetadata.Privileges);
            Assert.Null(resp.EntityMetadata.OneToManyRelationships);
            Assert.Null(resp.EntityMetadata.ManyToManyRelationships);
            Assert.Null(resp.EntityMetadata.ManyToOneRelationships);
            Assert.Null(resp.EntityMetadata.Attributes);
        }

        [Fact]
        public void RetrieveDefaultFilteredEntityMetadata()
        {
            var req = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName,
                EntityFilters = EntityFilters.Default
            };
            var resp = (RetrieveEntityResponse)orgAdminService.Execute(req);

            Assert.Null(resp.EntityMetadata.Privileges);
            Assert.Null(resp.EntityMetadata.OneToManyRelationships);
            Assert.Null(resp.EntityMetadata.ManyToManyRelationships);
            Assert.Null(resp.EntityMetadata.ManyToOneRelationships);
            Assert.Null(resp.EntityMetadata.Attributes);
        }

        [Fact]
        public void RetrieveMetadataChangesRequest_ObjectTypeCode_Equals()
        {
            var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
            entityFilter.Conditions.Add(new MetadataConditionExpression("ObjectTypeCode", MetadataConditionOperator.Equals, 1));
            var propertyExpression = new MetadataPropertiesExpression { AllProperties = false };
            propertyExpression.PropertyNames.Add("LogicalName");
            var entityQueryExpression = new EntityQueryExpression()
            {
                Criteria = entityFilter,
                Properties = propertyExpression
            };

            var retrieveMetadataChangesRequest = new RetrieveMetadataChangesRequest()
            {
                Query = entityQueryExpression
            };

            var response = (RetrieveMetadataChangesResponse)orgAdminService.Execute(retrieveMetadataChangesRequest);

            Assert.NotEmpty(response.EntityMetadata);
            Assert.Contains(response.EntityMetadata, e => e.LogicalName == "account");
        }

        [Fact]
        public void RetrieveMetadataChangesRequest_LogicalName_Equals()
        {
            var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
            entityFilter.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, "contact"));
            var entityQueryExpression = new EntityQueryExpression()
            {
                Criteria = entityFilter
            };

            var retrieveMetadataChangesRequest = new RetrieveMetadataChangesRequest()
            {
                Query = entityQueryExpression
            };

            var response = (RetrieveMetadataChangesResponse)orgAdminService.Execute(retrieveMetadataChangesRequest);

            Assert.Single(response.EntityMetadata);
            Assert.Equal("contact", response.EntityMetadata[0].LogicalName);
        }

        [Fact]
        public void RetrieveMetadataChangesRequest_LogicalName_In()
        {
            var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
            entityFilter.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.In, new[] { "account", "contact" }));
            var entityQueryExpression = new EntityQueryExpression()
            {
                Criteria = entityFilter
            };

            var retrieveMetadataChangesRequest = new RetrieveMetadataChangesRequest()
            {
                Query = entityQueryExpression
            };

            var response = (RetrieveMetadataChangesResponse)orgAdminService.Execute(retrieveMetadataChangesRequest);

            Assert.Equal(2, response.EntityMetadata.Count);
            Assert.Contains(response.EntityMetadata, e => e.LogicalName == "account");
            Assert.Contains(response.EntityMetadata, e => e.LogicalName == "contact");
        }

        [Fact]
        public void RetrieveMetadataChangesRequest_MetadataId_Equals()
        {
            // First get the account entity metadata to obtain its MetadataId
            var accountRequest = new RetrieveEntityRequest()
            {
                LogicalName = Account.EntityLogicalName
            };
            var accountResponse = (RetrieveEntityResponse)orgAdminService.Execute(accountRequest);
            var accountMetadataId = accountResponse.EntityMetadata.MetadataId.Value;

            // Now search by MetadataId
            var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
            entityFilter.Conditions.Add(new MetadataConditionExpression("MetadataId", MetadataConditionOperator.Equals, accountMetadataId));
            var entityQueryExpression = new EntityQueryExpression()
            {
                Criteria = entityFilter
            };

            var retrieveMetadataChangesRequest = new RetrieveMetadataChangesRequest()
            {
                Query = entityQueryExpression
            };

            var response = (RetrieveMetadataChangesResponse)orgAdminService.Execute(retrieveMetadataChangesRequest);

            Assert.Single(response.EntityMetadata);
            Assert.Equal("account", response.EntityMetadata[0].LogicalName);
            Assert.Equal(accountMetadataId, response.EntityMetadata[0].MetadataId);
        }

        [Fact]
        public void RetrieveMetadataChangesRequest_MetadataId_In()
        {
            // First get the account and contact entity metadata to obtain their MetadataIds
            var accountRequest = new RetrieveEntityRequest() { LogicalName = Account.EntityLogicalName };
            var accountResponse = (RetrieveEntityResponse)orgAdminService.Execute(accountRequest);
            var accountMetadataId = accountResponse.EntityMetadata.MetadataId.Value;

            var contactRequest = new RetrieveEntityRequest() { LogicalName = Contact.EntityLogicalName };
            var contactResponse = (RetrieveEntityResponse)orgAdminService.Execute(contactRequest);
            var contactMetadataId = contactResponse.EntityMetadata.MetadataId.Value;

            // Now search by MetadataId with In operator
            var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
            entityFilter.Conditions.Add(new MetadataConditionExpression("MetadataId", MetadataConditionOperator.In, new[] { accountMetadataId, contactMetadataId }));
            var entityQueryExpression = new EntityQueryExpression()
            {
                Criteria = entityFilter
            };

            var retrieveMetadataChangesRequest = new RetrieveMetadataChangesRequest()
            {
                Query = entityQueryExpression
            };

            var response = (RetrieveMetadataChangesResponse)orgAdminService.Execute(retrieveMetadataChangesRequest);

            Assert.Equal(2, response.EntityMetadata.Count);
            Assert.Contains(response.EntityMetadata, e => e.LogicalName == "account");
            Assert.Contains(response.EntityMetadata, e => e.LogicalName == "contact");
        }

        [Fact]
        public void RetrieveMetadataChangesRequest_NoMatches_ReturnsEmpty()
        {
            var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
            entityFilter.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, "nonexistententity"));
            var entityQueryExpression = new EntityQueryExpression()
            {
                Criteria = entityFilter
            };

            var retrieveMetadataChangesRequest = new RetrieveMetadataChangesRequest()
            {
                Query = entityQueryExpression
            };

            var response = (RetrieveMetadataChangesResponse)orgAdminService.Execute(retrieveMetadataChangesRequest);

            Assert.Empty(response.EntityMetadata);
        }

        [Fact]
        public void RetrieveMetadataChangesRequest_MultipleConditions()
        {
            // First get the account entity metadata to obtain its ObjectTypeCode
            var accountRequest = new RetrieveEntityRequest() { LogicalName = Account.EntityLogicalName };
            var accountResponse = (RetrieveEntityResponse)orgAdminService.Execute(accountRequest);
            var accountObjectTypeCode = accountResponse.EntityMetadata.ObjectTypeCode.Value;

            // Search with multiple conditions (LogicalName AND ObjectTypeCode)
            var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
            entityFilter.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, "account"));
            entityFilter.Conditions.Add(new MetadataConditionExpression("ObjectTypeCode", MetadataConditionOperator.Equals, accountObjectTypeCode));
            var entityQueryExpression = new EntityQueryExpression()
            {
                Criteria = entityFilter
            };

            var retrieveMetadataChangesRequest = new RetrieveMetadataChangesRequest()
            {
                Query = entityQueryExpression
            };

            var response = (RetrieveMetadataChangesResponse)orgAdminService.Execute(retrieveMetadataChangesRequest);

            Assert.Single(response.EntityMetadata);
            Assert.Equal("account", response.EntityMetadata[0].LogicalName);
        }

        [Fact]
        public void RetrieveMetadataChangesRequest_NotEquals()
        {
            // Get all entities except account
            var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
            entityFilter.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.NotEquals, "account"));
            var entityQueryExpression = new EntityQueryExpression()
            {
                Criteria = entityFilter
            };

            var retrieveMetadataChangesRequest = new RetrieveMetadataChangesRequest()
            {
                Query = entityQueryExpression
            };

            var response = (RetrieveMetadataChangesResponse)orgAdminService.Execute(retrieveMetadataChangesRequest);

            Assert.NotEmpty(response.EntityMetadata);
            Assert.DoesNotContain(response.EntityMetadata, e => e.LogicalName == "account");
        }

        [Fact]
        public void RetrieveMetadataChangesRequest_NotIn()
        {
            // Get all entities except account and contact
            var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
            entityFilter.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.NotIn, new[] { "account", "contact" }));
            var entityQueryExpression = new EntityQueryExpression()
            {
                Criteria = entityFilter
            };

            var retrieveMetadataChangesRequest = new RetrieveMetadataChangesRequest()
            {
                Query = entityQueryExpression
            };

            var response = (RetrieveMetadataChangesResponse)orgAdminService.Execute(retrieveMetadataChangesRequest);

            Assert.NotEmpty(response.EntityMetadata);
            Assert.DoesNotContain(response.EntityMetadata, e => e.LogicalName == "account");
            Assert.DoesNotContain(response.EntityMetadata, e => e.LogicalName == "contact");
        }

        [Fact]
        public void RetrieveMetadataChangesRequest_NoCriteria_ReturnsAllEntities()
        {
            // Request without criteria should return all entities
            var entityQueryExpression = new EntityQueryExpression();

            var retrieveMetadataChangesRequest = new RetrieveMetadataChangesRequest()
            {
                Query = entityQueryExpression
            };

            var response = (RetrieveMetadataChangesResponse)orgAdminService.Execute(retrieveMetadataChangesRequest);

            Assert.NotEmpty(response.EntityMetadata);
            Assert.Contains(response.EntityMetadata, e => e.LogicalName == "account");
            Assert.Contains(response.EntityMetadata, e => e.LogicalName == "contact");
        }
    }

}
