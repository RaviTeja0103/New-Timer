################################################################################
# Family Hub Timer - Build & Package Script (Windows PowerShell)
#
# This script performs a complete build and TPK packaging workflow:
# 1. Clean previous builds
# 2. Build the .NET application (Release mode)
# 3. Generate the TPK package
# 4. Verify the TPK file
# 5. Display deployment instructions
#
# Usage:
#   .\build-and-package.ps1              # Build and package with defaults
#   .\build-and-package.ps1 -Config Release    # Explicit Release configuration
#   .\build-and-package.ps1 -Config Debug     # Debug configuration
#
# Requirements:
#   - PowerShell 5.0+
#   - .NET SDK 8.0+
#   - Tizen Studio (for TPK generation)
#
# Output: FamilyHubTimer.tpk (ready to install on Family Hub)
#
################################################################################

param(
    [string]$Config = "Release",
    [switch]$SkipBuild = $false
)

# Color output helper
function Write-Header {
    param([string]$message)
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  $message" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Step {
    param([int]$number, [string]$message)
    Write-Host "[STEP $number/5] $message" -ForegroundColor Blue
}

function Write-Success {
    param([string]$message)
    Write-Host "✓ $message" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$message)
    Write-Host "✗ $message" -ForegroundColor Red
}

function Write-Warning-Custom {
    param([string]$message)
    Write-Host "⚠ $message" -ForegroundColor Yellow
}

function Write-Info {
    param([string]$message)
    Write-Host "[INFO] $message" -ForegroundColor Blue
}

# Configuration
$PROJECT_NAME = "FamilyHubTimer"
$SCRIPT_DIR = Split-Path -Parent $MyInvocation.MyCommand.Path
$BUILD_DIR = Join-Path $SCRIPT_DIR "bin\$Config\net8.0"
$TPK_BUILD_DIR = Join-Path $SCRIPT_DIR "tpk_build"
$TPK_FILE = Join-Path $SCRIPT_DIR "$PROJECT_NAME.tpk"
$MANIFEST_FILE = Join-Path $SCRIPT_DIR "tizen-manifest.xml"
$PROJECT_FILE = Join-Path $SCRIPT_DIR "FamilyHubTimer.csproj"

Write-Header "Family Hub Timer - Build & Package`n.NET → TPK (Tizen Package)"
Write-Info "Configuration: $Config"
Write-Info "Project Directory: $SCRIPT_DIR"
Write-Host ""

# Step 1: Clean previous builds
Write-Step 1 "Cleaning previous builds..."
if (Test-Path $BUILD_DIR) {
    Remove-Item (Join-Path $SCRIPT_DIR "bin") -Recurse -Force -ErrorAction SilentlyContinue
    Write-Success "Cleaned /bin directory"
}
if (Test-Path $TPK_BUILD_DIR) {
    Remove-Item $TPK_BUILD_DIR -Recurse -Force -ErrorAction SilentlyContinue
    Write-Success "Cleaned TPK build directory"
}
if (Test-Path $TPK_FILE) {
    Remove-Item $TPK_FILE -Force -ErrorAction SilentlyContinue
    Write-Success "Removed previous TPK file"
}
Write-Host ""

# Step 2: Build the .NET application
Write-Step 2 "Building .NET application ($Config)..."

if ($SkipBuild) {
    Write-Warning-Custom "Skipping build (--skip-build flag)"
} else {
    $buildOutput = & dotnet build $PROJECT_FILE -c $Config -v minimal 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success ".NET build successful"
    } else {
        Write-Error-Custom ".NET build failed"
        Write-Host $buildOutput
        exit 1
    }
}

# Verify build output exists
if (-not (Test-Path $BUILD_DIR)) {
    Write-Error-Custom "Build output directory not found: $BUILD_DIR"
    exit 1
}

$DLL_PATH = Join-Path $BUILD_DIR "$PROJECT_NAME.dll"
if (-not (Test-Path $DLL_PATH)) {
    Write-Error-Custom "Build output not found: $DLL_PATH"
    exit 1
}
Write-Success "Output binary: $DLL_PATH"
Write-Host ""

# Step 3: Prepare TPK package structure
Write-Step 3 "Preparing TPK package structure..."

# Create TPK build directories
New-Item -ItemType Directory -Path "$TPK_BUILD_DIR\bin" -Force | Out-Null
New-Item -ItemType Directory -Path "$TPK_BUILD_DIR\shared\res" -Force | Out-Null

# Copy manifest
if (-not (Test-Path $MANIFEST_FILE)) {
    Write-Error-Custom "Missing tizen-manifest.xml"
    exit 1
}
Copy-Item $MANIFEST_FILE "$TPK_BUILD_DIR\" -Force
Write-Success "Copied tizen-manifest.xml"

