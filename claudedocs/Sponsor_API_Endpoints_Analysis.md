# Sponsor API Endpoints Analysis

## Overview
Complete analysis of API endpoints used by Sponsor role in the ZiraAI application.

**Total Sponsorship Endpoints**: 58  
**Authentication Endpoints**: 11 (shared across all roles)  
**Sponsor-Specific**: 54  
**Farmer-Only**: 4 (redeem, block/unblock, my-sponsor, blocked list)

---

## 1. Authentication Endpoints (Used by ALL roles including Sponsor)

### 1.1 Public Authentication
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | `/api/v{version}/Auth/login` | Email/password login | Public |
| POST | `/api/v{version}/Auth/login-phone` | Phone number login | Public |
| POST | `/api/v{version}/Auth/register` | Email registration | Public |
| POST | `/api/v{version}/Auth/register-phone` | Phone registration | Public |
| POST | `/api/v{version}/Auth/refresh-token` | Refresh JWT token | Public |
| POST | `/api/v{version}/Auth/verify` | Verify email/OTP | Public |
| POST | `/api/v{version}/Auth/verify-phone-otp` | Verify phone OTP | Public |
| POST | `/api/v{version}/Auth/verify-phone-register` | Verify phone registration | Public |
| PUT | `/api/v{version}/Auth/forgot-password` | Reset password | Public |

### 1.2 Protected Authentication
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| PUT | `/api/v{version}/Auth/user-password` | Change password | Required |
| POST | `/api/v{version}/Auth/test` | Test authentication | Required |

**Notes:**
- Sponsors use standard authentication flow
- Phone-based auth supported for mobile users
- Password management available after profile creation

---

## 2. Sponsor Profile Management

### 2.1 Profile Setup
| Method | Path | Description | Role | Notes |
|--------|------|-------------|------|-------|
| POST | `/sponsorship/create-profile` | Create sponsor profile (one-time) | Any authenticated user | Transforms user to Sponsor role |
| PUT | `/sponsorship/update-profile` | Update sponsor profile | Sponsor, Admin | Full profile edit |
| GET | `/sponsorship/profile` | Get sponsor profile | Sponsor, Admin | View current profile |
| POST | `/sponsorship/profile` | Create/update profile (legacy) | Sponsor, Admin | Use update-profile instead |

**Profile Fields:**
- Company info: Name, Description, Type, Business Model
- Contact: Email, Phone, Person, Address (City, Country, Postal)
- Branding: Logo URL, Website URL
- Social: LinkedIn, Twitter, Facebook, Instagram
- Legal: Tax Number, Trade Registry Number
- Credentials: Optional password for email+password login

---

## 3. Package Purchase & Code Management

### 3.1 Tier Information
| Method | Path | Description | Role | Notes |
|--------|------|-------------|------|-------|
| GET | `/sponsorship/tiers-for-purchase` | Get available sponsorship tiers | Public | S, M, L, XL tiers (excludes Trial) |

### 3.2 Package Purchase
| Method | Path | Description | Role | Notes |
|--------|------|-------------|------|-------|
| POST | `/sponsorship/purchase-package` | Purchase bulk subscription packages | Sponsor, Admin | Creates codes |
| GET | `/sponsorship/purchases` | Get purchase history | Sponsor, Admin | All purchases |

### 3.3 Code Management
| Method | Path | Description | Role | Query Params |
|--------|------|-------------|------|--------------|
| POST | `/sponsorship/codes` | Create individual code | Sponsor, Admin | Manual code creation |
| GET | `/sponsorship/codes` | Get sponsorship codes | Sponsor, Admin | onlyUnused, onlyUnsent, sentDaysAgo, onlySentExpired, excludeDealerTransferred, page, pageSize |

**Code Filtering:**
- `onlyUnused`: Unused codes (sent or unsent)
- `onlyUnsent`: Never distributed (recommended for new distribution)
- `sentDaysAgo`: Codes sent X days ago but unused
- `onlySentExpired`: Sent codes that expired without use
- `excludeDealerTransferred`: Exclude codes transferred to dealers
- Pagination: `page` (1-∞), `pageSize` (1-200)

