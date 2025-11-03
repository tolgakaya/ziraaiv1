# Sponsor API Quick Reference

## All Sponsorship Endpoints (58 Total)

### Authentication (11 - Shared with all roles)
```
POST   /Auth/login                              ✅ Public
POST   /Auth/login-phone                        ✅ Public
POST   /Auth/register                           ✅ Public
POST   /Auth/register-phone                     ✅ Public
POST   /Auth/refresh-token                      ✅ Public
POST   /Auth/verify                             ✅ Public
POST   /Auth/verify-phone-otp                   ✅ Public
POST   /Auth/verify-phone-register              ✅ Public
PUT    /Auth/forgot-password                    ✅ Public
PUT    /Auth/user-password                      🔒 Auth Required
POST   /Auth/test                               🔒 Auth Required
```

### Profile Management (4)
```
POST   /sponsorship/create-profile              🔒 Any Authenticated
PUT    /sponsorship/update-profile              🔒 Sponsor, Admin
GET    /sponsorship/profile                     🔒 Sponsor, Admin
POST   /sponsorship/profile                     🔒 Sponsor, Admin (Legacy)
```

### Tier & Purchase (3)
```
GET    /sponsorship/tiers-for-purchase          ✅ Public
POST   /sponsorship/purchase-package            🔒 Sponsor, Admin
GET    /sponsorship/purchases                   🔒 Sponsor, Admin
```

### Code Management (4)
```
POST   /sponsorship/codes                       🔒 Sponsor, Admin
GET    /sponsorship/codes                       🔒 Sponsor, Admin (Paginated: max 200)
GET    /sponsorship/validate/{code}             🔒 Farmer, Sponsor, Admin
POST   /sponsorship/send-link                   🔒 Sponsor, Admin (SMS/WhatsApp)
```

### Farmer Insights (2)
```
GET    /sponsorship/farmers                     🔒 Sponsor, Admin
GET    /sponsorship/analyses                    🔒 Sponsor, Admin (Paginated: max 100)
GET    /sponsorship/analysis/{plantAnalysisId}  🔒 Sponsor, Admin (Tier-filtered)
```

### Core Analytics (4)
```
GET    /sponsorship/dashboard-summary           🔒 Sponsor, Admin (Cache: 15m)
GET    /sponsorship/statistics                  🔒 Sponsor, Admin
GET    /sponsorship/package-statistics          🔒 Sponsor, Admin
GET    /sponsorship/code-analysis-statistics    🔒 Sponsor, Admin
```

### Advanced Analytics (6)
```
GET    /sponsorship/link-statistics             🔒 Sponsor, Admin
GET    /sponsorship/messaging-analytics         🔒 Sponsor, Admin (Cache: 15m)
GET    /sponsorship/impact-analytics            🔒 Sponsor, Admin (Cache: 6h)
GET    /sponsorship/temporal-analytics          🔒 Sponsor, Admin (Cache: 1h)
GET    /sponsorship/roi-analytics               🔒 Sponsor, Admin (Cache: 12h)
GET    /sponsorship/smart-links/performance     🔒 Sponsor, Admin
```

### Basic Messaging (4)
```
POST   /sponsorship/messages                    🔒 Sponsor, Farmer, Admin (M+ tier)
GET    /sponsorship/messages/conversation       🔒 Sponsor, Farmer, Admin
PATCH  /sponsorship/messages/{messageId}/read   🔒 Sponsor, Farmer, Admin
PATCH  /sponsorship/messages/read               🔒 Sponsor, Farmer, Admin (Bulk)
```

### Advanced Messaging (7)
```
POST   /sponsorship/messages/attachments        🔒 Sponsor, Farmer, Admin (M+ tier)
POST   /sponsorship/messages/voice              🔒 Sponsor, Farmer, Admin (XL tier)
PUT    /sponsorship/messages/{messageId}        🔒 Sponsor, Farmer, Admin (M+ tier, 1h limit)
DELETE /sponsorship/messages/{messageId}        🔒 Sponsor, Farmer, Admin (24h limit)
POST   /sponsorship/messages/{messageId}/forward 🔒 Sponsor, Farmer, Admin (M+ tier)
GET    /sponsorship/messaging/features          🔒 Authenticated
PATCH  /sponsorship/admin/messaging/features/{id} 🔒 Admin only
```

### Smart Links (3 - XL Tier Only)
```
POST   /sponsorship/smart-links                 🔒 Sponsor, Admin (XL only)
GET    /sponsorship/smart-links                 🔒 Sponsor, Admin
GET    /sponsorship/smart-links/performance     🔒 Sponsor, Admin
```

### Logo & Branding (2)
```
GET    /sponsorship/logo-permissions/analysis/{id} 🔒 Authenticated
GET    /sponsorship/display-info/analysis/{id}     🔒 Authenticated
```

### Dealer Invitations (6)
```
POST   /sponsorship/dealer/invite                  🔒 Sponsor, Admin
POST   /sponsorship/dealer/invite-via-sms          🔒 Sponsor, Admin
GET    /sponsorship/dealer/invitation-details      ✅ Public (token required)
POST   /sponsorship/dealer/accept-invitation       🔒 Authenticated
GET    /sponsorship/dealer/invitations             🔒 Sponsor, Admin
GET    /sponsorship/dealer/invitations/my-pending  🔒 Dealer, Farmer, Sponsor
```

### Dealer Code Transfer (3)
```
POST   /sponsorship/dealer/transfer-codes       🔒 Sponsor, Admin
POST   /sponsorship/dealer/reclaim-codes        🔒 Sponsor, Admin
GET    /sponsorship/dealer/search               🔒 Sponsor, Admin (email search)
```