# Copy .NET runtime and application binaries
Copy-Item $DLL_PATH "$TPK_BUILD_DIR\bin\" -Force
Write-Success "Copied application binary"

# Copy runtime files if they exist
$RUNTIMES_PATH = Join-Path $BUILD_DIR "runtimes"
if (Test-Path $RUNTIMES_PATH) {
    Copy-Item $RUNTIMES_PATH "$TPK_BUILD_DIR\" -Recurse -Force
    Write-Success "Copied runtime files"
}

# Copy supporting DLLs
Get-ChildItem $BUILD_DIR -Filter "*.dll" -ErrorAction SilentlyContinue | 
    Where-Object { $_.Name -ne "$PROJECT_NAME.dll" } |
    ForEach-Object { Copy-Item $_.FullName "$TPK_BUILD_DIR\bin\" -Force }
Write-Success "Copied supporting libraries"

# Copy config files if they exist
if (Test-Path "$BUILD_DIR\config.json") {
    Copy-Item "$BUILD_DIR\config.json" "$TPK_BUILD_DIR\" -Force
}

Write-Host ""

# Step 4: Generate TPK package
Write-Step 4 "Generating TPK package..."

# Check if tizen CLI is available
$tizenCmd = Get-Command tizen -ErrorAction SilentlyContinue
if (-not $tizenCmd) {
    Write-Warning-Custom "'tizen' command not found"
    Write-Info "Tizen SDK not installed. Check Tizen Studio installation."
    Write-Host ""
    Write-Warning-Custom "To install Tizen SDK:"
    Write-Host "1. Download from: https://developer.tizen.org/development/tizen-studio/download"
    Write-Host "2. Run installer and select 'Tizen SDK' when prompted"
    Write-Host "3. Add to PATH environment variable (Tizen Studio bin directory)"
    Write-Host ""
    exit 1
}

# Generate TPK
$cwd = Get-Location
Set-Location $TPK_BUILD_DIR

try {
    $tizenOutput = & tizen package -t tpk -o ".." 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "TPK package generated"
    } else {
        Write-Warning-Custom "TPK generation completed with warnings"
        Write-Host $tizenOutput
    }
} catch {
    Write-Warning-Custom "Error during TPK generation: $_"
} finally {
    Set-Location $cwd
}

# Step 5: Verify TPK is ready
Write-Host ""
Write-Step 5 "Verifying TPK package..."

if (Test-Path $TPK_FILE) {
    $TPK_SIZE = (Get-Item $TPK_FILE).Length / 1MB
    Write-Success "TPK file created: $TPK_FILE ($([Math]::Round($TPK_SIZE, 2)) MB)"
} else {
    Write-Warning-Custom "TPK file not found at expected location: $TPK_FILE"
    Write-Info "Check tizen-studio installation and SDK configuration"
    Write-Host ""
    exit 1
}

# Success - show next steps
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║                    BUILD COMPLETE ✓                           ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Success ".NET application built successfully"
Write-Success "TPK package generated and ready"
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Enable Developer Mode on Family Hub (if not already done):"
Write-Host "   Device Settings → About → Tap Build Number 7 times"
Write-Host "   Settings → Developer Options → Enable USB Debugging"
Write-Host ""
Write-Host "2. Install TPK on device:"
Write-Host ""
Write-Host "   Option A - Using Tizen CLI (fastest):"
Write-Host "   tizen install -t tpk -n '$TPK_FILE' -s 192.168.1.50:26101" -ForegroundColor Yellow
Write-Host ""
Write-Host "   Option B - Using device IP (if different):"
Write-Host "   tizen install -t tpk -n '$TPK_FILE' -s <DEVICE_IP>:26101" -ForegroundColor Yellow
Write-Host ""
Write-Host "   Option C - Using Tizen Studio GUI:"
Write-Host "   - Open Tizen Studio"
Write-Host "   - Tools → Package Manager"
Write-Host "   - Select device and install $TPK_FILE"
Write-Host ""
Write-Host "3. After installation:"
Write-Host "   - App appears in Family Hub app list"
Write-Host "   - Tap to launch"
Write-Host "   - Test with a 10-second timer"
Write-Host ""
Write-Host "[TIP] View logs during testing:" -ForegroundColor Blue
Write-Host "      sdb -s <DEVICE_IP>:26101 dlog FamilyHubTimer" -ForegroundColor Yellow
Write-Host ""
Write-Host "Documentation: See TPK_DEPLOYMENT_GUIDE.md for detailed instructions" -ForegroundColor Yellow
Write-Host ""
