# Release process

Core and metadata releases are independent. A product tag chooses the package and its version; maintainers do not edit version headings or copy release descriptions manually.

## Repository setup

1. Run **Set up release labels** from `master` to create the labels below. For the initial implementation PR, create/apply its labels manually before merging.
2. Require **PR product labels**, **PR change category**, and the normal .NET **ci** check on `master`. Keep normal PR reviews and branch protection.
3. Enable **Settings → Actions → General → Workflow permissions → Allow GitHub Actions to create and approve pull requests**, including the organization permission if applicable. The writer uses `GITHUB_TOKEN`; it creates PRs but does not approve or merge them. No App, PAT, or direct-master bypass is needed.
4. Keep `NUGET_USER` and the NuGet Trusted Publishing policy configured for `release.yml` and the two packages. Restrict who can create release tags.

The separate **Release automation tests** workflow runs only for automation/workflow or packaging-configuration changes, or a manual dispatch. Do not make this path-filtered check globally required; if an old `release-automation` check was required, remove that requirement so unrelated PRs are not blocked. Product/category checks and .NET CI still run on every PR.

Automation uses Node 24 and GitHub CLI (`gh`). For local use, install both and authenticate with `gh auth login` or `GH_TOKEN`; set `GITHUB_REPOSITORY` to `owner/repository`.

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

## Changelog PR

After merges to `master`, the writer regenerates pending notes from merged PRs since each product's previous **successful GitHub release**. It creates/updates **one combined PR** on `automation/update-changelogs`. Additional merges and published-release history updates refresh that same open PR.

The bot PR has `product:none` and `change:chore`, so it does not appear in its own notes. It changes only `RELEASE_NOTES.md` and `src/MetadataGen/MetadataGenerator.Tool/CHANGELOG.md`. The writer explicitly dispatches its label checks and CI; maintainers review and merge it normally.

Labels and titles are read when notes are regenerated. Correct them before merging feature PRs; a deliberate later correction can update pending notes. Published history is preserved in the files, not rewritten from current PR metadata. Do not edit the generated branch by hand.

**Merge the changelog PR covering your source before tagging.** The release workflow rejects stale notes. If more changes merge, let the open PR refresh, or merge the next PR if the previous one already merged.

The initial notes collection cutoff is `46f2af04b6d5dc4797f6069c31610c7101de3b47`; existing core `1.20.2` and metadata `1.2.0` history is retained. Audit any old metadata changes not covered by that legacy release before the first new metadata release.

## Release a product

| Tag | NuGet package |
| --- | --- |
| `core-v<VERSION>` | `XrmMockup365` |
| `metadata-v<VERSION>` | `XrmMockup.MetadataGenerator` |

Use semantic versions such as `1.20.3` or `1.3.0-rc.1`. No combined-version tags are used for new releases.

1. Review and merge the changelog PR; confirm source CI is green.
2. Choose a version newer than the previous successful release of that product. Changing prerelease letter casing does not create a new NuGet version.
3. Optionally run **Release** manually with product, version, source ref, and `dry_run: true`. This builds a preview without NuGet login, publication, or repository writes.
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

The workflow checks out the exact source, builds/tests the solution, packs only the selected product, and makes **one NuGet push**. The matching `.snupkg` is packed beside the `.nupkg` and published automatically by that same command. Both files are retained as artifacts and attached to the GitHub release. Only after that push succeeds does it create the GitHub release, attach the package, and request the changelog PR's versioned history update. Review/merge that PR afterward. The published tag is not moved to include this later history commit.

## Failed release

If the NuGet push fails, the run stops and no GitHub release is created. Fix the cause, remove the failed tag remotely and locally, then create it again at the corrected/reconciled source:

```bash
git push origin --delete core-v1.20.3
git tag -d core-v1.20.3
```

**Check NuGet first if the failure was a timeout, a symbol upload failure, or happened after the push.** The regular package is uploaded before its symbols, so it may have been accepted even if symbol publishing failed; published versions cannot be overwritten. In that case, upload the saved matching `.snupkg` manually if needed, then complete the matching GitHub release using the saved artifacts, or choose a new package version. Do not blindly rebuild/push the same version or ignore duplicates.

If GitHub release creation or changelog PR updating fails after NuGet succeeds, recover those steps manually using the saved notes/package. Do not move a successful tag or republish the package. Rerunning **Update product changelogs** can refresh the pending history PR.

Use PRs for normal source changes. If someone bypasses protection with an emergency direct push, it has no automatic PR-derived note; document/classify the change through a follow-up PR and handle its release manually as needed.

See [the release-flow specification](docs/release-flow.md) for workflow boundaries and tests.
