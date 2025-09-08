# ZiraAI Environment Setup Guide

## Overview
ZiraAI uses a three-tier environment strategy: Development → Staging → Production

## Environment Architecture

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Development   │────▶│     Staging     │────▶│   Production    │
├─────────────────┤     ├─────────────────┤     ├─────────────────┤
│ Branch: dev     │     │ Branch: staging │     │ Branch: master  │
│ Local PostgreSQL│     │ Railway DB      │     │ Railway DB      │
│ Hot Reload      │     │ Auto Deploy     │     │ Manual Deploy   │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

## 1. Development Environment Setup

### Prerequisites
- .NET 9.0 SDK
- PostgreSQL 15+
- Docker (optional, for services)
- Visual Studio 2022 or VS Code

### Local Database Setup
```bash
# Create PostgreSQL database
psql -U postgres
CREATE DATABASE ziraai_dev;
CREATE USER ziraai WITH PASSWORD 'devpass';
GRANT ALL PRIVILEGES ON DATABASE ziraai_dev TO ziraai;
\q

# Run migrations
dotnet ef database update --project DataAccess --startup-project WebAPI
```

### Environment Configuration
1. Copy `.env.development.template` to `.env.development`
2. Update values for your local environment
3. Never commit `.env.development` to git

### Running Locally
```bash
# Install dependencies
dotnet restore

# Run with hot reload
dotnet watch run --project WebAPI/WebAPI.csproj

# Or run normally
dotnet run --project WebAPI/WebAPI.csproj
```

### Local Services (Docker)
```bash
# Start all services
docker-compose -f docker-compose.dev.yml up -d

# Services included:
# - PostgreSQL (5432)
# - RabbitMQ (5672/15672)
# - Redis (6379)
# - Mailhog (1025/8025)
```

## 2. Staging Environment Setup

### Railway Staging Project
1. Create new Railway project: `ziraai-staging`
2. Add PostgreSQL service
3. Configure environment variables from `.env.staging.template`

### GitHub Actions Setup
1. Add `RAILWAY_STAGING_TOKEN` to GitHub Secrets
2. Configure branch protection for `staging` branch
3. Enable auto-deploy from `staging` branch

### Deployment
```bash
# Merge to staging branch
git checkout staging
git merge development
git push origin staging

# Automatic deployment via GitHub Actions
```

## 3. Production Environment Setup

### Railway Production Project
1. Use existing Railway project: `ziraai-production`
2. Configure environment variables from `.env.production.template`
3. Enable manual deployment approval

### GitHub Actions Setup
1. Add `RAILWAY_PRODUCTION_TOKEN` to GitHub Secrets
2. Configure branch protection for `master` branch
3. Require PR reviews before merge

### Deployment
```bash
# Create PR from staging to master
git checkout master
git merge staging --no-ff
git push origin master

# Manual approval required in GitHub Actions
```

## 4. Branch Protection Rules

### Development Branch
- No direct pushes (optional for small teams)
- No force pushes
- Delete branch after merge: disabled

### Staging Branch
- Require pull request reviews: 1
- Dismiss stale reviews
- Require status checks (CI tests)
- No force pushes

### Master/Production Branch
- Require pull request reviews: 2
- Dismiss stale reviews
- Require status checks (all tests)
- Require branches to be up to date
- Include administrators
- No force pushes

## 5. Environment Variables Reference

### Critical Variables (All Environments)
- `DATABASE_CONNECTION_STRING`: PostgreSQL connection
- `JWT_SECRET_KEY`: Minimum 32 characters
- `ASPNETCORE_ENVIRONMENT`: Development/Staging/Production

### Service URLs
- Development: `https://localhost:5001`
- Staging: `https://ziraai-staging.railway.app`
- Production: `https://ziraai.com`

## 6. Database Migration Strategy

### Development → Staging
```bash
# Generate migration in development
dotnet ef migrations add MigrationName --project DataAccess --startup-project WebAPI

# Test locally
dotnet ef database update --project DataAccess --startup-project WebAPI

# Commit and push to staging
git add .
git commit -m "Add migration: MigrationName"
git push origin development
```

### Staging → Production
- Migrations auto-applied in staging
- Manually verified before production
- Rollback plan prepared

## 7. Monitoring & Logging

### Development
- Console logging
- Local file logs: `logs/development/`
- Swagger UI: `https://localhost:5001/swagger`

### Staging
- Application Insights
- File logs: `/var/log/ziraai/staging/`
- Swagger UI: `https://ziraai-staging.railway.app/swagger`

### Production
- Application Insights
- New Relic APM
- Sentry error tracking
- File logs: `/var/log/ziraai/production/`
- Swagger UI: Disabled

## 8. Troubleshooting

### Common Issues

#### PostgreSQL Connection Failed
```bash
# Check PostgreSQL service
systemctl status postgresql

# Test connection
psql -h localhost -U ziraai -d ziraai_dev
```

#### Migration Issues
```bash
# Remove last migration
dotnet ef migrations remove --project DataAccess --startup-project WebAPI

# Reset database
dotnet ef database drop --project DataAccess --startup-project WebAPI
dotnet ef database update --project DataAccess --startup-project WebAPI
```

#### Railway Deployment Failed
```bash
# Check Railway logs
railway logs --service ziraai-staging

# Redeploy
railway up --service ziraai-staging --environment staging
```

## 9. Security Checklist

- [ ] All secrets in environment variables
- [ ] Different JWT keys per environment
- [ ] Database passwords rotated regularly
- [ ] SSL certificates configured
- [ ] CORS properly configured
- [ ] Rate limiting enabled
- [ ] Security headers configured
- [ ] Audit logging enabled

## 10. Backup & Recovery

### Database Backups
- Development: Daily local backups
- Staging: Daily automated backups (7-day retention)
- Production: Hourly automated backups (30-day retention)

### Recovery Procedures
1. Identify issue and impact
2. Restore from latest backup
3. Apply migrations if needed
4. Verify data integrity
5. Resume operations

## Support
For issues or questions, contact the development team or check the project documentation.