# Step-by-Step Testing Guide

Complete testing guide for GPU/CPU runtime backend on Windows, macOS, and Linux.

## Prerequisites

### All Platforms
- .NET 8 SDK installed
- Git (to manage branches)
- 8GB+ RAM recommended
- Internet connection (for downloading models)

### Windows Specific
- Windows 10/11
- For GPU testing: NVIDIA GPU with latest drivers
- PowerShell or Command Prompt

### macOS Specific
- macOS 10.15 or later
- For Intel Macs with GPU: NVIDIA GPU (rare, legacy systems only)
- Terminal

### Linux Specific
- Ubuntu 20.04+ (or similar distribution)
- For GPU testing: NVIDIA GPU with drivers and CUDA Toolkit 12.x
- Docker (for container testing)
- For GPU Docker: NVIDIA Container Toolkit

---

## Part 1: Windows Testing

### Test 1A: Windows CPU Mode (Force CPU)

**Step 1**: Navigate to project directory
```powershell
cd C:\Code\SharpAI\SharpAI
```

**Step 2**: Create/modify settings file
```powershell
# Create sharpai.json if it doesn't exist
@"
{
  "Runtime": {
    "ForceBackend": "cpu"
  }
}
"@ | Out-File -FilePath "sharpai.json" -Encoding UTF8
```

**Step 3**: Run the server
```powershell
dotnet run --project src\SharpAI.Server\SharpAI.Server.csproj
```

**Step 4**: Verify output
Look for these log messages:
```
[NativeLibraryBootstrapper] backend forced to: cpu
[NativeLibraryBootstrapper] configuring cpu backend: ...
[NativeLibraryBootstrapper] successfully configured cpu backend
[LlamaSharpEngine] initializing LlamaSharp with CPU acceleration
```

**Step 5**: Test model pull (in a new PowerShell window)
```powershell
# Pull a small model
curl -X POST http://localhost:8000/api/pull `
  -H "Content-Type: application/json" `
  -d '{"name": "qwen2.5:0.5b"}'
```

**Step 6**: Test embeddings
```powershell
curl -X POST http://localhost:8000/api/embed `
  -H "Content-Type: application/json" `
  -d '{"model": "qwen2.5:0.5b", "input": "hello world"}'
```

**Step 7**: Stop the server
Press `Ctrl+C` in the server window

**Expected Result**: ✅ Server runs in CPU mode, model operations work

---

### Test 1B: Windows GPU Mode (if NVIDIA GPU available)

**Step 1**: Verify GPU is available
```powershell
nvidia-smi
```
Should show your GPU. If not, skip to Test 1C.

**Step 2**: Modify settings for GPU
```powershell
@"
{
  "Runtime": {
    "ForceBackend": "cuda"
  }
}
"@ | Out-File -FilePath "sharpai.json" -Encoding UTF8
```

**Step 3**: Run the server
```powershell
dotnet run --project src\SharpAI.Server\SharpAI.Server.csproj
```

**Step 4**: Verify GPU backend selected
Look for:
```
[NativeLibraryBootstrapper] backend forced to: cuda
[NativeLibraryBootstrapper] configuring cuda backend: ...
[NativeLibraryBootstrapper] successfully configured cuda backend
[LlamaSharpEngine] CUDA detected, 1 GPU device(s) available
```

**Step 5**: Monitor GPU usage (in another window)
```powershell
# Run this in loop to monitor
while($true) { nvidia-smi; sleep 2; clear }
```

**Step 6**: Pull and test model (same as Test 1A steps 5-6)

**Step 7**: Verify GPU memory usage
- Check `nvidia-smi` output shows GPU memory allocated
- GPU utilization should be >0% during inference

**Expected Result**: ✅ Server runs in GPU mode, GPU is utilized

---

### Test 1C: Windows Auto-Detection

**Step 1**: Enable auto-detection
```powershell
@"
{
  "Runtime": {
    "ForceBackend": null
  }
}
"@ | Out-File -FilePath "sharpai.json" -Encoding UTF8
```

**Step 2**: Run the server
```powershell
dotnet run --project src\SharpAI.Server\SharpAI.Server.csproj
```

