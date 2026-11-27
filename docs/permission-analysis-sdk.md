# Static permission checks in XrmMockup tests

Checks read source and compare required privileges with the mockup's current roles.
They **do not execute business logic, mutate state or prove record-level access**.
Any granted depth counts; ownership, sharing and field-level security are not proved.
See [the design](design/permission-analyzer.md) for architecture and analysis boundaries.

## Setup

Reference the existing `XrmMockup365` package and opt in from the .NET 10 test project:

```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'net10.0'">
  <XrmMockupEnablePermissionAnalysis>true</XrmMockupEnablePermissionAnalysis>
</PropertyGroup>
```

No extra NuGet package or dotnet tool is needed. Contracts/check APIs are available on
.NET Framework 4.6.2 and 4.8, but source loading requires the .NET 10 SDK on `PATH`.
The opt-in payload loads Roslyn/MSBuild in an isolated context; ordinary consumers do
not copy or load it. Project-reference tests must pass the property to their Core reference.

Restore/build trusted analyzed projects first. MSBuild design-time loading can execute
project tasks and generators; do not analyze untrusted projects in a job with secrets.
Load once in the fixture and reuse the source graph:

```csharp
using DG.Tools.XrmMockup;
using XrmMockup.PermissionAnalyzer;
using XrmPluginCore.Enums;

fixture.PermissionAnalysis = await PermissionAnalysis.LoadAsync(projectOrSolutionPath);

crm.CheckPermissions(
    fixture.PermissionAnalysis,
    PermissionSelector.Operation<Account>(EventOperation.Create, account => account.Name),
    SecurityRoles.XrmMockupTestAccessTeam)
    .ThrowIfFailed();
```

The loader uses `appsettings.json` beside the analyzed project/solution for rules,
metadata, exclusions, explicit entries and plugin projects, and loads project references.
The workspace is disposed after extraction. Source graphs are reused/re-extracted when
check-time lookup metadata changes; reload the analysis when source changes.
For local package previews use `scripts/Pack-Local.ps1` after a solution build.

## Scope and roles

```csharp
PermissionSelector.Plugin<OrderPlugin>(); // Exact type and assembly; all registered steps.
PermissionSelector.Plugin<OrderPlugin>(EventOperation.Update, ExecutionStage.PostOperation);
PermissionSelector.Plugin<OrderPlugin>("custom_message", ExecutionStage.PostOperation);
PermissionSelector.Operation<Account>(EventOperation.Update, account => account.Name);

var result = crm.CheckPermissions(analysis, scope, SecurityRoles.RoleA, SecurityRoles.RoleB);
var forUser = crm.CheckPermissions(analysis, scope, user.ToEntityReference());
```

Entity/column names come from SDK logical-name attributes. Expressions must select direct
entity properties. Specified columns control Update filtering and lookup requirements;
metadata must match the environment. Omitted columns are unknown: filtered steps are included
and synthetic writes report unresolved lookup contents. A low-level
`PermissionScope.ForOperation` with an explicit empty column list means no columns.

Operations include direct privileges, registered synchronous/asynchronous steps and recursive
cascades. Missing plugin source and fired workflows whose bodies are not analyzed are incomplete.

Pass generated role GUIDs, not display names. Empty/unknown role IDs and invalid local users
are rejected. Roles and direct/team assignments are read anew for each check; old results remain
snapshots. Explicit role lists represent direct assignments. Explicit impersonating users use
their own roles, never the primary caller's. Unknown identities remain unchecked.

`CallerId` requires operation privileges on both identities, plus directly assigned delegation
on the application user; team roles contribute other privileges, not delegation. An unspecified
end-user identity cannot be covered by the primary caller. SYSTEM needs no role coverage.
Missing table/global privilege metadata is unverifiable, not a security exemption.

## Results

`result.Manifest`, `result.Coverage` and `result.Passed` expose the shared analyzer result.
`ThrowIfFailed()` throws `PermissionCheckException` with the result attached for missing
privileges **or incompleteness**: unresolved operations/lookup contents, unchecked identities,
unverifiable metadata, source loading/compilation errors or call/resolution-depth limits.
Diagnostics identify roles, identities and source locations.

There is no permissive SDK mode, suppression option, automatic CRUD check, runtime tracing,
live role lookup or role suggestion. CLI behavior and configuration are documented in the
[CLI reference](permission-analyzer-reference.md).
