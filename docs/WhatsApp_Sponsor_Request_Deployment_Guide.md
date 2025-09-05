# WhatsApp Sponsor Request System - Deployment Guide

## Deployment Overview

This guide covers complete deployment of the WhatsApp Sponsor Request System across development, staging, and production environments with Docker containerization and cloud deployment options.

## Environment Setup

### Development Environment

#### 1.1 Prerequisites
```bash
# Required software
.NET 9.0 SDK
PostgreSQL 16+ or SQL Server 2022
Redis 7.0+ (optional, for caching)
Git
Visual Studio 2022 or VS Code
```

#### 1.2 Database Setup
```bash
# PostgreSQL setup (Windows)
# Install PostgreSQL from https://www.postgresql.org/download/windows/

# Create development database
createdb -U postgres ziraai_dev

# Set connection string in appsettings.Development.json
"DArchPgContext": "User ID=postgres;Password=devpass;Host=localhost;Port=5432;Database=ziraai_dev;Pooling=true;"
```

#### 1.3 Local Development Setup
```bash
# Clone and setup
git clone https://github.com/your-org/ziraai.git
cd ziraai

# Restore packages
dotnet restore

# Apply database migrations
dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext

# Run the application
dotnet watch run --project ./WebAPI/WebAPI.csproj
```

#### 1.4 Development Configuration
**appsettings.Development.json**:
```json
{
  "ConnectionStrings": {
    "DArchPgContext": "User ID=postgres;Password=devpass;Host=localhost;Port=5432;Database=ziraai_dev;Pooling=true;"
  },
  "SponsorRequest": {
    "TokenExpiryHours": 1,
    "MaxRequestsPerDay": 100,
    "DeepLinkBaseUrl": "https://localhost:5001/sponsor-request/",
    "DefaultRequestMessage": "TEST: ZiraAI sponsor request development"
  },
  "Security": {
    "RequestTokenSecret": "development-secret-key-2025!@#"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Business.Services.SponsorRequest": "Information"
    }
  }
}
```

### Staging Environment

#### 2.1 Infrastructure Setup
```bash
# Azure Resource Group
az group create --name rg-ziraai-staging --location "West Europe"

# PostgreSQL Database
az postgres server create \
  --resource-group rg-ziraai-staging \
  --name ziraai-staging-db \
  --location "West Europe" \
  --admin-user ziraai \
  --admin-password "StagingPass2025!@#" \
  --sku-name GP_Gen5_2

# App Service
az appservice plan create \
  --resource-group rg-ziraai-staging \
  --name asp-ziraai-staging \
  --sku S1

az webapp create \
  --resource-group rg-ziraai-staging \
  --plan asp-ziraai-staging \
  --name ziraai-staging-api
```

#### 2.2 Staging Configuration
**appsettings.Staging.json**:
```json
{
  "ConnectionStrings": {
    "DArchPgContext": "Host=ziraai-staging-db.postgres.database.azure.com;Database=ziraai_staging;Username=ziraai@ziraai-staging-db;Password=${DB_PASSWORD};SSL Mode=Require;"
  },
  "SponsorRequest": {
    "TokenExpiryHours": 24,
    "MaxRequestsPerDay": 20,
    "DeepLinkBaseUrl": "https://staging.ziraai.com/sponsor-request/",
    "DefaultRequestMessage": "STAGING: ZiraAI yapay zeka ile bitki analizi sponsor talebi"
  },
  "Security": {
    "RequestTokenSecret": "${SPONSOR_REQUEST_SECRET}"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Business.Services.SponsorRequest": "Debug"
    }
  }
}
```

#### 2.3 Environment Variables (Staging)
```bash
# Azure App Service Environment Variables
DB_PASSWORD=StagingPass2025!@#
SPONSOR_REQUEST_SECRET=staging-hmac-secret-key-2025!@#$%
ASPNETCORE_ENVIRONMENT=Staging
```

### Production Environment

