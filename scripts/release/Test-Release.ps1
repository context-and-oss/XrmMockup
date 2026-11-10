# Functional tests; no Pester/Node dependency or live GitHub/NuGet writes.
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Release.psm1') -Force
function Assert($Condition, [string]$Message) { if (!$Condition) { throw $Message } }
function Reject([scriptblock]$Action) {
    $rejected = $false
    try { & $Action | Out-Null } catch { $rejected = $true }
    Assert $rejected 'Expected an invalid operation to fail'
}
# Guard the workflow queue policy; actual scheduling remains GitHub's responsibility.
$workflow = Get-Content (Join-Path $PSScriptRoot '../../.github/workflows/release.yml') -Raw
$concurrency = [regex]::Match($workflow, '(?m)^concurrency:\r?\n(?:[ \t]+[^\r\n]*\r?\n)+').Value
Assert ($concurrency -match '(?m)^  queue: max\s*$') 'Release requests must queue instead of replacing a pending release'
Assert ($concurrency -match '(?m)^  cancel-in-progress: false\s*$') 'Queued releases must not cancel running publication'
Write-Output 'PASS: release workflow retains pending requests without canceling running publication'

$config = Get-ReleaseConfig
Assert ((Compare-Version '1.20.3-RC.1' '1.20.3-rc.1') -eq 0) 'NuGet casing identity'
Assert ((Compare-Version '1.20.3-rc.10' '1.20.3-rc.2') -gt 0) 'Numeric prerelease order'
Assert ((Get-ReleaseTag 'metadata-v1.2.1').product -eq 'metadata') 'Product tag'
Reject { Get-ReleaseTag 'v1.20.3' }; Reject { Get-ReleaseTag 'core-v01.2.3' }
Reject { Get-PrProducts @{ labels = @('product:none', 'product:core') } }
Reject { Get-PrNote @{ title = 'No category'; labels = @('product:core') } }
Reject { Get-PrNote @{ title = 'Add: Conflict'; labels = @('change:fix') } }
$note = Get-PrNote @{ title = 'Fix: Preserve $$ and $& in templates'; labels = @('product:core') }
Assert ($note.description -ceq 'Preserve $$ and $& in templates') 'Literal dollar signs'
$entries = @(@{ number = 1; products = @('core', 'metadata'); category = 'Fix'; description = $note.description })
$original = "# History`n`n### 1.20.2 - old`n* Published history`n"
$rendered = Get-Changelog $original $entries 'core'
Assert ($rendered.Contains('Published history')) 'Archive preservation'
Assert ((Get-Changelog $rendered $entries 'core') -ceq $rendered) 'Idempotent notes'
Write-Output 'PASS: classification, version identity, literal notes and history'

