# Admin Operations API - Documentation

**Version:** 1.0
**Branch:** feature/step-by-step-admin-operations
**Last Updated:** 2025-01-23

## 📋 Overview

This directory contains comprehensive documentation for the Admin Operations API, a complete administrative system for managing users, subscriptions, sponsorships, analytics, and plant analyses with full audit trail support.

## 📚 Documentation Files

### 1. **ADMIN_OPERATIONS_API_COMPLETE_GUIDE.md**
**Comprehensive API reference with 31 endpoints**

**Contents:**
- ✅ Authentication & Authorization
- ✅ All endpoint specifications
- ✅ Request/response examples
- ✅ Test scenarios
- ✅ Error handling
- ✅ Best practices

**Sections:**
- User Management (6 endpoints)
- Subscription Management (6 endpoints)
- Sponsorship Management (9 endpoints)
- Analytics & Reporting (4 endpoints)
- Audit Logs (4 endpoints)
- Plant Analysis Management (3 endpoints)

### 2. **ZiraAI_Admin_Operations_API.postman_collection.json**
**Ready-to-use Postman collection**

**Features:**
- ✅ 31 pre-configured requests
- ✅ Environment variable support
- ✅ Automatic token management
- ✅ Test scripts included
- ✅ Organized by feature

**How to Use:**
1. Import into Postman
2. Configure environment variables
3. Run authentication request
4. Start testing

### 3. **TESTING_GUIDE.md**
**Complete testing manual with validation**

**Contents:**
- ✅ Environment setup
- ✅ 5 detailed test scenarios
- ✅ Validation checklists
- ✅ Database verification queries
- ✅ Performance benchmarks
- ✅ Troubleshooting guide

**Scenarios Covered:**
1. User Lifecycle Management
2. Subscription Management
3. Sponsor On-Behalf-Of Operations
4. Bulk Operations
5. Analytics and Reporting

### 4. **ADMIN_API_SUMMARY.md**
**Quick reference and feature summary**

**Previous documentation** - Still valid, contains:
- Sprint summaries
- Implementation details
- Feature checklist

### 5. **ADMIN_OPERATIONS_COMPREHENSIVE_ANALYSIS.md**
**Technical architecture and analysis**

**Previous documentation** - Contains:
- Database schema
- Architecture diagrams
- Security considerations

### 6. **ADMIN_OPERATIONS_EXECUTION_PLAN.md**
**Development sprint plan**

**Previous documentation** - Contains:
- Sprint breakdown
- Implementation timeline
- Feature dependencies

### 7. **Database Migration Files**
- `001_AddAdminOperationLogs.sql`
- `002_AddUserAdminActionColumns.sql`
- `003_AddPlantAnalysesOBOColumns.sql`
- `README_MIGRATIONS.md`

## 🚀 Quick Start

### Prerequisites
```bash
✅ .NET 9.0 SDK
✅ PostgreSQL 14+
✅ Postman (or similar API client)
✅ Admin account credentials
```

### Setup Steps

**1. Database Setup**
```sql
-- Run migrations in order
\i 001_AddAdminOperationLogs.sql
\i 002_AddUserAdminActionColumns.sql
\i 003_AddPlantAnalysesOBOColumns.sql
```

**2. Import Postman Collection**
- Open Postman
- Import → `ZiraAI_Admin_Operations_API.postman_collection.json`
- Set environment variables:
  - `baseUrl`: Your API base URL
  - `adminToken`: Will be auto-set after login

**3. Authenticate**
```http
POST {{baseUrl}}/api/auth/login
{
  "email": "admin@ziraai.com",
  "password": "your-password"
}
```

**4. Start Testing**
- Run any endpoint from the collection
- All requests automatically use `{{adminToken}}`

## 📊 Feature Summary

### ✅ Implemented Features

#### User Management
- [x] Get all users with pagination
- [x] Get user by ID
- [x] Search users
- [x] Deactivate user
- [x] Reactivate user
- [x] Bulk deactivate users

#### Subscription Management
- [x] Get all subscriptions
- [x] Get subscription by ID
- [x] Assign subscription
- [x] Extend subscription
- [x] Cancel subscription
- [x] Bulk cancel subscriptions

#### Sponsorship Management
- [x] Get all purchases
- [x] Get purchase by ID
- [x] Approve purchase
- [x] Refund purchase
- [x] **Create purchase on behalf of sponsor (OBO)**
- [x] **Bulk send codes to farmers (OBO)**
- [x] **Get sponsor detailed report (OBO)**
- [x] Get all codes
- [x] Deactivate code

#### Analytics & Reporting
- [x] Get user statistics
- [x] Get subscription statistics
- [x] Get sponsorship statistics
- [x] Export statistics to CSV

#### Audit Logs
- [x] Get all audit logs
- [x] Get audit logs by admin
- [x] Get audit logs by target user
- [x] Get on-behalf-of logs

#### Plant Analysis Management
- [x] Create analysis on behalf of user
- [x] Get user analyses
- [x] Get all OBO analyses

### 🎯 Key Features

**Full Audit Trail**
- Every admin operation logged
- AdminOperationLog table
- Searchable and filterable

**On-Behalf-Of Support**
- Admin can act as sponsor
- Manual payment support
- Bulk code distribution
- Complete audit trail

**Bulk Operations**
- Efficient batch processing
- Individual audit logs
- Rollback capability

**Analytics & Export**
- Real-time statistics
- CSV export capability
- Date range filtering

**Pagination**
- All list endpoints
- Configurable page size
- Performance optimized

## 🔒 Security

### Authentication
- JWT Bearer token required
- Token auto-renewal supported
- Secure token storage