**Step 3**: Verify auto-detection result
- **With GPU**: Should see "GPU detected, selecting CUDA backend"
- **Without GPU**: Should see "no GPU detected, selecting CPU backend"

**Expected Result**: ✅ Correct backend selected automatically

---

## Part 2: macOS Testing

### Test 2A: macOS Intel CPU Mode

**Step 1**: Navigate to project directory
```bash
cd ~/Code/SharpAI/SharpAI  # Adjust path as needed
```

**Step 2**: Create settings file
```bash
cat > sharpai.json << 'EOF'
{
  "Runtime": {
    "ForceBackend": "cpu"
  }
}
EOF
```

**Step 3**: Run the server
```bash
dotnet run --project src/SharpAI.Server/SharpAI.Server.csproj
```

**Step 4**: Verify output
Look for:
```
[NativeLibraryBootstrapper] backend forced to: cpu
[NativeLibraryBootstrapper] configuring cpu backend: ./runtimes/cpu/libllama.dylib
```

**Step 5**: Test model operations (in new terminal)
```bash
# Pull model
curl -X POST http://localhost:8000/api/pull \
  -H "Content-Type: application/json" \
  -d '{"name": "qwen2.5:0.5b"}'

# Test embeddings
curl -X POST http://localhost:8000/api/embed \
  -H "Content-Type: application/json" \
  -d '{"model": "qwen2.5:0.5b", "input": "hello world"}'
```

**Expected Result**: ✅ Server runs in CPU mode on macOS

---

### Test 2B: macOS Apple Silicon (ARM64)

**Step 1**: Navigate to project directory
```bash
cd ~/Code/SharpAI/SharpAI
```

**Step 2**: Enable auto-detection
```bash
cat > sharpai.json << 'EOF'
{
  "Runtime": {
    "ForceBackend": null
  }
}
EOF
```

**Step 3**: Run the server
```bash
dotnet run --project src/SharpAI.Server/SharpAI.Server.csproj
```

**Step 4**: Verify Apple Silicon detection
Look for:
```
[NativeLibraryBootstrapper] detected platform: OSX, architecture: Arm64
[NativeLibraryBootstrapper] Apple Silicon detected, GPU backend not supported, using CPU
[NativeLibraryBootstrapper] configuring cpu backend: ./runtimes/cpu/libllama.dylib
```

**Step 5**: Test model operations (same as Test 2A step 5)

**Expected Result**: ✅ Auto-detects Apple Silicon and uses CPU

---

### Test 2C: macOS Intel with GPU (Legacy/Rare)

**Note**: Only applicable if you have an older Intel Mac with NVIDIA GPU

**Step 1**: Check for NVIDIA GPU
```bash
system_profiler SPDisplaysDataType | grep NVIDIA
```

**Step 2**: If NVIDIA GPU present, try GPU mode
```bash
cat > sharpai.json << 'EOF'
{
  "Runtime": {
    "ForceBackend": "cuda"
  }
}
EOF

dotnet run --project src/SharpAI.Server/SharpAI.Server.csproj
```

**Expected Result**:
- ✅ If NVIDIA GPU + drivers: GPU mode works
- ✅ If no NVIDIA support: Fallback to CPU with warning

---

## Part 3: Linux Testing (Local)

### Test 3A: Linux CPU Mode

**Step 1**: Navigate to project directory
```bash
cd ~/Code/SharpAI/SharpAI  # Adjust path as needed
```

**Step 2**: Create settings file
```bash
cat > sharpai.json << 'EOF'
{
  "Runtime": {
    "ForceBackend": "cpu"
  }
}
EOF
```

**Step 3**: Run the server
```bash
dotnet run --project src/SharpAI.Server/SharpAI.Server.csproj
```

**Step 4**: Verify output
```
[NativeLibraryBootstrapper] backend forced to: cpu
[NativeLibraryBootstrapper] configuring cpu backend: ./runtimes/cpu/libllama.so
```

**Step 5**: Test model operations (in new terminal)
```bash
# Pull model
curl -X POST http://localhost:8000/api/pull \
  -H "Content-Type: application/json" \
  -d '{"name": "qwen2.5:0.5b"}'

# Test embeddings
curl -X POST http://localhost:8000/api/embed \
  -H "Content-Type: application/json" \
  -d '{"model": "qwen2.5:0.5b", "input": "hello world"}'
```

