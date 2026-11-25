# Documentation migration verification and human review

Review date: **November 22, 2026**.

## Scope and provenance

The companion repository `https://github.com/delegateas/XrmMockup.wiki.git` was cloned
into a separate temporary checkout. The workspace-managed origin was not changed.
The imported source is wiki commit
`301684679ab6c1a45ccd6d56e54c18fc13cfc39f`, last committed on **November 3, 2023**
(`Added link to MS docs for connecting to D365 with tooling`). Verification used the
local product source at commit `9753791bd17d376caaf20fd10238dc99726fd29f`.
No wiki content, Git branches, tags, or pull requests were published.

The checkout contains **17 tracked documentation files**: 15 topic/example pages,
`Home.md`, and `_Sidebar.md`. One topic has an `.asciidoc` extension but already uses
Markdown headings and fenced code; it was migrated as Markdown. There are **no
tracked image assets**. All topic pages were imported; Home and sidebar were combined
into the documentation index rather than maintained as duplicate navigation pages.

Verification means source/API and checked-in regression-test inspection plus
migration/link checks. It does **not** mean live Dataverse parity or successful
execution of the historical examples. No metadata or context classes were regenerated,
and no live environment was provisioned. External dependencies and pending gaps are
listed below so that a human can decide what to modernize.

## Source-to-destination inventory

| Wiki file | Repository destination |
| --- | --- |
| `Home.md` | [Documentation index](README.md), with complete-clone claims corrected |
| `_Sidebar.md` | [Documentation index](README.md), merged with existing documentation navigation |
| `Setup.md` | [Setup](setup.md) |
| `Local-Plugin-Registration.asciidoc` | [Local plugin registration](local-plugin-registration.md) |
| `FAQ.md` | [FAQ](faq.md) |
| `How-it-works.md` | [How it works](how-it-works.md) |
| `IOrganizationService:-UI-vs-SDK.md` | [UI vs SDK](organization-service-ui-vs-sdk.md) |
| `Example:-Plugin.md` | [Plugin example](examples/plugin.md) |
| `Example:-Workflow.md` | [Workflow example](examples/workflow.md) |
| `Example:-CodeActivity.md` | [Code activity example](examples/code-activity.md) |
| `Example:-Security-Role.md` | [Security role example](examples/security-role.md) |
| `Example:-Time-Control.md` | [Time control example](examples/time-control.md) |
| `Example:-Snapshot.md` | [Snapshot example](examples/snapshot.md) |
| `Example:-Owner-Teams.md` | [Owner teams example](examples/owner-teams.md) |
| `Example:-Debug-using-live-data.md` | [Live-data example](examples/debug-using-live-data.md) |
| `Implementing-a-new-request.md` | [Request-handler contribution guide](implementing-a-new-request.md) |
| `Reporting-a-bug.md` | [Bug-reporting guide](reporting-a-bug.md) |

All seven pre-existing files under `docs/` were retained without edits and linked in
the index: auto-number, permission SDK, release flow, release sequence, alternate-key
design, permission-analyzer design, and permission-analyzer refactor notes. Tool READMEs,
`AGENTS.md`, and `RELEASE.md` remain their respective reference sources instead of
being copied or superseded. The main README now links to repository documentation.

## Clear errors corrected during migration

These are factual corrections, not implementation of the pending documentation proposals.

