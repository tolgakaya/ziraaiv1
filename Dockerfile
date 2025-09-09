# Multi-Environment .NET 9.0 Dockerfile
# Build arguments for environment configuration
ARG TARGET_ENVIRONMENT=Staging
ARG BUILD_CONFIGURATION=Release

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION
WORKDIR /src

# Copy project files
COPY ["WebAPI/WebAPI.csproj", "WebAPI/"]
COPY ["Business/Business.csproj", "Business/"]
COPY ["DataAccess/DataAccess.csproj", "DataAccess/"]
COPY ["Entities/Entities.csproj", "Entities/"]
COPY ["Core/Core.csproj", "Core/"]

# Restore dependencies
RUN dotnet restore "WebAPI/WebAPI.csproj"

# Copy all source code
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

# appsettings files are already included in the publish output
# No need to copy them separately as dotnet publish includes them


# Create uploads directory
RUN mkdir -p /app/wwwroot/uploads/plant-images && \
    chmod 755 /app/wwwroot/uploads/plant-images

# Install ICU libraries for Turkish culture support and debugging tools
RUN apt-get update && apt-get install -y libicu-dev netcat-traditional curl && rm -rf /var/lib/apt/lists/*

# Multi-environment configuration
ARG TARGET_ENVIRONMENT
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
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
ENV ASPNETCORE_LOGGING__CONSOLE__DISABLECOLORS=false

# Default service configuration (will be overridden by cloud provider variables)
ENV UseHangfire=false
ENV UseRedis=true
ENV UseRabbitMQ=false
ENV UseElasticsearch=false
ENV TaskScheduler__UseTaskScheduler=false
ENV FileStorage__Provider=Local

# Default database configuration (will be overridden by cloud provider variables)
ENV ConnectionStrings__DArchPgContext="Host=localhost;Port=5432;Database=ziraai;Username=postgres;Password=password"

# Clean cloud startup - database migration handled by deployment pipeline
ENTRYPOINT ["dotnet", "WebAPI.dll"]