**Expected Result**: ✅ Server runs in CPU mode

---

### Test 3B: Linux GPU Mode (if NVIDIA GPU available)

**Step 1**: Verify NVIDIA GPU and drivers
```bash
nvidia-smi
```
Should show GPU info. If not, skip to Test 3C.

**Step 2**: Verify CUDA installation
```bash
nvcc --version  # Should show CUDA version
ls /usr/lib/x86_64-linux-gnu/libcuda.so.1  # Should exist
```

**Step 3**: Configure for GPU
```bash
cat > sharpai.json << 'EOF'
{
  "Runtime": {
    "ForceBackend": "cuda"
  }
}
EOF
```

**Step 4**: Run the server
```bash
dotnet run --project src/SharpAI.Server/SharpAI.Server.csproj
```

**Step 5**: Verify GPU backend
```
[NativeLibraryBootstrapper] backend forced to: cuda
[NativeLibraryBootstrapper] configuring cuda backend: ./runtimes/cuda/libllama.so
[NativeLibraryBootstrapper] successfully configured cuda backend
[LlamaSharpEngine] CUDA detected, 1 GPU device(s) available
```

**Step 6**: Monitor GPU (in another terminal)
```bash
watch -n 1 nvidia-smi
```

**Step 7**: Test model operations (same as Test 3A step 5)

**Step 8**: Verify GPU usage
Check `nvidia-smi` shows:
- GPU memory allocated
- GPU utilization >0% during inference

**Expected Result**: ✅ Server runs in GPU mode, GPU utilized

---

### Test 3C: Linux Auto-Detection

**Step 1**: Enable auto-detection
```bash
cat > sharpai.json << 'EOF'
{
  "Runtime": {
    "ForceBackend": null
  }
}
EOF
```

**Step 2**: Run server
```bash
dotnet run --project src/SharpAI.Server/SharpAI.Server.csproj
```

**Step 3**: Verify detection
- **With GPU**: "GPU detected, selecting CUDA backend"
- **Without GPU**: "no GPU detected, selecting CPU backend"

**Expected Result**: ✅ Correct backend auto-selected

---

## Part 4: Docker Testing on Linux

### Test 4A: Build Docker Image

**Step 1**: Navigate to project directory
```bash
cd ~/Code/SharpAI/SharpAI
```

**Step 2**: Build the image
```bash
./docker-build.sh latest
```

**Step 3**: Verify build succeeded
Look for:
```
Found CPU backend: /root/.nuget/packages/llamasharp.backend.cpu/.../libllama.so
Found CUDA backend: /root/.nuget/packages/llamasharp.backend.cuda12/.../libllama.so
Native libraries organized:
Build Complete!
```

**Step 4**: Check image exists
```bash
docker images | grep sharpai
```
Should show: `sharpai   latest   ...`

**Expected Result**: ✅ Docker image built successfully

---

### Test 4B: Docker CPU Mode

**Step 1**: Run container in CPU mode
```bash
./docker-run.sh cpu
```
Or manually:
```bash
docker run --rm -it -p 8000:8000 sharpai:latest
```

**Step 2**: Verify CPU backend in logs
Look for:
```
[NativeLibraryBootstrapper] no GPU detected via any method
[NativeLibraryBootstrapper] no GPU detected, selecting CPU backend
```

**Step 3**: Test model operations (in new terminal)
```bash
# Pull model
curl -X POST http://localhost:8000/api/pull \
  -H "Content-Type: application/json" \
  -d '{"name": "qwen2.5:0.5b"}'

# Test embeddings
curl -X POST http://localhost:8000/api/embed \
  -H "Content-Type: application/json" \
  -d '{"model": "qwen2.5:0.5b", "input": "hello world"}'
```

**Step 4**: Stop container
Press `Ctrl+C`

**Expected Result**: ✅ Container runs in CPU mode

---

### Test 4C: Docker GPU Mode (Requires NVIDIA Docker)

**Step 1**: Verify NVIDIA Docker runtime
```bash
docker run --rm --gpus all nvidia/cuda:12.0-base nvidia-smi
```
Should show GPU. If error, install NVIDIA Container Toolkit first.

