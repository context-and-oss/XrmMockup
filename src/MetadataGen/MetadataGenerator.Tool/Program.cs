using System.CommandLine;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XrmMockup.MetadataGenerator.Core.Models;
using XrmMockup.MetadataGenerator.Core.Services;
using XrmMockup.MetadataGenerator.Tool;
using XrmMockup.MetadataGenerator.Tool.Extensions;
using XrmMockup.MetadataGenerator.Tool.Options;

// Define CLI options
var outputOption = new Option<string?>(CliOptions.Output.Primary, CliOptions.Output.Alias)
{
    Description = CliOptions.Output.Description,
    Arity = ArgumentArity.ZeroOrOne
};

var solutionsOption = new Option<string?>(CliOptions.Solutions.Primary, CliOptions.Solutions.Alias)
{
    Description = CliOptions.Solutions.Description,
    Arity = ArgumentArity.ZeroOrOne
};

var entitiesOption = new Option<string?>(CliOptions.Entities.Primary, CliOptions.Entities.Alias)
{
    Description = CliOptions.Entities.Description,
    Arity = ArgumentArity.ZeroOrOne
};

var configOption = new Option<string?>(CliOptions.Config.Primary, CliOptions.Config.Alias)
{
    Description = CliOptions.Config.Description,
    Arity = ArgumentArity.ZeroOrOne
};

var prettyPrintOption = new Option<bool>(CliOptions.PrettyPrint.Primary, CliOptions.PrettyPrint.Alias)
{
    Description = CliOptions.PrettyPrint.Description
};

var securityRolesOption = new Option<string?>(CliOptions.SecurityRoles.Primary, CliOptions.SecurityRoles.Alias)
{
    Description = CliOptions.SecurityRoles.Description,
    Arity = ArgumentArity.ZeroOrOne
};

var allSecurityRolesOption = new Option<bool>(CliOptions.AllSecurityRoles.Primary, CliOptions.AllSecurityRoles.Alias)
{
    Description = CliOptions.AllSecurityRoles.Description
};

var dataverseUrlOption = new Option<string?>(CliOptions.DataverseUrl.Primary, CliOptions.DataverseUrl.Alias)
{
    Description = CliOptions.DataverseUrl.Description,
    Arity = ArgumentArity.ZeroOrOne
};

// Validated and normalised here so an unknown value fails at parse time with the valid values
// listed, instead of surfacing later as a connection failure.
var credentialTypeOption = new Option<string?>(CliOptions.CredentialType.Primary, CliOptions.CredentialType.Alias)
{
    Description = CliOptions.CredentialType.Description,
    Arity = ArgumentArity.ExactlyOne,
    HelpName = CliOptions.CredentialType.HelpName,
    CustomParser = result =>
    {
        var value = result.Tokens.Count == 1 ? result.Tokens[0].Value.Trim() : string.Empty;
        var match = Array.Find(
            CliOptions.CredentialType.AllowedValues,
            allowed => string.Equals(allowed, value, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            result.AddError(
                $"'{value}' is not a valid {CliOptions.CredentialType.Primary}. " +
                $"Valid values: {string.Join(", ", CliOptions.CredentialType.AllowedValues)}.");
            return null;
        }

        return match;
    }
};

// Build root command
var rootCommand = new RootCommand("XrmMockup Metadata Generator - Generate metadata from Dataverse for XrmMockup testing")
{
    outputOption,
    solutionsOption,
    entitiesOption,
    configOption,
    prettyPrintOption,
    securityRolesOption,
    allSecurityRolesOption,
    dataverseUrlOption,
    credentialTypeOption
};

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var output = parseResult.GetValue(outputOption);
    var solutions = parseResult.GetValue(solutionsOption);
    var entities = parseResult.GetValue(entitiesOption);
    var config = parseResult.GetValue(configOption);
    var prettyPrint = parseResult.GetValue(prettyPrintOption);
    var securityRoles = parseResult.GetValue(securityRolesOption);
    var allSecurityRoles = parseResult.GetValue(allSecurityRolesOption);
    var dataverseUrl = parseResult.GetValue(dataverseUrlOption);
    var credentialType = parseResult.GetValue(credentialTypeOption);

    // If config path specified, change to that directory for config loading
    if (!string.IsNullOrEmpty(config))
    {
        var configDir = Path.GetDirectoryName(Path.GetFullPath(config));
        if (!string.IsNullOrEmpty(configDir))
        {
            Directory.SetCurrentDirectory(configDir);
        }
    }

    // Connection settings are owned by DataverseConnection, so CLI values are fed back in as
    // configuration rather than translated here. DataverseConnection reads DataverseUrl and
    // DataverseCredentialType (legacy: DATAVERSE_URL, DATAVERSE_CREDENTIAL_TYPE) off IConfiguration.
    var connectionOverrides = new Dictionary<string, string?>();
    if (!string.IsNullOrWhiteSpace(dataverseUrl))
        connectionOverrides[CliOptions.DataverseUrl.ConfigurationKey] = dataverseUrl;
    if (!string.IsNullOrWhiteSpace(credentialType))
        connectionOverrides[CliOptions.CredentialType.ConfigurationKey] = credentialType;

    // Build service provider
    var services = new ServiceCollection();
    services.AddMetadataGeneratorTool(metadataConfig => new GeneratorOptions
    {
        OutputDirectory = output ?? metadataConfig.OutputDirectory,
        Solutions = ParseCommaSeparated(solutions) ?? metadataConfig.Solutions,
        Entities = ParseCommaSeparated(entities) ?? metadataConfig.Entities,
        SecurityRoles = ParseCommaSeparated(securityRoles) ?? metadataConfig.SecurityRoles,
        AllSecurityRoles = allSecurityRoles || metadataConfig.AllSecurityRoles,
        PrettyPrint = prettyPrint || metadataConfig.PrettyPrint
    },
    new ConfigurationOverrides(connectionOverrides));

    await using var serviceProvider = services.BuildServiceProvider();

    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    var version = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
    logger.LogInformation("XrmMockup Metadata Generator v{Version}", version);

    try
    {
        var generator = serviceProvider.GetRequiredService<IMetadataGeneratorService>();
        await generator.GenerateAsync(cancellationToken);
        return 0;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Metadata generation failed");
        return 1;
    }
});

return await rootCommand.Parse(args).InvokeAsync();

static string[]? ParseCommaSeparated(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return null;

    return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
