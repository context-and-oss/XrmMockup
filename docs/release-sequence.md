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
    Notes->>GitHub: Create/update one changelog PR (product:none, change:chore)
    Notes->>Checks: Dispatch classification for the current PR head
    GitHub->>Checks: Native PR CI, maintainer approval when requested
    Note over Checks,GitHub: Verify both required checks bind to the bot PR during setup
    Note over Notes,GitHub: Further merges refresh the same open PR
    Maintainer->>GitHub: Review and merge changelog PR
    GitHub->>Notes: Trigger again, its own merge contributes no note

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
            Notes->>GitHub: Update combined changelog PR with versioned history
            Maintainer->>GitHub: Review and merge history PR
        end
    end
```

Pending notes and published history normally require two changelog PR merges, but later feature merges can share the history PR. There is only one open changelog PR at a time.

## Simplifications applied

- PowerShell handles policy and text/JSON transformations; `gh` and `dotnet` perform operations directly. No Node runtime or custom GitHub HTTP client.
- One read-only `pull_request` classification workflow fetches PR metadata once and reports a single native **PR release labels** check. Explicit bot-PR dispatch runs on its head branch.
- Product/category definitions live in one JSON configuration file.
- Label provisioning and GitHub publication are short workflow steps, not separate wrapper scripts.
- Tag pushes are the only publication trigger. Manual dispatch only builds previews.
- One package push also uploads matching symbols. Partial success is recovered manually from artifacts.
- No release ledger, merge queues, emergency audit mechanism, checksums or draft-retry framework.
- Tests assert observable behavior using local Git and mocked GitHub responses, not exact command argument order or exception wording. They run only for relevant file changes or manual dispatch.

## Further simplifications worth considering

1. **Keep the current review gate unless it becomes a burden.** Generating notes only at release time would remove the pre-release changelog PR, but files would no longer stay current after merges. That changes the agreed functionality.
2. **Keep the build/publish separation.** Combining them shortens YAML but exposes write/OIDC credentials to source builds; the current security boundary is worth the extra job.
3. **Start with ordinary CI on bot PRs.** A special Markdown-only CI shortcut could save builds, but adds another policy and reporting path. Add it only if actual CI cost warrants it.

See [maintainer instructions](../RELEASE.md) and [the release-flow specification](release-flow.md).
