# Missing Admin Endpoints - Implementation TODO

**Last Updated:** 2025-10-23

Bu dosya test sırasında keşfedilen ancak henüz implement edilmemiş admin endpoint'lerini listeler.

---

## Plant Analysis Management

### ❌ Get All On-Behalf-Of Analyses
**Endpoint:** `GET /api/admin/plant-analysis/on-behalf-of`

**Current Status:** Placeholder endpoint - redirects to audit logs

**Current Response:**
```json
{
  "success": true,
  "message": "Use audit logs to view all OBO operations: GET /api/admin/audit/on-behalf-of"
}
```

**Expected Implementation:**
- List all plant analyses created by admins on behalf of users
- Filter by: adminUserId, targetUserId, date range, status
- Pagination support
- Include analysis summary and admin metadata

**Priority:** MEDIUM

**Use Cases:**
- Admin dashboard showing all OBO operations
- Audit and compliance tracking
- Performance monitoring

**Reference:**
- Test File: `PLANT_ANALYSIS_ASYNC_TEST_RESULTS.md`
- Test: Test 3 - Get All On-Behalf-Of Analyses

---

## Sponsorship Management

### ❌ Get Sponsorship Statistics
**Endpoint:** `GET /api/admin/sponsorship/statistics`

**Current Status:** 404 Not Found

**Expected Implementation:**
- Overall sponsorship statistics
- Total purchases, codes generated, codes used
- Revenue analytics (total, by tier, by sponsor)
- Conversion rates (code usage percentage)
- Time-based metrics (daily, weekly, monthly)

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "totalPurchases": 150,
    "totalRevenue": 125000.50,
    "totalCodesGenerated": 5000,
    "totalCodesUsed": 3200,
    "usageRate": 64.0,
    "byTier": {
      "S": { "purchases": 50, "revenue": 15000 },
      "M": { "purchases": 60, "revenue": 45000 },
      "L": { "purchases": 30, "revenue": 45000 },
      "XL": { "purchases": 10, "revenue": 20000 }
    },
    "topSponsors": [...]
  }
}
```

**Priority:** HIGH - Essential for admin dashboard

**Use Cases:**
- Admin dashboard overview
- Business intelligence and reporting
- Revenue tracking and forecasting

**Reference:**
- Test File: `SPONSORSHIP_MANAGEMENT_TEST_RESULTS.md`
- Test: Test 6 - Get Sponsorship Statistics

---

### ❌ Get Sponsor Detailed Report
**Endpoint:** `GET /api/admin/sponsorship/sponsors/{sponsorId}/detailed-report`

**Current Status:** 404 Not Found

**Expected Implementation:**
- Complete sponsor activity report
- Purchase history with details
- Code generation and usage analytics
- Farmer engagement metrics (codes distributed, farmers onboarded)
- ROI and conversion metrics
- Time-series data for trends

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "sponsor": {
      "id": 159,
      "name": "User 1114",
      "email": "sponsor@example.com",
      "joinedDate": "2025-01-15T10:00:00"
    },
    "purchases": {
      "total": 10,
      "totalAmount": 50000.00,
      "currency": "TRY",
      "history": [...]
    },
    "codes": {
      "generated": 500,
      "used": 320,
      "active": 180,
      "expired": 0,
      "usageRate": 64.0
    },
    "impact": {
      "farmersReached": 320,
      "analysesCreated": 450,
      "subscriptionsActivated": 310
    }
  }
}
```

**Priority:** HIGH - Critical for sponsor relationship management

**Use Cases:**
- Sponsor performance review
- Account management and support
- Invoice generation and reporting
- Sponsor retention analysis

**Reference:**
- Test File: `SPONSORSHIP_MANAGEMENT_TEST_RESULTS.md`
- Test: Test 7 - Get Sponsor Detailed Report

---

## Analytics Management

### ❌ Get User Statistics
**Endpoint:** `GET /api/admin/analytics/user-statistics`

**Current Status:** 404 Not Found

