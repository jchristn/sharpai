# SharpAI Deployment Quick Start

Choose your deployment method and follow the corresponding guide.

## Documentation Overview

- **[DEPLOYMENT-GUIDE.md](DEPLOYMENT-GUIDE.md)** - Complete step-by-step deployment guide for all platforms
- **[RUNTIME-BACKENDS.md](RUNTIME-BACKENDS.md)** - Detailed backend configuration and troubleshooting
- **[TESTING-GUIDE.md](TESTING-GUIDE.md)** - Comprehensive testing procedures
- **[IMPLEMENTATION-SUMMARY.md](IMPLEMENTATION-SUMMARY.md)** - Technical implementation details

## Quick Start by Platform

### Windows (Bare-Metal)

```powershell
# Clone
git clone https://github.com/jchristn/sharpai.git
cd sharpai\src

# Build
dotnet build SharpAI.sln

# Run
cd SharpAI.Server
dotnet run

# Test
curl http://localhost:8000/
```

**See:** [DEPLOYMENT-GUIDE.md - Windows Deployment](DEPLOYMENT-GUIDE.md#windows-deployment)

---

### macOS (Bare-Metal)

```bash
# Clone
git clone https://github.com/jchristn/sharpai.git
cd sharpai/src

# Build
dotnet build SharpAI.sln

# Run
cd SharpAI.Server
dotnet run

# Test
curl http://localhost:8000/
```

**See:** [DEPLOYMENT-GUIDE.md - macOS Deployment](DEPLOYMENT-GUIDE.md#macos-deployment)

---

### Linux (Bare-Metal)

```bash
# Clone
git clone https://github.com/jchristn/sharpai.git
cd sharpai/src

# Build
dotnet build SharpAI.sln

# Run
cd SharpAI.Server
dotnet run

# Test
curl http://localhost:8000/
```

**See:** [DEPLOYMENT-GUIDE.md - Linux Deployment](DEPLOYMENT-GUIDE.md#linux-deployment)

---

### Docker (CPU Mode)

```bash
# Clone
git clone https://github.com/jchristn/sharpai.git
cd sharpai

# Build image
./docker-build.sh latest

# Run
./docker-run.sh cpu
# Or: docker run --rm -it -p 8000:8000 sharpai:latest

# Test
curl http://localhost:8000/
```

**See:** [DEPLOYMENT-GUIDE.md - Docker Deployment](DEPLOYMENT-GUIDE.md#docker-deployment)

---

### Docker (GPU Mode)

```bash
# Prerequisites: NVIDIA GPU, drivers, and NVIDIA Container Toolkit

# Clone
git clone https://github.com/jchristn/sharpai.git
cd sharpai

# Build image
./docker-build.sh latest

# Run with GPU
./docker-run.sh gpu
# Or: docker run --rm -it --gpus all -p 8000:8000 sharpai:latest

# Test
curl http://localhost:8000/
```

**See:** [DEPLOYMENT-GUIDE.md - Docker GPU Deployment](DEPLOYMENT-GUIDE.md#docker-gpu-deployment-nvidia)

---

### Docker Compose (CPU Mode)

```bash
# Clone
git clone https://github.com/jchristn/sharpai.git
cd sharpai

# Build image
./docker-build.sh latest

# Start service
docker-compose up -d

# View logs
docker-compose logs -f sharpai

# Test
curl http://localhost:8000/

# Stop
docker-compose down
```

**See:** [DEPLOYMENT-GUIDE.md - Docker Compose CPU](DEPLOYMENT-GUIDE.md#docker-compose-cpu-deployment)

---

### Docker Compose (GPU Mode)

```bash
# Prerequisites: NVIDIA GPU, drivers, and NVIDIA Container Toolkit

# Clone
git clone https://github.com/jchristn/sharpai.git
cd sharpai

# Build image
./docker-build.sh latest

# Start service with GPU
docker-compose -f docker-compose-gpu.yml up -d

# View logs
docker-compose -f docker-compose-gpu.yml logs -f sharpai

# Test
curl http://localhost:8000/

# Stop
docker-compose -f docker-compose-gpu.yml down
```

**See:** [DEPLOYMENT-GUIDE.md - Docker Compose GPU](DEPLOYMENT-GUIDE.md#docker-compose-gpu-deployment)

---

## Configuration

### Minimal Configuration (CPU Mode)

Create `sharpai.json`:
```json
{
  "Runtime": {
    "ForceBackend": "cpu",
    "EnableNativeLogging": false
  }
}
```

### GPU Configuration

Create `sharpai.json`:
```json
{
  "Runtime": {
    "ForceBackend": "cuda",
    "EnableNativeLogging": false
  }
}
```

### Auto-Detection (Recommended)

Create `sharpai.json`:
```json
{
  "Runtime": {
    "ForceBackend": null,
    "EnableNativeLogging": false
  }
}
```

**See:** [RUNTIME-BACKENDS.md - Configuration](RUNTIME-BACKENDS.md#configuration)

---

## Verification

### Quick Test Sequence

```bash
# 1. Check server
curl http://localhost:8000/

# 2. Pull a model
curl -X POST http://localhost:8000/api/pull \
  -H "Content-Type: application/json" \
  -d '{"name":"qwen2.5:0.5b"}'

# 3. Test embeddings
curl -X POST http://localhost:8000/api/embed \
  -H "Content-Type: application/json" \
  -d '{"model":"qwen2.5:0.5b","input":"hello world"}'

# 4. Test completion
curl -X POST http://localhost:8000/api/generate \
  -H "Content-Type: application/json" \
  -d '{"model":"qwen2.5:0.5b","prompt":"Hello"}'
```

---

## Troubleshooting

### Common Issues

**"library file not found"**
- Bare-metal: Run `dotnet clean && dotnet build`
- Docker: Rebuild with `docker build --no-cache`

**GPU not detected**
- Verify: `nvidia-smi` works
- Docker: Check NVIDIA Container Toolkit installed
- Run: `docker run --rm --gpus all nvidia/cuda:12.0-base nvidia-smi`

**Port already in use**
- Change port: `docker run -p 8080:8000 sharpai:latest`
- Or kill process using port 8000

**See:** [DEPLOYMENT-GUIDE.md - Troubleshooting](DEPLOYMENT-GUIDE.md#troubleshooting)

---

## Platform-Specific Notes

### Windows
- Use PowerShell (not Command Prompt) for best compatibility
- GPU requires NVIDIA drivers + CUDA Toolkit 12.x
- Docker Desktop must be running for container deployments

### macOS
- Apple Silicon (M1/M2/M3) does NOT support GPU acceleration
- Intel Macs with NVIDIA GPU are rare but supported
- Use Homebrew for .NET installation: `brew install dotnet@8`

### Linux
- Tested on Ubuntu 20.04+, Debian 11+
- GPU requires NVIDIA drivers, CUDA Toolkit, and Container Toolkit
- Use package managers for .NET: `sudo apt-get install dotnet-sdk-8.0`

---

## What to Expect

### Successful CPU Deployment Logs
```
[NativeLibraryBootstrapper] detected platform: <PLATFORM>, architecture: X64
[NativeLibraryBootstrapper] no GPU detected, selecting CPU backend
[NativeLibraryBootstrapper] found library at <path>
[NativeLibraryBootstrapper] successfully configured cpu backend
[SharpAI] starting SharpAI server
```

### Successful GPU Deployment Logs
```
[NativeLibraryBootstrapper] detected platform: <PLATFORM>, architecture: X64
[NativeLibraryBootstrapper] GPU detected, selecting CUDA backend
[NativeLibraryBootstrapper] found library at <path>
[NativeLibraryBootstrapper] successfully configured cuda backend
[LlamaSharpEngine] CUDA detected, N GPU device(s) available
[SharpAI] starting SharpAI server
```

### Clean Console
With `"EnableNativeLogging": false` (default), you won't see verbose llama.cpp messages.

---

## Performance Expectations

### CPU Mode
- Good for: Small models (<3B parameters), embeddings, testing
- Speed: Baseline performance
- Resource: Uses all CPU cores

### GPU Mode
- Good for: All model sizes, production workloads
- Speed: 2-10x faster than CPU (model dependent)
- Resource: Uses GPU VRAM (4GB+ recommended)

**Compare performance:**
```bash
# Time embedding generation
time curl -X POST http://localhost:8000/api/embed \
  -H "Content-Type: application/json" \
  -d '{"model":"qwen2.5:0.5b","input":"performance test"}'

# GPU should complete significantly faster
```

---

## Next Steps

1. ✅ Choose deployment method from above
2. ✅ Follow the corresponding section in DEPLOYMENT-GUIDE.md
3. ✅ Verify deployment with test commands
4. ✅ Configure for your use case (see RUNTIME-BACKENDS.md)
5. ✅ Run full tests (see TESTING-GUIDE.md)

---

## Support Resources

- **Full Deployment Guide:** [DEPLOYMENT-GUIDE.md](DEPLOYMENT-GUIDE.md)
- **Backend Configuration:** [RUNTIME-BACKENDS.md](RUNTIME-BACKENDS.md)
- **Testing Procedures:** [TESTING-GUIDE.md](TESTING-GUIDE.md)
- **GitHub Issues:** https://github.com/jchristn/sharpai/issues
- **GitHub Repository:** https://github.com/jchristn/sharpai
