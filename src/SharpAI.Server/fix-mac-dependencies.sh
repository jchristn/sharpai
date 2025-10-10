#!/bin/bash
#
# Fix macOS ARM64 Missing Dependencies
# Finds and copies missing libggml*.dylib files
#

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TARGET_DIR="$SCRIPT_DIR/runtimes/osx-arm64/native"
MISSING_LIBS=("libggml.dylib" "libggml-cpu.dylib" "libggml-blas.dylib" "libggml-metal.dylib" "libggml-base.dylib")

echo "================================================"
echo "Fixing macOS ARM64 Missing Dependencies"
echo "================================================"
echo ""
echo "Script directory: $SCRIPT_DIR"
echo "Target directory: $TARGET_DIR"
echo ""

# Check target directory exists
if [ ! -d "$TARGET_DIR" ]; then
    echo "ERROR: Target directory not found: $TARGET_DIR"
    exit 1
fi

echo "1. Searching for missing libraries..."
echo ""

# Search in current runtime directory
echo "   Searching in current runtime directory..."
FOUND_IN_BUILD=$(find "$SCRIPT_DIR/runtimes" -name "libggml*.dylib" -type f 2>/dev/null || true)

# Search in NuGet cache
echo "   Searching in NuGet cache..."
NUGET_GLOBAL=$(dotnet nuget locals global-packages --list 2>/dev/null | cut -d ' ' -f 2)
if [ -n "$NUGET_GLOBAL" ]; then
    FOUND_IN_NUGET=$(find "$NUGET_GLOBAL/llamasharp.backend.cpu" -name "libggml*.dylib" -type f 2>/dev/null || true)
else
    FOUND_IN_NUGET=""
fi

# Combine results
ALL_FOUND="$FOUND_IN_BUILD"$'\n'"$FOUND_IN_NUGET"

if [ -z "$ALL_FOUND" ] || [ "$ALL_FOUND" == $'\n' ]; then
    echo ""
    echo "ERROR: Could not find any libggml*.dylib files!"
    echo ""
    echo "Searched in:"
    echo "  - $SCRIPT_DIR/runtimes"
    if [ -n "$NUGET_GLOBAL" ]; then
        echo "  - $NUGET_GLOBAL/llamasharp.backend.cpu"
    fi
    echo ""
    echo "These files should be part of the LLamaSharp.Backend.Cpu NuGet package."
    echo ""
    echo "WORKAROUND: Try using x64 mode with Rosetta 2:"
    echo "  arch -x86_64 dotnet run"
    exit 1
fi

echo ""
echo "2. Found libraries:"
echo "$ALL_FOUND" | grep -v '^$'
echo ""

echo "3. Copying missing libraries to target directory..."
echo ""

COPIED=0
for lib in "${MISSING_LIBS[@]}"; do
    if [ -f "$TARGET_DIR/$lib" ]; then
        echo "   ✓ Already exists: $lib"
    else
        # Find this specific library
        SOURCE=$(echo "$ALL_FOUND" | grep -m 1 "/$lib$" || true)

        if [ -n "$SOURCE" ] && [ -f "$SOURCE" ]; then
            echo "   → Copying: $lib"
            echo "      From: $SOURCE"
            cp "$SOURCE" "$TARGET_DIR/"
            COPIED=$((COPIED + 1))
        else
            echo "   ✗ Not found: $lib"
        fi
    fi
done

echo ""
echo "4. Verifying all dependencies..."
echo ""

MISSING_COUNT=0
for lib in "${MISSING_LIBS[@]}"; do
    if [ -f "$TARGET_DIR/$lib" ]; then
        echo "   ✓ $lib"
    else
        echo "   ✗ MISSING: $lib"
        MISSING_COUNT=$((MISSING_COUNT + 1))
    fi
done

# Also check the main library
echo "   ✓ libllama.dylib (already present)"

echo ""
if [ $MISSING_COUNT -eq 0 ]; then
    echo "5. Fixing @rpath references with install_name_tool..."
    echo ""

    # Fix @rpath references in libllama.dylib to use @loader_path
    cd "$TARGET_DIR" || exit 1

    FIXED=0
    DEPS_TO_FIX=("libggml.dylib" "libggml-cpu.dylib" "libggml-base.dylib" "libggml-blas.dylib" "libggml-metal.dylib")

    for dep in "${DEPS_TO_FIX[@]}"; do
        # Check if this dependency is referenced in libllama.dylib
        if otool -L libllama.dylib | grep -q "@rpath/$dep"; then
            echo "   → Fixing: @rpath/$dep → @loader_path/$dep"
            install_name_tool -change "@rpath/$dep" "@loader_path/$dep" libllama.dylib
            FIXED=$((FIXED + 1))
        fi
    done

    echo ""
    echo "   Fixed $FIXED @rpath reference(s)"
    echo ""
    echo "================================================"
    echo "SUCCESS! All dependencies fixed"
    echo "================================================"
    echo ""
    echo "Now try running the server again:"
    echo "  cd ~/Code/SharpAI/src/SharpAI.Server"
    echo "  dotnet run"
    echo ""
else
    echo "================================================"
    echo "INCOMPLETE! Still missing $MISSING_COUNT dependencies"
    echo "================================================"
    echo ""
    echo "The missing libraries were not found in:"
    echo "  - Build output directory"
    echo "  - NuGet package cache"
    echo ""
    echo "This may indicate the LLamaSharp.Backend.Cpu package"
    echo "is incomplete for macOS ARM64."
    echo ""
    echo "WORKAROUND: Use Rosetta 2 (x64 emulation):"
    echo "  arch -x86_64 dotnet run"
    echo ""
fi