# A real Git fixture plus a small in-memory gh double exercise aggregation and
# preparation without duplicating gh's HTTP transport or a service implementation.
$root = Join-Path ([IO.Path]::GetTempPath()) ('release-test-' + [guid]::NewGuid())
$old = $PWD
$oldRepository = $env:GITHUB_REPOSITORY
$oldOutput = $env:GITHUB_OUTPUT
try {
    New-Item $root -ItemType Directory | Out-Null; Set-Location $root
    [void](Invoke-Git -Arguments @('init', '-b', 'master'))
    [void](Invoke-Git -Arguments @('config', 'user.name', 'maintainer'))
    [void](Invoke-Git -Arguments @('config', 'user.email', 'maintainer@example.test'))
    '.release/' | Set-Content '.gitignore'
    $global:TestPrs = @(); $global:TestBotPr = $null; $global:TestChecks = @(); $global:TestWrites = @()
    $global:TestReleases = foreach ($product in @('core', 'metadata')) {
        $version = $config.products[$product].baselineVersion
        $path = $config.products[$product].changelog
        if (Split-Path $path) { New-Item (Split-Path $path) -ItemType Directory -Force | Out-Null }
        [IO.File]::WriteAllText((Join-Path $PWD $path), (Get-Changelog "### $version - old`n* History`n" @() $product))
        @{ tag_name = "$product-v$version"; draft = $false; published_at = '2026-11-06T00:00:00Z'; body = '* History' }
    }
    [void](Invoke-Git -Arguments @('add', '.')); [void](Invoke-Git -Arguments @('commit', '-m', 'Baseline'))
    foreach ($release in $global:TestReleases) { [void](Invoke-Git -Arguments @('tag', $release.tag_name)) }
    $remote = "$root-remote.git"
    [void](Invoke-Git -Arguments @('init', '--bare', $remote))
    [void](Invoke-Git -Arguments @('remote', 'add', 'origin', $remote))
    [void](Invoke-Git -Arguments @('push', '-u', 'origin', 'master', '--tags'))
    $env:GITHUB_REPOSITORY = 'owner/repo'; $env:GITHUB_OUTPUT = Join-Path $root 'outputs'
    function global:gh {
        [CmdletBinding(PositionalBinding = $false)]
        param([Parameter(ValueFromPipeline)][string]$InputObject, [Parameter(ValueFromRemainingArguments)][object[]]$Arguments)
        process {
            $global:LASTEXITCODE = 0
            if ($Arguments[0] -eq 'api') {
                $path = [string]$Arguments[1]
                if ($path -match '/releases(\?|$)') { foreach ($item in $global:TestReleases) { $item | ConvertTo-Json -Compress -Depth 10 }; return }
                if ($path -match '/commits/([^/]+)/pulls') { foreach ($pr in $global:TestPrs | Where-Object { $_.merge_commit_sha -eq $Matches[1] }) { $pr | ConvertTo-Json -Compress -Depth 10 }; return }
                if ($path.Contains('pulls?')) {
                    if ($global:TestBotPr -and $global:TestBotPr.state -eq 'open') { $global:TestBotPr | ConvertTo-Json -Compress -Depth 10 }; return
                }
                if ($path.EndsWith('/check-runs')) { $global:TestChecks += $InputObject | ConvertFrom-Json -AsHashtable; return }
                if ($path -match '/pulls/(\d+)$') {
                    $pr = @($global:TestPrs + @($global:TestBotPr) | Where-Object { $_ -and $_.number -eq [int]$Matches[1] })[0]
                    $pr | ConvertTo-Json -Compress -Depth 10; return
                }
                throw "Unexpected test API request: $path"
            }
            if ($Arguments[0] -eq 'pr') {
                $global:TestWrites += $Arguments[1]
                if ($Arguments[1] -eq 'close') { $global:TestBotPr.state = 'closed'; return }
                if (!$global:TestBotPr) {
                    $global:TestBotPr = @{ number = 100; state = 'open'; title = 'Chore: Update generated product changelogs'; labels = @('product:none', 'change:chore'); user = @{ login = 'github-actions[bot]' }
                        head = @{ ref = 'automation/update-changelogs'; repo = @{ full_name = 'owner/repo' } }; base = @{ repo = @{ full_name = 'owner/repo' } } }
                }
                return
            }
            if ($Arguments[0] -eq 'workflow') { return }
            throw "Unexpected test CLI operation: $($Arguments -join '|') input:$InputObject"
        }
    }
    function Add-Feature([string[]]$Products, [string]$Title) {
        [void](Invoke-Git -Arguments @('fetch', 'origin', 'master'))
        [void](Invoke-Git -Arguments @('checkout', '--force', '-B', 'master', 'origin/master'))
        [void](Invoke-Git -Arguments @('reset', '--hard', 'origin/master'))
        $number = $global:TestPrs.Count + 1
        $Title | Set-Content "feature-$number.txt"
        [void](Invoke-Git -Arguments @('add', '.')); [void](Invoke-Git -Arguments @('commit', '-m', $Title))
        $sha = Invoke-Git -Arguments @('rev-parse', 'HEAD')
        $global:TestPrs += @{ number = $number; merged_at = '2026-11-06'; merge_commit_sha = $sha; title = $Title; labels = $Products; base = @{ ref = 'master' }; head = @{ sha = $sha } }
        [void](Invoke-Git -Arguments @('push', 'origin', 'master'))
    }
    $scripts = $PSScriptRoot
    Add-Feature @('product:core') 'Fix: Preserve $$ and $& in templates'
    & "$scripts/Update-Changelogs.ps1"
    Add-Feature @('product:metadata') 'Add: Tool feature'
    Add-Feature @('product:core', 'product:metadata') 'Change: Shared behavior'
    $master = Invoke-Git -Arguments @('rev-parse', 'origin/master')
    & "$scripts/Update-Changelogs.ps1"
    Assert ((Invoke-Git -Arguments @('rev-parse', 'origin/master')) -eq $master) 'The writer must not push master'
    Assert (@($global:TestWrites | Where-Object { $_ -eq 'create' }).Count -eq 1) 'Multiple merges must update one PR'
    $branch = $config.changelogBranch
    [void](Invoke-Git -Arguments @('fetch', 'origin', $branch))
    [void](Invoke-Git -Arguments @('checkout', '--force', '-B', 'master', 'origin/master'))
    [void](Invoke-Git -Arguments @('merge', '--no-ff', "origin/$branch", '-m', 'Merge notes PR'))
    $global:TestBotPr.state = 'closed'
    $global:TestPrs += @{ number = 100; merged_at = '2026-11-06'; merge_commit_sha = (Invoke-Git -Arguments @('rev-parse', 'HEAD')); title = 'Chore: Update notes'; labels = @('product:none'); base = @{ ref = 'master' } }
    [void](Invoke-Git -Arguments @('push', 'origin', 'master'))
    & "$scripts/Update-Changelogs.ps1"
    Assert ($global:TestWrites.Count -eq 2) 'Merging notes must not create a follow-up PR'
    foreach ($product in @('core', 'metadata')) {
        [void](Invoke-Git -Arguments @('checkout', '--force', '-B', 'master', 'origin/master'))
        [void](Invoke-Git -Arguments @('reset', '--hard', 'origin/master'))
        $version = if ($product -eq 'core') { '1.20.3' } else { '1.2.1' }
        & "$scripts/Prepare-Release.ps1" -Tag "$product-v$version" -Preview
        $metadata = Get-Content '.release/release.json' -Raw | ConvertFrom-Json
        Assert ($metadata.product -eq $product) 'Independent product selection'
        if ($product -eq 'core') {
            Assert ((Get-Content '.release/notes.md' -Raw).Contains('Preserve $$')) 'Saved notes preserve dollars'
            Assert ((Get-Content $config.products.core.changelog -Raw).Contains((Get-Content '.release/notes.md' -Raw).Trim())) 'Packaged history exactly includes saved release section'
        }
    }
    # A newer release of the other product must not invalidate an older core source.
    [void](Invoke-Git -Arguments @('checkout', '--force', '-B', 'master', 'origin/master'))
    [void](Invoke-Git -Arguments @('reset', '--hard', 'origin/master'))
    $earlier = Invoke-Git -Arguments @('rev-parse', 'HEAD')
    Add-Feature @('product:metadata') 'Add: Later metadata change'
    [void](Invoke-Git -Arguments @('tag', 'metadata-v1.2.1'))
    $global:TestReleases += @{ tag_name = 'metadata-v1.2.1'; draft = $false; body = '* Published'; published_at = '2026-11-06T12:00:00Z' }
    [void](Invoke-Git -Arguments @('checkout', '--force', '--detach', $earlier))
    & "$scripts/Prepare-Release.ps1" -Tag 'core-v1.20.3' -Preview
    # Released-only invalid titles are ignored; the pending core note remains valid.
    $global:TestPrs[1].title = 'Old metadata title without category'
    [void](Invoke-Git -Arguments @('reset', '--hard', $earlier))
    & "$scripts/Prepare-Release.ps1" -Tag 'core-v1.20.3' -Preview
    Assert (@(Get-PendingChanges $env:GITHUB_REPOSITORY 'origin/master' $global:TestReleases).Count -eq 2) 'Two-product collection ignores released invalid metadata titles'
    [void](Invoke-Git -Arguments @('reset', '--hard', $earlier))
    & "$scripts/Prepare-Release.ps1" -Tag 'core-v1.20.3' -Preview
    Reject { & "$scripts/Prepare-Release.ps1" -Tag 'core-v1.20.2' -Preview }
    [void](Invoke-Git -Arguments @('reset', '--hard', $earlier))
    $notesPath = Join-Path $PWD $config.products.core.changelog
    [IO.File]::WriteAllText($notesPath, (Get-Content $notesPath -Raw).Replace('Preserve $$', 'Incorrect note'))
    Reject { & "$scripts/Prepare-Release.ps1" -Tag 'core-v1.20.3' -Preview }
    [void](Invoke-Git -Arguments @('reset', '--hard', $earlier))
    [void](Invoke-Git -Arguments @('checkout', '-b', 'unmerged-feature'))
    'Unmerged' | Set-Content 'unmerged.txt'
    [void](Invoke-Git -Arguments @('add', '.')); [void](Invoke-Git -Arguments @('commit', '-m', 'Unmerged'))
    Reject { & "$scripts/Prepare-Release.ps1" -Tag 'core-v1.20.3' -Preview }
    [void](Invoke-Git -Arguments @('checkout', '--detach', $earlier))
    [void](Invoke-Git -Arguments @('tag', 'core-v1.20.3'))
    & "$scripts/Prepare-Release.ps1" -Tag 'core-v1.20.3'
    Assert ((Get-Content $env:GITHUB_OUTPUT -Raw).Contains('publish=true')) 'Tag preparation enables publication'
    Write-Output 'PASS: selected-product boundaries, released-note filtering, tag preparation and rejection of stale/unmerged/old-version sources'
    & "$scripts/Check-Pr.ps1" -PrNumber 1
    Assert ($global:TestChecks.Count -eq 2) 'Both required checks are reported'
    Assert (@($global:TestChecks | Where-Object { $_.conclusion -ne 'success' }).Count -eq 0) 'Valid PR checks succeed'
    $global:TestPrs[0].labels = @()
    Reject { & "$scripts/Check-Pr.ps1" -PrNumber 1 }
    Assert ($global:TestChecks[2].conclusion -eq 'failure' -and $global:TestChecks[3].conclusion -eq 'success') 'Invalid product labeling fails without hiding the category result'
    Write-Output 'PASS: single PR aggregation, no master push, no recursive PR, product previews and checks'
} finally {
    Remove-Item Function:global:gh -ErrorAction SilentlyContinue
    Set-Location $old; $env:GITHUB_REPOSITORY = $oldRepository; $env:GITHUB_OUTPUT = $oldOutput
    Remove-Item $root -Recurse -Force
    Remove-Item $remote -Recurse -Force -ErrorAction SilentlyContinue
}

