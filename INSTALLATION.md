# Installing VibeProxy for Windows

**⚠️ Requirements:** Windows 10 or later with .NET Desktop Runtime 8.0+

## Option 1: Download Pre-built Release (Recommended)

### Step 1: Download

1. Go to the [**Releases**](https://github.com/automazeio/vibeproxy/releases) page
2. Download the latest Windows release package (`VibeProxy-Windows-vX.X.X.zip`)
3. Extract the ZIP file to a folder of your choice

### Step 2: Install .NET Runtime (if needed)

VibeProxy requires .NET Desktop Runtime 8.0 or later.

**Check if you have it:**
```powershell
dotnet --list-runtimes
```

**If you need to install it:**
1. Download from [Microsoft .NET Download](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Select ".NET Desktop Runtime 8.0" for Windows
3. Run the installer

### Step 3: Launch

1. Navigate to the extracted folder
2. Double-click `VibeProxy.Windows.exe` to launch
3. If Windows SmartScreen appears, click "More info" → "Run anyway"

---

## Option 2: Build from Source

### Prerequisites

- Windows 10 or later
- .NET SDK 8.0 or later
- PowerShell 7+ (`pwsh`)
- Git

### Install Prerequisites

1. **Install .NET SDK 8.0**
   - Download from [Microsoft .NET Download](https://dotnet.microsoft.com/download/dotnet/8.0)
   - Run the installer

2. **Install PowerShell 7+ (if needed)**
   ```powershell
   winget install Microsoft.PowerShell
   ```

3. **Install Git (if needed)**
   ```powershell
   winget install Git.Git
   ```

### Build Instructions

1. **Clone the repository**
   ```powershell
   git clone https://github.com/automazeio/vibeproxy.git
   cd vibeproxy
   ```

2. **Build the application**
   ```powershell
   # Using PowerShell script directly
   pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/build-windows.ps1 -Configuration Release

   # Or using Make (if you have make installed)
   make release
   ```

   This will:
   - Build the Windows WPF application
   - Download and bundle CLIProxyAPI
   - Create a self-contained executable
   - Output to `out/publish/`

3. **Run the application**
   ```powershell
   cd out/publish
   .\VibeProxy.Windows.exe
   ```

### Build Commands

```powershell
# Build in Release mode (recommended)
make release

# Build in Debug mode
make build

# Clean build artifacts
make clean

# Show project info
make info
```

### Build Options

The build script supports various configurations:

```powershell
# Release build (optimized, smaller size)
pwsh scripts/build-windows.ps1 -Configuration Release

# Debug build (with debugging symbols)
pwsh scripts/build-windows.ps1 -Configuration Debug
```

---

## Verifying Downloads

Before installing any downloaded app, verify its authenticity:

### 1. Download from Official Source

Only download from the official [GitHub Releases](https://github.com/automazeio/vibeproxy/releases) page.

### 2. Verify Checksum (Optional)

Each release includes SHA-256 checksums:

```powershell
# Download the checksum file
curl -LO https://github.com/automazeio/vibeproxy/releases/download/vX.X.X/VibeProxy-Windows.zip.sha256

# Verify the download (PowerShell)
$hash = (Get-FileHash VibeProxy-Windows.zip -Algorithm SHA256).Hash
$expected = (Get-Content VibeProxy-Windows.zip.sha256).Split(' ')[0]
if ($hash -eq $expected) { Write-Host "✓ Checksum verified!" } else { Write-Host "✗ Checksum mismatch!" }
```

### 3. Inspect the Code

All source code is available in this repository - feel free to review before building.

---

## Troubleshooting

### "Windows protected your PC" (SmartScreen)

This is normal for new applications. Click "More info" → "Run anyway"

### .NET Runtime Not Found

**Error**: "To run this application, you must install .NET Desktop Runtime"

**Solution**:
1. Download [.NET Desktop Runtime 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Install the x64 version
3. Restart your computer
4. Try launching VibeProxy again

### Build Fails

**Error: dotnet not found**
```powershell
# Install .NET SDK
winget install Microsoft.DotNet.SDK.8
```

**Error: pwsh not found**
```powershell
# Install PowerShell 7+
winget install Microsoft.PowerShell
```

**Error: MSBuild failed**
- Make sure you have .NET SDK 8.0 or later installed
- Try cleaning: `make clean` then rebuild

### Port 8317 Already in Use

If you get an error about port 8317 being in use:

```powershell
# Find what's using the port
netstat -ano | findstr :8317

# Kill the process (replace PID with actual process ID)
taskkill /PID <PID> /F
```

### Still Having Issues?

- **Check System Requirements**: Windows 10 or later, .NET 8.0+
- **Check Event Viewer**: Look for application errors in Windows Event Viewer
- **Report an Issue**: [GitHub Issues](https://github.com/automazeio/vibeproxy/issues)

---

## Running on Startup (Optional)

To have VibeProxy start automatically with Windows:

1. Press `Win + R`
2. Type `shell:startup` and press Enter
3. Create a shortcut to `VibeProxy.Windows.exe` in this folder

Or use the "Launch at Startup" option in the VibeProxy settings window.

---

**Questions?** Open an [issue](https://github.com/automazeio/vibeproxy/issues) or check the [README](README.md).
