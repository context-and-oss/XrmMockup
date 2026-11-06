param([int]$PrNumber)
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Release.psm1')
if (!$PrNumber) { $event = Get-Content $env:GITHUB_EVENT_PATH -Raw | ConvertFrom-Json; $PrNumber = $event.pull_request.number }
if ($PrNumber -le 0) { throw 'A PR number is required' }
$pr = Get-GhJson "/repos/$env:GITHUB_REPOSITORY/pulls/$PrNumber"
$failed = $false
foreach ($check in @('PR product labels', 'PR change category')) {
    $failure = $null
    try {
        if ($check -eq 'PR product labels') { [void]@(Get-PrProducts $pr) } else { [void](Get-PrNote $pr) }
    } catch { $failure = $_.Exception.Message; $failed = $true }
    $body = @{ name = $check; head_sha = $pr.head.sha; status = 'completed'; conclusion = if ($failure) { 'failure' } else { 'success' }
        output = @{ title = $check; summary = if ($failure) { $failure } else { "Valid release classification for PR #$PrNumber" } } } | ConvertTo-Json -Depth 8 -Compress
    [void](Invoke-Gh -Arguments @('api', "/repos/$env:GITHUB_REPOSITORY/check-runs", '--method', 'POST', '--input', '-') -InputText $body)
}
if ($failed) { throw 'PR release classification failed' }
