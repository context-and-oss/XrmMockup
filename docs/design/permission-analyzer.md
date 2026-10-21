# Design: Permission Analyzer (`xrmmockup-permissions`)

Status: Draft

## 1. Problem

Teams that test Dataverse code with XrmMockup still find missing security role privileges
late, usually as `Principal user is missing prvWriteX privilege` in a test or production
environment. The code already holds the answer: every `Create`, `Retrieve`, `Update`,
`Delete`, `Associate`, query and message names a table and implies a privilege. XrmMockup
already downloads the security roles (`Metadata/SecurityRoles/*.xml`) and the plugin step
registrations, so the "granted" side is also on disk.

This tool analyzes a **consumer's codebase** (plugins, custom APIs, workflow activities,
Azure Functions and other callers of Dataverse, not XrmMockup itself) **statically**. It works
out which table privileges each entry point needs and compares them with the downloaded roles.

## 2. Goals and non-goals

Goals

- Static analysis only. The code is not executed and no Dataverse connection is needed.
- **Deterministic answers.** The same code and metadata always give the same output, with
  stable ordering. Every requirement states why it is included (§5.2). Nothing is scored or
  guessed: when the tool can't decide something, it includes it with the reason or lists it
  as unresolved. The requirement set is therefore a superset of what the code can need,
  never a sample.
- Cover plugin-style entry points (plugin steps, custom APIs, code activities) and
  application-style entry points (Azure Functions, plus any method named as an entry point).
- Two questions, available as CLI commands:
  1. **check**: does role *R* (or a set of roles) grant everything action *A* needs?
  2. **required**: exactly which privileges does action *A* need? Optionally: do given roles
     cover it?
- Scope can be the **whole solution** or a single **query**: one operation that fires
  plugins (e.g. `Update account`), one plugin step, one custom API or one Azure Function.
- Machine-readable output for CI, with an exit code that fails the build on gaps.

Non-goals (for the first version)

- Proving privilege *depth* (User/Business Unit/Parent-Child/Organization). Depth depends on
  who owns the data at runtime, so it is reported, not verified (see §8).
- Field-level security, column security profiles and app/form access.
- Code outside C# (JavaScript web resources, Power Automate flows, Power Fx).
- Changing XrmMockup core. The tool is a separate dotnet tool, not a subcommand of
  `xrmmockup-metadata`, which keeps Roslyn/MSBuild out of the metadata generator.

## 3. Background: how XrmMockup decides what is required

All privilege checks in mockup go through `Security.HasPermission`
(`src/XrmMockup365/Security.cs`). The request handlers define which checks each message
does, and the analyzer copies them so its answers match what a mockup test would enforce:

| Message | Checks (from the handler) |
|---|---|
| Create | Create on target; per lookup set: Append on target, Read + AppendTo on referenced record (`CreateRequestHandler.cs`) |
| Update | Write on target; per changed lookup: Append on target, AppendTo (+ Read for UI role) on referenced record (`UpdateRequestHandler.cs`) |
| Delete | Delete on target (`DeleteRequestHandler.cs`) |
| Retrieve | Read on target (`RetrieveRequestHandler.cs`) |
| RetrieveMultiple | Read on queried table and linked tables (`RetrieveMultipleRequestHandler.cs`) |
| Associate / Disassociate | Read + AppendTo on target, Read + Append on related records (`AssociateRequestHandler.cs`, `DisassociateRequestHandler.cs`) |
| Assign | Assign (+ Write) on target (`Security.CheckAssignPermission`) |
| Grant/ModifyAccess | Share on target, plus every right being shared (`Security.CheckSharingAccess`) |
| SendEmail | Read + Write on email, Read on regarding record |

Two mockup shortcuts matter for the analyzer:

- Calls made as the admin/SYSTEM user always pass, so operations that run as SYSTEM need no
  role privilege (§5.4).
- A table that no role mentions at all always passes. The analyzer reports such tables as
  `not-role-governed` instead of as required.

## 4. Architecture

A standalone .NET tool, kept out of XrmMockup core so consumers don't pull Roslyn/MSBuild
into their test projects.