### Dealer Analytics (2)
```
GET    /sponsorship/dealer/analytics/{dealerId} 🔒 Sponsor, Admin
GET    /sponsorship/dealer/summary              🔒 Sponsor, Admin
```

### Dealer Self-Service (2)
```
GET    /sponsorship/dealer/my-codes             🔒 Dealer, Sponsor (Paginated: max 200)
GET    /sponsorship/dealer/my-dashboard         🔒 Dealer, Sponsor
```

### Debug (1)
```
GET    /sponsorship/debug/user-info             🔒 Authenticated
```

---

## ❌ NOT for Sponsor (5 - Farmer-Only)

```
POST   /sponsorship/redeem                      🔒 Farmer, Admin
GET    /sponsorship/my-sponsor                  🔒 Farmer, Admin
POST   /sponsorship/messages/block              🔒 Farmer, Admin
DELETE /sponsorship/messages/block/{sponsorId} 🔒 Farmer, Admin
GET    /sponsorship/messages/blocked            🔒 Farmer, Admin
```

---

## Endpoint Count Summary

| Category | Count | Notes |
|----------|-------|-------|
| **Authentication** | 11 | Shared across all roles |
| **Profile Management** | 4 | Sponsor setup & updates |
| **Purchase & Tiers** | 3 | Package purchasing |
| **Code Management** | 4 | Create, retrieve, validate, distribute |
| **Farmer Insights** | 3 | View sponsored farmers & analyses |
| **Analytics** | 10 | Dashboard, statistics, ROI, temporal |
| **Messaging** | 11 | Text, images, voice, edit, delete |
| **Smart Links** | 3 | XL tier exclusive |
| **Logo Display** | 2 | Visibility permissions |
| **Dealer Management** | 13 | Invite, transfer, analytics, self-service |
| **Debug** | 1 | Development utility |
| **TOTAL SPONSOR** | **65** | (11 auth + 54 sponsorship) |
| **Farmer-Only** | 5 | Redeem, block, my-sponsor |

---

## Key Patterns

### URL Structure
```
/api/v{version}/Auth/{endpoint}           → Authentication
/api/v{version}/sponsorship/{endpoint}    → Sponsorship operations
```

### Authorization Levels
- ✅ **Public**: No authentication required (6 endpoints)
- 🔒 **Authenticated**: Any logged-in user (2 endpoints)
- 🔒 **Sponsor, Admin**: Sponsor or Admin role required (46 endpoints)
- 🔒 **Sponsor, Farmer, Admin**: Messaging endpoints (11 endpoints)
- 🔒 **Dealer, Sponsor**: Dealer self-service (2 endpoints)
- 🔒 **Farmer, Admin**: Farmer-only (5 endpoints)
- 🔒 **Admin**: Admin-only (1 endpoint)

### Pagination Limits
- `/sponsorship/codes`: 1-200 items per page
- `/sponsorship/analyses`: 1-100 items per page
- `/sponsorship/dealer/my-codes`: 1-200 items per page

### Cache TTLs
- Dashboard Summary: 15 minutes
- Messaging Analytics: 15 minutes
- Impact Analytics: 6 hours
- Temporal Analytics: 1 hour
- ROI Analytics: 12 hours

---

## Tier-Based Features

| Feature | S | M | L | XL |
|---------|---|---|---|-----|
| Data Access | Basic | Basic | Full | Full |
| Logo Display | Result only | Result + Analysis | All screens | All screens |
| Text Messaging | ❌ | ✅ | ✅ | ✅ |
| Image Attachments | ❌ | ✅ | ✅ | ✅ |
| Voice Messages | ❌ | ❌ | ❌ | ✅ |
| Message Editing | ❌ | ✅ | ✅ | ✅ |
| Smart Links | ❌ | ❌ | ❌ | ✅ |

---

## Mobile App Priority

### Essential (Dashboard)
1. `GET /sponsorship/dashboard-summary` - First screen
2. `GET /sponsorship/analyses` - Farmer data list
3. `GET /sponsorship/messages/conversation` - Messaging
4. `GET /sponsorship/dealer/my-dashboard` - Dealer view

### High Priority (Features)
1. `POST /sponsorship/messages` - Send messages
2. `GET /sponsorship/codes` - View codes
3. `POST /sponsorship/dealer/transfer-codes` - Distribute to dealers
4. `GET /sponsorship/impact-analytics` - Impact metrics

### Medium Priority (Admin)
1. `POST /sponsorship/purchase-package` - Buy packages
2. `GET /sponsorship/dealer/summary` - Dealer overview
3. `POST /sponsorship/dealer/invite` - Invite dealers
4. `GET /sponsorship/roi-analytics` - Financial metrics

---

## Common Query Parameters

### Pagination
```
?page=1&pageSize=50
```

### Date Filtering
```
?startDate=2025-01-01&endDate=2025-12-31
```

### Code Filtering
```
?onlyUnused=true&onlyUnsent=true&excludeDealerTransferred=true
```

### Analysis Filtering
```
?filterByTier=M&filterByCropType=wheat&sortBy=date&sortOrder=desc
```

### Messaging Filtering
```
?hasUnreadMessages=true&filterByMessageStatus=unread
```

---

## Response Formats

### Success
```json
{
  "success": true,
  "message": "Operation successful",
  "data": { ... }
}
```

### Error
```json
{
  "success": false,
  "message": "Error description"
}
```

### Paginated
```json
{
  "success": true,
  "data": {
    "items": [...],
    "page": 1,
    "pageSize": 50,
    "totalCount": 150,
    "totalPages": 3,
    "hasNext": true,
    "hasPrevious": false
  }
}
```
