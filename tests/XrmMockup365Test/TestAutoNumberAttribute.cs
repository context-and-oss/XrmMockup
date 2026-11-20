using DG.Tools.XrmMockup;
using DG.Tools.XrmMockup.Serialization;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestAutoNumberAttribute : RequestHandlerTestBase
    {
        private const string Table = "test_autonumber";
        private const string Column = "test_number";
        private readonly XrmMockupFixture fixture;

        public TestAutoNumberAttribute(XrmMockupFixture fixture) : base(fixture)
        {
            this.fixture = fixture;
            AddAutoTable(Table, "{SEQNUM:5}");
        }

        private void AddAutoTable(string table, string format)
        {
            var column = (StringAttributeMetadata)Text(Column);
            column.AutoNumberFormat = format;
            var second = (StringAttributeMetadata)Text("test_second");
            second.AutoNumberFormat = "{SEQNUM:5}";
            AddTable(table, Text("test_name"), column, second);
        }

        private string Number(Guid id, string table = Table) => Read(table, id).GetAttributeValue<string>(Column);
        private string Generate() => Number(Create(Table));
        private void Seed(long value, string table = Table, string column = Column) => orgAdminService.Execute(
            new SetAutoNumberSeedRequest { EntityName = table, AttributeName = column, Value = value });
        private long Next(string table = Table, string column = Column) =>
            ((GetNextAutoNumberValueResponse)orgAdminService.Execute(new GetNextAutoNumberValueRequest
            { EntityName = table, AttributeName = column })).NextAutoNumberValue;
        private long SeedValue() => ((GetAutoNumberSeedResponse)orgAdminService.Execute(
            new GetAutoNumberSeedRequest { EntityName = Table, AttributeName = Column })).AutoNumberSeedValue;

        [Fact]
        public void HandlersAreRegisteredExactlyOnce()
        {
            foreach (var name in new[] { "SetAutoNumberSeed", "GetAutoNumberSeed", "GetNextAutoNumberValue" })
                Assert.Single(engine.RequestHandlers.Where(h => h.HandlesRequest(name)));
        }

        [Fact]
        public void DefaultSeedAndReadRequestsDoNotConsumeSequence()
        {
            Assert.Equal(1000, SeedValue());
            Assert.Equal(1000, Next());
            Assert.Equal(1000, Next());
            Assert.Equal("01000", Generate());
            Assert.Equal("01001", Generate());
            Assert.Equal(1000, SeedValue());
            Assert.Equal(1002, Next());
        }

        [Fact]
        public async Task ConcurrentCreatesOnSameInstanceAllocateUniqueSequenceValues()
        {
            const int workers = 8;
            const int createsPerWorker = 32;
            const int seed = 2000;
            // Rendering between repeated placeholders exposes both duplicate allocations and
            // values that mix two reservations if the read/render/advance is not atomic.
            AddAutoTable(Table, "N-{SEQNUM:5}-{RANDSTRING:6}-{SEQNUM:5}");
            Seed(seed);
            Seed(3000, column: "test_second");

            using (var start = new Barrier(workers))
            {
                // Dedicated workers avoid a thread-pool starvation deadlock at the start barrier.
                var tasks = Enumerable.Range(0, workers).Select(_ => Task.Factory.StartNew(() =>
                {
                    Assert.True(start.SignalAndWait(TimeSpan.FromSeconds(30)), "Create workers did not start together.");
                    for (var i = 0; i < createsPerWorker; i++) Create(Table);
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
                await Task.WhenAll(tasks);
            }

            var rows = All(Table).Entities;
            var count = workers * createsPerWorker;
            Assert.Equal(count, rows.Count);
            var sequences = rows.Select(row =>
            {
                var parts = row.GetAttributeValue<string>(Column).Split('-');
                Assert.Equal(parts[1], parts[3]);
                return int.Parse(parts[1], CultureInfo.InvariantCulture);
            }).OrderBy(value => value).ToArray();
            Assert.Equal(count, sequences.Distinct().Count());
            Assert.Equal(Enumerable.Range(seed, count), sequences);
            Assert.Equal(seed + count, Next());
            Assert.Equal(3000 + count, Next(column: "test_second"));
        }

        [Fact]
        public void SeedCanBeChangedAfterGenerationAndCanBeZero()
        {
            Generate();
            Seed(42);
            Assert.Equal(42, SeedValue());
            Assert.Equal("00042", Generate());
            Seed(0);
            Assert.Equal("00000", Generate());
            Assert.Equal(1, Next());
        }

        [Fact]
        public void SequencesAreScopedToTableAndColumn()
        {
            AddAutoTable("test_othernumber", "{SEQNUM:5}");
            Seed(20);
            var id = Create(Table);
            Assert.Equal("00020", Number(id));
            Assert.Equal("01000", Read(Table, id).GetAttributeValue<string>("test_second"));
            Assert.Equal("01000", Number(Create("test_othernumber"), "test_othernumber"));
        }

        [Theory]
        [InlineData("manual")]
        [InlineData("CUSTOM-123")]
        public void ExplicitValuesArePreservedAndDoNotConsumeSequence(string value)
        {
            Assert.Equal(value, Number(Create(Table, (Column, value))));
            Assert.Equal(1000, Next());
        }

        [Fact]
        public void ExplicitEmptyStringSuppressesGenerationAndIsNormalizedToNull()
        {
            // Preserve the existing Create string normalization, without allocating a number.
            Assert.Null(Number(Create(Table, (Column, string.Empty))));
            Assert.Equal(1000, Next());
        }

        [Fact]
        public void ExplicitNullGeneratesAValue()
        {
            Assert.Equal("01000", Number(Create(Table, (Column, null))));
        }

        [Fact]
        public void UpdateDoesNotGenerateOrConsumeAndCanReplaceAValue()
        {
            var id = Create(Table);
            orgAdminService.Update(new Entity(Table, id) { [Column] = "updated" });
            Assert.Equal("updated", Number(id));
            orgAdminService.Update(new Entity(Table, id) { [Column] = null });
            Assert.Null(Number(id));
            Assert.Equal(1001, Next());
        }

        [Theory]
        [InlineData("{SEQNUM:12}", "000000001000")]
        [InlineData("{SEQNUM:1}", "1000")]
        [InlineData("N-{SEQNUM:5}-{SEQNUM:6}", "N-01000-001000")]
        [InlineData("{SEQNUM:005}", "01000")]
        public void SequenceParsesCompleteWidthAndReservesOnce(string format, string expected)
        {
            AddAutoTable(Table, format);
            Assert.Equal(expected, Generate());
            Assert.Equal(1001, Next());
        }

        [Theory]
        [InlineData("{SEQNUM:0}")]
        [InlineData("{SEQNUM:-1}")]
        [InlineData("{SEQNUM:1x}")]
        [InlineData("{SEQNUM: 2}")]
        [InlineData("{SEQNUM:4001}")]
        [InlineData("{SEQNUM:99999999999999999999}")]
        [InlineData("{SEQNUM:}")]
        [InlineData("{RANDSTRING:0}")]
        [InlineData("{RANDSTRING:7}")]
        [InlineData("{RANDSTRING:12}")]
        [InlineData("{RANDSTRING:2x}")]
        [InlineData("{DATETIMEUTC:}")]
        [InlineData("{DATETIMEUTC:%}")]
        [InlineData("{UNKNOWN:2}")]
        [InlineData("{SEQNUM:2")]
        [InlineData("SEQNUM:2}")]
        [InlineData("{{SEQNUM:2}}")]
        [InlineData("{SEQNUM:2}-{RANDSTRING:7}")]
        public void InvalidFormatsFaultWithoutConsumingSequence(string format)
        {
            AddAutoTable(Table, format);
            Assert.Throws<FaultException>(() => Generate());
            Assert.Equal(1000, Next());
            Assert.Empty(All(Table).Entities);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(6)]
        public void RandomStringsHaveRequestedLengthAndAlphabet(int length)
        {
            AddAutoTable(Table, "{RANDSTRING:" + length + "}-{RANDSTRING:" + length + "}");
            var parts = Generate().Split('-');
            Assert.All(parts, p => Assert.Matches("^[A-Z0-9]{" + length + "}$", p));
            Assert.Equal(1000, Next());
        }

        [Fact]
        public void UtcFormatsUseSimulatedClockAndInvariantCulture()
        {
            AddAutoTable(Table, "{SEQNUM:5}-{RANDSTRING:6}-{DATETIMEUTC:yyyy-MM-ddTHH:mm:ss}-{DATETIMEUTC:MMMM}");
            var offset = TimeSpan.FromDays(730);
            crm.AddTime(offset);
            var culture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                var before = DateTime.UtcNow.Add(offset);
                var number = Generate();
                var after = DateTime.UtcNow.Add(offset);
                Assert.StartsWith("01000-", number);
                var timestamp = DateTime.ParseExact(number.Substring(13, 19), "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                Assert.InRange(timestamp, before.AddSeconds(-1), after);
                Assert.EndsWith(timestamp.ToString("MMMM", CultureInfo.InvariantCulture), number);
            }
            finally { CultureInfo.CurrentCulture = culture; }
        }

        [Theory]
        [InlineData("missing", Column)]
        [InlineData(Table, "missing")]
        [InlineData(Table, "test_name")]
        [InlineData(null, Column)]
        [InlineData(Table, null)]
        public void SeedRequestsRejectMissingOrNonAutoNumberAttributes(string entity, string attribute)
        {
            Assert.Throws<FaultException>(() => Seed(2, entity, attribute));
            Assert.Throws<FaultException>(() => Next(entity, attribute));
            Assert.Throws<FaultException>(() => orgAdminService.Execute(new GetAutoNumberSeedRequest
            { EntityName = entity, AttributeName = attribute }));
            Assert.Equal(1000, Next());
        }

        [Fact]
        public void SeedRequiresAnInt64Value()
        {
            var request = new OrganizationRequest("SetAutoNumberSeed");
            request["EntityName"] = Table;
            request["AttributeName"] = Column;
            Assert.Throws<FaultException>(() => orgAdminService.Execute(request));
            request["Value"] = "42";
            Assert.Throws<FaultException>(() => orgAdminService.Execute(request));
            request["Value"] = 42L;
            Assert.IsType<SetAutoNumberSeedResponse>(orgAdminService.Execute(request));
            Assert.Equal(42, Next());
        }

        [Fact]
        public void NegativeSeedFaultsWithoutChangingState()
        {
            Seed(20);
            Assert.Throws<FaultException>(() => Seed(-1));
            Assert.Equal(20, SeedValue());
            Assert.Equal(20, Next());
        }

        [Fact]
        public void SequenceDoesNotWrapAtInt64Boundary()
        {
            Seed(long.MaxValue);
            Assert.Equal(long.MaxValue.ToString(CultureInfo.InvariantCulture), Generate());
            Assert.Throws<FaultException>(() => Generate());
            Assert.Throws<FaultException>(() => Next());
            Seed(1);
            Assert.Equal("00001", Generate());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SnapshotsRetainExhaustionState(bool json)
        {
            Seed(long.MaxValue);
            Generate();
            engine.TakeSnapshot("exhausted");
            var snapshot = engine.TakeJsonSnapshot();
            Seed(5);
            if (json) engine.RestoreJsonSnapshot(snapshot);
            else engine.RestoreToSnapshot("exhausted");
            Assert.Throws<FaultException>(() => Next());
            Assert.Throws<FaultException>(() => Generate());
        }

        [Fact]
        public void CreateMultipleAndUpsertUseTheCreateGenerator()
        {
            var targets = new EntityCollection { EntityName = Table };
            targets.Entities.Add(new Entity(Table));
            targets.Entities.Add(new Entity(Table) { [Column] = "manual" });
            var response = (CreateMultipleResponse)orgAdminService.Execute(new CreateMultipleRequest { Targets = targets });
            Assert.Equal("01000", Number(response.Ids[0]));
            Assert.Equal("manual", Number(response.Ids[1]));
            var created = (UpsertResponse)orgAdminService.Execute(new UpsertRequest { Target = new Entity(Table) });
            Assert.True(created.RecordCreated);
            Assert.Equal("01001", Number(created.Target.Id));
            var updated = (UpsertResponse)orgAdminService.Execute(new UpsertRequest
            { Target = new Entity(Table, created.Target.Id) { ["test_name"] = "changed" } });
            Assert.False(updated.RecordCreated);
            Assert.Equal("01001", Number(created.Target.Id));
            Assert.Equal(1002, Next());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SnapshotsRestoreDataSeedAndNextValueAndCanBeReused(bool json)
        {
            Seed(30);
            var original = Create(Table);
            crm.AddTime(10);
            var snapshot = engine.TakeJsonSnapshot();
            engine.TakeSnapshot("autonumber");
            for (var i = 0; i < 2; i++)
            {
                Seed(500);
                Create(Table);
                crm.AddTime(10);
                if (json) engine.RestoreJsonSnapshot(snapshot);
                else engine.RestoreToSnapshot("autonumber");
                Assert.Single(All(Table).Entities);
                Assert.Equal("00030", Number(original));
                Assert.Equal(30, SeedValue());
                Assert.Equal(31, Next());
                Assert.Equal(TimeSpan.FromDays(10), engine.TimeOffset);
                Assert.Equal("00031", Generate());
            }
        }

        [Fact]
        public void LegacyJsonSnapshotWithoutStateUsesDefaultSeed()
        {
            var dto = JsonSerializer.Deserialize<SnapshotDTO>(engine.TakeJsonSnapshot());
            dto.AutoNumbers = null;
            var legacy = JsonSerializer.Serialize(dto, new JsonSerializerOptions
            { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            Assert.DoesNotContain("AutoNumbers", legacy);
            Seed(50);
            engine.RestoreJsonSnapshot(legacy);
            Assert.Equal(1000, SeedValue());
            Assert.Equal("01000", Generate());
        }

        [Fact]
        public void ResetClearsSeedsAndCountersButSnapshotCanRestoreThem()
        {
            Seed(60);
            Generate();
            engine.TakeSnapshot("before-reset");
            crm.ResetEnvironment();
            Assert.Equal(1000, SeedValue());
            Assert.Equal("01000", Generate());
            engine.RestoreToSnapshot("before-reset");
            Assert.Equal(60, SeedValue());
            Assert.Equal("00061", Generate());
        }

        [Fact]
        public void InstancesDoNotShareSequenceStateAndJsonStateIsPortable()
        {
            Seed(80);
            Generate();
            var other = new TestAutoNumberAttribute(fixture);
            Assert.Equal("01000", other.Generate());
            Assert.Equal(1000, other.SeedValue());
            other.engine.RestoreJsonSnapshot(engine.TakeJsonSnapshot());
            Assert.Equal(80, other.SeedValue());
            Assert.Equal("00081", other.Generate());
            Assert.Equal(81, Next());
        }

        [Fact]
        public void FailedTransactionRestoresGeneratorAlongsideData()
        {
            Seed(90);
            var request = new ExecuteTransactionRequest
            {
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest { Target = new Entity(Table) },
                    new SetAutoNumberSeedRequest { EntityName = Table, AttributeName = Column, Value = -1 }
                }
            };
            Assert.Throws<FaultException>(() => orgAdminService.Execute(request));
            Assert.Empty(All(Table).Entities);
            Assert.Equal(90, SeedValue());
            Assert.Equal("00090", Generate());
        }
    }
}