| Correction | Verification evidence |
| --- | --- |
| Removed complete/exact clone and all-platform-logic guarantees from the Home summary/main README; documented supported simulation instead. | [Core request dispatch](../src/XrmMockup365/Core.cs), [request-handler guide](implementing-a-new-request.md), and incomplete-analysis boundaries in [the SDK guide](permission-analysis-sdk.md). |
| Current package/framework statement uses `XrmMockup365` and `net462`, `net48`, `net10.0`, rather than implying a separate maintained project for every historical CRM version. | [Engine project](../src/XrmMockup365/XrmMockup365.csproj). The legacy generator walkthrough was not modernized. |
| Corrected `createUser`/`createTeam`, the generated role file name, and the role namespace to `CreateUser`/`CreateTeam`, `TypeDeclarations.cs`, and `DG.Tools.XrmMockup.SecurityRoles`. Replaced the undefined `SR` alias. | [Public helper APIs](../src/XrmMockup365/XrmMockupBase.cs), [role declaration skeleton](../files/TypeDeclarations.cs). |
| Replaced “owner teams only; not Access teams” with limited, tested access-team support. | [TestAccessTeams](../tests/XrmMockup365Test/TestAccessTeams.cs), [AddUserToRecordTeam handler](../src/XrmMockup365/Requests/AddUserToRecordTeamRequestHandler.cs). |
| Corrected the proxy-type workaround to the actual `EnableProxyTypes` and `Assemblies` APIs and distinguished plugin discovery from explicit proxy selection. | [Settings](../src/XrmMockup365/XrmMockupSettings.cs), `EnableProxyTypes` in [XrmMockupBase](../src/XrmMockup365/XrmMockupBase.cs). |
| Qualified console tracing as default-factory behavior, not a guarantee for custom tracing factories. | [TracingService](../src/XrmMockup365/TracingService.cs), [tracing factory](../src/XrmMockup365/TracingServiceFactory.cs). |
| Fixed the nonexistent `MockupServiceSetting` type and the incorrect claim that Boolean/picklist defaults are UI-only. Documented both-role defaults and an actual conditional UI check. | [MockupServiceSettings](../src/XrmMockup365/MockupServiceSettings.cs), [Create handler](../src/XrmMockup365/Requests/CreateRequestHandler.cs), SDK-mode default tests in [TestCreate](../tests/XrmMockup365Test/TestCreate.cs). |
| Replaced nonexistent `XrmMockupShared` paths and CRM 2011/2013 compile-switch example with current Requests/Core locations and target-framework guidance. | [RequestHandler](../src/XrmMockup365/Requests/RequestHandler.cs), `GetRequestHandlers` in [Core](../src/XrmMockup365/Core.cs), [engine project](../src/XrmMockup365/XrmMockup365.csproj). |
| Fixed owner-team snippet: pass the declared team into `CreateTeam`, use an existing business-unit reference, consistently use `user1` as an `EntityReference`, assign `OwnerId` that reference directly, remove nonexistent reference-to-reference conversions, and retrieve through the SDK service. `CreateTeam` sets the owner-team type without requiring a generator-specific enum name. | [Helper signatures and owner-team creation](../src/XrmMockup365/XrmMockupBase.cs), [TestTeams](../tests/XrmMockup365Test/TestTeams.cs). Role assumptions and fixture completeness remain R5. |
| Corrected the plugin comment to “create contact”, CodeActivity suffix capitalization, workflow scenario's trailing space, and waiting-workflow scenario to update Description rather than Name. | [Plugin snippet](examples/plugin.md); matching assertions in [TestWorkflow](../tests/XrmMockup365Test/TestWorkflow.cs). |
| Corrected the claim that `AssemblyInitialize`/static methods are required for snapshots. Named snapshots are instance-local, and the methods may be called when an instance is available. Qualified the list as the named snapshot API, not every snapshot API. | [Snapshot APIs](../src/XrmMockup365/XrmMockupBase.cs), instance `snapshots` dictionary in [Core](../src/XrmMockup365/Core.cs), [TestSnapshot](../tests/XrmMockup365Test/TestSnapshot.cs). |
| Replaced wiki navigation with portable relative Markdown links, added page titles/back-links, and linked the existing guides. | Source inventory above and local link validation. The missing “Code Structure” target was recorded as R8, not invented. |

## Human review required — proposals not implemented

**The sections below are a backlog, not new product documentation.** Missing,
incomplete, deprecated, and uncertain substantive guidance was retained and flagged,
not filled in or removed. A warning/reference link does not approve the historical
instructions. Reviewers should approve the proposed scope before rewriting it.
Priorities describe documentation risk, not promised engineering work.

