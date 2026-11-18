# Repository instructions

This is the shared instruction file for coding agents. Keep repository guidance here;
`CLAUDE.md` imports this file rather than maintaining a separate copy.

## Project and code map

XrmMockup simulates Microsoft Dynamics 365/Dataverse locally so business logic,
plugins, workflows, custom APIs, and security can be tested without a live environment.

- `src/XrmMockup365/`: simulation library (`DG.Tools.XrmMockup`).
  - `XrmMockup.cs`: `XrmMockup365.GetInstance(settings)` entry point and shared metadata cache.
  - `XrmMockupBase.cs` / `XrmMockupBaseAsync.cs`: public service and environment APIs.
  - `Core.cs`: request dispatch, security checks, and plugin/workflow execution.
  - `Requests/`: CRM request handlers derived from `RequestHandler`.
  - `Plugin/`: execution and registration strategies, including DAXIF, non-DAXIF,
    and direct registration via `XrmMockupSettings.IPluginMetadata`.
  - `Workflow/`, including `WorkflowNode/`: workflow parsing and execution.
  - `XrmMockupSettings.cs`: authoritative settings types and defaults; inspect it
    before changing configuration or writing examples.
- `src/PermissionAnalyzer/`: static Dataverse privilege analysis shared by the SDK and
  standalone `xrmmockup-permissions` tool.
  - `PermissionAnalyzer.Abstractions/`: SDK-free models, scopes, results, and role coverage
    (`netstandard2.0`).
  - `PermissionAnalyzer.Core/`: Roslyn operation extraction, call graphs, cascades, and
    manifest building (`netstandard2.0`).
  - `PermissionAnalyzer.Workspace/`: opt-in .NET 10 source loading, configuration, and
    metadata readers using private wire contracts.
  - `PermissionAnalyzer.Tool/`: CLI frontend, packaged as `XrmMockup.PermissionAnalyzer`.
  - `PermissionAnalyzer.Tests/`: snippets compiled in memory against the real SDK and
    analyzer/loader regression tests.
- `src/ToolsCommon/`: shared CLI infrastructure, including console log formatting.
  This and the permission libraries are internal/non-packable projects; owning product
  packages ship their assemblies, not additional independently released NuGet packages.
- `src/MetadataSkeleton/`: serialized metadata model shared by the engine and generators.
- `src/MetadataGen/`:
  - `MetadataGenerator365/`: legacy .NET Framework generator, still bundled in the core package.
  - `MetadataGenerator.Core/`: modern metadata readers and generation logic.
  - `MetadataGenerator.Context/`: early-bound types used by the modern generator.
  - `MetadataGenerator.Tool/`: CLI, packaged as `XrmMockup.MetadataGenerator`
    with command name `xrmmockup-metadata`.
  - `MetadataGenerator.Tool.Tests/`: xUnit tests, including readers tested against XrmMockup.
- `tests/XrmMockup365Test/`: engine tests.
- `tests/TestPluginAssembly365/`: test plugins, activities, and early-bound context types.
- `tests/TestPluginAssembly462/`: .NET Framework 4.6.2-only plugins for compatibility regression tests.
- `tests/TestEnvProvisioner/`: live test-environment provisioning; not needed for ordinary local tests.

## Build and validation

Run commands from the repository root. CI (`.github/workflows/ci.yml`) runs on
Windows with .NET SDK 10. The engine targets `net462`, `net48`, and `net10.0`;
the engine tests and main test plugin assembly target `net48` and `net10.0`. The
compatibility test plugin assembly and legacy generator target `net462`; the modern
metadata projects target `net10.0`. Keep engine changes compatible with all three
targets and preserve `DATAVERSE_SERVICE_CLIENT` conditional code where SDK APIs differ.

