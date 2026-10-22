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
| Create | Create on target; per lookup set: Append on target, AppendTo (+ Read for UI role) on referenced record (`CreateRequestHandler.cs`) |
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
| Plugin step (metadata) | `Metadata/` plugin registrations (`MetaPlugin`): message, entity, stage, `ImpersonatingUserId` | Matched to classes by type name (`MetaPlugin.AssemblyName`) and `PluginTypeAssemblyName`, falling back to the type name alone; runs-as from impersonation. For classes that also register steps in code, only the impersonation is taken, onto the matching step |
| Plain `IPlugin` | Classes implementing `IPlugin` with steps only in metadata | `Execute(IServiceProvider)` |
| Custom API | `RegisterCustomAPI(...)` (XrmPluginCore) and custom API metadata | API unique name |
| Code activity | `CodeActivity` subclasses (`Execute(CodeActivityContext)`) | Type name |
| Azure Function (isolated) | Methods with `[Function("Name")]` (`Microsoft.Azure.Functions.Worker`) | Function name + trigger attribute (`HttpTrigger`, `ServiceBusTrigger`, `TimerTrigger`, …) |
| Azure Function (in-process) | Methods with `[FunctionName("Name")]` (`Microsoft.Azure.WebJobs`) | Same |
| Explicit | `--entry "Namespace.Type.Method"` or `XrmMockup:Permissions:EntryPoints` (§6.3) | Fully qualified method |

Discovery uses the semantic model (symbol names), not text matching, so aliases and partial
classes work.

Entry point ids: `plugin:{type}:{message}:{table|any}:{stage}` for steps (`{type}` is the
reflection name, with `+` for nested types), `plugin:{type}` for a plain `IPlugin` without known
steps, `custom-api:{name}`, `activity:{type}`, `function:{name}` and `method:{method id}`. Abstract
plugin classes, and plugin classes that other plugin classes derive from, are not entry points
themselves.

Methods with operations that no entry point reaches are reported under `unreached`, analyzed as if
called by the caller (assumed), so code called from outside the solution is never silently dropped.

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
5. Values returned by methods in the solution: each method summary records the tables of its
   `return` statements, which can depend on its parameters, and a call resolves them with its own
   arguments. Type arguments of generic methods are bound like parameters, so
   `Get<Contact>()` resolves `new T().LogicalName` or `Activator.CreateInstance<T>()` inside it.
   Recursive helpers stop at the cycle, and the depth limit of the call graph applies.
6. Otherwise the operation is recorded as **Unresolved**, with its source location. It is
   never dropped silently.

**Attribute resolution** (for Append/AppendTo): lookups set on early-bound properties
(`ctx_AccountId = …`, column from `[AttributeLogicalName]`), indexers
(`entity["parentcustomerid"] = new EntityReference(…)`) and `Attributes.Add` are found. The
referenced table is resolved from the `EntityReference` value like any other table. When it
can't be, the lookup's targets from the entity metadata in `Metadata/` are used: the single
target of a simple lookup (basis `direct`), or every target of a polymorphic lookup (customer,
regarding) with basis `all-lookup-targets`.

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

`Execute` is the generic way into Dataverse, so most real code reaches tables through request
objects as well as through the CRUD methods. The rules map each request type to the
privileges it needs. Phase 3.5 builds them.

**Rule table.** The rules are data (a table in `RequestRules`), not code, so they can be extended and
overridden in `XrmMockup:Permissions:Rules` (§6.3). A rule names the message (`Assign` or the
request type `AssignRequest`; matched without the `Request` suffix, ignoring case), the member
that holds the target, and the privileges. Further checks can name another member or a fixed
table, and apply only when a boolean member is set (`when`). A rule can also expand into other
requests, the way XrmMockup's handlers call each other, or hold nested requests (`requests`). A
configured rule replaces the built-in rule for its message:

