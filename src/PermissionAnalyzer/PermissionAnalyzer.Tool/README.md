# XrmMockup Permission Analyzer

A .NET CLI tool that works out which Dataverse table privileges your code needs, by static analysis.
It finds your entry points (plugin steps, custom APIs, code activities and Azure Functions), follows
the calls from each one through your code without running any of it, and lists the privilege each
`IOrganizationService` call needs on each table, together with the identity it runs as. It is a companion to
[XrmMockup](https://github.com/delegateas/XrmMockup) and reads the security roles that
`xrmmockup-metadata` downloads.

> **Preview.** This release lists the privileges each entry point needs. Checking them against
> security roles (the `check` command), plugin cascades and `Execute` requests are coming next.

## Installation

```bash
dotnet tool install --global XrmMockup.PermissionAnalyzer
```

Or locally in your repository:

```bash
dotnet new tool-manifest # if you don't have a manifest yet
dotnet tool install XrmMockup.PermissionAnalyzer
```

## Quick start

From the folder with your solution and `appsettings.json`:

```bash
xrmmockup-permissions required
```

Example output:

```
plugin:Contoso.Plugins.AccountPostPlugin:Update:account:PostOperation  src/Plugins/AccountPostPlugin.cs:14
  account   Read    Caller  src/Plugins/AccountPostPlugin.cs:20
  contact   Create  Caller  src/Services/ContactService.cs:31 (AccountPostPlugin.ctor (lambda, line 16) → ContactService.CreateFor)
  task      Create  System  src/Plugins/AccountPostPlugin.cs:44
  ? Update [Write] as Caller: entity comes from InputParameters["Target"]  src/Plugins/AccountPostPlugin.cs:40

Privileges per table
  account   Read
  contact   Create
  task      Create

1 entry point, 0 unreached, 3 tables, 3 requirements, 1 unresolved
```

Each entry point lists its requirements with the identity they run as (see [Runs as](#runs-as)) and
where the call is. A call in another method shows the call path from the entry point after it.

- Lines starting with `?` are calls whose table can't be determined statically. They are listed with
  the reason instead of being dropped, so the result never hides a call.
- Lines starting with `~` are assumptions the analysis had to make, such as the identity of a
  service passed in from outside.
- `Not reached from any entry point` lists methods that make service calls but that no entry point
  calls. Add them with `--entry` or `EntryPoints` if something outside the solution calls them.

## Commands

### `required`

Lists the table privileges each entry point needs.

| Option | Alias | Description |
|--------|-------|-------------|
| `--solution` | `-s` | Solution (`.sln`/`.slnx`) to analyze |
| `--project` | `-p` | A single project (`.csproj`) to analyze, together with the projects it references. Can't be combined with `--solution` |
| `--metadata` | `-m` | XrmMockup metadata folder with `SecurityRoles/` |
| `--exclude` | `-x` | Leave out projects matching a pattern (see [Excluding projects](#excluding-projects)). Repeatable |
| `--config` | `-c` | Path to `appsettings.json` (default: the current directory) |
| `--format` | `-f` | `text` (default) or `json` |
| `--output` | `-o` | Write to a file instead of standard output |
| `--plugin` | | Only one plugin, or one of its steps: `<type>[:<message>[:<stage>]]`, such as `AccountPostPlugin:Update` |
| `--custom-api` | | Only one custom API, by unique name |
| `--function` | | Only one Azure Function, by function name |
| `--entry` | | Analyze a method as an entry point, such as `Contoso.Sync.Importer.Run`, and show only that. Repeatable |

Use at most one of `--plugin`, `--custom-api`, `--function` and `--entry`. Without any, every entry
point is listed. A scope that matches nothing is an error. Type names may be simple or full and are
matched case-insensitively.

Exit codes: `0` on success, `2` on errors (invalid options, missing files, or a solution that fails to load).

## Entry points

| Kind | Found from |
|------|------------|
| Plugin step | `RegisterPluginStep<T>`, `RegisterStep<T, TService>` (lambda or `nameof(TService.Method)`) in [XrmPluginCore](https://github.com/delegateas/XrmPluginCore) plugin constructors |
| Plugin step (metadata) | Plugin steps in `Metadata/Metadata.xml`, for `IPlugin` classes that register no steps in code. Without metadata, such a class is one entry point for its `Execute` |
| Custom API | `RegisterCustomAPI` and `RegisterAPI<TService>` |
| Code activity | Subclasses of `CodeActivity` |
| Azure Function | Methods with `[Function]` (isolated worker) or `[FunctionName]` (in-process) |
| Method | `--entry` and the `EntryPoints` setting |

Entry point ids look like `plugin:Contoso.AccountPostPlugin:Update:account:PostOperation` (the table
is `any` for steps on every table), `custom-api:ctx_DoThing`, `activity:Contoso.MyActivity`,
`function:ProcessOrder` and `method:Contoso.Sync.Importer.Run(IOrganizationService)`.

From each entry point, the tool follows calls into the rest of the solution, including across
projects. Services and tables passed as arguments, stored in fields or set through constructors are
tracked into the called methods. Interface calls go to the implementation registered with dependency
injection (`AddScoped<IFoo, Foo>()` and similar, including XrmPluginCore's
`OnBeforeBuildServiceProvider`). Without a registration they go to every implementation in the
solution, and the requirement is marked `all-implementations`.

## Runs as

Dataverse checks privileges against the user a service runs as, so each requirement has one:

| Runs as | When |
|---------|------|
| `Caller` | The user who triggered the plugin, custom API or workflow: `OrganizationService`, `CreateOrganizationService(context.UserId)` |
| `User` | A specific user: `CreateOrganizationService(<constant guid>)`, `SetUserContext(...)` on a step, or the step's impersonating user from metadata |
| `ApplicationUser` | Azure Functions and other app code: `ServiceClient`, injected `IOrganizationService` |
| `ImpersonatedCaller` | App code that sets `CallerId` |
| `System` | `OrganizationAdminService`, `CreateOrganizationService(null)`. No role privilege is needed |

When the identity can't be determined, for example for a service passed in from outside the
analyzed code, the requirement is marked `(assumed)` and the assumption is listed with `~`.

## What is analyzed

Calls on `IOrganizationService`, `IOrganizationServiceAsync`/`Async2` and their implementations
(for example `ServiceClient`), including async forms:

| Call | Privileges |
|------|------------|
| `Create` | Create |
| `CreateAndReturn` | Create, Read |
| `Retrieve`, `RetrieveMultiple` | Read on the table and every linked table |
| `Update` | Write |
| `Delete` | Delete |
| `Associate`, `Disassociate` | Read, AppendTo on the target; Read, Append on the related records |
| `Execute` | Reported as unresolved for now |

The table is found from:

- early-bound types (`[EntityLogicalName]`), such as `new Account()`
- constant strings, such as `new Entity("account")` or `Account.EntityLogicalName`
- `QueryExpression`, `QueryByAttribute`, `AddLink` and FetchXML (including interpolated strings)
- locals assigned in the same method, including both branches of `?:` and `??`
- loops over the result of `RetrieveMultiple`

Calls inside lambdas and local functions count towards the method that contains them. Generic
helpers whose table is a type parameter are reported as unresolved for now.

## Configuration

The tool reads the same `appsettings.json` as `xrmmockup-metadata`. Its settings are under
`XrmMockup:Permissions`:

```json
{
  "XrmMockup": {
    "Metadata": {
      "OutputDirectory": "./Metadata"
    },
    "Permissions": {
      "Solution": "./src/Contoso.sln",
      "MetadataDirectory": "./tests/Metadata",
      "ExcludeProjects": ["*.Tests", "tests/*"],
      "EntryPoints": ["Contoso.Sync.Importer.Run"]
    }
  }
}
```

| Setting | Description |
|---------|-------------|
| `Solution` | Solution or project to analyze. Without it, the tool uses the single `.sln`/`.slnx` in the current directory |
| `MetadataDirectory` | Metadata folder. Defaults to `XrmMockup:Metadata:OutputDirectory`, then `./Metadata` |
| `ExcludeProjects` | Projects to leave out, such as test projects. See below |
| `EntryPoints` | Extra methods to analyze as entry points, by full name, such as code called from outside the solution. `--entry` adds to these. A name that matches no method is reported as a warning |

Configured paths are relative to the configuration file. `appsettings.{DOTNET_ENVIRONMENT}.json` and
environment variables (for example `XrmMockup__Permissions__Solution`) override the file, and
command-line options override everything.

### Excluding projects

Test projects call the organization service too, but those calls aren't what your users need
privileges for. Leave them out with patterns in `ExcludeProjects`, `--exclude`, or both; the
command line adds to the configured patterns:

```bash
xrmmockup-permissions required -x "*.Tests" -x "tests/*"
```

- `*` matches any characters and `?` matches one. Matching is case-insensitive.
- A pattern without a slash matches the project name, which is the file name without `.csproj`: `*.Tests`, `Contoso.IntegrationTests`.
- A pattern with a slash matches the project path relative to the solution: `tests/*`.
- A pattern that matches no project is reported as a warning, because it's probably a typo.

Excluded projects are not compiled, so excluding them also makes the run faster.

## Security roles

The metadata folder is also where plugin step registrations are read from (`Metadata.xml`).

When the metadata folder contains `SecurityRoles/`, the output also lists the required tables that no
role mentions. XrmMockup doesn't check privileges on those tables, so tests pass even when no role
grants them.

## Output

`--format json` writes the same content as an object with `entryPoints` (each with its `id`, `kind`,
`trigger`, `requirements`, `unresolved` and `assumptions`), `unreached` and `notRoleGoverned`.
Requirements carry `table`, `privilege`, `runsAs`, `basis` and their `sources`, each with the call
`path` from the entry point.

Output is deterministic: entry points, methods, tables and source locations are sorted ordinally, paths are
relative to the analyzed solution with forward slashes, and line endings are always `\n`. Running
the tool twice on the same code gives byte-identical output, so it can be committed and diffed in CI.
