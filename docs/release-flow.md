# Independent product release flow

Updated: 5 November 2026.

**Status:** implemented in the repository; GitHub repository configuration must be completed before production use. The workflows and shared scripts below implement the approved design. The maintainer-facing procedure and approved labels/tags are documented in [RELEASE.md](../RELEASE.md).

## Approved decisions

1. PR **labels** select the affected product(s); Git **tags** select the product and version to release. These are separate mechanisms.
2. Every PR targeting `master` must have `product:core`, `product:metadata`, both, or the exclusive maintenance label `product:none`.
3. Every PR must specify a change category through an approved label or a recognized title prefix. This also applies to `product:none` PRs.
4. Required validation pipelines enforce product and category classification before merge.
5. A merge-to-`master` pipeline generates and **commits** the tracked changelogs directly to `master`, keeping their `Unreleased` sections current. It does not wait for a release-preparation PR.
6. A product-prefixed tag releases only that product. The tag is the authoritative release version; maintainers do not manually edit version headings or copy descriptions into GitHub.
7. Release notes are generated from classified merged PRs, not file paths or manually maintained fragments.

## Repository findings motivating the change

Before this implementation:

- `.github/workflows/release.yml` publishes both products on every GitHub release event, using versions from the first numbered heading in two independent changelogs rather than validating the release tag.
- `scripts/Set-VersionFromChangelog.ps1`, CI, and `scripts/Pack-Local.ps1` depend on those headings.
- Both package projects use the entire changelog as `PackageReleaseNotes`.
- Manual dry runs still request NuGet credentials.
- Local history contains core-only, metadata-only, and combined-version tags. Initial release boundaries require an explicit migration mapping.
- The core package bundles the **legacy** `MetadataGenerator365.exe`; the independently released metadata product is the modern .NET tool. Core packing must continue to build/include the legacy generator.
- Metadata-tool tests reference the core library and both products use shared metadata source. Independent publication does not imply independent build/test coverage.

## 1. Classification contract

### Product labels

| Label | Meaning | Generated file |
| --- | --- | --- |
| `product:core` | Change affects `XrmMockup365` | `RELEASE_NOTES.md` |
| `product:metadata` | Change affects `XrmMockup.MetadataGenerator` | `src/MetadataGen/MetadataGenerator.Tool/CHANGELOG.md` |
| Both product labels | Shared change affects both products | Both files, independently |
| `product:none` | Repository maintenance with no product release-note entry | Neither file |

At least one approved product label is required. `product:none` cannot be combined with either product label. An unrelated/unknown label such as `product:other` does not satisfy the requirement. Ordinary labels remain allowed but do not affect release inclusion.

Product mapping is explicit, not inferred from changed paths. Reviewers must verify that shared code changes carry both labels when appropriate. `product:none` is not a way to hide product changes. It still requires a category and normal review.

The previously suggested `release-note:skip` label is **not** an approved substitute or exclusion mechanism. Use `product:none` for genuine repository maintenance.

### Categories and PR titles

Use the following approved categories, matching the repository's existing note style:

| Category label | Recognized title prefix | Rendered note prefix |
| --- | --- | --- |
| `change:feature` | `Add:` | `Add:` |
| `change:fix` | `Fix:` | `Fix:` |
| `change:change` | `Change:` | `Change:` |
| `change:breaking` | `Breaking:` | `Breaking:` |
| `change:chore` | `Chore:` | `Chore:` |

Title prefixes are case-insensitive, must occur at the beginning of the title, and must be followed by non-empty descriptive text. Arbitrary appearances of “fix” or “feature” elsewhere do not qualify. Conventional Commit forms such as `feat(scope):` are not implicitly accepted by this contract; support would require a documented extension.

Validation rules:

- No category label + recognized title prefix: pass and infer the category from the title.
- Exactly one category label + unprefixed, non-empty title: pass and use the label.
- Exactly one category label + matching title prefix: pass.
- No category label + no recognized title prefix: fail.
- Multiple approved category labels: fail, even if a title prefix is present.
- Category label and title prefix disagree: fail rather than silently choose one.
- The same rules apply to `product:none`; maintenance is not a category exemption.

Render a note as `* <Category>: <description> (#<PR-number>)`, removing a recognized title prefix before adding the canonical prefix so it is not duplicated. The initial implementation uses the PR title as the description; a PR-body release-note override is outside this approved scope. Do not use titles/labels as executable shell input.

