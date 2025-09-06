# ZiraAI Seed Data Documentation

## Overview
This document describes the seed data system implemented for ZiraAI application to ensure all necessary data is present in the database for the application to function properly on initial setup.

## Architecture

### Seed Data Structure
The seed data system is organized into the following components:

```
Business/
├── Seeds/
│   ├── ConfigurationSeeds.cs      # System configuration settings
│   ├── OperationClaimSeeds.cs     # Permission definitions
│   ├── GroupSeeds.cs               # User groups and group claims
│   └── UserSeeds.cs                # Default users and subscriptions
└── Services/
    └── DatabaseInitializer/
        ├── IDatabaseInitializerService.cs
        └── DatabaseInitializerService.cs
```

## Seed Data Categories

### 1. Configuration Data (`ConfigurationSeeds.cs`)
Contains 17 system configuration entries:
- **Image Processing Settings** (10 entries)
  - Image size limits (max 0.25MB for AI processing)
  - Dimension constraints (1920x1080 max)
  - Auto-resize settings
  - Supported formats
- **Application Settings** (3 entries)
  - N8N webhook URLs
  - Timeout configurations
- **RabbitMQ Settings** (4 entries)
  - Connection strings
  - Queue names

### 2. Operation Claims (`OperationClaimSeeds.cs`)
91 permission definitions organized by category:
- **System Administration** (IDs 1-4)
  - Admin, UserManagement, RoleManagement, ConfigurationManagement
- **User Roles** (IDs 5-6)
  - Farmer, Sponsor
- **Plant Analysis** (IDs 10-15)
  - CRUD operations, List, Export
- **Subscription Management** (IDs 20-25)
  - CRUD operations, Tier management
- **Sponsorship** (IDs 30-37)
  - CRUD operations, Code generation/distribution
- **Sponsor Profile** (IDs 40-45)
  - Profile management, Contact management, WhatsApp requests
- **Smart Links** (IDs 50-54) - XL Tier exclusive
  - CRUD operations, Analytics
- **Analytics** (IDs 60-62)
  - Dashboard, Reports, Export
- **Logs & Audit** (IDs 70-72)
  - View logs, Export, Security events
- **API Access** (IDs 80-82)
  - Full access, Read-only, Plant Analysis API
- **Mobile App** (IDs 90-91)
  - Access, Push notifications

### 3. Groups (`GroupSeeds.cs`)
5 default groups with associated claims:
- **Administrators** (ID 1)
  - Full system access (all 91 claims)
- **Farmers** (ID 2)
  - Basic plant analysis and profile access (8 claims)
- **Sponsors** (ID 3)
  - Sponsorship management and analytics (25 claims)
- **Support** (ID 4)
  - Read access and basic management (12 claims)
- **API Users** (ID 5)
  - API access only (2 claims)

### 4. Users (`UserSeeds.cs`)
3 default user accounts:

#### Admin User
- **Email**: admin@ziraai.com
- **Password**: Admin@123!
- **Subscription**: XL (10 years, complimentary)
- **Group**: Administrators
- **Note**: Change password after first login!

#### Demo Farmer
- **Email**: farmer@demo.ziraai.com
- **Password**: Farmer@123!
- **Subscription**: Trial (30 days)
- **Group**: Farmers

#### Demo Sponsor
- **Email**: sponsor@demo.ziraai.com
- **Password**: Sponsor@123!
- **Subscription**: L (1 month, auto-renew)
- **Group**: Sponsors
- **Profile**: Demo Agricultural Supplies Co.

### 5. Subscription Tiers
5 tiers (seeded via Entity Framework migrations):
- **Trial**: 30-day trial, 1 daily/30 monthly requests
- **S (Small)**: 99.99 TRY/month, 5 daily/50 monthly
- **M (Medium)**: 299.99 TRY/month, 20 daily/200 monthly
- **L (Large)**: 599.99 TRY/month, 50 daily/500 monthly
- **XL (Extra Large)**: 999.99 TRY/month, unlimited/1000 monthly

### 6. Languages & Translations
- **Languages**: Turkish (tr-TR), English (en-US)
- **Translations**: 136+ UI text translations

## Database Initialization Process