### 3.4 Code Validation
| Method | Path | Description | Role | Notes |
|--------|------|-------------|------|-------|
| GET | `/sponsorship/validate/{code}` | Validate code without redeeming | Farmer, Sponsor, Admin | Check validity |

---

## 4. Farmer Management & Insights

### 4.1 Farmer Data Access
| Method | Path | Description | Role | Notes |
|--------|------|-------------|------|-------|
| GET | `/sponsorship/farmers` | Get sponsored farmers | Sponsor, Admin | List of farmers using sponsor's codes |

### 4.2 Analysis Access (Tier-Based)
| Method | Path | Description | Role | Query Params |
|--------|------|-------------|------|--------------|
| GET | `/sponsorship/analyses` | List sponsored analyses | Sponsor, Admin | page, pageSize, sortBy, sortOrder, filterByTier, filterByCropType, startDate, endDate, dealerId, filterByMessageStatus, hasUnreadMessages, hasUnreadForCurrentUser, unreadMessagesMin |
| GET | `/sponsorship/analysis/{plantAnalysisId}` | Get single analysis (tier-filtered) | Sponsor, Admin | Data filtered by tier level |

**Analysis Filters:**
- **Pagination**: `page` (1-∞), `pageSize` (1-100)
- **Sorting**: `sortBy` (date, healthScore, cropType), `sortOrder` (asc, desc)
- **Tier**: `filterByTier` (S, M, L, XL)
- **Crop**: `filterByCropType`
- **Date**: `startDate`, `endDate`
- **Dealer**: `dealerId` (filter by specific dealer)
- **Messages**: `filterByMessageStatus`, `hasUnreadMessages`, `hasUnreadForCurrentUser`, `unreadMessagesMin`

---

## 5. Analytics & Statistics

### 5.1 Core Analytics
| Method | Path | Description | Role | Cache TTL |
|--------|------|-------------|------|-----------|
| GET | `/sponsorship/dashboard-summary` | Mobile dashboard (key metrics) | Sponsor, Admin | 15 min |
| GET | `/sponsorship/statistics` | Sponsorship usage statistics | Sponsor, Admin | - |
| GET | `/sponsorship/package-statistics` | Package distribution breakdown | Sponsor, Admin | - |
| GET | `/sponsorship/code-analysis-statistics` | Code-level analysis stats | Sponsor, Admin | - |

**Query Params (code-analysis-statistics):**
- `includeAnalysisDetails` (default: true)
- `topCodesCount` (default: 10)

### 5.2 Advanced Analytics
| Method | Path | Description | Role | Query Params | Cache TTL |
|--------|------|-------------|------|--------------|-----------|
| GET | `/sponsorship/messaging-analytics` | Message volumes, response metrics | Sponsor, Admin | startDate, endDate | 15 min |
| GET | `/sponsorship/impact-analytics` | Farmer reach, agricultural impact | Sponsor, Admin | - | 6 hours |
| GET | `/sponsorship/temporal-analytics` | Time-series trends | Sponsor, Admin | startDate, endDate, groupBy | 1 hour |
| GET | `/sponsorship/roi-analytics` | ROI and efficiency metrics | Sponsor, Admin | - | 12 hours |
| GET | `/sponsorship/link-statistics` | Link usage performance | Sponsor, Admin | startDate, endDate | - |

**Temporal Analytics Grouping:**
- `Day` (default), `Week`, `Month`

---

## 6. Messaging (Tier-Based)

### 6.1 Basic Messaging (M, L, XL tiers)
| Method | Path | Description | Role | Notes |
|--------|------|-------------|------|-------|
| POST | `/sponsorship/messages` | Send text message | Sponsor, Farmer, Admin | M+ tier required |
| GET | `/sponsorship/messages/conversation` | Get conversation | Sponsor, Farmer, Admin | otherUserId, plantAnalysisId, page, pageSize |
| PATCH | `/sponsorship/messages/{messageId}/read` | Mark single message read | Sponsor, Farmer, Admin | - |
| PATCH | `/sponsorship/messages/read` | Mark multiple messages read | Sponsor, Farmer, Admin | Bulk operation |

