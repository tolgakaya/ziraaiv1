# Railway Deployment Best Practices for ZiraAI

## Multi-Service Monorepo Strategy

### Service Isolation Pattern
```
Repository Structure:
├── WebAPI/ (Service 1)
│   ├── Dockerfile (relative to WebAPI/)
│   └── railway.json (WebAPI config)
├── PlantAnalysisWorkerService/ (Service 2)  
│   ├── Dockerfile (relative to PlantAnalysisWorkerService/)
│   └── railway.json (Worker config)
└── Shared Dependencies/ (Business, Core, DataAccess, Entities)
```

### Railway Configuration Rules

#### Root Directory Configuration
- **WebAPI Service**: Root Directory = `/WebAPI`
- **Worker Service**: Root Directory = `/PlantAnalysisWorkerService`
- **Build Context**: Limited to service directory + shared dependencies

#### Dockerfile Path Configuration
- **Must be relative** to root directory: `"dockerfilePath": "Dockerfile"`
- **Never absolute paths**: ❌ `"dockerfilePath": "WebAPI/Dockerfile"`
- **Railway auto-detects** dockerfile in root directory when relative path used

#### Watch Paths for Efficient CI/CD
```json
"watchPaths": [
  "ServiceName/**",       // Service-specific changes
  "Business/**",          // Shared business logic
  "Core/**",             // Cross-cutting concerns
  "DataAccess/**",       // Data access changes
  "Entities/**"          // Entity modifications
]
```

### Environment Variable Strategy

#### Service Feature Flags
```
WebAPI Specific:
- UseHangfire="false" (WebAPI doesn't need background jobs)
- UseTaskScheduler="false" (No task scheduling in API)

Worker Specific:  
- UseHangfire="true" (Worker needs background job processing)
- UseTaskScheduler="true" (Worker handles scheduled tasks)
```

#### Shared Infrastructure Variables
- Database: Same ConnectionStrings__DArchPgContext for both services
- Redis: Same CacheOptions__ configuration  
- RabbitMQ: Same RabbitMQ__ConnectionString for message passing
- JWT: Same TokenOptions__ for authentication consistency

#### Service-Specific Performance Tuning
```
Worker Performance Variables:
- WORKER_CONCURRENCY="8" (staging), "10" (production)
- WORKER_PREFETCH_COUNT="15" (staging), "20" (production)
- WORKER_HEARTBEAT_INTERVAL="30"
```

### Common Pitfalls and Solutions

#### Build Context Problems
**Problem**: `/Core/Core.csproj: not found` error
**Cause**: Incorrect root directory or dockerfile path
**Solution**: 
- Root directory must contain the service files
- Dockerfile path must be relative to root directory
- Shared dependencies must be accessible from build context

#### Service Deployment Conflicts
**Problem**: Wrong service being deployed
**Cause**: Missing or incorrect railway.json configuration
**Solution**:
- Each service MUST have its own railway.json
- Dockerfile paths must be service-specific
- Watch paths must prevent cross-service triggers

#### Environment Variable Confusion
**Problem**: Service can't connect to infrastructure
**Cause**: Copy-paste errors in environment variables
**Solution**:
- Use environment-specific templates
- Double-check connection string formats
- Test staging before production deployment

### Health Check Implementation
```json
"deploy": {
  "healthcheckPath": "/health",
  "healthcheckTimeout": 300,
  "restartPolicyType": "ON_FAILURE",
  "restartPolicyMaxRetries": 10
}
```

### Branch Strategy for Railway
```
development → staging (auto-deploy) → master (manual approval) → production
```

### Railway Service Naming Convention
- **API Service**: `ziraai-api` (clear service identification)
- **Worker Service**: `ziraai-worker` (distinguishes from API)
- **Environment Suffix**: `-staging`, `-prod` for environment clarity

### Documentation Maintenance
- **Mandatory**: Update CI-CD-DEPLOYMENT-GUIDE.md for any configuration change
- **Template**: Use DEPLOYMENT_CHANGE_TEMPLATE.md for tracking
- **Version Control**: Maintain change log with commit references

### Performance Monitoring
- Container resource limits appropriate for workload
- Health check endpoints for all services
- Monitoring logs for deployment issues
- Regular review of Railway metrics dashboard