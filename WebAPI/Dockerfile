# Railway Production Dockerfile for ZiraAI
# Optimized for .NET 9.0 Web API with PostgreSQL

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Railway expects applications to listen on 0.0.0.0:$PORT
# Default to 8080 if PORT not set
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files for dependency restoration
COPY ["WebAPI/WebAPI.csproj", "WebAPI/"]
COPY ["Business/Business.csproj", "Business/"]
COPY ["DataAccess/DataAccess.csproj", "DataAccess/"]
COPY ["Entities/Entities.csproj", "Entities/"]
COPY ["Core/Core.csproj", "Core/"]

# Restore dependencies
RUN dotnet restore "WebAPI/WebAPI.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/WebAPI"
RUN dotnet build "WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directory for file uploads
RUN mkdir -p /app/wwwroot/uploads/plant-images && \
    chmod 755 /app/wwwroot/uploads/plant-images

# Railway health check endpoint
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Railway deployment optimization
# Globalization support enabled for Turkish culture
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Install ICU libraries for globalization support
RUN apt-get update && apt-get install -y \
    libicu-dev \
    && rm -rf /var/lib/apt/lists/*

# Create diagnostic startup script for Railway debugging  
RUN echo '#!/bin/sh' > /app/startup-debug.sh && \
    echo 'echo "=== RAILWAY STARTUP DIAGNOSTICS ==="' >> /app/startup-debug.sh && \
    echo 'echo "Environment: $ASPNETCORE_ENVIRONMENT"' >> /app/startup-debug.sh && \
    echo 'echo "URLs: $ASPNETCORE_URLS"' >> /app/startup-debug.sh && \
    echo 'echo "Port: $PORT"' >> /app/startup-debug.sh && \
    echo 'echo "Time: $(date)"' >> /app/startup-debug.sh && \
    echo 'echo "Working Directory: $(pwd)"' >> /app/startup-debug.sh && \
    echo 'echo "Files in app:"' >> /app/startup-debug.sh && \
    echo 'ls -la' >> /app/startup-debug.sh && \
    echo 'echo "=== ENVIRONMENT VARIABLES ==="' >> /app/startup-debug.sh && \
    echo 'env | grep -E "(ConnectionStrings|TaskScheduler|ASPNETCORE)" | sort || echo "No matching env vars"' >> /app/startup-debug.sh && \
    echo 'echo "=== STARTING APPLICATION ==="' >> /app/startup-debug.sh && \
    echo 'echo "Executing: dotnet WebAPI.dll"' >> /app/startup-debug.sh && \
    echo 'exec dotnet WebAPI.dll' >> /app/startup-debug.sh

RUN chmod +x /app/startup-debug.sh

# Cache bust for Railway - force rebuild with diagnostic script
RUN echo "Diagnostic script v3 - $(date)" > /app/build-info.txt && cat /app/startup-debug.sh

# Force Railway to show all output - disable buffering
ENV DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION=1
ENV DOTNET_CLI_FORCE_UTF8_ENCODING=true

# Create wrapper that forces output to Railway logs
RUN echo '#!/bin/sh' > /app/railway-wrapper.sh && \
    echo 'echo "RAILWAY FORCE LOG START - $(date)"' >> /app/railway-wrapper.sh && \
    echo 'echo "Container starting..."' >> /app/railway-wrapper.sh && \
    echo '/bin/sh /app/startup-debug.sh 2>&1 | tee /proc/1/fd/1' >> /app/railway-wrapper.sh && \
    chmod +x /app/railway-wrapper.sh

ENTRYPOINT ["/bin/sh", "/app/railway-wrapper.sh"]