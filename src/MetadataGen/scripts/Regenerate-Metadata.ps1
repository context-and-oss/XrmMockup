<#
.SYNOPSIS
    Regenerates the TestMetadata folder using the MetadataGenerator.Tool and optionally XrmContext.

.DESCRIPTION
    This script builds and runs the MetadataGenerator.Tool to fetch fresh metadata
    from Dataverse and store it in the TestMetadata folder for use by XrmMockup tests.

    Optionally, it can also regenerate the XrmContext C# code by using the -IncludeContext switch.

.PARAMETER NoBuild
    Skip building the MetadataGenerator.Tool project (assumes it's already built).

.PARAMETER Verbose
    Show verbose output from the build and generation process.

.PARAMETER IncludeContext
    Also regenerate the XrmContext C# context classes.

.PARAMETER ContextOnly
    Only regenerate the XrmContext C# context (skip metadata generation).

.NOTES
    Requires:
    - .NET SDK
    - Valid Azure/Dataverse authentication (will prompt for interactive login if needed)

    The appsettings.json file in this directory configures:
    - DataverseUrl: The Dataverse environment to connect to
    - XrmMockup.Metadata.OutputDirectory: Where to store metadata (./TestMetadata)
    - XrmMockup.Metadata.Entities: List of entities to include in metadata
    - XrmContext: Configuration for XrmContext code generation
#>

param(
    [switch]$NoBuild,
    [switch]$Verbose,
    [switch]$IncludeContext,
    [switch]$ContextOnly
)

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot
$toolProjectPath = Join-Path $scriptDir "..\MetadataGenerator.Tool\MetadataGenerator.Tool.csproj"
$configPath = Join-Path $scriptDir "..\appsettings.json"

if (-not $ContextOnly) {
    Write-Host "=== Regenerate Test Metadata ===" -ForegroundColor Cyan
    Write-Host ""

    # Verify files exist
    if (-not (Test-Path $toolProjectPath)) {
        Write-Error "MetadataGenerator.Tool project not found at: $toolProjectPath"
        exit 1
    }

    if (-not (Test-Path $configPath)) {
        Write-Error "appsettings.json not found at: $configPath"
        exit 1
    }

    # Show config
    Write-Host "Configuration:" -ForegroundColor Yellow
    $config = Get-Content $configPath | ConvertFrom-Json
    Write-Host "  Dataverse URL: $($config.DataverseUrl)"
    Write-Host "  Output Directory: $($config.XrmMockup.Metadata.OutputDirectory)"
    Write-Host "  Entities: $($config.XrmMockup.Metadata.Entities.Count) entities"
    Write-Host ""

    # Build if needed
    if (-not $NoBuild) {
        Write-Host "Building MetadataGenerator.Tool..." -ForegroundColor Yellow
        $buildArgs = @("build", $toolProjectPath, "--configuration", "Release")
        if (-not $Verbose) {
            $buildArgs += "--verbosity", "quiet"
        }
        & dotnet @buildArgs
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Build failed"
            exit 1
        }
        Write-Host "Build succeeded" -ForegroundColor Green
        Write-Host ""
    }

    # Run the generator
    Write-Host "Running MetadataGenerator.Tool..." -ForegroundColor Yellow
    Write-Host "  Working directory: $scriptDir"
    Write-Host ""

    Push-Location $scriptDir
    try {
        $runArgs = @("run", "--project", $toolProjectPath, "--configuration", "Release", "--no-build", "--", "--config", $configPath)
        & dotnet @runArgs
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Metadata generation failed"
            exit 1
        }
    }
    finally {
        Pop-Location
    }

    Write-Host ""
    Write-Host "=== Metadata generation complete ===" -ForegroundColor Green

    # Show what was generated
    $outputDir = Join-Path $scriptDir $config.XrmMockup.Metadata.OutputDirectory
    if (Test-Path $outputDir) {
        Write-Host ""
        Write-Host "Generated files:" -ForegroundColor Yellow
        Get-ChildItem $outputDir -Recurse -File | ForEach-Object {
            $relativePath = $_.FullName.Substring($outputDir.Length + 1)
            $size = "{0:N0} KB" -f ($_.Length / 1KB)
            Write-Host "  $relativePath ($size)"
        }
    }
}

# Generate XrmContext if requested
if ($IncludeContext -or $ContextOnly) {
    Write-Host ""
    Write-Host "=== Regenerate XrmContext ===" -ForegroundColor Cyan
    Write-Host ""

    # Read config if not already loaded
    if (-not $config) {
        $config = Get-Content $configPath | ConvertFrom-Json
    }

    # Show XrmContext config
    Write-Host "XrmContext Configuration:" -ForegroundColor Yellow
    Write-Host "  Output Directory: $($config.XrmContext.OutputDirectory)"
    Write-Host "  Namespace: $($config.XrmContext.NamespaceSetting)"
    Write-Host "  Service Context: $($config.XrmContext.ServiceContextName)"
    Write-Host "  Entities: $($config.XrmContext.Entities.Count) entities"
    Write-Host ""

    # Run XrmContext
    Write-Host "Running XrmContext..." -ForegroundColor Yellow
    Push-Location $scriptDir/..
    try {
        & dotnet xrmcontext
        if ($LASTEXITCODE -ne 0) {
            Write-Error "XrmContext generation failed"
            exit 1
        }
    }
    finally {
        Pop-Location
    }

    Write-Host ""
    Write-Host "=== XrmContext generation complete ===" -ForegroundColor Green

    # Show what was generated
    $contextDir = Join-Path $scriptDir $config.XrmContext.OutputDirectory
    if (Test-Path $contextDir) {
        Write-Host ""
        Write-Host "Generated context files:" -ForegroundColor Yellow
        Get-ChildItem $contextDir -Recurse -File | ForEach-Object {
            $relativePath = $_.FullName.Substring($contextDir.Length + 1)
            $size = "{0:N0} KB" -f ($_.Length / 1KB)
            Write-Host "  $relativePath ($size)"
        }
    }
}
