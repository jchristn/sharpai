#
#
# Run the docker build command from src, not from the project directory
#
#

#
#
# Build stage
#
#
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy source
COPY . .

# Restore dependencies
RUN dotnet restore "SharpAI.Server/SharpAI.Server.csproj"

# Build the application
WORKDIR /src/SharpAI.Server
RUN dotnet build "SharpAI.Server.csproj" -c Release -o /app/build /p:GeneratePackageOnBuild=false

#
#
# Publish stage
#
#
FROM build AS publish
RUN dotnet publish SharpAI.Server.csproj -c Release --no-self-contained \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:GeneratePackageOnBuild=false \
    /p:ErrorOnDuplicatePublishOutputFiles=false

# Organize native libraries into separate directories
# LlamaSharp backends place their native libraries in runtimes/{rid}/native/
# We need to extract CPU and CUDA backends separately and organize them
WORKDIR /app/publish

# Create runtime directories
RUN mkdir -p runtimes/cpu runtimes/cuda

# The NuGet packages cache contains the native libraries
# We'll extract them from the restore cache
RUN NUGET_PACKAGES=$(dotnet nuget locals global-packages --list | cut -d ' ' -f 2) && \
    echo "NuGet packages location: $NUGET_PACKAGES" && \
    # Find and copy CPU backend native library \
    CPU_PKG=$(find $NUGET_PACKAGES/llamasharp.backend.cpu -type f -name "libllama.so" | head -n 1) && \
    if [ -f "$CPU_PKG" ]; then \
        echo "Found CPU backend: $CPU_PKG" && \
        cp "$CPU_PKG" runtimes/cpu/libllama.so; \
    else \
        echo "WARNING: CPU backend library not found"; \
    fi && \
    # Find and copy CUDA backend native library \
    CUDA_PKG=$(find $NUGET_PACKAGES/llamasharp.backend.cuda12 -type f -name "libllama.so" | head -n 1) && \
    if [ -f "$CUDA_PKG" ]; then \
        echo "Found CUDA backend: $CUDA_PKG" && \
        cp "$CUDA_PKG" runtimes/cuda/libllama.so; \
    else \
        echo "WARNING: CUDA backend library not found"; \
    fi && \
    # List what we have \
    echo "Native libraries organized:" && \
    ls -lah runtimes/cpu/ && \
    ls -lah runtimes/cuda/


#
#
# Runtime stage
#
#
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Install runtime dependencies for both CPU and CUDA
# CPU dependencies: libgomp1, libstdc++6, libc6, libopenblas0, liblapack3
# CUDA dependencies: libcublas, libcudart (from CUDA toolkit)
RUN apt-get update && apt-get install -y \
    libgomp1 libstdc++6 libc6 libopenblas0 liblapack3 \
    iputils-ping traceroute net-tools curl wget dnsutils iproute2 file vim procps \
    && rm -rf /var/lib/apt/lists/*

# Install CUDA runtime libraries (optional, only loaded if GPU is detected)
# Using CUDA 12.x compatible libraries
RUN apt-get update && apt-get install -y --no-install-recommends \
    gnupg2 ca-certificates && \
    curl -fsSL https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2204/x86_64/3bf863cc.pub | apt-key add - && \
    echo "deb https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2204/x86_64 /" > /etc/apt/sources.list.d/cuda.list && \
    apt-get update && apt-get install -y --no-install-recommends \
    libcublas-12-0 \
    libcudart-12-0 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8000
ENTRYPOINT ["dotnet", "SharpAI.Server.dll"]