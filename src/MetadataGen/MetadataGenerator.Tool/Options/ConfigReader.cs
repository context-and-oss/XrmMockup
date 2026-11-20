using Microsoft.Extensions.Configuration;

namespace XrmMockup.MetadataGenerator.Tool.Options;

/// <summary>
/// Reads configuration from appsettings.json, environment variables and command-line overrides.
/// </summary>
internal sealed class ConfigReader(ConfigurationOverrides overrides) : IConfigReader
{
    public const string ConfigFileBase = "appsettings";

    private IConfiguration? _configuration;

    public IConfiguration GetConfiguration()
    {
        _configuration ??= new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"{ConfigFileBase}.json", optional: true)
            .AddJsonFile($"{ConfigFileBase}.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            // Last source wins, so CLI overrides take precedence over files and the environment.
            .AddInMemoryCollection(overrides.Values)
            .Build();

        return _configuration;
    }
}