```bash
# Match CI validation
dotnet restore XrmMockup.slnx
dotnet build XrmMockup.slnx --configuration Release --no-restore
dotnet test XrmMockup.slnx --configuration Release --no-build

# Focused engine tests (net10.0 avoids trying to run .NET Framework tests on non-Windows)
dotnet test tests/XrmMockup365Test/XrmMockup365Test.csproj --framework net10.0 --filter "FullyQualifiedName~TestCreate"

# Metadata generator tests
dotnet test src/MetadataGen/MetadataGenerator.Tool.Tests/MetadataGenerator.Tool.Tests.csproj

# Permission analyzer tests
dotnet test src/PermissionAnalyzer/PermissionAnalyzer.Tests/PermissionAnalyzer.Tests.csproj

# Permission SDK and legacy-plugin compatibility regressions
dotnet test tests/XrmMockup365Test/XrmMockup365Test.csproj --framework net10.0 --filter "FullyQualifiedName~TestPermissionAnalysis|FullyQualifiedName~TestPluginFrameworkCompatibility"
```

A focused `net10.0` run does not replace full Windows/multi-target validation.
For code changes, run the affected tests first and the full suite when available;
report any SDK, platform, restore, or test limitations rather than claiming validation passed.

Local package preview (PowerShell; builds and packs all three products without publishing):

```powershell
./scripts/Pack-Local.ps1
```

For release automation changes, also run `./scripts/release/Test-Release.ps1` in
PowerShell. See `RELEASE.md` for PR product/category classification and release
procedures. Product tags (`core-v*`, `metadata-v*`, `permissions-v*`) select one
package/symbol pair. `scripts/release/Prepare-Release.ps1` supplies its version and
notes through `.release/version.props`; ordinary builds use development versions.
Do not hand-edit generated pending changelog sections or substitute
ad hoc version changes for the product-scoped release process.

## Test conventions

- Engine tests use `XrmMockupFixture` in `tests/XrmMockup365Test/XrmmockupFixture.cs`
  and normally derive from `UnitTestBase`, passing the fixture to its constructor.
- Fixtures share settings for metadata-cache reuse; `GetInstance(settings)` creates
  a fresh simulation per test. Preserve isolation of per-instance state when touching caching.
- `orgAdminService` is the admin SDK service; `orgAdminUIService` uses UI behavior;
  `orgGodService` bypasses security. Use the service appropriate to the behavior
  being tested, especially for permission regressions.
- Use early-bound entity types in tests where available (for example,
  `new Account { Name = "Test" }`). Use late-bound `Entity` only when the test
  specifically requires it or no generated type exists.
- Add regression coverage in the affected suite. Metadata reader tests normally use
  `Fixtures/ReaderTestBase.cs` and `Fixtures/XrmMockupFactory.cs` in the tool test project.

## Static permission analysis

Checks are explicit static privilege analysis, not scenario execution or proof of
record-level access. Do not add automatic checks during CRUD execution, invoke the CLI
from the SDK, or mutate mockup state when checking permissions. Preserve current
user/team role resolution, separate impersonated identities, and strict failures for
missing privileges or incomplete analysis.

The .NET 10 SDK test project enables `XrmMockupEnablePermissionAnalysis` to copy the
shared library payload; it is loaded on demand in an isolated load context. The
lightweight contracts/check APIs remain available on .NET Framework, but source
loading requires .NET 10. Do not bundle either dotnet tool into Core or introduce
independently released permission-library packages.

Analyzer output must remain deterministic: use ordinal comparers, sort collections,
and write `\n` line endings. See `docs/design/permission-analyzer.md` for the engine
design and `docs/permission-analysis-sdk.md` for SDK setup and boundaries. CLI
configuration uses `XrmMockup:Permissions`; consult
`src/PermissionAnalyzer/PermissionAnalyzer.Tool/README.md` rather than duplicating
its options here.

## Metadata and live-environment boundaries

Ordinary tests use checked-in metadata: `tests/XrmMockup365Test/Metadata/` and
`src/MetadataGen/MetadataGenerator.Tool.Tests/TestMetadata/`. Do not regenerate
metadata, context classes, or provision a live Dataverse environment as a routine test step.

When regeneration is explicitly needed, `tests/Regenerate-TestMetadata.ps1`
uses `tests/appsettings.json` to regenerate engine-test metadata and context classes;
`-ContextOnly` and `-MetadataOnly` limit its scope. This requires live Dataverse
authentication and is separate from the tool tests' `TestMetadata` assets.
Review generated diffs and do not commit credentials or tokens.

For CLI configuration, authentication, and generation procedures, consult
`src/MetadataGen/MetadataGenerator.Tool/README.md` and the scripts under
`src/MetadataGen/scripts/` rather than duplicating their reference material here.
