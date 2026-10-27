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
5. **Speed.** A profile showed the suspected causes were cheap: resolving a request once per shape and
   re-walking a method for each local cost well under a second on the sample. Most of the time went to
   Roslyn binding the method bodies, one at a time, then to garbage collection. `SummaryExtractor.Extract`
   now binds declarations in parallel, including within one large generated file, and builds the graph from
   them in source order, so the output is unchanged. The tool uses server GC. FetchXML parsing turns away
   text that can't be XML before it throws (every string argument is read as FetchXML), and member and type
   ids, relative paths and logical names are cached. On the sample, extraction went from about 24s to 9s.
6. **`ManifestBuilder` split up.** `Analysis/PrivilegeResolver.cs` resolves the execute privileges of
   custom API calls and the privileges request rules name. `Build` creates one from the entry points, so
   the builder no longer keeps custom API state between builds, and it collects into one `ResolvedHits`
   instead of passing three lists around. `Aggregate` only merges and sorts. `AddMetadataSteps` indexes
   the plugin classes and the code steps by type, so matching metadata steps is no longer quadratic.
7. **Smaller structural items.** Message names are constants in `Model/Messages.cs`, and the SDK member
   names the extraction reads in `Extraction/SdkNames.cs`. `Model/EntryPointIds.cs` builds every entry
   point id and reads the function name and method id back out. `TableResolution` no longer changes once
   made: `Union` returns a new one, so `Empty` is shared, and the one place that merged into a resolution
   another node still held (a method's return tables) no longer does. `TableResolver` reads collections of
   requests, records, entity references and lookups one way (`Filled` and `Added`), so a local
   `EntityReferenceCollection` filled with `AddRange` or `Insert` is now read instead of unresolved. The text
   writer's `Write` is split into a per-report class, the lines the text and Markdown reports share are in
   `ReportText`, and `ManifestWriters.Create` picks the writer. Fixed on the way: a default object cut off
   by the depth limit or a cycle is no longer memoized, `Cascades.Expand` keeps the steps a cascade fires,
   `--operation` selects only its own operation, a type parameter is read as its constraints allow, and
   `WithinTargets` keeps the columns, bypass and other parts of a value.

## Shared with XrmMockup and the metadata tool

Functionality duplicated between this tool, `xrmmockup-metadata` and XrmMockup365, found in the
critical review, for a later consolidation PR. Most valuable first; paths are relative to `src/`.

1. **Privilege checks per request.** `PermissionAnalyzer.Core/Extraction/RequestRules.cs` restates by hand
   what each XrmMockup handler's `CheckSecurity` checks, and the two disagree. XrmMockup skips checks the
   rules list: Merge runs Associate and Disassociate through `handler.Execute` and writes with `db.Update`
   (`MergeRequestHandler.cs`), QualifyLead writes the lead with `db.Update`, CloseIncident adds the
   resolution with `db.Add`, the Win/Lose/Close messages write the parent through SetState, and ReviseQuote
   reads with `db.GetEntity`. XrmMockup also checks things the rules miss: Read on the regarding record in
   SendEmail, Create on `quotedetail` in ReviseQuote, Append/AppendTo in QualifyLead, and the record team
   messages, which have no rule. It never checks a `prv*` privilege. `RequestRule.Enforced` records where
   XrmMockup doesn't check, but nothing reads it. Proposal: an SDK-free netstandard2.0 library with the
   per-message checks, used by the analyzer and by XrmMockup's handlers, or at least by a parity test.
   Until then, mark the rules above `Enforced = false` and report it.
2. **Step and workflow trigger matching.** XrmMockup splits filtered attributes and
   `triggeronupdateattributelist` on `,` without trimming or lowercasing (`XrmMockup365/Plugin/PluginTrigger.cs`,
   `Workflow/WorkflowManager.cs`, `WorkflowConstructor.cs`), so `"name, telephone1"` never fires on
   `telephone1`: a bug in XrmMockup. The analyzer trims and lowercases (`PluginStepLoader`, `WorkflowLoader`).
   XrmMockup reads a workflow with an empty primary entity as any table and stores `runas` without applying
   it; the analyzer drops the first and models the second. XrmMockup ignores the bypass parameters. Share
   the parsing and the matching of message, table and columns, in `MetadataSkeleton` or the library in 1.
3. **Reading the metadata files.** `Metadata.xml`, `Workflows`, `SecurityRoles`, `PrivilegeCatalog.xml` and
   the DataContract settings are spelled separately in `XrmMockup365/Internal/Utility.cs`,
   `MetadataGenerator.Core/Services/DataContractMetadataSerializer.cs`, `MetadataGenerator365/Program.cs`
   and `PermissionAnalyzer.Tool/Metadata/*Loader.cs`. The analyzer's `WorkflowLoader` lacks XrmMockup's
   duplicate workflow id check, and `MetadataFileLoader` merges `*Metadata.xml` in ordinal order where XrmMockup
   reads `Metadata.xml` first. Proposal: a `MetadataFiles` class in `MetadataSkeleton` with the names and
   readers; the analyzer keeps its streaming partial reads on top.
4. **Security role model.** `RoleLoader` maps roles onto the analyzer's own `Privilege` and `PrivilegeDepth`,
   and `RoleSet.Combine` repeats the max depth of `Security.GetPrincipalPrivilege`. The SDK-free enums are
   deliberate; they could move to the library in 1.
5. **Tool configuration.** The two tools' `ConfigReader`s are near copies. The metadata tool's `--config`
   ignores the file name and a missing file (`MetadataGenerator.Tool/Program.cs`); this tool honours both.
   `-s` is a `.sln` path here and Dataverse solution names there, and the metadata tool's
   `ShortCategoryConsoleFormatter` would shorten this tool's log prefix. Low value: a small shared source
   package at most.

Left alone on purpose: the legacy `MetadataGenerator365`, and the analyzer reading only parts of the
metadata instead of the whole `MetadataSkeleton`, for speed.
