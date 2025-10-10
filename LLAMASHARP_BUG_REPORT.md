# LlamaSharp Bug Report: macOS ARM64 Native Library Loading Failure

This document provides a comprehensive bug report for the LlamaSharp team regarding native library loading issues on macOS ARM64 (Apple Silicon).

---

## Summary

**Issue**: LlamaSharp.Backend.Cpu v0.25.0 fails to load on macOS ARM64 due to broken `@rpath` references in the native library dependencies.

**Impact**: Applications using LlamaSharp cannot initialize on macOS ARM64 without manual post-build fixes.

**Severity**: High - Completely blocks macOS ARM64 usage out-of-the-box

**Platforms Affected**: macOS ARM64 (Apple Silicon: M1, M2, M3, M4)

**Platforms Working**: Windows x64, Windows ARM64, Linux x64, macOS x64 (Intel)

---

## Environment

### System Information

```
macOS Version: Sonoma 15.6
Architecture: arm64 (Apple Silicon M2 Max)
Processor: Apple M2 Max
.NET Version: 9.0.8
.NET RID: osx-arm64
```

### Package Versions

```xml
<PackageReference Include="LLamaSharp" Version="0.25.0" />
<PackageReference Include="LLamaSharp.Backend.Cpu" Version="0.25.0" />
```

### Build Configuration

```
Configuration: Debug / Release (both affected)
Target Framework: net8.0
Self-Contained: No
```

---

## Problem Description

### Observed Behavior

When attempting to use LlamaSharp on macOS ARM64, the application fails during native library initialization with the following exception:

```
System.TypeInitializationException: The type initializer for 'LLama.Native.NativeApi' threw an exception.
 ---> LLama.Exceptions.RuntimeError: The native library cannot be correctly loaded. It could be one of the following reasons:
1. No LLamaSharp backend was installed. Please search LLamaSharp.Backend and install one of them.
2. You are using a device with only CPU but installed cuda backend. Please install cpu backend instead.
3. One of the dependency of the native library is missed. Please use `ldd` on linux, `dumpbin` on windows and `otool`to check if all the dependency of the native library is satisfied. Generally you could find the libraries under your output folder.
4. Try to compile llama.cpp yourself to generate a libllama library, then use `LLama.Native.NativeLibraryConfig.WithLibrary` to specify it at the very beginning of your code.

   at LLama.Native.NativeApi..cctor()
   at LLama.Native.NativeApi.llama_max_devices()
```

### Expected Behavior

The native library should load successfully when:
1. LLamaSharp.Backend.Cpu NuGet package is installed
2. Application is built and published
3. `NativeLibraryConfig.All.WithLibrary()` is called with the correct path

This works correctly on Windows x64, Linux x64, and should work on macOS ARM64.

---

## Root Cause Analysis

### Issue 1: Broken @rpath References

The `libllama.dylib` library in the NuGet package uses `@rpath` to reference its dependencies:

```bash
$ otool -L runtimes/osx-arm64/native/libllama.dylib

libllama.dylib:
    @rpath/libllama.dylib (compatibility version 0.0.0, current version 0.0.0)
    @rpath/libggml.dylib (compatibility version 0.0.0, current version 0.0.0)
    @rpath/libggml-cpu.dylib (compatibility version 0.0.0, current version 0.0.0)
    @rpath/libggml-blas.dylib (compatibility version 0.0.0, current version 0.0.0)
    @rpath/libggml-metal.dylib (compatibility version 0.0.0, current version 0.0.0)
    @rpath/libggml-base.dylib (compatibility version 0.0.0, current version 0.0.0)
    /usr/lib/libc++.1.dylib (compatibility version 1.0.0, current version 1700.255.5)
    /usr/lib/libSystem.B.dylib (compatibility version 1.0.0, current version 1345.120.2)
```

**Problem**: When .NET's P/Invoke loads `libllama.dylib`, it does NOT set the runtime path (`@rpath`) to include the directory containing the library. This causes macOS to fail finding the dependent `libggml*.dylib` files, even though they exist in the same directory.

**Error from dlopen:**
```
dlopen() failed: Library not loaded: @rpath/libggml.dylib
  Referenced from: <UUID> /path/to/libllama.dylib
  Reason: tried: '/Users/runner/work/LLamaSharp/LLamaSharp/build/bin/libggml.dylib' (no such file)
```

