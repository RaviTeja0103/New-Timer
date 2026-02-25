# 🔨 Build & Package Guide

## Quick Start

### One-Command Build & Package

The project includes automated build-and-package scripts that:
1. ✅ Clean previous builds
2. ✅ Build the .NET application
3. ✅ Generate the TPK package
4. ✅ Verify the output
5. ✅ Show deployment instructions

### macOS/Linux

```bash
./build-and-package.sh
```

**Output**: `FamilyHubTimer.tpk` (ready to install)

### Windows PowerShell

```powershell
.\build-and-package.ps1
```

**Output**: `FamilyHubTimer.tpk` (ready to install)

---

## 📋 What Happens During Build & Package

### Step 1: Clean (30 seconds)
- Removes previous `/bin` directory
- Removes previous TPK artifacts
- Ensures fresh build

### Step 2: Build .NET (15-30 seconds)
```bash
dotnet build -c Release
```
- Compiles all C# source files
- Links Tizen.NUI libraries
- Produces: `bin/Release/net8.0/FamilyHubTimer.dll` (~2-3 MB)

### Step 3: Prepare Package (5 seconds)
- Creates TPK directory structure
- Copies manifest file
- Copies application binary
- Copies supporting libraries

### Step 4: Generate TPK (10-20 seconds)
```bash
tizen package -t tpk
```
- Packages application for Tizen
- Produces: `FamilyHubTimer.tpk` (3-5 MB)
- Ready for device installation

### Step 5: Verify (5 seconds)
- Confirms TPK file exists
- Displays deployment instructions
- Shows next steps

**Total time: 2-4 minutes**

---

## 📁 Build Artifacts

After running `./build-and-package.sh`:

```
/Users/ritadas/Downloads/Ravi/
├── FamilyHubTimer.tpk            ← INSTALL THIS FILE
├── bin/Release/net8.0/
│   ├── FamilyHubTimer.dll        ← Compiled app
│   └── ...dependencies
├── tpk_build/                     ← Temporary (can delete)
│   ├── bin/
│   ├── tizen-manifest.xml
│   └── shared/
└── [other project files]
```

**Important**:
- `FamilyHubTimer.tpk` - This is what you install on device
- `tpk_build/` - Temporary directory (auto-cleaned on next build)

---

## 🎯 Build Variants

### Release Build (Default)
```bash
./build-and-package.sh Release
```
- Optimized for performance
- Smaller file size
- Use this for production

### Debug Build
```bash
./build-and-package.sh Debug
```
- Includes debug symbols
- Slower, larger file
- Use for troubleshooting
- More detailed logs

---

## 🖥️ VS Code Integration

### Using VS Code Tasks

Press `Ctrl+Shift+B` (or `Cmd+Shift+B` on macOS):

**Available tasks:**

1. **Build (dotnet)** - Quick .NET build only
2. **Build & Package TPK** - Full workflow (your main option)
3. **Clean Build Artifacts** - Remove build files
4. **Install TPK on Device** - Auto-install after build
5. **View Device Logs** - Watch real-time logs
6. **Connect to Device** - Test device connectivity

### Running a Task

1. `Ctrl+Shift+B` → Select "Build & Package TPK"
2. Wait for completion
3. See deployment instructions
4. Ready to install!

---

## ⚙️ Prerequisites

### Required

