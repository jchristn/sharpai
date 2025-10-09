# Runtime Backend Configuration

SharpAI supports automatic detection and configuration of CPU and GPU (CUDA) backends, allowing a single executable and Docker container to run in both GPU-accelerated and CPU-only environments.

## How It Works

### Automatic Detection

At startup, SharpAI automatically detects the available hardware and selects the appropriate native library backend:

1. **Platform Detection**: Identifies Windows, Linux, or macOS
2. **Architecture Detection**: Identifies x64 or ARM64 (Apple Silicon)
3. **GPU Detection**: Checks for NVIDIA GPU availability using multiple methods:
   - NVIDIA driver files (`/proc/driver/nvidia/version` on Linux)
   - Environment variables (`NVIDIA_VISIBLE_DEVICES`)
   - NVIDIA SMI tool execution
   - CUDA library files
4. **Backend Selection**:
   - **GPU**: Selected if NVIDIA GPU is detected (Linux/Windows)
   - **CPU**: Selected if no GPU is detected or on Apple Silicon (macOS ARM64)

### Platform Support

| Platform | Architecture | CPU Backend | GPU Backend (CUDA) |
|----------|--------------|-------------|-------------------|
| Windows | x64 | ✅ | ✅ |
| Linux | x64 | ✅ | ✅ |
| macOS | x64 | ✅ | ✅ |
| macOS | ARM64 (Apple Silicon) | ✅ | ❌ |

**Note**: Apple Silicon (ARM64) does not support CUDA. CPU backend is automatically selected on Apple Silicon devices.

## Configuration

### Settings File

Backend configuration is managed through the `sharpai.json` settings file:

```json
{
  "Runtime": {
    "ForceBackend": null,
    "CpuBackendPath": null,
    "GpuBackendPath": null,
    "EnableNativeLogging": false
  }
}
```

#### Settings Properties

- **ForceBackend**: Override automatic detection
  - Values: `"cpu"`, `"cuda"`, or `null` (auto-detect)
  - Default: `null` (automatic detection)
  - Use case: Force CPU mode on a GPU system, or for testing

- **CpuBackendPath**: Path to CPU backend native library
  - Default: Auto-detected from NuGet runtimes or `./runtimes/cpu/libllama.so` (Linux), `./runtimes/cpu/llama.dll` (Windows), `./runtimes/cpu/libllama.dylib` (macOS)
  - Supports environment variable expansion (e.g., `%USERPROFILE%` or `$HOME`)

- **GpuBackendPath**: Path to GPU backend native library
  - Default: Auto-detected from NuGet runtimes or `./runtimes/cuda/libllama.so` (Linux), `./runtimes/cuda/llama.dll` (Windows)
  - Supports environment variable expansion

- **EnableNativeLogging**: Enable/disable llama.cpp console logging
  - Values: `true` or `false`
  - Default: `false` (disabled - keeps console clean)
  - Use case: Enable for debugging native library issues

### Example Configurations

#### Force CPU Mode
```json
{
  "Runtime": {
    "ForceBackend": "cpu"
  }
}
```

#### Force GPU Mode
```json
{
  "Runtime": {
    "ForceBackend": "cuda"
  }
}
```

#### Custom Library Paths
```json
{
  "Runtime": {
    "CpuBackendPath": "/custom/path/libllama.so",
    "GpuBackendPath": "/custom/path/cuda/libllama.so"
  }
}
```

#### Disable Native Logging (Recommended for Clean Console)
```json
{
  "Runtime": {
    "EnableNativeLogging": false
  }
}
```
This prevents llama.cpp from printing verbose messages to console.

#### Enable Native Logging (For Debugging)
```json
{
  "Runtime": {
    "EnableNativeLogging": true
  }
}
```
Useful when troubleshooting model loading or inference issues.

## Running in Docker

### CPU-Only Mode

Standard Docker run (no GPU support):
```bash
docker run -p 8000:8000 sharpai:latest
```

The container will automatically detect the absence of GPU and use CPU backend.

### GPU-Accelerated Mode

Using NVIDIA Docker runtime:
```bash
docker run --gpus all -p 8000:8000 sharpai:latest
```

**Prerequisites**:
- NVIDIA GPU
- NVIDIA drivers installed on host
- NVIDIA Container Toolkit installed
- Docker configured to use NVIDIA runtime

