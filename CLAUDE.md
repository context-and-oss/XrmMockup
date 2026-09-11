# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

XrmMockup is a simulation engine that mocks Microsoft Dynamics 365/CRM instances locally. It enables testing business logic including plugins, workflows, custom APIs, and security roles without connecting to a live CRM environment.

## Build and Test Commands

```bash
# Build the solution (debug)
dotnet build XrmMockup.slnx

# Build for release (uses FAKE build script)
build.cmd Build

# Run all tests
dotnet test

# Run a specific test
dotnet test --filter "FullyQualifiedName~TestCreate"

# Run tests for a specific class
dotnet test --filter "FullyQualifiedName~DG.XrmMockupTest.TestWorkflow"

# Create NuGet package (uses FAKE build script)
build.cmd NuGet
```

## Architecture

### Core Components

- **XrmMockup365** (`src/XrmMockup365/XrmMockup.cs`) - Main entry point. Creates mockup instances via `XrmMockup365.GetInstance(settings)`. Uses static metadata caching for performance across tests.

- **XrmMockupBase** (`src/XrmMockup365/XrmMockupBase.cs`) - Abstract base class containing core mockup functionality: organization service creation, user/team management, security role handling, and database operations.

- **Core** (internal) - The simulation engine that processes all CRM requests, enforces security, and triggers plugins/workflows.

- **MetadataSkeleton** (`src/MetadataSkeleton/MetadataSkeleton.cs`) - Holds entity metadata, option sets, plugins, currencies, and default state/status mappings loaded from serialized files.

### Request Handlers

Located in `src/XrmMockup365/Requests/`. Each handler processes a specific CRM request type (Create, Update, Retrieve, etc.). Handlers extend `RequestHandler` abstract class.

### Plugin Execution

- `src/XrmMockup365/Plugin/` - Plugin execution infrastructure
- Supports multiple registration strategies: DAXIF-based, non-DAXIF, direct IPlugin registration
- Configure via `XrmMockupSettings.BasePluginTypes` and `IPluginMetadata`

### Workflow Engine

Located in `src/XrmMockup365/Workflow/`. Parses and executes CRM workflow definitions. Workflow nodes in `WorkflowNode/` represent individual workflow steps.

### Test Structure

- **XrmMockupFixture** (`tests/XrmMockup365Test/XrmMockupFixture.cs`) - xUnit fixture providing shared `XrmMockupSettings` configuration
- **UnitTestBase** (`tests/XrmMockup365Test/UnitTestBase.cs`) - Base class for tests. Creates fresh XrmMockup instance per test with pre-configured services and test users

## Key Settings (XrmMockupSettings)

```csharp
var settings = new XrmMockupSettings {
    BasePluginTypes = new[] { typeof(MyPluginBase) },           // Plugin base types to scan
    BaseCustomApiTypes = new[] { ("prefix", typeof(MyApi)) },   // Custom API types
    CodeActivityInstanceTypes = new[] { typeof(MyActivity) },   // Workflow activities
    EnableProxyTypes = true,                                     // Enable early-bound types
    MetadataDirectoryPath = "path/to/Metadata",                 // Metadata folder location
    IPluginMetadata = new MetaPlugin[] { ... },                 // Direct plugin registration
    EnablePowerFxFields = true                                   // PowerFx formula field evaluation
};
```

## Target Frameworks

The solution multi-targets:
- `net462` - Uses Microsoft.CrmSdk assemblies
- `net10.0` - Uses Microsoft.PowerPlatform.Dataverse.Client (define `DATAVERSE_SERVICE_CLIENT`)

## Metadata Generation

There are two metadata generators:

- **MetadataGenerator365** (`src/MetadataGen/MetadataGenerator365/`) - Legacy generator, extracts metadata from a live CRM instance and serializes it to files.

- **MetadataGenerator.Tool** (`src/MetadataGen/MetadataGenerator.Tool/`) - New .NET tool-based generator with modern architecture:
  - `MetadataGenerator.Core` - Business logic and readers for entity metadata, plugins, workflows, security roles, option sets, currencies, and organization data
  - `MetadataGenerator.Tool` - CLI entry point, packaged as a .NET tool (`xrmmockup-metadata`)
  - `MetadataGenerator.Tool.Tests` - Integration tests using XrmMockup365. To regenerate test metadata and context classes, run: `.\scripts\Regenerate-TestMetadata.ps1` (use `-ContextOnly` to only regenerate context classes). Always use early-bound entity types in tests (e.g., `new Workflow { Name = "Test" }` instead of `new Entity("workflow") { ["name"] = "Test" }`).

Tests consume metadata files from a `Metadata/` directory. Configure metadata generation via `appsettings.json`:

```json
{
  "DataverseUrl": "https://org.crm4.dynamics.com",
  "DataverseCredentialType": "browser",
  "XrmMockup": {
    "Metadata": {
      "OutputDirectory": "./Metadata",
      "Solutions": ["MySolution"],
      "Entities": ["account", "contact"]
    }
  }
}
```

Connection settings (`DataverseUrl`, `DataverseCredentialType`) are owned by the
[DataverseConnection](https://github.com/context-and-oss/DataverseConnection#configuration) package,
sit at the root of the file, and also accept their legacy uppercase forms (`DATAVERSE_URL`,
`DATAVERSE_CREDENTIAL_TYPE`). Authentication defaults to an interactive browser sign-in; override it
per run with `--credential-type browser|devicecode|azcli` (`--dataverse-url` overrides the URL).

## Common Test Patterns

```csharp
public class MyTest : UnitTestBase, IClassFixture<XrmMockupFixture>
{
    public MyTest(XrmMockupFixture fixture) : base(fixture) { }

    [Fact]
    public void TestSomething()
    {
        // orgAdminService - admin SDK service
        // orgAdminUIService - admin UI service (triggers UI-only plugins)
        // orgGodService - bypasses security
        // crm - XrmMockup365 instance

        var account = new Account { Name = "Test" };
        account.Id = orgAdminService.Create(account);
    }
}
```
