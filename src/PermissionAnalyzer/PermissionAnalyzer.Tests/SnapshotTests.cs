using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool;
using XrmMockup.PermissionAnalyzer.Tool.Commands;
using XrmMockup.PermissionAnalyzer.Tool.Output;
using XrmMockup.PermissionAnalyzer.Tool.Snapshots;
using XrmMockup.PermissionAnalyzer.Tool.Workspace;

namespace XrmMockup.PermissionAnalyzer.Tests;

public class SnapshotTests
{
    private const string Source = """
        public class P : Plugin {
            public P() {
                RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, Run)
                    .AddFilteredAttributes("name");
                RegisterCustomAPI("ctx_Run", c => c.OrganizationService.Create(new Contact()))
                    .WithExecutePrivilegeName("prvRun");
            }
            private void Run(LocalPluginContext c) {
                new Store(c.OrganizationService).Save(new Contact());
                c.OrganizationAdminService.Delete("task", Guid.Empty);
                c.OrganizationService.Retrieve(c.PluginExecutionContext.PrimaryEntityName, Guid.Empty, new ColumnSet());
            }
        }
        public class Store {
            private readonly IOrganizationService service;
            public Store(IOrganizationService service) { this.service = service; }
            public void Save(Entity entity) { Returned().Create(entity); }
            private IOrganizationService Returned() => service;
        }
        public class C : Plugin {
            public C() { RegisterPluginStep<Contact>(EventOperation.Create, ExecutionStage.PostOperation,
                c => c.OrganizationService.Delete("lead", Guid.Empty)); }
        }
        public class App {
            [Microsoft.Azure.Functions.Worker.Function("Sync")]
            public void Run(IOrganizationService service) { new Store(service).Save(new Account()); }
            public void Other(IOrganizationService service, string table) {
                service.Retrieve(table, Guid.Empty, new ColumnSet());
            }
        }
        """;

    private static readonly RoleDefinition Role = new("Reader",
        new Dictionary<string, IReadOnlyDictionary<Privilege, PrivilegeDepth>> {
            ["account"] = new Dictionary<Privilege, PrivilegeDepth> { [Privilege.Read] = PrivilegeDepth.Organization },
            ["contact"] = new Dictionary<Privilege, PrivilegeDepth>(),
            ["lead"] = new Dictionary<Privilege, PrivilegeDepth>()
        }, new Dictionary<string, PrivilegeDepth> { ["prvRun"] = PrivilegeDepth.Organization }, Guid.Parse("01234567-1234-1234-1234-123456789abc"));

    private static ManifestBuilder Builder()
    {
        var builder = new ManifestBuilder(Analyze.Root,
            [new RequestRule("ctx_Touch") { Checks = [new RuleCheck([Privilege.Write]) { Member = "Target" }] }],
            [new LookupColumn("account", "parentcustomerid", ["account", "contact"])]);
        Assert.True(builder.Add(Analyze.Compile(Source)));
        builder.AddPrivilegeCatalog([new PrivilegeDefinition("prvRun", null, [])], [new CustomApiDefinition("ctx_Run", "prvRun")]);
        return builder;
    }

    private static AnalysisSnapshot Snapshot(ManifestBuilder builder, IReadOnlyList<UnresolvedOperation>? diagnostics = null) =>
        AnalysisSnapshot.Create(builder, new AnalysisProvenance("App.csproj", [], [], "config-hash", []), diagnostics ?? [], [Role], "test", "src/App/");

    private static string Json(Manifest manifest, bool coverage = false)
    {
        using var writer = new StringWriter();
        new JsonManifestWriter().Write(manifest, coverage ? CoverageEvaluator.Evaluate(manifest,
            new CoverageOptions { Roles = RoleSet.Combine([Role], "test") }) : null, writer);
        return writer.ToString();
    }