# Exercise the actual validator in a separate process: a printed exception alone
# is not sufficient; invalid artifacts must return a failing exit code.
$root = Join-Path ([IO.Path]::GetTempPath()) ('package-test-' + [guid]::NewGuid())
$old = $PWD; $oldTag = $env:RELEASE_TAG
$pwsh = (Get-Process -Id $PID).Path
$validator = Join-Path $PSScriptRoot 'validate-packages.ps1'
function Write-TestPackage([string]$Path, [string]$Id, [string]$Version) {
    if (Test-Path $Path) { Remove-Item $Path }
    $zip = [IO.Compression.ZipFile]::Open($Path, [IO.Compression.ZipArchiveMode]::Create)
    try {
        $writer = [IO.StreamWriter]::new($zip.CreateEntry("$Id.nuspec").Open())
        try { $writer.Write("<package><metadata><id>$Id</id><version>$Version</version></metadata></package>") }
        finally { $writer.Dispose() }
    } finally { $zip.Dispose() }
}
function Assert-PackageValidation([bool]$Valid) {
    $output = & $pwsh -NoProfile -File $validator 2>&1
    $exit = $LASTEXITCODE
    Assert (($exit -eq 0) -eq $Valid) "Unexpected package validation exit $exit (expected valid=$Valid): $output"
}
try {
    New-Item (Join-Path $root '.release/packages') -ItemType Directory -Force | Out-Null
    Set-Location $root; $env:RELEASE_TAG = $null
    foreach ($product in @('core', 'metadata')) {
        Remove-Item '.release/packages/*' -Force -ErrorAction SilentlyContinue
        $id = $config.products[$product].packageId; $version = '2.0.0-rc.1'
        @{ product = $product; tag = "$product-v$version"; version = $version } | ConvertTo-Json | Set-Content '.release/release.json'
        $package = Join-Path $PWD ".release/packages/$id.$version.nupkg"
        $symbols = Join-Path $PWD ".release/packages/$id.$version.snupkg"
        Write-TestPackage $package $id $version; Write-TestPackage $symbols $id $version
        Assert-PackageValidation $true
        Write-TestPackage $package $id '9.9.9'; Assert-PackageValidation $false
        Write-TestPackage $package $id $version
        Write-TestPackage $symbols 'Wrong.Product' $version; Assert-PackageValidation $false
        Write-TestPackage $symbols $id $version
        'Unexpected' | Set-Content '.release/packages/other.nupkg'; Assert-PackageValidation $false
        Remove-Item '.release/packages/other.nupkg'
        Remove-Item $symbols; Assert-PackageValidation $false
    }
    Write-Output 'PASS: both product package/symbol pairs; wrong metadata, extra packages and missing symbols fail the process'
} finally {
    Set-Location $old; $env:RELEASE_TAG = $oldTag
    Remove-Item $root -Recurse -Force
}

# All assertions and cleanup succeeded. GitHub's pwsh wrapper returns
# LASTEXITCODE, so do not leak a deliberately failing validator's exit code.
# This must stay after the tests, not in finally, so real failures still fail.
$global:LASTEXITCODE = 0
