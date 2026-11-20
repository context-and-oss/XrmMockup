using System;
using System.Linq;
using System.ServiceModel;
using Task = System.Threading.Tasks.Task;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestAlternateKeyUniqueness : RequestHandlerTestBase
    {
        private const string Table = "test_uniquekey";
        private const string A = "test_a";
        private const string B = "test_b";
        private const int DuplicateKey = unchecked((int)0x80060892);

        public TestAlternateKeyUniqueness(XrmMockupFixture fixture) : base(fixture)
        {
            AddTable(Table, Text(A), Text(B), Attribute("numberofemployees", "test_number"),
                Attribute("revenue", "test_money"), Attribute("exchangerate", "test_decimal"), Date("test_date"), Choice("test_choice", 1, 2),
                Lookup("test_lookup", "account", "contact"), Attribute("transactioncurrencyid", "transactioncurrencyid"),
                Attribute("exchangerate", "exchangerate"), Attribute("revenue_base", "test_money_base"));
            Keys(Key("composite", A, B));
        }

        private EntityKeyMetadata Key(string name, params string[] columns)
        {
            var key = new EntityKeyMetadata { LogicalName = name, KeyAttributes = columns };
            Set(key, "EntityKeyIndexStatus", EntityKeyIndexStatus.Active);
            return key;
        }
        private void Keys(params EntityKeyMetadata[] keys) => Set(metadata.EntityMetadata[Table], "Keys", keys);
        private Guid Row(string a, string b) => Create(Table, (A, a), (B, b));
        private Entity Update(Guid id, params (string Name, object Value)[] attributes)
        {
            var entity = new Entity(Table, id);
            foreach (var attribute in attributes) entity[attribute.Name] = attribute.Value;
            return entity;
        }
        private FaultException<OrganizationServiceFault> Duplicate(Action action)
        {
            var fault = Assert.Throws<FaultException<OrganizationServiceFault>>(action);
            Assert.Equal(DuplicateKey, fault.Detail.ErrorCode);
            Assert.Equal(fault.Message, fault.Detail.Message);
            return fault;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IndependentKeysRejectDuplicateOfEitherKey(bool update)
        {
            Keys(Key("first", A), Key("second", B));
            Row("one", "x");
            if (update)
            {
                var id = Row("two", "y");
                Duplicate(() => orgAdminService.Update(Update(id, (B, "x"))));
                Assert.Equal("y", Read(Table, id)[B]);
            }
            else Duplicate(() => Row("two", "x"));
            Assert.Equal(update ? 2 : 1, All(Table).Entities.Count);
        }

        [Fact]
        public void PartialUpdateMergesWithTargetNotComparisonRow()
        {
            var id = Row("one", "x");
            Row("two", "y");
            orgAdminService.Update(Update(id, (A, "two")));
            Assert.Equal("two", Read(Table, id)[A]);
            Assert.Equal("x", Read(Table, id)[B]);
            // The omitted component now makes a real duplicate.
            Duplicate(() => orgAdminService.Update(Update(id, (B, "y"))));
            Assert.Equal("x", Read(Table, id)[B]);
        }

        [Fact]
        public void EachCandidateIsComparedIndependentlyAndSelfIsExcluded()
        {
            Row("one", "x");
            Row("two", "y");
            var id = Row("three", "y");
            Duplicate(() => orgAdminService.Update(Update(id, (A, "two"))));
            orgAdminService.Update(Update(id, (A, "three")));
            orgAdminService.Update(Update(id, ("test_number", 42)));
        }

        [Fact]
        public void MissingNullAndEmptyComponentsDoNotEnforceUniqueness()
        {
            var missing = Create(Table, (A, "one"));
            Row("one", null);
            Row("one", "");
            var populated = Row("one", "x");
            orgAdminService.Update(Update(populated, (B, null)));
            orgAdminService.Update(Update(missing, (A, "one")));
            Row("one", "x");
            Duplicate(() => orgAdminService.Update(Update(populated, (B, "x"))));
            Assert.Null(Read(Table, populated).GetAttributeValue<string>(B));
        }

        [Theory]
        [InlineData(EntityKeyIndexStatus.Pending)]
        [InlineData(EntityKeyIndexStatus.InProgress)]
        [InlineData(EntityKeyIndexStatus.Failed)]
        public void NonActiveKeysDoNotEnforceUniqueness(EntityKeyIndexStatus state)
        {
            var key = Key("key", A);
            Set(key, "EntityKeyIndexStatus", state);
            Keys(key);
            Row("one", "x");
            var id = Row("one", "y");
            orgAdminService.Update(Update(id, (B, "x")));
        }

        [Fact]
        public void NoKeysOrUnknownIndexStateDoNotEnforceUniqueness()
        {
            Keys(Key("unknown", A));
            Set(metadata.EntityMetadata[Table].Keys[0], "EntityKeyIndexStatus", null);
            Row("one", "x");
            Row("one", "x");
            Keys();
            Row("one", "x");
        }

        [Fact]
        public void MissingLabelsFallBackToLogicalNames()
        {
            metadata.EntityMetadata[Table].Attributes.First(a => a.LogicalName == A).DisplayName = null;
            metadata.EntityMetadata[Table].Attributes.First(a => a.LogicalName == B).DisplayName = new Label();
            Row("one", "x");
            var fault = Duplicate(() => Row("one", "x"));
            Assert.Contains("test_a, test_b", fault.Message);
            Assert.Contains("entity key composite", fault.Message);
        }

        [Fact]
        public void LocalizedLabelsAreUsedWhenPresent()
        {
            var key = Key("key", A);
            key.DisplayName = TestLabel(new Label("Name key", 1033));
            Keys(key);
            metadata.EntityMetadata[Table].Attributes.First(a => a.LogicalName == A).DisplayName = TestLabel(new Label("Name", 1033));
            Row("one", "x");
            var fault = Duplicate(() => Row("one", "y"));
            Assert.Contains("attribute values Name already", fault.Message);
            Assert.Contains("entity key Name key", fault.Message);
        }

        [Theory]
        [InlineData("test_number")]
        [InlineData("test_decimal")]
        [InlineData("test_money")]
        [InlineData("test_date")]
        [InlineData("test_choice")]
        public void TypedKeyValuesMatchTheirStoredRepresentation(string column)
        {
            object value = column == "test_decimal" ? (object)42m : column == "test_money" ? (object)new Money(42m) :
                column == "test_date" ? (object)new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) :
                column == "test_choice" ? (object)new OptionSetValue(1) : 42;
            Keys(Key("typed", column));
            Create(Table, (column, value));
            Duplicate(() => Create(Table, (column, value)));
            var id = column == "test_choice" ? Create(Table, (column, new OptionSetValue(2))) : Create(Table);
            Duplicate(() => orgAdminService.Update(Update(id, (column, value))));
        }

        [Fact]
        public void DefaultsAndGeneratedValuesAreCheckedAfterTheyAreApplied()
        {
            Keys(Key("choice", "test_choice"));
            Create(Table); // metadata default is 1
            Duplicate(() => Create(Table));

            Keys(Key("generated", A));
            var column = (StringAttributeMetadata)metadata.EntityMetadata[Table].Attributes.First(a => a.LogicalName == A);
            column.AutoNumberFormat = "N-{SEQNUM:5}";
            Create(Table, (A, "N-01000"));
            Duplicate(() => Create(Table));
        }

        [Fact]
        public void ExplicitCreateAttributesTakePrecedenceOverKeyAttributes()
        {
            Row("one", "x");
            var id = orgAdminService.Create(new Entity(Table)
            {
                KeyAttributes = { { A, "one" }, { B, "x" } }, [A] = "two"
            });
            Assert.Equal("two", Read(Table, id)[A]);
            Assert.Equal("x", Read(Table, id)[B]);
        }

        [Fact]
        public void FailedUpdateDoesNotAdvanceTheStoredRowVersion()
        {
            Row("one", "x");
            var id = Row("two", "x");
            var before = Read(Table, id).RowVersion;
            Duplicate(() => orgAdminService.Update(Update(id, (A, "one"))));
            Assert.Equal(before, Read(Table, id).RowVersion);
        }

        [Fact]
        public void StringsMatchCaseInsensitively()
        {
            Row("One", "X");
            Duplicate(() => Row("one", "x"));
        }

        [Fact]
        public void LookupKeysUseIdentityNotDisplayNameAndIncludeEntityType()
        {
            Keys(Key("lookup", "test_lookup"));
            var id = Guid.NewGuid();
            orgAdminService.Create(new Account { Id = id, Name = "Account" });
            orgAdminService.Create(new Contact { Id = id, FirstName = "Contact" });
            var lookup = new EntityReference("account", id) { Name = "Ignored" };
            Create(Table, ("test_lookup", lookup));
            Duplicate(() => Create(Table, ("test_lookup", new EntityReference("account", id))));
            var other = Create(Table, ("test_lookup", new EntityReference("contact", id)));
            Duplicate(() => orgAdminService.Update(Update(other, ("test_lookup", lookup))));
        }

        [Fact]
        public void AlternateKeyAddressedUpdateUsesResolvedRowAndNewAttributes()
        {
            Row("one", "x");
            var id = Row("two", "x");
            var target = new Entity(Table) { KeyAttributes = { { A, "two" }, { B, "x" } } };
            target[A] = "one";
            Duplicate(() => orgAdminService.Update(target));
            Assert.Equal("two", Read(Table, id)[A]);
            target[A] = "three";
            orgAdminService.Update(target);
            Assert.Equal("three", Read(Table, id)[A]);
        }

        [Fact]
        public void CreateAndUpsertPopulateColumnsFromKeyAttributes()
        {
            var target = new Entity(Table) { KeyAttributes = { { A, "one" }, { B, "x" } } };
            var response = (UpsertResponse)orgAdminService.Execute(new UpsertRequest { Target = target });
            Assert.True(response.RecordCreated);
            Assert.Equal("one", Read(Table, response.Target.Id)[A]);
            Assert.Equal("x", Read(Table, response.Target.Id)[B]);
            Duplicate(() => orgAdminService.Create(new Entity(Table) { KeyAttributes = { { A, "one" }, { B, "x" } } }));
            response = (UpsertResponse)orgAdminService.Execute(new UpsertRequest
            {
                Target = new Entity(Table) { KeyAttributes = { { A, "one" }, { B, "x" } }, ["test_number"] = 5 }
            });
            Assert.False(response.RecordCreated);
            Assert.Equal(5, Read(Table, response.Target.Id)["test_number"]);
        }

        [Fact]
        public void UpsertByIdChecksBothCreateAndUpdateBranches()
        {
            Row("one", "x");
            Duplicate(() => orgAdminService.Execute(new UpsertRequest { Target = Update(Guid.NewGuid(), (A, "one"), (B, "x")) }));
            var id = Row("two", "x");
            Duplicate(() => orgAdminService.Execute(new UpsertRequest { Target = Update(id, (A, "one")) }));
            Assert.Equal("two", Read(Table, id)[A]);
        }

        [Fact]
        public void PreOperationPluginChangesAreCheckedForCreateAndUpdate()
        {
            var accountMetadata = CloneMetadata(metadata.EntityMetadata["account"]);
            Set(accountMetadata, "Keys", new[] { Key("name_key", "name") });
            metadata.EntityMetadata["account"] = accountMetadata;
            crm.DisableRegisteredPlugins(true);
            crm.RegisterAdditionalPlugins(PluginRegistrationScope.Temporary, typeof(DG.Some.Namespace.AccountUniqueKeyPlugin));
            var existing = orgAdminService.Create(new Account { Name = "duplicate" });
            Duplicate(() => orgAdminService.Execute(new CreateRequest { Target = new Account { Name = "safe" }, ["tag"] = "duplicate" }));
            var id = orgAdminService.Create(new Account { Name = "other" });
            Duplicate(() => orgAdminService.Execute(new UpdateRequest { Target = new Account { Id = id, Name = "safe" }, ["tag"] = "duplicate" }));
            Assert.Equal("other", Read("account", id)["name"]);
            Assert.Equal("duplicate", Read("account", existing)["name"]);
        }

        [Theory]
        [InlineData("CreateMultiple")]
        [InlineData("UpdateMultiple")]
        [InlineData("UpsertMultiple")]
        public void BulkDuplicateRollsBackEarlierWrites(string message)
        {
            Row("existing", "x");
            var first = Update(Guid.NewGuid(), (A, "one"), (B, "x"));
            var second = Update(Guid.NewGuid(), (A, "existing"), (B, "x"));
            if (message == "UpdateMultiple")
            {
                first.Id = Row("before", "x");
                second.Id = Row("other", "x");
            }
            var count = All(Table).Entities.Count;
            var request = new OrganizationRequest(message)
            {
                ["Targets"] = new EntityCollection(new[] { first, second }) { EntityName = Table }
            };
            Duplicate(() => orgAdminService.Execute(request));
            Assert.Equal(count, All(Table).Entities.Count);
            if (message == "UpdateMultiple") Assert.Equal("before", Read(Table, first.Id)[A]);
            // Restored handlers must enforce the same keys too.
            Duplicate(() => Row("existing", "x"));
            Row("new", "x");
        }

        [Theory]
        [InlineData("UpdateMultiple")]
        [InlineData("UpsertMultiple")]
        public void BulkRequestsResolveEachAlternateKeyEvenWhenIdsAreEmpty(string message)
        {
            var one = Row("one", "x");
            var two = Row("two", "x");
            var first = new Entity(Table) { KeyAttributes = { { A, "one" }, { B, "x" } }, ["test_number"] = 1 };
            var second = new Entity(Table) { KeyAttributes = { { A, "two" }, { B, "x" } }, ["test_number"] = 2 };
            orgAdminService.Execute(new OrganizationRequest(message)
            {
                ["Targets"] = new EntityCollection(new[] { first, second }) { EntityName = Table }
            });
            Assert.Equal(1, Read(Table, one)["test_number"]);
            Assert.Equal(2, Read(Table, two)["test_number"]);
        }

        [Fact]
        public void BulkUpsertCreatesMultipleKeyAddressedRowsWithoutIds()
        {
            orgAdminService.Execute(new UpsertMultipleRequest
            {
                Targets = new EntityCollection(new[]
                {
                    new Entity(Table) { KeyAttributes = { { A, "one" }, { B, "x" } } },
                    new Entity(Table) { KeyAttributes = { { A, "two" }, { B, "x" } } }
                }) { EntityName = Table }
            });
            Assert.Equal(2, All(Table).Entities.Count);
        }

        [Fact]
        public void ExecuteMultiplePreservesFaultDetailAndContinuesWithoutRollingBackSuccesses()
        {
            Row("one", "x");
            var response = (ExecuteMultipleResponse)orgAdminService.Execute(new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings { ContinueOnError = true, ReturnResponses = true },
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest { Target = Update(Guid.Empty, (A, "two"), (B, "x")) },
                    new CreateRequest { Target = Update(Guid.Empty, (A, "one"), (B, "x")) },
                    new CreateRequest { Target = Update(Guid.Empty, (A, "three"), (B, "x")) }
                }
            });
            Assert.True(response.IsFaulted);
            Assert.Equal(DuplicateKey, response.Responses[1].Fault.ErrorCode);
            Assert.Equal(3, All(Table).Entities.Count);
        }

        [Fact]
        public void ExecuteTransactionRollsBackEarlierWritesOnDuplicate()
        {
            Row("one", "x");
            Duplicate(() => orgAdminService.Execute(new ExecuteTransactionRequest
            {
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest { Target = Update(Guid.Empty, (A, "two"), (B, "x")) },
                    new CreateRequest { Target = Update(Guid.Empty, (A, "one"), (B, "x")) }
                }
            }));
            Assert.Single(All(Table).Entities);
        }

        [Fact]
        public async Task ConcurrentCreatesCannotPublishTheSameKey()
        {
            var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() =>
            {
                try { Row("one", "x"); return true; }
                catch (FaultException<OrganizationServiceFault> e) when (e.Detail.ErrorCode == DuplicateKey) { return false; }
            })));
            Assert.Single(results.Where(success => success));
            Assert.Single(All(Table).Entities);
        }
    }
}
