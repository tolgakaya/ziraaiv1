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

# Create uploads directory
RUN mkdir -p /app/wwwroot/uploads/plant-images && \
    chmod 755 /app/wwwroot/uploads/plant-images

# Install ICU libraries for Turkish culture support and debugging tools
RUN apt-get update && apt-get install -y libicu-dev netcat-traditional curl && rm -rf /var/lib/apt/lists/*

# Railway environment configuration
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_RUNNING_IN_CONTAINER=true

# .NET Core logging configuration for debugging
ENV Logging__LogLevel__Default=Information
ENV Logging__LogLevel__Microsoft=Warning
ENV Logging__LogLevel__System=Warning
ENV Logging__Console__IncludeScopes=false

# Default Railway database configuration (will be overridden by Railway variables)
ENV ConnectionStrings__DArchPgContext="Host=localhost;Port=5432;Database=ziraai;Username=postgres;Password=password"

# Enhanced startup with detailed error capture
ENTRYPOINT ["sh", "-c", "echo 'Starting .NET application on port 8080...' && echo 'Current directory:' && pwd && echo 'Files present:' && ls -la && echo 'Environment check:' && env | grep -E '(ASPNETCORE|DATABASE|ConnectionStrings)' && echo 'Testing database connectivity...' && timeout 10 nc -z caboose.proxy.rlwy.net 23899 && echo 'Database connection OK' || echo 'Database connection FAILED' && echo 'Starting dotnet with detailed logging...' && export ASPNETCORE_ENVIRONMENT=Production && export Logging__LogLevel__Default=Information && export Logging__LogLevel__Microsoft=Information && dotnet WebAPI.dll --verbose 2>&1 | tee /tmp/app.log || (echo 'Application crashed. Exit code:' $? && echo 'Last 50 lines of log:' && tail -50 /tmp/app.log 2>/dev/null && echo 'Keeping container alive for debugging...' && sleep 300)"]