### 6.2 Advanced Messaging Features
| Method | Path | Description | Role | Tier | Notes |
|--------|------|-------------|------|------|-------|
| POST | `/sponsorship/messages/attachments` | Send with images/files | Sponsor, Farmer, Admin | M+ | Multipart form |
| POST | `/sponsorship/messages/voice` | Send voice message | Sponsor, Farmer, Admin | XL | Multipart form |
| PUT | `/sponsorship/messages/{messageId}` | Edit message | Sponsor, Farmer, Admin | M+ | 1 hour limit |
| DELETE | `/sponsorship/messages/{messageId}` | Delete message | Sponsor, Farmer, Admin | All | 24 hour limit |
| POST | `/sponsorship/messages/{messageId}/forward` | Forward message | Sponsor, Farmer, Admin | M+ | - |

### 6.3 Messaging Configuration
| Method | Path | Description | Role | Notes |
|--------|------|-------------|------|-------|
| GET | `/sponsorship/messaging/features` | Get feature availability | Authenticated | plantAnalysisId required |
| PATCH | `/sponsorship/admin/messaging/features/{featureId}` | Toggle feature | Admin only | Admin control |

---

## 7. Smart Links (XL Tier Only)

| Method | Path | Description | Role | Notes |
|--------|------|-------------|------|-------|
| POST | `/sponsorship/smart-links` | Create smart link | Sponsor, Admin | XL tier exclusive |
| GET | `/sponsorship/smart-links` | Get sponsor's smart links | Sponsor, Admin | - |
| GET | `/sponsorship/smart-links/performance` | Smart link analytics | Sponsor, Admin | - |

**Features:**
- Custom URL slugs
- Link tracking
- Performance metrics
- Click analytics

---

## 8. Link Distribution

| Method | Path | Description | Role | Notes |
|--------|------|-------------|------|-------|
| POST | `/sponsorship/send-link` | Send links via SMS/WhatsApp | Sponsor, Admin | Bulk sending |

**Request Body:**
- `Recipients`: List of phone numbers
- `Channel`: SMS or WhatsApp
- `SponsorId`: Auto-filled from JWT

---

## 9. Logo & Branding Display

### 9.1 Visibility Permissions (Database-Driven)
| Method | Path | Description | Role | Query Params |
|--------|------|-------------|------|--------------|
| GET | `/sponsorship/logo-permissions/analysis/{plantAnalysisId}` | Check logo display permission | Authenticated | screen (start, result, analysis, profile) |
| GET | `/sponsorship/display-info/analysis/{plantAnalysisId}` | Get sponsor info for display | Authenticated | screen (start, result, analysis, profile) |

**Screens:**
- `start`: Start screen visibility
- `result`: Result screen visibility
- `analysis`: Analysis detail visibility
- `profile`: Farmer profile visibility

**Logic:**
- Visibility based on redeemed code's tier
- Database-driven via `sponsor_visibility` feature flags
- Returns sponsor logo, name, website if allowed

---

## 10. Dealer Management (Sub-Sponsor Distribution)

### 10.1 Dealer Invitations
| Method | Path | Description | Role | Notes |
|--------|------|-------------|------|-------|
| POST | `/sponsorship/dealer/invite` | Create invitation (Invite/AutoCreate) | Sponsor, Admin | Email invitation |
| POST | `/sponsorship/dealer/invite-via-sms` | Send invitation via SMS | Sponsor, Admin | Mobile-optimized |
| GET | `/sponsorship/dealer/invitation-details` | Get invitation details | Public | token query param |
| POST | `/sponsorship/dealer/accept-invitation` | Accept invitation | Authenticated | Mobile endpoint |
| GET | `/sponsorship/dealer/invitations` | List invitations | Sponsor, Admin | status filter (Pending, Accepted, Expired, Cancelled) |
| GET | `/sponsorship/dealer/invitations/my-pending` | Get user's pending invitations | Dealer, Farmer, Sponsor | Auto-detect by email/phone |

