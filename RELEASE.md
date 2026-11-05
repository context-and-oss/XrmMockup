# Release process

**Before first use:** complete the repository setup below. The workflows are implemented, but required checks, the changelog writer identity, and NuGet Trusted Publishing must be configured before production releases.

## Repository setup

1. Run **Set up release labels** (`setup-release-labels.yml`) from `master` to create/update the approved labels. For the initial implementation PR, create/apply the needed labels manually before merging.
2. Require **`PR product labels`** and **`PR change category`** in the `master` branch ruleset. They are published against the PR head SHA. Restrict human direct pushes and general bypass; merge-queue validation is included.
3. Configure a dedicated GitHub App with **`RELEASE_APP_ID`** (repository variable) and **`RELEASE_APP_PRIVATE_KEY`** (secret); the workflow creates a short-lived, repository-scoped installation token. Grant the App contents write, pull requests read, and the narrowly scoped `master` bypass needed for generated commits. Alternatively, set **`RELEASE_BOT_TOKEN`** to a repository-scoped PAT for a dedicated bot and rotate it. The writer refuses to commit files outside the two changelogs and `.github/release-state.json`.
4. Keep **`NUGET_USER`** configured and verify the NuGet Trusted Publishing policy for `release.yml` and both package IDs. Restrict who may create the approved release tags.
5. Run **Update product changelogs** manually, confirm its generated commit, and try a manual **Release** preview before creating production tags.

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

1. The changelog reconciler runs after a push/merge to `master`.
2. It records the merged PR's product/category/title and updates each affected file's `### Unreleased` section.
3. It commits the generated files and release ledger directly to `master` using the approved automation identity.
4. A shared PR appears in both files. A `product:none` PR appears in neither.

Do not manually maintain generated pending entries or version headings. Existing historical sections are preserved. Later edits to an already recorded PR's labels/title do not silently rewrite its notes; request an explicit reviewed correction if needed.

There is a short asynchronous interval after a merge. **Wait for successful changelog reconciliation and its generated commit before tagging.** A tag whose source does not include all required PR records fails release validation.

## Maintainer release checklist

1. Confirm the selected product's pending notes are correct, the changelog updater has succeeded, and the source has passed verification.
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
7. Confirm the NuGet publication, finalized GitHub release/assets, and successful tracked-changelog finalization.

Releasing both products requires two tags and produces two GitHub releases. There is no need to edit either changelog's version, manually create a GitHub release, or copy a release description.

## Release notes and tracked history

Each release freezes the selected product's unreleased PR entries at the tagged source. The same snapshot supplies its GitHub release body and NuGet release notes. The pipeline generates a version/date section in the release workspace and stores the generated notes/snapshot as release artifacts.

After successful publication, the shared writer adds that exact versioned section to the tracked changelog on current `master`. It removes only that release's consumed pending entries; newer merged changes and the other product's notes remain untouched.

**The tag itself retains its reconciled `Unreleased` source file.** The release artifacts contain the generated versioned file, and `master` receives the historical section afterward. This is intentional: never move a published tag to include the later generated documentation commit.

## Failures and recovery

- **Missing/invalid PR classification:** correct labels/title and rerun validation. If already merged through an exceptional bypass, reconcile the correction before releasing.
- **Tag created before reconciliation:** no publication should occur. Complete reconciliation and select a valid source through the documented recovery path; never move a tag that has published artifacts. Prefer using the manual preview before creating another production tag.
- **An unfinished draft has been superseded by a newer same-product release:** do not retry publication of its obsolete snapshot. Abandon/clean up that draft as appropriate and prepare a release from current reconciled source with a new version. Matching already-completed releases can still be recovered without republishing.
- **Package publication fails:** the GitHub release remains a draft. Investigate and retry using the stored snapshot/artifacts; do not create a combined tag or accidentally publish the other product.
- **Package published but GitHub finalization fails:** recover the existing release. Do not assume a newly rebuilt package is identical merely because NuGet rejects the existing version.
- **Symbols uploaded but the publication receipt was not saved:** verify both successful uploads against the frozen artifacts/logs before recording the receipt and finalizing the same release. Symbol duplicates are not automatically ignored.
- **Release published but tracked-file update fails:** rerun changelog reconciliation/finalization. A later reconciliation can recover successful snapshots. Do not republish the package or move its tag.

Generated-file writes are serialized and restricted to the two changelogs and release ledger. Direct pushes to `master` are not a substitute for classified PRs; only the narrowly scoped automation identity receives the needed bypass.

When deploying release-automation fixes, start retries from the updated `master` workflow. Re-running a historical workflow definition does not retroactively replace its old steps.

For workflow requirements, security boundaries, migration baselines, recovery details, and acceptance tests, see [docs/release-flow.md](docs/release-flow.md).
