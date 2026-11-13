# Release sequence

Updated: 13 November 2026. This describes the simplified implementation.

```mermaid
sequenceDiagram
    autonumber
    actor Maintainer
    participant GitHub as GitHub repository
    participant Checks as PR checks and CI
    participant Notes as Changelog workflow
    participant Build as Release build (read-only)
    participant Publish as Release publish (write/OIDC)
    participant NuGet

    Maintainer->>GitHub: Open PR with product labels and category label or title prefix
    GitHub->>Checks: Validate classification and run .NET CI
    opt Automation or packaging configuration changed
        GitHub->>Checks: Run offline release functional tests
    end
    Maintainer->>GitHub: Review and merge feature PR to master
    GitHub->>Notes: Trigger on master push
    Notes->>GitHub: Read merged PRs and successful product releases
    Notes->>GitHub: Push generated changelog commit to temporary branch
    Notes->>Checks: Dispatch read-only ci.yml on that exact branch/commit
    Checks-->>Notes: CI result (failure stops without updating master)
    Notes->>GitHub: After CI succeeds, fast-forward master to checked commit
    Notes->>GitHub: Remove temporary branch
    opt Another merge advances master before the push
        Notes->>GitHub: Fetch new master, regenerate, rerun CI and retry (up to three attempts)
    end
    Note over Notes,GitHub: GITHUB_TOKEN push does not recursively trigger workflows
    Maintainer->>GitHub: Fetch master containing the notes update before tagging

    alt Manual preview
        Maintainer->>Build: Dispatch candidate tag and source_ref (preview-only)
    else Product release
        Maintainer->>GitHub: Push core-vVERSION or metadata-vVERSION
        GitHub->>Build: Trigger release
    end
    Build->>GitHub: Checkout trusted scripts and exact source separately
    Build->>Build: Validate selected product version, ancestry and fresh notes
    Build->>Build: Build/test solution and pack selected nupkg/snupkg
    Build->>Build: Validate package pair
    Build->>GitHub: Save packages, notes and build metadata as artifacts
    alt Preview
        Build-->>Maintainer: Artifacts only, no publication
    else Tag release
        GitHub->>Publish: Download successful build artifacts
        Publish->>Publish: Validate package pair and exact tag/source
        Publish->>NuGet: Obtain short-lived credentials through OIDC
        Publish->>NuGet: One dotnet nuget push (package and matching symbols)
        alt Push fails
            Publish-->>Maintainer: Stop, check partial acceptance before restarting
        else Push succeeds
            Publish->>GitHub: Create release with prepared notes and both assets
            Publish->>Notes: Dispatch history refresh
            Notes->>GitHub: Stage versioned history, run CI, then fast-forward master
        end
    end
```

Pending notes and published history are automatic bot commits on master. No changelog PR, PR-creation permission, or follow-up approval is needed. The required **ci** check runs before each direct write; other repository rules must also permit direct writes.

## Simplifications applied

- PowerShell handles policy and text/JSON transformations; `gh` and `dotnet` perform operations directly. No Node runtime or custom GitHub HTTP client.
- One read-only `pull_request` classification workflow fetches PR metadata once and reports a single native **PR release labels** check. Manual validation dispatch verifies the selected PR head commit.
- Product/category definitions live in one JSON configuration file.
- One serialized changelog writer stages only the two note files on temporary branches, dispatches CI on each exact commit, then fast-forwards master, with bounded regeneration retries for concurrent merges and no force pushes.
- Label provisioning and GitHub publication are short workflow steps, not separate wrapper scripts.
- Tag pushes are the only publication trigger. Manual dispatch only builds previews.
- One package push also uploads matching symbols. Partial success is recovered manually from artifacts.
- No release ledger, merge queues, emergency audit mechanism, checksums or draft-retry framework.
- Tests assert observable behavior using local Git and mocked GitHub responses, not exact command argument order or exception wording. They run only for relevant file changes or manual dispatch.

## Security boundaries retained

- Keep the build/publish separation: source builds have read-only credentials; only the trusted publish job has write/OIDC credentials.
- Keep source PR review, label validation and normal CI. The bot's derived Markdown-only commits explicitly run read-only CI before the protected push, without recursive push-triggered runs; release builds still test the exact tagged source.
- Keep the fresh-notes gate before publication. Maintainers tag a source containing the automatic notes commit, not an earlier feature merge with stale notes.

See [maintainer instructions](../RELEASE.md) and [the release-flow specification](release-flow.md).
