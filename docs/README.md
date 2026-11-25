# XrmMockup documentation

XrmMockup simulates Dynamics 365/Dataverse locally using downloaded metadata and your
plugin/workflow assemblies. Send requests through its organization services to test
supported behavior, process chains, waiting workflows, and user/team security roles.
It is not a complete clone of Dataverse; support for a feature does not guarantee
parity with every live-platform behavior.

This is the repository documentation entry point, combining the former GitHub wiki
with the existing feature, SDK, design, and release documents. The wiki migration's
source inventory, verified corrections, and **items requiring human review before
rewriting** are in the [documentation review report](documentation-review.md).
Legacy examples are marked in place; they are not all runnable modern quickstarts.

## Getting started

- [Setup](setup.md) — migrated legacy generator/MSTest walkthrough, with review warnings.
- [Metadata Generator](../src/MetadataGen/MetadataGenerator.Tool/README.md) — the maintained
  .NET 10 tool guide for installation, authentication, configuration, and generation.
- [Local plugin registration](local-plugin-registration.md) — DAXIF-style registration example.
- [Supported frameworks](../README.md#supported-frameworks).

## Concepts and features

- [How XrmMockup works](how-it-works.md).
- [Organization services: UI vs SDK](organization-service-ui-vs-sdk.md).
- [Frequently asked questions](faq.md).
- [Auto-number columns](auto-number.md).
- [Static permission checks in tests](permission-analysis-sdk.md).
- [Permission Analyzer CLI](../src/PermissionAnalyzer/PermissionAnalyzer.Tool/README.md) —
  standalone tool options, configuration, and reporting.

Static permission analysis is explicit, does not run scenarios or mutate mockup state,
and does not prove record-level access. Use the SDK/CLI guides above for its boundaries.

## Examples from the wiki

These examples use MSTest and generated early-bound types. Supply the appropriate
metadata, context, plugin/activity assemblies, and test services; see each page's
prerequisites and review notice.

- [Plugin](examples/plugin.md).
- [Workflow](examples/workflow.md).
- [Code activity](examples/code-activity.md).
- [Security role](examples/security-role.md).
- [Time control](examples/time-control.md).
- [Snapshot](examples/snapshot.md).
- [Owner teams](examples/owner-teams.md).
- [Debug using live data](examples/debug-using-live-data.md) — historical authentication;
  not current setup instructions.

## Contributing and architecture

- [Implementing a new request](implementing-a-new-request.md).
- [Reporting a bug](reporting-a-bug.md).
- [Repository code map, build commands, and test conventions](../AGENTS.md).
- [Alternate-key uniqueness](design/alternate-key-uniqueness.md).
- [Permission Analyzer design](design/permission-analyzer.md) — draft/design material,
  not the authoritative user-facing CLI reference.
- [Permission Analyzer candidate refactors](design/permission-analyzer-refactors.md) —
  engineering review notes, including proposals rather than shipped promises.

## Maintainer release documentation

- [Release procedures](../RELEASE.md) — authoritative product classification and release process.
- [Release flow](release-flow.md).
- [Release sequence](release-sequence.md).

## Documentation maintenance

- [Migration verification and human-review backlog](documentation-review.md).

Keep user documentation in Markdown in this repository. Keep tool configuration and
authentication reference material in the owning tool README, linked above, rather
than copying options into multiple guides. Existing draft/design documents retain
that status until reviewed.
