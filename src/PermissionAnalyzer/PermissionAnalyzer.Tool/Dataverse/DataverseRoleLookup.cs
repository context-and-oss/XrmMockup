using DataverseConnection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using XrmMockup.PermissionAnalyzer.Core.Coverage;
using XrmMockup.PermissionAnalyzer.Core.Model;
using XrmMockup.PermissionAnalyzer.Tool.Commands;
using XrmMockup.PermissionAnalyzer.Tool.Options;

namespace XrmMockup.PermissionAnalyzer.Tool.Dataverse;

/// <summary>Authentication belongs to the same connection library as the metadata tool, only on opt-in runs.</summary>
internal static class DataverseRoleLookup
{
    public static async Task<(RoleSet? User, RoleSet? Caller)> ResolveAsync(AnalysisRequest request,
        LoadedConfiguration configuration, IReadOnlyList<RoleDefinition>? roles, ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (request.User is null && request.CallerUser is null) return (null, null);
        if (roles is null || roles.Count == 0)
            throw new ToolException("User role lookup needs downloaded security roles. Pass --metadata with a folder that has SecurityRoles.");

        var connection = CreateConnectionConfiguration(request, configuration);
        using var connectionLifetime = connection as IDisposable;
        var url = connection["DataverseUrl"] ?? connection["DATAVERSE_URL"];
        if (!Uri.TryCreate(url, UriKind.Absolute, out var environment) || environment.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(environment.UserInfo) || !string.IsNullOrEmpty(environment.Query) || !string.IsNullOrEmpty(environment.Fragment))
            throw new ToolException("Set DataverseUrl (or --dataverse-url) to an HTTPS Dataverse environment URL.");

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(connection);
        services.AddSingleton(loggerFactory);
        services.AddLogging();
        services.AddDataverse();
        await using var provider = services.BuildServiceProvider();
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var client = provider.GetRequiredService<ServiceClient>();
            if (!client.IsReady) throw new ToolException("Dataverse connection failed. Check the connection and authentication settings.");
            var resolver = new UserRoleResolver(client);
            var readAt = DateTimeOffset.UtcNow;
            var user = request.User is { } username ? resolver.Resolve(username, roles, environment.AbsoluteUri,
                CliOptions.User.Primary, readAt, cancellationToken) : null;
            var caller = request.CallerUser is { } callerName ? resolver.Resolve(callerName, roles, environment.AbsoluteUri,
                CliOptions.CallerUser.Primary, readAt, cancellationToken) : null;
            return (user, caller);
        }, cancellationToken);
    }

    internal static IConfigurationRoot CreateConnectionConfiguration(AnalysisRequest request, LoadedConfiguration configuration)
    {
        var overrides = new Dictionary<string, string?>();
        if (request.DataverseUrl is not null) overrides["DataverseUrl"] = request.DataverseUrl;
        if (request.CredentialType is not null) overrides["DataverseCredentialType"] = request.CredentialType;
        var builder = new ConfigurationBuilder();
        if (configuration.ConnectionConfiguration is { } settings) builder.AddConfiguration(settings);
        return builder.AddInMemoryCollection(overrides).Build();
    }
}
