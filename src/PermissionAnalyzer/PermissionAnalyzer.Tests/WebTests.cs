using System.CommandLine;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool;
using XrmMockup.PermissionAnalyzer.Tool.Commands;
using XrmMockup.PermissionAnalyzer.Tool.Options;
using XrmMockup.PermissionAnalyzer.Tool.Output;
using XrmMockup.PermissionAnalyzer.Tool.Snapshots;
using XrmMockup.PermissionAnalyzer.Tool.Web;

namespace XrmMockup.PermissionAnalyzer.Tests;

public class WebTests
{
    private const string Source = """
        public class P : Plugin {
            public P() {
                RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation,
                    c => c.OrganizationService.Create(new Contact())).AddFilteredAttributes("name");
                RegisterCustomAPI("ctx_Run", c => c.OrganizationService.Create(new Contact()));
            }
        }
        public class C : Plugin {
            public C() { RegisterPluginStep<Contact>(EventOperation.Create, ExecutionStage.PostOperation,
                c => c.OrganizationService.Delete("lead", Guid.Empty)); }
        }
        public class App {
            [Microsoft.Azure.Functions.Worker.Function("Sync")]
            public void Run(IOrganizationService service) { service.Retrieve("account", Guid.Empty, new ColumnSet()); }
            public void Other(IOrganizationService service, string table) { service.Retrieve(table, Guid.Empty, new ColumnSet()); }
            [Microsoft.Azure.Functions.Worker.Function("ActForUser")]
            public void Impersonate(Guid userId) {
                var client = new Microsoft.PowerPlatform.Dataverse.Client.ServiceClient("AuthType=ClientSecret");
                client.CallerId = userId;
                client.Delete("lead", Guid.Empty);
            }
        }
        """;

    private static readonly RoleDefinition Reader = new("Reader",
        new Dictionary<string, IReadOnlyDictionary<Privilege, PrivilegeDepth>> {
            ["account"] = new Dictionary<Privilege, PrivilegeDepth> { [Privilege.Read] = PrivilegeDepth.User, [Privilege.Write] = PrivilegeDepth.User },
            ["contact"] = new Dictionary<Privilege, PrivilegeDepth>(),
            ["lead"] = new Dictionary<Privilege, PrivilegeDepth>()
        });
    private static readonly RoleDefinition Writer = new("Writer",
        new Dictionary<string, IReadOnlyDictionary<Privilege, PrivilegeDepth>> {
            ["account"] = new Dictionary<Privilege, PrivilegeDepth> { [Privilege.Read] = PrivilegeDepth.Organization },
            ["contact"] = new Dictionary<Privilege, PrivilegeDepth> { [Privilege.Create] = PrivilegeDepth.Organization },
            ["lead"] = new Dictionary<Privilege, PrivilegeDepth> { [Privilege.Delete] = PrivilegeDepth.Organization }
        }, new Dictionary<string, PrivilegeDepth> { [GlobalRequirement.ActOnBehalfOfAnotherUser] = PrivilegeDepth.Organization });

    private static ManifestBuilder Builder()
    {
        var builder = new ManifestBuilder(Analyze.Root);
        Assert.True(builder.Add(Analyze.Compile(Source)));
        return builder;
    }

    private static SecurityModel Model(PermissionsConfiguration? config = null, CoverageOptions? defaults = null,
        bool requirementsOnly = false) => new(Analyze.Root, Builder().Extracted(), [], [Writer, Reader],
            config ?? new PermissionsConfiguration(), defaults, null, requirementsOnly);

    private static IEnumerable<JsonElement> Requirements(JsonElement report) => report.GetProperty("entryPoints").EnumerateArray()
        .SelectMany(e => e.GetProperty("requirements").EnumerateArray());

    [Theory]
    [InlineData("--explore --port 9000 --snapshot model.json", true)]
    [InlineData("--explore --solution App.csproj --metadata Metadata --role Reader --save-snapshot model.json", true)]
    [InlineData("--explore --no-roles", true)]
    [InlineData("--web", false)]
    [InlineData("--port 9000", false)]
    [InlineData("--explore --port 0", false)]
    [InlineData("--explore --port 65536", false)]
    [InlineData("--explore --output report.json", false)]
    [InlineData("--explore --format json", false)]
    [InlineData("--explore --table account", false)]
    [InlineData("--explore --operation \"Update account\"", false)]
    [InlineData("--explore --strict", false)]
    public void ExploreOptionsAreValidated(string arguments, bool valid)
    {
        var options = new AnalysisOptions();
        var root = new RootCommand(); options.AddTo(root);
        var parsed = root.Parse(arguments);
        Assert.Equal(valid, parsed.Errors.Count == 0);
        if (valid) { var request = options.Read(parsed); Assert.True(request.Web); Assert.InRange(request.Port, 1, 65535); }
    }