### Authorization
- Admin role required
- Operation claim verification
- Fine-grained permissions

### Audit
- Complete operation logging
- IP address tracking
- User agent capture
- Before/after state tracking

## 📖 API Endpoints Overview

### Base URL
```
Development: https://localhost:5001
Staging: https://ziraai-api-sit.up.railway.app
Production: https://ziraai.com
```

### Endpoint Count by Module

| Module | Endpoints | Description |
|--------|-----------|-------------|
| User Management | 6 | User CRUD and bulk operations |
| Subscription Management | 6 | Subscription lifecycle management |
| Sponsorship Management | 9 | Purchase, codes, and OBO operations |
| Analytics & Reporting | 4 | Statistics and CSV export |
| Audit Logs | 4 | Operation tracking and viewing |
| Plant Analysis Management | 3 | Analysis OBO operations |
| **TOTAL** | **32** | Including authentication |

## 🧪 Testing

### Test Coverage

**Unit Tests:** (Recommended)
- Business logic validation
- Handler testing
- Service testing

**Integration Tests:** (Use Postman Collection)
- End-to-end workflows
- Database operations
- Audit trail verification

**Performance Tests:**
- Load testing recommendations
- Response time benchmarks
- Query optimization

### Run Tests

**Using Postman:**
```bash
# Import collection
# Set environment variables
# Run "0. Authentication" folder
# Execute test scenarios from TESTING_GUIDE.md
```

**Database Verification:**
```sql
-- After each operation, verify:
SELECT * FROM "AdminOperationLogs"
ORDER BY "CreatedDate" DESC
LIMIT 10;
```

## 📈 Performance

### Benchmarks

| Operation | Expected | Max Acceptable |
|-----------|----------|----------------|
| Get Users (50) | < 200ms | 500ms |
| Deactivate User | < 100ms | 300ms |
| Bulk Ops (10) | < 500ms | 1000ms |
| Statistics | < 300ms | 800ms |
| CSV Export | < 1000ms | 3000ms |

### Optimization Tips

1. **Use Pagination**: Always specify page and pageSize
2. **Filter Data**: Use date ranges for analytics
3. **Index Coverage**: Ensure proper database indexes
4. **Batch Operations**: Use bulk endpoints for multiple items

## 🐛 Troubleshooting

### Common Issues

**401 Unauthorized**
- Check token validity
- Re-authenticate
- Verify Bearer format

**403 Forbidden**
- Verify Admin role
- Check operation claims
- Review permissions

**Audit Log Missing**
- Check service registration
- Verify transaction scope
- Review database logs

**Performance Issues**
- Enable query logging
- Check database indexes
- Monitor connection pool

### Debug Queries

```sql
-- Check recent admin operations
SELECT a.*, u1."FullName" as AdminName, u2."FullName" as TargetName
FROM "AdminOperationLogs" a
LEFT JOIN "Users" u1 ON a."AdminUserId" = u1."UserId"
LEFT JOIN "Users" u2 ON a."TargetUserId" = u2."UserId"
ORDER BY a."CreatedDate" DESC
LIMIT 20;

-- Check slow queries
SELECT query, mean_exec_time, calls
FROM pg_stat_statements
WHERE query LIKE '%AdminOperationLog%'
ORDER BY mean_exec_time DESC
LIMIT 10;
```

## 📝 Best Practices

### Development
1. Always provide reason/notes for operations
2. Use pagination for all list endpoints
3. Verify audit logs after critical operations
4. Test in staging before production
5. Monitor performance metrics

### Operations
1. Review audit logs regularly
2. Export statistics for reporting
3. Clean up test data
4. Backup before bulk operations
5. Document manual interventions

### Security
1. Never commit admin credentials
2. Rotate tokens regularly
3. Monitor suspicious activity
4. Review audit logs for anomalies
5. Implement rate limiting

## 🔄 Changelog

### Version 1.0 (2025-01-23)
**Initial Release**

**Added:**
- ✅ Complete Admin Operations API (31 endpoints)
- ✅ Full audit trail system
- ✅ On-behalf-of sponsor operations
- ✅ Bulk operations support
- ✅ Analytics with CSV export
- ✅ Comprehensive documentation
- ✅ Postman collection
- ✅ Testing guide

**Database:**
- ✅ AdminOperationLog table
- ✅ User admin action columns
- ✅ PlantAnalysis OBO columns

## 📞 Support

### Resources
- **Main Documentation**: `ADMIN_OPERATIONS_API_COMPLETE_GUIDE.md`
- **Testing Guide**: `TESTING_GUIDE.md`
- **Postman Collection**: `ZiraAI_Admin_Operations_API.postman_collection.json`

### Contact
- **GitHub Issues**: https://github.com/tolgakaya/ziraaiv1/issues
- **API Documentation**: See Swagger at `/swagger`

## 🎯 Next Steps

### Recommended Reading Order
1. Start with this README
2. Read `ADMIN_OPERATIONS_API_COMPLETE_GUIDE.md`
3. Import Postman collection
4. Follow `TESTING_GUIDE.md`
5. Review audit trail in database

### Future Enhancements (Proposed)
- [ ] Sponsor profile management OBO
- [ ] Code regeneration and transfer
- [ ] Sponsor request bulk approval
- [ ] Manual statistics adjustment
- [ ] Financial operations (refunds, price adjustments)
- [ ] Bulk notification to sponsors
- [ ] Messaging quota management
- [ ] Advanced reporting and dashboards

---

**Generated:** 2025-01-23
**Version:** 1.0
**Branch:** feature/step-by-step-admin-operations
**Status:** ✅ Production Ready
