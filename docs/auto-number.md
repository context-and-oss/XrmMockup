# Auto-number columns

XrmMockup generates values during Create for string columns with a nonempty
`StringAttributeMetadata.AutoNumberFormat`. Metadata comes from the ordinary
metadata fixtures; no additional setting or generated entity class is needed.

Supported placeholders can be combined with literal text:

- `{SEQNUM:n}` pads the sequence to a minimum width, without truncation. The
  complete decimal parameter is parsed, including multi-digit widths. Widths
  must be between 1 and 4000 (the maximum Dataverse text-column length).
- `{RANDSTRING:n}` produces 1–6 uppercase ASCII letters/digits per placeholder.
- `{DATETIMEUTC:format}` uses an invariant-culture .NET date/time format and
  `DateTime.UtcNow + TimeOffset`, independently of `overriddencreatedon`.
  All date placeholders for one value use the same instant.

For example, `INV-{SEQNUM:12}-{RANDSTRING:6}-{DATETIMEUTC:yyyyMMdd}`.
Unknown tokens, unmatched/nested braces, empty parameters, invalid lengths, and
invalid date formats throw `FaultException`. A format is fully rendered before
its sequence is consumed. Repeated sequence placeholders in one column use one
number; each random placeholder generates its own string.

## Caller values and requests

An explicit nonempty Create value is preserved. An omitted or null value is
generated. An explicitly supplied empty string suppresses generation and is then
normalized to null by the engine's existing Create string handling. Update never
generates a new value. Explicit values do not consume the column's sequence. These are the simulator's tested
SDK-service behaviors; tests do not connect to a live Dataverse environment.

The following SDK requests are supported:

- `SetAutoNumberSeedRequest`: sets the seed and next sequence value for a column.
  Zero is allowed; negative values fault. Reseeding works after prior creates.
- `GetAutoNumberSeedRequest`: returns the last configured seed (default 1000).
- `GetNextAutoNumberValueRequest`: observes the next value without consuming it.

All three reject missing tables, missing columns, and non-auto-number columns.
State is independent per simulation instance, table, and column. Sequences start
at 1000 unless explicitly seeded. The last Int64 value can be emitted once;
subsequent generation faults rather than wrapping, until reseeded.

## Environment lifecycle

Named and JSON snapshots copy both seed and next-value state, including sequence
exhaustion. Restoring a snapshot restores generator state together with rows and
the simulated time offset. Snapshot state is copied on both capture and restore,
so a named snapshot can be restored repeatedly. JSON snapshots from before this
feature, which have no generator state, restore default counters.

`ResetEnvironment` clears seeds and counters. Existing named snapshots remain
available and can restore their saved state. `ExecuteTransaction` failures restore
sequence state via its existing JSON rollback. Ordinary failed creates may leave
sequence gaps (for example, a later column or validation may fail after a number
was allocated); this feature does not promise gap-free numbering.

Random output and wall-clock time are not deterministic snapshot state. A restore
replays sequential values, not the exact random strings or UTC timestamp. This
matches the engine's existing offset-based, rather than frozen, clock model.

## References

- [Microsoft: Create autonumber columns](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/create-auto-number-attributes)
  documents placeholders, random-length limits, default seed, and reseeding.
- [Microsoft: Autonumber columns](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/autonumber-fields)
  describes runtime generation and the possibility of sequence gaps.
