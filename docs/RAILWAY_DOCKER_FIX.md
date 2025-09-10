# Railway Docker Build Fix

## Problem
Railway was unable to find project dependencies (Core, Business, DataAccess, Entities) when building Docker images because:
- Each service's `railway.json` sets its own directory as the build context
- Dockerfiles couldn't access parent directories outside the build context

## Solution

### 1. File Structure
```
ziraai/
├── Dockerfile.worker          # Worker service Dockerfile
├── Dockerfile.webapi          # WebAPI Dockerfile
├── railway.json               # Root Railway config
├── PlantAnalysisWorkerService/
│   ├── railway.json          # Service-specific config
│   └── .env.railway          # Environment variables
└── WebAPI/
    ├── railway.json          # Service-specific config
    └── .env.railway          # Environment variables
```

### 2. Railway Dashboard Configuration

**IMPORTANT**: You must set these environment variables in Railway Dashboard for each service:

#### For Worker Service:
1. Go to your Railway project
2. Select the PlantAnalysisWorkerService
3. Go to Variables tab
4. Add: `RAILWAY_DOCKERFILE_PATH=Dockerfile.worker`
5. Set Root Directory (in Settings): Leave empty (use repository root)

#### For WebAPI Service:
1. Go to your Railway project
2. Select the WebAPI service
3. Go to Variables tab
4. Add: `RAILWAY_DOCKERFILE_PATH=Dockerfile.webapi`
5. Set Root Directory (in Settings): Leave empty (use repository root)

### 3. How It Works
- Railway uses the repository root as build context
- `RAILWAY_DOCKERFILE_PATH` tells Railway which Dockerfile to use
- Dockerfiles can now access all project directories (Core, Business, etc.)
- Each service still has its own railway.json for deployment configuration

### 4. Alternative: Using Watch Paths
To prevent unnecessary rebuilds, configure watch paths in Railway Dashboard:

#### Worker Service Watch Paths:
```
PlantAnalysisWorkerService/**
Business/**
Core/**
DataAccess/**
Entities/**
```

#### WebAPI Watch Paths:
```
WebAPI/**
Business/**
Core/**
DataAccess/**
Entities/**
```

## Verification
After setting the environment variables, Railway should:
1. Use the repository root as build context
2. Find the correct Dockerfile (Dockerfile.worker or Dockerfile.webapi)
3. Successfully access all project dependencies
4. Build and deploy successfully

## Troubleshooting
If builds still fail:
1. Verify `RAILWAY_DOCKERFILE_PATH` is set correctly in Railway Dashboard
2. Check that Root Directory is empty (not set to a subdirectory)
3. Ensure Dockerfiles are in the repository root
4. Check Railway build logs for the exact error message