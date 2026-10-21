# XrmMockup Permission Analyzer

A .NET CLI tool that works out which Dataverse table privileges your code needs, by static analysis.
It reads your plugins and other code that call `IOrganizationService`, without running any of it, and
lists the privilege each call needs on each table. It is a companion to
[XrmMockup](https://github.com/delegateas/XrmMockup) and reads the security roles that
`xrmmockup-metadata` downloads.

> **Preview.** This release lists the privileges each method needs for the service calls written
> directly in it. Entry points (plugins, custom APIs, Azure Functions), call graphs and the `check`
> command are coming next.

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
Contoso.Plugins.AccountPostPlugin.Execute(LocalPluginContext)  src/Plugins/AccountPostPlugin.cs:14
  account   Read    src/Plugins/AccountPostPlugin.cs:20
  contact   Create  src/Plugins/AccountPostPlugin.cs:31
  ? Update [Write]: entity comes from InputParameters["Target"]  src/Plugins/AccountPostPlugin.cs:40
```

Lines starting with `?` are calls whose table can't be determined statically. They are listed with
the reason instead of being dropped, so the result never hides a call.

## Commands

### `required`

Lists the table privileges each method needs.

| Option | Alias | Description |
|--------|-------|-------------|
| `--solution` | `-s` | Solution (`.sln`/`.slnx`) to analyze |
| `--project` | `-p` | A single project (`.csproj`) to analyze. Can't be combined with `--solution` |
| `--metadata` | `-m` | XrmMockup metadata folder with `SecurityRoles/` |
| `--exclude` | `-x` | Leave out projects matching a pattern (see [Excluding projects](#excluding-projects)). Repeatable |
| `--config` | `-c` | Path to `appsettings.json` (default: the current directory) |
| `--format` | `-f` | `text` (default) or `json` |
| `--output` | `-o` | Write to a file instead of standard output |

Exit codes: `0` on success, `2` on errors (invalid options, missing files, or a solution that fails to load).

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

Calls inside lambdas and local functions count towards the method that contains them.

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
      "ExcludeProjects": ["*.Tests", "tests/*"]
    }
  }
}
```

| Setting | Description |
|---------|-------------|
| `Solution` | Solution or project to analyze. Without it, the tool uses the single `.sln`/`.slnx` in the current directory |
| `MetadataDirectory` | Metadata folder. Defaults to `XrmMockup:Metadata:OutputDirectory`, then `./Metadata` |
| `ExcludeProjects` | Projects to leave out, such as test projects. See below |

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

When the metadata folder contains `SecurityRoles/`, the output also lists the required tables that no
role mentions. XrmMockup doesn't check privileges on those tables, so tests pass even when no role
grants them.

## Output

Output is deterministic: methods, tables and source locations are sorted ordinally, paths are
relative to the analyzed solution with forward slashes, and line endings are always `\n`. Running
the tool twice on the same code gives byte-identical output, so it can be committed and diffed in CI.
