# macOS ARM64 (Apple Silicon) Troubleshooting

## Current Status

**Issue**: Native library loading fails on macOS ARM64 (Apple Silicon) despite correct architecture and path configuration.

**Root Cause**: Unknown - either missing dependencies or LlamaSharp ARM64 compatibility issue.

---

## Diagnostic Steps

### Run Diagnostic Script

```bash
cd ~/Code/SharpAI/SharpAI
chmod +x diagnose-mac.sh
./diagnose-mac.sh
```

This will check:
1. Library exists and architecture
2. All dependencies are satisfied
3. Library can be loaded with dlopen()
4. System frameworks availability

**Share the output** - it will tell us exactly what's wrong.

---

## Workaround: Run Under Rosetta 2 (x64 Emulation)

If ARM64 doesn't work, you can run SharpAI under Rosetta 2 (x64 emulation) which should use the x64 libraries that are known to work.

### Step 1: Ensure Rosetta 2 is Installed

```bash
# Check if Rosetta 2 is installed
pgrep -q oahd && echo "Rosetta 2 is installed" || echo "Rosetta 2 NOT installed"

# If not installed, install it:
softwareupdate --install-rosetta --agree-to-license
```

### Step 2: Run Under Rosetta 2

```bash
cd ~/Code/SharpAI/src/SharpAI.Server

# Run .NET under x64 emulation
arch -x86_64 dotnet run
```

**Expected behavior**:
- Will use `runtimes/osx-x64/native/` libraries instead of `osx-arm64`
- Should work without errors (Windows x64 works, so macOS x64 should too)
- Performance will be slightly slower due to emulation, but should be functional

---

## Alternative: Use x64 .NET SDK

Instead of running each command with `arch -x86_64`, install the x64 version of .NET SDK:

### Step 1: Download x64 .NET SDK

```bash
# Download x64 .NET 8 SDK
curl -O https://download.visualstudio.microsoft.com/download/pr/your-version/dotnet-sdk-8.0.xxx-osx-x64.pkg

# Or visit: https://dotnet.microsoft.com/download/dotnet/8.0
# Choose: macOS x64 Installer
```

### Step 2: Install and Use

```bash
# After installing, verify architecture
file $(which dotnet)
# Should show: Mach-O 64-bit executable x86_64

# Now just run normally - it will use x64:
cd ~/Code/SharpAI/src/SharpAI.Server
dotnet run
```

---

## If Rosetta 2 Also Fails

If even Rosetta 2 doesn't work, there may be a deeper macOS-specific issue.

### Manual Dependency Check

```bash
cd ~/Code/SharpAI/src/SharpAI.Server/bin/Debug/net8.0/runtimes

# Check ARM64 dependencies
otool -L osx-arm64/native/libllama.dylib

# Check x64 dependencies
otool -L osx-x64/native/avx2/libllama.dylib
```

Common missing dependencies:
- **libc++**: Should be in `/usr/lib/libc++.dylib`
- **libSystem**: Should be in `/usr/lib/libSystem.dylib`
- **Accelerate**: Framework at `/System/Library/Frameworks/Accelerate.framework`

### Install Xcode Command Line Tools

Some dependencies come from Xcode Command Line Tools:

```bash
# Check if installed
xcode-select -p

# If not installed:
xcode-select --install
```

---

## Reporting the Issue

If diagnostics show all dependencies are satisfied but it still fails, this is a LlamaSharp compatibility issue.

### Information to Collect

Run these commands and save output:

```bash
# System info
sw_vers
uname -a
arch

# .NET info
dotnet --info

# Library info
cd ~/Code/SharpAI/src/SharpAI.Server/bin/Debug/net8.0/runtimes/osx-arm64/native/
file libllama.dylib
lipo -info libllama.dylib
otool -L libllama.dylib
codesign --verify --verbose libllama.dylib

# NuGet package version
grep -A 1 "LLamaSharp.Backend" ~/Code/SharpAI/src/SharpAI/SharpAI.csproj
```

### Report To

1. **SharpAI Issues**: https://github.com/jchristn/sharpai/issues
   - Title: "macOS ARM64 native library loading fails"
   - Include: Full diagnostic output + system info above

2. **LlamaSharp Issues**: https://github.com/SciSharp/LLamaSharp/issues
   - Title: "ARM64 macOS: Native library cannot be loaded"
   - Include: System info + proof that library exists and has correct architecture

---

## Expected Timeline

- **Immediate**: Use Rosetta 2 workaround (should work)
- **Short-term**: Wait for LlamaSharp ARM64 macOS fixes
- **Long-term**: Native ARM64 support when LlamaSharp is compatible

---

## Testing Checklist

- [ ] Run `diagnose-mac.sh` and check output
- [ ] Verify Rosetta 2 is installed
- [ ] Try running with `arch -x86_64 dotnet run`
- [ ] Verify x64 mode works (should succeed)
- [ ] If x64 works, document as ARM64 limitation
- [ ] If x64 also fails, check for macOS-specific issues

---

## Known Working Configurations

✅ **Windows x64** - CPU and GPU modes both work
✅ **Windows ARM64** - Not tested
✅ **Linux x64** - CPU and GPU modes work (Docker)
✅ **macOS x64 (Intel)** - Should work
✅ **macOS x64 (Rosetta 2 on Apple Silicon)** - Should work
❌ **macOS ARM64 (Apple Silicon native)** - Current issue

---

## Quick Test Command

```bash
# Test if Rosetta 2 workaround works:
cd ~/Code/SharpAI/src/SharpAI.Server
arch -x86_64 dotnet run

# In another terminal:
curl http://localhost:8000/

# If that works, use Rosetta 2 as the temporary solution
```
