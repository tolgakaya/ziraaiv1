# Railway .NET 9.0 Minimal Working Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install EF Core tools for migrations
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

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

# Install EF Core tools for migrations in final stage
RUN apt-get update && apt-get install -y curl && \
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0 --install-dir /usr/share/dotnet && \
    ln -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet && \
    dotnet tool install --global dotnet-ef --version 9.0.0
ENV PATH="$PATH:/root/.dotnet/tools"

# Create uploads directory
RUN mkdir -p /app/wwwroot/uploads/plant-images && \
    chmod 755 /app/wwwroot/uploads/plant-images

# Install ICU libraries for Turkish culture support and debugging tools
RUN apt-get update && apt-get install -y libicu-dev netcat-traditional curl && rm -rf /var/lib/apt/lists/*

# Railway environment configuration - use Development for Swagger and logging
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Development
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

# Railway startup with automatic database migration
ENTRYPOINT ["sh", "-c", "echo 'Starting ZiraAI deployment on Railway...' && echo 'Database connectivity test...' && timeout 10 nc -z caboose.proxy.rlwy.net 23899 && echo 'Database connection OK' || echo 'Database connection FAILED' && echo 'Running database migrations...' && dotnet ef database update --connection \"$ConnectionStrings__DArchPgContext\" --verbose && echo 'Migrations completed successfully!' && echo 'Starting ZiraAI API server...' && dotnet WebAPI.dll"]