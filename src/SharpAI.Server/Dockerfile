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
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS build
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
RUN dotnet publish "SharpAI.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:GeneratePackageOnBuild=false /p:ErrorOnDuplicatePublishOutputFiles=false


#
#
# Runtime stage
#
#
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
RUN apt-get update && apt-get install -y iputils-ping traceroute net-tools curl wget dnsutils iproute2 file vim procps && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8000
ENTRYPOINT ["dotnet", "SharpAI.Server.dll"]