# Permission analyzer: remaining consolidation

Correctness and behavior are defined in [the design](permission-analyzer.md).
Completed extraction, rule-application, manifest and output refactors are reflected in
code; this file lists only remaining work.

1. **Request-rule parity.** Keep required Dataverse privileges separate from actual
   XrmMockup enforcement. Partial enforcement is now annotated and unresolved runtime
   checks remain visible. Extend handler cross-checks before consolidating rules into an
   SDK-free shared definition; do not make static checks part of runtime CRUD.
2. **Trigger parsing/matching.** Share normalization of message/table/filtered attributes
   between the analyzer, plugin triggers and workflows. Resolve engine differences in
   workflow identity, table-less workflows and bypass handling with regression tests.
3. **Metadata readers.** Consolidate file names and serialization settings, preserving
   the workspace's streaming partial reads. Define duplicate-ID and multi-file merge
   policies explicitly before sharing readers with the engine and generators.
4. **Role conversion.** Share privilege/depth conversion where useful. Preserve SDK-free
   contracts, per-identity coverage and direct/team provenance; delegation is not a simple
   union of all assignments.
5. **CLI configuration.** Reuse common infrastructure only where the tools' path and option
   semantics agree. Do not change authentication behavior as a side effect.

No independently released library packages are needed. Leave the legacy generator alone
unless a concrete compatibility fix requires a change.
