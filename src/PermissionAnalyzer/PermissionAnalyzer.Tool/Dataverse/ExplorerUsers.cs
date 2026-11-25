using System.Security.Cryptography;
using DataverseConnection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;

namespace XrmMockup.PermissionAnalyzer.Tool.Dataverse;

internal sealed record UserConnectionRequest(string EnvironmentUrl, string CredentialType = "browser");
internal sealed record UserSearchRequest(string Search);
internal sealed record UserResolveRequest(Guid UserId, bool IsCaller = false);
internal sealed record ExplorerConnection(bool Connected, string? EnvironmentUrl = null, ExplorerUser? ConnectedUser = null);
internal sealed record ExplorerUser(Guid Id, string Name, string? Email, string? Upn, bool Disabled);
internal sealed record ExplorerSelection(string SelectionId, ExplorerUser User, string EnvironmentUrl, DateTimeOffset ReadAt,
    IReadOnlyList<string> Roles, IReadOnlyList<LiveRoleAssignment> Assignments, bool IsCaller);

/// <summary>Read-only, opt-in live service. Credentials and role sets never leave the backend.
/// All operations share a gate with query evaluation, so disconnect cannot race a selected identity.</summary>
internal sealed class ExplorerUsers(IReadOnlyList<RoleDefinition>? roles, IConfiguration? configuration,
    ILoggerFactory loggerFactory, Func<UserConnectionRequest, ExplorerUsers.ConnectedService>? factory = null) : IDisposable
{
    internal sealed class ConnectedService(IOrganizationService service, IDisposable lifetime, IDisposable? configurationLifetime = null) : IDisposable
    {
        internal IOrganizationService Service { get; } = service;
        public void Dispose() { try { lifetime.Dispose(); } finally { configurationLifetime?.Dispose(); } }
    }
    private ConnectedService? connection;
    private string? environmentUrl;
    private ExplorerUser? connectedUser;
    private readonly Dictionary<string, (RoleSet Roles, bool IsCaller)> selections = new(StringComparer.Ordinal);
    internal object Options => new { available = roles is { Count: > 0 },
        environmentUrl = SuggestedUrl(configuration?["DataverseUrl"] ?? configuration?["DATAVERSE_URL"]),
        credentialType = SuggestedCredential(configuration?["DataverseCredentialType"] ?? configuration?["DATAVERSE_CREDENTIAL_TYPE"]) };

    private static string SuggestedCredential(string? value) => value is "browser" or "devicecode" or "azcli" ? value : "browser";

    private static string SuggestedUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https"
        && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0 ? uri.GetLeftPart(UriPartial.Authority) : "";

    internal ExplorerConnection Status => new(connection is not null, environmentUrl, connectedUser);

    internal async Task<ExplorerConnection> ConnectAsync(UserConnectionRequest request, CancellationToken cancellationToken)
    {
        if (roles is not { Count: > 0 }) throw new ToolException("User lookup needs local security-role metadata. Load metadata or a saved analysis that contains roles.");
        ValidateConnection(request);
        if (connection is not null) throw new ToolException("Disconnect before connecting to another environment.");
        ConnectedService? next = null;
        try
        {
            // Authentication may prompt in the server's browser/terminal. It never receives browser-supplied credentials.
            next = await Task.Run(() => factory is null ? CreateConnection(request) : factory(request), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            // Verify the actual service identity, rather than inferring it from the
            // browser sign-in or a selected user. Commit connection state only after
            // both WhoAmI and the minimal profile read have succeeded.
            var user = await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var identity = next.Service.Execute(new WhoAmIRequest()) as WhoAmIResponse;
                if (identity is null || identity.UserId == Guid.Empty) throw new InvalidOperationException("WhoAmI did not return a user.");
                cancellationToken.ThrowIfCancellationRequested();
                return User(next.Service.Retrieve("systemuser", identity.UserId,
                    new ColumnSet("fullname", "domainname", "internalemailaddress", "isdisabled")));
            }, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            connectedUser = user;
            connection = next;
            environmentUrl = new Uri(request.EnvironmentUrl).GetLeftPart(UriPartial.Authority) + "/";
            return Status;
        }
        catch (OperationCanceledException) { next?.Dispose(); throw; }
        catch { next?.Dispose(); throw new ToolException("Could not connect to Dataverse. Check the environment URL and complete sign-in in the local browser or terminal, then try again."); }
    }

    internal async Task<IReadOnlyList<ExplorerUser>> SearchAsync(string search, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(search) || search.Trim().Length is < 2 or > 200)
            throw new ToolException("Enter between 2 and 200 characters of a user's name or email.");
        var service = RequireConnection();
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query = UserQuery(); query.TopCount = 50;
            // Prefix search avoids a leading-wildcard scan. Escape wildcard characters
            // so an email containing '_' is treated as a literal name, not a pattern.
            var term = string.Concat(search.Trim().Select(character => character switch {
                '%' => "[%]", '_' => "[_]", '[' => "[[]", ']' => "[]]", _ => character.ToString()
            }));
            var match = new FilterExpression(LogicalOperator.Or);
            foreach (var attribute in new[] { "fullname", "domainname", "internalemailaddress" }) match.AddCondition(attribute, ConditionOperator.Like, term + "%");
            query.Criteria.AddFilter(match);
            var users = service.RetrieveMultiple(query).Entities;
            cancellationToken.ThrowIfCancellationRequested();
            return (IReadOnlyList<ExplorerUser>)users.Select(User).ToList();
        }, cancellationToken);
    }

    internal async Task<ExplorerSelection> ResolveAsync(UserResolveRequest request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty) throw new ToolException("Choose a user from the search results.");
        var service = RequireConnection();
        if (selections.Count >= 100) throw new ToolException("This session has reached its user-selection limit. Disconnect and reconnect to clear it.");
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query = UserQuery(); query.TopCount = 1; query.Criteria.AddCondition("systemuserid", ConditionOperator.Equal, request.UserId);
            var entity = service.RetrieveMultiple(query).Entities.SingleOrDefault()
                ?? throw new ToolException("The selected user is no longer available. Search again.");
            var user = User(entity);
            if (user.Disabled) throw new ToolException("This user is disabled. Choose an enabled user.");
            var readAt = DateTimeOffset.UtcNow;
            var resolved = new UserRoleResolver(service).Resolve(user.Id, user.Upn ?? user.Email ?? user.Name,
                roles!, environmentUrl!, request.IsCaller ? CliOptions.CallerUser.Primary : CliOptions.User.Primary, readAt, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            selections.Add(id, (resolved, request.IsCaller));
            return new ExplorerSelection(id, user, environmentUrl!, readAt, resolved.Names, resolved.LiveSource!.Assignments, request.IsCaller);
        }, cancellationToken);
    }

    internal RoleSet Selection(string id, bool isCaller)
    {
        RequireConnection();
        if (!selections.TryGetValue(id, out var selected) || selected.IsCaller != isCaller)
            throw new ToolException("The selected user has expired or belongs to a different identity. Look up the user again.");
        return selected.Roles;
    }

    internal ExplorerConnection Disconnect()
    {
        selections.Clear(); environmentUrl = null; connectedUser = null;
        var old = connection; connection = null; old?.Dispose();
        return Status;
    }
    public void Dispose() => Disconnect();
    private IOrganizationService RequireConnection() => connection?.Service ?? throw new ToolException("Connect to Dataverse before looking up or using live users.");
    private static QueryExpression UserQuery()
    {
        var query = new QueryExpression("systemuser") { ColumnSet = new("systemuserid", "fullname", "domainname", "internalemailaddress", "isdisabled") };
        query.AddOrder("fullname", OrderType.Ascending); query.AddOrder("systemuserid", OrderType.Ascending);
        return query;
    }
    private static ExplorerUser User(Entity entity) => new(entity.Id, entity.GetAttributeValue<string>("fullname") ?? entity.Id.ToString(),
        entity.GetAttributeValue<string>("internalemailaddress"), entity.GetAttributeValue<string>("domainname"), entity.GetAttributeValue<bool>("isdisabled"));

    internal static void ValidateConnection(UserConnectionRequest request)
    {
        if (request.CredentialType is not ("browser" or "devicecode" or "azcli")) throw new ToolException("Choose browser sign-in, device code, or Azure CLI sign-in.");
        // Do not turn this authenticated loopback API into a proxy to arbitrary/private URLs.
        if (!Uri.TryCreate(request.EnvironmentUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !uri.IsDefaultPort
            || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.AbsolutePath != "/"
            || !new[] { ".dynamics.com", ".microsoftdynamics.us", ".dynamics.cn", ".crm.appsplatform.us" }.Any(suffix => uri.Host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
            throw new ToolException("Enter an HTTPS Dataverse cloud environment URL without a path, port, or credentials.");
    }
    private ConnectedService CreateConnection(UserConnectionRequest request)
    {
        var config = new ConfigurationBuilder();
        if (configuration is not null) config.AddConfiguration(configuration);
        var settings = config.AddInMemoryCollection(new Dictionary<string, string?> {
            ["DataverseUrl"] = request.EnvironmentUrl, ["DataverseCredentialType"] = request.CredentialType
        }).Build();
        var services = new ServiceCollection(); services.AddSingleton<IConfiguration>(settings); services.AddSingleton(loggerFactory); services.AddLogging(); services.AddDataverse();
        var provider = services.BuildServiceProvider();
        try
        {
            var client = provider.GetRequiredService<ServiceClient>();
            if (!client.IsReady) throw new ToolException("Dataverse connection failed.");
            return new ConnectedService(client, provider, settings as IDisposable);
        }
        catch { provider.Dispose(); (settings as IDisposable)?.Dispose(); throw; }
    }
}