    [Theory]
    [InlineData("--port 9000", "--port requires --explore.")]
    [InlineData("--explore --output report.json", "--explore can't be used with --output; choose query options in the UI.")]
    public void InvalidExplorerOptionsReportExpectedErrors(string arguments, string error)
    {
        var options = new AnalysisOptions();
        var root = new RootCommand(); options.AddTo(root);
        var parsed = root.Parse(arguments);
        Assert.Contains(parsed.Errors, e => e.Message == error);
    }

    [Fact]
    public void CatalogIncludesSortedRolesKnownGlobalDepthAndMethodSuggestions()
    {
        var catalog = JsonSerializer.SerializeToElement(Model().Catalog(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var roles = catalog.GetProperty("roles").EnumerateArray().ToList();
        Assert.Equal(new[] { "Reader", "Writer" }, roles.Select(r => r.GetProperty("name").GetString()));
        Assert.False(roles[0].GetProperty("globalPrivilegesKnown").GetBoolean());
        Assert.True(roles[1].GetProperty("globalPrivilegesKnown").GetBoolean());
        Assert.Contains(roles[1].GetProperty("grants").EnumerateArray(), g => g.GetProperty("table").ValueKind == JsonValueKind.Null
            && g.GetProperty("privilege").GetString() == GlobalRequirement.ActOnBehalfOfAnotherUser);
        Assert.Contains(catalog.GetProperty("methods").EnumerateArray(), m => m.GetString()!.Contains("App.Other", StringComparison.Ordinal));
    }

    [Fact]
    public void RepeatedQueriesAreIsolatedAndOperationAttributesControlCascades()
    {
        var model = Model();
        var query = new SecurityQuery("operation", "account", "Update", ["name"], ["Reader"]);
        var original = model.Query(query);
        Assert.Contains(Requirements(original), r => r.TryGetProperty("table", out var t) && t.GetString() == "lead" && r.GetProperty("coverage").GetProperty("status").GetString() == "missing");
        var combined = model.Query(query with { Roles = ["Reader", "Writer"] });
        Assert.DoesNotContain(Requirements(combined), r => r.GetProperty("coverage").GetProperty("status").GetString() == "missing");
        var excluded = model.Query(query with { Attributes = ["description"] });
        Assert.DoesNotContain(Requirements(excluded), r => r.GetProperty("table").GetString() == "contact");
        var unknownColumns = model.Query(query with { Attributes = [] });
        Assert.Contains(Requirements(unknownColumns), r => r.GetProperty("table").GetString() == "contact");
        model.Query(new SecurityQuery("method", "App.Other", NoRoles: true));
        Assert.Equal(original.GetRawText(), model.Query(query).GetRawText());
    }

    [Theory]
    [InlineData("plugin", "P")]
    [InlineData("custom-api", "ctx_Run")]
    [InlineData("function", "Sync")]
    [InlineData("method", "App.Run")]
    public void QueriesSupportExistingCliScopes(string scope, string target)
    {
        var report = Model().Query(new SecurityQuery(scope, target, Roles: ["Reader"]));
        Assert.Single(report.GetProperty("entryPoints").EnumerateArray());
    }

    [Fact]
    public void RolesCombineAtMaximumDepthAndFiltersPreserveWholeAnalysisCounts()
    {
        var model = Model();
        var query = new SecurityQuery("function", "Sync", Roles: ["Reader", "Writer"]);
        var report = model.Query(query);
        Assert.Equal("Organization", Assert.Single(Requirements(report)).GetProperty("coverage").GetProperty("depth").GetString());
        var filtered = model.Query(query with { Tables = ["contact"] });
        Assert.Empty(Requirements(filtered));
        Assert.Equal(report.GetProperty("result").GetRawText(), filtered.GetProperty("result").GetRawText());
    }

    [Fact]
    public void ConfiguredPrincipalsAndStartupRolesRemainAvailable()
    {
        var config = new PermissionsConfiguration { Principals = new() { ["Test/Sync"] = new() { Roles = ["Reader"] } } };
        var configured = Model(config).Query(new SecurityQuery("function", "Sync"));
        Assert.Equal("User", Assert.Single(Requirements(configured)).GetProperty("coverage").GetProperty("depth").GetString());
        var defaults = new CoverageOptions { Roles = RoleSet.Combine([Writer], "--role") };
        var startup = Model(defaults: defaults).Query(new SecurityQuery("function", "Sync"));
        Assert.Equal("Organization", Assert.Single(Requirements(startup)).GetProperty("coverage").GetProperty("depth").GetString());
    }

    [Fact]
    public void ImpersonatedCallerRolesAreNotMergedWithApplicationRoles()
    {
        var model = Model();
        var report = model.Query(new SecurityQuery("function", "ActForUser", Roles: ["Writer"], CallerRoles: ["Reader"]));
        var requirement = Requirements(report).Single(r => r.TryGetProperty("table", out var t) && t.GetString() == "lead" && r.GetProperty("runsAs").GetString() == "ImpersonatedCaller");
        Assert.Equal("missing", requirement.GetProperty("coverage").GetProperty("status").GetString());
        var granted = model.Query(new SecurityQuery("function", "ActForUser", Roles: ["Writer"], CallerRoles: ["Writer"]));
        Assert.Equal("granted", Requirements(granted).Single(r => r.TryGetProperty("table", out var t) && t.GetString() == "lead" && r.GetProperty("runsAs").GetString() == "ImpersonatedCaller").GetProperty("coverage").GetProperty("status").GetString());
    }

    [Fact]
    public async Task SnapshotReplayNeedsNoSourceAndRetainsDiagnostics()
    {
        var diagnostic = new UnresolvedOperation("Build", [], "Incomplete input", RunsAs.Caller,
            new RequirementSource(new SourceLocation("Project.csproj", 1, 1), ["Build"]));
        var snapshot = AnalysisSnapshot.Create(Builder(), new AnalysisProvenance("Missing.csproj", [], [], "hash", []),
            [diagnostic], [Reader, Writer], "test", "missing-source");
        var directory = Directory.CreateTempSubdirectory("permissions-web-");
        try
        {
            var path = Path.Combine(directory.FullName, "snapshot.json");
            await snapshot.SaveAsync(path, CancellationToken.None);
            var loaded = AnalysisSnapshot.Read(path);
            var model = new SecurityModel("/does-not-exist", loaded.Graph, loaded.Diagnostics, loaded.RoleDefinitions(),
                new PermissionsConfiguration(), null, loaded.Inputs);
            var report = model.Query(new SecurityQuery("function", "Sync", Roles: ["Reader"]));
            Assert.True(report.GetProperty("analysis").GetProperty("incomplete").GetBoolean());
        }
        finally { directory.Delete(recursive: true); }
    }

    [Fact]
    public void DefaultLiveTeamRolesRetainDirectDelegationBoundaries()
    {
        var direct = new RoleDefinition("Direct", Reader.Privileges, new Dictionary<string, PrivilegeDepth>());
        var live = RoleSet.Combine([direct, Writer], "Dataverse", [direct]);
        var defaults = new CoverageOptions { Roles = live, CallerRoles = RoleSet.Combine([Writer], "--caller-role") };
        var model = new SecurityModel(Analyze.Root, Builder().Extracted(), [], [direct, Writer],
            new PermissionsConfiguration(), defaults, null);
        var report = model.Query(new SecurityQuery("function", "ActForUser"));
        var delegation = Requirements(report).Single(r => r.GetProperty("privilege").GetString() == GlobalRequirement.ActOnBehalfOfAnotherUser);
        Assert.Equal("missing", delegation.GetProperty("coverage").GetProperty("status").GetString());
        var explicitRoles = model.Query(new SecurityQuery("function", "ActForUser", Roles: ["Writer"]));
        Assert.Equal("granted", Requirements(explicitRoles).Single(r => r.GetProperty("privilege").GetString() == GlobalRequirement.ActOnBehalfOfAnotherUser)
            .GetProperty("coverage").GetProperty("status").GetString());
    }

    [Fact]
    public void InvalidQueriesAndUnknownRolesFailClearly()
    {
        var model = Model();
        Assert.Contains("Unknown security role", Assert.Throws<ToolException>(() => model.Query(new SecurityQuery(Roles: ["NotARealRole"]))).Message);
        Assert.Throws<ToolException>(() => model.Query(new SecurityQuery("plugin", "NotAPlugin")));
        Assert.Throws<ToolException>(() => model.Query(new SecurityQuery("operation")));
        Assert.Throws<ToolException>(() => model.Query(new SecurityQuery("unknown")));
        Assert.Throws<ToolException>(() => model.Query(new SecurityQuery(Roles: ["Reader"], NoRoles: true)));
        Assert.Throws<ToolException>(() => Model(requirementsOnly: true).Query(new SecurityQuery(Roles: ["Reader"])));
        Assert.Throws<ToolException>(() => model.Query(new SecurityQuery(Tables: [null!])));
        Assert.False(model.Query(new SecurityQuery(NoRoles: true)).TryGetProperty("result", out _));
    }

    [Fact]
    public async Task BrowserLaunchHappensOnceAfterServerIsReadyAndUsesTheSelectedPort()
    {
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start(); var port = ((IPEndPoint)reservation.LocalEndpoint).Port; reservation.Stop();
        using var cancellation = new CancellationTokenSource();
        var opened = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var server = SecurityWebHost.RunAsync(Model(), port, NullLogger.Instance, cancellation.Token,
            address => { calls++; opened.TrySetResult(address); });
        try {
            var address = await opened.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal($"http://127.0.0.1:{port}/", address);
            using var client = new HttpClient();
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(address)).StatusCode);
            Assert.Equal(1, calls);
        }
        finally { cancellation.Cancel(); await server.WaitAsync(TimeSpan.FromSeconds(10)); }
    }

