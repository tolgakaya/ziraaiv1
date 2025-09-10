# Railway Deployment Troubleshooting History
**Date**: September 9, 2025

## Problem Evolution & Solutions

### Issue 1: Railway Auto-Detection Conflicts
**Problem**: Railway was using WebAPI Dockerfile for both services
**Error**: "Using Detected Dockerfile" - ignoring railway.json configs
**Solution Attempts**:
1. ❌ Created unique dockerfile names in service directories (WebAPI.Dockerfile, PlantAnalysisWorkerService.Dockerfile)
2. ❌ Updated railway.json with explicit paths
3. ✅ Moved Dockerfiles to repository root as Dockerfile.worker and Dockerfile.webapi

### Issue 2: Build Context Problems
**Problem**: "/Core/Core.csproj": not found
**Cause**: Railway using service directory as build context instead of repository root
**Solution**: Dockerfiles at root level with correct COPY paths from repository root

### Issue 3: Railway Config Path Discovery
**Problem**: No explicit field to specify railway.json location in Railway Dashboard
**Solution**: Use Root Directory setting - Railway automatically finds railway.json in that directory

## Final Working Configuration

### File Structure
```
/ (repository root)
  Dockerfile.worker          # PlantAnalysisWorkerService Dockerfile
  Dockerfile.webapi         # WebAPI Dockerfile
  
/PlantAnalysisWorkerService
  railway.json              # Points to ../Dockerfile.worker
  
/WebAPI
  railway.json              # Points to ../Dockerfile.webapi
```

### Railway Dashboard Settings
- Root Directory: `/PlantAnalysisWorkerService` or `/WebAPI`
- Railway finds railway.json automatically in root directory
- Dockerfiles referenced with `../` prefix to access root level

## Lessons Learned
1. Railway's auto-detection is aggressive and overrides railway.json unless Dockerfiles have unique names
2. Build context must be repository root for monorepo with shared dependencies
3. Railway Config Path is implicit - determined by Root Directory setting
4. Dockerfile paths in railway.json are relative to the railway.json location, not repository root

## Commands for Local Testing
```bash
# Test Worker Service
docker build -f Dockerfile.worker -t ziraai-worker .

# Test WebAPI
docker build -f Dockerfile.webapi -t ziraai-webapi .
```

Both commands run from repository root