#### Installing NVIDIA Container Toolkit

On Ubuntu/Debian:
```bash
# Add NVIDIA package repository
distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
curl -s -L https://nvidia.github.io/nvidia-docker/gpgkey | sudo apt-key add -
curl -s -L https://nvidia.github.io/nvidia-docker/$distribution/nvidia-docker.list | sudo tee /etc/apt/sources.list.d/nvidia-docker.list

# Install
sudo apt-get update
sudo apt-get install -y nvidia-container-toolkit

# Restart Docker
sudo systemctl restart docker
```

### Building the Docker Image

From the `src` directory:
```bash
docker build -f SharpAI.Server/Dockerfile -t sharpai:latest .
```

The Dockerfile automatically:
1. Includes both CPU and CUDA backends
2. Organizes native libraries into separate directories
3. Installs CUDA runtime libraries (only loaded if GPU is detected)

## Running Locally (Outside Docker)

### Windows

1. Run the application:
   ```powershell
   dotnet run --project src\SharpAI.Server\SharpAI.Server.csproj
   ```

2. The bootstrapper will automatically detect GPU availability
3. Check console output for backend selection:
   ```
   [NativeLibraryBootstrapper] GPU detected, selecting CUDA backend
   ```

### Linux

1. Run the application:
   ```bash
   dotnet run --project src/SharpAI.Server/SharpAI.Server.csproj
   ```

2. For GPU support, ensure NVIDIA drivers and CUDA toolkit are installed
3. Check console output for backend selection

### macOS

1. Run the application:
   ```bash
   dotnet run --project src/SharpAI.Server/SharpAI.Server.csproj
   ```

2. On Apple Silicon, CPU backend is automatically selected (CUDA not supported)
3. On Intel Macs with NVIDIA GPU (rare), CUDA backend may be selected if drivers are available

## Troubleshooting

### Issue: GPU not detected

**Symptoms**: Application uses CPU backend despite having an NVIDIA GPU

**Solutions**:
1. Check NVIDIA drivers:
   ```bash
   nvidia-smi
   ```
2. Check CUDA libraries (Linux):
   ```bash
   ls /usr/lib/x86_64-linux-gnu/libcuda.so.1
   ```
3. For Docker, ensure `--gpus all` flag is used
4. Check NVIDIA_VISIBLE_DEVICES environment variable:
   ```bash
   echo $NVIDIA_VISIBLE_DEVICES
   ```
5. Force GPU backend in settings if detection is failing:
   ```json
   {"Runtime": {"ForceBackend": "cuda"}}
   ```

### Issue: Native library not found

**Symptoms**:
```
[NativeLibraryBootstrapper] library file not found: ./runtimes/cpu/libllama.so
```

**Solutions**:
1. Verify native libraries are present:
   ```bash
   ls -la runtimes/cpu/
   ls -la runtimes/cuda/
   ```
2. Check the publish output includes both backends
3. Specify explicit paths in settings:
   ```json
   {
     "Runtime": {
       "CpuBackendPath": "/full/path/to/libllama.so",
       "GpuBackendPath": "/full/path/to/cuda/libllama.so"
     }
   }
   ```

### Issue: CUDA initialization fails

**Symptoms**:
```
System.TypeInitializationException: The type initializer for 'LLama.Native.NativeApi' threw an exception.
```

**Solutions**:
1. The bootstrapper should catch this and fall back to CPU automatically
2. If fallback fails, check that CPU backend library exists
3. Force CPU mode in settings:
   ```json
   {"Runtime": {"ForceBackend": "cpu"}}
   ```
4. Verify CUDA runtime libraries are installed:
   - Linux: `libcublas-12-0`, `libcudart-12-0`
   - Windows: CUDA Toolkit 12.x

### Issue: Performance is slow (using CPU when GPU should be used)

**Symptoms**: Model inference is slower than expected

**Solutions**:
1. Check startup logs to confirm GPU backend was selected:
   ```
   [NativeLibraryBootstrapper] GPU detected, selecting CUDA backend
   ```
2. Check LlamaSharp initialization logs:
   ```
   [LlamaSharpEngine] initializing LlamaSharp with GPU acceleration
   ```
3. Monitor GPU usage:
   ```bash
   watch -n 1 nvidia-smi
   ```
