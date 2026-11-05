# Release process

**Before first use:** complete the repository setup below. The workflows are implemented, but required checks, permission to create changelog PRs, and NuGet Trusted Publishing must be configured before production releases.

## Tools

Release automation uses **Node 24** for classification, rendering, and validation, and **GitHub CLI (`gh`)** for GitHub operations. For local use, install both tools and authenticate with `gh auth login`, or provide `GH_TOKEN`; set `GITHUB_REPOSITORY` to the intended `owner/repository`. GitHub-hosted runners already include `gh`.

## Repository setup

1. Run **Set up release labels** (`setup-release-labels.yml`) from `master` to create/update the approved labels. For the initial implementation PR, create/apply the needed labels manually before merging.
2. Require **`PR product labels`** and **`PR change category`** in the `master` branch ruleset. They are published against the PR head SHA. Keep normal branch protection and required reviews in place; merge-queue validation is included. No changelog automation bypass is needed.
3. In **Settings → Actions → General → Workflow permissions**, enable **Allow GitHub Actions to create and approve pull requests** (and the organization-level permission if applicable). The pipeline passes the built-in `GITHUB_TOKEN` to `gh` through `GH_TOKEN`; it does not approve or automatically merge PRs. No GitHub App or bot PAT is required. The token writes only the reserved branch `automation/update-changelogs`, never `master`.
4. Keep **`NUGET_USER`** configured and verify the NuGet Trusted Publishing policy for `release.yml` and both package IDs. Restrict who may create the approved release tags.
5. Run **Update product changelogs** manually, review and merge its generated PR after checks pass, and try a manual **Release** preview before creating production tags.

The initial collection cutoff is `46f2af04b6d5dc4797f6069c31610c7101de3b47`, with baseline versions core `1.20.2` and metadata `1.2.0`. Historical notes remain intact; audit any older metadata changes not covered by its latest legacy release and record reviewed pending entries before the first new metadata release.

## Products and release tags

The products have independent versions and independent releases:

| Git release tag | NuGet package | Changelog |
| --- | --- | --- |
| `core-v<VERSION>` | `XrmMockup365` | `RELEASE_NOTES.md` |
| `metadata-v<VERSION>` | `XrmMockup.MetadataGenerator` | `src/MetadataGen/MetadataGenerator.Tool/CHANGELOG.md` |

`VERSION` is a NuGet-compatible semantic version, such as `1.20.3` or `1.21.0-rc.1`. Release tags must use one of the two prefixes above. Combined tags, unprefixed versions, and historical tag formats are not approved for new releases. Do not rename or move existing release tags.

PR product labels determine **which notes to include**. Git release tags determine **which product/version to publish**. Labelling or merging a feature PR does not publish a package.

## Required PR classification

Every PR targeting `master` must specify a product and category before merging, including bot and repository-maintenance PRs.

### Product labels

Choose at least one approved label:

| PR label | Use for |
| --- | --- |
| `product:core` | Changes affecting the core package |
| `product:metadata` | Changes affecting the modern metadata tool |
| Both labels above | Shared changes affecting both products |
| `product:none` | Repository maintenance with no product release-note entry |

`product:none` must be used alone: do not combine it with either product label. It is not appropriate for a product change simply because no release is planned yet. An unknown product label does not satisfy validation.

### Category labels or title prefixes

Choose exactly one approved category label **or** begin the PR title with its recognized prefix:

| Category label | Alternative title prefix | Example title |
| --- | --- | --- |
| `change:feature` | `Add:` | `Add: Support aggregate queries` |
| `change:fix` | `Fix:` | `Fix: Handle missing security roles` |
| `change:change` | `Change:` | `Change: Update authentication defaults` |
| `change:breaking` | `Breaking:` | `Breaking: Drop an unsupported target framework` |
| `change:chore` | `Chore:` | `Chore: Refresh CI configuration` |

A recognized title prefix makes the category label optional. Prefix matching is case-insensitive and requires descriptive text after the colon. If you use both, they must agree. Multiple category labels or a conflicting title fail validation.

These category rules **also apply to `product:none`**. For example, a maintenance PR can use `product:none` with the title `Chore: Update issue templates`, without a category label.

The PR title supplies the user-facing release-note text, rendered as:

```markdown
* Fix: Handle missing security roles (#123)
```

Write descriptive titles and review product classification before merging. Shared-source fixes often need both product labels. Categories describe the change; the maintainer still chooses the release version.

The required checks are **`PR product labels`** and **`PR change category`**. Labels and title edits rerun validation. Both checks must be configured as required on `master`; the workflow files alone are not merge protection.

## What happens after a merge

1. **Update product changelogs** runs after merges into `master`.
2. It creates or refreshes **one combined PR** from `automation/update-changelogs`, titled **Chore: Update generated product changelogs**. Core notes, metadata notes, and release-history updates share this PR.
3. Every additional merge into `master` updates that same open PR. Multiple merges do not create multiple changelog PRs.
4. The bot applies `product:none` and `change:chore`. Its PR is excluded from both release notes and the ledger, so merging it does not generate another housekeeping PR.
5. The workflow explicitly starts the PR product/category checks and CI using `gh workflow run`. It does not rely on bot-created PR events to trigger checks.
6. A maintainer reviews and merges the PR through normal branch protection. Merge, squash, and rebase are supported. Only then do the tracked changelogs on `master` include the generated updates.

