# InsightImageGen Build Script
# Usage:
#   .\build.ps1              - Full build with installer
#   .\build.ps1 -SkipInstaller   - Build without installer
#   .\build.ps1 -Clean       - Clean build

param(
    [switch]$SkipInstaller,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

# Configuration
$ProjectName = "InsightMediaGenerator"
$ProjectPath = "InsightMediaGenerator\InsightMediaGenerator.csproj"
$PublishDir = "publish"
$OutputDir = "Output"
$Configuration = "Release"
$Runtime = "win-x64"
$Version = "1.0.0"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " InsightImageGen Build Script v$Version" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Clean if requested
if ($Clean) {
    Write-Host "[1/4] Cleaning previous builds..." -ForegroundColor Yellow

    if (Test-Path $PublishDir) {
        Remove-Item -Recurse -Force $PublishDir
        Write-Host "  - Removed $PublishDir" -ForegroundColor Gray
    }

    if (Test-Path $OutputDir) {
        Remove-Item -Recurse -Force $OutputDir
        Write-Host "  - Removed $OutputDir" -ForegroundColor Gray
    }

    # Clean bin/obj folders
    Get-ChildItem -Path "InsightMediaGenerator" -Include bin,obj -Recurse -Directory | ForEach-Object {
        Remove-Item -Recurse -Force $_.FullName
        Write-Host "  - Removed $($_.FullName)" -ForegroundColor Gray
    }

    Write-Host "  Clean completed." -ForegroundColor Green
    Write-Host ""
}

# Step 1: Restore dependencies
Write-Host "[1/4] Restoring dependencies..." -ForegroundColor Yellow
dotnet restore $ProjectPath
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Restore failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  Restore completed." -ForegroundColor Green
Write-Host ""

# Step 2: Build
Write-Host "[2/4] Building $ProjectName..." -ForegroundColor Yellow
dotnet build $ProjectPath -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  Build completed." -ForegroundColor Green
Write-Host ""

# Step 3: Publish
Write-Host "[3/4] Publishing to $PublishDir..." -ForegroundColor Yellow

# Create publish directory
if (!(Test-Path $PublishDir)) {
    New-Item -ItemType Directory -Path $PublishDir | Out-Null
}

dotnet publish $ProjectPath -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $PublishDir --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Publish failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  Publish completed." -ForegroundColor Green
Write-Host ""

# Step 4: Create Installer (optional)
if (-not $SkipInstaller) {
    Write-Host "[4/4] Creating installer..." -ForegroundColor Yellow

    # Check for Inno Setup
    $InnoSetupPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (!(Test-Path $InnoSetupPath)) {
        Write-Host "WARNING: Inno Setup not found at $InnoSetupPath" -ForegroundColor Yellow
        Write-Host "  Skipping installer creation." -ForegroundColor Yellow
        Write-Host "  Install from: https://jrsoftware.org/isdl.php" -ForegroundColor Gray
    } else {
        # Create output directory
        if (!(Test-Path $OutputDir)) {
            New-Item -ItemType Directory -Path $OutputDir | Out-Null
        }

        # Run Inno Setup
        & $InnoSetupPath "setup.iss"
        if ($LASTEXITCODE -ne 0) {
            Write-Host "ERROR: Installer creation failed!" -ForegroundColor Red
            exit 1
        }
        Write-Host "  Installer created successfully." -ForegroundColor Green
    }
} else {
    Write-Host "[4/4] Skipping installer creation (--SkipInstaller)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output locations:" -ForegroundColor White
Write-Host "  Application: $PublishDir\" -ForegroundColor Gray
if (-not $SkipInstaller -and (Test-Path "$OutputDir\InsightImageGen_Setup_$Version.exe")) {
    Write-Host "  Installer:   $OutputDir\InsightImageGen_Setup_$Version.exe" -ForegroundColor Gray
}
Write-Host ""
