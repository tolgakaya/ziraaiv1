# Multi-Environment .NET 9.0 WebAPI Dockerfile
# Build arguments for environment configuration
ARG TARGET_ENVIRONMENT=Staging
ARG BUILD_CONFIGURATION=Release

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION
WORKDIR /src

# Copy project files (using paths from repository root)
COPY ["WebAPI/WebAPI.csproj", "WebAPI/"]
COPY ["Business/Business.csproj", "Business/"]
COPY ["DataAccess/DataAccess.csproj", "DataAccess/"]
COPY ["Entities/Entities.csproj", "Entities/"]
COPY ["Core/Core.csproj", "Core/"]

# Restore dependencies
RUN dotnet restore "WebAPI/WebAPI.csproj"

# Copy all source code (from repository root)
COPY . .

# Build
WORKDIR "/src/WebAPI"
RUN dotnet build "WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION
RUN dotnet publish "WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
ARG TARGET_ENVIRONMENT
WORKDIR /app
COPY --from=publish /app/publish .

# Create config directory for backup files
RUN mkdir -p /app/config

# Copy all appsettings files first 
COPY --from=build /src/WebAPI/appsettings*.json /app/config/

# Copy and rename environment-specific appsettings file as main appsettings.json
# This ensures .NET Core loads the correct environment configuration
RUN if [ "$TARGET_ENVIRONMENT" = "Development" ] && [ -f /app/config/appsettings.Development.json ]; then \
        cp /app/config/appsettings.Development.json /app/appsettings.json; \
    elif [ "$TARGET_ENVIRONMENT" = "Staging" ] && [ -f /app/config/appsettings.Staging.json ]; then \
        cp /app/config/appsettings.Staging.json /app/appsettings.json; \
    elif [ "$TARGET_ENVIRONMENT" = "Production" ] && [ -f /app/config/appsettings.Production.json ]; then \
        cp /app/config/appsettings.Production.json /app/appsettings.json; \
    else \
        echo "Warning: No environment-specific appsettings found, using default"; \
        cp /app/config/appsettings.json /app/appsettings.json 2>/dev/null || echo "No default appsettings.json found"; \
    fi

# Create logs directory for API
RUN mkdir -p /app/logs/api && \
    chmod 755 /app/logs/api

# Create uploads directory for file storage
RUN mkdir -p /app/wwwroot/uploads/plant-images && \
    chmod 755 /app/wwwroot/uploads/plant-images

# Install ICU libraries for Turkish culture support and debugging tools
RUN apt-get update && apt-get install -y libicu-dev netcat-traditional curl && rm -rf /var/lib/apt/lists/*

# Multi-environment configuration
ARG TARGET_ENVIRONMENT
ENV ASPNETCORE_ENVIRONMENT=$TARGET_ENVIRONMENT
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_RUNNING_IN_CONTAINER=true

# .NET Core logging configuration - optimized for cloud environments
ENV Logging__LogLevel__Default=Information
ENV Logging__LogLevel__Microsoft=Information
ENV Logging__LogLevel__System=Information
ENV Logging__Console__IncludeScopes=true
ENV Logging__Console__LogLevel__Default=Information
ENV DOTNET_CONSOLE_ANSI_COLOR=1

# Default service configuration (will be overridden by cloud provider variables)
ENV UseHangfire=false
ENV UseRedis=true
ENV UseRabbitMQ=true
ENV UseElasticsearch=false

# Default database configuration (will be overridden by cloud provider variables)
ENV ConnectionStrings__DArchPgContext="Host=localhost;Port=5432;Database=ziraai;Username=postgres;Password=password"
ENV TaskSchedulerOptions__ConnectionString="Host=localhost;Port=5432;Database=ziraai;Username=postgres;Password=password"

# Default RabbitMQ configuration (will be overridden by cloud provider variables)
ENV RabbitMQ__ConnectionString="amqp://guest:guest@localhost:5672/"

# Default Redis configuration (will be overridden by cloud provider variables) 
ENV CacheOptions__Host="localhost"
ENV CacheOptions__Port="6379"
ENV CacheOptions__Password=""
ENV CacheOptions__Database="0"
ENV CacheOptions__Ssl="false"

# Health check for API
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Clean cloud startup - database migration handled by deployment pipeline
ENTRYPOINT ["dotnet", "WebAPI.dll"]