Notice it's looking in `/Users/runner/work/LLamaSharp/LLamaSharp/build/bin/` - **this is the path from the CI/CD build machine**, not the user's machine.

### Issue 2: Hardcoded Build Paths

The error message reveals that the libraries were built with a hardcoded rpath pointing to the GitHub Actions runner path:
- `/Users/runner/work/LLamaSharp/LLamaSharp/build/bin/`

This path doesn't exist on end-user machines.

---

## Reproduction Steps

### Minimal Reproduction

1. **Create new .NET console application:**
   ```bash
   dotnet new console -n LlamaSharpTest
   cd LlamaSharpTest
   ```

2. **Add LlamaSharp packages:**
   ```bash
   dotnet add package LLamaSharp --version 0.25.0
   dotnet add package LLamaSharp.Backend.Cpu --version 0.25.0
   ```

3. **Add test code:**
   ```csharp
   using LLama.Native;

   Console.WriteLine("Configuring LlamaSharp for macOS ARM64...");

   // Get the native library path
   var libPath = Path.Combine(
       AppContext.BaseDirectory,
       "runtimes", "osx-arm64", "native", "libllama.dylib"
   );

   Console.WriteLine($"Library path: {libPath}");
   Console.WriteLine($"Exists: {File.Exists(libPath)}");

   // Configure library
   NativeLibraryConfig.All.WithLibrary(libPath, "llama");
   Console.WriteLine("Library configured successfully");

   // Try to use it - THIS WILL FAIL
   try
   {
       long devices = NativeApi.llama_max_devices();
       Console.WriteLine($"Devices: {devices}");
   }
   catch (Exception ex)
   {
       Console.WriteLine($"ERROR: {ex.GetType().Name}");
       Console.WriteLine($"Message: {ex.Message}");
       if (ex.InnerException != null)
       {
           Console.WriteLine($"Inner: {ex.InnerException.Message}");
       }
   }
   ```

4. **Build and run:**
   ```bash
   dotnet build
   dotnet run
   ```

### Expected Output (Current - FAIL)

```
Configuring LlamaSharp for macOS ARM64...
Library path: /path/to/bin/Debug/net8.0/runtimes/osx-arm64/native/libllama.dylib
Exists: True
Library configured successfully
ERROR: TypeInitializationException
Message: The type initializer for 'LLama.Native.NativeApi' threw an exception.
Inner: The native library cannot be correctly loaded...
```

### Expected Output (After Fix - PASS)

```
Configuring LlamaSharp for macOS ARM64...
Library path: /path/to/bin/Debug/net8.0/runtimes/osx-arm64/native/libllama.dylib
Exists: True
Library configured successfully
Devices: 0
```

---

## Diagnostic Information

### File Locations in NuGet Package

```bash
$ dotnet nuget locals global-packages --list
global-packages: /Users/username/.nuget/packages

$ find /Users/username/.nuget/packages/llamasharp.backend.cpu/0.25.0 -name "*.dylib"
.../runtimes/osx-arm64/native/libllama.dylib
.../runtimes/osx-arm64/native/libggml.dylib
.../runtimes/osx-arm64/native/libggml-cpu.dylib
.../runtimes/osx-arm64/native/libggml-blas.dylib
.../runtimes/osx-arm64/native/libggml-metal.dylib
.../runtimes/osx-arm64/native/libggml-base.dylib
```

✅ All required libraries ARE present in the NuGet package.

### Dependency Analysis

```bash
$ cd bin/Debug/net8.0/runtimes/osx-arm64/native
$ otool -L libllama.dylib

libllama.dylib:
    @rpath/libllama.dylib (compatibility version 0.0.0, current version 0.0.0)
    @rpath/libggml.dylib ← PROBLEM: Uses @rpath
    @rpath/libggml-cpu.dylib ← PROBLEM: Uses @rpath
    @rpath/libggml-blas.dylib ← PROBLEM: Uses @rpath
    @rpath/libggml-metal.dylib ← PROBLEM: Uses @rpath
    @rpath/libggml-base.dylib ← PROBLEM: Uses @rpath
    /usr/lib/libc++.1.dylib ✓ OK: Absolute path
    /usr/lib/libSystem.B.dylib ✓ OK: Absolute path
```

