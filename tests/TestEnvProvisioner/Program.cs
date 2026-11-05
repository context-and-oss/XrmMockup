using DataverseConnection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using XrmMockup.TestEnvProvisioner;

// ---------------------------------------------------------------------------------------------
// XrmMockup test-environment provisioner
//
// Recreates the custom test entities, relationships, option sets and security roles that the
// XrmMockup test suite depends on, so that after running this tool you can regenerate metadata
// (Regenerate-TestMetadata.ps1) and the corresponding tests can be migrated/un-skipped.
//
// Auth + target org: identical to the metadata generator. It reads DataverseUrl (legacy:
// DATAVERSE_URL) from a config file (default ./appsettings.json, override with --config <path>) or
// the environment, and connects with the DataverseConnection package. Authentication defaults to
// an interactive browser sign-in; override it with --credential-type browser|devicecode|azcli.
//
// Run:
//   dotnet run --project tests/TestEnvProvisioner -- --config tests/appsettings.json
//
// The tool is idempotent: it checks for existing components before creating them, so it is safe
// to re-run. Use --whatif to print the plan without writing anything.
// ---------------------------------------------------------------------------------------------

string? configPath = GetArg(args, "--config");
bool whatIf = args.Contains("--whatif", StringComparer.OrdinalIgnoreCase);
// Unmanaged solution that the created entities/roles are added to (so metadata regeneration and
// solution export pick them up). Override with --solution <uniquename>.
string solutionName = GetArg(args, "--solution") ?? "XrmMockup";

// Inspect mode: print an existing attribute's SourceType + FormulaDefinition (the classic calculated
// or rollup XAML, or the Power Fx text) and exit — use it to capture XAML to bake into TestSchema.
//   --dump-formula <entitylogicalname>.<attributelogicalname>
string? dumpTarget = GetArg(args, "--dump-formula");

// Connection overrides, mirroring the metadata generator's --dataverse-url / --credential-type.
// These are the values accepted by DataverseConnection's DataverseCredentialType setting.
string[] credentialTypes = ["browser", "devicecode", "azcli"];
string? dataverseUrlArg = GetArg(args, "--dataverse-url");
string? credentialTypeArg = GetArg(args, "--credential-type");
if (credentialTypeArg is not null &&
    !credentialTypes.Contains(credentialTypeArg.Trim(), StringComparer.OrdinalIgnoreCase))
{
    Console.Error.WriteLine(
        $"'{credentialTypeArg}' is not a valid --credential-type. Valid values: {string.Join(", ", credentialTypes)}.");
    return 1;
}

// Build configuration the same way the metadata generator does — appsettings.json (from the
// --config file's folder, or the current directory), environment variables, then the CLI overrides
// above. This IConfiguration is registered in DI below, because DataverseConnection's AddDataverse()
// reads DataverseUrl (or the legacy DATAVERSE_URL) from IConfiguration, NOT from the environment
// variable directly. The same configuration selects the credential: DataverseCredentialType =
// browser (the default since DataverseConnection 1.2.0), devicecode or azcli.
var baseDir = Directory.GetCurrentDirectory();
var jsonFile = "appsettings.json";
if (!string.IsNullOrEmpty(configPath))
{
    var full = Path.GetFullPath(configPath);
    baseDir = Path.GetDirectoryName(full)!;
    jsonFile = Path.GetFileName(full);
}

var cliOverrides = new Dictionary<string, string?>();
if (!string.IsNullOrWhiteSpace(dataverseUrlArg))
    cliOverrides["DataverseUrl"] = dataverseUrlArg;
if (!string.IsNullOrWhiteSpace(credentialTypeArg))
    cliOverrides["DataverseCredentialType"] = credentialTypeArg.Trim();

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(baseDir)
    .AddJsonFile(jsonFile, optional: true)
    .AddEnvironmentVariables()
    // Last source wins, so CLI overrides beat the file and the environment.
    .AddInMemoryCollection(cliOverrides)
    .Build();

