# 🚨 Deployment Configuration Change Notice

**Date**: [YYYY-MM-DD]  
**Changed By**: [Developer Name]  
**Change Type**: [Configuration/Environment/Service/Infrastructure]

## 📋 Summary
Brief description of what changed in the deployment configuration.

## 🔄 Changes Made

### Railway Configuration
- [ ] Railway.json modified
- [ ] Service settings updated
- [ ] Environment variables changed
- [ ] Dockerfile updated
- [ ] Root directory changed

### Environment Variables
**Staging Changes:**
```env
# List new/modified variables
NEW_VARIABLE="value"
MODIFIED_VARIABLE="new_value"
```

**Production Changes:**
```env
# List new/modified variables  
NEW_VARIABLE="value"
MODIFIED_VARIABLE="new_value"
```

### Service Configuration
- [ ] WebAPI configuration
- [ ] PlantAnalysisWorkerService configuration
- [ ] Database changes
- [ ] Cache configuration
- [ ] Message queue settings

## 🎯 Impact Analysis

### Affected Services
- [ ] WebAPI
- [ ] PlantAnalysisWorkerService
- [ ] Database
- [ ] Redis Cache
- [ ] RabbitMQ
- [ ] External integrations

### Deployment Impact
- [ ] Requires new deployment
- [ ] Requires restart
- [ ] Zero-downtime compatible
- [ ] Requires database migration
- [ ] Requires cache flush

## ✅ Testing Checklist

- [ ] Tested in development environment
- [ ] Staging deployment successful
- [ ] Health checks passing
- [ ] Environment variables validated
- [ ] Service communication verified
- [ ] Performance impact assessed

## 📚 Documentation Updates

- [ ] CI-CD-DEPLOYMENT-GUIDE.md updated
- [ ] Environment variable templates updated
- [ ] Troubleshooting section updated
- [ ] README updated (if applicable)

## 🔧 Action Items

### Before Deployment
- [ ] Backup current configuration
- [ ] Notify team members
- [ ] Schedule deployment window
- [ ] Prepare rollback plan

### During Deployment  
- [ ] Monitor deployment logs
- [ ] Verify health checks
- [ ] Test critical functionality
- [ ] Check performance metrics

### After Deployment
- [ ] Update CI-CD-DEPLOYMENT-GUIDE.md
- [ ] Document any issues encountered
- [ ] Update team on completion
- [ ] Monitor for 24 hours

## 🚨 Rollback Plan
Describe how to rollback these changes if something goes wrong:

1. [Step 1]
2. [Step 2]
3. [Step 3]

## 📝 Notes
Additional notes, warnings, or special considerations.

---
**REMINDER**: Update CI-CD-DEPLOYMENT-GUIDE.md with these changes!