```jsonc
{ "message": "AssignRequest", "target": "Target", "privileges": ["Assign", "Write"] },
{ "message": "SetStateRequest", "target": "EntityMoniker", "privileges": ["Write"], "enforced": false },
{ "message": "UpsertRequest", "as": ["CreateRequest", "UpdateRequest"] },
{ "message": "CreateMultipleRequest", "targets": "Targets", "as": ["CreateRequest"] },
{ "message": "ExecuteMultipleRequest", "requests": "Requests" },
{ "message": "GrantAccessRequest", "target": "Target", "privileges": ["Share", "$PrincipalAccess.AccessMask"] },
{ "message": "QualifyLeadRequest", "target": "LeadId", "privileges": ["Write"],
  "checks": [{ "table": "account", "privileges": ["Create"], "when": "CreateAccount" }, /* contact, opportunity */] },
{ "message": "WhoAmIRequest" }
```

`targets` makes the `as` rules' checks on `Target` apply to that member instead. `$Member` adds
the rights an `AccessRights` member grants; when it can't be read, the request is unresolved. A
`when` member is skipped only when it is a constant `false`.

Built-in rules, in groups:

| Requests | Privileges |
|---|---|
| `CreateRequest`, `UpdateRequest`, `DeleteRequest`, `RetrieveRequest`, `RetrieveMultipleRequest`, `AssociateRequest`, `DisassociateRequest` | Same as the matching `IOrganizationService` method (§3) |
| `UpsertRequest` | Create and Write on the target (whether the record exists is a runtime fact) |
| `CreateMultipleRequest`, `UpdateMultipleRequest`, `UpsertMultipleRequest`, `DeleteMultipleRequest` | As the single request, per table in `Targets` (`EntityCollection.EntityName`, or the tables of the entities added) |
| `SetStateRequest`, `AssignRequest`, `GrantAccessRequest`, `ModifyAccessRequest`, `RevokeAccessRequest`, `MergeRequest`, `QualifyLeadRequest`, `SendEmailRequest` and the other messages XrmMockup implements | As listed in §3, or from the handler |
| `InitializeFileBlocksUploadRequest` / `InitializeFileBlocksDownloadRequest` | Write / Read on `Target` |
| Requests that need no table privilege: block transfers that continue an initialized one (`UploadBlockRequest`, `CommitFileBlocksUploadRequest`, `DownloadBlockRequest`), metadata (`RetrieveEntityRequest`, `RetrieveAttributeRequest`, `RetrieveMetadataChangesRequest`, …), `WhoAmIRequest`, `RetrieveVersionRequest`, `FetchXmlToQueryExpressionRequest` | None. Not reported as unresolved |

Rules follow Dataverse. Some checks Dataverse makes aren't enforced by XrmMockup's handlers
(for example `SetState` and the file block requests). Each rule records whether
XrmMockup enforces it, so the cross-check in §9 knows which differences are expected.

**Requests through the code.** A request is tracked like a table (§5.5): through locals,
fields, parameters, return values and object initializers, so a request built in one method and
executed by a shared helper resolves at each call. Request collections
(`ExecuteMultipleRequest.Requests`, `ExecuteTransactionRequest.Requests`, and
`List<OrganizationRequest>` or similar passed between methods) hold everything added to them
with `Add`, `AddRange` or a collection initializer. Batching (`Chunk`, `Skip`/`Take`, `foreach`)
passes the contents through. The contents of a collection are the union of everything added to
it, so they over-approximate, like the rest of the call graph.

**Requests without a rule** stay unresolved, with a reason that says which:

- A request without a rule: "no rule for message 'Xxx'". It can be added in `Rules`.
- A request without a rule whose message is a custom API found in the code, a plugin step
  message in the metadata, or has a publisher prefix (`ctx_Approve`): recognized as a
  custom API call. Its execute privilege comes in phase 5 (§5.7) and the plugins it runs in
  phase 4. Until then it is listed under `Not checked` as a custom API call, apart from
  the rest.
- A request whose type or table can't be determined: the reason, as for other operations.

**Append and AppendTo** (also phase 3.5): Create, Update and their request forms (`Upsert`,
`*Multiple`, batches) add Append on the target and AppendTo on each table referenced by a
lookup that is set (see attribute resolution in §5.2), as XrmMockup checks them. `ownerid` and
`null` values are skipped, as in XrmMockup; the Read XrmMockup also checks for the UI role
doesn't apply to code. Lookups are followed through locals, collections, helpers and
parameters: a helper's parameter contributes the lookups set on it there, and the caller's.