**Expected Implementation:**
- Total users, active users, inactive users
- User registration trends (today, this week, this month)
- User role distribution (Admin, Farmer, Sponsor)
- User activity metrics
- Average user engagement

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "totalUsers": 137,
    "activeUsers": 137,
    "inactiveUsers": 0,
    "registeredToday": 4,
    "registeredThisWeek": 4,
    "registeredThisMonth": 53,
    "byRole": {
      "Admin": 5,
      "Farmer": 120,
      "Sponsor": 12
    }
  }
}
```

**Priority:** HIGH - Essential for admin dashboard

**Use Cases:**
- Admin dashboard overview
- User growth tracking
- User engagement monitoring

**Reference:**
- Test File: `ANALYTICS_TEST_RESULTS.md`
- Test: Test 1 - Get User Statistics

---

### ❌ Get Subscription Statistics
**Endpoint:** `GET /api/admin/analytics/subscription-statistics`

**Current Status:** 404 Not Found

**Expected Implementation:**
- Total subscriptions by status (active, expired, cancelled)
- Subscriptions by type (trial, sponsored, paid)
- Revenue analytics
- Average subscription duration
- Subscription conversion rates

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "totalSubscriptions": 79,
    "activeSubscriptions": 52,
    "expiredSubscriptions": 25,
    "trialSubscriptions": 62,
    "sponsoredSubscriptions": 9,
    "paidSubscriptions": 8,
    "totalRevenue": 6729.90,
    "currency": "TRY",
    "avgDuration": 26.6
  }
}
```

**Priority:** HIGH - Essential for admin dashboard

**Use Cases:**
- Revenue tracking and forecasting
- Subscription health monitoring
- Business intelligence reporting

**Reference:**
- Test File: `ANALYTICS_TEST_RESULTS.md`
- Test: Test 2 - Get Subscription Statistics

---

### ❌ Get Dashboard Overview
**Endpoint:** `GET /api/admin/analytics/dashboard-overview`

**Current Status:** 404 Not Found

**Expected Implementation:**
- Consolidated dashboard metrics
- Quick stats for all major areas (users, subscriptions, sponsorships)
- Recent activity summary
- Key performance indicators (KPIs)
- Alerts and notifications

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "users": { "total": 137, "newToday": 4 },
    "subscriptions": { "active": 52, "revenue": 6729.90 },
    "sponsorships": { "totalPurchases": 12, "codesUsed": 18 },
    "analyses": { "totalToday": 10, "totalMonth": 250 },
    "alerts": []
  }
}
```

**Priority:** HIGH - Core admin dashboard feature

**Use Cases:**
- Main admin dashboard landing page
- Quick system health check
- Executive summary view

**Reference:**
- Test File: `ANALYTICS_TEST_RESULTS.md`
- Test: Test 4 - Get Dashboard Overview

---

### ❌ Get Activity Logs
**Endpoint:** `GET /api/admin/analytics/activity-logs`

**Current Status:** 404 Not Found

**Expected Implementation:**
- System-wide activity logs
- User actions and events
- Pagination support
- Filter by: user, action type, date range
- Search functionality

**Expected Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "userId": 166,
      "userName": "Tolga KAYA",
      "action": "User Login",
      "timestamp": "2025-10-23T19:00:00",
      "details": "Successful login from IP 192.168.1.1"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalCount": 1500
  }
}
```

**Priority:** MEDIUM - Useful for monitoring and troubleshooting

**Use Cases:**
- System activity monitoring
- Security audit trail
- Troubleshooting user issues
- Compliance reporting

**Reference:**
- Test File: `ANALYTICS_TEST_RESULTS.md`
- Test: Test 5 - Get Activity Logs

---

## Implementation Priority

### Phase 1 (HIGH Priority)
1. ❌ Get Sponsorship Statistics
2. ❌ Get Sponsor Detailed Report
3. ❌ Get User Statistics
4. ❌ Get Subscription Statistics
5. ❌ Get Dashboard Overview

### Phase 2 (MEDIUM Priority)
6. ❌ Get All OBO Plant Analyses (implement properly instead of placeholder)
7. ❌ Get Activity Logs

---

## Notes

- All missing endpoints are GET operations (read-only)
- No database schema changes required
- Primarily aggregation and reporting queries
- Should use existing repository methods where possible
- Consider caching for statistics endpoints (heavy queries)
- Export endpoint (`/api/admin/analytics/export`) already works - proves backend can compile statistics

---

**Created By:** Claude Code
**Date:** 2025-10-23
**Last Updated:** 2025-10-23
