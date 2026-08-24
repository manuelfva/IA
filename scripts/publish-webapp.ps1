<#
.SYNOPSIS
    Publishes Test-IA.WebApp as a self-contained deployment package.
.DESCRIPTION
    This script:
    1. Cleans the solution
    2. Restores NuGet packages
    3. Builds all projects
    4. Runs unit tests
    5. Publishes Test-IA.WebApp in Release mode (self-contained)
    6. Creates a deployment zip with README instructions
.NOTES
    Requires .NET 10.0 SDK and a domain-joined Windows machine for full validation.
#>

#Requires -Version 5.1

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$Configuration = "Release",

    [Parameter(Mandatory = $false)]
    [string]$RuntimeIdentifier = "win-x64",

    [Parameter(Mandatory = $false)]
    [string]$OutputPath = "",

    [Parameter(Mandatory = $false)]
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

# Determine solution root directory
$SolutionRoot = Split-Path -Parent $PSScriptRoot
$WebAppProject = Join-Path $SolutionRoot "src\Test-IA.WebApp\Test-IA.WebApp.csproj"
$ReleasesDir = Join-Path $SolutionRoot "releases"

# Create releases directory if it does not exist
if (-not (Test-Path $ReleasesDir)) {
    New-Item -Path $ReleasesDir -ItemType Directory -Force | Out-Null
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Test-IA.WebApp Deployment Publisher" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean
Write-Host "[1/6] Cleaning solution..." -ForegroundColor Yellow
& dotnet clean $SolutionRoot --configuration $Configuration
Write-Host ""

# Step 2: Restore
Write-Host "[2/6] Restoring NuGet packages..." -ForegroundColor Yellow
& dotnet restore $SolutionRoot
Write-Host ""

# Step 3: Build
Write-Host "[3/6] Building solution..." -ForegroundColor Yellow
& dotnet build $SolutionRoot --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "BUILD FAILED. Aborting deployment." -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 4: Test (unless skipped)
if (-not $SkipTests) {
    Write-Host "[4/6] Running unit tests..." -ForegroundColor Yellow
    & dotnet test $SolutionRoot --configuration $Configuration --no-build --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "TESTS FAILED. Aborting deployment." -ForegroundColor Red
        exit 1
    }
    Write-Host ""
}
else {
    Write-Host "[4/6] Skipping tests (SkipTests flag set)." -ForegroundColor Yellow
    Write-Host ""
}

# Step 5: Publish
Write-Host "[5/6] Publishing WebApp (self-contained)..." -ForegroundColor Yellow
if ($OutputPath) {
    & dotnet publish $WebAppProject --configuration $Configuration --runtime $RuntimeIdentifier --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=false -p:StripSymbols=false --output $OutputPath
}
else {
    $PublishOutput = Join-Path $ReleasesDir "publish"
    & dotnet publish $WebAppProject --configuration $Configuration --runtime $RuntimeIdentifier --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=false -p:StripSymbols=false --output $PublishOutput
}
if ($LASTEXITCODE -ne 0) {
    Write-Host "PUBLISH FAILED. Aborting deployment." -ForegroundColor Red
    exit 1
}
Write-Host ""

# Determine the publish directory
if ($OutputPath) {
    $PublishDir = $OutputPath
}
else {
    $PublishDir = Join-Path $ReleasesDir "publish"
}

# Step 6: Create deployment zip
Write-Host "[6/6] Creating deployment package..." -ForegroundColor Yellow

# Generate timestamp for the zip filename
$CurrentDate = [System.TimeZoneInfo]::ConvertTimeFromUtc([DateTime]::UtcNow, [System.TimeZoneInfo]::FindSystemTimeZoneById("Romance Standard Time"))
$Timestamp = $CurrentDate.ToString("yyyyMMdd-HHmmss")
$ZipFileName = "Test-IA.WebApp-deploy-${Timestamp}.zip"
$ZipPath = Join-Path $ReleasesDir $ZipFileName

# Create the zip file
if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}
Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipPath -Force

# Display results
$ZipSizeMB = (Get-Item $ZipPath).Length / 1MB
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Deployment Package Created Successfully" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Package:  $ZipPath" -ForegroundColor Cyan
Write-Host "Size:     $([math]::Round($ZipSizeMB, 2)) MB" -ForegroundColor Cyan
Write-Host "Location: $ReleasesDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "Publish directory: $PublishDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "To deploy:" -ForegroundColor White
Write-Host "  1. Copy $ZipFileName to the target server" -ForegroundColor White
Write-Host "  2. Extract: Expand-Archive $ZipFileName -DestinationPath <install-folder>" -ForegroundColor White
Write-Host "  3. Run: .\Test-IA.WebApp.exe" -ForegroundColor White
Write-Host ""
