using Microsoft.Extensions.Configuration;

namespace XrmMockup.PermissionAnalyzer.Tool.Options;

/// <summary>
/// Reads configuration the same way as the metadata tool: appsettings.json, then
/// appsettings.{DOTNET_ENVIRONMENT}.json, then environment variables. Command-line options are applied on top
/// by the caller.
/// </summary>
internal static class ConfigReader
{
    public const string ConfigFileBase = "appsettings";
    public const string MetadataOutputDirectoryKey = "XrmMockup:Metadata:OutputDirectory";

    /// <summary>
    /// Reads the configuration next to <paramref name="configPath"/>, or in the current directory when it's
    /// <c>null</c>. Relative paths in the configuration are relative to <see cref="LoadedConfiguration.Directory"/>.
    /// </summary>
    public static LoadedConfiguration Read(string? configPath)
    {
        string directory;
        string fileName;
        if (configPath is null)
        {
            directory = Directory.GetCurrentDirectory();
            fileName = $"{ConfigFileBase}.json";
        }
        else
        {
            var fullPath = Path.GetFullPath(configPath);
            if (!File.Exists(fullPath)) throw new ToolException($"Configuration file not found: {fullPath}");
            directory = Path.GetDirectoryName(fullPath)!;
            fileName = Path.GetFileName(fullPath);
        }

        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(directory)
            .AddJsonFile(fileName, optional: configPath is null)
            .AddJsonFile($"{ConfigFileBase}.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var permissions = configuration.GetSection(PermissionsConfiguration.SectionPath).Get<PermissionsConfiguration>()
            ?? new PermissionsConfiguration();

        return new LoadedConfiguration(directory, permissions, configuration[MetadataOutputDirectoryKey]);
    }
}

/// <param name="Directory">The folder that relative paths in the configuration are relative to.</param>
/// <param name="Permissions">The <c>XrmMockup:Permissions</c> section.</param>
/// <param name="MetadataOutputDirectory">The metadata tool's output folder, when configured.</param>
internal sealed record LoadedConfiguration(string Directory, PermissionsConfiguration Permissions, string? MetadataOutputDirectory);
