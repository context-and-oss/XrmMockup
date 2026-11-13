$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Release.psm1')
$config = Get-ReleaseConfig
$allowed = @($config.products.Values | ForEach-Object { $_.changelog })
if (Invoke-Git -Arguments @('status', '--porcelain')) { throw 'Changelog updates require a clean working tree' }

# A merge can arrive after checkout even though workflow writers are serialized.
# Never force-push or rebase generated notes: rebuild them from the new master.
$maxAttempts = 3
foreach ($attempt in 1..$maxAttempts) {
    [void](Invoke-Git -Arguments @('fetch', 'origin', '+refs/heads/master:refs/remotes/origin/master', '--tags'))
    [void](Invoke-Git -Arguments @('checkout', '-B', 'release-changelog-update', 'origin/master'))
    $source = Invoke-Git -Arguments @('rev-parse', 'HEAD')
    $releases = @(Get-GhList "/repos/$env:GITHUB_REPOSITORY/releases")
    $entries = @(Get-PendingChanges $env:GITHUB_REPOSITORY $source $releases)
    foreach ($product in $config.products.Keys) {
        $path = $config.products[$product].changelog
        [IO.File]::WriteAllText((Join-Path $PWD $path), (Get-Changelog (Get-Content $path -Raw) $entries $product @(Get-ProductReleases $releases $product)))
    }
    if (!(Invoke-Git -Arguments @('diff', '--name-only', 'HEAD'))) {
        Write-Output 'Changelogs are current'; return
    }
    [void](Invoke-Git -Arguments (@('add', '--') + $allowed))
    $paths = (Invoke-Git -Arguments @('diff', '--cached', '--name-only')) -split "`n" | Where-Object { $_ }
    if ($paths | Where-Object { $_ -notin $allowed }) { throw 'Changelog update contains unrelated changes' }
    [void](Invoke-Git -Arguments @('config', 'user.name', 'github-actions[bot]'))
    [void](Invoke-Git -Arguments @('config', 'user.email', '41898282+github-actions[bot]@users.noreply.github.com'))
    [void](Invoke-Git -Arguments @('commit', '-m', 'Chore: Update generated product changelogs'))
    # Handle only a moving master as retryable; permission/protection failures
    # with an unchanged remote should surface immediately.
    & git push origin HEAD:refs/heads/master
    if ($LASTEXITCODE -eq 0) {
        Write-Output "Updated product changelogs on master from $source"; return
    }
    [void](Invoke-Git -Arguments @('fetch', 'origin', '+refs/heads/master:refs/remotes/origin/master'))
    if ((Invoke-Git -Arguments @('rev-parse', 'origin/master')) -eq $source) {
        throw 'Cannot push changelogs to master; check contents: write and repository branch rules'
    }
    if ($attempt -lt $maxAttempts) { Write-Output 'Master advanced during the update; regenerating changelogs' }
}
throw "Master kept advancing; changelog update failed after $maxAttempts attempts. Rerun Update product changelogs."