### R1: Legacy setup and generator configuration

**Priority: high. Category: deprecated/incomplete documentation.**

- Affected: [Setup](setup.md) sections 2–4 and the generator answer in [FAQ](faq.md).
- Finding: the wiki documents an executable/config file, CRM-version suffixes,
  username/password and `OnlineFederation`, plus legacy argument aliases. The current
  engine still bundles the legacy `net462` generator, so this is not evidence that
  the generator has been removed or formally deprecated. However, those instructions
  are not the modern CLI's configuration/authentication contract, and their exact
  NuGet installation/output paths have not been tested with a current consumer project.
- Evidence: [engine package content entries](../src/XrmMockup365/XrmMockup365.csproj),
  [legacy App.config](../src/MetadataGen/MetadataGenerator365/App.config), and the
  [modern tool README](../src/MetadataGen/MetadataGenerator.Tool/README.md).
- Proposed review: decide whether to archive the legacy walkthrough or maintain a
  Windows/.NET Framework appendix. Approve a modern install/generate/load walkthrough
  that links the tool README rather than duplicates its options. Verify package output
  paths, entity selection, generated files, and metadata freshness. Keep credentials
  out of source control; do not run live metadata regeneration for this review.
- Migration action: preserved the configuration, argument table, and executable steps;
  added a legacy warning and link to the existing modern reference.

### R2: Shared static MSTest base class and metadata location

**Priority: high. Category: incomplete/outdated documentation.**

- Affected: [Setup](setup.md), and examples that assume its shared `crm` instance.
- Finding: the wiki has static `crm`, assembly initialization, and per-test
  `ResetEnvironment`, with no parallel-test policy or explicit metadata-directory
  handling. The current fixture shares settings for metadata-cache reuse but creates
  a fresh simulation for each test. The wiki never shows a complete consumer project,
  package references, generated-type namespaces, or working-directory-independent
  `MetadataDirectoryPath` setup.
- Evidence: [XrmMockupFixture](../tests/XrmMockup365Test/XrmmockupFixture.cs),
  [UnitTestBase](../tests/XrmMockup365Test/UnitTestBase.cs),
  [settings](../src/XrmMockup365/XrmMockupSettings.cs),
  [instance/cache entry point](../src/XrmMockup365/XrmMockup.cs).
- Proposed review: approve a runnable, per-test-isolated consumer quickstart and an
  explicit metadata path strategy; decide whether MSTest and xUnit both need examples.
  Sharing immutable settings must not be confused with sharing mutable simulations.
- Migration action: preserved the base class; linked the current fixture for comparison.

### R3: Live-data authentication and platform boundary

**Priority: high. Category: deprecated documentation.**

- Affected: [Debug using live data](examples/debug-using-live-data.md).
- Finding: `Env.providerType`, `uri`, `username`, and `password` no longer exist.
  Current `Env` exposes only `Url`; both it and `OnlineEnvironment` are compiled only
  under `DATAVERSE_SERVICE_CLIENT` (.NET 10). This example cannot compile against
  current targets as written.
- Evidence: [Settings/Env](../src/XrmMockup365/XrmMockupSettings.cs),
  [engine conditional compilation](../src/XrmMockup365/XrmMockup365.csproj),
  [OnlineDataService](../src/XrmMockup365/Online/OnlineDataService.cs).
- Additional discrepancy: the settings XML comment claims `DefaultAzureCredential`,
  while `OnlineDataService` explicitly documents DataverseConnection's default
  interactive-browser authentication and configuration for `azcli`/`devicecode`.
  Confirm dependency behavior before adopting either comment as the public contract.
- Proposed review: approve replacement with `OnlineEnvironment = new Env { Url =
  "https://your-org.crm.dynamics.com" }` on .NET 10, verified authentication instructions,
  read/fallback/caching scope, local mutation behavior, and a separate opt-in debugging
  configuration. Do not store user passwords or turn ordinary unit tests into live tests.
