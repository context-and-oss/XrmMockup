# Release process

Core and metadata releases are independent. A product tag chooses the package and its version; maintainers do not edit version headings or copy release descriptions manually.

## Repository setup

1. Run **Set up release labels** from `master` to create the labels below. The initial implementation PR is excluded by the migration cutoff; all subsequent PRs must be classified before merging.
2. Use **PR release labels** and the normal .NET **ci** check for source PRs. If you configure required checks, replace any old **PR product labels**, **PR change category**, or **validate** requirements with **PR release labels**. Continue reviewing source PRs normally. Branch protection or repository rules must permit the changelog writer to push directly to `master`; `contents: write` does not bypass those rules.
3. Ensure repository/organization Actions policy permits `GITHUB_TOKEN` to use `contents: write`. The changelog workflow requests that permission explicitly and only `pull-requests: read` for metadata. **Allow GitHub Actions to create and approve pull requests is not needed**; leave it disabled. No App or PAT is needed.
4. Keep `NUGET_USER` and the NuGet Trusted Publishing policy configured for `release.yml` and the two packages. Restrict who can create release tags.

The separate **Release automation tests** workflow runs only for automation/workflow or packaging-configuration changes, or a manual dispatch. Do not make this path-filtered check globally required; if an old `release-automation` check was required, remove that requirement so unrelated PRs are not blocked. The single release-label check and .NET CI still run on every PR.

Automation uses PowerShell 7, Git and GitHub CLI (`gh`); release builds also need .NET. Node is not needed. For live local operations, authenticate with `gh auth login` or `GH_TOKEN` and set `GITHUB_REPOSITORY` to `owner/repository`.

Run offline functional tests with `pwsh -NoProfile -File scripts/release/Test-Release.ps1`. These use temporary local Git repositories, mocked `gh` responses and synthetic package archives; no remote access, GitHub authentication or NuGet publication is needed.

The label workflow uses `pull_request` with read-only permissions and reruns when labels, the title, or the PR head change. Missing or invalid classification fails the native **PR release labels** check; no custom check runs are posted. Fork runs may require maintainer approval. Review changes to the workflow and validator as part of normal code review. For a manual validation of a same-repository PR, select its head branch in **Validate PR release labels → Run workflow** and supply its PR number; dispatching on another commit fails rather than reporting success against the wrong commit.

## Required PR labels

Every PR to `master` needs a product classification:

| Labels | Meaning |
| --- | --- |
| `product:core` | Core package changes |
| `product:metadata` | Metadata tool changes |
| Both | Changes affecting both products |
| `product:none` | Repository maintenance; no release-note entry |

`product:none` is exclusive: do not combine it with a product label. Use the affected product even if you are not planning an immediate release.

Also choose one category label **or** start the title with its prefix:

| Category label | Title prefix |
| --- | --- |
| `change:feature` | `Add:` |
| `change:fix` | `Fix:` |
| `change:change` | `Change:` |
| `change:breaking` | `Breaking:` |
| `change:chore` | `Chore:` |

Prefixes are case-insensitive and require descriptive text. Multiple category labels or a conflicting prefix fail validation. Maintenance PRs also need a category. The title supplies the user-facing note, for example `* Fix: Handle missing roles (#123)`.

## Automatic changelog commits

After merges to `master`, **Update product changelogs** regenerates pending notes from merged PRs since each product's previous **successful GitHub release**. It commits only `RELEASE_NOTES.md` and `src/MetadataGen/MetadataGenerator.Tool/CHANGELOG.md` directly to `master` as `github-actions[bot]`. There is no generated branch or changelog PR to approve or merge. Identical content produces no commit, and the bot's direct commits contribute no PR-derived note.

Writers are serialized. Each run starts from current remote `master`, rather than an old event SHA. If another merge wins the push race, the writer regenerates from the new master and retries, up to three attempts. It never force-pushes. An unchanged-master push rejection fails immediately so permission or branch-rule problems are visible. Rerun the workflow after resolving a failure.

Pushes made with `GITHUB_TOKEN` do not start another push-triggered Actions run, so generated commits do not recursively trigger the writer or normal CI. Check source PR/master-merge CI, not the presence of a CI check on the bot's Markdown-only commit. The release workflow still builds/tests the exact tagged source.

