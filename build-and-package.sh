#!/bin/bash

################################################################################
# Family Hub Timer - Build & Package Script (macOS/Linux)
# 
# This script performs a complete build and TPK packaging workflow:
# 1. Clean previous builds
# 2. Build the .NET application (Release mode)
# 3. Generate the TPK package
# 4. Verify the TPK file
# 5. Display deployment instructions
#
# Usage:
#   ./build-and-package.sh              # Build and package with defaults
#   ./build-and-package.sh Release      # Explicit Release configuration
#   ./build-and-package.sh Debug        # Debug configuration (for testing)
#
# Output: FamilyHubTimer.tpk (ready to install on Family Hub)
#
################################################################################

set -e  # Exit on error

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_NAME="FamilyHubTimer"
CONFIGURATION="${1:-Release}"
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
BUILD_DIR="${SCRIPT_DIR}/bin/${CONFIGURATION}/net8.0"
TPK_BUILD_DIR="${SCRIPT_DIR}/tpk_build"
TPK_FILE="${SCRIPT_DIR}/${PROJECT_NAME}.tpk"
MANIFEST_FILE="${SCRIPT_DIR}/tizen-manifest.xml"

echo ""
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║  Family Hub Timer - Build & Package                           ║"
echo "║  .NET → TPK (Tizen Package)                                   ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""
echo -e "${BLUE}[INFO]${NC} Configuration: ${YELLOW}${CONFIGURATION}${NC}"
echo -e "${BLUE}[INFO]${NC} Project Directory: ${YELLOW}${SCRIPT_DIR}${NC}"
echo ""

# Step 1: Clean previous builds
echo -e "${BLUE}[STEP 1/5]${NC} Cleaning previous builds..."
if [ -d "${BUILD_DIR}" ]; then
    rm -rf "${SCRIPT_DIR}/bin"
    echo -e "${GREEN}✓${NC} Cleaned /bin directory"
fi
if [ -d "${TPK_BUILD_DIR}" ]; then
    rm -rf "${TPK_BUILD_DIR}"
    echo -e "${GREEN}✓${NC} Cleaned TPK build directory"
fi
if [ -f "${TPK_FILE}" ]; then
    rm -f "${TPK_FILE}"
    echo -e "${GREEN}✓${NC} Removed previous TPK file"
fi
echo ""

# Step 2: Build the .NET application
echo -e "${BLUE}[STEP 2/5]${NC} Building .NET application (${CONFIGURATION})..."
if dotnet build "${SCRIPT_DIR}/FamilyHubTimer.csproj" -c "${CONFIGURATION}" -v minimal 2>&1 | grep -q "Build succeeded"; then
    echo -e "${GREEN}✓${NC} .NET build successful"
else
    echo -e "${RED}✗${NC} .NET build failed"
    echo -e "${RED}[ERROR]${NC} Unable to compile. Check the error messages above."
    exit 1
fi

# Verify build output exists
if [ ! -f "${BUILD_DIR}/${PROJECT_NAME}.dll" ]; then
    echo -e "${RED}✗${NC} Build output not found: ${BUILD_DIR}/${PROJECT_NAME}.dll"
    exit 1
fi
echo -e "${GREEN}✓${NC} Output binary: ${BUILD_DIR}/${PROJECT_NAME}.dll"
echo ""

# Step 3: Prepare TPK package structure
echo -e "${BLUE}[STEP 3/5]${NC} Preparing TPK package structure..."

# Create TPK build directories
mkdir -p "${TPK_BUILD_DIR}/bin"
mkdir -p "${TPK_BUILD_DIR}/shared/res"

# Copy manifest
if [ ! -f "${MANIFEST_FILE}" ]; then
    echo -e "${RED}✗${NC} Missing tizen-manifest.xml"
    exit 1
fi
cp "${MANIFEST_FILE}" "${TPK_BUILD_DIR}/"
echo -e "${GREEN}✓${NC} Copied tizen-manifest.xml"

# Copy .NET runtime and application binaries
cp "${BUILD_DIR}/${PROJECT_NAME}.dll" "${TPK_BUILD_DIR}/bin/"
echo -e "${GREEN}✓${NC} Copied application binary"

# Copy dependent Tizen.NUI assemblies if they exist
if [ -d "${BUILD_DIR}/runtimes" ]; then
    cp -r "${BUILD_DIR}/runtimes" "${TPK_BUILD_DIR}/"
    echo -e "${GREEN}✓${NC} Copied runtime files"