Only lookups set in code are seen. A record from a source the analysis can't follow (retrieved,
a field, a deconstruction) adds no requirement and no unresolved entry, because it usually
carries no changed lookups. A lookup value whose table can't be resolved, with no metadata for
its column, is reported as unresolved with the reason prefixed `lookup value:`.

### 5.4 Execution identity ("runs as")

Each operation is tagged with the principal it runs under, because this decides which roles
are checked:

| Source of the service | Runs as |
|---|---|
| `factory.CreateOrganizationService(context.UserId)` / `localContext.OrganizationService` | **Caller** (or the step's impersonating user) |
| `factory.CreateOrganizationService(null)` / `OrganizationAdminService` | **System**: no role privilege needed, listed for information |
| `factory.CreateOrganizationService(<constant guid>)` | **User** (by id) |
| Step with `ImpersonatingUserId` in metadata, or `.SetUserContext(<guid>)` in XrmPluginCore | **User** (the impersonating user) replaces Caller for the step |
| Azure Function / app code (`ServiceClient`, injected `IOrganizationService`) | **Application user**, mapped to roles in `XrmMockup:Permissions:Principals` (§6.3); a `CallerId` assignment switches it to **Caller (impersonated)** |

When the source can't be determined, the operation defaults to the entry point's caller (Caller
for plugins, custom APIs and activities; Application user for functions and `--entry` methods) and
is flagged as assumed, with the reason listed under `assumptions`.

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
   step is included with basis `filter-unknown`. The set attributes are tracked like the
   lookups set on a record: early-bound properties, indexers and `Attributes.Add` on a created
   entity or a local, and on a parameter per call site. An entity from elsewhere, a computed
   column name, or an entity passed to another method in the solution makes them unknown.
3. Adds the step's requirements under the **effective** identity (caller, or the step's
   impersonating user), and expands the operations of the step in turn. Each step is expanded
   once per identity and basis per entry point, which bounds the expansion and stops cycles, so
   no depth limit is needed.

The messages an operation fires are derived from the operation:

- Direct calls fire their own message (`Create`, `Update`, `Delete`, `Retrieve`,
  `RetrieveMultiple`, `Associate`, `Disassociate`; `CreateAndReturn` fires `Create`), on the
  table of the call. `RetrieveMultiple` fires on the query's own table, not its linked tables.
- A request fires its rule's message and the messages it runs as (§5.3): `Upsert` fires
  `Upsert`, `Create` and `Update`, and `CreateMultiple` fires `CreateMultiple` and `Create`. The
  table is the one of the rule's first member check, usually `Target`. A request without a rule,
  such as a custom API call, fires its message without a table, which matches steps and custom
  APIs registered without a table.
- A step registered without a table matches every table.

The fired step ids are listed per entry point under `triggers`, including steps fired by other
steps.

This is what makes the "query a specific operation" mode useful: `Update account` gives the
full set of privileges a user needs for that update to succeed, including every plugin that
fires.

**`--operation`.** A user operation is an entry point of kind `Operation` with no code. It is
evaluated as a request of its message whose first member check (usually `Target`) refers to the
given table, run as `Caller`:

- The privileges of the member checks on that member are needed on the table, and those of
  fixed-table checks on their table. Checks on other members (`RelatedEntities`), access masks
  and nested requests can't be known and are reported as unresolved. A message without a rule
  is reported as unresolved and fires its message on the table.
- It fires the rule's messages on the table, setting the `--attributes` columns when given, so
  filtered Update steps match as for code (step 2). Without them, the columns set are unknown.
- For a message that creates or writes the table, each lookup among the attributes needs `Append`
  on the table and `AppendTo` on its targets from the metadata.

Its own hits are reported at the location `--operation` with no path, so a fired step's path
starts at the operation.

