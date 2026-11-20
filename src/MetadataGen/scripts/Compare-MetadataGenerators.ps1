# Compares metadata output between the old MetadataGenerator365.exe and the new xrmmockup-metadata tool
# Usage: .\Compare-MetadataGenerators.ps1 -Username "user@domain.com" [-Solutions "MySolution"] [-NoBuild]
#
# Prerequisites:
# - For old generator: Username will prompt for ADAL login
# - For new generator: Run 'az login' first (uses DefaultAzureCredential)

param(
    [Parameter(Mandatory=$true)]
    [string]$Username,

    [string]$Solutions = "XrmBedrock",

    [string]$DataverseUrl = "https://org507f744f.crm4.dynamics.com",

    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir

# Default entities that the new tool always includes (from GeneratorOptions.DefaultEntities)
# These must be passed to the old generator to ensure both tools fetch the same entities
$defaultEntities = @(
    "businessunit",
    "systemuser",
    "transactioncurrency",
    "role",
    "systemuserroles",
    "team",
    "teamroles",
    "activitypointer",
    "roletemplate",
    "fileattachment",
    "workflow"
)

# Create temp directories for output and config
$tempBase = Join-Path $env:TEMP "MetadataCompare_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
$oldOutputDir = Join-Path $tempBase "Old"
$newOutputDir = Join-Path $tempBase "New"
$configDir = Join-Path $tempBase "Config"

New-Item -ItemType Directory -Path $oldOutputDir -Force | Out-Null
New-Item -ItemType Directory -Path $newOutputDir -Force | Out-Null
New-Item -ItemType Directory -Path $configDir -Force | Out-Null

Write-Host "Dataverse URL: $DataverseUrl" -ForegroundColor Cyan
Write-Host "Solutions: $Solutions" -ForegroundColor Cyan
Write-Host "Default entities: $($defaultEntities -join ', ')" -ForegroundColor Cyan
Write-Host "Temp directory: $tempBase" -ForegroundColor Cyan
Write-Host "Old generator output: $oldOutputDir" -ForegroundColor Cyan
Write-Host "New generator output: $newOutputDir" -ForegroundColor Cyan
Write-Host ""

# Build paths
$oldExePath = Join-Path $repoRoot "MetadataGenerator365\bin\Release\net462\MetadataGenerator365.exe"
$newToolPath = Join-Path $repoRoot "MetadataGenerator.Tool\bin\Release\net10.0\XrmMockup.MetadataGenerator.Tool.dll"
$solutionPath = Join-Path $repoRoot "..\..\XrmMockup.slnx"

if ($NoBuild) {
    # Verify executables exist when skipping build
    if (-not (Test-Path $oldExePath)) {
        Write-Error "Old generator not found at: $oldExePath`nRun without -NoBuild to build first"
    }
    if (-not (Test-Path $newToolPath)) {
        Write-Error "New generator not found at: $newToolPath`nRun without -NoBuild to build first"
    }
} else {
    # Build the solution
    Write-Host "Building solution..." -ForegroundColor Yellow
    & dotnet build $solutionPath --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed with exit code $LASTEXITCODE"
    }
    Write-Host ""
}

# Create temporary appsettings.json for new tool
$appsettingsPath = Join-Path $configDir "appsettings.json"

# Build metadata config - the new tool automatically includes DefaultEntities
$metadataConfig = @{
    OutputDirectory = $newOutputDir
    Solutions = @($Solutions -split ',')
}

$appsettingsContent = @{
    DATAVERSE_URL = $DataverseUrl
    XrmMockup = @{
        Metadata = $metadataConfig
    }
} | ConvertTo-Json -Depth 4

Set-Content -Path $appsettingsPath -Value $appsettingsContent
Write-Host "Created temporary appsettings.json at: $appsettingsPath" -ForegroundColor Gray
Write-Host ""

# Run old generator
Write-Host "Running OLD MetadataGenerator365..." -ForegroundColor Yellow

# Microsoft's well-known sample app ID for Dataverse interactive authentication
$appId = "51f81489-12ee-4a9e-aaae-a2591f45987d"
$redirectUri = "app://58145B91-0C36-4500-8554-080854F2AC97"
$tokenCachePath = Join-Path $env:TEMP "XrmMockup_TokenCache.dat"