**Step 2**: Install NVIDIA Container Toolkit (if needed)
```bash
# Ubuntu/Debian
distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
curl -s -L https://nvidia.github.io/nvidia-docker/gpgkey | sudo apt-key add -
curl -s -L https://nvidia.github.io/nvidia-docker/$distribution/nvidia-docker.list | \
  sudo tee /etc/apt/sources.list.d/nvidia-docker.list

sudo apt-get update
sudo apt-get install -y nvidia-container-toolkit
sudo systemctl restart docker
```

**Step 3**: Run container with GPU
```bash
./docker-run.sh gpu
```
Or manually:
```bash
docker run --rm -it --gpus all -p 8000:8000 sharpai:latest
```

**Step 4**: Verify GPU backend in logs
```
[NativeLibraryBootstrapper] NVIDIA_VISIBLE_DEVICES detected: 0
[NativeLibraryBootstrapper] GPU detected, selecting CUDA backend
[NativeLibraryBootstrapper] successfully configured cuda backend
[LlamaSharpEngine] CUDA detected, 1 GPU device(s) available
```

**Step 5**: Monitor GPU (in another terminal)
```bash
watch -n 1 nvidia-smi
```

**Step 6**: Test model operations
```bash
# Pull model
curl -X POST http://localhost:8000/api/pull \
  -H "Content-Type: application/json" \
  -d '{"name": "qwen2.5:0.5b"}'

# Test embeddings
curl -X POST http://localhost:8000/api/embed \
  -H "Content-Type: application/json" \
  -d '{"model": "qwen2.5:0.5b", "input": "hello world"}'
```

**Step 7**: Verify GPU usage
- `nvidia-smi` should show container using GPU
- Memory allocated to container
- GPU utilization >0% during inference

**Expected Result**: ✅ Container runs in GPU mode, GPU utilized

---

### Test 4D: Docker Auto-Detection

**Step 1**: Run with auto-detection
```bash
./docker-run.sh auto
```

**Step 2**: Verify behavior
- **If `nvidia-smi` works on host**: Starts with `--gpus all`, uses GPU
- **If `nvidia-smi` fails on host**: Starts without GPU, uses CPU

**Expected Result**: ✅ Automatically selects correct mode

---

## Part 5: Performance Comparison

### Test 5A: CPU vs GPU Speed Test

**Step 1**: Pull a test model (if not already done)
```bash
curl -X POST http://localhost:8000/api/pull \
  -H "Content-Type: application/json" \
  -d '{"name": "qwen2.5:0.5b"}'
```

**Step 2**: Test CPU performance
```bash
# Start server in CPU mode
# Then run:
time curl -X POST http://localhost:8000/api/embed \
  -H "Content-Type: application/json" \
  -d '{"model": "qwen2.5:0.5b", "input": "This is a test sentence for performance comparison."}'
```
Note the time (e.g., "0.5s")

**Step 3**: Restart in GPU mode and test
```bash
# Restart server in GPU mode
# Then run same curl command with time
```
Note the time (should be faster, e.g., "0.1s")

**Step 4**: Compare results
GPU should be 2-10x faster depending on model and hardware

**Expected Result**: ✅ GPU mode is significantly faster

---

## Part 6: Error Recovery Testing

### Test 6A: GPU Fallback to CPU

**Step 1**: Simulate GPU failure
```bash
# Create settings pointing to non-existent GPU library
cat > sharpai.json << 'EOF'
{
  "Runtime": {
    "GpuBackendPath": "/nonexistent/path/libllama.so",
    "CpuBackendPath": "./runtimes/cpu/libllama.so",
    "ForceBackend": "cuda"
  }
}
EOF
```

**Step 2**: Run server
```bash
dotnet run --project src/SharpAI.Server/SharpAI.Server.csproj
```

**Step 3**: Verify fallback
Should see:
```
[NativeLibraryBootstrapper] library file not found: /nonexistent/path/libllama.so
[NativeLibraryBootstrapper] no explicit library path configured, using default
```
Then falls back to auto-detection

**Expected Result**: ✅ Graceful handling of misconfiguration

---

## Part 7: Configuration Testing

### Test 7A: Custom Library Paths

