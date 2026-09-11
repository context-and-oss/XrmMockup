using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using XrmMockup.MetadataGenerator.Core.Extensions;
using XrmMockup.MetadataGenerator.Core.Models;
using XrmMockup.MetadataGenerator.Tool.Logging;
using XrmMockup.MetadataGenerator.Tool.Options;

using MsOptions = Microsoft.Extensions.Options;

namespace XrmMockup.MetadataGenerator.Tool.Extensions;

/// <summary>
/// Extension methods for registering tool services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds configuration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationOverrides">
    /// Flat configuration keys from the command line, applied with the highest precedence.
    /// </param>
    public static IServiceCollection AddToolConfiguration(
        this IServiceCollection services,
        ConfigurationOverrides? configurationOverrides = null)
    {
        services.AddSingleton(configurationOverrides ?? ConfigurationOverrides.Empty);
        services.AddSingleton<IConfigReader, ConfigReader>();
        services.AddSingleton(sp => sp.GetRequiredService<IConfigReader>().GetConfiguration());
        return services;
    }

    /// <summary>
    /// Adds generator options from configuration and CLI overrides.
    /// </summary>
    public static IServiceCollection AddGeneratorOptions(
        this IServiceCollection services,
        Func<MetadataConfiguration, GeneratorOptions> optionsFactory)
    {
        services.AddSingleton(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var metadataConfig = new MetadataConfiguration();
            configuration.GetSection(MetadataConfiguration.SectionPath).Bind(metadataConfig);

            return MsOptions.Options.Create(optionsFactory(metadataConfig));
        });

        return services;
    }

    /// <summary>
    /// Adds logging services.
    /// </summary>
    public static IServiceCollection AddToolLogging(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddConsole(options =>
                {
                    options.FormatterName = ShortCategoryConsoleFormatter.FormatterName;
                })
                .AddConsoleFormatter<ShortCategoryConsoleFormatter, SimpleConsoleFormatterOptions>(options =>
                {
                    options.TimestampFormat = "HH:mm:ss ";
                    options.ColorBehavior = LoggerColorBehavior.Enabled;
                });
        });

        return services;
    }

    /// <summary>
    /// Adds all required services for the metadata generator.
    /// </summary>
    public static IServiceCollection AddMetadataGeneratorTool(
        this IServiceCollection services,
        Func<MetadataConfiguration, GeneratorOptions> optionsFactory,
        ConfigurationOverrides? configurationOverrides = null)
    {
        return services
            .AddToolConfiguration(configurationOverrides)
            .AddGeneratorOptions(optionsFactory)
            .AddToolLogging()
            .AddMetadataGenerator();
    }
}
