# Installing SharpAI on macOS ARM64 (Apple Silicon)

Complete step-by-step installation and troubleshooting guide for macOS with Apple Silicon (M1, M2, M3, M4).

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Installation Steps](#installation-steps)
3. [Post-Installation Fix (Required)](#post-installation-fix-required)
4. [Verification](#verification)
5. [Troubleshooting](#troubleshooting)
6. [Common Issues and Solutions](#common-issues-and-solutions)
7. [Performance Notes](#performance-notes)

---

## Prerequisites

### System Requirements

- **Hardware**: Mac with Apple Silicon (M1, M2, M3, M4)
- **macOS**: Version 12.0 (Monterey) or later
- **RAM**: 8GB minimum, 16GB+ recommended
- **Disk Space**: 10GB+ free space (for models)
- **Internet**: Required for downloading models

### Required Software

#### 1. Xcode Command Line Tools

Required for `install_name_tool` and system libraries.

```bash
# Check if already installed
xcode-select -p

# If not installed, install it:
xcode-select --install
```

Click "Install" in the dialog that appears and wait for completion.

**Verification:**
```bash
xcode-select -p
# Should output: /Library/Developer/CommandLineTools
```

#### 2. .NET 8 SDK

**Option A: Download from Microsoft (Recommended)**

1. Visit: https://dotnet.microsoft.com/download/dotnet/8.0
2. Download: **macOS ARM64 Installer**
3. Run the installer package
4. Follow installation wizard

**Option B: Using Homebrew**

```bash
brew install dotnet@8
```

**Verification:**
```bash
dotnet --version
# Should output: 8.0.xxx

dotnet --info | grep RID
# Should output: RID: osx-arm64
```

**IMPORTANT**: Ensure you're using the **ARM64 version** of .NET, not the x64 version. The RID should be `osx-arm64`.

#### 3. Git

```bash
# Check if installed
git --version

# If not installed, install via Xcode Command Line Tools (step 1)
# Or via Homebrew:
brew install git
```

---

## Installation Steps

### Step 1: Clone the Repository

```bash
# Navigate to your code directory
cd ~
mkdir -p Code
cd Code

# Clone SharpAI
git clone https://github.com/jchristn/sharpai.git
cd sharpai
```

### Step 2: Build the Project

```bash
# Navigate to source directory
cd src

# Clean any previous builds
dotnet clean SharpAI.sln

# Build the solution
dotnet build SharpAI.sln
```

**Expected output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**If build fails**, see [Troubleshooting - Build Failures](#build-failures).

### Step 3: Verify Binary Output

```bash
# Navigate to output directory
cd SharpAI.Server/bin/Debug/net8.0

# Check if binary exists
ls -lh SharpAI.Server.dll

# Check native libraries
ls -lh runtimes/osx-arm64/native/
```

**You should see:**
- `SharpAI.Server.dll` (main application)
- `runtimes/osx-arm64/native/libllama.dylib` (main native library)
- Other `libggml*.dylib` files (dependencies)

---

## Post-Installation Fix (Required)

**CRITICAL**: Due to a packaging issue in LlamaSharp, you must fix the library path references before running.

### Why This Is Needed

The `libllama.dylib` library uses `@rpath` references to find its dependencies (`libggml*.dylib` files). .NET's P/Invoke doesn't resolve these paths correctly, causing the library to fail loading.

We need to change `@rpath` to `@loader_path`, which tells macOS to look in the same directory as the library.

### Automated Fix Script

```bash
cd ~/Code/sharpai
chmod +x fix-mac-arm64-paths.sh
./fix-mac-arm64-paths.sh
```

### Manual Fix (If Script Doesn't Exist)

```bash
# Navigate to native library directory
cd ~/Code/sharpai/src/SharpAI.Server/bin/Debug/net8.0/runtimes/osx-arm64/native

# Check current dependencies (should show @rpath)
otool -L libllama.dylib

# Fix each @rpath reference to use @loader_path
install_name_tool -change @rpath/libggml.dylib @loader_path/libggml.dylib libllama.dylib
install_name_tool -change @rpath/libggml-cpu.dylib @loader_path/libggml-cpu.dylib libllama.dylib
install_name_tool -change @rpath/libggml-blas.dylib @loader_path/libggml-blas.dylib libllama.dylib
install_name_tool -change @rpath/libggml-metal.dylib @loader_path/libggml-metal.dylib libllama.dylib
install_name_tool -change @rpath/libggml-base.dylib @loader_path/libggml-base.dylib libllama.dylib

# Verify the fix (should now show @loader_path)
otool -L libllama.dylib
```

**Expected output after fix:**
```
libllama.dylib:
    @rpath/libllama.dylib (compatibility version 0.0.0, current version 0.0.0)
    @loader_path/libggml.dylib (compatibility version 0.0.0, current version 0.0.0)
    @loader_path/libggml-cpu.dylib (compatibility version 0.0.0, current version 0.0.0)
    @loader_path/libggml-blas.dylib (compatibility version 0.0.0, current version 0.0.0)
    @loader_path/libggml-metal.dylib (compatibility version 0.0.0, current version 0.0.0)
    @loader_path/libggml-base.dylib (compatibility version 0.0.0, current version 0.0.0)
    /usr/lib/libc++.1.dylib (compatibility version 1.0.0, current version 1700.255.5)
    /usr/lib/libSystem.B.dylib (compatibility version 1.0.0, current version 1345.120.2)
```

✅ All `libggml*.dylib` references should now show `@loader_path` instead of `@rpath`.

### Create Automated Fix Script

Save this for future rebuilds:

```bash
cat > ~/Code/sharpai/fix-mac-arm64-paths.sh << 'SCRIPT_EOF'
#!/bin/bash
#
# Fix macOS ARM64 Library Path References
# Run this after every rebuild
#

set -e

NATIVE_DIR="$HOME/Code/sharpai/src/SharpAI.Server/bin/Debug/net8.0/runtimes/osx-arm64/native"
LIB_FILE="$NATIVE_DIR/libllama.dylib"

echo "================================================"
echo "Fixing macOS ARM64 Library Path References"
echo "================================================"
echo ""

if [ ! -f "$LIB_FILE" ]; then
    echo "ERROR: Library not found: $LIB_FILE"
    echo ""
    echo "Please build the project first:"
    echo "  cd ~/Code/sharpai/src"
    echo "  dotnet build SharpAI.sln"
    exit 1
fi

echo "Library: $LIB_FILE"
echo ""
echo "Fixing @rpath references..."

install_name_tool -change @rpath/libggml.dylib @loader_path/libggml.dylib "$LIB_FILE" 2>/dev/null || true
install_name_tool -change @rpath/libggml-cpu.dylib @loader_path/libggml-cpu.dylib "$LIB_FILE" 2>/dev/null || true
install_name_tool -change @rpath/libggml-blas.dylib @loader_path/libggml-blas.dylib "$LIB_FILE" 2>/dev/null || true
install_name_tool -change @rpath/libggml-metal.dylib @loader_path/libggml-metal.dylib "$LIB_FILE" 2>/dev/null || true
install_name_tool -change @rpath/libggml-base.dylib @loader_path/libggml-base.dylib "$LIB_FILE" 2>/dev/null || true

echo ""
echo "Verifying fix..."
if otool -L "$LIB_FILE" | grep -q "@loader_path"; then
    echo "✅ SUCCESS: Library paths fixed!"
    echo ""
    echo "Dependencies now using @loader_path:"
    otool -L "$LIB_FILE" | grep "@loader_path"
else
    echo "⚠️  WARNING: No @loader_path references found"
    echo "This may be normal if paths were already fixed or use absolute paths"
fi

echo ""
echo "================================================"
echo "You can now run the server:"
echo "  cd ~/Code/sharpai/src/SharpAI.Server"
echo "  dotnet run"
echo "================================================"
SCRIPT_EOF

chmod +x ~/Code/sharpai/fix-mac-arm64-paths.sh
```

---

## Verification

### Step 1: Start the Server

```bash
cd ~/Code/sharpai/src/SharpAI.Server
dotnet run
```

### Step 2: Check Startup Logs

**Look for these successful initialization messages:**

```
[NativeLibraryBootstrapper] detected platform: OSX, architecture: Arm64
[NativeLibraryBootstrapper] Apple Silicon detected, GPU backend not supported, using CPU
[NativeLibraryBootstrapper] using ARM64 CPU backend
[NativeLibraryBootstrapper] found library at NuGet path: .../osx-arm64/native/libllama.dylib
[NativeLibraryBootstrapper] configuring cpu backend: ...
[NativeLibraryBootstrapper] successfully configured cpu backend
[NativeLibraryBootstrapper] library loaded successfully, 0 device(s) reported
[NativeLibraryBootstrapper] native library logging disabled
[SharpAI] starting SharpAI server
```

✅ **Success indicators:**
- No warnings about "failed to configure cpu backend"
- "library loaded successfully" message appears
- Server starts without exceptions

❌ **Failure indicators:**
- "failed to configure cpu backend, will attempt fallback"
- TypeInitializationException errors
- "The native library cannot be correctly loaded"

If you see failures, see [Troubleshooting](#troubleshooting) section.

### Step 3: Test API Endpoint

In a **new terminal window**:

```bash
# Test the homepage
curl http://localhost:8000/

# Should return HTML with SharpAI branding
```

### Step 4: Pull a Test Model

```bash
# Pull a small model for testing
curl -X POST http://localhost:8000/api/pull \
  -H "Content-Type: application/json" \
  -d '{"name":"leliuga/all-MiniLM-L6-v2-GGUF"}'

# Wait for download to complete (shows progress)
```

### Step 5: Test Embeddings

```bash
# Generate embeddings
curl -X POST http://localhost:8000/api/embed \
  -H "Content-Type: application/json" \
  -d '{"model":"leliuga/all-MiniLM-L6-v2-GGUF","input":"Hello, World!"}'

# Should return JSON with embeddings array
```

✅ **If all tests pass, installation is complete!**

---

## Troubleshooting

### Diagnostic Tools

#### Tool 1: Check Library Dependencies

```bash
cd ~/Code/sharpai/src/SharpAI.Server/bin/Debug/net8.0/runtimes/osx-arm64/native

# List all libraries
ls -lh *.dylib

# Check libllama.dylib dependencies
otool -L libllama.dylib

# Check architecture
file libllama.dylib
lipo -info libllama.dylib
```

**What to look for:**
- ✅ All `libggml*.dylib` files exist in the directory
- ✅ `libllama.dylib` is ARM64 architecture
- ✅ Dependencies use `@loader_path` (not `@rpath`)

#### Tool 2: Test Library Loading

```bash
# Create simple test program
cat > /tmp/test_load.c << 'EOF'
#include <stdio.h>
#include <dlfcn.h>

int main(int argc, char *argv[]) {
    if (argc != 2) {
        fprintf(stderr, "Usage: %s <library_path>\n", argv[0]);
        return 1;
    }

    void *handle = dlopen(argv[1], RTLD_NOW);
    if (!handle) {
        fprintf(stderr, "dlopen() failed: %s\n", dlerror());
        return 1;
    }

    printf("✅ Library loaded successfully\n");
    dlclose(handle);
    return 0;
}
EOF

# Compile
cc -o /tmp/test_load /tmp/test_load.c

# Test
/tmp/test_load ~/Code/sharpai/src/SharpAI.Server/bin/Debug/net8.0/runtimes/osx-arm64/native/libllama.dylib
```

**Expected**: `✅ Library loaded successfully`

**If it fails**, the error message will tell you which dependency is missing.

#### Tool 3: Run Full Diagnostic

```bash
cd ~/Code/sharpai
chmod +x diagnose-mac.sh
./diagnose-mac.sh
```

Review the output for missing dependencies or issues.

---

## Common Issues and Solutions

### Issue 1: "Library configured but failed to load"

**Symptom:**
```
[NativeLibraryBootstrapper] failed to configure cpu backend, will attempt fallback
TypeInitializationException: The type initializer for 'LLama.Native.NativeApi' threw an exception
RuntimeError: The native library cannot be correctly loaded
```

**Cause:** Library path references not fixed.

**Solution:**

1. **Run the fix script:**
   ```bash
   cd ~/Code/sharpai
   ./fix-mac-arm64-paths.sh
   ```

2. **Or manually fix** (see [Post-Installation Fix](#post-installation-fix-required))

3. **Verify the fix:**
   ```bash
   cd ~/Code/sharpai/src/SharpAI.Server/bin/Debug/net8.0/runtimes/osx-arm64/native
   otool -L libllama.dylib | grep libggml
   ```
   Should show `@loader_path/libggml...` not `@rpath/libggml...`

---

### Issue 2: Missing libggml*.dylib Files

**Symptom:**
```
dlopen() failed: Library not loaded: @loader_path/libggml.dylib
Reason: image not found
```

**Cause:** Dependency libraries weren't copied during build.

**Solution:**

1. **Find the missing libraries:**
   ```bash
   find ~/Code/sharpai/src/SharpAI.Server/bin -name "libggml*.dylib" -type f
   ```

2. **If found in a different directory, copy them:**
   ```bash
   # Example (adjust source path based on find results):
   cp ~/Code/sharpai/src/SharpAI.Server/bin/Debug/net8.0/runtimes/osx-arm64/native-temp/libggml*.dylib \
      ~/Code/sharpai/src/SharpAI.Server/bin/Debug/net8.0/runtimes/osx-arm64/native/
   ```

3. **If not found anywhere, search NuGet cache:**
   ```bash
   NUGET_DIR=$(dotnet nuget locals global-packages --list | cut -d ' ' -f 2)
   find "$NUGET_DIR/llamasharp.backend.cpu" -name "libggml*.dylib" -type f
   ```

4. **Copy from NuGet cache:**
   ```bash
   # Example (adjust path):
   cp /path/from/find/libggml*.dylib \
      ~/Code/sharpai/src/SharpAI.Server/bin/Debug/net8.0/runtimes/osx-arm64/native/
   ```

5. **Re-run the path fix:**
   ```bash
   ~/Code/sharpai/fix-mac-arm64-paths.sh
   ```

---

### Issue 3: Wrong Architecture

**Symptom:**
```
lipo -info libllama.dylib
# Shows: x86_64 instead of arm64
```

**Cause:** Using x64 version of .NET or libraries.

**Solution:**

1. **Check .NET architecture:**
   ```bash
   dotnet --info | grep RID
   ```
   Should be: `RID: osx-arm64`

2. **If showing `osx-x64`:**
   - Uninstall x64 .NET SDK
   - Download and install ARM64 version from https://dotnet.microsoft.com/download/dotnet/8.0
   - Choose **macOS ARM64 Installer**

3. **Rebuild after installing correct SDK:**
   ```bash
   cd ~/Code/sharpai/src
   dotnet clean SharpAI.sln
   dotnet build SharpAI.sln
   ~/Code/sharpai/fix-mac-arm64-paths.sh
   ```

---

### Issue 4: Xcode Command Line Tools Not Installed

**Symptom:**
```
install_name_tool: command not found
```

**Solution:**

```bash
# Install Xcode Command Line Tools
xcode-select --install

# Click "Install" in the dialog

# Verify installation
xcode-select -p
# Should output: /Library/Developer/CommandLineTools

# Verify install_name_tool is available
which install_name_tool
# Should output: /usr/bin/install_name_tool
```

---

### Issue 5: Permission Denied

**Symptom:**
```
install_name_tool: can't open file: libllama.dylib (Permission denied)
```

**Solution:**

```bash
# Check file permissions
ls -l ~/Code/sharpai/src/SharpAI.Server/bin/Debug/net8.0/runtimes/osx-arm64/native/libllama.dylib

# Add write permission if needed
chmod u+w ~/Code/sharpai/src/SharpAI.Server/bin/Debug/net8.0/runtimes/osx-arm64/native/*.dylib

# Re-run fix script
~/Code/sharpai/fix-mac-arm64-paths.sh
```

---

### Issue 6: Build Failures

**Symptom:**
```
dotnet build SharpAI.sln
# Error: ... NuGet package restore failed
```

**Solution:**

1. **Clear NuGet cache:**
   ```bash
   dotnet nuget locals all --clear
   ```

2. **Restore packages explicitly:**
   ```bash
   cd ~/Code/sharpai/src
   dotnet restore SharpAI.sln
   ```

3. **Try build again:**
   ```bash
   dotnet build SharpAI.sln
   ```

4. **If still failing, check .NET installation:**
   ```bash
   dotnet --version
   # Should be 8.0.xxx

   dotnet --info
   # Check for errors in output
   ```

---

### Issue 7: Port Already in Use

**Symptom:**
```
Failed to bind to address http://localhost:8000
Address already in use
```

**Solution:**

1. **Find process using port 8000:**
   ```bash
   lsof -i :8000
   ```

2. **Kill the process:**
   ```bash
   kill <PID>
   ```

3. **Or change the port in settings:**
   ```bash
   cd ~/Code/sharpai/src/SharpAI.Server
   cat > sharpai.json << 'EOF'
{
  "Rest": {
    "Port": 8080
  },
  "Runtime": {
    "EnableNativeLogging": false
  }
}
EOF
   ```

4. **Run on new port:**
   ```bash
   dotnet run
   # Now accessible at http://localhost:8080
   ```

---

### Issue 8: Server Starts But Model Operations Fail

**Symptom:**
- Server starts successfully
- API endpoints respond
- But model pull or inference fails with library errors

**Cause:** Library loaded initially but fails when actually used.

**Solution:**

1. **Check full error in logs**

2. **Verify all dependencies are present:**
   ```bash
   cd ~/Code/sharpai/src/SharpAI.Server/bin/Debug/net8.0/runtimes/osx-arm64/native
   ls -1 *.dylib
   ```

   Should see:
   ```
   libggml-base.dylib
   libggml-blas.dylib
   libggml-cpu.dylib
   libggml-metal.dylib
   libggml.dylib
   libllama.dylib
   ```

3. **Test each library loads:**
   ```bash
   for lib in *.dylib; do
       echo "Testing: $lib"
       /tmp/test_load "$PWD/$lib" || echo "FAILED: $lib"
   done
   ```

4. **Fix any failing libraries**

---

## Performance Notes

### Apple Silicon GPU Acceleration

**Note:** While Apple Silicon has powerful GPUs (Metal), the current LlamaSharp/llama.cpp backend does NOT utilize Metal acceleration when run through .NET on macOS.

**What this means:**
- ✅ CPU inference works (using ARM64 optimized code)
- ❌ GPU acceleration via Metal is not available through SharpAI on macOS
- ℹ️ CPU performance on Apple Silicon is still quite good for smaller models

**For GPU acceleration on macOS**, you would need:
- Native llama.cpp with Metal support (outside of .NET)
- Or wait for LlamaSharp to add proper Metal backend support through .NET

### Performance Expectations

**On M1/M2/M3 Pro or Max (CPU only):**
- Small models (<1B params): Fast, real-time inference
- Medium models (1-7B params): Good performance
- Large models (7B+ params): Slow, may require quantization

**Recommendations:**
- Use Q4_K_M or Q5_K_M quantized models for best size/quality ratio
- Stick to models <7B parameters for good CPU performance
- Close other applications to free up RAM

### Monitoring Performance

```bash
# Monitor CPU usage
top -o cpu

# Monitor memory usage
top -o mem

# Watch specific process
top -pid $(pgrep -f SharpAI.Server)
```

---

## After Installation

### Running the Server

```bash
# Navigate to server directory
cd ~/Code/sharpai/src/SharpAI.Server

# Start server
dotnet run

# Server will start on http://localhost:8000
```

### Stopping the Server

Press `Ctrl+C` in the terminal where the server is running.

### Updating SharpAI

```bash
# Navigate to repository
cd ~/Code/sharpai

# Pull latest changes
git pull origin main

# Rebuild
cd src
dotnet clean SharpAI.sln
dotnet build SharpAI.sln

# IMPORTANT: Re-run the path fix after rebuild
cd ~/Code/sharpai
./fix-mac-arm64-paths.sh

# Start server
cd src/SharpAI.Server
dotnet run
```

**Always run `fix-mac-arm64-paths.sh` after rebuilding!**

### Configuration

Configuration file: `~/Code/sharpai/src/SharpAI.Server/sharpai.json`

**Example configuration:**
```json
{
  "Runtime": {
    "ForceBackend": "cpu",
    "EnableNativeLogging": false
  },
  "Storage": {
    "ModelsDirectory": "./models/"
  },
  "Logging": {
    "ConsoleLogging": true,
    "LogDirectory": "./logs/",
    "LogFilename": "sharpai.log"
  },
  "Rest": {
    "Port": 8000,
    "Host": "localhost"
  }
}
```

---

## Summary Checklist

Installation is successful when ALL of these are true:

- [ ] Xcode Command Line Tools installed
- [ ] .NET 8 SDK (ARM64) installed
- [ ] Repository cloned
- [ ] Project builds without errors
- [ ] Post-installation fix script created
- [ ] Library path references fixed (showing `@loader_path`)
- [ ] All `libggml*.dylib` files present
- [ ] Server starts without library errors
- [ ] Logs show "library loaded successfully"
- [ ] Homepage accessible at http://localhost:8000
- [ ] Model pull succeeds
- [ ] Model inference works

---

## Getting Help

If you've followed all troubleshooting steps and still have issues:

1. **Collect diagnostic information:**
   ```bash
   # System info
   sw_vers > ~/sharpai-debug.txt
   uname -a >> ~/sharpai-debug.txt

   # .NET info
   dotnet --info >> ~/sharpai-debug.txt

   # Library info
   cd ~/Code/sharpai/src/SharpAI.Server/bin/Debug/net8.0/runtimes/osx-arm64/native
   otool -L libllama.dylib >> ~/sharpai-debug.txt
   ls -lh *.dylib >> ~/sharpai-debug.txt

   # Startup logs
   cd ~/Code/sharpai/src/SharpAI.Server
   dotnet run 2>&1 | head -50 >> ~/sharpai-debug.txt
   ```

2. **Create GitHub issue:**
   - Go to: https://github.com/jchristn/sharpai/issues
   - Title: "macOS ARM64: [Brief description of issue]"
   - Attach: `sharpai-debug.txt`
   - Describe: What you tried and what happened

3. **Check existing issues:**
   - Search: https://github.com/jchristn/sharpai/issues
   - Someone may have already solved your issue

---

## Alternative: Using Rosetta 2 (x64 Emulation)

If ARM64 native mode continues to have issues, you can run in x64 mode via Rosetta 2:

```bash
# Install Rosetta 2 if needed
softwareupdate --install-rosetta --agree-to-license

# Run in x64 mode
cd ~/Code/sharpai/src/SharpAI.Server
arch -x86_64 dotnet run
```

This uses the x64 libraries which don't have the path reference issues.

**Trade-offs:**
- ✅ More stable (fewer path issues)
- ❌ Slower performance (emulation overhead)
- ℹ️ Still CPU-only (no GPU acceleration)

---

## Appendix: Technical Details

### Why the Path Fix Is Needed

**Background:**
- llama.cpp builds multiple shared libraries (`libggml*.dylib`)
- `libllama.dylib` depends on these libraries
- On macOS, libraries reference dependencies via `@rpath` (runtime path)
- .NET's P/Invoke doesn't set the rpath correctly when loading native libraries

**The Fix:**
- Change `@rpath` to `@loader_path` in the library references
- `@loader_path` tells macOS to look in the same directory as the library being loaded
- This is independent of the .NET runtime's rpath settings

### Files Modified

The fix modifies only the dependency references inside `libllama.dylib`. It does NOT modify:
- The actual library code
- Any .NET assemblies
- Configuration files

You can verify this with:
```bash
# Before fix
otool -L libllama.dylib | grep @rpath

# After fix
otool -L libllama.dylib | grep @loader_path
```

### Upstream Issue

This is a known packaging issue with LlamaSharp for macOS ARM64:
- GitHub: https://github.com/SciSharp/LLamaSharp
- The NuGet package includes libraries with `@rpath` references
- But doesn't provide a way to set the rpath when loaded via .NET

Future versions of LlamaSharp may fix this packaging issue.

---

**Last Updated:** 2025-10-10
**Tested On:** macOS Sonoma 15.6, M2 Max, .NET 8.0.8
