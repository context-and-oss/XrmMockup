# Static permission checks in XrmMockup tests

Permission checks analyze source code and compare the privileges it may need with the mockup's
current role definitions. **They do not run a plugin or an organization request, change mockup
state, or prove record-level access.** Ownership, sharing and access depth are not simulated by this check:
as in the standalone analyzer, any granted privilege depth counts.

## Setup

Reference only the existing `XrmMockup365` package. Enable source loading explicitly in the
.NET 10 test project:

```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'net10.0'">
  <XrmMockupEnablePermissionAnalysis>true</XrmMockupEnablePermissionAnalysis>
</PropertyGroup>
```

There are **no additional NuGet packages and no dotnet tool installation is required**. The
permission contracts, analyzer engine and workspace loader remain internal shared library
projects. `XrmMockup365` ships the lightweight .NET Standard 2.0 contracts in its normal library
assets for all three supported target frameworks. It also carries the library-only .NET 10 analyzer
payload and its dependencies in a private folder. The opt-in target copies that payload into
build/publish output; ordinary Core consumers do not copy or load it. Roslyn/MSBuild run in a
separate load context so they do not change the host's workflow dependencies.

The standalone Permission Analyzer package ships the same shared libraries alongside its own
CLI. Neither dotnet tool is bundled into Core, and the Metadata Generator remains unchanged.
Only the existing three product packages are packed/published, so the selected-product release
flow still uses one package/symbol pair per product. Shared contracts have a stable assembly
identity; internal assemblies do not introduce independently versioned NuGet dependencies.

For local packages, build the solution and use `scripts/Pack-Local.ps1`. Core packing expects the
shared workspace library output from that solution build; `pack --no-build` does not rebuild it.
Repository/project-reference tests reference the internal contracts project directly and pass
`XrmMockupEnablePermissionAnalysis` to their Core project reference; packaged consumers need
only the property above.

Core supports .NET Framework 4.6.2 and 4.8. Checks and result contracts are available on both,
but loading source requires .NET 10. No Roslyn/MSBuild dependencies are added to normal Core
PackageReferences on any supported target framework.

Install the .NET 10 SDK with `dotnet` on `PATH`, and restore/build the analyzed project before loading it. Load once in the test fixture and reuse it:

```csharp
using DG.Tools.XrmMockup;
using XrmMockup.PermissionAnalyzer;
using XrmPluginCore.Enums;

fixture.PermissionAnalysis = await PermissionAnalysis.LoadAsync(projectOrSolutionPath);

crm.CheckPermissions(
    fixture.PermissionAnalysis,
    PermissionSelector.Operation<Account>(EventOperation.Create),
    SecurityRoles.XrmMockupTestAccessTeam)
    .ThrowIfFailed();
```

The loader reads `appsettings.json` next to the analyzed project/solution, when present, using
its existing `XrmMockup:Permissions` settings for rules, metadata, excluded projects, explicit
entry methods and plugin solutions. Project references are loaded too. It disposes the workspace
once the compilations and source summaries are loaded. Source graphs are reused; if the current
mockup's lookup catalog changes, a new metadata-dependent source graph is extracted from the
already-loaded compilations and reused. Reload the analysis if the source code changes.

## Select a scope

```csharp
// All registered steps of this exact plugin type and assembly:
PermissionSelector.Plugin<OrderPlugin>();

// Narrow to a message and stage; omitted filters mean all registered steps:
PermissionSelector.Plugin<OrderPlugin>(EventOperation.Update, ExecutionStage.PostOperation);
PermissionSelector.Plugin<OrderPlugin>("custom_message", ExecutionStage.PostOperation);

// Synthetic operations include direct privileges even when no plugin is registered:
PermissionSelector.Operation<Account>(EventOperation.Create);
PermissionSelector.Operation<Account>(EventOperation.Update, account => account.Name);
PermissionSelector.Operation<Account>("custom_message");
```

Entity and attribute logical names come from SDK `EntityLogicalName` and `AttributeLogicalName`
attributes. Attribute expressions must select direct entity properties, not computations or nested
members. Specified attributes determine Update filtering and lookup Append/AppendTo privileges.
Omitted attributes mean unknown attributes, conservatively including filtered Update steps.
Entity operations include applicable plugin steps at all stages, asynchronous steps and recursive
cascades. The mockup's currently registered workflows and registered plugin code that was not loaded are reported as incomplete.

## Choose roles or a user

Pass generated `SecurityRoles` GUID members, not display names or sanitized generated member
names. Multiple IDs combine their privileges. Unknown IDs and empty explicit lists are rejected.

```csharp
var result = crm.CheckPermissions(analysis, scope, SecurityRoles.RoleA, SecurityRoles.RoleB);
var forUser = crm.CheckPermissions(analysis, scope, user.ToEntityReference());
```

The user overload requires an existing local `systemuser` and reads current direct roles and
roles of teams the user belongs to. Role definitions and assignments are read anew for every
check, so changes between checks take effect. Existing results remain snapshots.

Explicit impersonating users are checked separately using their own current direct/team roles.
Unknown impersonating users and identities whose roles cannot be established (including an
unspecified `CallerId` identity) are not silently checked against the primary caller's roles.
SYSTEM operations retain the analyzer's no-role-required behavior.

## Inspect or assert results

`result.Passed`, `result.Manifest` and `result.Coverage` expose the same manifest and coverage
engine used by the CLI. `ThrowIfFailed()` throws a framework-neutral `PermissionCheckException`
with the result attached when privileges are missing **or the analysis is incomplete**: unresolved
operations, unchecked identities, unavailable global privilege data, source compilation errors or
call-limit truncation. Diagnostics identify entry points, roles, identities and source locations.
There is no permissive SDK mode or suppression option.

The standalone `xrmmockup-permissions` CLI retains its existing options and reporting behavior.
Checks are explicit only: no automatic CRUD validation, runtime tracing, live Dataverse lookups
or role suggestions are performed.
