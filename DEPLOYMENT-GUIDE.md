# SharpAI Deployment Guide

Complete step-by-step guide for deploying SharpAI on Windows, macOS, Linux, Docker, and Docker Compose - with or without GPU acceleration.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Windows Deployment](#windows-deployment)
3. [macOS Deployment](#macos-deployment)
4. [Linux Deployment](#linux-deployment)
5. [Docker Deployment](#docker-deployment)
6. [Docker Compose Deployment](#docker-compose-deployment)
7. [Verification](#verification)
8. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### All Platforms
- .NET 8 SDK or Runtime
- 8GB+ RAM (16GB+ recommended for larger models)
- 10GB+ free disk space (for models)
- Internet connection (for downloading models)

### GPU Requirements (Optional)
- **NVIDIA GPU only** (AMD/Intel not supported)
- NVIDIA drivers installed
- CUDA Toolkit 12.x (for bare-metal GPU)
- For Docker: NVIDIA Container Toolkit

### Download .NET

**Windows:**
```powershell
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0
# Or use winget:
winget install Microsoft.DotNet.SDK.8
```

**macOS:**
```bash
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0
# Or use Homebrew:
brew install dotnet@8
```

**Linux (Ubuntu/Debian):**
```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0

# Or use package manager:
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

---

## Windows Deployment

### Windows CPU-Only Deployment

**Step 1: Clone the repository**
```powershell
cd C:\
git clone https://github.com/jchristn/sharpai.git
cd sharpai
```

**Step 2: Build the application**
```powershell
cd src
dotnet build SharpAI.sln
```

**Step 3: Configure for CPU mode**
```powershell
cd SharpAI.Server
@"
{
  "Runtime": {
    "ForceBackend": "cpu",
    "EnableNativeLogging": false
  }
}
"@ | Out-File -FilePath "sharpai.json" -Encoding UTF8
```

**Step 4: Run the server**
```powershell
dotnet run
```

**Step 5: Verify it's running**
```powershell
# In a new PowerShell window:
curl http://localhost:8000/
```

**Expected output:**
```
[NativeLibraryBootstrapper] detected platform: WINDOWS, architecture: X64
[NativeLibraryBootstrapper] backend forced to: cpu
[NativeLibraryBootstrapper] found library at NuGet path: ...\runtimes\win-x64\native\avx2\llama.dll
[NativeLibraryBootstrapper] successfully configured cpu backend
[SharpAI] starting SharpAI server
```

---

### Windows GPU Deployment (NVIDIA)

**Step 1: Verify GPU and drivers**
```powershell
nvidia-smi
```
Should display your GPU. If not, install NVIDIA drivers from: https://www.nvidia.com/Download/index.aspx

**Step 2: Install CUDA Toolkit 12.x** (if not already installed)
Download from: https://developer.nvidia.com/cuda-downloads

**Step 3: Clone and build** (same as CPU steps 1-2)

**Step 4: Configure for GPU mode**
```powershell
cd src\SharpAI.Server
@"
{
  "Runtime": {
    "ForceBackend": "cuda",
    "EnableNativeLogging": false
  }
}
"@ | Out-File -FilePath "sharpai.json" -Encoding UTF8
```

**Step 5: Run the server**
```powershell
dotnet run
```

**Step 6: Verify GPU is being used**
```powershell
# In another window - monitor GPU usage:
while($true) { nvidia-smi; sleep 2; clear }

# Test inference:
curl -X POST http://localhost:8000/api/pull -H "Content-Type: application/json" -d '{\"name\":\"qwen2.5:0.5b\"}'
```

**Expected output:**
```
[NativeLibraryBootstrapper] detected platform: WINDOWS, architecture: X64
[NativeLibraryBootstrapper] backend forced to: cuda
[NativeLibraryBootstrapper] found library at NuGet path: ...\runtimes\win-x64\native\cuda12\llama.dll
[NativeLibraryBootstrapper] successfully configured cuda backend
[LlamaSharpEngine] CUDA detected, 1 GPU device(s) available
```

---

### Windows Auto-Detection

**Configure for auto-detection:**
```powershell
@"
{
  "Runtime": {
    "ForceBackend": null,
    "EnableNativeLogging": false
  }
}
"@ | Out-File -FilePath "sharpai.json" -Encoding UTF8

dotnet run
```

Will automatically use GPU if available, otherwise CPU.

---

## macOS Deployment

### macOS CPU Deployment (Intel & Apple Silicon)

**Step 1: Clone the repository**
```bash
cd ~/
git clone https://github.com/jchristn/sharpai.git
cd sharpai
```

**Step 2: Build the application**
```bash
cd src
dotnet build SharpAI.sln
```

**Step 3: Configure settings**
```bash
cd SharpAI.Server
cat > sharpai.json << 'EOF'
{
  "Runtime": {
    "ForceBackend": "cpu",
    "EnableNativeLogging": false
  }
}
EOF
```

**Step 4: Run the server**
```bash
dotnet run
```

**Step 5: Verify it's running**
```bash
# In a new terminal:
curl http://localhost:8000/
```

**Expected output (Intel Mac):**
```
[NativeLibraryBootstrapper] detected platform: OSX, architecture: X64
[NativeLibraryBootstrapper] backend forced to: cpu
[NativeLibraryBootstrapper] found library at NuGet path: .../runtimes/osx-x64/native/avx2/libllama.dylib
```

**Expected output (Apple Silicon):**
```
[NativeLibraryBootstrapper] detected platform: OSX, architecture: Arm64
[NativeLibraryBootstrapper] Apple Silicon detected, GPU backend not supported, using CPU
[NativeLibraryBootstrapper] found library at NuGet path: .../runtimes/osx-arm64/native/libllama.dylib
```

---

### macOS GPU Deployment (Legacy Intel with NVIDIA - Rare)

**Note:** Only applicable to older Intel Macs with NVIDIA GPUs. Apple Silicon does NOT support CUDA.

**Step 1: Verify GPU**
```bash
system_profiler SPDisplaysDataType | grep NVIDIA
```

**Step 2: If NVIDIA GPU present, configure for GPU**
```bash
cat > sharpai.json << 'EOF'
{
  "Runtime": {
    "ForceBackend": "cuda",
    "EnableNativeLogging": false
  }
}
EOF

dotnet run
```

**If no CUDA support:** Will automatically fallback to CPU with a warning.

---

## Linux Deployment

### Linux CPU Deployment

**Step 1: Clone the repository**
```bash
cd ~/
git clone https://github.com/jchristn/sharpai.git
cd sharpai
```

**Step 2: Install dependencies**
```bash
sudo apt-get update
sudo apt-get install -y libgomp1 libstdc++6 libc6
```

**Step 3: Build the application**
```bash
cd src
dotnet build SharpAI.sln
```

**Step 4: Configure for CPU**
```bash
cd SharpAI.Server
cat > sharpai.json << 'EOF'
{
  "Runtime": {
    "ForceBackend": "cpu",
    "EnableNativeLogging": false
  }
}
EOF
```

**Step 5: Run the server**
```bash
dotnet run
```

**Step 6: Verify it's running**
```bash
# In a new terminal:
curl http://localhost:8000/
```

**Expected output:**
```
[NativeLibraryBootstrapper] detected platform: LINUX, architecture: X64
[NativeLibraryBootstrapper] backend forced to: cpu
[NativeLibraryBootstrapper] found library at NuGet path: .../runtimes/linux-x64/native/avx2/libllama.so
[NativeLibraryBootstrapper] successfully configured cpu backend
```

---

### Linux GPU Deployment (NVIDIA)

**Step 1: Verify GPU and drivers**
```bash
nvidia-smi
```
Should show your GPU. If not, install NVIDIA drivers:

```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install -y nvidia-driver-535  # Or latest version

# Reboot after installation
sudo reboot
```

**Step 2: Install CUDA Toolkit 12.x**
```bash
# Ubuntu 22.04 example:
wget https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2204/x86_64/cuda-keyring_1.1-1_all.deb
sudo dpkg -i cuda-keyring_1.1-1_all.deb
sudo apt-get update
sudo apt-get install -y cuda-toolkit-12-0

# Add to PATH
echo 'export PATH=/usr/local/cuda/bin:$PATH' >> ~/.bashrc
echo 'export LD_LIBRARY_PATH=/usr/local/cuda/lib64:$LD_LIBRARY_PATH' >> ~/.bashrc
source ~/.bashrc

# Verify
nvcc --version
```

**Step 3: Clone and build** (same as CPU steps 1-3)

**Step 4: Install runtime dependencies**
```bash
sudo apt-get install -y libgomp1 libstdc++6 libc6 libcublas-12-0 libcudart-12-0
```

**Step 5: Configure for GPU**
```bash
cd src/SharpAI.Server
cat > sharpai.json << 'EOF'
{
  "Runtime": {
    "ForceBackend": "cuda",
    "EnableNativeLogging": false
  }
}
EOF
```

**Step 6: Run the server**
```bash
dotnet run
```

**Step 7: Verify GPU usage**
```bash
# In another terminal:
watch -n 1 nvidia-smi

# Test inference:
curl -X POST http://localhost:8000/api/pull \
  -H "Content-Type: application/json" \
  -d '{"name":"qwen2.5:0.5b"}'
```

**Expected output:**
```
[NativeLibraryBootstrapper] detected platform: LINUX, architecture: X64
[NativeLibraryBootstrapper] backend forced to: cuda
[NativeLibraryBootstrapper] found library at NuGet path: .../runtimes/linux-x64/native/cuda12/libllama.so
[NativeLibraryBootstrapper] successfully configured cuda backend
[LlamaSharpEngine] CUDA detected, 1 GPU device(s) available
```

---

## Docker Deployment

### Prerequisites for Docker

**Install Docker:**

**Ubuntu/Debian:**
```bash
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER
# Log out and back in for group changes to take effect
```

**Windows:** Download Docker Desktop from https://www.docker.com/products/docker-desktop

**macOS:** Download Docker Desktop from https://www.docker.com/products/docker-desktop

---

### Docker CPU Deployment

**Step 1: Clone the repository**
```bash
git clone https://github.com/jchristn/sharpai.git
cd sharpai
```

**Step 2: Build the Docker image**
```bash
# Linux/macOS:
./docker-build.sh latest

# Windows:
docker-build.bat latest

# Or manually:
docker build -f src/SharpAI.Server/Dockerfile -t sharpai:latest src/
```

**Step 3: Verify build output**
Look for these messages during build:
```
Found CPU backend: /root/.nuget/packages/llamasharp.backend.cpu/.../libllama.so
Found CUDA backend: /root/.nuget/packages/llamasharp.backend.cuda12/.../libllama.so
Native libraries organized:
total 340M
-rw-r--r-- 1 root root 170M ... libllama.so  (cpu)
total 340M
-rw-r--r-- 1 root root 170M ... libllama.so  (cuda)
```

**Step 4: Run the container (CPU mode)**
```bash
# Using helper script:
./docker-run.sh cpu

# Or manually:
docker run --rm -it -p 8000:8000 sharpai:latest
```

**Step 5: Verify it's running**
```bash
curl http://localhost:8000/
```

**Expected logs:**
```
[NativeLibraryBootstrapper] detected platform: LINUX, architecture: X64
[NativeLibraryBootstrapper] no GPU detected via any method
[NativeLibraryBootstrapper] no GPU detected, selecting CPU backend
[NativeLibraryBootstrapper] found library at custom path: /app/runtimes/cpu/libllama.so
[NativeLibraryBootstrapper] successfully configured cpu backend
```

**Step 6: Test model operations**
```bash
# Pull a model
curl -X POST http://localhost:8000/api/pull \
  -H "Content-Type: application/json" \
  -d '{"name":"qwen2.5:0.5b"}'

# Generate embeddings
curl -X POST http://localhost:8000/api/embed \
  -H "Content-Type: application/json" \
  -d '{"model":"qwen2.5:0.5b","input":"hello world"}'
```

**Step 7: Stop the container**
Press `Ctrl+C`

---

### Docker GPU Deployment (NVIDIA)

**Step 1: Install NVIDIA Container Toolkit**

**Ubuntu/Debian:**
```bash
# Add NVIDIA package repository
distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
curl -fsSL https://nvidia.github.io/libnvidia-container/gpgkey | sudo gpg --dearmor -o /usr/share/keyrings/nvidia-container-toolkit-keyring.gpg
curl -s -L https://nvidia.github.io/libnvidia-container/$distribution/libnvidia-container.list | \
  sed 's#deb https://#deb [signed-by=/usr/share/keyrings/nvidia-container-toolkit-keyring.gpg] https://#g' | \
  sudo tee /etc/apt/sources.list.d/nvidia-container-toolkit.list

# Install
sudo apt-get update
sudo apt-get install -y nvidia-container-toolkit

# Configure Docker
sudo nvidia-ctk runtime configure --runtime=docker
sudo systemctl restart docker
```

**Step 2: Verify NVIDIA Docker works**
```bash
docker run --rm --gpus all nvidia/cuda:12.0-base-ubuntu22.04 nvidia-smi
```
Should show your GPU. If error, check NVIDIA drivers are installed on host.

**Step 3: Build Docker image** (same as CPU deployment step 2)

**Step 4: Run with GPU support**
```bash
# Using helper script:
./docker-run.sh gpu

# Or manually:
docker run --rm -it --gpus all -p 8000:8000 sharpai:latest
```

**Step 5: Verify GPU is detected**

**Expected logs:**
```
[NativeLibraryBootstrapper] detected platform: LINUX, architecture: X64
[NativeLibraryBootstrapper] NVIDIA_VISIBLE_DEVICES detected: 0
[NativeLibraryBootstrapper] GPU detected, selecting CUDA backend
[NativeLibraryBootstrapper] found library at custom path: /app/runtimes/cuda/libllama.so
[NativeLibraryBootstrapper] successfully configured cuda backend
[LlamaSharpEngine] CUDA detected, 1 GPU device(s) available
```

**Step 6: Monitor GPU usage**
```bash
# In another terminal:
watch -n 1 nvidia-smi
```

**Step 7: Test model operations** (same as CPU step 6)

---

### Docker Auto-Detection

**Run with automatic backend detection:**
```bash
# Using helper script (auto-detects GPU on host):
./docker-run.sh auto

# This will:
# - Check if nvidia-smi works on host
# - If yes: starts with --gpus all (GPU mode)
# - If no: starts without GPU (CPU mode)
```

---

### Docker Persistence (Data & Models)

**Create volume for persistent storage:**
```bash
# Create volume
docker volume create sharpai-data

# Run with volume
docker run --rm -it \
  -p 8000:8000 \
  -v sharpai-data:/app/models \
  -v sharpai-data:/app \
  sharpai:latest

# For GPU:
docker run --rm -it \
  --gpus all \
  -p 8000:8000 \
  -v sharpai-data:/app/models \
  -v sharpai-data:/app \
  sharpai:latest
```

**Or use bind mount:**
```bash
mkdir -p ~/sharpai-data/models

docker run --rm -it \
  -p 8000:8000 \
  -v ~/sharpai-data:/app/models \
  sharpai:latest
```

---

## Docker Compose Deployment

### Docker Compose CPU Deployment

**Step 1: Create docker-compose.yml**
```bash
cd ~/sharpai
cat > docker-compose.yml << 'EOF'
version: '3.8'

services:
  sharpai:
    image: sharpai:latest
    container_name: sharpai-cpu
    ports:
      - "8000:8000"
    volumes:
      - sharpai-models:/app/models
      - sharpai-data:/app
    environment:
      - ASPNETCORE_URLS=http://+:8000
    restart: unless-stopped

volumes:
  sharpai-models:
  sharpai-data:
EOF
```

**Step 2: Build the image** (if not already built)
```bash
./docker-build.sh latest
```

**Step 3: Start the service**
```bash
docker-compose up -d
```

**Step 4: View logs**
```bash
docker-compose logs -f sharpai
```

**Expected logs:**
```
[NativeLibraryBootstrapper] no GPU detected, selecting CPU backend
[NativeLibraryBootstrapper] found library at custom path: /app/runtimes/cpu/libllama.so
```

**Step 5: Test the service**
```bash
curl http://localhost:8000/
```

**Step 6: Stop the service**
```bash
docker-compose down
```

**Step 7: Stop and remove volumes**
```bash
docker-compose down -v
```

---

### Docker Compose GPU Deployment

**Step 1: Create docker-compose-gpu.yml**
```bash
cat > docker-compose-gpu.yml << 'EOF'
version: '3.8'

services:
  sharpai:
    image: sharpai:latest
    container_name: sharpai-gpu
    ports:
      - "8000:8000"
    volumes:
      - sharpai-models:/app/models
      - sharpai-data:/app
    environment:
      - ASPNETCORE_URLS=http://+:8000
      - NVIDIA_VISIBLE_DEVICES=all
      - NVIDIA_DRIVER_CAPABILITIES=compute,utility
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: all
              capabilities: [gpu]
    restart: unless-stopped

volumes:
  sharpai-models:
  sharpai-data:
EOF
```

**Step 2: Ensure NVIDIA Container Toolkit is installed** (see Docker GPU step 1)

**Step 3: Start the service**
```bash
docker-compose -f docker-compose-gpu.yml up -d
```

**Step 4: View logs**
```bash
docker-compose -f docker-compose-gpu.yml logs -f sharpai
```

**Expected logs:**
```
[NativeLibraryBootstrapper] NVIDIA_VISIBLE_DEVICES detected: all
[NativeLibraryBootstrapper] GPU detected, selecting CUDA backend
[NativeLibraryBootstrapper] found library at custom path: /app/runtimes/cuda/libllama.so
[LlamaSharpEngine] CUDA detected, 1 GPU device(s) available
```

**Step 5: Verify GPU usage**
```bash
# On host:
nvidia-smi

# Should show container using GPU memory
```

**Step 6: Stop the service**
```bash
docker-compose -f docker-compose-gpu.yml down
```

---

### Docker Compose with Custom Configuration

**Step 1: Create configuration file**
```bash
mkdir -p config
cat > config/sharpai.json << 'EOF'
{
  "Runtime": {
    "ForceBackend": null,
    "EnableNativeLogging": false
  },
  "Storage": {
    "ModelsDirectory": "./models/"
  },
  "Logging": {
    "ConsoleLogging": true,
    "LogDirectory": "./logs/",
    "LogFilename": "sharpai.log"
  }
}
EOF
```

**Step 2: Create docker-compose with config mount**
```bash
cat > docker-compose-config.yml << 'EOF'
version: '3.8'

services:
  sharpai:
    image: sharpai:latest
    container_name: sharpai-custom
    ports:
      - "8000:8000"
    volumes:
      - sharpai-models:/app/models
      - ./config/sharpai.json:/app/sharpai.json:ro
      - sharpai-logs:/app/logs
    environment:
      - ASPNETCORE_URLS=http://+:8000
    restart: unless-stopped

volumes:
  sharpai-models:
  sharpai-logs:
EOF
```

**Step 3: Start with custom config**
```bash
docker-compose -f docker-compose-config.yml up -d
```

---

### Docker Compose Multi-Instance Deployment

**Run multiple instances (e.g., CPU for embeddings, GPU for inference):**

```bash
cat > docker-compose-multi.yml << 'EOF'
version: '3.8'

services:
  sharpai-cpu:
    image: sharpai:latest
    container_name: sharpai-cpu
    ports:
      - "8001:8000"
    volumes:
      - sharpai-models-cpu:/app/models
    environment:
      - ASPNETCORE_URLS=http://+:8000
    restart: unless-stopped

  sharpai-gpu:
    image: sharpai:latest
    container_name: sharpai-gpu
    ports:
      - "8002:8000"
    volumes:
      - sharpai-models-gpu:/app/models
    environment:
      - ASPNETCORE_URLS=http://+:8000
      - NVIDIA_VISIBLE_DEVICES=all
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: all
              capabilities: [gpu]
    restart: unless-stopped

volumes:
  sharpai-models-cpu:
  sharpai-models-gpu:
EOF
```

**Start both instances:**
```bash
docker-compose -f docker-compose-multi.yml up -d
```

**Access:**
- CPU instance: http://localhost:8001
- GPU instance: http://localhost:8002

---

## Verification

### Basic Verification

**1. Check server is running:**
```bash
curl http://localhost:8000/
```
Should return HTML homepage.

**2. Pull a test model:**
```bash
curl -X POST http://localhost:8000/api/pull \
  -H "Content-Type: application/json" \
  -d '{"name":"qwen2.5:0.5b"}'
```

**3. List models:**
```bash
curl http://localhost:8000/api/tags
```

**4. Test embeddings:**
```bash
curl -X POST http://localhost:8000/api/embed \
  -H "Content-Type: application/json" \
  -d '{"model":"qwen2.5:0.5b","input":"test"}'
```

**5. Test completion:**
```bash
curl -X POST http://localhost:8000/api/generate \
  -H "Content-Type: application/json" \
  -d '{"model":"qwen2.5:0.5b","prompt":"Hello"}'
```

---

### GPU Verification

**Check backend selection in logs:**
```
CPU mode: "[NativeLibraryBootstrapper] no GPU detected, selecting CPU backend"
GPU mode: "[NativeLibraryBootstrapper] GPU detected, selecting CUDA backend"
         "[LlamaSharpEngine] CUDA detected, N GPU device(s) available"
```

**Monitor GPU usage:**
```bash
# Bare-metal or Docker with --gpus flag:
nvidia-smi

# Should show:
# - GPU memory allocated
# - GPU utilization >0% during inference
```

**Performance comparison (GPU should be faster):**
```bash
# CPU mode:
time curl -X POST http://localhost:8000/api/embed \
  -H "Content-Type: application/json" \
  -d '{"model":"qwen2.5:0.5b","input":"performance test"}'

# GPU mode (same request):
# Should complete 2-10x faster
```

---

## Troubleshooting

### Issue: "library file not found"

**Bare-metal:**
```bash
# Rebuild to ensure NuGet packages are restored
dotnet clean
dotnet build
```

**Docker:**
```bash
# Rebuild image without cache
docker build --no-cache -f src/SharpAI.Server/Dockerfile -t sharpai:latest src/

# Verify libraries in image
docker run --rm -it --entrypoint sh sharpai:latest
ls -la /app/runtimes/cpu/
ls -la /app/runtimes/cuda/
exit
```

---

### Issue: GPU not detected

**Bare-metal:**
```bash
# Check NVIDIA drivers
nvidia-smi

# Check CUDA toolkit
nvcc --version

# Check library files
ls -la /usr/lib/x86_64-linux-gnu/libcuda.so.1
```

**Docker:**
```bash
# Test NVIDIA Docker
docker run --rm --gpus all nvidia/cuda:12.0-base nvidia-smi

# If fails, reinstall NVIDIA Container Toolkit
sudo apt-get purge nvidia-container-toolkit
sudo apt-get install -y nvidia-container-toolkit
sudo systemctl restart docker
```

---

### Issue: Docker build warnings about missing libraries

**Check NuGet packages during build:**
```bash
# The Dockerfile should show:
# "Found CPU backend: ..."
# "Found CUDA backend: ..."

# If missing, packages may not be in cache
# Solution: Clear and rebuild
docker system prune -a
docker build -f src/SharpAI.Server/Dockerfile -t sharpai:latest src/
```

---

### Issue: Port already in use

**Find process using port:**
```bash
# Linux/macOS:
sudo lsof -i :8000

# Windows:
netstat -ano | findstr :8000

# Kill process or use different port:
docker run -p 8080:8000 sharpai:latest
```

---

### Issue: Container exits immediately

**Check logs:**
```bash
docker logs <container-id>

# Or for Docker Compose:
docker-compose logs sharpai
```

**Common causes:**
- Missing configuration file
- Port conflict
- Insufficient memory

---

### Issue: Models not persisting

**Ensure volumes are configured:**
```bash
# Check volumes
docker volume ls

# Inspect volume
docker volume inspect sharpai-models

# Or use bind mount to host directory
docker run -v $(pwd)/models:/app/models sharpai:latest
```

---

### Issue: Native logging still appears

**Verify configuration:**
```bash
# Check sharpai.json contains:
cat sharpai.json
# Should have: "EnableNativeLogging": false

# Restart server after changing config
```

---

## Summary Checklist

### Bare-Metal Deployment
- [ ] .NET 8 SDK/Runtime installed
- [ ] Repository cloned and built
- [ ] Configuration file created (sharpai.json)
- [ ] Server starts without errors
- [ ] Backend selection confirmed in logs (CPU or GPU)
- [ ] Model pull successful
- [ ] Inference operations work
- [ ] GPU utilization confirmed (if GPU mode)

### Docker Deployment
- [ ] Docker installed
- [ ] NVIDIA Container Toolkit installed (if GPU)
- [ ] Docker image builds successfully
- [ ] Native libraries organized during build
- [ ] Container starts without errors
- [ ] Backend selection confirmed in logs
- [ ] Model operations work in container
- [ ] Volumes configured for persistence
- [ ] GPU utilization in container (if GPU mode)

### Docker Compose Deployment
- [ ] docker-compose.yml created
- [ ] Volumes configured
- [ ] Service starts with `docker-compose up -d`
- [ ] Logs accessible with `docker-compose logs`
- [ ] API accessible on configured port
- [ ] Service restarts on failure
- [ ] GPU resources allocated (if GPU mode)

---

## Quick Reference

### Configuration Locations

**Bare-metal:**
- Windows: `C:\path\to\sharpai\src\SharpAI.Server\sharpai.json`
- Linux/macOS: `~/sharpai/src/SharpAI.Server/sharpai.json`

**Docker:**
- In container: `/app/sharpai.json`
- Via volume: Mount host file to `/app/sharpai.json`

### Default Ports
- HTTP: 8000
- Can be changed in docker-compose.yml or with `-p` flag

### Common Commands

**Bare-metal:**
```bash
# Start
dotnet run --project src/SharpAI.Server/SharpAI.Server.csproj

# Build
dotnet build src/SharpAI.sln
```

**Docker:**
```bash
# Build
./docker-build.sh latest

# Run CPU
./docker-run.sh cpu

# Run GPU
./docker-run.sh gpu

# Run auto
./docker-run.sh auto
```

**Docker Compose:**
```bash
# Start
docker-compose up -d

# Stop
docker-compose down

# Logs
docker-compose logs -f

# Restart
docker-compose restart
```

---

## Support

For issues, questions, or contributions:
- GitHub: https://github.com/jchristn/sharpai
- Issues: https://github.com/jchristn/sharpai/issues
- Documentation: See RUNTIME-BACKENDS.md and TESTING-GUIDE.md
