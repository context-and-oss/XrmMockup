$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Release.psm1')
$config = Get-ReleaseConfig; $branch = $config.changelogBranch
[void](Invoke-Git -Arguments @('fetch', 'origin', 'master', '--tags'))
[void](Invoke-Git -Arguments @('checkout', '-B', 'release-changelog-update', 'origin/master'))
$open = @(Get-GhList "/repos/$env:GITHUB_REPOSITORY/pulls?state=open&base=master&head=$([uri]::EscapeDataString($env:GITHUB_REPOSITORY.Split('/')[0] + ':' + $branch))")
if ($open.Count -gt 1 -or ($open.Count -and (!(Test-ChangelogPr $open[0]) -or $open[0].base.repo.full_name -ne $env:GITHUB_REPOSITORY))) { throw 'Reserved branch has an unexpected PR' }
$head = (Invoke-Git -Arguments @('ls-remote', '--heads', 'origin', "refs/heads/$branch")) -split '\s+' | Select-Object -First 1
$allowed = @($config.products.Values | ForEach-Object { $_.changelog })
if ($head) {
    [void](Invoke-Git -Arguments @('fetch', 'origin', "+refs/heads/${branch}:refs/remotes/origin/$branch"))
    $paths = (Invoke-Git -Arguments @('diff', '--name-only', "origin/master...origin/$branch")) -split "`n" | Where-Object { $_ }
    if ($paths | Where-Object { $_ -notin $allowed }) { throw 'Reserved branch contains unrelated changes' }
}
$releases = @(Get-GhList "/repos/$env:GITHUB_REPOSITORY/releases")
$entries = @(Get-PendingChanges $env:GITHUB_REPOSITORY (Invoke-Git -Arguments @('rev-parse', 'HEAD')) $releases)
foreach ($product in $config.products.Keys) {
    $path = $config.products[$product].changelog
    [IO.File]::WriteAllText((Join-Path $PWD $path), (Get-Changelog (Get-Content $path -Raw) $entries $product @(Get-ProductReleases $releases $product)))
}
if (!(Invoke-Git -Arguments @('diff', '--name-only', 'HEAD'))) {
    if ($open.Count) { [void](Invoke-Gh -Arguments @('pr', 'close', "$($open[0].number)", '--repo', $env:GITHUB_REPOSITORY)) }
    Write-Output 'Changelogs are current'; return
}
[void](Invoke-Git -Arguments (@('add', '--') + $allowed))
if (!$head -or (Invoke-Git -Arguments @('diff', '--cached', '--name-only', $head))) {
    [void](Invoke-Git -Arguments @('config', 'user.name', 'github-actions[bot]'))
    [void](Invoke-Git -Arguments @('config', 'user.email', '41898282+github-actions[bot]@users.noreply.github.com'))
    [void](Invoke-Git -Arguments @('commit', '-m', 'Chore: Update generated product changelogs'))
    [void](Invoke-Git -Arguments @('push', "--force-with-lease=refs/heads/${branch}:$head", 'origin', "HEAD:refs/heads/$branch"))
}
$arguments = @('pr', $(if ($open.Count) { 'edit' } else { 'create' }))
if ($open.Count) { $arguments += "$($open[0].number)" } else { $arguments += @('--head', $branch, '--base', 'master') }
$arguments += @('--repo', $env:GITHUB_REPOSITORY, '--title', 'Chore: Update generated product changelogs', '--body', 'Generated notes and release history. Further master merges update this same PR. Review and merge before tagging.')
$arguments += if ($open.Count) { @('--add-label', 'product:none', '--add-label', 'change:chore') } else { @('--label', 'product:none', '--label', 'change:chore') }
if ($open.Count) {
    foreach ($label in $open[0].labels.name | Where-Object { $_ -match '^(product|change):' -and $_ -notin @('product:none', 'change:chore') }) { $arguments += @('--remove-label', $label) }
}
[void](Invoke-Gh -Arguments $arguments)
$pr = @(Get-GhList "/repos/$env:GITHUB_REPOSITORY/pulls?state=open&base=master&head=$([uri]::EscapeDataString($env:GITHUB_REPOSITORY.Split('/')[0] + ':' + $branch))")[0]
@{ pr_number = "$($pr.number)" } | ConvertTo-Json -Compress | & gh workflow run pr-classification.yml --repo $env:GITHUB_REPOSITORY --ref master --json
if ($LASTEXITCODE -ne 0) { throw 'Cannot dispatch PR classification' }
Write-Output "Updated changelog PR #$($pr.number); master was not pushed"