## 2. PR validation pipelines

The implementation adds separate lightweight workflows/checks, independent of expensive build/test CI:

| Workflow | Required check | Responsibility |
| --- | --- | --- |
| `.github/workflows/pr-product-labels.yml` | `PR product labels` | Enforce the product rules |
| `.github/workflows/pr-change-category.yml` | `PR change category` | Enforce category/title rules |

Run both on PRs targeting `master` for `opened`, `reopened`, `synchronize`, `labeled`, `unlabeled`, `edited`, and `ready_for_review`. Re-fetch current PR labels/title through the API; do not rely only on an older event payload. Changes to either title or labels must rerun both checks. GitHub supports these event types. [S10]

These jobs require read-only metadata access, no secrets, and no checkout/build/execution of PR code. Use a trusted base-branch metadata-only workflow, such as `pull_request_target`, so a contributor cannot edit the validator in their PR to bypass it. Never combine that trigger with execution of the PR head's code. Ensure checks report against the actual PR head SHA: a base-branch workflow result alone is not a required check on the proposed merge. Explicit check publication needs narrowly scoped `checks: write` and must otherwise remain metadata-only; use a GitHub App or equivalent trusted check publisher where repository policy requires it.

Make **both checks required** in the `master` ruleset/branch protection. Restrict direct pushes and bypass to the narrowly scoped changelog automation identity. A failing Action alone is insufficient without these repository settings. Do not skip validation jobs when classification is absent: skipped checks can appear successful. [S11]

If a merge queue is used, add `merge_group: checks_requested` handling and validate all constituent PRs under the same rules. Do not silently pass merge-group events or leave required checks absent. [S10]

Generated changelog commits contain no feature PR and are handled through the approved automation identity, not a public “skip validation” label. There is no blanket exemption for human PRs, Dependabot PRs, or PRs titled “release.”

## 3. Generate tracked changelogs on merge to master

### Workflow and permissions

`.github/workflows/update-changelogs.yml` runs with `push` to `master` and a manual reconciliation trigger for recovery. A push-triggered reconciler queries newly reachable merged PRs instead of assuming every push is a single merge. Closing a PR without merging it must not create an entry. [S10]

Use a trusted automation identity to commit generated updates **directly to `master`**. This is the approved choice for immediate tracked-file updates, not a bot-owned preparation PR. Restrict its writable files to:

- `RELEASE_NOTES.md`;
- `src/MetadataGen/MetadataGenerator.Tool/CHANGELOG.md`;
- the machine-readable reconciliation ledger, stored in `.github/release-state.json`.

This requires explicit repository/org setup for narrowly scoped branch-rule bypass and token permissions; ordinary contributors must not receive that bypass. The writer must reject any unexpected changed path before committing. Generated-only commits do not produce release-note entries. If its credential starts another push workflow, reconciliation produces no further commit when content is unchanged. Do not depend on `[skip ci]` for correctness or skip normal checks on feature merges. Token-trigger behavior must be accounted for explicitly. [S5, S12]

### Deterministic reconciliation

1. Fetch current `master`, relevant tags, successful release records, and the existing ledger.
2. Discover all unprocessed merged PRs whose merge commits are reachable from the source history. Handle the repository's actual merge strategies; do not use timestamps alone as inclusion boundaries.
3. Resolve and validate product/category metadata. Freeze the first processed title, category, product selection, PR number, and merge/source commit in the ledger. Later label/title edits must not silently alter already recorded notes. Corrections need an explicit reviewed operation.
4. Render `### Unreleased` at the top of each product's changelog, preserving existing preambles and immutable released history. Deduplicate by **product + PR number**; shared changes appear once in each file.
5. Incorporate any newly successful release snapshots as described below, removing only the entries consumed by those releases from the appropriate pending section.
6. Commit only changed generated files. Refresh/reconcile on a non-fast-forward push instead of overwriting newer work. Do not force-push `master`.

Use stable category ordering from the category table and oldest-first source order within a category, with PR number as a deterministic tie-breaker. Persist `product:none` decisions in the ledger for audit/reconciliation, but do not render them in either product file.

