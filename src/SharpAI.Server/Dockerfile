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

WORKDIR /app
COPY --from=build /app/publish .

# Make startup script executable
RUN chmod +x start-linux.sh

EXPOSE 8000
ENTRYPOINT ["./start-linux.sh"]