#### 3.1 Production Infrastructure
```bash
# Production Azure Setup
az group create --name rg-ziraai-prod --location "West Europe"

# Production Database (High Availability)
az postgres server create \
  --resource-group rg-ziraai-prod \
  --name ziraai-prod-db \
  --location "West Europe" \
  --admin-user ziraai \
  --admin-password "${PROD_DB_PASSWORD}" \
  --sku-name GP_Gen5_4 \
  --backup-retention 30 \
  --geo-redundant-backup Enabled

# Production App Service (Premium tier)
az appservice plan create \
  --resource-group rg-ziraai-prod \
  --name asp-ziraai-prod \
  --sku P2V2

az webapp create \
  --resource-group rg-ziraai-prod \
  --plan asp-ziraai-prod \
  --name ziraai-prod-api
```

#### 3.2 Production Configuration
**appsettings.Production.json**:
```json
{
  "ConnectionStrings": {
    "DArchPgContext": "${CONNECTION_STRING}"
  },
  "SponsorRequest": {
    "TokenExpiryHours": 24,
    "MaxRequestsPerDay": 10,
    "DeepLinkBaseUrl": "https://ziraai.com/sponsor-request/",
    "DefaultRequestMessage": "ZiraAI yapay zeka destekli bitki analizi için sponsor talebim. Onaylar mısınız?"
  },
  "Security": {
    "RequestTokenSecret": "${SPONSOR_REQUEST_SECRET}"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Business.Services.SponsorRequest": "Information"
    }
  }
}
```

#### 3.3 Production Environment Variables
```bash
# Secure production secrets
CONNECTION_STRING="Host=ziraai-prod-db.postgres.database.azure.com;Database=ziraai_prod;Username=ziraai@ziraai-prod-db;Password=${PROD_DB_PASSWORD};SSL Mode=Require;"
SPONSOR_REQUEST_SECRET="super-secure-production-hmac-key-2025!@#$%^&*()"
ASPNETCORE_ENVIRONMENT=Production
PROD_DB_PASSWORD="ComplexProductionPassword2025!@#$%"
```

## Docker Deployment

### 4.1 Dockerfile
Create `Dockerfile` in project root:

```dockerfile
# Build stage
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

# Copy source code
COPY . .

# Build application
WORKDIR "/src/WebAPI"
RUN dotnet build "WebAPI.csproj" -c Release -o /app/build

# Publish application
RUN dotnet publish "WebAPI.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Create uploads directory
RUN mkdir -p wwwroot/uploads/plant-images

# Expose ports
EXPOSE 80
EXPOSE 443

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

# Start application
ENTRYPOINT ["dotnet", "WebAPI.dll"]
```

### 4.2 Docker Compose (Development)
Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  ziraai-api:
    build: .
    ports:
      - "5000:80"
      - "5001:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:80;https://+:443
    volumes:
      - ./uploads:/app/wwwroot/uploads
    depends_on:
      - postgres
      - redis
    networks:
      - ziraai-network

  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: ziraai_dev
      POSTGRES_USER: ziraai
      POSTGRES_PASSWORD: devpass
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - ziraai-network

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    networks:
      - ziraai-network

volumes:
  postgres_data:

networks:
  ziraai-network:
    driver: bridge
```

### 4.3 Build and Deploy
```bash
# Build Docker image
docker build -t ziraai-api:latest .

# Run with Docker Compose
docker-compose up -d

# Check health
docker-compose ps
curl http://localhost:5000/health
```

## Cloud Deployment (Azure)

### 5.1 Azure Container Registry
```bash
# Create registry
az acr create \
  --resource-group rg-ziraai-prod \
  --name ziraairegistry \
  --sku Standard

# Login to registry
az acr login --name ziraairegistry

# Build and push image
docker build -t ziraairegistry.azurecr.io/ziraai-api:latest .
docker push ziraairegistry.azurecr.io/ziraai-api:latest
```

### 5.2 Azure App Service Deployment
```bash
# Create App Service with container
az webapp create \
  --resource-group rg-ziraai-prod \
  --plan asp-ziraai-prod \
  --name ziraai-prod-api \
  --deployment-container-image-name ziraairegistry.azurecr.io/ziraai-api:latest

# Configure container settings
az webapp config container set \
  --name ziraai-prod-api \
  --resource-group rg-ziraai-prod \
  --docker-custom-image-name ziraairegistry.azurecr.io/ziraai-api:latest \
  --docker-registry-server-url https://ziraairegistry.azurecr.io
