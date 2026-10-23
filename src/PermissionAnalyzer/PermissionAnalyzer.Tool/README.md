# XrmMockup Permission Analyzer

A .NET CLI tool that works out which Dataverse table privileges your code needs, by static analysis.
It finds your entry points (plugin steps, custom APIs, code activities and Azure Functions), follows
the calls from each one through your code without running any of it, and lists the privilege each
`IOrganizationService` call needs on each table, together with the identity it runs as. It can then check
those privileges against your security roles. It is a companion to
[XrmMockup](https://github.com/delegateas/XrmMockup) and reads the security roles that
`xrmmockup-metadata` downloads.

> **Preview.** This release lists the privileges each entry point needs and checks them against
> security roles, including plugin cascades and the execute privileges of custom APIs.

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

1 entry point, 0 unreached methods, 3 tables, 3 table privileges (3 requirements counted per entry point and method), 1 unresolved
```

The last line counts distinct table privileges, and separately each requirement once per entry point
or unreached method that needs it.

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
| `--solution` | `-s` | Solution (`.sln`/`.slnx`) or project (`.csproj`) to analyze. A project is analyzed together with the projects it references; methods in those that the project doesn't reach aren't listed as unreached, since a shared library also has code for other apps |
| `--metadata` | `-m` | XrmMockup metadata folder with `SecurityRoles/` |
| `--exclude` | `-x` | Leave out projects matching a pattern (see [Excluding projects](#excluding-projects)). Repeatable |
| `--plugin-solution` | | A solution or project with plugin code that the analyzed code fires but doesn't include, used for plugin cascades only (see [Plugin code in another solution](#plugin-code-in-another-solution)). Repeatable |
| `--config` | `-c` | Path to `appsettings.json` (default: the current directory) |
| `--format` | `-f` | `text` (default) or `json` |
| `--output` | `-o` | Write to a file instead of standard output |
| `--plugin` | | Only one plugin, or one of its steps: `<type>[:<message>[:<stage>]]`, such as `AccountPostPlugin:Update` |
| `--custom-api` | | Only one custom API, by unique name |
| `--function` | | Only one Azure Function, by function name |
| `--entry` | | Analyze a method as an entry point, such as `Contoso.Sync.Importer.Run`, and show only that. Repeatable |
| `--operation` | | Only a user operation on a table and the plugin steps it fires: `"<message> <table>"`, such as `"Update account"` (see [User operations](#user-operations)) |
| `--attributes` | | The columns the `--operation` sets, such as `name,statecode`. Comma-separated or repeatable |
| `--role` | `-r` | Also show whether these roles grant each requirement (see [`check`](#check)). Repeatable |
| `--caller-role` | | Roles of the user that app code acts for through `CallerId`. Repeatable |

Use at most one of `--plugin`, `--custom-api`, `--function`, `--entry` and `--operation`. Without any, every entry
point is listed. A scope that matches nothing is an error. Type names may be simple or full and are
matched case-insensitively.

Without `--role` or `--caller-role`, the output has no role data, so it only changes when the code or
the metadata changes. That is the output to commit and diff. With them, each requirement is marked as
`check` does, but the exit code stays `0`.

Exit codes: `0` on success, `2` on errors (invalid options, missing files, or a solution that fails to load).

### `check`

Checks that security roles grant every privilege the code needs. It takes the same options as
`required`, plus `--strict`.

```bash
xrmmockup-permissions check --role "Salesperson" --plugin AccountPostPlugin
xrmmockup-permissions check --role "Salesperson" --role "Marketing"
xrmmockup-permissions check --function ProcessOrder --role "Integration App" --caller-role "Salesperson"
xrmmockup-permissions check --operation "Update account" --attributes statecode --role "Salesperson"
xrmmockup-permissions check   # roles from Principals and Users in appsettings.json
```

Example output:

```
plugin:Contoso.Plugins.AccountPostPlugin:Update:account:PostOperation  src/Plugins/AccountPostPlugin.cs:14
  roles for Caller: Salesperson (--role)
  ✔ account   Read    Caller  BusinessUnit  src/Plugins/AccountPostPlugin.cs:20
  ✘ contact   Create  Caller  missing       src/Services/ContactService.cs:31 (AccountPostPlugin.ctor (lambda, line 16) → ContactService.CreateFor)
  · task      Create  System  SYSTEM        src/Plugins/AccountPostPlugin.cs:44
...
Result: 1 granted, 1 missing, 1 as SYSTEM
Missing privileges:
  ✘ contact  Create  Caller  Salesperson (--role)  in plugin:Contoso.Plugins.AccountPostPlugin:Update:account:PostOperation
Not checked: 1 unresolved operation (1 with an unknown table)
```

Each requirement is checked against the roles of the identity it runs as:

| Runs as | Checked against |
|---------|-----------------|
| `Caller` | `--role` |
| `ApplicationUser` | `--role`, else the entry point's function app in `Principals` |
| `User` | The user's roles in `Users` |
| `ImpersonatedCaller` | `--caller-role`: the call runs with the privileges of the user in `CallerId` |
| `System` | Nothing; SYSTEM needs no role |

- Repeated roles combine as they do in Dataverse: a privilege is granted when any of them grants it,
  at the highest depth any of them grants.
- Any depth counts. The granted depth (`User`, `BusinessUnit`, `ParentChildBusinessUnit` or
  `Organization`) is shown but doesn't affect the result.
- Requirements with an `(assumed)` identity are checked like the rest.
- `✔` is granted (or on a table no role governs, which XrmMockup doesn't check), `✘` is missing, `-` is
  [suppressed](#configuration), and `·` is not checked, unverifiable or runs as SYSTEM. Each entry point first lists the
  roles each identity was checked against, or why it wasn't checked.
- Role names are matched exactly, then ignoring case. An unknown name is an error that suggests the
  closest names.
- `Missing privileges` lists each missing privilege once, with the identity, the roles that don't
  grant it and every entry point or unreached method that needs it. The `check` error names them too.
- `Not checked` lists what the result doesn't cover: unresolved operations (split into requests
  without a rule, custom API calls, and operations with an unknown table or request), requirements whose identity has no roles and
  unverifiable privileges. It is left out when everything was checked. When nothing is
  missing but something wasn't checked, `check` exits with `0` and logs a warning; `--strict` makes it fail.
- Some privileges aren't tied to a table, such as `prvActOnBehalfOfAnotherUser` or the execute
  privilege of a custom API. They are listed with their name in the table column and `global` as the
  privilege, and under `Privileges not tied to a table`.
- Setting `CallerId` needs `prvActOnBehalfOfAnotherUser` on the identity that sets it, usually the
  application user, for every operation that acts for the user in `CallerId`.
- Privileges not tied to a table are checked against the roles' global privileges, which
  `xrmmockup-metadata` downloads in releases after v1.2.0. Roles from older metadata don't have them,
  so these privileges are `unverifiable` until you regenerate the metadata.

Exit codes:

| Code | Meaning |
|------|---------|
| `0` | Every checked privilege is granted, suppressed or on a table no role governs |
| `1` | Some privilege is missing. With `--strict`, also when an operation is unresolved, an identity has no roles to check against, or a privilege is unverifiable |
| `2` | Error: invalid options, missing files, an unknown role, or no roles at all (no `--role`, `--caller-role`, and nothing analyzed matches `Principals` or `Users`) |

## Entry points

| Kind | Found from |
|------|------------|
| Plugin step | `RegisterPluginStep<T>`, `RegisterStep<T, TService>` (lambda or `nameof(TService.Method)`) in [XrmPluginCore](https://github.com/delegateas/XrmPluginCore) plugin constructors |
| Plugin step (metadata) | Plugin steps in `Metadata/Metadata.xml`, for `IPlugin` classes that register no steps in code. Without metadata, such a class is one entry point for its `Execute` |
| Custom API | `RegisterCustomAPI` and `RegisterAPI<TService>` |
| Code activity | Subclasses of `CodeActivity` |
| Azure Function | Methods with `[Function]` (isolated worker) or `[FunctionName]` (in-process) |
| Method | `--entry` and the `EntryPoints` setting |
| Operation | `--operation` |

Entry point ids look like `plugin:Contoso.AccountPostPlugin:Update:account:PostOperation` (the table
is `any` for steps on every table), `custom-api:ctx_DoThing`, `activity:Contoso.MyActivity`,
`function:ProcessOrder`, `method:Contoso.Sync.Importer.Run(IOrganizationService)` and
`operation:Update account(name,statecode)`.

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
| `Create` | Create, and Append and AppendTo for [lookups](#lookups) |
| `CreateAndReturn` | Create, Read, and Append and AppendTo for lookups |
| `Retrieve`, `RetrieveMultiple` | Read on the table and every linked table |
| `Update` | Write, and Append and AppendTo for lookups |
| `Delete` | Delete |
| `Associate`, `Disassociate` | Read, AppendTo on the target; Read, Append on the related records |
| `Execute` | From the rule for the request's message, see [Requests](#requests) |

Calls on an `OrganizationServiceContext`, including generated contexts such as `new Xrm(service)`:

| Call | Privileges |
|------|------------|
| `CreateQuery`, and query properties such as `AccountSet` | Read on the table, and on every table a LINQ `join` adds |
| `AddObject` | Create, and Append and AppendTo for lookups |
| `UpdateObject` | Write, and Append and AppendTo for lookups |
| `DeleteObject` | Delete |
| `AddLink`, `DeleteLink` | Read, AppendTo on the source; Read, Append on the target |
| `AddRelatedObject` | Create on the related record, and the privileges of `AddLink` |
| `Execute` | As on the service |

The privileges of a change are reported where it's queued, not at `SaveChanges`. A context runs as
the service it's created with, also in code inside a context class and when it's kept in a field.
`LoadProperty` reads through a relationship whose table isn't known, and is reported as unresolved.

The table is found from:

- early-bound types (`[EntityLogicalName]`), such as `new Account()`
- constant strings, such as `new Entity("account")` or `Account.EntityLogicalName`
- `QueryExpression`, `QueryByAttribute`, `AddLink` and FetchXML (including interpolated strings)
- locals assigned in the same method, including both branches of `?:` and `??`
- loops over the result of `RetrieveMultiple`
- the value a helper method returns, resolved for each call with that call's arguments, such as
  `service.Create(Build(flag))` or `RetrieveMultiple(Query<Contact>())`
- type arguments of generic methods, such as `service.Get<Contact>(id)` where `Get<T>` uses
  `new T().LogicalName` or `Activator.CreateInstance<T>()`
- type arguments of generic types, from the object a method is called on, such as
  `new Repository<Contact>(service).GetRecords()` where `Repository<T>` runs `CreateQuery<T>()`.
  This also works for a subclass such as `ContactRepository : Repository<Contact>`, for overrides and
  for calls through a generic interface such as `IRepository<Contact>`
- the record `Retrieve` returns, which is of the table it asks for
- an early-bound lookup property, such as `order.CustomerId.LogicalName`, from the lookup's targets in
  the entity metadata, as for [lookups](#lookups)
- `PrimaryEntityName` of the execution context in a plugin step, which is the step's table

### Requests

A request passed to `Execute` is checked against the rule for its message. Requests are followed like
tables: through locals, parameters, helper methods, object initializers, `Parameters["…"]`, and
collections built with `Add`, `AddRange` or collection expressions, including batching with `Chunk` or
`foreach`. Built-in rules cover:

| Request | Privileges |
|---------|------------|
| `Create`, `Update`, `Delete`, `Retrieve`, `RetrieveMultiple`, `Associate`, `Disassociate` | As the matching call above |
| `Upsert` | Create and Write on `Target` |
| `CreateMultiple`, `UpdateMultiple`, `UpsertMultiple`, `DeleteMultiple` | As the single request, on each table in `Targets` |
| `ExecuteMultiple`, `ExecuteTransaction` | The privileges of every request in `Requests` |
| `Assign` | Assign, Write |
| `SetState` | Write on `EntityMoniker` |
| `GrantAccess`, `ModifyAccess` | Share, and each right in `PrincipalAccess.AccessMask` |
| `RevokeAccess` | Share |
| `Merge` | Read, Write; `prvMerge` |
| `QualifyLead` | Write on the lead; Create on account, contact and opportunity when asked to create them |
| `SendEmail` | Read, Write on email, with the assumption that it is sent as the caller (sending as another user needs `prvSendAsUser`) |
| `CloseIncident`, `WinOpportunity`, `LoseOpportunity`, `WinQuote`, `CloseQuote` | Create on the close activity, Write on the closed record |
| `ReviseQuote` | Read, Create on quote |
| `InitializeFileBlocksUpload` / `InitializeFileBlocksDownload` | Write / Read on `Target` |
| `BulkDelete` | `prvBulkDelete` |
| `ImportSolution`, `ImportSolutionAsync` / `ExportSolution`, `ExportSolutionAsync` | `prvImportCustomization` / `prvExportCustomization` |
| `PublishXml`, `PublishAllXml` | `prvPublishCustomization` |
| `ExecuteWorkflow` | `prvWorkflowExecution` |
| `DeleteRecordChangeHistory` / `DeleteAuditData` | `prvDeleteRecordChangeHistory` / `prvDeleteAuditPartitions` |
| `RetrieveAuditDetails`, `RetrieveAttributeChangeHistory`, `RetrieveRecordChangeHistory` | `prvReadRecordAuditHistory`, `prvReadAuditSummary` |
| Block transfers, metadata requests, `WhoAmI`, `RetrieveVersion`, `RetrievePrincipalAccess` and similar | None |

Privileges named with `prv` are looked up in `PrivilegeCatalog.xml`: one not tied to a table is a
[privilege not tied to a table](#check), one tied to tables is the table privilege it grants on each,
and a name the catalog doesn't have is reported as unresolved. Without a catalog they are taken as
privileges not tied to a table.

Some request parameters and columns need a privilege of their own:

| Set | Needs |
|-----|-------|
| `BypassCustomPluginExecution` | `prvBypassCustomPlugins` |
| `BypassBusinessLogicExecution`, `BypassBusinessLogicExecutionStepIds` | `prvBypassCustomBusinessLogic` |
| `overriddencreatedon` on a record that is created | `prvOverrideCreatedOnCreatedBy` |

A bypass parameter set to a constant `false` or empty value needs nothing. The bypassed plugin steps
don't run, see [plugin cascades](#plugin-cascades).

A request without a rule is reported as unresolved. When its message is a custom API (one found in
the code, in `PrivilegeCatalog.xml`, a plugin step message in the metadata, or a name with a publisher
prefix such as `ctx_Approve`), it is reported as a custom API call. Add a rule for your own messages
in the [`Rules` setting](#request-rules).

### Custom APIs

Calling a custom API needs its execute privilege, if it has one. It is read from the registration in
the code, else from `PrivilegeCatalog.xml`, which `xrmmockup-metadata` writes with every privilege and
custom API in the environment:

| Registration | Requirement |
|--------------|-------------|
| `.WithExecutePrivilege<Account>(Privilege.Read)` | Read on `account` (basis `execute-privilege`) |
| `.WithExecutePrivilege("ctx_Order", Privilege.Write)` | Write on `ctx_order` |
| `.WithExecutePrivilegeName("prvReadctx_Order")` | The table privilege the catalog has for the name, or the name itself when it isn't tied to a table |
| An execute privilege in the catalog only | The same, for a custom API whose code isn't analyzed |

A privilege name is only looked up in the catalog, never guessed from its spelling. A name without a
catalog, a name the catalog doesn't have, a name computed at runtime, and a privilege whose access
right isn't a table privilege are reported as unresolved.

The caller of a custom API needs the privilege, and so does the custom API's own entry point, under
`Caller`. When the custom API is implemented in the analyzed code, its code runs as a
[cascade](#plugin-cascades). A custom API that is only in the catalog is reported as unresolved,
because its code isn't analyzed.

### Lookups

A lookup set on a record that is created or updated, directly or through a request such as `Upsert` or
`CreateMultiple`, needs Append on the record's table and AppendTo on the table the lookup refers to.
Lookups are found where the code sets them: early-bound properties (`order.CustomerId = …`), indexers
(`entity["parentcustomerid"] = …`) and `Attributes.Add`, on new records and on records passed in from
callers. `ownerid` and lookups set to `null` need nothing.

The referenced table comes from the `EntityReference` value, like any other table. When it can't be
resolved, the lookup's targets in the entity metadata (`*Metadata.xml` in the metadata folder) are used:
the only target of a simple lookup, or every target of a polymorphic one such as a customer lookup, with
basis `all-lookup-targets`. Without metadata such a lookup is reported as unresolved.

The analysis only sees lookups set in the code. A record retrieved and written back unchanged, or one
from a source it can't follow, adds no Append or AppendTo.

### Plugin cascades

An operation also runs the plugin steps registered for its message and table, and those steps need
their privileges in the same request. Each entry point therefore includes the requirements of every
step its operations fire, and of the steps those fire in turn, listed after `fires:` under the entry
point. Steps come from code registrations and from the plugin steps in the metadata:

- A step runs as the identity of the operation that fired it, such as the application user of an
  Azure Function or the user it acts for through `CallerId`, unless the step impersonates a user.
- `Create`, `Update`, `Delete`, `Retrieve`, `RetrieveMultiple`, `Associate` and `Disassociate` fire their
  own message. A request fires its message on its `Target` table, and the messages it runs as:
  `Upsert` also fires `Create` and `Update`, and `CreateMultiple` also fires `Create`.
- A step without a table fires for every table. An operation on a table that can't be resolved fires
  only those: the steps on a table are reported as unresolved ("may fire any of 12 Update steps"),
  because including every step of the message would include most of the solution and tell nothing.
- An operation on a polymorphic lookup's targets fires the steps of each target, with basis
  `all-lookup-targets`.
- An Update step with filtered attributes, from `.AddFilteredAttributes(…)` or the metadata, fires
  only for an update that sets one of them. The columns set are read from early-bound properties,
  indexers and `Attributes.Add` on the entity, or on the `Target` of a request, and followed into
  helpers it is passed to. When they can't all be determined, such as for a retrieved entity, the
  plugin's `Target`, a column name computed at runtime, or an entity passed to a method that can
  set more, the step fires with basis `filter-unknown`.
- A call to a custom API implemented in the analyzed code runs that code.
- A request with `BypassCustomPluginExecution` set to `true` doesn't run synchronous steps, and one with
  `BypassBusinessLogicExecution` doesn't run the synchronous (`CustomSync`) or asynchronous
  (`CustomAsync`) steps it names. A step's mode comes from `.SetExecutionMode(…)` or the metadata.
  Steps are still run when the value is set at runtime, and for `BypassBusinessLogicExecutionStepIds`,
  because the analysis doesn't know step ids. A custom API's own code always runs.
- A metadata step whose plugin code isn't loaded is reported as unresolved, so missing plugin code is
  never treated as needing nothing. Load it with `--plugin-solution` when it's in another solution.
- Each step is expanded once per identity, so steps that fire each other don't loop.

The source path of a cascaded requirement runs from the entry point through the operation (such as
`Create contact`) and the step id into the step's code. The JSON output lists a step's filter as
`filteredAttributes` on its trigger.

#### Plugin code in another solution

A function app or integration rarely references the plugin projects, so the steps it fires have no
code in its solution. Add the solutions or projects with the plugin code with `--plugin-solution`, or
`PluginSolutions` in the configuration:

```bash
xrmmockup-permissions required -s ./src/Contoso.Functions/Contoso.Functions.csproj -m ./tests/Metadata \
  --plugin-solution ../Contoso.Plugins/Contoso.Plugins.sln
```

Their code is used for the steps the analyzed code fires, and their files are shown relative to the
analyzed solution, such as `../Contoso.Plugins/AccountPlugin.cs:12`. Their own entry points, and
their methods that no entry point reaches, aren't listed, unless a scope such as `--plugin` selects
one. `ExcludeProjects` applies to them as well, with path patterns relative to their own solution.

### User operations

`--operation "<message> <table>"` answers "what does a user need to do this", such as updating an
account in an app. The operation runs as `Caller` and needs the privileges of its request rule (see
[Request rules](#request-rules)) on the table, plus those of every plugin step it fires:

```bash
xrmmockup-permissions required --operation "Update account" --attributes name,statecode
xrmmockup-permissions check --operation "Create contact" --attributes parentcustomerid --role "Salesperson"
```

- The message may be any message with a rule, such as `Create`, `Update`, `Delete`, `Assign` or
  `Upsert`, which fires the steps of `Create` and `Update` too. A message without a rule, such as a
  custom API, is reported as unresolved, but still fires the steps registered on it.
- `--attributes` are the columns the operation sets. They decide which Update steps filtered on
  columns fire. Without them, those steps fire with basis `filter-unknown`.
- A lookup column among them needs `Append` on the table and `AppendTo` on the tables it can refer
  to, from the metadata (`all-lookup-targets` when there are several).
- Request members other than the table, such as `RelatedEntities` of `Associate`, and access rights
  granted by `GrantAccess`, aren't known and are reported as unresolved.

Requirements of the operation itself have the source `--operation`; those of a step it fires have the
step's code, with a path that starts at the operation.

Calls inside lambdas and local functions count towards the method that contains them. A generic
helper or generic class method that no entry point calls is listed under unreached methods with its
type parameter as the unresolved reason.

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
      "PluginSolutions": ["../Contoso.Plugins/Contoso.Plugins.sln"],
      "EntryPoints": ["Contoso.Sync.Importer.Run"],
      "Principals": {
        "Contoso.Functions": { "Roles": ["Integration App"] },
        "Contoso.Functions/ProcessOrder": { "Roles": ["Integration App", "Order Processor"] }
      },
      "Users": {
        "6a1e5c4d-2b3f-4a5e-8c7d-9e0f1a2b3c4d": { "Name": "svc-plugins", "Roles": ["Plugin Service"] }
      },
      "Suppress": [
        { "Table": "ctx_auditlog", "Privilege": "Create", "Reason": "Created by SYSTEM via helper" },
        { "Privilege": "prvActOnBehalfOfAnotherUser", "Reason": "Granted by the integration team" }
      ],
      "Rules": [
        { "Message": "ctx_Approve", "Target": "Target", "Privileges": ["Write"] }
      ]
    }
  }
}
```

| Setting | Description |
|---------|-------------|
| `Solution` | Solution or project to analyze. Without it, the tool uses the single `.sln`/`.slnx` in the current directory |
| `MetadataDirectory` | Metadata folder. Defaults to `XrmMockup:Metadata:OutputDirectory`, then `./Metadata` |
| `ExcludeProjects` | Projects to leave out, such as test projects. See below |
| `PluginSolutions` | Solutions or projects with plugin code that the analyzed code fires, used for plugin cascades only. `--plugin-solution` adds to these. See [Plugin code in another solution](#plugin-code-in-another-solution) |
| `EntryPoints` | Extra methods to analyze as entry points, by full name, such as code called from outside the solution. `--entry` adds to these. A name that matches no method is reported as a warning |
| `Principals` | The application user's roles per function app, by assembly name, or per function as `<assembly>/<function>` (which wins). Used when `--role` isn't given |
| `Users` | The roles of specific users by user id, such as the impersonating user of a plugin step. `Name` is shown in the output |
| `Suppress` | Missing privileges to accept, with the reason. Leave out `Privilege` to accept every privilege on the table, or leave out `Table` to accept a privilege not tied to a table by its name. Suppressed privileges are shown but don't fail `check` |
| `Rules` | Rules for requests passed to `Execute`, which replace the built-in rule for their message or add one. See below |

Configured paths are relative to the configuration file. `appsettings.{DOTNET_ENVIRONMENT}.json` and
environment variables (for example `XrmMockup__Permissions__Solution`) override the file, and
command-line options override everything.

### Request rules

Each rule names the `Message` (`ctx_Approve`, or a request type such as `AssignRequest`) and what it
needs:

```json
"Rules": [
  { "Message": "ctx_Approve", "Target": "Target", "Privileges": ["Write"] },
  { "Message": "ctx_ApproveMultiple", "Targets": "Targets", "As": ["ctx_Approve"] },
  { "Message": "ctx_Share", "Target": "Target", "Privileges": ["Share", "$Access"] },
  { "Message": "ctx_Close", "Checks": [{ "Table": "task", "Privileges": ["Create"], "When": "CreateTask" }] },
  { "Message": "ctx_Ping" }
]
```

| Field | Description |
|-------|-------------|
| `Target`, `Privileges` | The privileges needed on the table of the request member `Target` names. `$Member` adds the rights an `AccessRights` member grants |
| `Checks` | Further checks, each on a request `Member` or a fixed `Table`, with `Privileges` and an optional boolean `When` member that must be set |
| `As` | Messages whose checks the request makes as well |
| `Targets` | A member holding several targets; the `As` rules' checks on `Target` apply to it |
| `Requests` | A member holding requests that run as part of this one |
| `GlobalPrivileges` | Privileges the request needs by name, such as `prvBulkDelete`, looked up in the privilege catalog as above |
| `Assumption` | What the analysis assumes about the request, listed under assumptions wherever it is called |
| `Enforced` | `false` when XrmMockup doesn't make the checks (Dataverse still does) |

A rule with none of these needs no privilege, so its requests are no longer reported.

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

### .NET Framework projects

Projects that target .NET Framework, such as plugin projects, need its reference assemblies. Windows
has them from the Developer Packs. Elsewhere, the analyzer uses the
`Microsoft.NETFramework.ReferenceAssemblies.net*` packages in the NuGet cache (`NUGET_PACKAGES` or
`~/.nuget/packages`), which a restore of an SDK-style project targeting .NET Framework downloads.
Set `TargetFrameworkRootPath` to use another folder.

Strong-name signing is ignored, so a key file that isn't checked in doesn't stop the analysis.

## Security roles

The metadata folder is also where plugin step registrations are read from (`Metadata.xml`).

When the metadata folder contains `SecurityRoles/`, the output also lists the required tables that no
role mentions. XrmMockup doesn't check privileges on those tables, so tests pass even when no role
grants them. `check` and `--role` use the roles from this folder, by name.

## Output

`--format json` writes the same content as an object with `entryPoints` (each with its `id`, `kind`,
`trigger`, `requirements`, `unresolved`, `assumptions` and `triggers`, the ids of the steps it fires),
`unreached` and `notRoleGoverned`.
Requirements carry `table`, `privilege`, `runsAs`, `basis` and their `sources`, each with the call
`path` from the entry point. A privilege not tied to a table has no `table`, and its name as
`privilege`.

With roles, each requirement also has `coverage` (`status`: `granted`, `not-governed`, `missing`,
`suppressed`, `not-checked`, `system` or `unverifiable`, with `depth` or `reason`), each entry point has `roles` (the
roles per identity, with their `origin`), and the document
has a `result` with the counts, `unresolvedNoRule` and `unresolvedCustomApi` (the unresolved operations
that are requests without a rule and custom API calls) and `incomplete` (whether anything wasn't checked), and `missing` lists each missing privilege
with `table` (left out for a privilege not tied to a table), `privilege`, `runsAs`, `roles`, `origin` and `in` (the entry points and methods that need
it). Without roles, none of these are written.

Output is deterministic: entry points, methods, tables and source locations are sorted ordinally, paths are
relative to the analyzed solution with forward slashes, and line endings are always `\n`. Running
the tool twice on the same code gives byte-identical output, so it can be committed and diffed in CI.
