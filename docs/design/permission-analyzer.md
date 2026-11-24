# Permission analyzer

Static privilege analysis shared by the `xrmmockup-permissions` CLI and explicit
XrmMockup SDK checks. It reads C# source; it does not execute business logic or prove
record-level access.

Usage and configuration: [CLI reference](../../src/PermissionAnalyzer/PermissionAnalyzer.Tool/README.md).
SDK setup: [permission checks](../permission-analysis-sdk.md).

## Architecture

- **Abstractions** (`netstandard2.0`): SDK-free scopes, manifests, roles and coverage.
- **Core** (`netstandard2.0`): Roslyn discovery, extraction, call graphs and cascades.
- **Workspace** (`net10.0`): MSBuild source loading, configuration and metadata readers.
- **Tool** (`net10.0`): CLI, optional live role lookup and report writers.

These are shared internal libraries, not independently released packages. Core's
existing package carries an opt-in analyzer payload, loaded in an isolated context;
neither dotnet tool is bundled into Core.

Pipeline: load compilations → discover entry points → extract method summaries →
evaluate reachable calls and plugin/workflow triggers → build manifest → check roles.
Source graphs can be reused; SDK metadata, roles and assignments are read at check time.
Checks never mutate mockup state or run automatically during CRUD.

## Analysis contract

The output is **known requirements plus explicit incompleteness**, not an unconditional
superset of every runtime privilege. Within supported constructs, branches and possible
interface implementations are conservatively unioned. Every requirement retains its
identity, basis, source and call path.

Supported entry points include registered plugins, custom APIs, code activities, Azure
Functions, explicit methods and synthetic message/table operations. Helpers, DI,
request batches, queries, entity lookups and service identities are followed through
source. Metadata registrations and additional plugin projects supply cascades.
Multi-targeted compilations merge resolved facts into an ordinally ordered union.

Unknown operations, lookup contents, workflow bodies, missing plugin source, source-load
or compilation failures, and call/resolution-depth limits make analysis incomplete.
A known table does not make unknown lookup contents safe to ignore. Synthetic writes
without specified attributes report unknown lookup contents. Explicit attributes are
interpreted against the supplied lookup metadata; that metadata must match the environment.
Known empty lookup sets from constructors and helpers remain empty, not unresolved.

Synchronous and asynchronous cascades are included. Async requirements describe background
work as well as the initiating operation; an async failure need not fail that operation.
Bypass parameters suppress only steps that can definitely be shown to be skipped.

## Identities and coverage

- Caller, application user, explicit user and SYSTEM remain separate identities.
- Unknown service identities are labelled assumed; they do not erase requirements.
- SYSTEM needs no role coverage. Other identities without established roles are unchecked.
- Roles combine by privilege union and maximum granted depth. Any depth counts; ownership,
  business units, sharing and field-level security are not proved.
- `CallerId` requires the operation's privileges on **both** the application user and end
  user, including global privileges. The application user additionally needs directly
  assigned `prvActOnBehalfOfAnotherUser`; team inheritance cannot grant delegation.
  Explicit role lists/configured mappings describe direct assignments. Live and local user
  checks retain direct/team provenance while combining other privileges normally.
- A table absent from all supplied role metadata is **unverifiable**, not an established
  security exemption. Missing global privilege data is also unverifiable.

Rules describe required privileges independently of what XrmMockup currently enforces.
`Enforced = false` is reported as an assumption, including through composed rules; it
never removes a requirement. Runtime-dependent checks that cannot be resolved are
reported as unresolved, rather than asserted away (for example email regarding/sender
and lookup values copied by compound messages).

The CLI normally reports without failing. `--fail-on-missing` fails on missing privileges;
`--strict` also fails on incompleteness and unverifiable/unchecked coverage. Configuration
and loading errors exit `2`. SDK results are always strict and accept no suppressions.

## Artifacts and trust

JSON has `schemaVersion: 1`. Breaking shape/meaning changes require a schema version bump;
additive fields are allowed. `analysis.incomplete`, truncated section IDs, selected entry
point IDs, report filters and whole-analysis count scope are present even without coverage.
CLI JSON also records loaded project variants/preprocessor symbols and source hashes,
exclusions, an effective permission-configuration hash and metadata file fingerprints.
Hashes identify inputs, not a portable execution environment or a reproducible build.
`tablesWithoutRoleMetadata` is an inventory gap, never proof of an exemption.

`--no-roles` excludes role coverage/assignments and role files from those fingerprints.
Live user lookup is opt-in, uses downloaded role definitions and records its environment,
identity, assignments and read time separately. Ordinary analysis stays offline.

Order collections ordinally and write `\n` line endings. Report filters change presentation,
not analysis, completeness or exit codes. Filtered JSON explicitly identifies the filter;
counts refer to the whole selected analysis.

MSBuild design-time loading can execute project tasks and source generators. Analyze only
trusted build inputs; fork-PR jobs must not expose secrets, and security metadata and policy
configuration must come from the trusted base branch.

## Validation and boundaries

Regression tests cover extraction, identities, cascades, multi-target determinism, strict
compilation failures, direct/team delegation, unknown lookup contents, incomplete metadata
and role-free/filtered artifacts. Engine tests check current local assignments and state
isolation; selected scenarios compare static requirements with actual handler denials.
Validation must distinguish expected mockup enforcement gaps from analyzer omissions.

Deferred: record-level/depth proof, workflow body analysis, non-C# code, role suggestions,
unused-role analysis, IDE integration and an optional runtime recorder. None is required
for explicit static checks. Remaining consolidation work is listed in
[candidate refactors](permission-analyzer-refactors.md).
