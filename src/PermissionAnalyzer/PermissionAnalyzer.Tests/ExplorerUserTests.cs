using System.CommandLine;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using WhoAmIRequest = Microsoft.Crm.Sdk.Messages.WhoAmIRequest;
using WhoAmIResponse = Microsoft.Crm.Sdk.Messages.WhoAmIResponse;
using XrmMockup.PermissionAnalyzer.Core.Analysis;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool;
using XrmMockup.PermissionAnalyzer.Tool.Commands;
using XrmMockup.PermissionAnalyzer.Tool.Dataverse;
using XrmMockup.PermissionAnalyzer.Tool.Options;
using XrmMockup.PermissionAnalyzer.Tool.Web;
using XrmMockup.PermissionAnalyzer.Tool.Snapshots;
using XrmMockup.PermissionAnalyzer.Tool.Output;

namespace XrmMockup.PermissionAnalyzer.Tests;

public class ExplorerUserTests
{
    private static readonly Guid Alice = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid Bob = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid ReaderId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid WriterId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid TeamId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private const string Url = "https://example.crm.dynamics.com";
    private static readonly RoleDefinition Reader = new("Reader", new Dictionary<string, IReadOnlyDictionary<Privilege, PrivilegeDepth>> {
        ["account"] = new Dictionary<Privilege, PrivilegeDepth> { [Privilege.Read] = PrivilegeDepth.User }
    }, new Dictionary<string, PrivilegeDepth>(), ReaderId);
    private static readonly RoleDefinition Writer = new("Writer", new Dictionary<string, IReadOnlyDictionary<Privilege, PrivilegeDepth>> {
        ["account"] = new Dictionary<Privilege, PrivilegeDepth> { [Privilege.Write] = PrivilegeDepth.Organization }
    }, new Dictionary<string, PrivilegeDepth> { [GlobalRequirement.ActOnBehalfOfAnotherUser] = PrivilegeDepth.Organization }, WriterId);
    private static Entity User(Guid id, bool disabled = false) => new("systemuser", id) {
        ["fullname"] = id == Alice ? "Alice Example" : "Bob Example", ["domainname"] = id == Alice ? "alice@example.com" : "bob@example.com",
        ["internalemailaddress"] = id == Alice ? "alice@example.com" : "bob@example.com", ["isdisabled"] = disabled
    };
    private static Entity Role(Guid id, bool team = false)
    {
        var entity = new Entity("role", id) { ["name"] = id == ReaderId ? "Reader" : "Writer" };
        if (team) { entity["team.name"] = new AliasedValue("team", "name", "Operations"); entity["team.teamid"] = new AliasedValue("team", "teamid", TeamId); }
        return entity;
    }
    private static EntityCollection Page(params Entity[] entities) => new(entities.ToList());
    private sealed class Lifetime : IDisposable { public bool Disposed; public void Dispose() => Disposed = true; }
    private sealed class Service(Func<QueryExpression, EntityCollection> read, Func<OrganizationRequest, OrganizationResponse>? execute = null,
        Func<string, Guid, ColumnSet, Entity>? retrieve = null) : IOrganizationService
    {
        public readonly List<QueryExpression> Queries = [];
        public readonly List<OrganizationRequest> Requests = [];
        public EntityCollection RetrieveMultiple(QueryBase query) { Queries.Add((QueryExpression)query); return read((QueryExpression)query); }
        public Guid Create(Entity entity) => throw new NotSupportedException();
        public void Update(Entity entity) => throw new NotSupportedException();
        public void Delete(string name, Guid id) => throw new NotSupportedException();
        public Entity Retrieve(string name, Guid id, ColumnSet columns) => retrieve?.Invoke(name, id, columns) ?? User(id);
        public OrganizationResponse Execute(OrganizationRequest request) {
            Requests.Add(request);
            if (execute is not null) return execute(request);
            Assert.IsType<WhoAmIRequest>(request);
            return new WhoAmIResponse { Results = new ParameterCollection { ["UserId"] = Bob } };
        }
        public void Associate(string name, Guid id, Relationship relationship, EntityReferenceCollection references) => throw new NotSupportedException();
        public void Disassociate(string name, Guid id, Relationship relationship, EntityReferenceCollection references) => throw new NotSupportedException();
    }
    private static Service DefaultService() => new(query => {
        if (query.EntityName == "systemuser") {
            var id = query.Criteria.Conditions.FirstOrDefault(c => c.AttributeName == "systemuserid")?.Values[0];
            return id is Guid userId ? Page(User(userId)) : Page(User(Alice), User(Bob));
        }
        return query.LinkEntities[0].LinkToEntityName == "systemuserroles" ? Page(Role(ReaderId)) : Page(Role(WriterId, true));
    });
    private static ExplorerUsers Users(Service service, Lifetime lifetime, IReadOnlyList<RoleDefinition>? roles = null) =>
        new(roles ?? [Reader, Writer], null, NullLoggerFactory.Instance, _ => new(service, lifetime));
    private static ManifestBuilder Builder()
    {
        var builder = new ManifestBuilder(Analyze.Root);
        Assert.True(builder.Add(Analyze.Compile("""
          public class App {
            [Microsoft.Azure.Functions.Worker.Function("Read")]
            public void Read(IOrganizationService service) { service.Retrieve("account", Guid.Empty, new ColumnSet()); }
            [Microsoft.Azure.Functions.Worker.Function("ActForUser")]
            public void Run(Guid id) {
              var service = new Microsoft.PowerPlatform.Dataverse.Client.ServiceClient("AuthType=ClientSecret");
              service.CallerId = id; service.Update(new Account());
            }
          }
          """)));
        return builder;
    }