4. Verify GPU layers are being used:
   ```
   [LlamaSharpEngine] CUDA detected, N GPU device(s) available
   ```

### Issue: Docker build fails to find native libraries

**Symptoms**: Build completes but warnings show:
```
WARNING: CPU backend library not found
WARNING: CUDA backend library not found
```

**Solutions**:
1. Ensure both backend packages are referenced in SharpAI.csproj:
   ```xml
   <PackageReference Include="LLamaSharp.Backend.Cpu" Version="0.25.0" />
   <PackageReference Include="LLamaSharp.Backend.Cuda12" Version="0.25.0" />
   ```
2. Check NuGet package cache during build:
   - The Dockerfile extracts from `~/.nuget/packages/`
3. Try clearing NuGet cache and rebuilding:
   ```bash
   docker build --no-cache -f SharpAI.Server/Dockerfile -t sharpai:latest .
   ```

## Verification

### Check Backend Selection

Look for these log messages at startup:

**Auto-detection successful**:
```
[NativeLibraryBootstrapper] detected platform: Linux, architecture: X64
[NativeLibraryBootstrapper] nvidia-smi detected GPU: Tesla T4
[NativeLibraryBootstrapper] GPU detected, selecting CUDA backend
[NativeLibraryBootstrapper] configuring cuda backend: ./runtimes/cuda/libllama.so
[NativeLibraryBootstrapper] successfully configured cuda backend
```

**CPU fallback**:
```
[NativeLibraryBootstrapper] detected platform: Linux, architecture: X64
[NativeLibraryBootstrapper] no GPU detected via any method
[NativeLibraryBootstrapper] no GPU detected, selecting CPU backend
[NativeLibraryBootstrapper] configuring cpu backend: ./runtimes/cpu/libllama.so
[NativeLibraryBootstrapper] successfully configured cpu backend
```

**Apple Silicon**:
```
[NativeLibraryBootstrapper] detected platform: OSX, architecture: Arm64
[NativeLibraryBootstrapper] Apple Silicon detected, GPU backend not supported, using CPU
[NativeLibraryBootstrapper] configuring cpu backend: ./runtimes/cpu/libllama.dylib
```

### Verify GPU Usage

When using GPU backend:

1. Check NVIDIA SMI shows GPU memory usage:
   ```bash
   nvidia-smi
   ```

2. Look for GPU utilization (should be >0% during inference)

3. Check LlamaSharp logs:
   ```
   [LlamaSharpEngine] CUDA detected, 1 GPU device(s) available
   [LlamaSharpEngine] initializing LlamaSharp with GPU acceleration (999 layers requested)
   ```

## Advanced Configuration

### Environment Variables

You can use environment variables in path settings:

**Linux/macOS**:
```json
{
  "Runtime": {
    "CpuBackendPath": "$HOME/custom-libs/cpu/libllama.so",
    "GpuBackendPath": "$HOME/custom-libs/cuda/libllama.so"
  }
}
```

**Windows**:
```json
{
  "Runtime": {
    "CpuBackendPath": "%USERPROFILE%\\custom-libs\\cpu\\llama.dll",
    "GpuBackendPath": "%USERPROFILE%\\custom-libs\\cuda\\llama.dll"
  }
}
```

### Testing Different Backends

To test both backends on the same machine:

1. **Test CPU**:
   ```json
   {"Runtime": {"ForceBackend": "cpu"}}
   ```
   Run and verify performance baseline

2. **Test GPU**:
   ```json
   {"Runtime": {"ForceBackend": "cuda"}}
   ```
   Run and compare performance (should be significantly faster)

3. **Test Auto-detection**:
   ```json
   {"Runtime": {"ForceBackend": null}}
   ```
   Run and verify correct backend is selected

## Best Practices

1. **Use auto-detection**: Let the bootstrapper detect the environment unless you have specific requirements
2. **Monitor logs**: Always check startup logs to confirm the correct backend was selected
3. **Test both modes**: If deploying to mixed environments, test both CPU and GPU modes
4. **Keep defaults**: The default paths work for standard deployments; only customize if needed
5. **Docker GPU setup**: Ensure NVIDIA Container Toolkit is properly installed for GPU support
6. **Fallback handling**: The system automatically falls back to CPU if GPU initialization fails