### Architecture Verification

```bash
$ file libllama.dylib
libllama.dylib: Mach-O 64-bit dynamically linked shared library arm64

$ lipo -info libllama.dylib
Non-fat file: libllama.dylib is architecture: arm64
```

✅ Library is correct ARM64 architecture.

### Manual Load Test

```bash
$ cat > test.c << 'EOF'
#include <stdio.h>
#include <dlfcn.h>
int main() {
    void *h = dlopen("./libllama.dylib", RTLD_NOW);
    if (!h) { printf("ERROR: %s\n", dlerror()); return 1; }
    printf("SUCCESS\n");
    dlclose(h);
    return 0;
}
EOF

$ cc -o test test.c
$ ./test
ERROR: dlopen(./libllama.dylib, 0x0002): Library not loaded: @rpath/libggml.dylib
  Referenced from: <UUID> /path/to/libllama.dylib
  Reason: tried: '/Users/runner/work/LLamaSharp/LLamaSharp/build/bin/libggml.dylib' (no such file)
```

This proves the issue is in the library packaging, not in the .NET P/Invoke code.

---

## Workaround

### User-Side Fix

Users must manually fix the library references after every build:

```bash
cd bin/Debug/net8.0/runtimes/osx-arm64/native

install_name_tool -change @rpath/libggml.dylib @loader_path/libggml.dylib libllama.dylib
install_name_tool -change @rpath/libggml-cpu.dylib @loader_path/libggml-cpu.dylib libllama.dylib
install_name_tool -change @rpath/libggml-blas.dylib @loader_path/libggml-blas.dylib libllama.dylib
install_name_tool -change @rpath/libggml-metal.dylib @loader_path/libggml-metal.dylib libllama.dylib
install_name_tool -change @rpath/libggml-base.dylib @loader_path/libggml-base.dylib libllama.dylib
```

**After fix:**
```bash
$ otool -L libllama.dylib
libllama.dylib:
    @rpath/libllama.dylib
    @loader_path/libggml.dylib ✓ Fixed
    @loader_path/libggml-cpu.dylib ✓ Fixed
    @loader_path/libggml-blas.dylib ✓ Fixed
    @loader_path/libggml-metal.dylib ✓ Fixed
    @loader_path/libggml-base.dylib ✓ Fixed
    /usr/lib/libc++.1.dylib
    /usr/lib/libSystem.B.dylib
```

**Verification:**
```bash
$ ./test
SUCCESS  ← Now works!
```

---

## Recommended Fix

### For LlamaSharp Maintainers

The libraries should be built with `@loader_path` instead of `@rpath` for better portability with P/Invoke.

### Option 1: Fix at Build Time (Preferred)

Modify the CMake or build script to use `@loader_path`:

```cmake
# In CMakeLists.txt or build script:
if(APPLE)
    set_target_properties(llama PROPERTIES
        INSTALL_RPATH "@loader_path"
        BUILD_WITH_INSTALL_RPATH TRUE
    )

    # For each dependency
    set_target_properties(ggml PROPERTIES
        INSTALL_NAME_DIR "@loader_path"
    )
endif()
```

Or use linker flags:
```bash
-install_name @loader_path/libggml.dylib
-install_name @loader_path/libggml-cpu.dylib
# etc...
```

### Option 2: Fix After Build (Post-Processing)

Add a post-build step in the NuGet packaging script:

```bash
# In package build script
if [ "$(uname)" = "Darwin" ]; then
    cd runtimes/osx-arm64/native

    # Fix all @rpath references to @loader_path
    install_name_tool -change @rpath/libggml.dylib @loader_path/libggml.dylib libllama.dylib
    install_name_tool -change @rpath/libggml-cpu.dylib @loader_path/libggml-cpu.dylib libllama.dylib
    install_name_tool -change @rpath/libggml-blas.dylib @loader_path/libggml-blas.dylib libllama.dylib
    install_name_tool -change @rpath/libggml-metal.dylib @loader_path/libggml-metal.dylib libllama.dylib
    install_name_tool -change @rpath/libggml-base.dylib @loader_path/libggml-base.dylib libllama.dylib
fi
```

### Option 3: Documentation Workaround

If the above fixes aren't immediately feasible, document the issue and provide the workaround script in:
- Package README
- GitHub documentation
- Release notes

