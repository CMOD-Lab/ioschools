# blocker-30: Dockerfile for ASP.NET Web Forms application on EKS
# Multi-stage build to reduce final image size (blocker-33, 34, 35, 36, 37)
# Uses Windows Server Core with IIS for ASP.NET Web Forms compatibility

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2019 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY ioschools.sln .
COPY ioschools/ioschools.csproj ioschools/
COPY ioschools.DB/ioschools.DB.csproj ioschools.DB/
COPY ioschools.Data/ioschools.Data.csproj ioschools.Data/
COPY ioschools.Caching/ioschools.Caching.csproj ioschools.Caching/

# Restore NuGet packages
RUN nuget restore ioschools.sln

# Copy remaining source files
COPY . .

# Build in Release configuration
RUN msbuild ioschools/ioschools.csproj /p:Configuration=Release /p:DeployOnBuild=true /p:PublishProfile=FolderProfile /p:PublishUrl=C:\publish

# Stage 2: Runtime image
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2019 AS runtime
WORKDIR /inetpub/wwwroot

# Copy only published output (no source, no PDB files)
COPY --from=build C:\publish .

# Remove PDB files to reduce image size (blocker-36, 37)
RUN powershell -Command "Get-ChildItem -Path . -Filter *.pdb -Recurse | Remove-Item -Force"

# Health check endpoint (mandatory containerization requirement)
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD powershell -Command "try { $response = Invoke-WebRequest -Uri 'http://localhost/health' -UseBasicParsing; if ($response.StatusCode -eq 200) { exit 0 } else { exit 1 } } catch { exit 1 }"

# Environment variables for connection strings and configuration
# These should be injected via Kubernetes Secrets and ConfigMaps
ENV DB_CONNECTION_STRING="" \
    DB_CONNECTION_STRING_T="" \
    APP_HTTP_HOST="" \
    CACHE_SCHEDULER_URL="" \
    CLICKATELL_API_URL="" \
    CLICKATELL_USERNAME="" \
    CLICKATELL_PASSWORD="" \
    CLICKATELL_API_ID=""

EXPOSE 80
