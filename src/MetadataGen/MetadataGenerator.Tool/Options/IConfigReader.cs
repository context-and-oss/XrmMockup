using Microsoft.Extensions.Configuration;

namespace XrmMockup.MetadataGenerator.Tool.Options;

/// <summary>
/// Reads configuration from appsettings.json and environment variables.
/// </summary>
public interface IConfigReader
{
    /// <summary>
    /// Gets the configuration.
    /// </summary>
    IConfiguration GetConfiguration();
}
