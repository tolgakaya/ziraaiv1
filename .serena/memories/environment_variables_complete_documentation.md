# Environment Variables Complete Documentation - Created

**Date:** 2025-10-07
**Status:** ✅ Complete

## What Was Created

Comprehensive environment variables documentation covering ALL configuration needs for ZiraAI project across all environments.

## File Location

`claudedocs/ENVIRONMENT_VARIABLES_COMPLETE_REFERENCE.md`

## Coverage

### 1. Critical Variables
- ASPNETCORE_ENVIRONMENT
- Database connection strings
- Redis configuration
- All mandatory variables

### 2. By Category (14 Sections Total)
1. Critical Variables
2. Database Configuration (PostgreSQL)
3. External Services (RabbitMQ, Redis, N8N)
4. Referral & Deep Links (Mobile App, Sponsor Request)
5. Security & Authentication (JWT, Internal Secrets)
6. File Storage (Local, FreeImageHost, ImgBB, S3)
7. Monitoring & Logging (Serilog, Performance)
8. SignalR Configuration
9. Background Jobs (Hangfire)
10. SMS & WhatsApp Services
11. Railway-Specific Variables
12. Development Environment (Complete)
13. Staging Environment (Complete)
14. Production Environment (Complete)

### 3. Environment-Specific Configurations

**Development:**
- Local PostgreSQL, Redis, RabbitMQ
- Mock SMS/WhatsApp providers
- FreeImageHost for images
- Debug logging enabled
- localhost:5001 URLs

**Staging:**
- Railway PostgreSQL, Redis, RabbitMQ
- Real SMS/WhatsApp (optional)
- S3 + CloudFront recommended
- Production-like settings
- ziraai-api-sit.up.railway.app URLs

**Production:**
- Railway managed services
- All real providers required
- S3 + CloudFront mandatory
- Optimized logging
- ziraai.com URLs

## Key Information

### Railway-Specific Variables
- `DATABASE_URL` → auto-mapped to `ConnectionStrings__DArchPgContext`
- `RAILWAY_ENVIRONMENT` → cloud detection
- `RAILWAY_PUBLIC_DOMAIN` → public URL
- `REDIS_HOST`, `REDIS_PORT`, `REDIS_PASSWORD` → Redis plugin
- `RABBITMQ_URL` → RabbitMQ plugin

### Critical Referral System Variables
```bash
MobileApp__PlayStorePackageName=com.ziraai.app.{env}
Referral__DeepLinkBaseUrl=https://{domain}/ref/
SponsorRequest__DeepLinkBaseUrl=https://{domain}/sponsor-request/
```

### Security Variables (Must be unique per environment)
```bash
TokenOptions__SecurityKey=UNIQUE_64_CHAR_MIN
Security__RequestTokenSecret=UNIQUE_SECRET
WebAPI__InternalSecret=UNIQUE_INTERNAL_SECRET
```

## Safety Features in Documentation

✅ No actual production passwords/keys included
✅ All examples use placeholders
✅ Warnings about secret rotation
✅ Security best practices section
✅ Different secrets per environment reminder
✅ Strong password policy documented

## Integration

- Added to `CLAUDE.md` "See Also" section with ⭐ star
- References existing environment-configuration.md
- Complements referral-testing-guide.md

## What Makes This Comprehensive

1. **Complete Coverage:** Every variable from appsettings.Development.json, appsettings.Staging.json, appsettings.Production.json
2. **WebAPI + WorkerService:** Both projects covered
3. **Railway Integration:** Railway-specific variables explained
4. **Type Information:** Data types, formats, required/optional status
5. **Examples:** Real-world examples for each environment
6. **Troubleshooting:** Common issues and solutions
7. **Verification:** Testing commands to verify configuration
8. **Security:** Best practices, rotation schedules, strong passwords

## Use Cases

1. **New Developer Onboarding:** Complete reference for local setup
2. **Railway Deployment:** Copy-paste environment variables for staging/production
3. **Troubleshooting:** Quick reference when configs fail
4. **Security Audit:** Review all secrets and sensitive data
5. **Documentation:** Single source of truth for all configs

## Next Steps for Team

1. **Staging Deployment:** Use staging section to configure Railway variables
2. **Production Deployment:** Use production section with unique secrets
3. **Keep Updated:** Update when adding new environment-specific configs
4. **Review Schedule:** Quarterly review (noted in doc)

## Related Documentation

- `docs/ENVIRONMENT_SETUP.md` - General setup guide (less detailed)
- `claudedocs/environment-configuration.md` - Referral-specific config
- `claudedocs/referral-testing-guide.md` - Testing with environments

## Validation Status

✅ All variables verified against source code
✅ appsettings.Development.json checked
✅ appsettings.Staging.json checked
✅ appsettings.Production.json checked
✅ Program.cs Railway mapping verified
✅ PlantAnalysisWorkerService configs included

## Critical Reminder

⚠️ **NEVER commit production values to Git**
⚠️ **Always use Railway environment variables for sensitive data**
⚠️ **Rotate secrets regularly (documented in guide)**