    private static SecurityModel Model(ExplorerUsers users, bool requirementsOnly = false) =>
        new(Analyze.Root, Builder().Extracted(), [], [Reader, Writer], new PermissionsConfiguration(), null, null, requirementsOnly, users: users);

    [Fact]
    public async Task OptInSearchIsBoundedAndSelectionKeepsLiveDirectAndTeamAssignments()
    {
        var service = DefaultService(); var lifetime = new Lifetime();
        using var users = Users(service, lifetime);
        await Assert.ThrowsAsync<ToolException>(() => users.SearchAsync("Alice", CancellationToken.None));
        Assert.Empty(service.Queries);
        var connection = await users.ConnectAsync(new(Url), CancellationToken.None);
        Assert.Equal(Bob, connection.ConnectedUser!.Id);
        Assert.Equal("Bob Example", connection.ConnectedUser.Name);
        Assert.IsType<WhoAmIRequest>(Assert.Single(service.Requests));
        var matches = await users.SearchAsync("Example", CancellationToken.None);
        Assert.Equal(2, matches.Count);
        var search = Assert.Single(service.Queries);
        Assert.Equal(50, search.TopCount); Assert.Equal("fullname", search.Orders[0].AttributeName);
        Assert.Equal(LogicalOperator.Or, Assert.Single(search.Criteria.Filters).FilterOperator);
        Assert.Equal(3, search.Criteria.Filters[0].Conditions.Count);
        var selected = await users.ResolveAsync(new(Alice), CancellationToken.None);
        Assert.Equal(["Reader", "Writer"], selected.Roles); Assert.Equal(2, selected.Assignments.Count);
        Assert.Equal("Operations", selected.Assignments[1].TeamName); Assert.Equal(Alice, selected.User.Id);
        var roles = users.Selection(selected.SelectionId, false);
        Assert.Equal(PrivilegeDepth.Organization, roles.Depth("account", Privilege.Write));
        Assert.Null(roles.DirectGlobalDepth(GlobalRequirement.ActOnBehalfOfAnotherUser));
        Assert.False(roles.LiveSource!.IsCaller);
        Assert.Throws<ToolException>(() => users.Selection(selected.SelectionId, true));
        users.Disconnect(); Assert.True(lifetime.Disposed); Assert.Null(users.Status.ConnectedUser);
        Assert.Throws<ToolException>(() => users.Selection(selected.SelectionId, false));
    }