```
src/PermissionAnalyzer/
  PermissionAnalyzer.Core/      netstandard2.0
    Model/                      Privilege, Requirement, RoleDefinition, EntryPoint, Manifest
    Discovery/                  Entry-point discovery (plugins, custom APIs, activities, functions)
    Extraction/                 Roslyn operation extraction + table/attribute resolution
    Rules/                      Message → privilege rule table (mirrors mockup handlers)
    Graph/                      Call graph, DI resolution, plugin cascade expansion
    Analysis/                   Manifest building, coverage of RoleDefinitions
  PermissionAnalyzer.Tool/      net10.0, PackAsTool, command `xrmmockup-permissions`
    Metadata/                   Role loading (MetadataSkeleton) → RoleDefinition
    Workspace/                  MSBuildWorkspace loading
  PermissionAnalyzer.Tests/     xUnit; in-memory compilations against the real SDK + test metadata
```

- Core targets `netstandard2.0` (Roslyn APIs only, no MSBuild) so the same rules can later
  ship as a `DiagnosticAnalyzer` package for IDE warnings, reading `Metadata/` via
  `AdditionalFiles`.
- The Tool loads the consumer solution with `Microsoft.CodeAnalysis.Workspaces.MSBuild`
  (`MSBuildWorkspace`) and passes compilations to Core. Since Roslyn 4.x the workspace builds
  in an out-of-process build host, so `Microsoft.Build.Locator` isn't needed. A multi-targeted
  project loads once per target framework; the duplicate findings merge into one.
- Role and metadata files are read in the Tool with the shared `MetadataSkeleton.projitems`
  (same `DataContractSerializer` format XrmMockup and the metadata generator use), so there is
  no second definition of the model. The skeleton depends on the Dataverse SDK, so the Tool
  converts each role to Core's SDK-free `RoleDefinition`, and Core stays `netstandard2.0` with
  Roslyn as its only dependency.

Pipeline:

```
solution ──► compile ──► discover entry points ──► extract operations per method
                                   │                          │
                                   ▼                          ▼
                         call graph + DI resolution ◄─────────┘
                                   │
                                   ▼
               expand plugin cascades (operation → triggered steps → their operations)
                                   │
                                   ▼
               per-entry-point requirement set ──► role coverage (check / required) ──► report
```

## 5. Analysis

### 5.1 Entry-point discovery

| Kind | Detected by | Identity |
|---|---|---|
| Plugin step (XrmPluginCore) | `RegisterPluginStep<T>(EventOperation, ExecutionStage, handler)` and `RegisterStep<T, TService>(…, service => …)` / `nameof(TService.Method)` in plugin constructors; `.AddFilteredAttributes`, `.AddImage` read as well | `T` → table, operation, stage, handler method |
| Plugin step (metadata) | `Metadata/` plugin registrations (`MetaPlugin`): message, entity, stage, `ImpersonatingUserId` | Matched to classes by `PluginTypeAssemblyName`; runs-as from impersonation |
| Plain `IPlugin` | Classes implementing `IPlugin` with steps only in metadata | `Execute(IServiceProvider)` |
| Custom API | `RegisterCustomAPI(...)` (XrmPluginCore) and custom API metadata | API unique name |
| Code activity | `CodeActivity` subclasses (`Execute(CodeActivityContext)`) | Type name |
| Azure Function (isolated) | Methods with `[Function("Name")]` (`Microsoft.Azure.Functions.Worker`) | Function name + trigger attribute (`HttpTrigger`, `ServiceBusTrigger`, `TimerTrigger`, …) |
| Azure Function (in-process) | Methods with `[FunctionName("Name")]` (`Microsoft.Azure.WebJobs`) | Same |
| Explicit | `--entry "Namespace.Type.Method"` or `XrmMockup:Permissions:EntryPoints` (§6.3) | Fully qualified method |

Discovery uses the semantic model (symbol names), not text matching, so aliases and partial
classes work.

### 5.2 Operation extraction

Within each method body, the extractor finds:

- `IOrganizationService`, `IOrganizationServiceAsync`/`Async2` and `ServiceClient`:
  `Create`, `Retrieve`, `RetrieveMultiple`, `Update`, `Delete`, `Associate`, `Disassociate`,
  `Execute`, and their async forms.