// Work from the config folder so relative paths line up with the generator's behaviour.
if (!string.IsNullOrEmpty(configPath))
{
    Directory.SetCurrentDirectory(baseDir);
}

// DataverseConnection prefers DataverseUrl and falls back to the legacy DATAVERSE_URL, so accept both.
var targetUrl = configuration["DataverseUrl"];
if (string.IsNullOrWhiteSpace(targetUrl))
    targetUrl = configuration["DATAVERSE_URL"];

if (string.IsNullOrEmpty(targetUrl))
{
    Console.Error.WriteLine($"DataverseUrl is not set. Looked in '{Path.Combine(baseDir, jsonFile)}' and " +
        "environment variables. Pass --dataverse-url <url>, --config <appsettings.json>, or set the " +
        "DataverseUrl (legacy: DATAVERSE_URL) environment variable.");
    return 1;
}

Console.WriteLine($"Target environment: {targetUrl}");
if (whatIf) Console.WriteLine("** --whatif: no changes will be written **");

var services = new ServiceCollection();
services.AddSingleton(configuration);   // AddDataverse() resolves the Dataverse settings from IConfiguration
services.AddDataverse();
await using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<ServiceClient>();
if (!client.IsReady)
{
    Console.Error.WriteLine($"Failed to connect: {client.LastError}");
    return 1;
}
Console.WriteLine($"Connected to org {client.ConnectedOrgFriendlyName}.");

var builder = new MetadataBuilder((IOrganizationService)client, whatIf);

if (!string.IsNullOrEmpty(dumpTarget))
{
    var dot = dumpTarget.IndexOf('.');
    if (dot <= 0 || dot == dumpTarget.Length - 1)
    {
        Console.Error.WriteLine("Usage: --dump-formula <entitylogicalname>.<attributelogicalname>");
        return 1;
    }
    builder.DumpFormula(dumpTarget[..dot], dumpTarget[(dot + 1)..]);
    return 0;
}

try
{
    // 1. Publisher (provides the "ctx" prefix) + the unmanaged solution all components are added to.
    //    NOTE: the solution's publisher must own the "ctx" prefix; if --solution points at an existing
    //    solution with a different publisher prefix, creating ctx_* components in it will be rejected.
    var publisherId = builder.EnsurePublisher("ContextAnd", "ctx", 77079);
    builder.EnsureSolution(solutionName, solutionName, publisherId);

    // 2. Custom entities + their scalar/lookup/optionset/multiselect attributes + relationships.
    TestSchema.CreateEntities(builder);
    TestSchema.CreateRelationships(builder);

    // 3. Computed columns (Power Fx formula auto; classic calculated + rollups once their XAML is
    //    captured into TestSchema.Definitions via --dump-formula). Reference the base columns above.
    TestSchema.CreateComputedColumns(builder);

    // 3b. Alternate keys (XrmContext emits a Retrieve_<key> helper per key on regeneration).
    TestSchema.CreateAlternateKeys(builder);

    // 4. Security roles required by the security/privilege tests.
    TestRoles.CreateRoles(builder);

    // 5. Enforced state transitions on ctx_parent: Active(1) -> Inactive(2) only (Inactive->Active and
    //    same-state are then invalid), matching the state-transition tests in TestStates.
    builder.SetEnforcedStateTransitions(TestSchema.Names.ParentEntity, (1, new[] { 2 }), (2, System.Array.Empty<int>()));

    // 6. Publish everything.
    builder.PublishAll();

    // 5. Report the pieces that are intentionally NOT scripted (see TestSchema.PrintManualSteps).
    TestSchema.PrintManualSteps();

    Console.WriteLine("Done. Now regenerate metadata + context (scripts/Regenerate-TestMetadata.ps1) and migrate/un-skip the dependent tests.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Provisioning failed: {ex.Message}");
    Console.Error.WriteLine(ex);
    return 1;
}

static string? GetArg(string[] argv, string name)
{
    var i = Array.FindIndex(argv, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
    return i >= 0 && i + 1 < argv.Length ? argv[i + 1] : null;
}