A shared feature PR appears in both changelogs; an ordinary `product:none` maintenance PR appears in neither. The automation PR changes only the two changelogs and `.github/release-state.json`. Do not commit unrelated files or edit the reserved branch directly; the writer refuses to overwrite unowned changes.

Pending note metadata is frozen when first generated, including while the changelog PR awaits review. Later edits to the originating PR's title/labels do not silently rewrite it; use an explicitly reviewed ledger/source correction if needed. Existing historical sections are preserved.

**Before tagging, wait for the changelog PR's checks, review it, and merge it.** Pending PR content is not treated as released source. If more feature PRs merge before you tag, let the same changelog PR refresh (or merge the next one if the previous PR has already merged). A source missing required PR records fails release validation.

## Maintainer release checklist

1. Confirm the selected product's pending notes are correct and the changelog PR covering the source has been reviewed and merged. Required checks must pass; an open changelog PR is not enough.
2. Choose that product's next version. Review breaking changes and prerelease intent; no automated version bump is inferred solely from category labels.
3. Fetch current `master` and tags. Select the exact reconciled commit to release; do not unintentionally tag unrelated local work. Preparation and publication use trusted default-branch scripts to validate this separate source checkout.
4. Optionally run a manual release preview with the product, version, source ref, and `dry_run: true`. Preview creates no release, obtains no NuGet credentials, and commits/pushes no tracked-file updates. Release files are generated only in its temporary build workspace.
5. Create and push an annotated tag for the selected product:

```bash
git fetch origin master --tags

# Replace the version and source SHA with the chosen values.
# These are examples, not a request to publish these versions.
git tag -a core-v1.20.3 <reconciled-commit-sha> -m "XrmMockup365 1.20.3"
git push origin core-v1.20.3

# A separate metadata release, possibly from the same reconciled commit:
git tag -a metadata-v1.2.1 <reconciled-commit-sha> -m "MetadataGenerator 1.2.1"
git push origin metadata-v1.2.1
```

6. Monitor the selected product's release workflow. It builds/tests the solution but packs/publishes only that product, using the tag's version.
7. Confirm the NuGet publication and finalized GitHub release/assets. Review and merge the changelog PR containing the versioned history update afterward.

Releasing both products requires two tags and produces two GitHub releases. There is no need to edit either changelog's version, manually create a GitHub release, or copy a release description.

## Release notes and tracked history

Each release freezes the selected product's unreleased PR entries at the tagged source. The same snapshot supplies its GitHub release body and NuGet release notes. The pipeline generates a version/date section in the release workspace and stores the generated notes/snapshot as release artifacts.

After successful publication, the shared writer adds that exact versioned section to the same open changelog PR, or creates the next PR if the previous one has merged. Review and merge it to update history on `master`. It removes only that release's consumed pending entries; newer merged changes and the other product's notes remain untouched. Multiple publication/merge updates are combined into one open PR.

**The tag itself retains its reconciled `Unreleased` source file.** The release artifacts contain the generated versioned file, and `master` receives the historical section afterward. This is intentional: never move a published tag to include the later generated documentation commit.

## Failures and recovery

- **Missing/invalid PR classification:** correct labels/title and rerun validation. If already merged through an exceptional bypass, reconcile the correction before releasing.
- **Tag created before reconciliation:** no publication should occur. Complete reconciliation and select a valid source through the documented recovery path; never move a tag that has published artifacts. Prefer using the manual preview before creating another production tag.
- **An unfinished draft has been superseded by a newer same-product release:** do not retry publication of its obsolete snapshot. Abandon/clean up that draft as appropriate and prepare a release from current reconciled source with a new version. Matching already-completed releases can still be recovered without republishing.
- **Package publication fails:** the GitHub release remains a draft. Investigate and retry using the stored snapshot/artifacts; do not create a combined tag or accidentally publish the other product.
- **Package published but GitHub finalization fails:** recover the existing release. Do not assume a newly rebuilt package is identical merely because NuGet rejects the existing version.
- **Symbols uploaded but the publication receipt was not saved:** verify both successful uploads against the frozen artifacts/logs before recording the receipt and finalizing the same release. Symbol duplicates are not automatically ignored.
- **Release published but changelog PR creation/update fails:** rerun **Update product changelogs**. It can recover successful snapshots and update the same PR. Do not republish the package or move its tag. Merge the resulting PR to finish tracked history.

Generated-file writes are serialized and restricted to the two changelogs and release ledger on the reserved bot branch. Updates use a lease when refreshing that branch to avoid overwriting unexpected concurrent edits. The pipeline never pushes to `master`, approves its own PR, or bypasses required checks/reviews.

When deploying release-automation fixes, start retries from the updated `master` workflow. Re-running a historical workflow definition does not retroactively replace its old steps.

For workflow requirements, security boundaries, migration baselines, recovery details, and acceptance tests, see [docs/release-flow.md](docs/release-flow.md).