Serialize all writes to these shared generated files, including release finalization, under one writer concurrency group with `cancel-in-progress: false`. Product publication can remain separately serialized. Event coalescing must be safe because a writer reconstructs all missing work from repository/release state rather than consuming only the latest event.

If reconciliation encounters a reachable feature PR with invalid classification, fail visibly and block releasing that source until corrected/reconciled. Do not silently omit it. Disallow human direct pushes as normal policy; approved emergency pushes require an explicit recorded classification/correction before release.

### Freshness gate

“Always up to date” means the generated files are committed automatically after each merge and successful reconciliation. There is still an asynchronous interval after the feature merge. Maintainers must wait for that update before tagging.

The release pipeline independently validates that the tagged commit's ledger accounts for every merged PR reachable since the migration baseline and that its rendered pending sections match those records. If a tag races the generator, fail before publication; do not silently pull newer `master` into the tagged build.

## 4. Tag-triggered product release

### Approved release tags

| Tag | Package | Pack target | Release title |
| --- | --- | --- | --- |
| `core-v<VERSION>` | `XrmMockup365` | `src/XrmMockup365/XrmMockup365.csproj` | `XrmMockup365 <VERSION>` |
| `metadata-v<VERSION>` | `XrmMockup.MetadataGenerator` | `src/MetadataGen/MetadataGenerator.Tool/MetadataGenerator.Tool.csproj` | `MetadataGenerator <VERSION>` |

Support NuGet-compatible SemVer versions, including prereleases such as `core-v1.21.0-rc.1`. Reject malformed versions, unknown products, and legacy combined tags. Release versions are independently selected by the maintainer; category labels do not automatically decide the version bump.

Replace the existing GitHub-release-event publication trigger with tag pushes matching `core-v*` and `metadata-v*`. Validate the entire tag after glob matching. Retain manual preview with explicit product, version, and source ref; default it to dry run. Do not keep a second publication route on `release: released`.

Resolve the product explicitly from the tag, not `paths:`; path filters are not evaluated for tag pushes. Fetch the exact tagged commit and history, verify it is on the approved `master` ancestry, apply tag-creation restrictions, and run the freshness gate. Only reconciled commits may be released. [S1]

### Release snapshot and versioned file history

Select the previous **successful release of the same product**, on the same reachable release line. The first new-format release uses the explicit migration baseline. Do not select the latest repository-wide tag/release or parse combined legacy tags as ordinary SemVer.

Freeze the included PR IDs and their recorded metadata from the tagged ledger, excluding entries already successfully released for this product. A PR recorded for both products may be pending for one and already released for the other. Later merges/label edits must not alter this snapshot.

Generate a selected-product file with a version/date section in the release workspace, and extract that exact section for GitHub/NuGet release notes. Store the snapshot, generated file, and version/date as release artifacts so retries and finalization reuse identical content. The date is set once in UTC when the release attempt is first prepared. Ordinary category-only changes with `product:none` never enter the snapshot.

After successful publication, commit the versioned section and release-state update back to current `master` through the shared reconciler. Preserve every newer unreleased PR and the other product's pending entries. Use the stored released snapshot, not a fresh query of mutable PR metadata. If two products release together, reconcile both successful snapshots without overwriting either update.

**Deliberate ordering trade-off:** a release tag points to a reconciled commit with an `Unreleased` section; its release/package artifacts contain the generated versioned section. The subsequent documentation/history commit is newer than that tag. Never move the published tag to include it. Retain the snapshot so the workspace transformation is reproducible and identifiable. This keeps tagging as the release command without requiring a preparation PR or manual version-heading edits.

A failed documentation push after successful publication is recoverable independently: published artifacts are not rebuilt/republished. The shared writer also discovers successful release snapshots on later merge/manual runs, so a missed finalization event does not lose release history. Expose pending/failed finalization clearly to maintainers.

### Build, package, and publish