---

## Impact Assessment

### Current State

- ❌ **macOS ARM64**: Completely broken out-of-the-box
- ✅ **Windows x64**: Works
- ✅ **Linux x64**: Works
- ✅ **macOS x64**: Likely works (not fully tested but uses same packaging approach as Windows)

### User Impact

1. **High friction**: Users must manually fix libraries after every build
2. **Poor experience**: Error messages don't indicate the actual problem
3. **Limited adoption**: macOS ARM64 users cannot use LlamaSharp without workarounds
4. **Documentation burden**: Projects using LlamaSharp must document the workaround

### Affected Use Cases

- ✅ Development on Windows → Works
- ✅ Deployment to Linux → Works
- ❌ Development on Mac (M1/M2/M3) → Broken
- ❌ Distribution to macOS ARM64 users → Broken without post-install scripts

---

## Testing Recommendations

### Verification After Fix

1. **Build test application** (reproduction steps above)
2. **Run on macOS ARM64** without any manual fixes
3. **Verify** `NativeApi.llama_max_devices()` succeeds
4. **Test** actual model loading and inference
5. **Verify** `otool -L` shows `@loader_path` references

### Platforms to Test

- macOS Sonoma (14.x) - ARM64
- macOS Sequoia (15.x) - ARM64
- M1, M2, M3, M4 processors
- .NET 8.0 and .NET 9.0

### Regression Testing

Ensure fix doesn't break other platforms:
- Windows x64/ARM64
- Linux x64/ARM64
- macOS x64 (Intel)

---

## Additional Notes

### Why @loader_path Works Better for P/Invoke

**@rpath (current)**:
- Requires the loading application to set runtime paths
- .NET P/Invoke doesn't set rpath for loaded libraries
- Breaks when loaded from .NET

**@loader_path (recommended)**:
- Path relative to the library being loaded
- Works regardless of how the library is loaded
- Standard practice for portable shared libraries

### Comparison with Other Platforms

**Windows**: Uses DLL search paths (current directory, PATH)
**Linux**: Uses RPATH/RUNPATH embedded in ELF
**macOS**: Uses @rpath/@loader_path in Mach-O

The fix aligns macOS behavior with the "look in same directory" behavior that works on Windows/Linux.

---

## References

### Apple Documentation

- [Dynamic Library Programming Topics](https://developer.apple.com/library/archive/documentation/DeveloperTools/Conceptual/DynamicLibraries/)
- [install_name_tool man page](https://www.unix.com/man-page/osx/1/install_name_tool/)
- [dyld man page](https://www.unix.com/man-page/osx/1/dyld/)

### Related Issues

- Similar .NET P/Invoke issues: [dotnet/runtime#43408](https://github.com/dotnet/runtime/issues/43408)
- macOS rpath best practices: [cmake.org/cmake/help/latest/prop_tgt/INSTALL_RPATH](https://cmake.org/cmake/help/latest/prop_tgt/INSTALL_RPATH.html)

---

## Reproduction Repository

A minimal reproduction case has been prepared at:
**https://github.com/jchristn/sharpai** (see `INSTALLING-ON-MAC-ARM64.md`)

The repository includes:
- Working application (with workaround)
- Diagnostic scripts
- Detailed troubleshooting guide
- Automated fix script for users

---

## Contact

**Reported By**: Joel Christner (jchristn)
**Date**: 2025-10-10
**Project**: SharpAI (https://github.com/jchristn/sharpai)
**LlamaSharp Version**: 0.25.0
**Platform**: macOS ARM64 (Apple Silicon)

---

## Summary for Developers

**Problem**: `@rpath` references in `libllama.dylib` don't resolve when loaded via .NET P/Invoke on macOS ARM64.

**Root Cause**: .NET doesn't set runtime path for P/Invoke libraries; `@rpath` points to non-existent CI build path.

**Fix**: Change `@rpath` references to `@loader_path` in the macOS ARM64 build.

**Effort**: Low - Single build script change or post-build step.

**Impact**: Fixes completely broken macOS ARM64 support.

**Urgency**: High - Affects all macOS ARM64 users (large and growing platform).

---

**Thank you for maintaining LlamaSharp! This is a fantastic project and we appreciate your work. We hope this detailed report helps resolve the macOS ARM64 issues quickly.** 🙏
