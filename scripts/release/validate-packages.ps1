param([switch]$Existing)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$snapshot = Get-Content '.release/release-snapshot.json' -Raw | ConvertFrom-Json
$packageId = if ($snapshot.product -eq 'core') { 'XrmMockup365' } elseif ($snapshot.product -eq 'metadata') { 'XrmMockup.MetadataGenerator' } else { throw 'Unknown product' }

function Get-PackageContentHash([string]$Path, [bool]$RequireLegacy = $false) {
    $archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path $Path))
    try {
        $parts = [Collections.Generic.List[string]]::new()
        $nuspecs = @($archive.Entries | Where-Object { $_.FullName -match '^[^/]+\.nuspec$' })
        if ($nuspecs.Count -ne 1) { throw "Expected one root nuspec in $Path" }
        $reader = [IO.StreamReader]::new($nuspecs[0].Open())
        try { [xml]$nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
        if ($nuspec.package.metadata.id -cne $packageId -or $nuspec.package.metadata.version -cne $snapshot.version) { throw "Package ID/version mismatch in $Path" }
        [string[]]$names = @($archive.Entries | ForEach-Object { $_.FullName })
        if (@($names | Select-Object -Unique).Count -ne $names.Count) { throw 'Duplicate entries in package' }
        [Array]::Sort($names, [StringComparer]::Ordinal)
        foreach ($name in $names) {
            $entry = $archive.GetEntry($name)
            if ($entry.FullName.EndsWith('/') -or $entry.FullName -eq '.signature.p7s') { continue }
            # NuGet adds repository signatures. Compare unpacked content excluding that signature.
            $stream = $entry.Open()
            $hasher = [Security.Cryptography.SHA512]::Create()
            try { $digest = [Convert]::ToHexString($hasher.ComputeHash($stream)).ToLowerInvariant() } finally { $stream.Dispose(); $hasher.Dispose() }
            $parts.Add("$($entry.FullName)`0$digest")
        }
        if ($RequireLegacy) {
            foreach ($target in @('net462', 'net10.0')) {
                foreach ($asset in @('MetadataGenerator365.exe', 'MetadataGenerator365.exe.config', 'TypeDeclarations.cs')) {
                    if (-not ($archive.Entries.FullName -contains "content/$target/Metadata/$asset")) { throw "Missing legacy generator asset $asset for $target" }
                }
            }
        }
        $hasher = [Security.Cryptography.SHA512]::Create()
        try { return [Convert]::ToHexString($hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes(($parts -join "`n")))).ToLowerInvariant() } finally { $hasher.Dispose() }
    } finally { $archive.Dispose() }
}

if ($Existing) {
    $expected = @($snapshot.packages | Where-Object { $_.name.EndsWith('.nupkg') })
    if ($expected.Count -ne 1 -or (Get-PackageContentHash '.release/existing.nupkg' ($snapshot.product -eq 'core')) -cne $expected[0].contentHash) { throw 'Existing NuGet package content differs from the frozen artifact. Investigate the version collision.' }
} else {
    $files = @(Get-ChildItem '.release/packages' -File)
    if ($files.Count -ne 2) { throw 'Expected only the selected nupkg and snupkg' }
    $content = [ordered]@{}
    foreach ($file in $files) {
        $requireLegacy = $snapshot.product -eq 'core' -and $file.Extension -eq '.nupkg'
        $content[$file.Name] = Get-PackageContentHash $file.FullName $requireLegacy
    }
    $content | ConvertTo-Json | Set-Content '.release/package-content.json' -Encoding utf8
}
