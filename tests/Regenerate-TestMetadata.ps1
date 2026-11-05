<#
.SYNOPSIS
    Regenerates the test project's Metadata and XrmContext from Dataverse using tests/appsettings.json.

.DESCRIPTION
    Runs the local MetadataGenerator.Tool project against tests/appsettings.json so that:
      - Metadata is written to  XrmMockup365Test/Metadata        (XrmMockup.Metadata.OutputDirectory)
      - XrmContext classes go to TestPluginAssembly365/Context    (XrmContext.OutputDirectory)

    Both the MetadataGenerator.Tool and the xrmcontext tool read a file literally named
    "appsettings.json" from the current working directory and resolve their OutputDirectory
    relative to it, so all paths land inside the test projects.

    The xrmcontext tool is a separate .NET tool (it produces the early-bound "context" classes;
    MetadataGenerator.Tool only produces metadata). This script ensures a local tool manifest
    exists under tests/.config so xrmcontext can run with tests/ as the working directory.

.PARAMETER NoBuild
    Skip building the MetadataGenerator.Tool project (assumes it's already built).

.PARAMETER Verbose
    Show verbose output from the build and generation process.

.PARAMETER MetadataOnly
    Only regenerate metadata (skip the XrmContext context classes).

.PARAMETER ContextOnly
    Only regenerate the XrmContext context classes (skip metadata generation).

.NOTES
    Requires:
    - .NET SDK
    - Valid Azure/Dataverse authentication (prompts for interactive login if needed)
#>

param(
    [switch]$NoBuild,
    [switch]$Verbose,
    [switch]$MetadataOnly,
    [switch]$ContextOnly
)

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot
$toolProjectPath = Join-Path $scriptDir "..\src\MetadataGen\MetadataGenerator.Tool\MetadataGenerator.Tool.csproj"
$configPath = Join-Path $scriptDir "appsettings.json"

# Keep in sync with src/MetadataGen/.config/dotnet-tools.json
$xrmContextVersion = "4.0.0-beta.26"

if (-not (Test-Path $configPath)) {
    Write-Error "appsettings.json not found at: $configPath"
    exit 1
}
$config = Get-Content $configPath | ConvertFrom-Json

# ---------------------------------------------------------------------------
# Metadata
# ---------------------------------------------------------------------------
if (-not $ContextOnly) {
    Write-Host "=== Regenerate Test Metadata ===" -ForegroundColor Cyan
    Write-Host ""

    if (-not (Test-Path $toolProjectPath)) {
        Write-Error "MetadataGenerator.Tool project not found at: $toolProjectPath"
        exit 1
    }

    Write-Host "Configuration:" -ForegroundColor Yellow
    Write-Host "  Dataverse URL: $($config.DataverseUrl)"
    Write-Host "  Output Directory: $($config.XrmMockup.Metadata.OutputDirectory)"
    Write-Host "  Entities: $($config.XrmMockup.Metadata.Entities.Count) entities"
    Write-Host ""

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

    Write-Host "Running MetadataGenerator.Tool..." -ForegroundColor Yellow
    Write-Host "  Working directory: $scriptDir"
    Write-Host ""

    # --config sets the tool's working dir to the config folder (tests/), so
    # OutputDirectory resolves to tests/XrmMockup365Test/Metadata.
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

# ---------------------------------------------------------------------------
# XrmContext (early-bound context classes)
# ---------------------------------------------------------------------------
if (-not $MetadataOnly) {
    Write-Host ""
    Write-Host "=== Regenerate XrmContext ===" -ForegroundColor Cyan
    Write-Host ""

    Write-Host "XrmContext Configuration:" -ForegroundColor Yellow
    Write-Host "  Output Directory: $($config.XrmContext.OutputDirectory)"
    Write-Host "  Namespace: $($config.XrmContext.NamespaceSetting)"
    Write-Host "  Entities: $($config.XrmContext.Entities.Count) entities"
    Write-Host ""

    # xrmcontext reads tests/appsettings.json from the working dir, so it must run
    # from tests/. The tool manifest only resolves by walking UP the tree, and tests/
    # is not under src/MetadataGen, so ensure a local manifest exists here.
    Push-Location $scriptDir
    try {
        # `dotnet new tool-manifest` writes .config/dotnet-tools.json on older SDKs and
        # dotnet-tools.json in the folder root on newer ones, so accept either.
        $manifestPath = @(".config/dotnet-tools.json", "dotnet-tools.json") |
            ForEach-Object { Join-Path $scriptDir $_ } |
            Where-Object { Test-Path $_ } |
            Select-Object -First 1
        if (-not $manifestPath) {
            Write-Host "Creating local tool manifest..." -ForegroundColor Yellow
            & dotnet new tool-manifest | Out-Null
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Failed to create tool manifest"
                exit 1
            }
        }

        # Always install the pinned version: on an existing manifest this rewrites the entry, so a
        # bump to $xrmContextVersion takes effect instead of `dotnet tool restore` keeping the old one.
        Write-Host "Installing xrmcontext $xrmContextVersion..." -ForegroundColor Yellow
        & dotnet tool install xrmcontext --version $xrmContextVersion | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to install xrmcontext"
            exit 1
        }

        Write-Host "Running XrmContext..." -ForegroundColor Yellow
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
