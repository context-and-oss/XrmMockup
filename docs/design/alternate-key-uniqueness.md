# Alternate-key uniqueness

Alternate-key resolution and uniqueness enforcement are separate concerns. Resolution
uses supplied `KeyAttributes` to identify a row. Enforcement uses the entity metadata's
active `Keys` and the column values of the row that will actually be persisted.

## Write boundary

`XrmDb.Add` and `XrmDb.Update` check the converted prospective `DbRow` immediately
before publishing it. Update handlers already merge their sparse Target with the
persisted row; omitted composite-key columns therefore come from that target row,
not a comparison row. The boundary also includes defaults, generated values,
empty-string normalization, and pre-operation plugin changes. An update excludes
only its resolved row ID from the candidate search.

Every key and candidate is compared independently. Keys whose index status is not
`Active` (including an unspecified status), empty definitions, and keys with any
null/missing component are not enforced. This respects supplied metadata state; it
does not simulate asynchronous index-build jobs. Lookup values compare logical name and ID;
other types reuse the database's alternate-key comparison semantics. Lookup display
names are irrelevant. A database-local write lock keeps validation and publication
atomic for concurrent inserts/updates.

Duplicate values raise `FaultException<OrganizationServiceFault>` with error code
`0x80060892` (`DuplicateRecordEntityKey`). Messages use user-localized field/key labels
when available and logical-name fallbacks otherwise. `ExecuteMultiple` retains this
fault detail rather than replacing it with an untyped error.

## Routed requests and rollback

Create populates omitted columns from `KeyAttributes`; explicit Attributes take
precedence. Update identification does not overwrite prospective column values.
Upsert and the bulk handlers route through the ordinary Create/Update operations,
so they cannot bypass the check. Empty IDs do not conflate distinct alternate-key
addresses in UpdateMultiple or UpsertMultiple.

A rejected single-row write leaves its stored row and row version unchanged.
CreateMultiple, UpdateMultiple, and UpsertMultiple use the existing snapshot machinery
around their pipeline to roll back earlier writes and plugin database side effects
if a later item fails. ExecuteTransaction retains its existing snapshot rollback.
ExecuteMultiple remains non-atomic and follows its continue-on-error settings.
These snapshots are the existing mock transaction mechanism, not a new general
transaction/isolation engine or elastic-table partial-success implementation.

## Regression tests

`TestAlternateKeyUniqueness` uses instance-local synthetic metadata via the existing
`RequestHandlerTestBase`. It covers independent keys/candidates, partial composites,
nulls and index states, typed comparisons/faults, label fallbacks, key-addressed
updates, plugin changes, upsert/bulk routing, transaction rollback, and concurrent
creates. Resolution tests that need duplicate names declare the composite keys they
exercise instead of violating the checked-in name-only key. Test plugins use GUID
suffixes rather than a small random number range for their active name keys.

This replaces the historical implementation from imported PR #183 by MattTrinder1, using its
requirements/test scenarios without retaining its generator filename/system-entity
options. Its commits and original author attribution remain in branch history.