    [Fact]
    public async Task LocalServerServesEmbeddedUiProtectsApiAndStopsCleanly()
    {
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start(); var port = ((IPEndPoint)reservation.LocalEndpoint).Port; reservation.Stop();
        using var cancellation = new CancellationTokenSource();
        var server = SecurityWebHost.RunAsync(Model(), port, NullLogger.Instance, cancellation.Token);
        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}/"), Timeout = TimeSpan.FromSeconds(10) };
        try
        {
            var page = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            Assert.Contains("id=\"root\"", await page.Content.ReadAsStringAsync());
            Assert.Contains("/app.js", await page.Content.ReadAsStringAsync());
            Assert.Contains("frame-ancestors 'none'", Assert.Single(page.Headers.GetValues("Content-Security-Policy")));
            Assert.Contains("no-store", page.Headers.CacheControl!.ToString());
            Assert.False(page.Headers.Contains("Access-Control-Allow-Origin"));
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("app.js")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("styles.css")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("unknown-file")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/model")).StatusCode);
            var session = await client.GetFromJsonAsync<JsonElement>("api/session");
            client.DefaultRequestHeaders.Add("X-XrmMockup-Token", session.GetProperty("token").GetString());
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/model")).StatusCode);
            var report = await client.PostAsJsonAsync("api/query", new { scope = "function", target = "Sync", roles = new[] { "Reader" } });
            Assert.Equal(HttpStatusCode.OK, report.StatusCode);
            Assert.Single(Requirements((await report.Content.ReadFromJsonAsync<JsonElement>())));
            Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.GetAsync("api/query")).StatusCode);
            foreach (var json in new[] { "{", "null", "{\"scope\":null}", "{\"unexpected\":true}", "{\"roles\":[null]}" })
                Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("api/query", new StringContent(json, Encoding.UTF8, "application/json"))).StatusCode);
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, (await client.PostAsync("api/query", new StringContent("{}"))).StatusCode);
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await client.PostAsync("api/query", new StringContent(new string(' ', 65537), Encoding.UTF8, "application/json"))).StatusCode);
            using var foreignOrigin = new HttpRequestMessage(HttpMethod.Get, "api/session");
            foreignOrigin.Headers.Add("Origin", "https://example.com");
            Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(foreignOrigin)).StatusCode);
            using var crossSite = new HttpRequestMessage(HttpMethod.Get, "api/session");
            crossSite.Headers.Add("Sec-Fetch-Site", "cross-site");
            Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(crossSite)).StatusCode);
            using var rebind = new HttpRequestMessage(HttpMethod.Get, "api/session"); rebind.Headers.Host = $"attacker.example:{port}";
            Assert.False((await client.SendAsync(rebind)).IsSuccessStatusCode);
            // Invalid requests do not end the session.
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/model")).StatusCode);
        }
        finally { cancellation.Cancel(); await server.WaitAsync(TimeSpan.FromSeconds(10)); }
        // The same port can be used again after shutdown.
        using var next = new CancellationTokenSource();
        var restarted = SecurityWebHost.RunAsync(Model(), port, NullLogger.Instance, next.Token);
        next.Cancel(); await restarted.WaitAsync(TimeSpan.FromSeconds(10));
    }
}
