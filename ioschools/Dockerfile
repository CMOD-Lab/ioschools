# =============================================================================
# Multi-stage Dockerfile for ioschools ASP.NET MVC Application
# Target: AWS EKS (Elastic Kubernetes Service)
# Build Stage: mcr.microsoft.com/dotnet/sdk:8.0
# Runtime Stage: mcr.microsoft.com/dotnet/sdk:6.0-alpine (explicit base image)
# =============================================================================

# ---- Build Stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

WORKDIR /src

# Copy project file first for optimal layer caching (NuGet restore)
COPY ["ioschools/ioschools.csproj", "ioschools/"]

# Restore NuGet dependencies (cached layer)
RUN dotnet restore "ioschools/ioschools.csproj"

# Copy remaining source code
COPY . .

# Build in Release configuration
RUN dotnet build "ioschools/ioschools.csproj" -c Release --no-restore

# Publish application to /app/publish
RUN dotnet publish "ioschools/ioschools.csproj" -c Release -o /app/publish --no-build

# ---- Runtime Stage ----
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS runtime

# Set working directory
WORKDIR /app

# Create non-root user for security
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

# Set .NET environment variables
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:80 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=UTC \
    LANG=en_US.UTF-8 \
    LC_ALL=en_US.UTF-8

# Copy published artifacts from build stage
COPY --from=builder /app/publish .

# Set ownership to non-root user
RUN chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Expose application port
EXPOSE 80

# Entry point
ENTRYPOINT ["dotnet", "ioschools.dll"]