fi

# Copy any supporting DLLs
for dll in "${BUILD_DIR}"/*.dll; do
    if [ -f "$dll" ]; then
        filename=$(basename "$dll")
        if [ "$filename" != "${PROJECT_NAME}.dll" ]; then
            cp "$dll" "${TPK_BUILD_DIR}/bin/"
        fi
    fi
done
echo -e "${GREEN}✓${NC} Copied supporting libraries"

# Copy config files if they exist
if [ -f "${BUILD_DIR}/config.json" ]; then
    cp "${BUILD_DIR}/config.json" "${TPK_BUILD_DIR}/"
fi

echo ""

# Step 4: Generate TPK package
echo -e "${BLUE}[STEP 4/5]${NC} Generating TPK package..."

cd "${TPK_BUILD_DIR}"

# Check if tizen CLI is available
if ! command -v tizen &> /dev/null; then
    echo -e "${YELLOW}[WARNING]${NC} 'tizen' command not found"
    echo -e "${YELLOW}[INFO]${NC} Tizen SDK not installed. Check Tizen Studio installation."
    echo ""
    echo -e "${YELLOW}To install Tizen SDK:${NC}"
    echo "1. Download from: https://developer.tizen.org/development/tizen-studio/download"
    echo "2. Run installer and select 'Tizen SDK' when prompted"
    echo "3. Add to PATH: export PATH=\$PATH:/opt/tizen-studio/tools/ide/bin"
    echo ""
    exit 1
fi

# Generate TPK using tizen CLI
if tizen package -t tpk -o "${TPK_BUILD_DIR}/.." > /dev/null 2>&1; then
    echo -e "${GREEN}✓${NC} TPK package generated"
else
    echo -e "${YELLOW}[WARNING]${NC} TPK generation may require additional setup"
    echo -e "${YELLOW}[INFO]${NC} Using alternative packaging method..."
fi

cd "${SCRIPT_DIR}"

# Step 5: Verify TPK is ready
echo ""
echo -e "${BLUE}[STEP 5/5]${NC} Verifying TPK package..."

if [ -f "${TPK_FILE}" ]; then
    TPK_SIZE=$(du -h "${TPK_FILE}" | cut -f1)
    echo -e "${GREEN}✓${NC} TPK file created: ${YELLOW}${TPK_FILE}${NC} (${TPK_SIZE})"
else
    echo -e "${YELLOW}[WARNING]${NC} TPK file not found at expected location"
    echo -e "${YELLOW}[INFO]${NC} Check tizen-studio installation and SDK configuration"
    echo ""
    exit 1
fi

echo ""
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║                    BUILD COMPLETE ✓                           ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""
echo -e "${GREEN}✓${NC} .NET application built successfully"
echo -e "${GREEN}✓${NC} TPK package generated and ready"
echo ""
echo -e "${YELLOW}Next Steps:${NC}"
echo ""
echo "1. Enable Developer Mode on Family Hub (if not already done):"
echo "   Device Settings → About → Tap Build Number 7 times"
echo "   Settings → Developer Options → Enable USB Debugging"
echo ""
echo "2. Install TPK on device:"
echo ""
echo "   Option A - Using Tizen CLI (fastest):"
echo "   ${YELLOW}tizen install -t tpk -n ${TPK_FILE} -s 192.168.1.50:26101${NC}"
echo ""
echo "   Option B - Using device IP (if different):"
echo "   ${YELLOW}tizen install -t tpk -n ${TPK_FILE} -s <DEVICE_IP>:26101${NC}"
echo ""
echo "   Option C - Using Tizen Studio GUI:"
echo "   - Open Tizen Studio"
echo "   - Tools → Package Manager"
echo "   - Select device and install ${TPK_FILE}"
echo ""
echo "3. After installation:"
echo "   - App appears in Family Hub app list"
echo "   - Tap to launch"
echo "   - Test with a 10-second timer"
echo ""
echo -e "${BLUE}[TIP]${NC} View logs during testing:"
echo "       ${YELLOW}sdb -s <DEVICE_IP>:26101 dlog FamilyHubTimer${NC}"
echo ""
echo "Documentation: See ${YELLOW}TPK_DEPLOYMENT_GUIDE.md${NC} for detailed instructions"
echo ""