    private static async Task<AnalysisSnapshot> RoundTrip(AnalysisSnapshot snapshot)
    {
        var directory = Directory.CreateTempSubdirectory("permission-snapshot-").FullName;
        try {
            var path = Path.Combine(directory, "snapshot.json");
            await snapshot.SaveAsync(path, CancellationToken.None);
            return AnalysisSnapshot.Read(path);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public async Task FullAnalysisRoundTripsIdentitiesSourcesCallsCascadesAndGlobalPrivileges()
    {
        var source = Builder();
        var snapshot = await RoundTrip(Snapshot(source));
        var restored = ManifestBuilder.Restore(Analyze.Root, snapshot.Graph);
        Assert.Equal(Json(source.Build()), Json(restored.Build()));
        Assert.Equal(Json(source.Build(Role.Privileges.Keys), true), Json(restored.Build(Role.Privileges.Keys), true));
        var role = Assert.Single(snapshot.RoleDefinitions()!);
        Assert.Equal(Role.RoleId, role.RoleId);
        Assert.Equal(Role.GlobalPrivileges, role.GlobalPrivileges);
        Assert.Equal("src/App/", snapshot.SourceRoot);
    }

    [Theory]
    [InlineData("Update", "account", "name")]
    [InlineData("Update", "account", "other")]
    [InlineData("Update", "account", null)]
    [InlineData("Create", "account", "parentcustomerid")]
    [InlineData("UpsertRequest", "account", "name")]
    [InlineData("ctx_Touch", "account", null)]
    [InlineData("ctx_Run", "account", null)]
    [InlineData("Associate", "contact", null)]
    [InlineData("SendEmail", "email", null)]
    public async Task NewOperationsAgainstSnapshotMatchFreshEvaluation(string message, string table, string? column)
    {
        var fresh = Builder();
        var snapshot = await RoundTrip(Snapshot(fresh));
        var restored = ManifestBuilder.Restore(Analyze.Root, snapshot.Graph);
        string[]? columns = column is null ? null : [column];
        fresh.AddOperation(message, table, columns); restored.AddOperation(message, table, columns);
        var scope = EntryPointSelector.Operation(message, table);
        // AddOperation normalizes request-type aliases, so use the actual operation id for UpsertRequest.
        if (message == "UpsertRequest") scope = EntryPointSelector.Operation("Upsert", table);
        Assert.Equal(Json(fresh.Build(Role.Privileges.Keys, scope), true), Json(restored.Build(Role.Privileges.Keys, scope), true));
        Assert.Single(restored.Build(scope: scope).EntryPoints);
    }

    [Fact]
    public async Task SnapshotDoesNotRetainQueriesOrComputedCoverage()
    {
        var fresh = Builder();
        fresh.AddOperation("Update", "account", ["name"]);
        fresh.AddEntryMethods(["App.Other"]);
        // Graph snapshots deliberately do not retain synthetic operations. Configured entry methods do persist.
        var snapshot = await RoundTrip(Snapshot(fresh));
        var restored = ManifestBuilder.Restore(Analyze.Root, snapshot.Graph);
        Assert.DoesNotContain(restored.Build().EntryPoints, e => e.Kind == EntryPointKind.Operation);
        Assert.Contains(restored.Build().EntryPoints, e => e.Id.StartsWith("method:App.Other", StringComparison.Ordinal));
        var scope = EntryPointSelector.Methods(["App.Run"]);
        restored.AddEntryMethods(["App.Run"]);
        Assert.Single(restored.Build(scope: scope).EntryPoints);
        Assert.Null(restored.Build().TablesWithoutRoleMetadata);
    }

    [Fact]
    public async Task SourceDiagnosticsSurviveEveryNewScenarioAndStrictCoverage()
    {
        var diagnostic = new UnresolvedOperation("source compilation", [], "unbound source", RunsAs.Caller,
            new RequirementSource(new SourceLocation("Broken.cs", 1, 1), []));
        var snapshot = await RoundTrip(Snapshot(Builder(), [diagnostic]));
        var restored = ManifestBuilder.Restore(Analyze.Root, snapshot.Graph);
        restored.AddOperation("WhoAmI", "systemuser");
        var manifest = restored.Build(scope: EntryPointSelector.Operation("WhoAmI", "systemuser")).WithDiagnostics(snapshot.Diagnostics);
        Assert.True(manifest.Incomplete);
        Assert.Equal("unbound source", Assert.Single(Assert.Single(manifest.EntryPoints).Unresolved).Reason);
        Assert.False(CoverageEvaluator.Evaluate(manifest, new CoverageOptions { Roles = RoleSet.Combine([Role], "test") }).Summary.Passed(strict: true));
    }

    [Fact]
    public async Task SnapshotBytesAreDeterministicAndReadWriteStableAcrossCultures()
    {
        var directory = Directory.CreateTempSubdirectory("permission-snapshot-").FullName;
        var previous = CultureInfo.CurrentCulture;
        try {
            var first = Path.Combine(directory, "first.json"); var second = Path.Combine(directory, "second.json");
            await Snapshot(Builder()).SaveAsync(first, CancellationToken.None);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            await Snapshot(Builder()).SaveAsync(second, CancellationToken.None);
            Assert.Equal(await File.ReadAllBytesAsync(first), await File.ReadAllBytesAsync(second));
            await AnalysisSnapshot.Read(first).SaveAsync(second, CancellationToken.None);
            Assert.Equal(await File.ReadAllBytesAsync(first), await File.ReadAllBytesAsync(second));
            Assert.DoesNotContain('\r', await File.ReadAllTextAsync(first));
        }
        finally { CultureInfo.CurrentCulture = previous; Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public async Task GenericDispatchDiAndFrameworkAlternativesRoundTrip()
    {
        const string source = """
            public class P : Plugin {
                public P() => RegisterStep<Account, IStore>(EventOperation.Update, ExecutionStage.PostOperation, s => s.Run());
                protected override IServiceCollection OnBeforeBuildServiceProvider(IServiceCollection services) => services.AddScoped<IStore, Store>();
            }
            public interface IStore { void Run(); }
            public abstract class BaseStore<T> where T : Entity, new() {
                protected readonly IOrganizationService service;
                protected BaseStore(IOrganizationService service) { this.service = service; }
                public virtual void Run() { service.RetrieveMultiple(Query<T>()); }
                private QueryExpression Query<E>() where E : Entity, new() => new QueryExpression(new E().LogicalName);
            }
            [EntityLogicalName(Names.Table)]
            public class Record : Entity { public Record() : base(Names.Table) { } }
            public class Store : BaseStore<Record>, IStore {
                public Store(IOrganizationService service) : base(service) { }
                public override void Run() { base.Run(); service.Delete(Echo(Names.Table), Guid.Empty); }
                static string Echo(string table) => table;
            }
            """;
        var builder = new ManifestBuilder(Analyze.Root);
        builder.Add(Analyze.Compile(source + "static class Names { public const string Table = \"account\"; }"));
        builder.Add(Analyze.Compile(source + "static class Names { public const string Table = \"contact\"; }"));
        var snapshot = await RoundTrip(Snapshot(builder));
        var restored = ManifestBuilder.Restore(Analyze.Root, snapshot.Graph);
        var manifest = restored.Build();
        Assert.Equal(Json(builder.Build()), Json(manifest));
        var step = manifest.Entry("plugin:P:Update:account:PostOperation");
        Assert.Equal(["account:Read", "account:Delete", "contact:Read", "contact:Delete"], step.Requirements());
        Assert.Empty(step.Unresolved);
    }

    [Fact]
    public async Task ExternalPluginMetadataImpersonationWorkflowsAndMissingCodeRoundTrip()
    {
        var builder = new ManifestBuilder(Analyze.Root);
        builder.Add(Analyze.Compile("public class App { public void Run(IOrganizationService s) => s.Create(new Account()); }"));
        builder.AddPluginCode(Analyze.Compile("""
            public class External : IPlugin {
                public void Execute(IServiceProvider provider) {
                    var c = (IPluginExecutionContext)provider.GetService(typeof(IPluginExecutionContext));
                    var f = (IOrganizationServiceFactory)provider.GetService(typeof(IOrganizationServiceFactory));
                    f.CreateOrganizationService(c.UserId).Retrieve(c.PrimaryEntityName, Guid.Empty, new ColumnSet());
                    f.CreateOrganizationService(c.InitiatingUserId).Delete("contact", Guid.Empty);
                }
            }
            """, assemblyName: "External.Plugins"));
        builder.AddPluginSteps([
            new PluginStepRegistration("External", "External.Plugins", "Create", "account", 40, "01234567-1234-1234-1234-123456789abc"),
            new PluginStepRegistration("Missing", "Missing.Plugins", "Create", "account", 40, null)
        ]);
        builder.AddWorkflows([new WorkflowRegistration("Notify", "account", true, false, null, true, null)]);
        var snapshot = await RoundTrip(Snapshot(builder));
        var restored = ManifestBuilder.Restore(Analyze.Root, snapshot.Graph);
        Assert.Equal(Json(builder.Build()), Json(restored.Build()));
        // Metadata-only/external steps must remain available to new operations, not just reported entry points.
        builder.AddOperation("Create", "account", []); restored.AddOperation("Create", "account", []);
        var scope = EntryPointSelector.Operation("Create", "account");
        var manifest = restored.Build(scope: scope);
        Assert.Equal(Json(builder.Build(scope: scope)), Json(manifest));
        var entry = Assert.Single(manifest.EntryPoints);
        Assert.Contains(entry.Requirements, r => r.Table == "account" && r.RunsAs.Kind == RunsAsKind.User);
        Assert.Contains(entry.Unresolved, u => u.Reason.Contains("workflow"));
        Assert.Contains(entry.Unresolved, u => u.Reason.Contains("code isn't analyzed"));
    }

    [Fact]
    public async Task CaptureIsUnfilteredAndReplayWorksAfterDeletingTheProject()
    {
        var directory = Directory.CreateTempSubdirectory("permission-capture-").FullName;
        try {
            var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory, "source")).FullName;
            var project = Path.Combine(sourceDirectory, "App.csproj");
            var sdkAssembly = System.Security.SecurityElement.Escape(typeof(Microsoft.Xrm.Sdk.IPlugin).Assembly.Location);
            await File.WriteAllTextAsync(project, $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
                  <ItemGroup><Reference Include="Microsoft.Xrm.Sdk"><HintPath>{sdkAssembly}</HintPath></Reference></ItemGroup>
                </Project>
                """);
            await File.WriteAllTextAsync(Path.Combine(sourceDirectory, "App.cs"), """
                using System; using Microsoft.Xrm.Sdk; using Microsoft.Xrm.Sdk.Query;
                public class App {
                    public void Run(IOrganizationService service) { service.Retrieve("account", Guid.Empty, new ColumnSet()); }
                }
                """);
            using var restore = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet") {
                Arguments = $"restore \"{project}\" --ignore-failed-sources", RedirectStandardOutput = true,
                RedirectStandardError = true, UseShellExecute = false
            })!;
            var output = await restore.StandardOutput.ReadToEndAsync();
            var errors = await restore.StandardError.ReadToEndAsync();
            await restore.WaitForExitAsync(); Assert.True(restore.ExitCode == 0, output + errors);

            var snapshotPath = Path.Combine(directory, "snapshot.json"); var report = Path.Combine(directory, "report.json");
            var command = new RootCommand(); var options = new AnalysisOptions(); options.AddTo(command);
            async Task<int> Run(params string[] args) {
                var parsed = command.Parse(args); Assert.Empty(parsed.Errors);
                return await AnalysisRunner.RunAsync(options.Read(parsed), CancellationToken.None);
            }
            Assert.Equal(ExitCodes.Success, await Run("--solution", project, "--save-snapshot", snapshotPath,
                "--operation", "WhoAmI systemuser", "--table", "nothing", "--format", "json", "--output", report));
            var snapshot = AnalysisSnapshot.Read(snapshotPath);
            Assert.NotEmpty(snapshot.Inputs.Projects);
            Assert.Null(snapshot.Roles);
            Assert.Empty(snapshot.ColumnCatalog!);
            Assert.DoesNotContain(snapshot.Graph.Entries, e => e.Kind == EntryPointKind.Operation);
            using (var captureReport = JsonDocument.Parse(await File.ReadAllTextAsync(report))) {
                Assert.False(captureReport.RootElement.TryGetProperty("result", out _));
                Assert.False(captureReport.RootElement.TryGetProperty("tablesWithoutRoleMetadata", out _));
            }

            // Even stale configured roles must not prevent default snapshot capture. The downloaded
            // catalog remains in the snapshot for subsequent checks, not coverage in the capture report.
            var config = Path.Combine(directory, "appsettings.json");
            await File.WriteAllTextAsync(config, """
                { "XrmMockup": { "Permissions": { "Principals": { "App": { "Roles": ["Missing configured role"] } } } } }
                """);
            // Source + metadata + save-snapshot must only capture, even with stale configured roles.
            // Run in a child process to check stdout without changing global Console.Out in xUnit.
            var captureStart = new System.Diagnostics.ProcessStartInfo(
                Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet") {
                RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
                WorkingDirectory = directory
            };
            foreach (var argument in new[] { typeof(AnalysisRunner).Assembly.Location,
                "--solution", project, "--metadata", TestMetadata.DirectoryPath,
                "--config", config, "--save-snapshot", snapshotPath })
                captureStart.ArgumentList.Add(argument);
            using (var captureProcess = System.Diagnostics.Process.Start(captureStart)!) {
                var stdout = captureProcess.StandardOutput.ReadToEndAsync();
                var stderr = captureProcess.StandardError.ReadToEndAsync();
                await captureProcess.WaitForExitAsync();
                Assert.True(captureProcess.ExitCode == ExitCodes.Success, await stderr);
                Assert.Equal(string.Empty, await stdout);
                Assert.Contains("Wrote complete analysis snapshot", await stderr);
            }
            var captured = AnalysisSnapshot.Read(snapshotPath);
            Assert.NotEmpty(captured.Inputs.Projects);
            Assert.NotEmpty(captured.Graph.Nodes);
            Assert.NotEmpty(captured.RoleDefinitions()!);
            var accountColumns = captured.ColumnCatalog!.Single(t => t.Table == "account").Columns;
            Assert.Contains(accountColumns, c => c.LogicalName == "name");
            Assert.Contains(accountColumns, c => c.LogicalName == "ownerid");

            var capture = new[] { "--solution", project, "--metadata", TestMetadata.DirectoryPath,
                "--config", config, "--save-snapshot", snapshotPath, "--operation", "WhoAmI systemuser",
                "--format", "json", "--output", report };
            Assert.Equal(ExitCodes.Success, await Run(capture));
            Assert.NotEmpty(AnalysisSnapshot.Read(snapshotPath).RoleDefinitions()!);
            using (var captureReport = JsonDocument.Parse(await File.ReadAllTextAsync(report)))
                Assert.False(captureReport.RootElement.TryGetProperty("result", out _));
            Assert.Equal(ExitCodes.Error, await Run(capture.Concat(["--strict"]).ToArray()));
            var knownRole = XrmMockup.PermissionAnalyzer.Tool.Metadata.RoleLoader.Load(TestMetadata.DirectoryPath)[0].Name;
            Assert.Equal(ExitCodes.Success, await Run(capture.Concat(["--role", knownRole, "--strict"]).ToArray()));
            using (var checkedReport = JsonDocument.Parse(await File.ReadAllTextAsync(report)))
                Assert.True(checkedReport.RootElement.TryGetProperty("result", out _));

            Directory.Delete(sourceDirectory, recursive: true);
            Assert.Equal(ExitCodes.Success, await Run("--snapshot", snapshotPath, "--entry", "App.Run", "--no-roles",
                "--format", "json", "--output", report));
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(report));
            var entry = Assert.Single(document.RootElement.GetProperty("entryPoints").EnumerateArray());
            Assert.Equal("account", Assert.Single(entry.GetProperty("requirements").EnumerateArray()).GetProperty("table").GetString());
            // A fresh role catalog is still usable, without reading its plugin/lookup/privilege metadata.
            var role = XrmMockup.PermissionAnalyzer.Tool.Metadata.RoleLoader.Load(TestMetadata.DirectoryPath)[0].Name;
            Assert.Equal(ExitCodes.Success, await Run("--snapshot", snapshotPath, "--operation", "WhoAmI systemuser",
                "--metadata", TestMetadata.DirectoryPath, "--role", role, "--strict", "--output", report));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public async Task ResolutionLimitsAreReevaluatedAndCannotBecomeCompleteAfterReplay()
    {
        var helpers = string.Join("\n", Enumerable.Range(0, 24).Select(i =>
            $"static IOrganizationService S{i}(IOrganizationService s) => " + (i == 23 ? "s;" : $"S{i + 1}(s);")));
        var builder = new ManifestBuilder(Analyze.Root);
        builder.Add(Analyze.Compile($$"""
            public class P : Plugin {
                public P() => RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation,
                    c => S0(c.OrganizationService).Delete("account", Guid.Empty));
                {{helpers}}
            }
            """));
        var snapshot = await RoundTrip(Snapshot(builder));
        var restored = ManifestBuilder.Restore(Analyze.Root, snapshot.Graph);
        Assert.Equal(Json(builder.Build()), Json(restored.Build()));
        Assert.True(Assert.Single(restored.Build().EntryPoints).Truncated);
        Assert.True(restored.Build().Incomplete);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("json")]
    [InlineData("markdown")]
    [InlineData("sarif")]
    public async Task ReplaySupportsEveryReportFormat(string format)
    {
        var directory = Directory.CreateTempSubdirectory("permission-snapshot-report-").FullName;
        try {
            var path = Path.Combine(directory, "snapshot.json"); var report = Path.Combine(directory, "report");
            await Snapshot(Builder()).SaveAsync(path, CancellationToken.None);
            var command = new RootCommand(); var options = new AnalysisOptions(); options.AddTo(command);
            var parsed = command.Parse(["--snapshot", path, "--format", format, "--output", report,
                "--operation", "Update account", "--attributes", "name", "--role", "Reader", "--detail", "full"]);
            Assert.Empty(parsed.Errors);
            Assert.Equal(ExitCodes.Success, await AnalysisRunner.RunAsync(options.Read(parsed), CancellationToken.None));
            var content = await File.ReadAllTextAsync(report);
            Assert.NotEmpty(content);
            Assert.DoesNotContain('\r', content);
            if (format == "sarif") {
                using var document = JsonDocument.Parse(content);
                Assert.Contains("src/App/", content);
                Assert.Equal("2.1.0", document.RootElement.GetProperty("version").GetString());
            }
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public async Task RejectsNullGraphFactsWithoutDiscardingDiagnostics()
    {
        var directory = Directory.CreateTempSubdirectory("permission-snapshot-invalid-").FullName;
        try {
            var path = Path.Combine(directory, "snapshot.json");
            await Snapshot(Builder()).SaveAsync(path, CancellationToken.None);
            var node = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(path))!;
            node["graph"]!["nodes"]!.AsArray().Add((System.Text.Json.Nodes.JsonNode?)null);
            await File.WriteAllTextAsync(path, node.ToJsonString());
            Assert.Contains("null facts", Assert.Throws<ToolException>(() => AnalysisSnapshot.Read(path)).Message);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Theory]
    [InlineData("{}", "Not a permission analysis snapshot")]
    [InlineData("{\"kind\":\"xrmmockup-permissions-snapshot\",\"schemaVersion\":999}", "Unsupported")]
    [InlineData("{\"kind\":\"xrmmockup-permissions-snapshot\",\"schemaVersion\":1}", "Cannot read")]
    [InlineData("not json", "Cannot read")]
    public void RejectsReportsMalformedFilesAndUnsupportedSchemas(string content, string error)
    {
        var path = Path.GetTempFileName();
        try { File.WriteAllText(path, content); Assert.Contains(error, Assert.Throws<ToolException>(() => AnalysisSnapshot.Read(path)).Message); }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("--solution App.csproj --metadata Metadata", true)]
    [InlineData("--config appsettings.json --exclude *.Tests --plugin-solution Plugins.csproj", true)]
    [InlineData("--no-roles", true)]
    [InlineData("--explore", false)]
    [InlineData("--format text", false)]
    [InlineData("--format json", false)]
    [InlineData("--output report.json", false)]
    [InlineData("--detail normal", false)]
    [InlineData("--table account", false)]
    [InlineData("--privilege Write", false)]
    [InlineData("--runs-as Caller", false)]
    [InlineData("--plugin P", false)]
    [InlineData("--custom-api ctx_Run", false)]
    [InlineData("--function Sync", false)]
    [InlineData("--entry App.Run", false)]
    [InlineData("--operation \"Update account\"", false)]
    [InlineData("--role Reader", false)]
    [InlineData("--caller-role Reader", false)]
    [InlineData("--user someone", false)]
    [InlineData("--caller-user someone", false)]
    [InlineData("--strict", false)]
    [InlineData("--fail-on-missing", false)]
    public void SaveSnapshotOnlyCapturesUnlessAQueryReportOrExplorerIsRequested(string extra, bool captureOnly)
    {
        var command = new RootCommand(); var options = new AnalysisOptions(); options.AddTo(command);
        var parsed = command.Parse("--save-snapshot snapshot.json " + extra);
        Assert.Empty(parsed.Errors);
        Assert.Equal(captureOnly, options.Read(parsed).CaptureOnly);
        Assert.False(options.Read(command.Parse("--snapshot snapshot.json")).CaptureOnly);
        Assert.False(options.Read(command.Parse("")).CaptureOnly);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("--no-roles", true)]
    [InlineData("--role Reader", false)]
    [InlineData("--caller-role Reader", false)]
    [InlineData("--user someone", false)]
    [InlineData("--caller-user someone", false)]
    [InlineData("--strict", false)]
    [InlineData("--fail-on-missing", false)]
    public void CaptureDefaultsToRequirementsOnlyUnlessCoverageIsExplicit(string extra, bool requirementsOnly)
    {
        var command = new RootCommand(); var options = new AnalysisOptions(); options.AddTo(command);
        var parsed = command.Parse("--save-snapshot snapshot.json " + extra);
        Assert.Empty(parsed.Errors);
        Assert.Equal(requirementsOnly, options.Read(parsed).RequirementsOnly);
        Assert.False(options.Read(command.Parse("--snapshot snapshot.json")).RequirementsOnly);
        Assert.False(options.Read(command.Parse("")).RequirementsOnly);
    }

    [Theory]
    [InlineData("--solution", "App.csproj")]
    [InlineData("--exclude", "*.Tests")]
    [InlineData("--plugin-solution", "Plugins.csproj")]
    [InlineData("--save-snapshot", "another.json")]
    [InlineData("--output", "snapshot.json")]
    public void RejectsConflictingSnapshotOptions(string option, string value)
    {
        var command = new RootCommand(); new AnalysisOptions().AddTo(command);
        Assert.NotEmpty(command.Parse(["--snapshot", "snapshot.json", option, value]).Errors);
    }

    [Fact]
    public async Task CliQueriesSnapshotWithoutSourceOrMetadataAndUsesEmbeddedRoles()
    {
        var directory = Directory.CreateTempSubdirectory("permission-snapshot-cli-").FullName;
        try {
            var snapshot = Path.Combine(directory, "snapshot.json"); var report = Path.Combine(directory, "report.json");
            await Snapshot(Builder()).SaveAsync(snapshot, CancellationToken.None);
            var command = new RootCommand(); var options = new AnalysisOptions(); options.AddTo(command);
            async Task<int> Run(params string[] query) {
                var parsed = command.Parse(new[] { "--snapshot", snapshot, "--format", "json", "--output", report }.Concat(query).ToArray());
                Assert.Empty(parsed.Errors);
                return await AnalysisRunner.RunAsync(options.Read(parsed), CancellationToken.None);
            }
            Assert.Equal(ExitCodes.MissingPrivileges, await Run("--operation", "Update account", "--attributes", "name", "--role", "Reader", "--strict", "--table", "nothing"));
            Assert.Equal(ExitCodes.Success, await Run("--operation", "WhoAmI systemuser", "--role", "Reader", "--strict"));
            Assert.Equal(ExitCodes.Success, await Run("--function", "Sync", "--no-roles"));
            Assert.Equal(ExitCodes.Success, await Run("--custom-api", "ctx_Run", "--no-roles"));
            Assert.Equal(ExitCodes.Success, await Run("--plugin", "P:Update", "--no-roles"));
            Assert.Equal(ExitCodes.Success, await Run("--entry", "App.Other", "--no-roles"));
            Assert.Equal(ExitCodes.Error, await Run("--function", "NotThere", "--no-roles"));
            Assert.Equal(ExitCodes.Error, await Run("--role", "NotThere"));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