**Invitation Types:**
- **Invite**: Send link to existing sponsor
- **AutoCreate**: Create new sponsor with auto-password

### 10.2 Code Transfer
| Method | Path | Description | Role | Notes |
|--------|------|-------------|------|-------|
| POST | `/sponsorship/dealer/transfer-codes` | Transfer codes to dealer | Sponsor, Admin | From sponsor to dealer |
| POST | `/sponsorship/dealer/reclaim-codes` | Reclaim unused codes | Sponsor, Admin | From dealer back to sponsor |

**Transfer Request:**
- `DealerId`: Target dealer user ID
- `PurchaseId`: Source purchase ID
- `CodeCount`: Number of codes to transfer

### 10.3 Dealer Search
| Method | Path | Description | Role | Notes |
|--------|------|-------------|------|-------|
| GET | `/sponsorship/dealer/search` | Search dealer by email | Sponsor, Admin | Manual search (Method A) |

### 10.4 Dealer Analytics
| Method | Path | Description | Role | Notes |
|--------|------|-------------|------|-------|
| GET | `/sponsorship/dealer/analytics/{dealerId}` | Get dealer performance | Sponsor, Admin | Codes received/sent/used |
| GET | `/sponsorship/dealer/summary` | Get all dealers summary | Sponsor, Admin | Aggregated statistics |

### 10.5 Dealer Self-Service (Dealer Role)
| Method | Path | Description | Role | Query Params |
|--------|------|-------------|------|--------------|
| GET | `/sponsorship/dealer/my-codes` | Get dealer's codes | Dealer, Sponsor | page, pageSize, onlyUnsent |
| GET | `/sponsorship/dealer/my-dashboard` | Dealer dashboard summary | Dealer, Sponsor | Quick stats |

**Dealer Dashboard Metrics:**
- Total codes received
- Available codes (unsent)
- Codes sent to farmers
- Codes redeemed/used
- Pending invitations count

---

## 11. Farmer-Only Endpoints (NOT for Sponsor)

| Method | Path | Description | Role | Notes |
|--------|------|-------------|------|-------|
| POST | `/sponsorship/redeem` | Redeem sponsorship code | Farmer, Admin | Creates subscription |
| GET | `/sponsorship/my-sponsor` | Get user's sponsor info | Farmer, Admin | Coming soon |
| POST | `/sponsorship/messages/block` | Block sponsor from messaging | Farmer, Admin | Farmer blocks sponsor |
| DELETE | `/sponsorship/messages/block/{sponsorId}` | Unblock sponsor | Farmer, Admin | Farmer unblocks sponsor |
| GET | `/sponsorship/messages/blocked` | Get blocked sponsors list | Farmer, Admin | Farmer's blocked list |

---

## 12. Debug & Utility

| Method | Path | Description | Role | Notes |
|--------|------|-------------|------|-------|
| GET | `/sponsorship/debug/user-info` | Get user roles and claims | Authenticated | Development/troubleshooting |

---

## Summary by Category

### Sponsor-Specific Operations (54 endpoints)
1. **Profile Management**: 4 endpoints
2. **Package Purchase**: 3 endpoints
3. **Code Management**: 3 endpoints
4. **Farmer Insights**: 2 endpoints
5. **Analytics**: 10 endpoints
6. **Messaging**: 11 endpoints
7. **Smart Links**: 3 endpoints (XL tier)
8. **Link Distribution**: 1 endpoint
9. **Logo Display**: 2 endpoints
10. **Dealer Management**: 14 endpoints
11. **Dealer Self-Service**: 2 endpoints
12. **Debug**: 1 endpoint

### Shared Operations
- **Authentication**: 11 endpoints (all roles)
- **Code Validation**: 1 endpoint (Farmer/Sponsor/Admin)