**Cascades from Azure Functions and app code.** The same expansion applies to the operations an
Azure Function (or an `--entry` method) performs, so `check --role "Integration App" --function
ProcessOrder` and `required --function ProcessOrder --role …` cover the plugins the function's
requests fire, not just the function's own calls. A request that fails in a plugin fails for the
function too. Specifics:

- **Identity**: fired steps run as the identity of the operation that fired them: the
  application user, or the impersonated user when the function sets `CallerId` (Dataverse then
  runs plugins as that user). A step's own impersonation (`ImpersonatingUserId`,
  `SetUserContext`) overrides it, as in step 3, and is checked against the `Users` mapping
  (§6.3) instead of the function's roles. Operations that run as System still fire plugins;
  those run as System unless impersonated.
- **Plugin code location**: function apps rarely reference their plugin projects, so the plugin
  code isn't in the analyzed solution. The step registry still comes from `Metadata/`, and the
  plugin code is added with a setting that lists extra solutions or projects to load for
  cascades only (for example `XrmMockup:Permissions:PluginSolutions`, or a repeatable
  `--plugins <path>`). Their own entry points aren't reported unless asked for.
- **Steps without code**: a metadata step whose plugin type isn't in any loaded code is listed
  as a finding ("step `X` fires but its code wasn't analyzed"), so missing plugin code is never
  silently treated as needing nothing.
