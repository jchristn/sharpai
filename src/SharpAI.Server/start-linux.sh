#!/bin/bash
#
# SharpAI.Server startup script for Linux
# Validates dependencies and launches the server
#

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo ""
echo "SharpAI.Server - Linux Startup"
echo ""

# Detect architecture
ARCH=$(uname -m)
if [ "$ARCH" = "x86_64" ]; then
    RID="linux-x64"
elif [ "$ARCH" = "aarch64" ]; then
    RID="linux-arm64"
else
    echo "ERROR: Unsupported architecture: $ARCH"
    exit 1
fi

echo "Detected architecture: $ARCH (RID: $RID)"
echo ""

# Check for AVX2 support (on x64)
AVX_VARIANT="avx2"
if [ "$ARCH" = "x86_64" ]; then
    if grep -q avx2 /proc/cpuinfo; then
        AVX_VARIANT="avx2"
        echo "CPU features: AVX2 supported"
    else
        AVX_VARIANT="avx"
        echo "CPU features: AVX2 not supported, using AVX"
    fi

    NATIVE_DIR="runtimes/$RID/native/$AVX_VARIANT"
else
    NATIVE_DIR="runtimes/$RID/native"
fi

echo "Native library directory: $NATIVE_DIR"
echo ""

# Verify native library exists
if [ ! -d "$NATIVE_DIR" ]; then
    echo "ERROR: Native library directory not found: $NATIVE_DIR"
    echo ""
    echo "This may indicate the LLamaSharp backend package was not restored correctly."
    echo "Try running: dotnet restore"
    exit 1
fi

LIBLLAMA_PATH="$NATIVE_DIR/libllama.so"
if [ ! -f "$LIBLLAMA_PATH" ]; then
    echo "ERROR: libllama.so not found at: $LIBLLAMA_PATH"
    exit 1
fi

echo "✓ Found libllama.so"

# Verify dependencies
MISSING_DEPS=0
REQUIRED_DEPS=("libggml-base.so" "libggml-cpu.so" "libggml.so")

for dep in "${REQUIRED_DEPS[@]}"; do
    if [ -f "$NATIVE_DIR/$dep" ]; then
        echo "✓ Found $dep"
    else
        echo "✗ MISSING: $dep"
        MISSING_DEPS=$((MISSING_DEPS + 1))
    fi
done

if [ $MISSING_DEPS -gt 0 ]; then
    echo ""
    echo "ERROR: Missing $MISSING_DEPS required dependencies"
    echo ""
    echo "The LLamaSharp backend package may be incomplete."
    echo "Try restoring packages: dotnet restore"
    exit 1
fi

echo ""
echo "All dependencies verified successfully"
echo "Starting SharpAI.Server"
echo ""

# Launch the server
# Check if we have a native executable (self-contained) or need to use dotnet
if [ -f "./SharpAI.Server" ]; then
    exec ./SharpAI.Server "$@"
else
    exec dotnet SharpAI.Server.dll "$@"
fi
