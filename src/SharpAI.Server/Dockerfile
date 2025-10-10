#
# SharpAI.Server Dockerfile
# Run docker build from src directory, not from SharpAI.Server
#

#
# Build stage
#
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy source
COPY . .

# Build and publish
WORKDIR /src/SharpAI.Server
RUN dotnet publish SharpAI.Server.csproj -c Release -o /app/publish \
    --no-self-contained \
    /p:UseAppHost=false \
    /p:GeneratePackageOnBuild=false
    
#
# Runtime stage
# Base image is Debian-based, works on any host (Debian/Ubuntu/etc.)
#
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Install runtime dependencies required by LlamaSharp native libraries
# - libgomp1: OpenMP runtime for parallel processing
# - libstdc++6: C++ standard library
# Note: Both CPU and CUDA backends are included from NuGet packages
# For GPU support, run container with: docker run --gpus all ...
RUN apt-get update && apt-get install -y \
    libgomp1 \
    libstdc++6 \
    iputils-ping \
    traceroute \
    net-tools \
    curl \
    wget \
    dnsutils \
    iproute2 \
    vim \
    file \
    && rm -rf /var/lib/apt/lists/*

# Install NVIDIA CUDA runtime libraries (amd64 only - ARM64 doesn't support NVIDIA GPUs)
RUN if [ "$(dpkg --print-architecture)" = "amd64" ]; then \
        apt-get update && apt-get install -y --no-install-recommends \
        gnupg2 ca-certificates && \
        curl -fsSL https://developer.download.nvidia.com/compute/cuda/repos/debian12/x86_64/3bf863cc.pub | gpg --dearmor -o /usr/share/keyrings/cuda-archive-keyring.gpg && \
        echo "deb [signed-by=/usr/share/keyrings/cuda-archive-keyring.gpg] https://developer.download.nvidia.com/compute/cuda/repos/debian12/x86_64/ /" > /etc/apt/sources.list.d/cuda.list && \
        apt-get update && apt-get install -y --no-install-recommends \
        cuda-cudart-12-4 \
        libcublas-12-4 && \
        rm -rf /var/lib/apt/lists/*; \
    fi

WORKDIR /app
COPY --from=build /app/publish .

# Make startup script executable
RUN chmod +x start-linux.sh

EXPOSE 8000
ENTRYPOINT ["./start-linux.sh"]