### NOT Used by Sponsor
- **Code Redemption**: 1 endpoint (Farmer-only)
- **Sponsor Blocking**: 3 endpoints (Farmer-only)
- **My Sponsor Info**: 1 endpoint (Farmer-only)

---

## Tier-Based Feature Matrix

| Feature | S Tier | M Tier | L Tier | XL Tier |
|---------|--------|--------|--------|---------|
| Data Access | Basic | Basic | Full | Full |
| Logo Display | Result only | Result + Analysis | All screens | All screens |
| Messaging | ❌ | ✅ Text only | ✅ Text + Images | ✅ Text + Images + Voice |
| Message Editing | ❌ | ✅ (1hr) | ✅ (1hr) | ✅ (1hr) |
| Message Forwarding | ❌ | ✅ | ✅ | ✅ |
| Smart Links | ❌ | ❌ | ❌ | ✅ |
| Profile Visibility | Minimal | Medium | High | Full |

---

## Authentication Flow for Sponsors

### Initial Setup
1. User registers via `/Auth/register` or `/Auth/register-phone`
2. User logs in via `/Auth/login` or `/Auth/login-phone`
3. User creates sponsor profile via `/sponsorship/create-profile`
   - Sets company details
   - Optional: Sets password for email+password login
   - System assigns "Sponsor" role

### Subsequent Access
1. Login via email+password or phone+OTP
2. JWT token contains:
   - `UserId` (NameIdentifier claim)
   - `Roles` (Role claims - includes "Sponsor")
   - `Email` (Email claim)
   - `Phone` (MobilePhone claim)
3. All sponsor endpoints validate role via `[Authorize(Roles = "Sponsor,Admin")]`

---

## Mobile App Considerations

### Offline-First Endpoints
- `/sponsorship/dashboard-summary` - Cached 15 min
- `/sponsorship/dealer/my-dashboard` - Quick stats

### Pagination Support
- `/sponsorship/codes` - Max 200 per page
- `/sponsorship/analyses` - Max 100 per page
- `/sponsorship/dealer/my-codes` - Max 200 per page

### Deep Link Support
- Dealer invitations: `/dealer/invitation-details?token={token}`
- Code redemption: Handled by `RedemptionController` (not in Sponsorship)

---

## Error Handling

### Common Error Responses
- `401 Unauthorized`: Missing or invalid JWT token
- `403 Forbidden`: Valid token but insufficient permissions
- `400 Bad Request`: Validation errors, invalid parameters
- `404 Not Found`: Resource doesn't exist
- `500 Internal Server Error`: Server-side errors

### Role-Based Errors
- Farmer accessing `/sponsorship/purchase-package` → 403
- Sponsor accessing `/sponsorship/redeem` → 403
- Tier-based feature access → 400 with message "Feature requires M tier or higher"

---

## Rate Limiting & Caching

### Cache TTLs
- Dashboard Summary: 15 minutes
- Messaging Analytics: 15 minutes
- Impact Analytics: 6 hours
- Temporal Analytics: 1 hour
- ROI Analytics: 12 hours

### Performance Optimizations
- Pagination required for large datasets
- Database-driven configuration (15 min cache)
- Eager loading for related entities
- Index-optimized queries for dealer operations

---

## Security Considerations

1. **Role-Based Access**: All endpoints validate `Sponsor` or `Admin` role
2. **User Ownership**: UserId extracted from JWT claims (cannot be spoofed)
3. **Code Ownership**: Codes validated against SponsorId
4. **Dealer Permissions**: Transfer/reclaim operations validate sponsor ownership
5. **Message Access**: Conversation endpoints verify participant authorization
6. **Logo Display**: Database-driven feature flags prevent unauthorized access

---

## Notes

- All endpoints use API versioning: `/api/v{version}/sponsorship/...`
- JWT authentication required for all non-public endpoints
- User ID extracted from `ClaimTypes.NameIdentifier`
- Sponsor role assigned after profile creation
- Database-driven feature flags for logo visibility
- Tier-based data filtering for farmer analyses
- Dealer system enables hierarchical code distribution
