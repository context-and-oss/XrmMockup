# Shared policy and text processing. gh/dotnet remain the operation implementations.
$script:Config = Get-Content (Join-Path $PSScriptRoot 'config.json') -Raw | ConvertFrom-Json -AsHashtable
$script:Begin = '<!-- BEGIN GENERATED RELEASE NOTES -->'
$script:End = '<!-- END GENERATED RELEASE NOTES -->'
function Get-ReleaseConfig { return $script:Config }
function Invoke-Gh {
    param([string[]]$Arguments, [string]$InputText)
    $output = if ($PSBoundParameters.ContainsKey('InputText')) { $InputText | & gh @Arguments } else { & gh @Arguments }
    if ($LASTEXITCODE -ne 0) { throw "gh $($Arguments[0]) failed (exit $LASTEXITCODE)" }
    return $output
}
function Get-GhJson {
    param([string]$Path)
    $text = Invoke-Gh -Arguments @('api', $Path)
    if ($text) { return ($text -join "`n" | ConvertFrom-Json -AsHashtable) }
}
function Get-GhList {
    param([string]$Path)
    $separator = if ($Path.Contains('?')) { '&' } else { '?' }
    foreach ($line in @(Invoke-Gh -Arguments @('api', "$Path${separator}per_page=100", '--paginate', '--jq', '.[] | @json'))) {
        if ($line) { $line | ConvertFrom-Json -AsHashtable }
    }
}
function Invoke-Git {
    param([string[]]$Arguments)
    $output = & git @Arguments
    if ($LASTEXITCODE -ne 0) { throw "git $($Arguments[0]) failed (exit $LASTEXITCODE)" }
    return ($output -join "`n").Trim()
}
function Test-Ancestor {
    param([string]$Commit, [string]$Source)
    & git merge-base --is-ancestor $Commit $Source
    if ($LASTEXITCODE -gt 1) { throw 'Cannot verify Git ancestry' }
    return $LASTEXITCODE -eq 0
}
function Get-PrProducts {
    param($Pr)
    $labels = @($Pr.labels | ForEach-Object { if ($_ -is [string]) { $_ } else { $_.name } })
    $selected = @($script:Config.products.Keys | Where-Object { "product:$_" -in $labels })
    if ('product:none' -in $labels) {
        if ($selected.Count) { throw 'product:none is exclusive' }
        return
    }
    if (!$selected.Count) { throw 'Choose product:core, product:metadata, both, or product:none' }
    return $selected
}
function Get-PrNote {
    param($Pr)
    $labels = @($Pr.labels | ForEach-Object { if ($_ -is [string]) { $_ } else { $_.name } })
    $categories = @($script:Config.categories.Keys | Where-Object { $_ -in $labels })
    if ($categories.Count -gt 1) { throw 'Choose one category label' }
    $title = $Pr.title.Trim()
    $match = [regex]::Match($title, '^(Add|Fix|Change|Breaking|Chore):\s*(.+)$', 'IgnoreCase')
    $prefix = if ($categories.Count) { $script:Config.categories[$categories[0]] } elseif ($match.Success) { $match.Groups[1].Value } else { throw 'A category label or title prefix is required' }
    if ($match.Success -and $match.Groups[1].Value -ine $prefix) { throw 'Category label and title prefix conflict' }
    $description = if ($match.Success) { $match.Groups[2].Value.Trim() } else { $title }
    if (!$description -or $title -match '[\r\n]' -or $title -match '^(Add|Fix|Change|Breaking|Chore):\s*$') { throw 'Use a descriptive single-line title' }
    return @{ category = $script:Config.categories.Values | Where-Object { $_ -ieq $prefix } | Select-Object -First 1; description = $description }
}
function Get-ReleaseTag {
    param([string]$Tag)
    if ($Tag -cnotmatch '^(core|metadata)-v(.+)$') { throw 'Use core-vVERSION or metadata-vVERSION' }
    $product = $Matches[1]; $version = $Matches[2]
    if ($version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-([\da-zA-Z-]+(?:\.[\da-zA-Z-]+)*))?$' -or $version -match '-v\d+\.\d+\.\d+') { throw 'Invalid release version' }
    [void][semver]$version
    if (($version -split '-', 2)[-1] -split '\.' | Where-Object { $_ -match '^0\d+$' }) { throw 'Numeric prerelease identifiers cannot have leading zeros' }
    return @{ product = $product; version = $version; tag = $Tag; prerelease = $version.Contains('-') }
}
function Compare-Version {
    param([string]$Left, [string]$Right)
    return ([semver]$Left.ToLowerInvariant()).CompareTo([semver]$Right.ToLowerInvariant())
}
function Get-ProductReleases {
    param([object[]]$Releases, [string]$Product)
    return $Releases | Where-Object { !$_.draft -and $_.tag_name.StartsWith("$Product-v") } |
        Sort-Object { [semver](Get-ReleaseTag $_.tag_name).version.ToLowerInvariant() } -Descending
}
function Get-PendingChanges {
    param([string]$Repository, [string]$Source, [object[]]$Releases, [string]$Product)
    $selected = if ($Product) { @($Product) } else { @($script:Config.products.Keys) }
    $refs = @(); $ranges = @{}
    foreach ($name in $selected) {
        $latest = @(Get-ProductReleases $Releases $name)
        $ref = if ($latest.Count) { $latest[0].tag_name } else { $script:Config.historyBaseline }
        if (!(Test-Ancestor $ref $Source)) { throw 'Source predates this product release' }
        $refs += $ref
        $ranges[$name] = @(Invoke-Git -Arguments @('rev-list', "$ref..$Source")) -split "`n"
    }
    $start = if ($refs.Count -eq 1) { $refs[0] } else { Invoke-Git -Arguments (@('merge-base') + $refs) }
    $seen = @{}
    foreach ($sha in ((Invoke-Git -Arguments @('rev-list', '--reverse', '--first-parent', "$start..$Source")) -split "`n")) {
        if (!$sha) { continue }
        foreach ($pr in @(Get-GhList "/repos/$Repository/commits/$sha/pulls")) {
            if (!$pr.merged_at -or !$pr.merge_commit_sha -or $pr.base.ref -ne 'master' -or $seen.ContainsKey($pr.number) -or !(Test-Ancestor $pr.merge_commit_sha $Source)) { continue }
            $seen[$pr.number] = $true
            # Associated PRs may already be released for every selected product.
            # Check commit membership before requiring modern classification labels.
            if (!@($selected | Where-Object { $pr.merge_commit_sha -in $ranges[$_] }).Count) { continue }
            $pending = @(Get-PrProducts $pr | Where-Object { $ranges.ContainsKey($_) -and $pr.merge_commit_sha -in $ranges[$_] })
            if (!$pending.Count) { continue }
            $note = Get-PrNote $pr
            @{ number = $pr.number; products = $pending; category = $note.category; description = $note.description }
        }
    }
}
function Get-NoteText {
    param([object[]]$Entries, [string]$Product)
    $lines = foreach ($prefix in @('Add', 'Fix', 'Change', 'Breaking', 'Chore')) {
        foreach ($entry in $Entries | Where-Object { $Product -in $_.products -and $_.category -eq $prefix }) {
            "* ${prefix}: $($entry.description.Replace('&', '&amp;').Replace('<', '&lt;').Replace('>', '&gt;')) (#$($entry.number))"
        }
    }
    return $lines -join "`n"
}
function Format-ReleaseDate {
    param([DateTime]$Date)
    return $Date.ToString('d MMMM yyyy', [Globalization.CultureInfo]::InvariantCulture)
}
function Get-PackageReleaseNotes {
    param([string]$Changelog, [string]$Product, [string]$Repository)
    # Select complete versioned sections, excluding preambles, Unreleased and generator markers.
    $text = $Changelog.Replace("`r`n", "`n").Replace($script:Begin, '').Replace($script:End, '')
    $sections = @([regex]::Matches($text, '^### v?\d[^\n]*(?:\n(?!### )[^\n]*)*', 'Multiline') |
        Select-Object -First 10 | ForEach-Object { $_.Value.TrimEnd() })
    if (!$sections.Count) { throw 'No released sections in the changelog' }
    if (!$Repository) { throw 'Repository is required for the full release history link' }
    $path = $script:Config.products[$Product].changelog
    return ($sections -join "`n`n") + "`n`nFull release history: https://github.com/$Repository/blob/master/$path"
}
function Get-Changelog {
    param([string]$Original, [object[]]$Entries, [string]$Product, [object[]]$Releases = @())
    $archive = $Original.Replace("`r`n", "`n")
    $start = $archive.IndexOf($script:Begin); $finish = $archive.IndexOf($script:End)
    if (($start -lt 0) -ne ($finish -lt 0) -or ($start -ge 0 -and $finish -lt $start)) { throw 'Damaged changelog markers' }
    if ($start -ge 0) {
        $block = $archive.Substring($start + $script:Begin.Length, $finish - $start - $script:Begin.Length)
        $versions = [regex]::Match($block, '^### v?\d', 'Multiline')
        $history = if ($versions.Success) { $block.Substring($versions.Index).Trim() + "`n`n" } else { '' }
        $archive = $archive.Substring(0, $start) + $history + $archive.Substring($finish + $script:End.Length).TrimStart("`n")
    }
    $first = [regex]::Match($archive, '^### v?\d', 'Multiline')
    $preamble = if ($first.Success) { $archive.Substring(0, $first.Index).TrimEnd() } else { $archive.TrimEnd() }
    $history = if ($first.Success) { $archive.Substring($first.Index).TrimEnd() } else { '' }
    $recorded = @([regex]::Matches($history, '^### v?([^\s]+)', 'Multiline') | ForEach-Object { $_.Groups[1].Value.ToLowerInvariant() })
    $notes = Get-NoteText $Entries $Product
    if (!$notes) { $notes = '* No unreleased changes.' }
    $sections = @($preamble, "$script:Begin`n### Unreleased`n$notes`n$script:End")
    foreach ($release in $Releases) {
        $version = (Get-ReleaseTag $release.tag_name).version
        if ($version.ToLowerInvariant() -in $recorded) { continue }
        $body = $release.body.Trim()
        if ($body.StartsWith("### $version -")) {
            # Releases published with the former ISO formatter still use the display date format.
            $heading = [regex]::Match($body, '^### [^\n]+ - (\d{4}-\d{2}-\d{2})(?=\r?$)', 'Multiline')
            if ($heading.Success) {
                $date = [DateTime]::ParseExact($heading.Groups[1].Value, 'yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture)
                $body = $body.Remove($heading.Index, $heading.Length).Insert($heading.Index, "### $version - $(Format-ReleaseDate $date)")
            }
            $sections += $body
        } else {
            $sections += "### $version - $(Format-ReleaseDate ([DateTimeOffset]$release.published_at).UtcDateTime)`n$body"
        }
    }
    $sections += $history
    return (($sections | Where-Object { $_ }) -join "`n`n") + "`n"
}
Export-ModuleMember -Function *