- `Execute(new XxxRequest { … })`, where the request type is looked up in the rule table
  (§5.3). Requests built locally and added to `ExecuteMultipleRequest.Requests` /
  `ExecuteTransactionRequest.Requests` are unpacked.
- `OrganizationServiceContext` (including generated contexts): `CreateQuery<T>()` /
  `CreateQuery("x")` and LINQ joins (Read on each joined table), `AddObject`, `UpdateObject`,
  `DeleteObject`, `AddLink`/`AddRelatedObject` (Append/AppendTo), `SaveChanges`.
- Known helper extension methods (XrmPluginCore and common framework helpers), listed as
  rule entries instead of hard-coded.

**Table resolution**, tried in this order:

1. Early-bound type: `[EntityLogicalName("account")]` on `T` / the constructed type.
2. Constant strings: `new Entity("account")`, `new EntityReference("account", id)`,
   `new QueryExpression("account")`, `LinkEntity` names, `EntityLogicalName` constants.
3. FetchXML: string literals and interpolated constants parsed for `entity` and
   `link-entity` names.
4. Local data flow inside the method: locals, `readonly` fields, constructor-assigned fields
   and `ToEntityReference()` on resolved entities.
5. Otherwise the operation is recorded as **Unresolved**, with its source location. It is
   never dropped silently.

**Attribute resolution** (for Append/AppendTo): lookup properties set on early-bound
entities (`ctx_AccountId = …`) or indexers (`entity["parentcustomerid"] = new EntityReference(…)`)
are mapped to the target table through relationship metadata in `Metadata/`. Polymorphic
lookups (customer, owner, regarding) use the `EntityReference.LogicalName` when it can be
resolved. Otherwise every target the relationship metadata allows is included.

Each requirement records its **basis**, which says why it is in the set. A basis is a fact
about the code, not a likelihood:

| Basis | Meaning |
|---|---|
| `direct` | Table and privilege resolved from the call itself (types, constants, local data flow) |
| `all-implementations` | Reached through an interface with no DI registration; every implementation is included (§5.5) |
| `all-lookup-targets` | Polymorphic lookup whose target can't be resolved; every allowed target is included |
| `filter-unknown` | Update step with filtered attributes, called where the set attributes can't be determined; the step is included (§5.6) |
| `execute-privilege` | Custom API execute privilege (§5.7) |

Operations whose table can't be resolved at all are listed as `unresolved` with their
location and reason. They are kept apart from the requirement set, because a privilege
can't be named without a table.

### 5.3 Message → privilege rules

