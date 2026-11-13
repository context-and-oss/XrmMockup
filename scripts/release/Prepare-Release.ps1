param([Parameter(Mandatory)][string]$Tag, [switch]$Preview)
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Release.psm1')
$config = Get-ReleaseConfig; $release = Get-ReleaseTag $Tag
$source = Invoke-Git -Arguments @('rev-parse', 'HEAD')
if (!(Test-Ancestor $source 'origin/master')) { throw 'Release source must be on master ancestry' }
if (!$Preview -and (Invoke-Git -Arguments @('rev-parse', "refs/tags/$Tag^{commit}")) -cne $source) { throw 'Tag differs from selected source' }
$releases = @(Get-GhList "/repos/$env:GITHUB_REPOSITORY/releases")
$previous = @(Get-ProductReleases $releases $release.product)
$latest = if ($previous.Count) { (Get-ReleaseTag $previous[0].tag_name).version } else { $config.products[$release.product].baselineVersion }
if ((Compare-Version $release.version $latest) -le 0) { throw 'Choose a version newer than the last successful product release' }
$entries = @(Get-PendingChanges $env:GITHUB_REPOSITORY $source $releases $release.product)
$product = $config.products[$release.product]
$original = (Get-Content $product.changelog -Raw).Replace("`r`n", "`n")
if ((Get-Changelog $original $entries $release.product $previous) -cne $original) { throw 'Wait for Update product changelogs to commit notes covering this source to master before tagging' }
$notes = Get-NoteText $entries $release.product
if (!$notes) { throw 'No pending changes for the selected product' }
$section = "### $($release.version) - $([DateTime]::UtcNow.ToString('yyyy-MM-dd'))`n$notes"
New-Item '.release/packages' -ItemType Directory -Force | Out-Null
$release.sourceSha = $source
$release | ConvertTo-Json | Set-Content '.release/release.json' -Encoding utf8NoBOM
Set-Content '.release/notes.md' $section -Encoding utf8NoBOM
$path = [Security.SecurityElement]::Escape((Join-Path $PWD '.release/notes.md'))
$props = "<Project><PropertyGroup><$($product.versionProperty)>$($release.version)</$($product.versionProperty)><$($product.notesProperty)>$path</$($product.notesProperty)></PropertyGroup></Project>"
Set-Content '.release/version.props' $props -Encoding utf8NoBOM
[IO.File]::WriteAllText((Join-Path $PWD $product.changelog), $original.Replace("### Unreleased`n$notes", $section))
if ($env:GITHUB_OUTPUT) { "tag=$Tag`nproject=$($product.project)`npublish=$((!$Preview).ToString().ToLowerInvariant())" | Add-Content $env:GITHUB_OUTPUT }
