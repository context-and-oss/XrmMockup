param([int]$PrNumber, [string]$ExpectedHeadSha)
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Release.psm1')
if (!$PrNumber) { $event = Get-Content $env:GITHUB_EVENT_PATH -Raw | ConvertFrom-Json; $PrNumber = $event.pull_request.number }
if ($PrNumber -le 0) { throw 'A PR number is required' }
$pr = Get-GhJson "/repos/$env:GITHUB_REPOSITORY/pulls/$PrNumber"
# A dispatched native check belongs to the selected ref, not an arbitrary PR.
if ($ExpectedHeadSha -and $pr.head.sha -ne $ExpectedHeadSha) { throw 'Dispatch this workflow on the PR head branch so the check is attached to the correct commit' }
$failures = @()
foreach ($check in @('Product labels', 'Change category')) {
    try {
        if ($check -eq 'Product labels') { [void]@(Get-PrProducts $pr) } else { [void](Get-PrNote $pr) }
        Write-Output "$check`: valid"
    } catch { $failures += "$check`: $($_.Exception.Message)" }
}
if ($failures.Count) { throw ($failures -join "; ") }
Write-Output "Valid release classification for PR #$PrNumber"