- Derive the authoritative selected product version from the tag. Map product-scoped `CoreReleaseVersion` / `MetadataReleaseVersion` inputs to the appropriate `Version` before building.
- Do not apply a global solution-wide `Version`, and do not build once then change only `PackageVersion` during `pack --no-build`.
- Build/test the full solution on Windows initially, preserving the core package's legacy generator assets and cross-product tests.
- Pack only the selected project into a clean product/version-specific output directory. Validate package ID/version, assembly/tool version, and required assets.
- Supply only the selected release section as `PackageReleaseNotes`, preferably via generated props rather than shell-interpolated Markdown. Ensure the core package's bundled `RELEASE_NOTES.md` is the generated release snapshot rather than a stale archive.
- Create/reuse a draft GitHub release for the existing tag, using the derived title and snapshot; upload only this product's validated artifacts and release snapshot.
- Obtain narrowly scoped NuGet Trusted Publishing credentials immediately before pushing. Keep ordinary verification credential-free. [S3]
- Publish the draft only after the selected NuGet package/symbol publication succeeds. Mark prereleases appropriately. Use `--verify-tag`; keep metadata releases from taking the core's global “Latest” designation with explicit latest handling. [S4]
- Trigger shared tracked-file finalization after publication. Persist successful-release metadata before relying on a follow-up workflow.

NuGet and GitHub publication are not atomic. Serialize publication per product with `cancel-in-progress: false`. Reuse drafts/snapshots on retry and do not blindly treat an existing NuGet version as identical to a rebuilt artifact. Verify prior successful upload state before skipping an existing version; otherwise fail for investigation instead of masking a collision with unconditional `--skip-duplicate`.

Build/test jobs use read-only contents permissions. A separate metadata-only preparation job has contents write to recover draft releases (draft visibility requires push access); it does not build or execute project code and makes no publication API writes. Restrict `contents: write`, `id-token: write`, and bot write/bypass privileges to the jobs/identities that need them. Dry runs must not obtain NuGet credentials, create/upload/publish GitHub releases, or commit/push tracked-file updates. Temporary release-workspace transformations are still generated for preview packaging.

Bot-created tags/releases using `GITHUB_TOKEN` do not automatically launch another push/release workflow. Use explicit orchestration/dispatch or an appropriately scoped GitHub App where needed; do not depend on recursive events. [S5]

### Implemented scripts and recovery details

The dependency-free Node 24 modules live under `scripts/release/`. `model.mjs` owns the shared label/category/tag contract and deterministic rendering; `github.mjs` handles authenticated, paginated API access and source ancestry. The CLI scripts perform validation, reconciliation, preparation, artifact hashing, publication, and label provisioning. Tests use temporary Git repositories and a local mock API; they do not publish or modify the real repository's release state.

`setup-release-labels.yml` provisions the approved labels on manual dispatch. Configure `PR product labels` and `PR change category` as required head-SHA checks and configure `RELEASE_APP_ID` / `RELEASE_APP_PRIVATE_KEY` (or a scoped `RELEASE_BOT_TOKEN` PAT) for the direct writer. `NUGET_USER` and the existing Trusted Publishing policy must allow `release.yml` and the selected packages. A live publish requests finalization through an explicit dispatch of `update-changelogs.yml`; published snapshots can also be recovered on its next merge/manual run.

Both a `.nupkg` and `.snupkg` are required. Package archives are validated in PowerShell before hashing. NuGet repository signing changes the transport bytes, so retry checks compare canonical unpacked content excluding `.signature.p7s`, not the uploaded ZIP's raw hash. Frozen GitHub assets still use exact SHA-512 transport hashes. Publication stores a `nuget-publication.json` receipt after both uploads succeed; only releases with a matching receipt are eligible for tracked history.

If symbol upload succeeds but writing that receipt fails, a retry deliberately fails on an unverified symbol duplicate rather than masking it. An operator must verify both uploads against the frozen artifacts/upload logs before recording the receipt and finalizing the existing release. A partially uploaded draft without a complete snapshot can similarly require manual cleanup before a clean retry; existing assets are never clobbered.

## 5. Implementation and rollout checklist

1. Provision the approved labels and document them in `RELEASE.md`.
2. Add and test both PR validation pipelines; require their check names on `master` and configure merge-queue handling if applicable.
3. The seeded ledger uses the collection cutoff `46f2af04b6d5dc4797f6069c31610c7101de3b47`, core **1.20.2**, and metadata **1.2.0**. Both versions were confirmed present in the NuGet index on 5 November 2026. Existing release notes are retained as immutable archives. This is a prospective PR-classification cutover: audit any metadata changes between its legacy release and the collection cutoff, and seed explicitly reviewed pending entries if needed. Do not rewrite old tags or republish old versions.
4. Add the merge reconciler, ledger, shared writer lock, freshness validation, and narrowly scoped automation permission. Test direct generated-file updates without granting general branch bypass.
5. Replace the old release-event workflow with validated tag releases, scoped build versions, selected-product packing, release snapshots, publication, and tracked-file finalization.
6. Update CI/local pack scripts to stop treating the first numbered changelog heading as a release version. Define a separate development build version policy. Ordinary CI/local builds use `0.0.0-dev`; local pack accepts explicit product-scoped version overrides and is side-effect-free with respect to release state.
7. Exercise previews for both products, shared changes, maintenance-only PRs, prereleases, race conditions, and interrupted finalization before enabling publication.

