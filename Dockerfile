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

# .NET Core logging configuration - force console output
ENV Logging__LogLevel__Default=Information
ENV Logging__LogLevel__Microsoft=Information
ENV Logging__LogLevel__System=Information
ENV Logging__Console__IncludeScopes=true
ENV Logging__Console__LogLevel__Default=Information
ENV DOTNET_CONSOLE_ANSI_COLOR=1
ENV ASPNETCORE_LOGGING__CONSOLE__DISABLECOLORS=false

# Railway service configuration - disable optional services
ENV UseHangfire=false
ENV UseRedis=false
ENV UseRabbitMQ=false
ENV UseElasticsearch=false
ENV TaskScheduler__UseTaskScheduler=false
ENV FileStorage__Provider=Local

# Default Railway database configuration (will be overridden by Railway variables)
ENV ConnectionStrings__DArchPgContext="Host=localhost;Port=5432;Database=ziraai;Username=postgres;Password=password"

# Application health test with forced console logging
ENTRYPOINT ["sh", "-c", "echo 'Starting .NET application on port 8080...' && echo 'Environment check:' && env | grep -E '(ASPNETCORE|DATABASE|ConnectionStrings|Use|Logging)' && echo 'Testing database connectivity...' && timeout 10 nc -z caboose.proxy.rlwy.net 23899 && echo 'Database connection OK' || echo 'Database connection FAILED' && echo 'Starting dotnet with console logging...' && ASPNETCORE_LOGGING__CONSOLE__LOGLEVEL__DEFAULT=Information dotnet WebAPI.dll 2>&1 | tee /tmp/app.log & APP_PID=$! && echo 'App started with PID:' $APP_PID && sleep 10 && echo 'Testing app health after 10s...' && curl -f http://localhost:8080/health 2>/dev/null && echo 'Health check OK' || echo 'Health check FAILED' && sleep 10 && echo 'Testing Swagger endpoint...' && curl -f http://localhost:8080/swagger 2>/dev/null && echo 'Swagger OK' || echo 'Swagger FAILED' && sleep 20 && if kill -0 $APP_PID 2>/dev/null; then echo 'App running successfully for 40s, stopping for log analysis'; kill $APP_PID; else echo 'App terminated unexpectedly'; fi && echo '=== FULL APPLICATION LOGS ===' && cat /tmp/app.log && echo '=== END OF LOGS ===' && echo 'Container staying alive for debugging...' && sleep 300"]