- **dotnet 8.0 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Tizen Studio**: [Download](https://developer.tizen.org/development/tizen-studio/download)

### Tizen Studio Setup

1. Download Tizen Studio
2. Extract and run installer
3. When prompted, select **Tizen SDK**
4. After installation, add to PATH:

   **macOS/Linux:**
   ```bash
   export PATH=$PATH:/opt/tizen-studio/tools/ide/bin
   ```

   **Windows:**
   ```
   Add C:\tizen-studio\tools\ide\bin to PATH environment variable
   ```

5. Verify installation:
   ```bash
   tizen --version
   ```

---

## 🔍 Troubleshooting

### ❌ "tizen: command not found"

**Solution**: Tizen Studio not installed or not in PATH

```bash
# macOS/Linux - Add to ~/.bashrc or ~/.zshrc
export PATH=$PATH:/opt/tizen-studio/tools/ide/bin
source ~/.bashrc

# Verify
tizen --version
```

### ❌ "Build failed: CS0103 'X' does not exist"

**Solution**: Missing dependencies or Tizen.NUI not found

```bash
# Clean and rebuild
rm -rf bin obj
dotnet restore
dotnet build -c Release
```

### ❌ "tizen package failed"

**Solution**: Check Tizen SDK configuration

```bash
# Verify Tizen SDK
tizen list-sdk

# Update SDK if needed
tizen update-sdk
```

### ❌ "FamilyHubTimer.tpk not created"

**Solution**: Check script output for errors

- Look for `[ERROR]` or `[WARNING]` messages
- Verify Tizen CLI is working: `tizen --version`
- Check file permissions: `ls -la build-and-package.sh`

---

## 📊 Build Configuration

### Project Structure

```
FamilyHubTimer/
├── FamilyHubTimer.csproj          ← Project file
├── tizen-manifest.xml             ← App manifest
├── MainWindow.cs                  ← Entry point
├── Models/
│   └── TimerModel.cs
├── Services/
│   ├── TimerService.cs
│   ├── PersistenceService.cs
│   └── NotificationService.cs
├── Views/
│   ├── MainSetupView.cs
│   ├── TimerListView.cs
│   └── TimerRunningView.cs
├── Utils/
│   └── AppConstants.cs
├── build-and-package.sh           ← Build script (macOS/Linux)
├── build-and-package.ps1          ← Build script (Windows)
└── .vscode/tasks.json             ← VS Code tasks
```

### Project File (FamilyHubTimer.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Tizen.NUI" Version="0.2.43" />
  </ItemGroup>
</Project>
```

---

## ✅ Build Success Indicators

When build is successful, you'll see:

```
╔════════════════════════════════════════════════════════════════╗
║                    BUILD COMPLETE ✓                           ║
╚════════════════════════════════════════════════════════════════╝

✓ .NET application built successfully
✓ TPK package generated and ready
```

**File created**: `FamilyHubTimer.tpk`

---

## 🚀 Next Steps After Building

### 1. Enable Device Developer Mode

On your Samsung Family Hub:
```
Settings → About Device → Build Number (tap 7 times)
Settings → Developer Options → Enable USB Debugging
```

### 2. Install TPK

```bash
# Using Tizen CLI (fastest)
tizen install -t tpk -n FamilyHubTimer.tpk -s 192.168.1.50:26101

# Or using VS Code task
Ctrl+Shift+B → Select "Install TPK on Device"
```

### 3. Test

- App appears in Family Hub app list
- Tap to launch
- Create a 10-second timer
- Verify countdown and alerts work

### 4. View Logs (optional)

```bash
sdb -s 192.168.1.50:26101 dlog FamilyHubTimer
```

---

## 📝 Advanced Options

### Custom Device IP

```bash
./build-and-package.sh Release
# Then when prompted, use your device's actual IP
tizen install -t tpk -n FamilyHubTimer.tpk -s YOUR.DEVICE.IP:26101
```

### Just Build (No Package)

```bash
dotnet build -c Release
```

### Just Package (Skip Build)

```bash
# If binary already exists
cd tpk_build
tizen package -t tpk
```

### Full Clean + Rebuild

```bash
./build-and-package.sh Release
# Includes: clean + build + package
```

---

## 🔗 Related Documentation

- [TPK_DEPLOYMENT_GUIDE.md](TPK_DEPLOYMENT_GUIDE.md) - Installation guide
- [TIZEN_NATIVE_GUIDE.md](TIZEN_NATIVE_GUIDE.md) - Architecture guide
- [README.md](README.md) - Project overview
- [ARCHITECTURE.md](ARCHITECTURE.md) - Technical design
- [API_REFERENCE.md](API_REFERENCE.md) - Code reference

---

## 💡 Tips

1. **Fast iteration**: `./build-and-package.sh` handles everything
2. **VS Code users**: Use `Ctrl+Shift+B` for quick access
3. **Check logs**: `sdb dlog` helps debug issues
4. **Device IP**: Usually `192.168.1.50` for Family Hub
5. **Multiple devices**: Change IP in tasks.json for different devices

---

## 📞 Support

If you encounter issues:

1. Check **Troubleshooting** section above
2. Review `tizen --version` (ensure it's installed)
3. Check device connectivity: `sdb devices`
4. View full build output (no `-v minimal` flag)
5. See **TPK_DEPLOYMENT_GUIDE.md** for detailed help

---

**Build script version**: 2.0  
**Last updated**: February 25, 2026  
**Status**: ✅ Production Ready
