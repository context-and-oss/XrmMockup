$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Release.psm1')
Add-Type -AssemblyName System.IO.Compression.FileSystem
$release = Get-Content '.release/release.json' -Raw | ConvertFrom-Json
if ($release.tag -cne "$($release.product)-v$($release.version)") { throw 'Release version differs from the tag' }
$config = Get-Content (Join-Path $PSScriptRoot 'config.json') -Raw | ConvertFrom-Json -AsHashtable
$id = if ($release.product -eq 'core') { 'XrmMockup365' } elseif ($release.product -eq 'metadata') { 'XrmMockup.MetadataGenerator' } else { throw 'Unknown product' }
$changelogName = Split-Path $config.products[$release.product].changelog -Leaf
$notes = (Get-Content '.release/notes.md' -Raw).Replace("`r`n", "`n").TrimEnd()
$packageHistoryNotes = (Get-Content '.release/package-notes.md' -Raw).Replace("`r`n", "`n").TrimEnd()
$heading = ($notes -split "`n", 2)[0]
if ($heading -cnotmatch ("^### " + [regex]::Escape($release.version) + ' - \d{1,2} (January|February|March|April|May|June|July|August|September|October|November|December) \d{4}$')) { throw 'Release notes version differs from the tag' }
$files = @(Get-ChildItem '.release/packages' -File | Sort-Object Name)
$expected = @("$id.$($release.version).nupkg", "$id.$($release.version).snupkg")
if ($files.Count -ne 2 -or $files[0].Name -cne $expected[0] -or $files[1].Name -cne $expected[1]) { throw 'Expected only the selected package and matching symbols' }
foreach ($file in $files) {
    $archive = [IO.Compression.ZipFile]::OpenRead($file.FullName)
    try {
        $spec = @($archive.Entries | Where-Object { $_.FullName -match '^[^/]+\.nuspec$' })
        if ($spec.Count -ne 1) { throw 'Expected one nuspec' }
        $reader = [IO.StreamReader]::new($spec[0].Open())
        try { [xml]$xml = $reader.ReadToEnd() } finally { $reader.Dispose() }
        if ($xml.package.metadata.id -cne $id -or $xml.package.metadata.version -cne $release.version) { throw 'Package ID/version mismatch' }
        $packageNotes = ([string]$xml.package.metadata.releaseNotes).Replace("`r`n", "`n").TrimEnd()
        if ($packageNotes -cne $packageHistoryNotes) { throw 'Package release notes differ from the saved release notes' }
        if ($file.Extension -eq '.nupkg') {
            $changelog = @($archive.Entries | Where-Object { $_.FullName -ceq $changelogName })
            if ($changelog.Count -ne 1) { throw "Expected $changelogName at the package root" }
            $reader = [IO.StreamReader]::new($changelog[0].Open())
            try { $history = $reader.ReadToEnd().Replace("`r`n", "`n") } finally { $reader.Dispose() }
            # The newest section must describe this release, not Unreleased or an older version.
            $firstHeading = [regex]::Match($history, '^### [^\n]+', 'Multiline')
            if (!$firstHeading.Success -or $firstHeading.Value -cne $heading -or !$history.Substring($firstHeading.Index).StartsWith("$notes`n", [StringComparison]::Ordinal)) {
                throw 'Packaged changelog does not start with the saved release section'
            }
            if ((Get-PackageReleaseNotes $history $release.product $env:GITHUB_REPOSITORY) -cne $packageHistoryNotes) {
                throw 'Package release notes differ from the latest ten changelog releases and history link'
            }
        }
    } finally { $archive.Dispose() }
}
# Publishing jobs set RELEASE_TAG; previews only validate the package itself.
if ($env:RELEASE_TAG) {
    if ($release.tag -cne $env:RELEASE_TAG) { throw 'Artifact tag differs from selected release' }
    $sha = git rev-parse "refs/tags/$($release.tag)^{commit}"
    if ($LASTEXITCODE -ne 0 -or $sha -cne $release.sourceSha) { throw 'Tag changed since the build' }
    git merge-base --is-ancestor $release.sourceSha origin/master
    if ($LASTEXITCODE -ne 0) { throw 'Release source is not on master ancestry' }
}