    [Theory]
    [InlineData("http://example.crm.dynamics.com")]
    [InlineData("https://127.0.0.1")]
    [InlineData("https://example.com")]
    [InlineData("https://example.crm.dynamics.com:8443")]
    [InlineData("https://example.crm.dynamics.com/path")]
    [InlineData("https://name:secret@example.crm.dynamics.com")]
    [InlineData("https://example.crm.dynamics.com?token=secret")]
    [InlineData("https://example.crm.dynamics.com.attacker.test")]
    public void RejectsUnsafeConnectionTargets(string url) => Assert.Throws<ToolException>(() => ExplorerUsers.ValidateConnection(new(url)));

    [Fact]
    public async Task WhoAmIFailureDisposesConnectionAndAllowsRetryWithoutAnAssumedIdentity()
    {
        var attempts = 0; var firstLifetime = new Lifetime(); var secondLifetime = new Lifetime();
        using var users = new ExplorerUsers([Reader, Writer], null, NullLoggerFactory.Instance, _ => {
            attempts++;
            return attempts == 1 ? ConnectedFailure() : new(DefaultService(), secondLifetime);
            ExplorerUsers.ConnectedService ConnectedFailure() => new(new Service(_ => Page(),
                _ => throw new InvalidOperationException("secret from WhoAmI")), firstLifetime);
        });
        var error = await Assert.ThrowsAsync<ToolException>(() => users.ConnectAsync(new(Url), CancellationToken.None));
        Assert.DoesNotContain("secret from WhoAmI", error.Message);
        Assert.True(firstLifetime.Disposed); Assert.False(users.Status.Connected);
        Assert.Null(users.Status.ConnectedUser); Assert.Null(users.Status.EnvironmentUrl);
        var connected = await users.ConnectAsync(new(Url), CancellationToken.None);
        Assert.Equal(Bob, connected.ConnectedUser!.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidIdentityOrUnreadableProfileDoesNotPublishConnectedState(bool profileFailure)
    {
        var lifetime = new Lifetime();
        var service = new Service(_ => Page(),
            _ => new WhoAmIResponse { Results = new ParameterCollection { ["UserId"] = profileFailure ? Bob : Guid.Empty } },
            (_, _, _) => throw new InvalidOperationException("Profile unavailable"));
        using var users = Users(service, lifetime);
        await Assert.ThrowsAsync<ToolException>(() => users.ConnectAsync(new(Url), CancellationToken.None));
        Assert.True(lifetime.Disposed); Assert.Null(users.Status.ConnectedUser); Assert.False(users.Status.Connected);
    }

    [Fact]
    public async Task MalformedSearchAndFailedAuthenticationDoNotLeaveAConnection()
    {
        var attempts = 0;
        using var failed = new ExplorerUsers([Reader], null, NullLoggerFactory.Instance, _ => { attempts++; throw new InvalidOperationException("secret-token"); });
        var error = await Assert.ThrowsAsync<ToolException>(() => failed.ConnectAsync(new(Url), CancellationToken.None));
        Assert.DoesNotContain("secret-token", error.Message); Assert.False(failed.Status.Connected);
        await Assert.ThrowsAsync<ToolException>(() => failed.ConnectAsync(new(Url, "password"), CancellationToken.None));
        Assert.Equal(1, attempts);
        var service = DefaultService(); using var users = Users(service, new());
        await users.ConnectAsync(new(Url), CancellationToken.None);
        foreach (var search in new[] { "", "a", new string('a', 201), "%" })
            await Assert.ThrowsAsync<ToolException>(() => users.SearchAsync(search, CancellationToken.None));
        Assert.Empty(service.Queries);
        await users.SearchAsync("a_%", CancellationToken.None);
        Assert.Equal("a[_][%]%", service.Queries[0].Criteria.Filters[0].Conditions[0].Values[0]);
        await Assert.ThrowsAsync<ToolException>(() => users.ResolveAsync(new(Guid.Empty), CancellationToken.None));
    }

    [Fact]
    public async Task MissingMetadataAndDisabledUsersFailWithoutCachingASuccessfulIdentity()
    {
        var service = new Service(query => query.EntityName == "systemuser" ? Page(User(Alice)) : Page(Role(Guid.NewGuid())));
        using var users = Users(service, new()); await users.ConnectAsync(new(Url), CancellationToken.None);
        var error = await Assert.ThrowsAsync<ToolException>(() => users.ResolveAsync(new(Alice), CancellationToken.None));
        Assert.Contains("missing or duplicated", error.Message);
        using var disabled = Users(new Service(_ => Page(User(Alice, true))), new()); await disabled.ConnectAsync(new(Url), CancellationToken.None);
        await Assert.ThrowsAsync<ToolException>(() => disabled.ResolveAsync(new(Alice), CancellationToken.None));
        using var noRoles = new ExplorerUsers([], null, NullLoggerFactory.Instance, _ => throw new Exception("must not connect"));
        await Assert.ThrowsAsync<ToolException>(() => noRoles.ConnectAsync(new(Url), CancellationToken.None));
    }

    [Fact]
    public async Task SeparateSelectionsRetainIdentitiesAndStrictDirectDelegationBoundaryInQueryCoverage()
    {
        using var users = Users(DefaultService(), new()); await users.ConnectAsync(new(Url), CancellationToken.None);
        var app = await users.ResolveAsync(new(Alice), CancellationToken.None);
        var caller = await users.ResolveAsync(new(Bob, true), CancellationToken.None);
        Assert.NotEqual(app.SelectionId, caller.SelectionId);
        Assert.True(users.Selection(caller.SelectionId, true).LiveSource!.IsCaller);
        var model = Model(users);
        var report = model.Query(new("function", "Read", UserSelection: app.SelectionId));
        Assert.Equal("granted", report.GetProperty("entryPoints")[0].GetProperty("requirements")[0].GetProperty("coverage").GetProperty("status").GetString());
        var impersonated = model.Query(new("function", "ActForUser", UserSelection: app.SelectionId, CallerUserSelection: caller.SelectionId));
        var delegation = impersonated.GetProperty("entryPoints")[0].GetProperty("requirements").EnumerateArray()
            .Single(r => r.GetProperty("privilege").GetString() == GlobalRequirement.ActOnBehalfOfAnotherUser);
        Assert.Equal("missing", delegation.GetProperty("coverage").GetProperty("status").GetString());
        Assert.Throws<ToolException>(() => model.Query(new("function", "Read", UserSelection: caller.SelectionId)));
        Assert.Throws<ToolException>(() => model.Query(new("function", "Read", Roles: ["Reader"], UserSelection: app.SelectionId)));
        Assert.Throws<ToolException>(() => model.Query(new("function", "Read", NoRoles: true, UserSelection: app.SelectionId)));
        Assert.Throws<ToolException>(() => Model(users, true).Query(new("function", "Read", UserSelection: app.SelectionId)));
        users.Disconnect();
        Assert.Throws<ToolException>(() => model.Query(new("function", "Read", UserSelection: app.SelectionId)));
    }

    [Fact]
    public async Task SnapshotReplayCanUseLiveUsersWithoutSavingSelectionsOrReloadingSource()
    {
        var path = Path.GetTempFileName();
        try {
            var artifact = AnalysisSnapshot.Create(Builder(), new AnalysisProvenance("Missing.csproj", [], [], "hash", []), [], [Reader, Writer], "test", "/missing");
            await artifact.SaveAsync(path, CancellationToken.None);
            var snapshot = AnalysisSnapshot.Read(path);
            var roles = snapshot.RoleDefinitions()!;
            using var users = new ExplorerUsers(roles, null, NullLoggerFactory.Instance, _ => new(DefaultService(), new Lifetime()));
            await users.ConnectAsync(new(Url), CancellationToken.None);
            var selected = await users.ResolveAsync(new(Alice), CancellationToken.None);
            var model = new SecurityModel("/does-not-exist", snapshot.Graph, snapshot.Diagnostics, roles, new PermissionsConfiguration(), null,
                snapshot.Inputs, columnCatalog: snapshot.ColumnCatalog, users: users);
            var report = model.Query(new("function", "Read", UserSelection: selected.SelectionId));
            Assert.Equal("granted", report.GetProperty("entryPoints")[0].GetProperty("requirements")[0].GetProperty("coverage").GetProperty("status").GetString());
            await snapshot.SaveAsync(path, CancellationToken.None);
            var saved = await File.ReadAllTextAsync(path);
            Assert.DoesNotContain(selected.SelectionId, saved);
            Assert.DoesNotContain("alice@example.com", saved);
            Assert.DoesNotContain("liveSource", saved);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task CancelledAuthenticationDisposesTheConnectionAndDoesNotCacheUsers()
    {
        using var cancellation = new CancellationTokenSource(); var lifetime = new Lifetime();
        using var users = new ExplorerUsers([Reader], null, NullLoggerFactory.Instance, _ => {
            cancellation.Cancel(); return new(DefaultService(), lifetime);
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => users.ConnectAsync(new(Url), cancellation.Token));
        Assert.True(lifetime.Disposed); Assert.False(users.Status.Connected);
    }

    [Theory]
    [InlineData("", "https://configured.crm.dynamics.com", "devicecode")]
    [InlineData("--dataverse-url https://override.crm.dynamics.com", "https://override.crm.dynamics.com", "devicecode")]
    [InlineData("--credential-type azcli", "https://configured.crm.dynamics.com", "azcli")]
    [InlineData("--dataverse-url https://override.crm.dynamics.com --credential-type browser", "https://override.crm.dynamics.com", "browser")]
    [InlineData("-u https://override.crm.dynamics.com -t azcli", "https://override.crm.dynamics.com", "azcli")]
    public void ExplorerDefaultsApplyConfigurationThenCliOverridesWithoutConnecting(string arguments, string expectedUrl, string expectedCredential)
    {
        var root = new RootCommand(); var options = new AnalysisOptions(); options.AddTo(root);
        var parsed = root.Parse("--explore --snapshot model.json " + arguments);
        Assert.Empty(parsed.Errors);
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["DataverseUrl"] = "https://configured.crm.dynamics.com", ["DataverseCredentialType"] = "devicecode"
        }).Build();
        var merged = DataverseRoleLookup.CreateConnectionConfiguration(options.Read(parsed), new LoadedConfiguration(".", new(), null, settings));
        try {
            using var users = new ExplorerUsers([Reader], merged, NullLoggerFactory.Instance, _ => throw new Exception("Must not connect"));
            var data = JsonSerializer.SerializeToElement(users.Options);
            Assert.Equal(expectedUrl, data.GetProperty("environmentUrl").GetString());
            Assert.Equal(expectedCredential, data.GetProperty("credentialType").GetString());
            Assert.False(users.Status.Connected);
        }
        finally { (merged as IDisposable)?.Dispose(); (settings as IDisposable)?.Dispose(); }
    }

    [Fact]
    public void ConnectionOptionsDoNotExposeConfiguredCredentials()
    {
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["DataverseUrl"] = "https://username:secret@example.crm.dynamics.com?token=secret", ["ClientSecret"] = "never-in-browser"
        }).Build();
        try {
            using var users = new ExplorerUsers([Reader], settings, NullLoggerFactory.Instance);
            var data = JsonSerializer.Serialize(users.Options);
            Assert.DoesNotContain("secret", data); Assert.DoesNotContain("never-in-browser", data);
        }
        finally { (settings as IDisposable)?.Dispose(); }
    }

    [Fact]
    public async Task DisconnectWaitsForInFlightRoleLookupBeforeDisposingTheConnection()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim(false);
        var fallback = DefaultService(); var lifetime = new Lifetime();
        var service = new Service(query => {
            if (query.EntityName == "systemuser" && query.TopCount == 1) { started.TrySetResult(); release.Wait(TimeSpan.FromSeconds(10)); }
            return fallback.RetrieveMultiple(query);
        });
        using var users = Users(service, lifetime);
        using var reservation = new TcpListener(IPAddress.Loopback, 0); reservation.Start(); var port = ((IPEndPoint)reservation.LocalEndpoint).Port; reservation.Stop();
        using var cancellation = new CancellationTokenSource();
        var server = SecurityWebHost.RunAsync(Model(users), port, NullLogger.Instance, cancellation.Token);
        using var client = new HttpClient { BaseAddress = new($"http://127.0.0.1:{port}/"), Timeout = TimeSpan.FromSeconds(15) };
        try {
            var session = await client.GetFromJsonAsync<JsonElement>("api/session"); client.DefaultRequestHeaders.Add("X-XrmMockup-Token", session.GetProperty("token").GetString());
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("api/users/connect", new { environmentUrl = Url })).StatusCode);
            var resolving = client.PostAsJsonAsync("api/users/resolve", new { userId = Alice });
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var disconnecting = client.PostAsJsonAsync("api/users/disconnect", new { });
            await Task.Delay(100);
            Assert.False(disconnecting.IsCompleted); Assert.False(lifetime.Disposed);
            release.Set();
            Assert.Equal(HttpStatusCode.OK, (await resolving).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await disconnecting).StatusCode);
            Assert.True(lifetime.Disposed); Assert.False(users.Status.Connected);
        }
        finally { release.Set(); cancellation.Cancel(); await server.WaitAsync(TimeSpan.FromSeconds(10)); }
    }

    [Fact]
    public async Task UserEndpointsRetainSessionAndSameOriginProtections()
    {
        using var users = Users(DefaultService(), new());
        using var reservation = new TcpListener(IPAddress.Loopback, 0); reservation.Start(); var port = ((IPEndPoint)reservation.LocalEndpoint).Port; reservation.Stop();
        using var cancellation = new CancellationTokenSource();
        var server = SecurityWebHost.RunAsync(Model(users), port, NullLogger.Instance, cancellation.Token);
        using var client = new HttpClient { BaseAddress = new($"http://127.0.0.1:{port}/"), Timeout = TimeSpan.FromSeconds(10) };
        try {
            foreach (var endpoint in new[] { "connect", "disconnect", "search", "resolve" })
                Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("api/users/" + endpoint, new { })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/users/status")).StatusCode);
            var session = await client.GetFromJsonAsync<JsonElement>("api/session"); client.DefaultRequestHeaders.Add("X-XrmMockup-Token", session.GetProperty("token").GetString());
            Assert.False((await client.GetFromJsonAsync<ExplorerConnection>("api/users/status"))!.Connected);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("api/users/connect", new { environmentUrl = Url, credentialType = "browser" })).StatusCode);
            var status = await client.GetFromJsonAsync<ExplorerConnection>("api/users/status");
            Assert.True(status!.Connected); Assert.Equal(Bob, status.ConnectedUser!.Id);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.GetAsync("api/users/search")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("api/users/search", new { })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("api/users/resolve", new { userId = "not-a-guid" })).StatusCode);
            var search = await client.PostAsJsonAsync("api/users/search", new { search = "Example" }); Assert.Equal(HttpStatusCode.OK, search.StatusCode);
            Assert.Equal(2, (await search.Content.ReadFromJsonAsync<ExplorerUser[]>())!.Length);
            var selection = await (await client.PostAsJsonAsync("api/users/resolve", new { userId = Alice, isCaller = false })).Content.ReadFromJsonAsync<ExplorerSelection>();
            Assert.NotNull(selection);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("api/query", new { scope = "function", target = "Read", userSelection = selection.SelectionId })).StatusCode);
            using var foreign = new HttpRequestMessage(HttpMethod.Post, "api/users/disconnect") { Content = JsonContent.Create(new { }) }; foreign.Headers.Add("Origin", "https://attacker.test");
            Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(foreign)).StatusCode); Assert.True(users.Status.Connected);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("api/users/disconnect", new { })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("api/query", new { scope = "function", target = "Read", userSelection = selection.SelectionId })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("api/users/search", new { search = "Example" })).StatusCode);
        }
        finally { cancellation.Cancel(); await server.WaitAsync(TimeSpan.FromSeconds(10)); }
    }
}