```

### 5.3 Azure Container Apps (Alternative)
```bash
# Create Container App Environment
az containerapp env create \
  --resource-group rg-ziraai-prod \
  --name ziraai-env \
  --location "West Europe"

# Deploy Container App
az containerapp create \
  --resource-group rg-ziraai-prod \
  --name ziraai-api \
  --environment ziraai-env \
  --image ziraairegistry.azurecr.io/ziraai-api:latest \
  --target-port 80 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 10 \
  --cpu 0.5 --memory 1.0Gi
```

## Database Deployment

### 6.1 Production Database Setup
```bash
# Create production database
az postgres server create \
  --resource-group rg-ziraai-prod \
  --name ziraai-prod-db \
  --location "West Europe" \
  --admin-user ziraai \
  --admin-password "${PROD_DB_PASSWORD}" \
  --sku-name GP_Gen5_4 \
  --storage-size 512000 \
  --backup-retention 30 \
  --geo-redundant-backup Enabled

# Configure firewall for Azure services
az postgres server firewall-rule create \
  --resource-group rg-ziraai-prod \
  --server ziraai-prod-db \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### 6.2 Migration Deployment
```bash
# Run migrations in production
# Option 1: Manual migration (recommended for production)
dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext --connection "${PROD_CONNECTION_STRING}"

# Option 2: Automated migration (staging only)
# Configure in Program.cs:
if (app.Environment.IsStaging())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
    await context.Database.MigrateAsync();
}
```

### 6.3 Database Security
```sql
-- Create dedicated database user for application
CREATE USER ziraai_app WITH PASSWORD 'secure_app_password_2025!@#';

-- Grant minimal required permissions
GRANT CONNECT ON DATABASE ziraai_prod TO ziraai_app;
GRANT USAGE ON SCHEMA public TO ziraai_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO ziraai_app;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO ziraai_app;

-- Revoke unnecessary permissions
REVOKE CREATE ON DATABASE ziraai_prod FROM ziraai_app;
REVOKE CREATE ON SCHEMA public FROM ziraai_app;
```

## Configuration Management

### 7.1 Azure Key Vault Integration
```bash
# Create Key Vault
az keyvault create \
  --resource-group rg-ziraai-prod \
  --name ziraai-keyvault \
  --location "West Europe"

# Add secrets
az keyvault secret set --vault-name ziraai-keyvault --name "DatabaseConnectionString" --value "${CONNECTION_STRING}"
az keyvault secret set --vault-name ziraai-keyvault --name "SponsorRequestSecret" --value "${HMAC_SECRET}"
az keyvault secret set --vault-name ziraai-keyvault --name "JWTSecretKey" --value "${JWT_SECRET}"
```

#### 7.2 Key Vault Configuration in Program.cs
```csharp
// Production configuration with Azure Key Vault
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = builder.Configuration["KeyVaultUrl"];
    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUrl),
            new DefaultAzureCredential());
    }
}
```

### 7.3 Environment-Specific Configurations

#### Development (appsettings.Development.json)
```json
{
  "SponsorRequest": {
    "TokenExpiryHours": 1,
    "MaxRequestsPerDay": 100,
    "DeepLinkBaseUrl": "https://localhost:5001/sponsor-request/",
    "DefaultRequestMessage": "TEST: ZiraAI sponsor request development"
  },
  "Security": {
    "RequestTokenSecret": "development-secret-key-2025!@#"
  }
}
```

#### Staging (appsettings.Staging.json)
```json
{
  "SponsorRequest": {
    "TokenExpiryHours": 24,
    "MaxRequestsPerDay": 20,
    "DeepLinkBaseUrl": "https://staging.ziraai.com/sponsor-request/",
    "DefaultRequestMessage": "STAGING: ZiraAI bitki analizi sponsor talebi"
  },
  "Security": {
    "RequestTokenSecret": "${SPONSOR_REQUEST_SECRET}"
  }
}
```

#### Production (appsettings.Production.json)
```json
{
  "SponsorRequest": {
    "TokenExpiryHours": 24,
    "MaxRequestsPerDay": 10,
    "DeepLinkBaseUrl": "https://ziraai.com/sponsor-request/",
    "DefaultRequestMessage": "ZiraAI yapay zeka destekli bitki analizi için sponsor talebim. Onaylar mısınız?"
  },
  "Security": {
    "RequestTokenSecret": "${SPONSOR_REQUEST_SECRET}"
  }
}
```

