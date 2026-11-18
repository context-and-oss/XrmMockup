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
- `tests/TestEnvProvisioner/`: live test-environment provisioning; not needed for ordinary local tests.

## Build and validation

Run commands from the repository root. CI (`.github/workflows/ci.yml`) runs on
Windows with .NET SDKs 8 and 10. The engine, engine tests, and test plugin assembly
multi-target `net462` and `net10.0`; the modern metadata projects target `net10.0`.
The legacy generator targets `net462`. Keep engine changes compatible with both
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
```

A focused `net10.0` run does not replace full Windows/multi-target validation.
For code changes, run the affected tests first and the full suite when available;
report any SDK, platform, restore, or test limitations rather than claiming validation passed.

Local package preview (PowerShell; builds and packs both products without publishing):

```powershell
./scripts/Pack-Local.ps1
```

For release automation changes, also run `./scripts/release/Test-Release.ps1` in
PowerShell. See `RELEASE.md` for PR product/category classification and release
procedures. Do not hand-edit generated pending changelog sections or substitute
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