### Acceptance cases

**PR classification:**

- Core, metadata, both, and exclusive none each pass with a valid category.
- Missing product, unknown-product-only, or none-plus-product fail.
- Category label or recognized title prefix alone passes; matching label+prefix passes.
- Missing, multiple, or conflicting categories fail, including on maintenance PRs.
- Editing titles/labels updates check results; invalid classification blocks merge.

**Generated files:**

- Each merged product PR appears once in the correct `Unreleased` section(s).
- `product:none` produces no visible entry; shared PRs appear in both files.
- Reconciliation commits generated files to `master`, preserves released history, and is a no-op on retry.
- Concurrent merge/finalization runs lose neither pending notes nor successful release records.
- A tag created before its notes are reconciled fails the freshness gate.

**Release:**

- Each product tag publishes only its package, with consistent tag/package/assembly/title/artifact versions.
- Two tags at one commit produce independent releases and histories.
- Notes use the selected product's recorded PRs and previous successful release.
- Release finalization writes the correct versioned section while retaining later pending entries and the other product's notes.
- Publication success followed by documentation failure is recoverable without moving tags or republishing packages.
- Invalid/unapproved tags and dry runs have no publication side effects; dry runs need no NuGet identity.
- Legacy core generator assets and full solution test coverage remain intact.

## Online sources

Primary sources consulted on 5 November 2026. GitHub's public documentation pages were not all accessible from this environment, so their official documentation repository/API schema was used where necessary.

- **S1 — GitHub Actions path-filter behavior:** `https://github.com/github/docs/blob/main/data/reusables/actions/workflows/triggering-a-workflow-paths1.md`
- **S2 — GitHub generated release notes and API input schema:** `https://github.com/github/docs/blob/main/content/repositories/releasing-projects-on-github/automatically-generated-release-notes.md`; `https://github.com/github/rest-api-description/blob/main/descriptions/api.github.com/api.github.com.json` (operation `/repos/{owner}/{repo}/releases/generate-notes`).
- **S3 — NuGet Trusted Publishing:** `https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing`; `https://github.com/NuGet/docs.microsoft.com-nuget/blob/main/docs/nuget-org/trusted-publishing.md`
- **S4 — GitHub CLI release creation:** `https://cli.github.com/manual/gh_release_create`; implementation/documentation text: `https://github.com/cli/cli/blob/trunk/pkg/cmd/release/create/create.go`
- **S5 — GitHub workflow token-trigger behavior:** `https://github.com/github/docs/blob/main/data/reusables/actions/actions-do-not-trigger-workflows.md`
- **S6 — Release Please manifest releases:** `https://github.com/googleapis/release-please/blob/main/docs/manifest-releaser.md`
- **S7 — Release Please strategies and arbitrary/XML file updates:** `https://github.com/googleapis/release-please/blob/main/docs/customizing.md`
- **S8 — Release Please Action outputs and token caveats:** `https://github.com/googleapis/release-please-action/blob/main/README.md`
- **S9 — Keep a Changelog:** `https://keepachangelog.com/en/1.1.0/`; source: `https://github.com/olivierlacan/keep-a-changelog/blob/main/source/en/1.1.0/index.html.haml`

- **S10 — GitHub workflow events, label changes, merged PRs, and merge groups:** `https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows`
- **S11 — Required status checks:** `https://docs.github.com/en/pull-requests/reference/status-checks`
- **S12 — Workflow triggering and repository Actions permissions:** `https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/trigger-a-workflow`; `https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/enabling-features-for-your-repository/managing-github-actions-settings-for-a-repository`
- **S13 — Release Please release-PR lifecycle:** `https://github.com/googleapis/release-please`