- Migration action: kept the legacy example behind an explicit do-not-use-as-current-setup
  warning. Neither it nor the conflicting source comments were rewritten.

### R4: Third-party comparison

**Priority: medium. Category: unverified/outdated documentation.**

- Affected: the FakeXrmEasy comparison in [FAQ](faq.md), and repeated XrmContext versus
  CrmSvcUtil recommendations in the example pages.
- Finding: the 2023 wiki makes absolute claims about another project's mocking,
  workflow, security, and process-chain support without a version or reproducible
  comparison. XrmMockup's source cannot establish another project's current behavior.
  The repeated XrmContext comparison link was retained, not independently validated.
- Proposed review: remove the comparison or substantiate a neutral, dated, version-scoped
  comparison using current primary documentation. Decide whether to retain the repeated
  third-party generator recommendation or simply state the examples' required types.
- Migration action: preserved the FAQ comparison under a historical/unverified warning;
  did not assert that its claims are true or silently replace them with new claims.

### R5: Example prerequisites and test framework

**Priority: high. Category: incomplete documentation.**

- Affected: [Plugin](examples/plugin.md), [Workflow](examples/workflow.md),
  [Code activity](examples/code-activity.md), [Security role](examples/security-role.md),
  [Owner teams](examples/owner-teams.md), and [Time control](examples/time-control.md).
- Finding: snippets assume MSTest, a DAXIF-style `Plugin` base, generated `Xrm`/entity
  classes, services and fields, and preinstalled workflows/metadata. They are not
  standalone projects. The checked-in workflow tests explicitly call `AddWorkflow`
  and the fixture uses `IncludeAllWorkflows = false`, unlike the wiki setup.
- Security example: `Scheduler`/Lead depend on the consumer's downloaded role and
  entity metadata; the checked-in role declarations do not contain `Scheduler`.
  The try/catch catches general `Exception` and identifies a fault type rather than
  a specific missing Create privilege. Owner-team tests assume particular Account
  read depths for `SystemCustomizer`; that is an environment-dependent role definition,
  not a guaranteed platform invariant. God-service fixture setup must be explained.
- Evidence: [TestWorkflow](../tests/XrmMockup365Test/TestWorkflow.cs),
  [fixture](../tests/XrmMockup365Test/XrmmockupFixture.cs),
  [TestTeams](../tests/XrmMockup365Test/TestTeams.cs),
  [checked-in role GUIDs](../tests/XrmMockup365Test/Metadata/TypeDeclarations.cs).
- Proposed review: choose a supported consumer scaffold and imports, specify workflow
  registration/loading and required role depths, and replace overly broad fault assertions
  with precise failures. Do not invent universal role privileges or regenerate fixtures.
- Migration action: corrected clear variable/API errors and scenario text; added excerpt
  warnings. Did not complete scaffolds, change role definitions, or convert examples to xUnit.

### R6: Workflow screenshots and waiting-workflow setup

**Priority: high. Category: unavailable assets/incomplete documentation.**

- Affected: [Workflow](examples/workflow.md) and [Time control](examples/time-control.md).
- Finding: the wiki has no tracked assets. Its only two images are hosted at
  `http://i.imgur.com/vKmRQGD.png` and `http://i.imgur.com/gcJUSPW.png`. Attempts to fetch
  their HTTPS equivalents during migration returned **HTTP 403**. This proves they
  were inaccessible here, not that they are permanently deleted. No placeholder or
  fabricated screenshot was substituted; the original image references remain.
- The waiting example omits creation/selection of the Danish currency required by
  its workflow. The current [TestTimeWorkflow](../tests/XrmMockup365Test/TestWorkflow.cs)
  explicitly sets up `Dansk krone` and supplies `TransactionCurrencyId`. The example's
  `AddDays(1)` then `AddDays(2)` advances a cumulative three days; it checks eventual
  completion, not a precise two-day boundary.