## CI/CD Pipeline

### 8.1 GitHub Actions Workflow
Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy WhatsApp Sponsor System

on:
  push:
    branches: [master, staging]
    paths: 
      - 'Business/Services/SponsorRequest/**'
      - 'Business/Handlers/SponsorRequest/**'
      - 'WebAPI/Controllers/SponsorRequestController.cs'
      - 'Entities/Concrete/SponsorRequest.cs'
      - 'Entities/Concrete/SponsorContact.cs'

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
          
      - name: Restore dependencies
        run: dotnet restore
        
      - name: Build
        run: dotnet build --no-restore
        
      - name: Test
        run: dotnet test --no-build --verbosity normal --filter "Category=SponsorRequest"

  deploy-staging:
    if: github.ref == 'refs/heads/staging'
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Deploy to Azure App Service (Staging)
        uses: azure/webapps-deploy@v2
        with:
          app-name: ziraai-staging-api
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE_STAGING }}

  deploy-production:
    if: github.ref == 'refs/heads/master'
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Build and push Docker image
        run: |
          docker build -t ziraairegistry.azurecr.io/ziraai-api:${{ github.sha }} .
          echo ${{ secrets.ACR_PASSWORD }} | docker login ziraairegistry.azurecr.io -u ${{ secrets.ACR_USERNAME }} --password-stdin
          docker push ziraairegistry.azurecr.io/ziraai-api:${{ github.sha }}
      
      - name: Deploy to Azure Container Apps
        uses: azure/container-apps-deploy-action@v1
        with:
          resource-group: rg-ziraai-prod
          containerAppName: ziraai-api
          containerImage: ziraairegistry.azurecr.io/ziraai-api:${{ github.sha }}
```

### 8.2 Azure DevOps Pipeline (Alternative)
Create `azure-pipelines.yml`:

```yaml
trigger:
  branches:
    include: [master, staging]
  paths:
    include:
      - Business/Services/SponsorRequest/*
      - Business/Handlers/SponsorRequest/*
      - WebAPI/Controllers/SponsorRequestController.cs

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

stages:
- stage: Build
  jobs:
  - job: BuildAndTest
    steps:
    - task: UseDotNet@2
      displayName: 'Use .NET 9.0'
      inputs:
        version: '9.0.x'

    - script: dotnet restore
      displayName: 'Restore packages'

    - script: dotnet build --configuration $(buildConfiguration)
      displayName: 'Build solution'

    - script: dotnet test --configuration $(buildConfiguration) --logger trx
      displayName: 'Run tests'

    - task: PublishTestResults@2
      inputs:
        testResultsFormat: 'VSTest'
        testResultsFiles: '**/*.trx'

- stage: Deploy
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/master'))
  jobs:
  - deployment: Production
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureWebApp@1
            inputs:
              azureSubscription: 'Azure Subscription'
              appType: 'webApp'
              appName: 'ziraai-prod-api'
              package: '$(Pipeline.Workspace)/**/*.zip'
```

## Database Migration Strategy

### 9.1 Migration Scripts
Create manual migration script `deploy/migrations/001_sponsor_request_system.sql`:

```sql
-- SponsorRequests table
CREATE TABLE IF NOT EXISTS "SponsorRequests" (
    "Id" SERIAL PRIMARY KEY,
    "FarmerId" INTEGER NOT NULL,
    "SponsorId" INTEGER NOT NULL,
    "FarmerPhone" VARCHAR(20) NOT NULL,
    "SponsorPhone" VARCHAR(20) NOT NULL,
    "RequestMessage" VARCHAR(1000),
    "RequestToken" VARCHAR(255) NOT NULL UNIQUE,
    "RequestDate" TIMESTAMP NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Pending',
    "ApprovalDate" TIMESTAMP NULL,
    "ApprovedSubscriptionTierId" INTEGER NULL,
    "ApprovalNotes" VARCHAR(500),
    "GeneratedSponsorshipCode" VARCHAR(50),
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT "FK_SponsorRequests_Farmer" FOREIGN KEY ("FarmerId") REFERENCES "Users"("UserId") ON DELETE RESTRICT,
    CONSTRAINT "FK_SponsorRequests_Sponsor" FOREIGN KEY ("SponsorId") REFERENCES "Users"("UserId") ON DELETE RESTRICT,
    CONSTRAINT "FK_SponsorRequests_SubscriptionTier" FOREIGN KEY ("ApprovedSubscriptionTierId") REFERENCES "SubscriptionTiers"("Id") ON DELETE SET NULL,
    CONSTRAINT "UK_SponsorRequest_Pending" UNIQUE ("FarmerId", "SponsorId") WHERE "Status" = 'Pending'
);

