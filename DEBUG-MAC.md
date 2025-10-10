# macOS Debug Commands

Run these on your Mac to help diagnose the issue:

## 1. Check if libraries exist
```bash
cd ~/Code/SharpAI/src/SharpAI.Server/bin/Debug/net8.0

# Find all dylib files
find . -name "*.dylib" -type f

# Check specific paths
ls -la runtimes/osx-arm64/native/ 2>/dev/null || echo "osx-arm64 not found"
ls -la runtimes/osx-x64/native/ 2>/dev/null || echo "osx-x64 not found"
```

## 2. Check configuration file
```bash
cd ~/Code/SharpAI/src/SharpAI.Server
cat sharpai.json 2>/dev/null || echo "sharpai.json not found"
```

## 3. Get full startup logs
```bash
cd ~/Code/SharpAI/src/SharpAI.Server

# Run with verbose logging
dotnet run 2>&1 | tee startup.log

# Then CTRL+C and show me startup.log
```

## 4. Check NuGet packages
```bash
# Find where NuGet packages are stored
dotnet nuget locals global-packages --list

# Then search for LlamaSharp backend packages
find $(dotnet nuget locals global-packages --list | cut -d ' ' -f 2) -name "libllama.dylib" 2>/dev/null
```

## 5. Check architecture
```bash
uname -m  # Should show "arm64" for M2
dotnet --info | grep -i "RID"
```

Please run these and share the output.
