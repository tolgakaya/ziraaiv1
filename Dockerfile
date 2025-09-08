# Railway .NET 9.0 Minimal Working Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
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
RUN dotnet build "WebAPI.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Copy appsettings files explicitly
COPY --from=build /src/WebAPI/appsettings*.json ./


# Create uploads directory
RUN mkdir -p /app/wwwroot/uploads/plant-images && \
    chmod 755 /app/wwwroot/uploads/plant-images

# Install ICU libraries for Turkish culture support and debugging tools
RUN apt-get update && apt-get install -y libicu-dev netcat-traditional curl && rm -rf /var/lib/apt/lists/*

# Railway environment configuration
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
# Environment will be set by Railway (Development, Staging, Production)
ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Staging}
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_RUNNING_IN_CONTAINER=true

# .NET Core logging configuration - force console output
ENV Logging__LogLevel__Default=Information
ENV Logging__LogLevel__Microsoft=Information
ENV Logging__LogLevel__System=Information
ENV Logging__Console__IncludeScopes=true
ENV Logging__Console__LogLevel__Default=Information
ENV DOTNET_CONSOLE_ANSI_COLOR=1
ENV ASPNETCORE_LOGGING__CONSOLE__DISABLECOLORS=false

# Railway service configuration - Redis enabled for staging
ENV UseHangfire=false
ENV UseRedis=true
ENV UseRabbitMQ=false
ENV UseElasticsearch=false
ENV TaskScheduler__UseTaskScheduler=false
ENV FileStorage__Provider=Local

# Default Railway database configuration (will be overridden by Railway variables)
ENV ConnectionStrings__DArchPgContext="Host=localhost;Port=5432;Database=ziraai;Username=postgres;Password=password"

# Clean Railway startup - database already migrated
ENTRYPOINT ["dotnet", "WebAPI.dll"]