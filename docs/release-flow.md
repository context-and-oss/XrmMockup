# Simplified independent release flow

Updated: 13 November 2026. Implemented in the repository; complete the setup in [RELEASE.md](../RELEASE.md) before production use.

See [the current sequence diagram and simplification analysis](release-sequence.md). The diagram reflects the simplified PowerShell implementation.

## Contract

- PR labels choose core, metadata, both, or exclusive `product:none`.
- A category label or matching `Add:`, `Fix:`, `Change:`, `Breaking:`, or `Chore:` title prefix is mandatory.
- One combined changelog PR is updated after master merges and successful releases. Maintainers review/merge it before tagging.
- `core-v<VERSION>` and `metadata-v<VERSION>` independently publish their one selected NuGet package.
- Each release makes one `dotnet nuget push` (including its matching `.snupkg`), without skipping duplicates. Failure stops the run; maintainers can delete the failed tag and restart if NuGet has not accepted that version.
- No merge-queue support, emergency audit mechanism, persistent release ledger/checkpoints, package checksums, receipts, or automatic partial-publication recovery.

## PR checks

`pr-classification.yml` runs on `pull_request` metadata/head changes targeting master and supports explicit dispatch by PR number on the PR head branch. It fetches current PR metadata once and validates both product and category rules in one native **PR release labels** job, with read-only contents/PR permissions and no custom check-run publishing. Missing or invalid classification fails that job. A dispatch verifies that the selected commit matches the PR head, so its native check is attached to the correct commit. PR runs check out the normal PR merge ref; review workflow/validator changes as part of normal code review. Fork runs may require maintainer approval.

The bot-owned changelog PR must have `product:none` and `change:chore`. Normal maintenance PRs use the same classification rules. The writer explicitly dispatches classification by PR number on the changelog head branch. CI uses its native pull-request event rather than workflow dispatch; maintainers must approve its run when requested and verify both required checks are bound to the bot PR during initial setup.

## Notes generation

`update-changelogs.yml` runs on master pushes or manual dispatch, using GITHUB_TOKEN with contents/PR/actions write. It writes only the reserved branch `automation/update-changelogs`, never master.

1. Fetch master/tags and list published GitHub releases.
2. Choose the latest successful same-product release as that product's notes boundary; before any new-format release, use the fixed migration cutoff `6e689d959b9013e2318320e9c317449d0de02c2d` (the PR #361 rollout merge). This exclusive boundary excludes the unclassified rollout and older PRs without rewriting legacy history.
3. Find merged PRs in the relevant commit ranges through gh. Ignore open PRs, changes already released for that product, and `product:none` notes. Check that a PR merge commit is pending for at least one selected product before validating product labels, then determine pending products before validating category/title, so later edits to released-only PRs do not block unrelated pending changes; shared PRs still validate for any unreleased product. Unassociated direct commits have no automatic PR note and are not special-cased.
4. Regenerate each Unreleased section, preserving recorded versioned history. Add new published sections from GitHub release bodies once. PR metadata may update pending notes; published sections do not change.
5. Update one open PR using the fixed head/base. Further merges refresh that PR; identical content makes no new commit. Leases protect the reserved branch, and unrelated branch-file changes fail rather than being overwritten.
6. Apply the maintenance labels, dispatch classification, and leave approval/merge to maintainers. Merging the bot PR does not add a note and therefore does not generate another PR for itself.

There is no mutable release-state file. Existing historical changelog sections remain the archive. Pending notes are derived, not a second manually maintained version source.

## Release workflow

Workflow-level concurrency serializes each product independently. Tag releases and manual previews share that product's group, with `cancel-in-progress: false` and `queue: max`: running requests are not canceled and up to 100 pending requests are retained. Additional requests are canceled when the queue is full. Runs are ordered by when they start waiting, not guaranteed dispatch order.

### Read-only build

The Windows build job checks out trusted scripts and the requested source separately. Trusted preparation code validates the entire product tag/version, master ancestry, version progression, and that the selected changelog matches the merged-PR notes at that source. Preparation scopes release boundaries and range ancestry to the selected product; a later release of the other product does not invalidate an earlier reviewed source. The changelog writer still collects both products on current master. An unmerged source cannot replace privileged preparation code. Manual dispatch takes a candidate tag and source ref and is always preview-only. Only a tag push enables publication.

Prerelease identifiers are compared case-insensitively to match NuGet version identity. Release sections are inserted literally, preserving dollar-sign sequences from PR titles in both saved notes and the packaged changelog.

Preparation writes temporary `.release/release.json`, `.release/notes.md`, and product-scoped MSBuild inputs. This metadata is only a build artifact, not a recovery ledger. The tag determines the Version before restore/build, and only the selected product receives the release override.

Build/test the full solution to retain cross-product coverage and the legacy generator bundled with core. Pack the selected project and its symbols. Validate that the `.nupkg`/`.snupkg` pair belongs to the selected ID/version; no archive checksumming or comparisons with existing NuGet content are performed. Retain package/notes as workflow artifacts for manual recovery.

### One push, then GitHub

The publish job is skipped for previews. It checks out only trusted default-branch code, downloads the selected build artifact, and verifies package metadata and the tag's exact source before obtaining NuGet credentials.

1. Log in through the existing NuGet Trusted Publishing action.
2. Push the one nupkg with its matching snupkg in the same directory. The same command publishes the regular package followed by its symbols. A nonzero exit stops the job. There is no separate automated symbols command and no duplicate skipping.
3. Only on success, use gh to create the GitHub release for the existing tag, attach that package and its symbols and use the prepared versioned notes. Metadata releases do not take the repository's global Latest designation; prereleases are marked.
4. Dispatch the writer to update the same combined changelog PR with successful release history.

GitHub and NuGet are not atomic. A failure before NuGet acceptance can be restarted with a deleted/recreated tag; a symbol failure may occur after the regular package is accepted and needs manual completion using the saved symbol package; after acceptance, a maintainer completes GitHub/history using saved artifacts or releases a new version. The automation does not rebuild, compare, or resume old drafts.

## Functional tests

`release-automation.yml` runs the automation suite only when `scripts/release/**`, workflow definitions, or the package project/shared build configuration changes. It also supports manual dispatch. It is path-filtered and must not be required globally. The ordinary `ci.yml` .NET job and single **PR release labels** check remain unconditional for PRs; bot changelog-only updates do not run the automation unit suite.

Run `pwsh -NoProfile -File scripts/release/Test-Release.ps1` without credentials or remote access. The dependency-free suite uses real temporary local Git repositories, a mocked gh command, and synthetic NuGet ZIP archives. It checks the release workflow's queue configuration and covers single-check classification failures and dispatch commit binding, literal notes/history, several merges producing one PR without pushing master, bot-PR exclusion, first-deployment migration with no product releases and unclassified legacy PRs, strict post-migration classification, independent product previews and boundaries, released-note filtering, tag preparation, stale/unmerged/old-version rejection, and package/symbol validation. Invalid archives are tested in separate PowerShell processes to verify nonzero exit codes, not exception wording.

These tests do not prove live GitHub queue scheduling, permissions, required-check binding, runner builds or NuGet/OIDC/symbol publication. Verify those in GitHub during rollout. No exact CLI argument, TLS/library, checksum, recovery-framework or merge-queue tests are retained.

## References

- GitHub CLI workflows: `https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/use-github-cli`
- CLI API/pagination: `https://cli.github.com/manual/gh_api`
- Release creation: `https://cli.github.com/manual/gh_release_create`
- NuGet push options: `https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-push`
- NuGet Trusted Publishing: `https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing`