- **Unresolved operations**: an operation whose table is unknown fires the steps registered without
  a table only. The steps on a table are reported as one unresolved finding per operation ("may fire
  any of N steps") rather than included: in a real solution every step of the message, and
  everything those fire, would be most of the solution, which says nothing about the operation.
  Tables are resolved where they can be first: the record `Retrieve` returns is of the table it
  asks for, an early-bound lookup property refers to its targets in the metadata, and
  `PrimaryEntityName` in a plugin step is the step's table.
- **Output**: plugin requirements keep the full path, from the function through the request to
  the step and into the plugin code (`ProcessOrder → OrderService.Submit → Create
  ctx_order → plugin:Contoso.OrderPostPlugin:Create:ctx_order:PostOperation → …`), and the
  entry point lists the steps it fires under `triggers` (§7). Asynchronous steps are included,
  since they need the same privileges even though their failure doesn't fail the request.
  Marking them as asynchronous needs the step mode, which the code registrations don't carry
  yet, and is deferred.

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
- Each requirement is checked against the roles of the identity it runs as (§5.4):

  | Runs as | Checked against |
  |---|---|
  | Caller | `--role` |
  | Application user | `--role`, else `Principals` for the function (`<assembly>/<function>`), else for its app (`<assembly>`) |
  | User | `Users` for that user id; otherwise "not checked: runs as user X" |
  | Caller (impersonated) | `--caller-role` (repeatable); otherwise "not checked" |
  | System | Nothing; listed for information |

  `--role` replaces `Principals`. Requirements with an assumed identity are checked like the
  rest and keep their `(assumed)` label, so a missing privilege on one fails `check`.
- **Impersonation**: a call made with `CallerId` set runs with the privileges of the
  impersonated user, so its requirements are checked against `--caller-role`. The identity that
  sets `CallerId` (the application user, for app code) must also hold
  `prvActOnBehalfOfAnotherUser`. That is a global privilege, which the role metadata doesn't
  contain until phase 5, so it is reported per entry point as unverifiable. It fails only
  with `--strict`.
- Without `--role` or `--caller-role`, and with no `Principals` or `Users` entry that matches
  the analyzed code, `check` has nothing to check against and exits with `2`. A role name that
  isn't in `Metadata/` is also an error (exit `2`), which suggests the closest names.
- `Suppress` entries (§6.3) turn a missing privilege into "suppressed", which doesn't fail.
  A table no role in `Metadata/` mentions counts as covered, because XrmMockup doesn't check it.
- `--strict` also fails on unresolved operations, identities with no roles to check against
  and unverifiable privileges.
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

As built in phase 3, the text output lists the roles per identity under each entry point
(`roles for Caller: Salesperson (--role)`) and marks each requirement `✔` (granted, with the
depth, or on a table no role governs), `✘` (missing), `-` (suppressed) or `·` (not checked, or
SYSTEM). The impersonation privilege is a `!` line.

The `Result:` line only counts what was checked. A `Missing privileges:` list follows it, then a
`Not checked:` line, so a pass isn't read as proof that the roles are enough:

```
6 entry points, 2 unreached methods, 9 tables, 14 table privileges (31 requirements counted per entry point and method), 5 unresolved
Result: 27 granted, 4 missing
Missing privileges:
  ✘ ctx_orderline  Create  ApplicationUser  Integration App (--role)  in function:ProcessOrder, function:ResubmitOrder
  ✘ ctx_orderline  Read    ApplicationUser  Integration App (--role)  in function:GetOrder
Not checked: 5 unresolved operations (1 request without a rule, 3 custom API calls, 1 with an unknown table or request)
```

`Missing privileges` merges the missing requirements by table, privilege, identity and roles,
so each gap is listed once with every entry point that needs it. `Not checked` lists unresolved
operations (requests with no rule and custom API calls apart from operations with an unknown
table or request, §5.3), requirements whose identity has no roles, entry points whose
impersonation privilege is unverifiable. The line is left out when everything was checked. When nothing is missing but something wasn't
checked, `check` exits `0` with a warning, and `--strict` fails.

**`required`**: exactly what is needed, optionally with role coverage.

```
xrmmockup-permissions required                                   # full solution manifest
xrmmockup-permissions required --operation "Create contact"
xrmmockup-permissions required --function ProcessOrder --role "Integration App"
```

- With no role options: the requirement manifest per entry point (table → privileges,
  identity, basis, locations). It contains no role data, so it depends only on code and
  metadata. This is the artifact to commit or diff in CI.
- `--role <name>` and `--caller-role <name>` (repeatable): the same manifest with a
  granted/missing column for the given roles, plus the `Principals` and `Users` settings. It
  reports, like `check`, but doesn't change the exit code.

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
- The tool makes no Dataverse connection (a future `check --user` would be the exception,
  §10), so it ignores `DataverseUrl` and
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
    "id": "plugin:Contoso.AccountPostPlugin:Update:account:PostOperation",
    "kind": "PluginStep",
    "type": "Contoso.AccountPostPlugin", "assembly": "Contoso.Plugins",
    "trigger": { "message": "Update", "table": "account", "stage": "PostOperation" },
    "impersonatingUserId": "…",          // only when the step impersonates
    "declaration": { "file": "Plugins/AccountPostPlugin.cs", "line": 13, "column": 13 },
    "requirements": [{
      "table": "ctx_parent", "privilege": "Create",
      "runsAs": "Caller", "basis": "direct",   // "userId" for User, "assumed": true when guessed
      "sources": [{ "file": "Plugins/AccountPostPlugin.cs", "line": 37, "column": 21,
                    "path": ["AccountPostPlugin.Execute"] }]
    }],
    "unresolved": [ /* operation + location + reason + path */ ],
    "assumptions": [ /* reason + location */ ],
    // With roles only (check, required --role): each requirement also gets
    // "coverage": { "status": "granted", "depth": "User" }   (or missing, suppressed + reason, not-governed, not-checked, system)
    "roles": [{ "runsAs": "Caller", "roles": ["Salesperson"], "origin": "--role" }],   // or "note" when not checked
    "impersonation": { "privilege": "prvActOnBehalfOfAnotherUser", "runsAs": "ApplicationUser", "status": "unverifiable" },
    "triggers": [ "plugin:Contoso.ContactPostPlugin:Create:contact:PostOperation" ]   // phase 4
  }],
  "unreached": [ /* per-method manifests, as in phase 1 */ ],
  "notRoleGoverned": ["activityparty"],
  "missing": [{ "table": "contact", "privilege": "Create", "runsAs": "Caller", "roles": ["Salesperson"],
                "origin": "--role", "in": ["plugin:Contoso.AccountPostPlugin:Update:account:PostOperation"] }],   // with roles only
  "result": { "granted": 12, "notGoverned": 0, "missing": 2, "suppressed": 0, "notChecked": 1,
              "system": 1, "unresolved": 1, "unresolvedNoRule": 0, "unresolvedCustomApi": 0, "unverifiable": 0,
              "incomplete": true }   // with roles only
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
| 3.5 | Execute rule table (§5.3): built-in rules, `Rules` setting, `ExecuteMultiple`/`ExecuteTransaction` and `*Multiple` requests, requests and request collections followed through the call graph, requests needing no table privilege, custom API calls recognized; Append/AppendTo via relationship metadata | Requests and lookups checked; what is left under `Not checked` is what static analysis can't see |
| 4 | Plugin cascade expansion (done: steps and custom APIs fired by direct calls and requests, from every entry point including Azure Functions and app code, identity, unknown tables reported, steps without code, FilteredAttributes, `--operation` scope), then plugin code loaded from other solutions (§5.6) | "What does a user need to do X"; a function's roles checked against the plugins it fires |
| 5 | LINQ and `OrganizationServiceContext` (§5.2); privilege catalog + global privileges in the metadata generator, custom API execute privileges (§5.7), `prvActOnBehalfOfAnotherUser` checked | Near-complete coverage |
| 6 | SARIF/Markdown output, docs, NuGet tool packaging alongside `xrmmockup-metadata` | Release |
| Later | Role suggestions (which single role, or smallest combination of downloaded roles, covers a requirement), minimum required depth and roles from a user's assignments (see below), IDE `DiagnosticAnalyzer` package on Core, real-time workflow support, runtime recorder cross-check | |

