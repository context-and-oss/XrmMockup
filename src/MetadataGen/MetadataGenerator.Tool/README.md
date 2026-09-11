# XrmMockup Metadata Generator

[![Build Status](https://github.com/delegateas/XrmMockup/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/delegateas/XrmMockup/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/XrmMockup.MetadataGenerator.svg)](https://www.nuget.org/packages/XrmMockup.MetadataGenerator)

A .NET CLI tool for generating metadata from Microsoft Dataverse/Dynamics 365 environments. This tool is a companion to [XrmMockup](https://github.com/delegateas/XrmMockup), extracting the metadata files required for local CRM simulation in your tests.

## Installation

Install the tool globally:

```bash
dotnet tool install --global XrmMockup.MetadataGenerator
```

Or install locally in your project:

```bash
dotnet new tool-manifest # if you don't have a manifest yet
dotnet tool install XrmMockup.MetadataGenerator
```

## Quick Start

1. Create an `appsettings.json` in your project directory
2. Run the tool:

```bash
dotnet tool xrmmockup-metadata
```

The tool will connect to Dataverse using an interactive browser sign-in and generate metadata files.

## Authentication

This tool uses the [DataverseConnection](https://github.com/context-and-oss/DataverseConnection) library
for authentication. DataverseConnection owns these settings, so the tool reads them from the same
flat configuration keys the library documents (see its
[Configuration](https://github.com/context-and-oss/DataverseConnection#configuration) section), and
passes any CLI override straight back to the library.

| Setting | Legacy key | Default | Description |
|---------|------------|---------|-------------|
| `DataverseUrl` | `DATAVERSE_URL` | required | Your Dataverse environment URL (e.g., `https://your-org.crm4.dynamics.com`) |
| `DataverseCredentialType` | `DATAVERSE_CREDENTIAL_TYPE` | `browser` | Azure credential used to authenticate (see below) |

Both keys can be set in `appsettings.json`, as environment variables, or on the command line
(`--dataverse-url` / `--credential-type`). The legacy uppercase keys are still honoured, but the
PascalCase names are preferred.

### Credential types

| Value | Credential | Notes |
|-------|------------|-------|
| `browser` (default) | `InteractiveBrowserCredential` | Opens a browser sign-in; tokens are cached between runs |
| `devicecode` | `DeviceCodeCredential` | Prints a code to enter on another device — use when no browser is available (SSH, containers) |
| `azcli` | `AzureCliCredential` | Reuses an existing `az login` session — useful for CI/CD and scripted runs |

Values are case-insensitive. An unknown value fails at parse time and lists the valid ones.

## Configuration

### JSON Configuration (appsettings.json)

Create an `appsettings.json` file in your working directory:

```json
{
  "DataverseUrl": "https://your-org.crm4.dynamics.com",
  "DataverseCredentialType": "browser",
  "XrmMockup": {
    "Metadata": {
      "OutputDirectory": "./Metadata",
      "Solutions": ["MySolution", "AnotherSolution"],
      "Entities": ["account", "contact", "opportunity"],
      "SecurityRoles": ["System Administrator", "Basic User"],
      "AllSecurityRoles": false,
      "PrettyPrint": false
    }
  }
}
```

#### Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `OutputDirectory` | string | `"./Metadata"` | Directory where metadata files will be written |
| `Solutions` | string[] | `[]` | Solution unique names to extract entities from |
| `Entities` | string[] | `[]` | Additional entity logical names to include |
| `SecurityRoles` | string[] | not set | Security role names to include (see [Security Role Filtering](#security-role-filtering)) |
| `AllSecurityRoles` | bool | `false` | Include all security roles regardless of other filtering |
| `PrettyPrint` | bool | `false` | Format XML output for readability (increases file size) |

`DataverseUrl` and `DataverseCredentialType` sit at the root of the file, not under `XrmMockup:Metadata`,
because they are read by DataverseConnection rather than by this tool.

### Environment-Specific Configuration

The tool supports environment-specific configuration files. Set the `DOTNET_ENVIRONMENT` environment variable and create a matching config file:

- `appsettings.json` - Base configuration (always loaded)
- `appsettings.Development.json` - Loaded when `DOTNET_ENVIRONMENT=Development`
- `appsettings.Production.json` - Loaded when `DOTNET_ENVIRONMENT=Production`

Environment-specific files override values from the base configuration.

## CLI Options

All CLI options override corresponding values from the configuration file.

```
xrmmockup-metadata [options]

Options:
  -o, --output <path>        Output directory for generated metadata files
  -s, --solutions <names>    Comma-separated list of solution unique names
  -e, --entities <names>     Comma-separated list of additional entity logical names
  -r, --security-roles <names>  Comma-separated list of security role names to include
  -a, --all-security-roles   Include all security roles regardless of filtering
  -c, --config <path>        Path to appsettings.json configuration file
  -p, --pretty-print         Format XML output for readability
  -u, --dataverse-url <url>  Dataverse environment URL
  -t, --credential-type <browser|devicecode|azcli>
                             Azure credential used to authenticate (default: browser)
  --help                     Show help information
  --version                  Show version information
```

### Examples

Generate metadata for a specific solution:

```bash
xrmmockup-metadata --solutions "MySolution"
```

Generate metadata with custom output directory:

```bash
xrmmockup-metadata -o "./tests/Metadata" -s "MySolution,CoreSolution"
```

Include additional entities not in any solution:

```bash
xrmmockup-metadata -s "MySolution" -e "account,contact,lead"
```

Use a specific configuration file:

```bash
xrmmockup-metadata --config "/path/to/appsettings.json"
```

Include specific security roles:

```bash
xrmmockup-metadata -r "System Administrator,Basic User"
```

Include all security roles (useful when solutions contain no roles):

```bash
xrmmockup-metadata -s "MySolution" --all-security-roles
```

Enable pretty-printed XML output:

```bash
xrmmockup-metadata --pretty-print
```

Authenticate with a device code instead of a browser (e.g. over SSH):

```bash
xrmmockup-metadata --credential-type devicecode
```

Reuse an existing Azure CLI session and target a specific environment:

```bash
xrmmockup-metadata -u "https://your-org.crm4.dynamics.com" -t azcli
```

## Security Role Filtering

Security role inclusion depends on the combination of `Solutions`, `SecurityRoles`, and `AllSecurityRoles`:

| `AllSecurityRoles` | `Solutions` | `SecurityRoles` | Result |
|--------------------|-------------|-----------------|--------|
| `true` | any | any | **All roles** |
| `false` | none | not set | **All roles** (backward compatible default) |
| `false` | none | `[]` (empty) | **No roles** |
| `false` | none | `["X", ...]` | **Only named roles** |
| `false` | specified | not set or `[]` | **Solution roles only** (may be empty) |
| `false` | specified | `["X", ...]` | **Solution roles + named roles** |

When `SecurityRoles` is not set (omitted from config), the behavior depends on whether solutions are specified. If no solutions are specified, all roles are included for backward compatibility. If solutions are specified, only roles within those solutions are included.

Setting `SecurityRoles` to an empty array explicitly opts out of additional roles. Use `AllSecurityRoles` to force inclusion of all roles regardless of other settings.

## Default Entities

The following entities are always included regardless of solution or entity configuration:

- `businessunit`
- `systemuser`
- `transactioncurrency`
- `role`
- `systemuserroles`
- `team`
- `teamroles`
- `activitypointer`
- `roletemplate`
- `fileattachment`

These entities are required for XrmMockup's core functionality (security model, user management, etc.).

## Generated Files

The tool generates the following files in the output directory:

| File | Contents |
|------|----------|
| `Metadata.xml` | Serialized MetadataSkeleton containing entity metadata, option sets, plugins, currencies, and organization settings |
| `Workflows/` | Individual XML files for each workflow definition |
| `SecurityRoles/` | Individual XML files for each security role with privilege definitions |
| `TypeDeclarations.cs` | C# source file with security role GUIDs for use in tests |

## Using with XrmMockup

Configure XrmMockup to use the generated metadata:

```csharp
var settings = new XrmMockupSettings
{
    MetadataDirectoryPath = "./Metadata",
    // ... other settings
};

using var crm = XrmMockup365.GetInstance(settings);
```

## CI/CD Integration

The default `browser` credential cannot work unattended, so pipelines should select a
non-interactive credential. Sign in with the Azure CLI first, then pass `--credential-type azcli`
(or set `DataverseCredentialType`). See the
[DataverseConnection](https://github.com/context-and-oss/DataverseConnection#configuration)
documentation for the full set of credential options.

```yaml
# Azure DevOps example
- task: AzureCLI@2
  displayName: 'Generate XrmMockup Metadata'
  inputs:
    azureSubscription: $(ServiceConnection)
    scriptType: 'bash'
    scriptLocation: 'inlineScript'
    inlineScript: |
      dotnet tool run xrmmockup-metadata \
        -o ./tests/Metadata \
        -s "$(SolutionName)" \
        -u "$(DataverseUrl)" \
        -t azcli
```

```yaml
# GitHub Actions example
- uses: azure/login@v2
  with:
    client-id: ${{ secrets.AZURE_CLIENT_ID }}
    tenant-id: ${{ secrets.AZURE_TENANT_ID }}
    subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
- name: Generate XrmMockup Metadata
  env:
    DataverseUrl: ${{ secrets.DATAVERSE_URL }}
    DataverseCredentialType: azcli
  run: dotnet tool run xrmmockup-metadata -o ./tests/Metadata -s "${{ vars.SOLUTION_NAME }}"
```

## Troubleshooting

### Authentication Issues

- Verify `DataverseUrl` is correct and accessible
- If no browser can be opened (SSH session, container, CI agent), switch credential: `--credential-type devicecode` or `--credential-type azcli`
- See [DataverseConnection](https://github.com/context-and-oss/DataverseConnection#configuration) documentation for authentication troubleshooting
- Ensure the authenticating user/service principal has the System Administrator or System Customizer security role

### Missing Entities

- Verify the entity is included in one of the specified solutions, or add it explicitly to the `Entities` list
- Check that the authenticating user has read access to the entity metadata

### Large Metadata Files

- Keep `PrettyPrint` disabled (default) for production use
- Consider limiting the number of solutions/entities to only those needed for testing
