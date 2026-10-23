# Permission analyzer: candidate refactors

Bigger refactors found in the pre-push review of the permission analyzer, most valuable first.
The simple fixes from that review are already in. Paths are relative to
`src/PermissionAnalyzer/PermissionAnalyzer.Core`.

## Done

1. **One shared Roslyn operation helper class.** `Extraction/Operations.cs` holds `Unwrap`, `Root`,
   `IsLocal`, `LocalValues`, `Argument`, `GuidConstant`, the in-source check and the two framework checks
   (now `IsSystemCall` and `IsFrameworkType`, as they mean different things); `SourcePaths.Declaration`
   replaces the two `Declaration` copies. Services now also report locals changed by deconstruction or
   compound assignment, as tables already did.
2. **Rules are applied once.** `Extraction/RuleApplication.cs` applies a rule to a request's members
   through `IRequestMembers`; `TableResolver` reads them from code and `UserOperation` from `--operation`.
   The copies had drifted less than the review first said: `--operation` treated a check's `When` as
   possibly set, which the shared code now does explicitly. A rule without a check on a member now fires
   its steps on its fixed table rather than the operation's table, and an access mask on any member is
   reported, not just on the primary one.
3. **A common type for entry points and unreached methods.** `EntryPoint` and `MethodManifest` implement
   `Model/IManifestSection.cs`, `Manifest.Sections` lists both in report order, and `CoverageReport.For`
   takes a section. In the tool, `Output/PrivilegeNeed.cs` projects a section's table and global
   requirements, filtered and with their coverage, for the text, JSON, SARIF and role change reports;
   the `"global"` placeholder privilege is gone from `RoleChanges` and Markdown (`TableChange.IsGlobal`),
   and only the text report still shows `global` in its privilege column. No separate `RequirementSet`
   was needed.
4. **Message names.** The `Request` suffix is removed only from a request type's name
   (`RequestRules.MessageOfType`). A message from code or a request name is matched exactly: to a rule named
   as the message, or as its request type, configured rules first. A name a person writes in `As` or
   `--operation` (`MessageNamed`) falls back to the message of a request type, so `CreateRequest` is still
   `Create`. Steps fire on the message as called, so a custom API named `ctx_SubmitRequest` with a rule
   runs its code.

## 5. Speed

`Execute` resolves each request once per request shape (9 times), and resolving a local re-walks the
whole method each time, which is quadratic per method. A per-method index of assignments plus
resolving each request once would fix both. This matters on large solutions.

## 6. Split up `ManifestBuilder`

`Analysis/ManifestBuilder.cs` (about 670 lines) holds custom API state that `Build` resets, and three
privilege methods pass the same three output lists through 7-8 parameters. Extract a
`PrivilegeResolver`. Matching the metadata plugin steps (`AddMetadataSteps`) is quadratic and could
use an index.

## 7. Smaller structural items

- Constants for message and SDK member names (a `Messages`/`SdkNames` class), so a typo fails to
  compile instead of never matching.
- Entry point ids built and parsed in one place (an `EntryPointIds` builder/parser).
- `DefaultObject` caches results that were cut off by the depth limit or a cycle, which can
  under-report.
- An immutable `TableResolution` (today `Empty` returns a new mutable instance and `Add` mutates it).
- Split the ~190-line `TextManifestWriter.Write`, move the shared report text helpers into a
  `ReportText` class, and add a `ManifestWriters.Create` factory.
- One collection-reading strategy for requests, records, lookups and entity references in
  `TableResolver` (about 150 lines less); it would also expose the `AddRange`/`Insert` inconsistency.
- Loose ends: `Cascades.Expand` drops triggers, `EntryPointSelector.Operation` matches every
  operation, `ShapesOf` gives entity shapes to any type parameter, and `WithinTargets` drops
  `_columns` and narrowed targets.