**Idea: minimum required depth.** A `--min-depth` option (or per-table setting) that only
counts a privilege as covered at or above a given depth, so CI could, for example, require
`Organization` Read on reference tables. It needs a detailed specification before it is
built: whether the minimum applies globally, per table, per privilege or per entry point;
how it interacts with combined roles and with tables no role governs; how it relates to the
ownership XrmMockup actually checks at runtime (owner, business unit, sharing); and how it is
configured in `XrmMockup:Permissions`. Deferred until then.

**Idea: roles from a user's current assignments.** `check --user <username>` (e.g.
`--user jane@contoso.com`) looks the user up in Dataverse and checks against the roles they
actually hold, instead of a hand-written `--role` list:

- Look up the `systemuser` by `domainname` (UPN), with `internalemailaddress` as a fallback.
  An unknown or ambiguous user is an error.
- Collect the user's directly assigned roles (`systemuserroles`) and the roles of every team
  they belong to (`teammembership` → `teamroles`), including Microsoft Entra group teams. In
  Dataverse, both kinds add privileges.
- Match each role to the role definitions in `Metadata/` through its root role
  (`parentrootroleid`), because every business unit has its own copy of a role with a
  different id. A role that isn't in `Metadata/` is an error that suggests regenerating the
  metadata, so the check never silently runs against fewer roles than the user has.
- The output names each role and where it comes from (direct, or which team).
- The same lookup serves impersonated calls: a `--caller-user <username>` option would give
  the roles of the end user that app code acts for through `CallerId`, instead of a
  hand-written `--caller-role` list. The impersonating identity still needs
  `prvActOnBehalfOfAnotherUser` (§6.2).

Things to settle before building it:

- It is the first feature that connects to Dataverse. It would reuse the metadata tool's
  connection settings (`DataverseUrl`, `DataverseCredentialType`, `--credential-type`).
  Only `check --user` connects; `required` stays offline (goal in §2, §6.3).
- The result depends on live data, so it is not deterministic over time. The output should
  record the environment URL, the user and when the roles were read. A `--save-roles` option
  could write the resolved role list to a file, so a later run can reproduce the check
  without connecting.
- Whether to use the user's business unit, and the business units of the teams, once depth
  matters (see minimum required depth above).
- The signed-in identity needs read access to `systemuser`, `team` and `role`.

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
- **Roles per identity**: `--role` covers the caller and the application user, `--caller-role`
  the user acted for through `CallerId`, and `Users` specific users. A requirement whose
  identity has no roles is "not checked" instead of silently passing or failing (§6.2).
- **Role suggestions deferred**: the first version answers "what is needed" and "do these
  roles cover it". Working out which roles would cover a requirement comes later.