Labels and titles are read when notes are regenerated. Correct them before merging source PRs; after a deliberate later correction, run **Update product changelogs** manually from `master`. Published history is preserved in the files, not rewritten from current PR metadata. Do not edit generated pending sections by hand.

**Wait for the changelog workflow to finish and fetch its commit before tagging.** The release workflow rejects a source with stale notes. If another source PR merges, wait for the next update covering it. A tag on the feature merge commit before the notes update remains stale even after the bot updates `master`.

For migration, close any old open changelog PR and remove the obsolete `automation/update-changelogs` branch once its history is reconciled. The new writer does not manage or merge that branch.

The initial notes collection cutoff is the release-automation rollout merge, PR #361 (`6e689d959b9013e2318320e9c317449d0de02c2d`, 10 November 2026). The cutoff is exclusive: the rollout PR and older PRs do not need retroactive product/category labels. Existing core `1.20.2` and metadata `1.2.0` history is retained. Audit any old metadata changes not covered by that legacy release before the first new metadata release. Missing or invalid classification on subsequent pending PRs still fails; it is not silently skipped.

## Release a product

| Tag | NuGet package |
| --- | --- |
| `core-v<VERSION>` | `XrmMockup365` |
| `metadata-v<VERSION>` | `XrmMockup.MetadataGenerator` |

Use semantic versions such as `1.20.3` or `1.3.0-rc.1`. No combined-version tags are used for new releases.

1. Confirm source PR/master-merge CI is green, wait for **Update product changelogs** to finish, and select the resulting source with current notes.
2. Choose a version newer than the previous successful release of that product. Changing prerelease letter casing does not create a new NuGet version.
3. Optionally run **Release** manually with a candidate `tag` (for example `core-v1.20.3`) and `source_ref`. Manual dispatch is preview-only. This builds a preview without NuGet login, publication, or repository writes.
4. Fetch current refs, choose the exact reviewed source, and create/push an annotated tag:

```bash
git fetch origin master --tags
git tag -a core-v1.20.3 <reviewed-source-sha> -m "XrmMockup365 1.20.3"
git push origin core-v1.20.3

# A separate metadata release, possibly at the same commit:
git tag -a metadata-v1.2.1 <reviewed-source-sha> -m "MetadataGenerator 1.2.1"
git push origin metadata-v1.2.1
```

These versions are examples. Releasing both products means two tags and two releases.

Release runs and manual previews share a separate concurrency queue for each product. One run executes at a time, and `queue: max` retains up to 100 pending runs rather than replacing a pending release when another request arrives. Requests beyond that limit are canceled. Queue order follows when runs start waiting, not necessarily tag-push order; choose and push release versions in order.

The workflow checks out the exact source, builds/tests the solution, packs only the selected product, and makes **one NuGet push**. The matching `.snupkg` is packed beside the `.nupkg` and published automatically by that same command. Both files are retained as artifacts and attached to the GitHub release. Only after that push succeeds does it create the GitHub release, attach the package, and dispatch **Update product changelogs** to commit versioned history directly to `master`. No follow-up PR is needed. The published tag is not moved to include this later history commit.

## Failed release

If the NuGet push fails, the run stops and no GitHub release is created. Fix the cause, remove the failed tag remotely and locally, then create it again at the corrected/reconciled source:

```bash
git push origin --delete core-v1.20.3
git tag -d core-v1.20.3
```

**Check NuGet first if the failure was a timeout, a symbol upload failure, or happened after the push.** The regular package is uploaded before its symbols, so it may have been accepted even if symbol publishing failed; published versions cannot be overwritten. In that case, upload the saved matching `.snupkg` manually if needed, then complete the matching GitHub release using the saved artifacts, or choose a new package version. Do not blindly rebuild/push the same version or ignore duplicates.

If GitHub release creation or changelog updating fails after NuGet succeeds, recover those steps manually using the saved notes/package. Do not move a successful tag or republish the package. Rerunning **Update product changelogs** can refresh release history directly on `master`.

Use PRs for normal source changes. A non-bot direct push also triggers the writer, but it has no automatic PR-derived note; document/classify the change through a follow-up PR and handle its release manually as needed.

See [the sequence diagram and simplification options](docs/release-sequence.md), or [the release-flow specification](docs/release-flow.md) for workflow boundaries and tests.