The rules are data (an embedded JSON/C# table), not code, so they can be extended and
overridden in `XrmMockup:Permissions:Rules` (§6.3):

```jsonc
{ "message": "AssignRequest", "target": "Target",   "privileges": ["Assign", "Write"] },
{ "message": "SetStateRequest", "target": "EntityMoniker", "privileges": ["Write"] },
{ "message": "GrantAccessRequest", "target": "Target", "privileges": ["Share", "$PrincipalAccess.AccessMask"] },
{ "message": "QualifyLeadRequest", "target": "LeadId", "privileges": ["Write"],
  "creates": ["account?", "contact?", "opportunity?"] }
```

Built-in rules cover the messages XrmMockup implements (`src/XrmMockup365/Requests/`). Unknown
messages are reported as `unknown-message` findings so they show up instead of passing silently.

### 5.4 Execution identity ("runs as")

Each operation is tagged with the principal it runs under, because this decides which roles
are checked:

| Source of the service | Runs as |
|---|---|
| `factory.CreateOrganizationService(context.UserId)` / `localContext.OrganizationService` | **Caller** (or the step's impersonating user) |
| `factory.CreateOrganizationService(null)` / `OrganizationAdminService` | **System**: no role privilege needed, listed for information |
| `factory.CreateOrganizationService(<constant guid>)` | **Specific user** (by id) |
| Step with `ImpersonatingUserId` in metadata | **Impersonated user** |
| Azure Function / app code (`ServiceClient`, injected `IOrganizationService`) | **Application user**, mapped to roles in `XrmMockup:Permissions:Principals` (§6.3); a `CallerId` assignment switches it to **Caller (impersonated)** |

When the source can't be determined, the operation defaults to **Caller** and is flagged
(the conservative choice).

### 5.5 Call graph and dependency injection

- An inter-procedural call graph is built over all source methods in the solution. An
  entry point's requirements are the union of the operations reachable from it, with the
  call path kept for reporting.
- The `IOrganizationService` passed as an argument, or stored in a field or constructor,
  keeps its runs-as tag across calls.
- Interface calls (e.g. `IAccountService.HandleCreate` from `RegisterStep<Account, IAccountService>`,
  or services injected into Azure Functions) are resolved by:
  1. DI registrations found in the solution (`services.AddScoped<IFoo, Foo>()`, XrmPluginCore
     service registration, `Program.cs`/`Startup.cs` of function apps), then
  2. falling back to every implementation in the solution, with basis `all-implementations`.
- Recursion and cycles are handled by memoizing per-method summaries (requirement sets
  plus parameter-dependent placeholders).

### 5.6 Plugin cascade expansion

A data operation that runs as the caller also fires the synchronous and asynchronous steps
registered for that message and table, and those run as the same caller unless
impersonated. Mockup does the same through `RequestExecutionPipeline`. The analyzer:

1. Builds a step registry from code registrations (§5.1) merged with metadata registrations.
2. For each operation `(message, table, attributes)`, finds matching steps. For Update it
   respects `FilteredAttributes` when the set attributes are known. When they aren't, the
   step is included with basis `filter-unknown`.
3. Adds the step's requirements under the **effective** identity (caller, or the step's
   impersonating user), up to a depth limit (default 8, matching Dataverse's loop
   protection in spirit), with a cycle guard.

This is what makes the "query a specific operation" mode useful: `Update account` gives the
full set of privileges a user needs for that update to succeed, including every plugin that
fires.

Real-time workflows (XAML in `Metadata/Workflows`) are a later extension: mockup already
parses them, and their create/update steps can be turned into operations with the same rules.

### 5.7 Custom API execute privileges

A custom API can require an execute privilege (`ExecutePrivilegeName`). That must be an
existing privilege, so it maps to role data exactly:

- **Where the name comes from**: the custom API registration in code (XrmPluginCore
  `RegisterCustomAPI`, or the DAXIF `ExtendedCustomAPIConfig` tuple that mockup already reads
  into `CustomApiConfig.ExecutePrivilegeName`), or the downloaded custom API metadata.
- **Name → privilege**: `SecurityRoleReader` already queries `privilege` and
  `privilegeobjecttypecodes`, but drops the privilege names. The metadata generator gets an
  additive change: it also writes a privilege catalog (`name`, `privilegeid`, `accessright`,
  `objecttypecode`). Looking a name up in the catalog gives `(table, AccessRight)`. For
  example, `prvWriteAccount` gives `(account, Write)`, which is checked against roles like any
  other requirement (basis `execute-privilege`).
- **Privileges not tied to a table** (`objecttypecode = none`, e.g. `prvExportToExcel`): the
  reader currently skips them (`if (entityName == "none") continue;`), so roles carry no data
  for them. The generator change also keeps these on the role, stored separately from the
  per-table `Privileges` dictionary so mockup's table checks are unaffected. They are checked
  by name.
- **Calling a custom API**: `Execute(new OrganizationRequest("ctx_DoThing"))` or a generated
  request class needs the execute privilege, plus the API implementation's own requirements
  under the caller's identity. This is expanded the same way as plugin cascades (§5.6).

Until a privilege catalog is present in `Metadata/`, execute privileges are reported as
`unresolved` with reason `privilege-catalog-missing`. They are never derived from naming
conventions.

## 6. CLI

```
xrmmockup-permissions <command> [scope] [options]
```

Shared options:

| Option | Meaning |
|---|---|
| `--solution <path>` / `--project <path>` | Code to analyze (default: the single `.sln`/`.slnx` in the current directory) |
| `--metadata <dir>` | XrmMockup `Metadata/` folder (security roles, plugin steps, entity metadata). Default: `XrmMockup:Metadata:OutputDirectory` from `appsettings.json` |
| `--config <file>` / `-c` | Path to `appsettings.json`, same as `xrmmockup-metadata` (§6.3) |
| `--format text\|json\|markdown\|sarif` | Output format (default `text`; `sarif` for GitHub code scanning) |
| `--output <file>` | Write report to file |
| `--include-system` | Also list operations that run as SYSTEM |
| `--basis <list>` | Only show requirements with the given basis (§5.2); filtering is applied after analysis, so it never changes what is computed |

### 6.1 Scope selectors

Exactly one, or none for the whole solution:

| Selector | Example | Meaning |
|---|---|---|
| *(none)* | | Every discovered entry point |
| `--operation "<message> <table>"` | `--operation "Update account" --attributes accountnumber` | A user operation, plus every plugin it fires (§5.6) |
| `--plugin <type>[:<message>[:<stage>]]` | `--plugin AccountPostPlugin:Update` | One plugin, or one of its steps |
| `--custom-api <name>` | `--custom-api ctx_DoThing` | One custom API |
| `--function <name>` | `--function ProcessOrder` | One Azure Function (by function name) |
| `--entry <method>` | `--entry Contoso.Sync.Importer.Run` | Any method as an entry point |

### 6.2 Commands

**`check`**: does a role grant what the action needs?

```
xrmmockup-permissions check --role "Salesperson" --operation "Update account"
xrmmockup-permissions check --role "Salesperson" --role "Marketing" --plugin AccountPostPlugin
xrmmockup-permissions check --role "Integration App" --function ProcessOrder
```

- Repeating `--role` combines roles (union of privileges, highest depth wins), the same way
  mockup combines a principal's roles in `Security.JoinPrivileges`.
- **Any depth counts**: a privilege is covered when the combined roles grant it at any depth
  (`User`, `BusinessUnit`, `ParentChildBusinessUnit` or `Organization`). The granted depth is
  shown next to the requirement, but it doesn't affect the result or the exit code (§8). A
  minimum-depth option is a possible future feature (§10).
- With no scope and an app-user mapping in `appsettings.json`, each Azure Function is checked against
  its configured roles.
- Output: granted / missing per `(table, privilege)` with the depth the role grants, the
  source location and the call path. Exit code `0` = covered, `1` = missing privileges,
  `2` = analysis error. Unresolved operations alone don't fail the run unless `--strict` is set.

```
Update account (as caller) — role "Salesperson"
  ✔ account      Write     [User]   (request)
  ✔ account      Read      [User]   AccountPostPlugin → Execute  AccountPostPlugin.cs:37
  ✘ ctx_parent   Create             AccountPostPlugin → Execute  AccountPostPlugin.cs:37
  ✘ ctx_parent   Append             AccountPostPlugin → Execute  AccountPostPlugin.cs:37  (ctx_accountid)
  ✔ account      AppendTo  [User]   AccountPostPlugin → Execute  AccountPostPlugin.cs:37
  ? <unresolved> Update             AccountHelper.Touch  AccountHelper.cs:12  table name from parameter 'logicalName'
Result: 2 missing, 1 unresolved
```

**`required`**: exactly what is needed, optionally with role coverage.

```
xrmmockup-permissions required                                   # full solution manifest
xrmmockup-permissions required --operation "Create contact"
xrmmockup-permissions required --function ProcessOrder --role "Integration App"
```

- With no role options: the requirement manifest per entry point (table → privileges,
  identity, basis, locations). It contains no role data, so it depends only on code and
  metadata. This is the artifact to commit or diff in CI.
- `--role <name>` (repeatable): the same manifest with a granted/missing column for the
  given roles. It reports, like `check`, but doesn't change the exit code.

Finding which downloaded roles, or smallest combination of roles, would cover a requirement
is deferred (§10).

### 6.3 Configuration (`appsettings.json`)

The tool reads the same `appsettings.json` as `xrmmockup-metadata` and follows its
conventions:

- Sources, later ones winning: `appsettings.json`, `appsettings.{DOTNET_ENVIRONMENT}.json`,
  environment variables, then command-line options. This is the metadata tool's
  `ConfigReader` order, which the permissions tool reuses.
- `--config <path>` / `-c` points at the file; by default it is read from the current directory.
- Settings live under `XrmMockup:Permissions`, next to the existing `XrmMockup:Metadata`
  section. Each setting is a property on a `PermissionsConfiguration` record
  (`SectionPath = "XrmMockup:Permissions"`), like `MetadataConfiguration`.
- The metadata folder is, in order: `--metadata`, `XrmMockup:Permissions:MetadataDirectory`,
  `XrmMockup:Metadata:OutputDirectory`, then `./Metadata` when it exists. A project that already
  generates metadata needs no extra path setting. Configured paths are relative to the config
  file; command-line paths are relative to the current directory.
- Without `--solution`, `--project` or `Solution`, the tool uses the single `*.sln`/`*.slnx` in
  the current directory.
- The tool makes no Dataverse connection, so it ignores `DataverseUrl` and
  `DataverseCredentialType`.

The section holds what static analysis can't know:

```jsonc
{
  "DataverseUrl": "https://your-org.crm4.dynamics.com",
  "XrmMockup": {
    "Metadata": {
      "OutputDirectory": "./Metadata",
      "Solutions": ["MySolution"]
    },
    "Permissions": {
      "Solution": "./src/Contoso.sln",
      "MetadataDirectory": "./tests/Metadata",   // optional, see above
      // Projects to leave out, e.g. tests. Wildcards; a pattern with '/' matches the solution-relative path.
      // --exclude/-x adds to these.
      "ExcludeProjects": ["*.Tests", "tests/*"],
      // Azure Function apps / app code: which roles their application user has
      "Principals": {
        "Contoso.Functions": { "Roles": ["Integration App"] },
        "Contoso.Functions/ProcessOrder": { "Roles": ["Integration App", "Order Processor"] }
      },
      // Impersonating users from plugin step metadata, mapped to roles
      "Users": {
        "00000000-0000-0000-0000-000000000001": { "Name": "svc-plugins", "Roles": ["Plugin Service"] }
      },
      "EntryPoints": ["Contoso.Sync.Importer.Run"],
      "Rules": [ /* extra or overridden message rules, §5.3 */ ],
      "Suppress": [
        { "Table": "ctx_auditlog", "Privilege": "Create", "Reason": "Created by SYSTEM via helper" }
      ]
    }
  }
}
```

Command-line options override the matching setting, as in the metadata tool. For example,
`--solution` overrides `Solution` and `--metadata` overrides the metadata folder.

## 7. Output model

```jsonc
{
  "entryPoints": [{
    "id": "plugin:AccountPostPlugin:Update:PostOperation",
    "kind": "PluginStep",
    "trigger": { "message": "Update", "table": "account", "stage": "PostOperation" },
    "requirements": [{
      "table": "ctx_parent", "privilege": "Create",
      "runsAs": "Caller", "basis": "direct",
      "sources": [{ "file": "Plugins/AccountPostPlugin.cs", "line": 37,
                    "path": ["AccountPostPlugin.Execute"] }]
    }],
    "unresolved": [ /* operation + location + reason */ ],
    "triggers": [ "plugin:ContactPostPlugin:Create:PostOperation" ]
  }],
  "notRoleGoverned": ["activityparty"]
}
```

The same model drives text, Markdown, JSON and SARIF output. SARIF results point at the source
location of each missing privilege.

## 8. Limitations

- **Depth**: the report shows the depth each role grants (`User`, `BusinessUnit`,
  `ParentChildBusinessUnit`, `Organization`) next to each requirement, but can't prove it is
  enough, because that depends on record ownership at runtime. For now any depth counts as
  covered (§6.2).
- Table names computed at runtime, reflection, `dynamic`, and requests built in other
  assemblies (binary-only references) come out as `Unresolved`.
- Interface dispatch without DI registrations over-approximates (all implementations).
- Conditional code is treated as reachable (no path sensitivity), so requirements are a
  superset.
- Team-inherited roles and record sharing are outside the static view. `check` answers for
  the roles given, not for a specific user's effective rights.

## 9. Validation strategy

- **Golden manifests**: `PermissionAnalyzer.Tests` analyzes `tests/TestPluginAssembly365`
  (which covers XrmPluginCore, DI-based, legacy DAXIF, non-DAXIF and direct plugins, custom
  APIs and code activities) and compares the output with committed manifests.
- **New fixtures**: small projects for isolated and in-process Azure Functions, DI-heavy
  services, FetchXML/LINQ queries and `ExecuteMultiple`.
- **Cross-check with XrmMockup**: for selected scenarios, give a test user exactly the
  privileges the tool reports, run the scenario in XrmMockup and expect success; remove one
  reported privilege and expect an access-denied fault. This ties the rule table to mockup's
  real behavior and catches drift when handlers change.
- A runtime permission recorder in XrmMockup (recording every check inside
  `Security.HasPermission`) is a possible follow-up for automating this cross-check. It is not
  part of this design.

## 10. Delivery plan

| Phase | Scope | Outcome |
|---|---|---|
| 1 | Project skeleton, MSBuildWorkspace loading, role loading via MetadataSkeleton, direct `IOrganizationService` CRUD with early-bound/literal tables, per-method manifest, `required` (no scope). Also pulled forward: `QueryExpression`/`AddLink`/FetchXML tables, `Associate`/`Disassociate`, loops over `RetrieveMultiple` results, `Solution`/`MetadataDirectory` configuration | First useful manifest |
| 2 | Entry-point discovery (XrmPluginCore, metadata steps, custom APIs, activities, Azure Functions), call graph, runs-as tagging, DI resolution | Per-entry-point requirements; `--plugin`, `--custom-api`, `--function`, `--entry` |
| 3 | `check` command, `required --role`, role combination, exit codes, `XrmMockup:Permissions` configuration | Both questions answerable; CI-ready |
| 4 | Plugin cascade expansion, `--operation` scope, FilteredAttributes | "What does a user need to do X" |
| 5 | Queries (QueryExpression, LinkEntity, FetchXML, LINQ), Append/AppendTo via relationship metadata, Execute/ExecuteMultiple rule table; privilege catalog + global privileges in the metadata generator, custom API execute privileges | Near-complete coverage |
| 6 | SARIF/Markdown output, docs, NuGet tool packaging alongside `xrmmockup-metadata` | Release |
| Later | Role suggestions (which single role, or smallest combination of downloaded roles, covers a requirement), minimum required depth (see below), IDE `DiagnosticAnalyzer` package on Core, real-time workflow support, runtime recorder cross-check | |

**Idea: minimum required depth.** A `--min-depth` option (or per-table setting) that only
counts a privilege as covered at or above a given depth, so CI could, for example, require
`Organization` Read on reference tables. It needs a detailed specification before it is
built: whether the minimum applies globally, per table, per privilege or per entry point;
how it interacts with combined roles and with tables no role governs; how it relates to the
ownership XrmMockup actually checks at runtime (owner, business unit, sharing); and how it is
configured in `XrmMockup:Permissions`. Deferred until then.

## 11. Decisions

- **Packaging**: a separate dotnet tool, `xrmmockup-permissions`, not a subcommand of
  `xrmmockup-metadata`.
- **Deterministic output**: `required` gives an exact, reproducible requirement set derived
  from code and metadata only. Role coverage (`check`, `required --role`)
  is a layer on top that compares that set against the roles present in `Metadata/`. The
  tool does not guess whether the downloaded role set is complete.
- **Custom API privileges**: execute privileges are mapped exactly through a privilege
  catalog added to the metadata generator (§5.7).
- **Configuration**: a `XrmMockup:Permissions` section in the shared `appsettings.json`,
  loaded the same way as the metadata tool. There is no separate config file (§6.3).
- **Any depth counts**: role coverage treats a privilege granted at any depth as covered,
  and only reports the depth. A minimum-depth check is deferred until it is specified (§10).
- **Role suggestions deferred**: the first version answers "what is needed" and "do these
  roles cover it". Working out which roles would cover a requirement comes later.