### Automatic Initialization
The `DatabaseInitializerService` runs automatically on application startup:

1. **Check Phase**: Verifies if essential data exists
   - Operation Claims
   - Admin User
   - Groups
   - Subscription Tiers

2. **Seed Phase**: If data is missing, seeds in order:
   1. Operation Claims
   2. Groups
   3. Group Claims
   4. Configuration
   5. Subscription Tiers
   6. Users
   7. User Groups
   8. User Subscriptions
   9. Sponsor Profile

### Manual Initialization
For manual seeding or re-seeding:

```bash
# Run migrations first
dotnet ef database update --project DataAccess --startup-project WebAPI

# Application will seed on first run
dotnet run --project WebAPI
```

## Implementation Details

### Service Registration
In `WebAPI/Startup.cs`:
```csharp
services.AddScoped<IDatabaseInitializerService, DatabaseInitializerService>();
```

### Initialization Call
In `Configure` method:
```csharp
InitializeDatabase(app).GetAwaiter().GetResult();
```

### Logging
The service logs all operations:
- Info: Successful seeding operations
- Warning: Important notices (e.g., default passwords)
- Error: Initialization failures

## Security Considerations

### Default Credentials
⚠️ **IMPORTANT**: Change default passwords immediately after first login!

| Account | Email | Default Password |
|---------|-------|------------------|
| Admin | admin@ziraai.com | Admin@123! |
| Demo Farmer | farmer@demo.ziraai.com | Farmer@123! |
| Demo Sponsor | sponsor@demo.ziraai.com | Sponsor@123! |

### Password Requirements
All passwords must meet:
- Minimum 8 characters
- At least 1 uppercase letter
- At least 1 lowercase letter
- At least 1 digit
- At least 1 special character

## Verification Commands

### Check Seed Data Status
```sql
-- Check Operation Claims
SELECT COUNT(*) FROM "OperationClaims";  -- Should be 91

-- Check Groups
SELECT COUNT(*) FROM "Groups";  -- Should be 5

-- Check Admin User
SELECT * FROM "Users" WHERE "Email" = 'admin@ziraai.com';

-- Check Subscription Tiers
SELECT COUNT(*) FROM "SubscriptionTiers";  -- Should be 5

-- Check Configuration
SELECT COUNT(*) FROM "Configurations";  -- Should be 17
```

### Reset Seed Data
To reset and re-seed:
```sql
-- WARNING: This will delete all data!
TRUNCATE TABLE "UserGroups", "GroupClaims", "UserSubscriptions", "SponsorProfiles" CASCADE;
TRUNCATE TABLE "Users", "Groups", "OperationClaims", "Configurations" CASCADE;
```

Then restart the application to re-seed.

## Troubleshooting

### Common Issues

1. **Seed data not creating**
   - Check database connection string
   - Verify migrations are applied
   - Check application logs for errors

2. **Duplicate key errors**
   - Database already contains partial seed data
   - Use reset commands above if safe

3. **Permission denied errors**
   - Ensure database user has CREATE/INSERT permissions
   - Check PostgreSQL role permissions

### Log Locations
- Development: Console output
- Production: Check configured log providers (File, PostgreSQL)

## Maintenance

### Adding New Seed Data
1. Update appropriate seed class in `Business/Seeds/`
2. Modify `DatabaseInitializerService` if new entity
3. Test in development environment
4. Document changes here

### Updating Existing Seed Data
1. Create migration for data updates
2. Or update seed classes and reset database
3. Test thoroughly before production deployment

## Environment-Specific Configurations

### Development
- Full seed data including demo accounts
- Verbose logging enabled
- All features enabled

### Staging
- Production-like seed data
- Demo accounts optional
- Standard logging

### Production
- Minimal seed data (admin only)
- No demo accounts
- Security-focused logging
- Change admin password immediately

## Related Documentation
- [Database Schema](./DATABASE_SCHEMA.md)
- [Authentication & Authorization](./AUTH_DOCUMENTATION.md)
- [API Documentation](./API_DOCUMENTATION.md)
- [Deployment Guide](./DEPLOYMENT_GUIDE.md)

## Contact
For issues or questions about seed data:
- Check application logs first
- Review this documentation
- Contact DevOps team for production issues