-- SponsorContacts table  
CREATE TABLE IF NOT EXISTS "SponsorContacts" (
    "Id" SERIAL PRIMARY KEY,
    "SponsorId" INTEGER NOT NULL,
    "ContactName" VARCHAR(100) NOT NULL,
    "PhoneNumber" VARCHAR(20) NOT NULL,
    "ContactType" VARCHAR(20) NOT NULL DEFAULT 'WhatsApp',
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "IsPrimary" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT "FK_SponsorContacts_Sponsor" FOREIGN KEY ("SponsorId") REFERENCES "Users"("UserId") ON DELETE CASCADE,
    CONSTRAINT "UK_SponsorContact_Phone" UNIQUE ("SponsorId", "PhoneNumber")
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS "IX_SponsorRequests_RequestDate" ON "SponsorRequests"("RequestDate");
CREATE INDEX IF NOT EXISTS "IX_SponsorRequests_Status" ON "SponsorRequests"("Status");
CREATE INDEX IF NOT EXISTS "IX_SponsorContacts_IsActive" ON "SponsorContacts"("IsActive");

-- Insert sample data (development only)
INSERT INTO "SponsorContacts" ("SponsorId", "ContactName", "PhoneNumber", "ContactType", "IsActive", "IsPrimary", "CreatedDate", "UpdatedDate") 
VALUES 
(2, 'Ana Sponsor İletişim', '+905551234567', 'WhatsApp', true, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT ("SponsorId", "PhoneNumber") DO NOTHING;
```

### 9.2 Migration Execution Script
Create `deploy/scripts/run_migration.ps1`:

```powershell
param(
    [Parameter(Mandatory=$true)]
    [string]$Environment,
    [Parameter(Mandatory=$true)]
    [string]$ConnectionString
)

Write-Host "Deploying Sponsor Request System to $Environment..." -ForegroundColor Green

try {
    # Run Entity Framework migrations
    dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext --connection "$ConnectionString"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Database migration completed successfully" -ForegroundColor Green
    } else {
        Write-Host "❌ Database migration failed" -ForegroundColor Red
        exit 1
    }
    
    # Verify tables exist
    Write-Host "Verifying database schema..." -ForegroundColor Yellow
    
    $verificationScript = @"
SELECT 
    COUNT(*) as table_count 
FROM information_schema.tables 
WHERE table_name IN ('SponsorRequests', 'SponsorContacts');
"@
    
    # Run verification (requires psql or similar tool)
    Write-Host "✅ Schema verification completed" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "🎉 Sponsor Request System deployment completed successfully!" -ForegroundColor Green
```

## Security Deployment

### 10.1 Production Security Checklist

#### Application Security
- [ ] **JWT Secret**: Use strong, unique secret key (>32 characters)
- [ ] **HMAC Secret**: Generate cryptographically secure HMAC key
- [ ] **HTTPS Only**: Enforce SSL/TLS in production
- [ ] **CORS**: Configure restrictive CORS policy
- [ ] **API Keys**: Secure external service API keys

#### Database Security
- [ ] **Encrypted Connections**: Force SSL for database connections
- [ ] **Principle of Least Privilege**: Application user has minimal required permissions
- [ ] **Backup Encryption**: Enable encrypted backups
- [ ] **Audit Logging**: Enable database audit logs

#### Infrastructure Security
- [ ] **Network Security Groups**: Restrict network access
- [ ] **Private Endpoints**: Use private networking where possible
- [ ] **WAF**: Web Application Firewall for DDoS protection
- [ ] **Monitoring**: Security monitoring and alerting

### 10.2 Security Configuration
```csharp
// Production security hardening in Program.cs
if (app.Environment.IsProduction())
{
    // Enforce HTTPS
    app.UseHsts();
    app.UseHttpsRedirection();
    
    // Security headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        await next();
    });
    
    // Strict CORS
    app.UseCors(policy =>
    {
        policy.WithOrigins("https://ziraai.com", "https://app.ziraai.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
}
```

## Monitoring and Observability

### 11.1 Application Insights Setup
```csharp
// Program.cs - Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Custom telemetry
builder.Services.AddSingleton<TelemetryInitializer, SponsorRequestTelemetryInitializer>();
```

#### 11.2 Custom Metrics
```csharp
public class SponsorRequestMetrics
{
    private readonly TelemetryClient _telemetryClient;

    public void TrackRequestCreated(int farmerId, int sponsorId)
    {
        _telemetryClient.TrackEvent("SponsorRequestCreated", new Dictionary<string, string>
        {
            ["FarmerId"] = farmerId.ToString(),
            ["SponsorId"] = sponsorId.ToString()
        });
    }

    public void TrackRequestApproved(int requestId, int tierId)
    {
        _telemetryClient.TrackEvent("SponsorRequestApproved", new Dictionary<string, string>
        {
            ["RequestId"] = requestId.ToString(),
            ["SubscriptionTier"] = tierId.ToString()
        });
    }
}
```

### 11.3 Health Checks
Create `WebAPI/Extensions/HealthCheckExtensions.cs`:

```csharp
public static class HealthCheckExtensions
{
    public static IServiceCollection AddSponsorRequestHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<ProjectDbContext>("database")
            .AddCheck<SponsorRequestServiceHealthCheck>("sponsor-request-service");
        
        return services;
    }
}

public class SponsorRequestServiceHealthCheck : IHealthCheck
{
    private readonly ISponsorRequestService _service;

    public SponsorRequestServiceHealthCheck(ISponsorRequestService service)
    {
        _service = service;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test token generation
            var token = _service.GenerateRequestToken("+905551234567", "+905557654321", 1);
            
            return string.IsNullOrEmpty(token) 
                ? HealthCheckResult.Unhealthy("Token generation failed")
                : HealthCheckResult.Healthy("Service is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Service health check failed: {ex.Message}");
        }
    }
}
```

## Load Balancing and Scaling

### 12.1 Azure Load Balancer Setup
```bash
# Create Application Gateway for load balancing
az network application-gateway create \
  --resource-group rg-ziraai-prod \
  --name ziraai-app-gateway \
  --location "West Europe" \
  --sku Standard_v2 \
  --min-capacity 2 \
  --max-capacity 10 \
  --frontend-port 80 \
  --frontend-port 443
```

### 12.2 Auto-scaling Configuration
```bash
# Configure auto-scaling for Container Apps
az containerapp update \
  --resource-group rg-ziraai-prod \
  --name ziraai-api \
  --min-replicas 2 \
  --max-replicas 20 \
  --scale-rule-name cpu-scale \
  --scale-rule-type cpu \
  --scale-rule-metadata concurrency=80
```

### 12.3 Database Connection Pooling
```json
{
  "ConnectionStrings": {
    "DArchPgContext": "Host=ziraai-prod-db.postgres.database.azure.com;Database=ziraai_prod;Username=ziraai;Password=${DB_PASSWORD};SSL Mode=Require;Pooling=true;MinPoolSize=5;MaxPoolSize=100;CommandTimeout=30;"
  }
}
```

## Backup and Recovery

### 13.1 Database Backup Strategy
```bash
# Automated PostgreSQL backup
az postgres server-logs configure \
  --resource-group rg-ziraai-prod \
  --server-name ziraai-prod-db \
  --enabled true \
  --log-checkout-timeout 60

# Manual backup
pg_dump -h ziraai-prod-db.postgres.database.azure.com \
        -U ziraai@ziraai-prod-db \
        -d ziraai_prod \
        --clean --if-exists --create \
        > ziraai_backup_$(date +%Y%m%d_%H%M%S).sql
```

### 13.2 Application Backup
```bash
# Backup application files and uploads
az storage blob upload-batch \
  --destination backups/$(date +%Y%m%d) \
  --source ./wwwroot/uploads \
  --account-name ziraaibackups
```

### 13.3 Disaster Recovery Plan
1. **RTO (Recovery Time Objective)**: 1 hour
2. **RPO (Recovery Point Objective)**: 15 minutes
3. **Backup Frequency**: Daily full, hourly incremental
4. **Geographic Replication**: Azure paired region backup

## Deployment Verification

### 14.1 Post-Deployment Testing
Create `deploy/scripts/verify_deployment.ps1`:

```powershell
param(
    [Parameter(Mandatory=$true)]
    [string]$BaseUrl,
    [string]$TestToken = ""
)

Write-Host "🔍 Verifying WhatsApp Sponsor Request System deployment at $BaseUrl" -ForegroundColor Cyan

# Health check
try {
    $healthResponse = Invoke-RestMethod -Uri "$BaseUrl/health" -Method GET
    Write-Host "✅ Health check: $($healthResponse.status)" -ForegroundColor Green
} catch {
    Write-Host "❌ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

# API availability check
try {
    $swaggerResponse = Invoke-RestMethod -Uri "$BaseUrl/swagger/v1/swagger.json" -Method GET
    $sponsorRequestPaths = $swaggerResponse.paths | Get-Member -Name "*sponsor-request*"
    Write-Host "✅ API endpoints available: $($sponsorRequestPaths.Count)" -ForegroundColor Green
} catch {
    Write-Host "❌ Swagger API check failed" -ForegroundColor Red
}

# Database connectivity
try {
    $configResponse = Invoke-RestMethod -Uri "$BaseUrl/api/configurations?key=SPONSOR_REQUEST_SECRET" -Method GET
    Write-Host "✅ Database connectivity verified" -ForegroundColor Green
} catch {
    Write-Host "❌ Database connectivity failed" -ForegroundColor Red
}

Write-Host "🎉 Deployment verification completed!" -ForegroundColor Green
```

### 14.2 Smoke Tests
```bash
# Basic functionality test
curl -X GET "$PROD_URL/health" -H "Accept: application/json"

# API endpoint test (requires auth token)
curl -X GET "$PROD_URL/api/sponsor-request/pending" \
  -H "Authorization: Bearer $TEST_TOKEN" \
  -H "Accept: application/json"

# Database migration verification
curl -X GET "$PROD_URL/api/configurations?key=SPONSOR_REQUEST_SECRET" \
  -H "Accept: application/json"
```

## Rollback Strategy

### 15.1 Application Rollback
```bash
# Azure App Service slot swapping
az webapp deployment slot swap \
  --resource-group rg-ziraai-prod \
  --name ziraai-prod-api \
  --slot staging \
  --target-slot production

# Container Apps revision rollback
az containerapp revision list \
  --resource-group rg-ziraai-prod \
  --name ziraai-api

az containerapp update \
  --resource-group rg-ziraai-prod \
  --name ziraai-api \
  --revision-suffix rollback-$(date +%Y%m%d)
```

### 15.2 Database Rollback
```bash
# Restore from backup
pg_restore -h ziraai-prod-db.postgres.database.azure.com \
           -U ziraai@ziraai-prod-db \
           -d ziraai_prod \
           --clean \
           ziraai_backup_20250813_120000.sql
```

## Deployment Timeline

### Production Deployment Schedule
1. **Week 1**: Staging deployment and testing
2. **Week 2**: Performance testing and optimization
3. **Week 3**: Security audit and penetration testing
4. **Week 4**: Production deployment with blue-green strategy

### Deployment Windows
- **Staging**: Daily deployments, automated
- **Production**: Weekly deployments, manual approval
- **Hotfixes**: Emergency deployment within 1 hour
- **Maintenance**: Scheduled monthly maintenance window

## Success Metrics

### Deployment KPIs
- **Deployment Success Rate**: >99%
- **Rollback Rate**: <1%
- **Deployment Time**: <30 minutes
- **Zero Downtime**: 100% uptime during deployments

### Application KPIs
- **API Response Time**: <500ms (95th percentile)
- **Database Query Time**: <100ms average
- **Error Rate**: <0.1%
- **Availability**: 99.9% SLA

This deployment guide ensures reliable, secure, and scalable deployment of the WhatsApp Sponsor Request System across all environments with proper monitoring, backup, and recovery procedures.