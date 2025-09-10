# 🚀 ZiraAI CI/CD & Deployment Guide

**Version:** 2.0  
**Last Updated:** September 9, 2025  
**Maintainer:** Development Team  

> ⚠️ **CRITICAL:** This document MUST be updated whenever deployment configurations change!

## 📋 Table of Contents

- [Overview](#overview)
- [Repository Structure](#repository-structure)
- [Railway Multi-Service Deployment](#railway-multi-service-deployment)
- [Environment Configurations](#environment-configurations)
- [Deployment Workflows](#deployment-workflows)
- [Troubleshooting](#troubleshooting)
- [Change Log](#change-log)

## 🏗️ Overview

ZiraAI uses a **multi-service monorepo architecture** with two main services:
- **WebAPI**: REST API service for client interactions
- **PlantAnalysisWorkerService**: Background worker for async plant analysis

Both services are deployed independently on **Railway** platform using Docker containerization.

## 📁 Repository Structure

```
ziraai/
├── WebAPI/                          # REST API Service
│   ├── Dockerfile                   # WebAPI container definition
│   ├── railway.json                 # WebAPI Railway configuration
│   └── Controllers/
├── PlantAnalysisWorkerService/      # Background Worker Service
│   ├── Dockerfile                   # Worker container definition
│   ├── railway.json                 # Worker Railway configuration
│   └── Services/
├── Business/                        # Shared business logic
├── Core/                           # Cross-cutting concerns
├── DataAccess/                     # Entity Framework & repositories
├── Entities/                       # Domain models & DTOs
└── docs/                           # Documentation
```

## 🚂 Railway Multi-Service Deployment

### Service Configuration Matrix

| Service | Railway Name | Root Directory | Dockerfile | Config File |
|---------|--------------|----------------|------------|-------------|
| **WebAPI** | `ziraai-api` | `/WebAPI` | `WebAPI/WebAPI.Dockerfile` | `WebAPI/railway.json` |
| **Worker** | `ziraai-worker` | `/PlantAnalysisWorkerService` | `PlantAnalysisWorkerService/PlantAnalysisWorkerService.Dockerfile` | `PlantAnalysisWorkerService/railway.json` |

### Railway.json Configurations

#### WebAPI Railway Configuration
```json
{
  "$schema": "https://railway.com/railway.schema.json",
  "build": {
    "builder": "DOCKERFILE",
    "dockerfilePath": "WebAPI.Dockerfile",
    "buildCommand": "echo 'Building ZiraAI WebAPI - REST API Service'"
  },
  "deploy": {
    "startCommand": "dotnet WebAPI.dll",
    "healthcheckPath": "/health",
    "healthcheckTimeout": 300,
    "restartPolicyType": "ON_FAILURE",
    "restartPolicyMaxRetries": 10
  },
  "environments": {
    "staging": {
      "variables": {
        "ASPNETCORE_ENVIRONMENT": "Staging"
      }
    },
    "production": {
      "variables": {
        "ASPNETCORE_ENVIRONMENT": "Production"
      }
    }
  },
  "watchPaths": [
    "WebAPI/**",
    "Business/**",
    "Core/**", 
    "DataAccess/**",
    "Entities/**"
  ]
}
```

#### Worker Service Railway Configuration
```json
{
  "$schema": "https://railway.com/railway.schema.json",
  "build": {
    "builder": "DOCKERFILE",
    "dockerfilePath": "PlantAnalysisWorkerService.Dockerfile",
    "buildCommand": "echo 'Building PlantAnalysisWorkerService - Worker Service'"
  },
  "deploy": {
    "startCommand": "dotnet PlantAnalysisWorkerService.dll",
    "healthcheckPath": "/health",
    "healthcheckTimeout": 300,
    "restartPolicyType": "ON_FAILURE",
    "restartPolicyMaxRetries": 10
  },
  "environments": {
    "staging": {
      "variables": {
        "ASPNETCORE_ENVIRONMENT": "Staging"
      }
    },
    "production": {
      "variables": {
        "ASPNETCORE_ENVIRONMENT": "Production"
      }
    }
  },
  "watchPaths": [
    "PlantAnalysisWorkerService/**",
    "Business/**",
    "Core/**",
    "DataAccess/**",
    "Entities/**"
  ]
}
```

## 🌍 Environment Configurations

### Staging Environment

#### WebAPI Staging Variables
```env
# Core Configuration
ASPNETCORE_ENVIRONMENT="Staging"
ASPNETCORE_URLS="http://0.0.0.0:8080"

# Database
ConnectionStrings__DArchPgContext="Host=tramway.proxy.rlwy.net;Port=39540;Database=railway;Username=postgres;Password=cEAvVsWsZIHDUaUKUMiSTRaTGmuswdEd"

# Redis Cache
CacheOptions__Host="maglev.proxy.rlwy.net"
CacheOptions__Port="38265"
CacheOptions__Password="pFCgxGquNowJtjLBguvHLXMhyRghrcxv"

# RabbitMQ
RabbitMQ__ConnectionString="amqp://0jVVvzhvUENz002P:G4Qx1f-w.Jf.pMTP3lnPB2XiNe~CX3kr@interchange.proxy.rlwy.net:44738/"

# JWT Tokens
TokenOptions__SecurityKey="ZiraAI-Staging-JWT-SecretKey-2025!@"
TokenOptions__Issuer="ZiraAI_Staging"

# Service Features
UseHangfire="false"
UseRabbitMQ="true" 
UseRedis="true"
UseElasticsearch="false"
```

#### PlantAnalysisWorkerService Staging Variables
```env
# Core Configuration
ASPNETCORE_ENVIRONMENT="Staging" 
ASPNETCORE_URLS="http://0.0.0.0:8080"

# Database (Same as WebAPI)
ConnectionStrings__DArchPgContext="Host=tramway.proxy.rlwy.net;Port=39540;Database=railway;Username=postgres;Password=cEAvVsWsZIHDUaUKUMiSTRaTGmuswdEd"

# Redis Cache (Same as WebAPI)
CacheOptions__Host="maglev.proxy.rlwy.net"
CacheOptions__Port="38265"
CacheOptions__Password="pFCgxGquNowJtjLBguvHLXMhyRghrcxv"

# RabbitMQ (Same as WebAPI)
RabbitMQ__ConnectionString="amqp://0jVVvzhvUENz002P:G4Qx1f-w.Jf.pMTP3lnPB2XiNe~CX3kr@interchange.proxy.rlwy.net:44738/"

# Service Features (Worker Specific)
UseHangfire="true"          # DIFFERENT: Worker needs Hangfire
UseRabbitMQ="true"
UseRedis="true"
UseTaskScheduler="true"     # DIFFERENT: Worker needs task scheduling

# Worker Performance
WORKER_CONCURRENCY="8"
WORKER_PREFETCH_COUNT="15"
WORKER_HEARTBEAT_INTERVAL="30"

# Logging (Worker Specific)
SeriLogConfigurations__FileLogConfiguration__FolderPath="/app/logs/worker-staging/"
```

### Production Environment

#### WebAPI Production Variables
```env
# Core Configuration
ASPNETCORE_ENVIRONMENT="Production"
ASPNETCORE_URLS="http://0.0.0.0:8080"

# Database
ConnectionStrings__DArchPgContext="Host=yamabiko.proxy.rlwy.net;Port=41760;Database=railway;Username=postgres;Password=rcrHmHyxJLKYacWzzJoqVRwtJadyEBDQ"

# Redis Cache
CacheOptions__Host="nozomi.proxy.rlwy.net"
CacheOptions__Port="14296"
CacheOptions__Password="yZVPznpdrsfaMyklktEOUaiRaWhxjBJR"

# RabbitMQ
RabbitMQ__ConnectionString="amqp://X0BgI2uKKWMOIkEU:Z990CcFb3Xu9BQ7dUIJw2oxC8D3eSoA8@hopper.proxy.rlwy.net:16220/"

# JWT Tokens
TokenOptions__SecurityKey="ZiraAI-Prod-JWT-SecretKey-2025!@"
TokenOptions__Issuer="ZiraAI_Prod"

# Service Features
UseHangfire="false"
UseRabbitMQ="true"
UseRedis="true"
UseElasticsearch="false"
```

#### PlantAnalysisWorkerService Production Variables
```env
# Core Configuration
ASPNETCORE_ENVIRONMENT="Production"
ASPNETCORE_URLS="http://0.0.0.0:8080"

# Database (Same as WebAPI)
ConnectionStrings__DArchPgContext="Host=yamabiko.proxy.rlwy.net;Port=41760;Database=railway;Username=postgres;Password=rcrHmHyxJLKYacWzzJoqVRwtJadyEBDQ"

# Redis Cache (Same as WebAPI)
CacheOptions__Host="nozomi.proxy.rlwy.net"
CacheOptions__Port="14296"
CacheOptions__Password="yZVPznpdrsfaMyklktEOUaiRaWhxjBJR"

# RabbitMQ (Same as WebAPI)
RabbitMQ__ConnectionString="amqp://X0BgI2uKKWMOIkEU:Z990CcFb3Xu9BQ7dUIJw2oxC8D3eSoA8@hopper.proxy.rlwy.net:16220/"

# Service Features (Worker Specific)
UseHangfire="true"          # DIFFERENT: Worker needs Hangfire
UseRabbitMQ="true"
UseRedis="true"
UseTaskScheduler="true"     # DIFFERENT: Worker needs task scheduling

# Worker Performance
WORKER_CONCURRENCY="10"
WORKER_PREFETCH_COUNT="20" 
WORKER_HEARTBEAT_INTERVAL="30"

# Logging (Worker Specific)
SeriLogConfigurations__FileLogConfiguration__FolderPath="/app/logs/worker/"
```

## 🔄 Deployment Workflows

### Git Branch Strategy
```
master (production)
│
├── staging (pre-production)
│   │
│   └── development (active development)
```

### Deployment Flow

#### 1. Development to Staging
```bash
# Create PR from development to staging
gh pr create --base staging --head development \
  --title "Feature: Description" \
  --body "Detailed changes"

# Auto-deploy to staging environment on merge
```

#### 2. Staging to Production
```bash
# Create production release PR
gh pr create --base master --head staging \
  --title "🚀 Production Release: Version X.X" \
  --body "Production-ready features"

# Manual approval required for production deployment
```

### Railway Deployment Settings

#### WebAPI Service Settings
```bash
# Railway Dashboard → WebAPI Service
Source:
├── Repository: tolgakaya/ziraaiv1
├── Root Directory: /WebAPI
├── Branch: staging/master
└── Railway Config: WebAPI/railway.json

Build:
├── Builder: Dockerfile
├── Dockerfile Path: WebAPI.Dockerfile (relative to /WebAPI)
└── Build Command: echo 'Building ZiraAI WebAPI - REST API Service'

Deploy:
├── Start Command: dotnet WebAPI.dll
├── Health Check: /health
└── Restart Policy: ON_FAILURE
```

#### Worker Service Settings
```bash
# Railway Dashboard → Worker Service
Source:
├── Repository: tolgakaya/ziraaiv1
├── Root Directory: /PlantAnalysisWorkerService
├── Branch: staging/master
└── Railway Config: PlantAnalysisWorkerService/railway.json

Build:
├── Builder: Dockerfile
├── Dockerfile Path: PlantAnalysisWorkerService.Dockerfile (relative to /PlantAnalysisWorkerService)
└── Build Command: echo 'Building PlantAnalysisWorkerService - Worker Service'

Deploy:
├── Start Command: dotnet PlantAnalysisWorkerService.dll
├── Health Check: /health
└── Restart Policy: ON_FAILURE
```

## 🔧 Troubleshooting

### Common Issues

#### 1. Build Context Problems
**Error**: `"/Core/Core.csproj": not found`

**Cause**: Incorrect root directory or dockerfile path configuration

**Solution**:
- Ensure Root Directory is set correctly in Railway
- Verify dockerfile path is relative to root directory
- Check railway.json configuration matches Railway settings

#### 2. Service Deployment Conflicts
**Error**: Wrong service being deployed

**Cause**: Missing or incorrect railway.json configuration

**Solution**:
- Ensure each service has its own railway.json
- Verify dockerfilePath is relative (not absolute)
- Check watchPaths are service-specific

#### 3. Railway Dockerfile Auto-Detection Conflicts
**Error**: Wrong service being built, Railway using incorrect Dockerfile

**Cause**: Railway auto-detection choosing wrong Dockerfile when multiple exist

**Solution**:
- Use unique Dockerfile names: `WebAPI.Dockerfile`, `PlantAnalysisWorkerService.Dockerfile`
- Update railway.json `dockerfilePath` to reference unique names
- Ensure build context is from repository root, not service directory
- Verify Railway Dashboard service configuration

#### 4. Environment Variable Issues
**Error**: Service can't connect to database/Redis/RabbitMQ

**Cause**: Missing or incorrect environment variables

**Solution**:
- Compare with environment variable templates above
- Verify Railway environment variable names match exactly
- Check connection strings are current (Railway rotates credentials)

### Health Check Endpoints

#### WebAPI Health Check
```http
GET https://ziraai-api-staging.up.railway.app/health
```

#### Worker Service Health Check
```http
GET https://ziraai-worker-staging.up.railway.app/health
```

## 📝 Change Log

### Version 2.1 - September 9, 2025
#### 🔧 Railway Dockerfile Detection Fix
- **Resolved Railway Auto-Detection Conflicts**
  - Created unique Dockerfile names: `WebAPI.Dockerfile`, `PlantAnalysisWorkerService.Dockerfile` 
  - Updated railway.json configurations to use service-specific dockerfile paths
  - Fixed build context issues preventing Railway deployment
  - Added troubleshooting section for dockerfile detection conflicts

- **Technical Resolution**
  - Railway was using auto-detection instead of railway.json specifications
  - Unique dockerfile names prevent Railway confusion between services
  - Build context optimized for repository root deployment
  - Both services now deploy independently without conflicts

#### 📚 Documentation Updates
- Updated service configuration matrix with correct dockerfile paths
- Enhanced troubleshooting section with Railway-specific solutions
- Added dockerfile naming convention best practices

### Version 2.0 - September 9, 2025
#### 🚀 Major Changes
- **Added PlantAnalysisWorkerService Docker Support**
  - Multi-environment Dockerfile with Railway integration
  - Environment-specific configuration management
  - Health checks and container monitoring

- **Implemented Service Isolation**
  - Separate railway.json for WebAPI and Worker services
  - Independent deployment configurations
  - Isolated watch paths to prevent cross-service builds

- **Enhanced Railway Configuration**
  - Root directory-based service separation
  - Dockerfile path optimization for build context
  - Fixed build context issues with dependency access

#### 🔧 Technical Improvements
- **Multi-Service Deployment**: Proper Railway monorepo configuration
- **Environment Variables**: Comprehensive staging and production configs
- **Build Optimization**: Efficient Docker multi-stage builds
- **Performance Tuning**: Worker-specific concurrency and prefetch settings

#### 📚 Documentation
- Complete CI/CD deployment guide
- Environment variable templates
- Troubleshooting section
- Service isolation documentation

#### 🔗 Related PRs
- PR #13: Add PlantAnalysisWorkerService Docker Support
- PR #14: Production Release with Docker & Railway Deployment
- Commit: 759eabe - Add service isolation with separate Railway configurations

### Version 1.0 - Previous
#### 🏗️ Initial Setup
- Basic Railway deployment for WebAPI
- PostgreSQL database integration
- Redis caching layer
- JWT authentication system

---

## 📞 Support

For deployment issues:
1. Check this documentation first
2. Review Railway logs for specific error messages
3. Verify environment variables match templates
4. Contact development team with specific error details

---

**🔄 Remember**: Update this document whenever deployment configurations change!