- Proposed review: recover the original screenshots (including permission to store them)
  as repository-local assets or replace them with reviewed textual workflow definitions.
  Approve explicit currency/workflow prerequisites and decide whether to show an exact
  wait-boundary assertion. Do not synthesize workflow logic from unavailable pictures.
- Migration action: preserved image references and incomplete setup, corrected only the
  Name/Description scenario mismatch, and flagged these limitations.

### R7: Snapshot scope and test isolation

**Priority: medium. Category: incomplete documentation.**

- Affected: [Snapshot example](examples/snapshot.md).
- Finding: the wiki describes only named database snapshots and a shared MSTest setup.
  Current snapshots also capture security, time offset, organization/user references,
  and auto-number state. There are ZIP/JSON-backed APIs, while queued workflows,
  plugin registrations, and traces are not described as part of the snapshot contract.
  Restoring named snapshots from another instance is not supported by the per-instance map.
- Evidence: snapshot capture/restore in [Core](../src/XrmMockup365/Core.cs),
  [public APIs](../src/XrmMockup365/XrmMockupBase.cs),
  [TestSnapshot](../tests/XrmMockup365Test/TestSnapshot.cs),
  [TestZipSnapshot](../tests/XrmMockup365Test/TestZipSnapshot.cs), and
  existing [auto-number lifecycle guidance](auto-number.md#environment-lifecycle).
- Proposed review: define precisely what is restored, what is not, repeated restore/reset
  behavior, ZIP portability and compatibility, disposal, and parallel-test isolation.
  Reconcile amortized shared setup with the recommended fresh-instance fixture pattern.
- Migration action: corrected false initialization requirements; did not add an unreviewed
  comprehensive snapshot contract or replace the historical shared-instance example.

### R8: Missing Code Structure page

**Priority: low. Category: missing documentation.**

- Affected: the old `_Sidebar.md` links to `Code Structure`, but no matching page exists
  in the cloned wiki repository.
- Evidence: complete 17-file inventory above. [AGENTS.md](../AGENTS.md#project-and-code-map)
  already provides a repository code map.
- Proposed review: decide whether the agent-oriented code map is sufficient for contributors
  or whether a separate human-facing architecture guide is needed. Do not create a page
  from an absent source or retain a dead internal navigation link.
- Migration action: index links the existing code map; no replacement article was invented.

### R9: Current engine features without user guides

**Priority: medium. Category: missing/incomplete documentation.**

- Finding: the wiki does not provide dedicated current guides for several public APIs
  now present in the engine. This is an API inventory, not a claim of exhaustive
  feature coverage or formal deprecation of older features.
- Candidates for review: `IPluginMetadata` direct registration and XrmPluginCore
  registration alongside the documented DAXIF strategy; `BaseCustomApiTypes` and
  `StrictCustomApiParameters`; async services/target differences; Power Fx field
  evaluation; `TraceLog`, `PluginTraceLog`, custom tracing factories, workflow diagnostics,
  `LogFilePath`, `MinLogLevel`, and `LoggerFactory`; access-team setup and security depth
  boundaries; supported request inventory and unsupported-feature limitations.
- Evidence: [Settings](../src/XrmMockup365/XrmMockupSettings.cs),
  [public base APIs](../src/XrmMockup365/XrmMockupBase.cs),
  [async APIs](../src/XrmMockup365/XrmMockupBaseAsync.cs),
  [PluginManager](../src/XrmMockup365/Plugin/PluginManager.cs),
  [Core request registrations](../src/XrmMockup365/Core.cs).
- Proposed review: prioritize task-oriented guides and supported-target examples; verify
  option defaults in code and tests. Link existing auto-number and permission-analysis
  docs instead of rewriting them. Establish the difference between runtime security
  enforcement and explicit static privilege analysis.
- Migration action: existing guides were indexed; no missing feature tutorials were written.

### R10: Request-handler contribution guide

**Priority: medium. Category: incomplete documentation.**

- Affected: [Implementing a new request](implementing-a-new-request.md).
- Finding: “copy a handler and implement Execute” omits `CheckSecurity`,
  `InitializePreOperation`, pipeline ordering, typed SDK fault propagation, rollback,
  and tests for alternate-key/bulk routing and security where applicable.
- Evidence: [RequestHandler](../src/XrmMockup365/Requests/RequestHandler.cs),
  [Core](../src/XrmMockup365/Core.cs),
  existing [alternate-key design](design/alternate-key-uniqueness.md),
  [build/test instructions](../AGENTS.md#build-and-validation).
- Proposed review: approve a lifecycle/security checklist and a minimal example based on
  a current handler. Specify when each concern applies instead of assuming copying an
  existing Execute method is enough. Keep behavior compatible across all engine targets.
- Migration action: fixed paths and obsolete target switches; linked build instructions
  and left the incomplete lifecycle guidance flagged for review.

### R11: Existing design/refactor documents versus shipped references

**Priority: medium. Category: draft/possibly stale documentation.**

- Affected: [Permission Analyzer design](design/permission-analyzer.md) and
  [candidate refactors](design/permission-analyzer-refactors.md).
- Finding: the design remains `Status: Draft` and includes CLI/configuration examples,
  a delivery plan, and future runtime role-verification proposals. Refactor notes mix
  completed work, historical parity findings, and later consolidation ideas. These
  should not be interpreted as the shipped tool/API contract. Runtime scenario
  verification sketches are not the current static `CheckPermissions` API.
- Evidence: current [permission SDK guide](permission-analysis-sdk.md),
  [CLI README](../src/PermissionAnalyzer/PermissionAnalyzer.Tool/README.md),
  [SDK implementation](../src/XrmMockup365/PermissionChecks.cs).
- Proposed review: decide what to archive, reconcile delivery-plan status and options
  against the current CLI, and refresh refactor/parity findings. Preserve historical
  design intent and separate future proposals from supported behavior. Do not create
  automatic CRUD permission analysis or conflate static checks with record-level proof.
- Migration action: files retained byte-for-byte; index labels them as draft/design notes.

### R12: Release documentation operational status

**Priority: low. Category: incomplete operational verification.**

- Affected: existing [release flow](release-flow.md), [release sequence](release-sequence.md),
  and [RELEASE.md](../RELEASE.md).
- Finding: these describe local workflow/script implementation and required GitHub
  setup; repository files alone do not establish production branch rules, token
  permissions, environments, NuGet credentials, or whether setup is complete.
- Proposed review: maintainers should confirm actual repository configuration and that
  the November 13, 2026 diagrams still reflect the deployed process. Keep `RELEASE.md`
  authoritative and never hand-edit generated pending changelogs during docs cleanup.
- Migration action: existing release documents were not changed, release automation was
  not executed or published, and no production-readiness claim was added.

## Validation and limitations

- Wiki checkout succeeded; tracked file inventory and exact source commit recorded.
- All 15 topic pages were mapped to Markdown files; Home/sidebar merged into one index.
- Local Markdown document/file links and heading anchors were checked: **25 Markdown
  files and 180 local links**, including the main README, new pages, and retained docs.
  Wiki-specific navigation is no longer needed; inventory and code-fence checks passed.
- Existing seven `docs/` files were checked for unintended edits and retained unchanged.
- Code/API corrections were checked against local implementations and regression-test source.
  Historical snippets still need the prerequisites and review work above; they were not
  compiled as standalone examples.
- Both workflow-image HTTPS fetches failed with HTTP 403. Other retained external
  comparisons/authentication references were not comprehensively validated.
- `dotnet --info` failed with `dotnet: command not found`. No .NET build or tests ran.
  The full Windows/multi-target suite and example compilation remain unverified.
- No live Dataverse requests, metadata regeneration, commits, pushes, or publishing
  were performed. Git changes remain local for human review.
