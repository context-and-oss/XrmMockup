param(
    [string]$Output = './nupkg',
    [string]$Configuration = 'Release',
    [string]$CoreVersion = '0.0.0-dev',
    [string]$MetadataVersion = '0.0.0-dev'
)

# Local packaging is a preview only: no changelog writes, release state, or credentials.
$ErrorActionPreference = 'Stop'
$versionArgs = @("-p:CoreReleaseVersion=$CoreVersion", "-p:MetadataReleaseVersion=$MetadataVersion")
dotnet build --configuration $Configuration @versionArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet pack './src/XrmMockup365/XrmMockup365.csproj' --configuration $Configuration --no-build --output $Output @versionArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet pack './src/MetadataGen/MetadataGenerator.Tool/MetadataGenerator.Tool.csproj' --configuration $Configuration --no-build --output $Output @versionArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
