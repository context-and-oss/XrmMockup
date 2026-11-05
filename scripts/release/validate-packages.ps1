$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$release = Get-Content '.release/release.json' -Raw | ConvertFrom-Json
if ($release.tag -cne "$($release.product)-v$($release.version)") { throw 'Release version differs from the tag' }
$id = if ($release.product -eq 'core') { 'XrmMockup365' } elseif ($release.product -eq 'metadata') { 'XrmMockup.MetadataGenerator' } else { throw 'Unknown product' }
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