# Use ConnectionString method with interactive login
$connectionString = "AuthType=OAuth;Url=$DataverseUrl;AppId=$appId;RedirectUri=$redirectUri;TokenCacheStorePath=$tokenCachePath;LoginPrompt=Auto;Username=$Username"

# Pass default entities to old generator so both tools fetch the same entities
$entitiesArg = $defaultEntities -join ','

$oldArgs = @(
    "/out:$oldOutputDir",
    "/solutions:$Solutions",
    "/entities:$entitiesArg",
    "/method:ConnectionString",
    "/connectionString:$connectionString"
)

Write-Host "  Command: $oldExePath $($oldArgs -join ' ')" -ForegroundColor Gray
$oldStartTime = Get-Date
& $oldExePath @oldArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "Old generator failed with exit code $LASTEXITCODE"
}
$oldDuration = (Get-Date) - $oldStartTime
Write-Host "  Completed in $($oldDuration.TotalSeconds.ToString('F1')) seconds" -ForegroundColor Green
Write-Host ""

# Run new generator
Write-Host "Running NEW xrmmockup-metadata tool..." -ForegroundColor Yellow
$newArgs = @(
    $newToolPath,
    "--config", $appsettingsPath
)

Write-Host "  Command: dotnet $($newArgs -join ' ')" -ForegroundColor Gray
$newStartTime = Get-Date
# Run from the config directory to ensure only the temp appsettings.json is loaded
Push-Location $configDir
try {
    & dotnet @newArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "New generator failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}
$newDuration = (Get-Date) - $newStartTime
Write-Host "  Completed in $($newDuration.TotalSeconds.ToString('F1')) seconds" -ForegroundColor Green
Write-Host ""

# Compare outputs
Write-Host "Comparing outputs..." -ForegroundColor Yellow
Write-Host ""

function Get-XmlSemanticHash {
    <#
    .SYNOPSIS
    Computes a semantic hash of XML content, ignoring namespace prefix differences.

    .DESCRIPTION
    DataContractSerializer uses different prefix naming schemes depending on .NET version:
    - Old (.NET Framework): Sequential letters (a:, b:, c:)
    - New (.NET 10): Depth-based prefixes (d2p1:, d3p1:, d5p1:)

    This function extracts the semantic content (element local names, namespace URIs,
    attribute values, text content) and produces a hash that is identical regardless
    of prefix naming scheme.

    NOTE: This is slow for large files (>1MB) due to PowerShell's recursive overhead.
    For large files, use Get-XmlElementCount for a quick approximate comparison.
    #>
    param([string]$FilePath)

    try {
        $xml = [System.Xml.XmlDocument]::new()
        $xml.PreserveWhitespace = $false
        $xml.Load($FilePath)

        # Build a canonical string representation by walking the DOM
        $sb = [System.Text.StringBuilder]::new()

        function Write-XmlNode {
            param([System.Xml.XmlNode]$node, [System.Text.StringBuilder]$builder)

            switch ($node.NodeType) {
                ([System.Xml.XmlNodeType]::Element) {
                    # Write element with namespace URI instead of prefix
                    $builder.Append("<{$($node.NamespaceURI)}$($node.LocalName)") | Out-Null

                    # Sort attributes by namespace URI + local name (excluding xmlns declarations)
                    $attrs = @($node.Attributes | Where-Object {
                        -not ($_.Prefix -eq 'xmlns' -or $_.LocalName -eq 'xmlns')
                    } | Sort-Object { "$($_.NamespaceURI)|$($_.LocalName)" })

                    foreach ($attr in $attrs) {
                        # For i:type attributes, normalize the value (remove prefix, keep local name)
                        $attrValue = $attr.Value
                        if ($attr.LocalName -eq 'type' -and $attr.NamespaceURI -eq 'http://www.w3.org/2001/XMLSchema-instance') {
                            # The type value is in format "prefix:TypeName" - we need to resolve it to namespace
                            if ($attrValue -match '^([^:]+):(.+)$') {
                                $typePrefix = $Matches[1]
                                $typeName = $Matches[2]
                                # Resolve the prefix to namespace URI
                                $typeNs = $node.GetNamespaceOfPrefix($typePrefix)
                                $attrValue = "{$typeNs}$typeName"
                            }
                        }
                        $builder.Append(" {$($attr.NamespaceURI)}$($attr.LocalName)=`"$attrValue`"") | Out-Null
                    }

                    $builder.Append(">") | Out-Null

                    foreach ($child in $node.ChildNodes) {
                        Write-XmlNode -node $child -builder $builder
                    }

                    $builder.Append("</{$($node.NamespaceURI)}$($node.LocalName)>") | Out-Null
                }
                ([System.Xml.XmlNodeType]::Text) {
                    $builder.Append($node.Value.Trim()) | Out-Null
                }
                ([System.Xml.XmlNodeType]::CDATA) {
                    $builder.Append("<![CDATA[$($node.Value)]]>") | Out-Null
                }
                ([System.Xml.XmlNodeType]::Document) {
                    foreach ($child in $node.ChildNodes) {
                        Write-XmlNode -node $child -builder $builder
                    }
                }
            }
        }

        Write-XmlNode -node $xml -builder $sb

        # Return MD5 hash of the canonical representation
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($sb.ToString())
        $md5 = [System.Security.Cryptography.MD5]::Create()
        $hashBytes = $md5.ComputeHash($bytes)
        return [System.BitConverter]::ToString($hashBytes) -replace '-', ''
    }
    catch {
        # If XML parsing fails, return error
        return "ERROR: $($_.Exception.Message)"
    }
}

function Get-XmlElementCount {
    <#
    .SYNOPSIS
    Fast comparison using XmlReader - counts elements and their local names.

    .DESCRIPTION
    For large XML files, performs a quick structural comparison by counting
    element occurrences. This is much faster than full semantic hashing but
    may miss subtle differences in attribute values or text content.
    #>
    param([string]$FilePath)

    try {
        $settings = [System.Xml.XmlReaderSettings]::new()
        $settings.IgnoreWhitespace = $true
        $settings.IgnoreComments = $true
        $settings.IgnoreProcessingInstructions = $true

        $reader = [System.Xml.XmlReader]::Create($FilePath, $settings)
        $elementCounts = @{}
        $textHash = [System.Security.Cryptography.MD5]::Create()
        $textBytes = [System.Collections.Generic.List[byte]]::new()

        while ($reader.Read()) {
            switch ($reader.NodeType) {
                ([System.Xml.XmlNodeType]::Element) {
                    # Use namespace URI + local name as key (ignores prefix)
                    $key = "{$($reader.NamespaceURI)}$($reader.LocalName)"
                    if ($elementCounts.ContainsKey($key)) {
                        $elementCounts[$key]++
                    } else {
                        $elementCounts[$key] = 1
                    }
                }
                ([System.Xml.XmlNodeType]::Text) {
                    $bytes = [System.Text.Encoding]::UTF8.GetBytes($reader.Value.Trim())
                    $textBytes.AddRange($bytes)
                }
            }
        }
        $reader.Close()

        # Create summary string
        $sorted = $elementCounts.GetEnumerator() | Sort-Object Name | ForEach-Object { "$($_.Name)=$($_.Value)" }
        $summary = $sorted -join "|"

        # Hash the summary + text content
        $summaryBytes = [System.Text.Encoding]::UTF8.GetBytes($summary)
        $allBytes = [byte[]]::new($summaryBytes.Length + $textBytes.Count)
        [System.Array]::Copy($summaryBytes, $allBytes, $summaryBytes.Length)
        if ($textBytes.Count -gt 0) {
            $textBytes.CopyTo($allBytes, $summaryBytes.Length)
        }

        $hashBytes = $textHash.ComputeHash($allBytes)
        return [System.BitConverter]::ToString($hashBytes) -replace '-', ''
    }
    catch {
        return "ERROR: $($_.Exception.Message)"
    }
}

function Compare-MetadataXml {
    <#
    .SYNOPSIS
    Compares two Metadata.xml files entity by entity.

    .DESCRIPTION
    Extracts EntityMetadata from both files, compares them by LogicalName,
    and reports which entities differ, are missing, or are extra.
    Also compares DefaultStateStatus, accounting for the fact that the new
    generator only includes state/status for entities in the metadata.
    Returns a detailed comparison result.
    #>
    param(
        [string]$OldFile,
        [string]$NewFile
    )

    $result = @{
        IsIdentical = $true
        OldEntityCount = 0
        NewEntityCount = 0
        MissingInNew = @()
        ExtraInNew = @()
        Different = @()
        StateStatusInfo = ""
        Summary = ""
    }

    try {
        # Load both XML files
        $oldXml = [System.Xml.XmlDocument]::new()
        $oldXml.Load($OldFile)
        $newXml = [System.Xml.XmlDocument]::new()
        $newXml.Load($NewFile)

        # Create namespace managers
        $oldNs = [System.Xml.XmlNamespaceManager]::new($oldXml.NameTable)
        $newNs = [System.Xml.XmlNamespaceManager]::new($newXml.NameTable)

        # Find the EntityMetadata namespace prefix dynamically
        # The structure is: MetadataSkeleton/EntityMetadata/ArrayOfEntityMetadata/EntityMetadata
        $oldEntitiesParent = $oldXml.DocumentElement.ChildNodes | Where-Object { $_.LocalName -eq "EntityMetadata" } | Select-Object -First 1
        $newEntitiesParent = $newXml.DocumentElement.ChildNodes | Where-Object { $_.LocalName -eq "EntityMetadata" } | Select-Object -First 1

        if (-not $oldEntitiesParent -or -not $newEntitiesParent) {
            $result.Summary = "Could not find EntityMetadata element in one or both files"
            return $result
        }

        $oldArray = $oldEntitiesParent.ChildNodes | Where-Object { $_.LocalName -eq "ArrayOfEntityMetadata" } | Select-Object -First 1
        $newArray = $newEntitiesParent.ChildNodes | Where-Object { $_.LocalName -eq "ArrayOfEntityMetadata" } | Select-Object -First 1

        if (-not $oldArray -or -not $newArray) {
            $result.Summary = "Could not find ArrayOfEntityMetadata element in one or both files"
            return $result
        }

        # Extract entities by LogicalName
        $oldEntities = @{}
        $newEntities = @{}

        foreach ($entity in $oldArray.ChildNodes) {
            if ($entity.LocalName -eq "EntityMetadata") {
                $logicalName = ($entity.ChildNodes | Where-Object { $_.LocalName -eq "LogicalName" } | Select-Object -First 1).InnerText
                if ($logicalName) {
                    $oldEntities[$logicalName] = $entity.OuterXml
                }
            }
        }

        foreach ($entity in $newArray.ChildNodes) {
            if ($entity.LocalName -eq "EntityMetadata") {
                $logicalName = ($entity.ChildNodes | Where-Object { $_.LocalName -eq "LogicalName" } | Select-Object -First 1).InnerText
                if ($logicalName) {
                    $newEntities[$logicalName] = $entity.OuterXml
                }
            }
        }

        $result.OldEntityCount = $oldEntities.Count
        $result.NewEntityCount = $newEntities.Count

        # Find missing entities (in old but not in new)
        foreach ($name in $oldEntities.Keys) {
            if (-not $newEntities.ContainsKey($name)) {
                $result.MissingInNew += $name
                $result.IsIdentical = $false
            }
        }

        # Find extra entities (in new but not in old)
        foreach ($name in $newEntities.Keys) {
            if (-not $oldEntities.ContainsKey($name)) {
                $result.ExtraInNew += $name
                $result.IsIdentical = $false
            }
        }

        # Compare common entities using semantic hash
        $md5 = [System.Security.Cryptography.MD5]::Create()
        foreach ($name in $oldEntities.Keys) {
            if ($newEntities.ContainsKey($name)) {
                # Normalize XML by removing namespace prefixes for comparison
                $oldNormalized = $oldEntities[$name] -replace '\s+xmlns[^"]*"[^"]*"', '' -replace '<[a-z0-9]+:', '<' -replace '</[a-z0-9]+:', '</'
                $newNormalized = $newEntities[$name] -replace '\s+xmlns[^"]*"[^"]*"', '' -replace '<[a-z0-9]+:', '<' -replace '</[a-z0-9]+:', '</'

                $oldHash = [System.BitConverter]::ToString($md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($oldNormalized))) -replace '-', ''
                $newHash = [System.BitConverter]::ToString($md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($newNormalized))) -replace '-', ''

                if ($oldHash -ne $newHash) {
                    $result.Different += $name
                    $result.IsIdentical = $false
                }
            }
        }

        # Compare DefaultStateStatus - new generator only includes entities in metadata
        # So we only compare entries that exist in both, and report the count difference
        $oldStateStatus = $oldXml.DocumentElement.ChildNodes | Where-Object { $_.LocalName -eq "DefaultStateStatus" } | Select-Object -First 1
        $newStateStatus = $newXml.DocumentElement.ChildNodes | Where-Object { $_.LocalName -eq "DefaultStateStatus" } | Select-Object -First 1

        if ($oldStateStatus -and $newStateStatus) {
            # Count entries in each
            $oldStateEntries = @($oldStateStatus.SelectNodes(".//*[local-name()='KeyValueOfstringDictionaryOfintint']"))
            $newStateEntries = @($newStateStatus.SelectNodes(".//*[local-name()='KeyValueOfstringDictionaryOfintint']"))

            $oldStateCount = $oldStateEntries.Count
            $newStateCount = $newStateEntries.Count

            # Extract entity names from state/status entries
            $oldStateEntities = @{}
            $newStateEntities = @{}

            foreach ($entry in $oldStateEntries) {
                $key = ($entry.ChildNodes | Where-Object { $_.LocalName -eq "Key" } | Select-Object -First 1).InnerText
                if ($key) { $oldStateEntities[$key] = $entry.OuterXml }
            }

            foreach ($entry in $newStateEntries) {
                $key = ($entry.ChildNodes | Where-Object { $_.LocalName -eq "Key" } | Select-Object -First 1).InnerText
                if ($key) { $newStateEntities[$key] = $entry.OuterXml }
            }

            # Check that all new state/status entities exist in old (they should be a subset)
            $stateStatusDifferent = @()
            foreach ($name in $newStateEntities.Keys) {
                if ($oldStateEntities.ContainsKey($name)) {
                    # Normalize and compare
                    $oldNorm = $oldStateEntities[$name] -replace '\s+xmlns[^"]*"[^"]*"', '' -replace '<[a-z0-9]+:', '<' -replace '</[a-z0-9]+:', '</'
                    $newNorm = $newStateEntities[$name] -replace '\s+xmlns[^"]*"[^"]*"', '' -replace '<[a-z0-9]+:', '<' -replace '</[a-z0-9]+:', '</'
                    if ($oldNorm -ne $newNorm) {
                        $stateStatusDifferent += $name
                    }
                }
            }

            # Check for entities in new state/status that aren't in old (unexpected)
            $unexpectedStateStatus = @($newStateEntities.Keys | Where-Object { -not $oldStateEntities.ContainsKey($_) })

            $result.StateStatusInfo = "StateStatus: Old=$oldStateCount, New=$newStateCount (filtered to metadata entities)"

            if ($stateStatusDifferent.Count -gt 0) {
                $result.StateStatusInfo += "; Different: $($stateStatusDifferent -join ', ')"
                $result.IsIdentical = $false
            }

            if ($unexpectedStateStatus.Count -gt 0) {
                $result.StateStatusInfo += "; Unexpected in NEW: $($unexpectedStateStatus -join ', ')"
                $result.IsIdentical = $false
            }
        }

        # Build summary
        $summaryParts = @()
        $summaryParts += "Entities: Old=$($result.OldEntityCount), New=$($result.NewEntityCount)"
        if ($result.MissingInNew.Count -gt 0) {
            $summaryParts += "Missing in NEW: $($result.MissingInNew -join ', ')"
        }
        if ($result.ExtraInNew.Count -gt 0) {
            $summaryParts += "Extra in NEW: $($result.ExtraInNew -join ', ')"
        }
        if ($result.Different.Count -gt 0) {
            $summaryParts += "Different: $($result.Different -join ', ')"
        }
        if ($result.StateStatusInfo) {
            $summaryParts += $result.StateStatusInfo
        }
        $result.Summary = $summaryParts -join "; "

        return $result
    }
    catch {
        $result.Summary = "Error comparing: $($_.Exception.Message)"
        $result.IsIdentical = $false
        return $result
    }
}

function Compare-Directories {
    param(
        [string]$Dir1,
        [string]$Dir2,
        [string]$RelativePath = ""
    )

    $differences = @()

    # Get files in both directories
    $files1 = @{}
    $files2 = @{}

    if (Test-Path $Dir1) {
        Get-ChildItem -Path $Dir1 -File -Recurse | ForEach-Object {
            $relativeName = $_.FullName.Substring($Dir1.Length).TrimStart('\', '/')
            $files1[$relativeName] = $_
        }
    }

    if (Test-Path $Dir2) {
        Get-ChildItem -Path $Dir2 -File -Recurse | ForEach-Object {
            $relativeName = $_.FullName.Substring($Dir2.Length).TrimStart('\', '/')
            $files2[$relativeName] = $_
        }
    }

    # Find files only in old output
    foreach ($file in $files1.Keys) {
        if (-not $files2.ContainsKey($file)) {
            $differences += [PSCustomObject]@{
                File = $file
                Status = "Only in OLD"
                Details = ""
            }
        }
    }

    # Find files only in new output
    foreach ($file in $files2.Keys) {
        if (-not $files1.ContainsKey($file)) {
            $differences += [PSCustomObject]@{
                File = $file
                Status = "Only in NEW"
                Details = ""
            }
        }
    }

    # Compare common files
    foreach ($file in $files1.Keys) {
        if ($files2.ContainsKey($file)) {
            $file1 = $files1[$file]
            $file2 = $files2[$file]

            # For XML files, compare semantic content (ignoring namespace prefix differences)
            if ($file -like "*.xml") {
                # Special handling for Metadata.xml - entity-by-entity comparison
                if ($file -eq "Metadata.xml") {
                    $metadataComparison = Compare-MetadataXml -OldFile $file1.FullName -NewFile $file2.FullName
                    if (-not $metadataComparison.IsIdentical) {
                        $differences += [PSCustomObject]@{
                            File = $file
                            Status = "Content differs"
                            Details = $metadataComparison.Summary
                            MetadataComparison = $metadataComparison
                        }
                    }
                }
                else {
                    $fileSize = $file1.Length

                    # For large files (>1MB), use fast XmlReader-based comparison
                    # For smaller files, use full semantic hash
                    if ($fileSize -gt 1MB) {
                        $hash1 = Get-XmlElementCount $file1.FullName
                        $hash2 = Get-XmlElementCount $file2.FullName
                        $method = "fast structural"
                    } else {
                        $hash1 = Get-XmlSemanticHash $file1.FullName
                        $hash2 = Get-XmlSemanticHash $file2.FullName
                        $method = "semantic"
                    }

                    if ($hash1.StartsWith("ERROR:") -or $hash2.StartsWith("ERROR:")) {
                        $differences += [PSCustomObject]@{
                            File = $file
                            Status = "Parse error"
                            Details = "Old: $hash1, New: $hash2"
                        }
                    }
                    elseif ($hash1 -ne $hash2) {
                        $differences += [PSCustomObject]@{
                            File = $file
                            Status = "Content differs"
                            Details = "XML content differs ($method comparison)"
                        }
                    }
                }
            }
            # For non-XML files, compare by hash using .NET directly
            else {
                $md5 = [System.Security.Cryptography.MD5]::Create()
                $stream1 = [System.IO.File]::OpenRead($file1.FullName)
                $hash1 = [System.BitConverter]::ToString($md5.ComputeHash($stream1)) -replace '-', ''
                $stream1.Close()

                $stream2 = [System.IO.File]::OpenRead($file2.FullName)
                $hash2 = [System.BitConverter]::ToString($md5.ComputeHash($stream2)) -replace '-', ''
                $stream2.Close()

                if ($hash1 -ne $hash2) {
                    $differences += [PSCustomObject]@{
                        File = $file
                        Status = "Content differs"
                        Details = "File content differs"
                    }
                }
            }
        }
    }

    return $differences
}

$differences = Compare-Directories -Dir1 $oldOutputDir -Dir2 $newOutputDir

# Report results
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "COMPARISON RESULTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$oldFileCount = (Get-ChildItem -Path $oldOutputDir -File -Recurse).Count
$newFileCount = (Get-ChildItem -Path $newOutputDir -File -Recurse).Count

Write-Host "Files generated:" -ForegroundColor White
Write-Host "  Old generator: $oldFileCount files" -ForegroundColor Gray
Write-Host "  New generator: $newFileCount files" -ForegroundColor Gray
Write-Host ""

Write-Host "Performance:" -ForegroundColor White
Write-Host "  Old generator: $($oldDuration.TotalSeconds.ToString('F1')) seconds" -ForegroundColor Gray
Write-Host "  New generator: $($newDuration.TotalSeconds.ToString('F1')) seconds" -ForegroundColor Gray
Write-Host ""

if ($differences.Count -eq 0) {
    Write-Host "SUCCESS: Output is identical!" -ForegroundColor Green
}
else {
    Write-Host "DIFFERENCES FOUND: $($differences.Count)" -ForegroundColor Red
    Write-Host ""

    $differences | Format-Table -AutoSize

    # Show detailed diff for first few differing files
    $contentDiffs = $differences | Where-Object { $_.Status -eq "Content differs" } | Select-Object -First 3

    foreach ($diff in $contentDiffs) {
        Write-Host ""
        Write-Host "Diff for: $($diff.File)" -ForegroundColor Yellow
        Write-Host "-" * 60

        $oldFile = Join-Path $oldOutputDir $diff.File
        $newFile = Join-Path $newOutputDir $diff.File

        if ($diff.File -eq "Metadata.xml" -and $diff.MetadataComparison) {
            # Show detailed entity-level differences for Metadata.xml
            $mc = $diff.MetadataComparison
            Write-Host "Entity counts: Old=$($mc.OldEntityCount), New=$($mc.NewEntityCount)" -ForegroundColor Gray

            if ($mc.MissingInNew.Count -gt 0) {
                Write-Host "Missing in NEW ($($mc.MissingInNew.Count)):" -ForegroundColor Red
                foreach ($entity in $mc.MissingInNew) {
                    Write-Host "  - $entity" -ForegroundColor Red
                }
            }

            if ($mc.ExtraInNew.Count -gt 0) {
                Write-Host "Extra in NEW ($($mc.ExtraInNew.Count)):" -ForegroundColor Green
                foreach ($entity in $mc.ExtraInNew) {
                    Write-Host "  + $entity" -ForegroundColor Green
                }
            }

            if ($mc.Different.Count -gt 0) {
                Write-Host "Different entities ($($mc.Different.Count)):" -ForegroundColor Yellow
                foreach ($entity in $mc.Different) {
                    Write-Host "  ~ $entity" -ForegroundColor Yellow
                }
            }

            if ($mc.StateStatusInfo) {
                Write-Host ""
                Write-Host "DefaultStateStatus:" -ForegroundColor Cyan
                Write-Host "  $($mc.StateStatusInfo)" -ForegroundColor Gray
            }

            Write-Host ""
            Write-Host "For detailed XML diff, use a diff tool:" -ForegroundColor Gray
            Write-Host "  Old: $oldFile" -ForegroundColor Gray
            Write-Host "  New: $newFile" -ForegroundColor Gray
        }
        elseif ($diff.File -like "*.xml") {
            # For other XML files, suggest using a diff tool
            Write-Host "Use a diff tool to compare:" -ForegroundColor Gray
            Write-Host "  Old: $oldFile" -ForegroundColor Gray
            Write-Host "  New: $newFile" -ForegroundColor Gray
        }
    }
}

Write-Host ""
Write-Host "Output preserved for manual inspection:" -ForegroundColor Cyan
Write-Host "  $tempBase" -ForegroundColor Gray
Write-Host "    Old/  - Old generator output" -ForegroundColor Gray
Write-Host "    New/  - New generator output" -ForegroundColor Gray
Write-Host "    Config/appsettings.json - Temporary config" -ForegroundColor Gray
Write-Host ""
Write-Host "To clean up, run:" -ForegroundColor Gray
Write-Host "  Remove-Item -Recurse -Force '$tempBase'" -ForegroundColor Gray