**Step 1**: Find actual library location
```bash
# Linux
find ~/.nuget/packages -name "libllama.so" | head -2

# macOS
find ~/.nuget/packages -name "libllama.dylib" | head -2

# Windows (PowerShell)
Get-ChildItem -Path $env:USERPROFILE\.nuget\packages -Recurse -Filter llama.dll | Select-Object -First 2
```

**Step 2**: Configure custom paths
```bash
cat > sharpai.json << 'EOF'
{
  "Runtime": {
    "CpuBackendPath": "<FULL_PATH_TO_CPU_LIBRARY>",
    "ForceBackend": "cpu"
  }
}
EOF
```

**Step 3**: Run and verify
```bash
dotnet run --project src/SharpAI.Server/SharpAI.Server.csproj
```

Should see:
```
[NativeLibraryBootstrapper] using CPU backend path from settings: <YOUR_PATH>
```

**Expected Result**: ✅ Custom paths work correctly

---

## Verification Checklist

After completing all tests, verify:

### Windows
- [ ] CPU mode works (forced)
- [ ] GPU mode works (if NVIDIA GPU available)
- [ ] Auto-detection works correctly
- [ ] Model pull and inference work
- [ ] GPU utilization confirmed (if GPU mode)

### macOS Intel
- [ ] CPU mode works
- [ ] Auto-detection works
- [ ] Model operations work

### macOS Apple Silicon
- [ ] CPU mode works
- [ ] Auto-detection selects CPU (no GPU support)
- [ ] "Apple Silicon detected" message appears
- [ ] Model operations work

### Linux Local
- [ ] CPU mode works
- [ ] GPU mode works (if NVIDIA GPU available)
- [ ] Auto-detection works correctly
- [ ] Model operations work
- [ ] GPU utilization confirmed (if GPU mode)

### Linux Docker
- [ ] Image builds successfully
- [ ] CPU mode container works
- [ ] GPU mode container works (if NVIDIA Docker available)
- [ ] Auto-detection script works
- [ ] Model operations work in container
- [ ] GPU utilization in container (if GPU mode)

### General
- [ ] Logging clearly indicates backend selection
- [ ] GPU mode is faster than CPU mode (where applicable)
- [ ] Fallback to CPU works when GPU fails
- [ ] Custom paths configuration works
- [ ] No errors or warnings in build

---

## Troubleshooting

### Issue: "library file not found"
**Solution**:
```bash
# Verify native libraries exist
ls -la runtimes/cpu/
ls -la runtimes/cuda/

# If missing, rebuild
dotnet build src/SharpAI.Server/SharpAI.Server.csproj
```

### Issue: Docker build fails to find libraries
**Solution**:
```bash
# Clear cache and rebuild
docker build --no-cache -f src/SharpAI.Server/Dockerfile -t sharpai:latest src/
```

### Issue: GPU not detected in Docker
**Solution**:
```bash
# Verify NVIDIA Docker runtime
docker run --rm --gpus all nvidia/cuda:12.0-base nvidia-smi

# If fails, reinstall NVIDIA Container Toolkit
sudo apt-get install -y nvidia-container-toolkit
sudo systemctl restart docker
```

### Issue: Model download fails
**Solution**:
```bash
# Check internet connection
curl -I https://huggingface.co

# Check disk space
df -h

# Try a smaller model
curl -X POST http://localhost:8000/api/pull \
  -H "Content-Type: application/json" \
  -d '{"name": "qwen2.5:0.5b"}'
```

---

## Quick Test Summary

**Minimal test sequence** (5 minutes):
1. Build: `cd src && dotnet build SharpAI.sln` ✅
2. Run: `dotnet run --project SharpAI.Server/SharpAI.Server.csproj` ✅
3. Check logs for backend selection ✅
4. Pull model: `curl -X POST http://localhost:8000/api/pull -H "Content-Type: application/json" -d '{"name":"qwen2.5:0.5b"}'` ✅
5. Test inference: `curl -X POST http://localhost:8000/api/embed -H "Content-Type: application/json" -d '{"model":"qwen2.5:0.5b","input":"test"}'` ✅

**Complete test sequence** (30-60 minutes):
- Run all platform-specific tests (Part 1-3)
- Run Docker tests (Part 4)
- Run performance comparison (Part 5)
- Run error recovery tests (Part 6)
- Verify checklist complete
