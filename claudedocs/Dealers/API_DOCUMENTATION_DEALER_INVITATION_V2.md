# Dealer Invitation API Documentation v2.0

**Version**: 2.0
**Date**: 2025-10-30
**Status**: Production Ready
**Migration Required**: Yes (see migration script)

---

## 🎯 Overview

The Dealer Invitation system allows sponsors to invite dealers and transfer sponsorship codes efficiently. Version 2.0 introduces intelligent code selection with optional `packageTier` filtering, eliminating the `purchaseId` requirement and enabling multi-purchase automatic selection.

---

## 🆕 What's New in v2.0

### Key Features
- ✅ **Optional `packageTier` Filter**: S, M, L, XL tier-based filtering
- ✅ **Multi-Purchase Support**: Automatic code selection from multiple purchases
- ✅ **FIFO Intelligent Ordering**: Codes expiring soonest selected first
- ✅ **Code Reservation System**: Prevents double-allocation during pending invitations
- ✅ **Backward Compatible**: Old `purchaseId` parameter still works

### Breaking Changes
- ❌ **NONE** - Fully backward compatible

---

## 📋 API Endpoints

### 1. Invite Dealer via SMS

**Endpoint**: `POST /api/v1/sponsorship/dealer/invite-via-sms`

**Description**: Create dealer invitation and send SMS with deep link. Codes are reserved until invitation accepted/expired.

#### Request Headers
```http
Authorization: Bearer {sponsor_token}
x-dev-arch-version: 1.0
Content-Type: application/json
```

#### Request Body (New - v2.0)
```json
{
  "email": "dealer@example.com",
  "phone": "+905551234567",
  "dealerName": "ABC Tarım",
  "packageTier": "M",        // ✨ NEW: Optional (S, M, L, XL)
  "codeCount": 5
}
```

#### Request Body (Old - v1.0 - Still Works)
```json
{
  "email": "dealer@example.com",
  "phone": "+905551234567",
  "dealerName": "ABC Tarım",
  "purchaseId": 26,          // ✅ Still supported
  "codeCount": 5
}
```

#### Request Body (Combined - Max Precision)
```json
{
  "email": "dealer@example.com",
  "phone": "+905551234567",
  "dealerName": "ABC Tarım",
  "purchaseId": 26,          // ✅ Purchase filter
  "packageTier": "M",        // ✅ + Tier filter
  "codeCount": 5
}
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `email` | string | Yes | Dealer email address |
| `phone` | string | Yes | Dealer phone (+90XXXXXXXXXX format) |
| `dealerName` | string | Yes | Dealer company name |
| `packageTier` | string | **No** | **NEW**: Tier filter (S, M, L, XL) |
| `purchaseId` | integer | **No** | Purchase ID filter (backward compatibility) |
| `codeCount` | integer | Yes | Number of codes to transfer (1-100) |

#### Response Success (200 OK)
```json
{
  "success": true,
  "message": "📱 Bayilik daveti +905551234567 numarasına SMS ile gönderildi",
  "data": {
    "invitationId": 123,
    "invitationToken": "abc123def456...",
    "invitationLink": "https://ziraai.com/ref/DEALER-abc123def456",
    "email": "dealer@example.com",
    "phone": "+905551234567",
    "dealerName": "ABC Tarım",
    "codeCount": 5,
    "status": "Pending",
    "invitationType": "Invite",
    "createdAt": "2025-10-30T10:00:00Z",
    "expiryDate": "2025-11-06T10:00:00Z",
    "smsSent": true,
    "smsDeliveryStatus": "Sent"
  }
}
```

#### Response Error (400 Bad Request)
```json
{
  "success": false,
  "message": "Yetersiz kod (M tier). Mevcut: 3, İstenen: 5"
}
```

#### Business Logic

**Intelligent Code Selection Algorithm:**
```
1. Get all available codes for sponsor:
   WHERE SponsorId = X
   AND IsUsed = false
   AND DealerId IS NULL
   AND ReservedForInvitationId IS NULL
   AND ExpiryDate > NOW

2. Apply purchaseId filter (if specified):
   WHERE SponsorshipPurchaseId = Y

3. Apply packageTier filter (if specified):
   WHERE SubscriptionTierId = TierId(PackageTier)

4. Intelligent ordering:
   ORDER BY ExpiryDate ASC,  -- Codes expiring soonest first (FIFO)
            CreatedDate ASC  -- Oldest first

5. Take requested count:
   TAKE CodeCount

6. Reserve codes:
   SET ReservedForInvitationId = InvitationId
   SET ReservedAt = NOW
```

**SMS Message Template:**
```
Merhaba!

{sponsorName} firması tarafından ZiraAI bayiliği için davet edildiniz.

Davetiyenizi kabul etmek için aşağıdaki linke tıklayın:
{deepLink}

Token: {token}

ZiraAI uygulamasını indirin:
{playStoreLink}
```

---

### 2. Create Dealer Invitation (Invite or AutoCreate)

**Endpoint**: `POST /api/v1/sponsorship/dealer/invite`

**Description**: Create dealer invitation with two modes: "Invite" (link-based) or "AutoCreate" (instant account creation).

#### Request Body (Invite Mode - with packageTier)
```json
{
  "invitationType": "Invite",
  "email": "dealer@example.com",
  "phone": "+905551234567",
  "dealerName": "ABC Tarım",
  "packageTier": "L",        // ✨ NEW: Optional tier filter
  "codeCount": 15
}
```

#### Request Body (AutoCreate Mode)
```json
{
  "invitationType": "AutoCreate",
  "email": "quickdealer@example.com",
  "dealerName": "Quick Dealer LLC",
  "purchaseId": 26,          // ✅ Still works (backward compatible)
  "codeCount": 20
}
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `invitationType` | string | Yes | "Invite" or "AutoCreate" |
| `email` | string | Yes | Dealer email |
| `phone` | string | No | Dealer phone (required for Invite) |
| `dealerName` | string | Yes | Company name |
| `packageTier` | string | **No** | **NEW**: Tier filter (S, M, L, XL) |
| `purchaseId` | integer | **No** | Purchase ID (backward compatibility) |
| `codeCount` | integer | Yes | Number of codes (1-100) |

#### Response Success (Invite Mode)
```json
{
  "success": true,
  "message": "Davetiye dealer@example.com adresine gönderildi",
  "data": {
    "invitationId": 124,
    "invitationToken": "xyz789abc123...",
    "invitationLink": "https://ziraai.com/dealer-invitation?token=xyz789abc123",
    "email": "dealer@example.com",
    "phone": "+905551234567",
    "dealerName": "ABC Tarım",
    "codeCount": 15,
    "status": "Pending",
    "invitationType": "Invite",
    "createdAt": "2025-10-30T11:00:00Z"
  }
}
```

#### Response Success (AutoCreate Mode)
```json
{
  "success": true,
  "message": "Bayi hesabı oluşturuldu. Login: quickdealer@example.com, Şifre: Abc123Xyz789",
  "data": {
    "invitationId": 125,
    "invitationToken": "auto456def...",
    "email": "quickdealer@example.com",
    "dealerName": "Quick Dealer LLC",
    "codeCount": 20,
    "status": "Accepted",
    "invitationType": "AutoCreate",
    "autoCreatedPassword": "Abc123Xyz789",
    "createdDealerId": 200,
    "createdAt": "2025-10-30T12:00:00Z"
  }
}
```

**AutoCreate Workflow:**
1. Create dealer user account
2. Generate random password (12 chars)
3. Assign Sponsor role
4. Transfer codes immediately (no reservation)
5. Return credentials to sponsor

---

### 3. Transfer Codes to Dealer

**Endpoint**: `POST /api/v1/sponsorship/dealer/transfer-codes`

**Description**: Direct code transfer from sponsor to existing dealer (Method A).

#### Request Body (New - with packageTier)
```json
{
  "dealerId": 158,
  "packageTier": "M",        // ✨ NEW: Optional tier filter
  "codeCount": 5
}
```

#### Request Body (Old - with purchaseId)
```json
{
  "dealerId": 158,
  "purchaseId": 26,          // ✅ Still works
  "codeCount": 5
}
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `dealerId` | integer | Yes | Dealer user ID |
| `packageTier` | string | **No** | **NEW**: Tier filter (S, M, L, XL) |
| `purchaseId` | integer | **No** | Purchase ID (backward compatibility) |
| `codeCount` | integer | Yes | Number of codes (1-100) |
| `transferNote` | string | No | Optional transfer note |

#### Response Success
```json
{
  "success": true,
  "message": "Bayiye başarıyla 5 kod aktarıldı",
  "data": {
    "transferredCodeIds": [940, 941, 942, 943, 944],
    "transferredCount": 5,
    "dealerId": 158,
    "dealerName": "User 1113",
    "transferredAt": "2025-10-30T13:00:00Z"
  }
}
```

**Transfer Logic:**
- No reservation (direct transfer)
- Same intelligent selection algorithm as invitations
- Supports tier filtering
- Backward compatible with purchaseId

---

### 4. Accept Dealer Invitation

**Endpoint**: `POST /api/v1/sponsorship/dealer/accept-invitation`

**Description**: Dealer accepts invitation, reserved codes transferred to dealer.

#### Request Body
```json
{
  "invitationToken": "abc123def456..."
}
```

#### Response Success
```json
{
  "success": true,
  "message": "Davetiye kabul edildi. 5 kod hesabınıza aktarıldı",
  "data": {
    "invitationId": 123,
    "dealerId": 158,
    "codesTransferred": 5,
    "dealerName": "ABC Tarım",
    "acceptedAt": "2025-10-30T14:00:00Z"
  }
}
```

**Transfer Logic with Fallback:**
```
1. Find invitation by token
2. Get reserved codes:
   WHERE ReservedForInvitationId = InvitationId

3. If reserved codes insufficient:
   - Get fresh codes using intelligent selection
   - Same priority: Tier → ExpiryDate → CreatedDate

4. Transfer codes to dealer:
   - SET DealerId = dealer userId
   - SET TransferredAt = NOW
   - SET TransferredByUserId = sponsor userId
   - CLEAR ReservedForInvitationId = NULL
   - CLEAR ReservedAt = NULL

5. Update invitation:
   - SET Status = "Accepted"
   - SET AcceptedDate = NOW
```

---

## 🧠 Intelligent Code Selection

### Algorithm Priority
1. **PurchaseId Filter** (if specified) - Backward compatibility
2. **PackageTier Filter** (if specified) - S, M, L, XL
3. **Expiry Date** (ascending) - FIFO, prevent waste
4. **Creation Date** (ascending) - Oldest first

### Example Scenarios

#### Scenario 1: Tier M Only
```json
{
  "packageTier": "M",
  "codeCount": 10
}
```
**Result**: 10 M-tier codes from ANY purchase, expiring soonest first

#### Scenario 2: No Filters (Multi-Purchase)
```json
{
  "codeCount": 20
}
```
**Result**: 20 codes from MULTIPLE purchases, ANY tier, expiring soonest first

#### Scenario 3: Precise Filtering
```json
{
  "purchaseId": 26,
  "packageTier": "M",
  "codeCount": 5
}
```
**Result**: 5 M-tier codes ONLY from purchase #26

---

## 🔒 Code Reservation System

### Purpose
Prevents double-allocation when multiple invitations created simultaneously.

### Database Fields
- **`ReservedForInvitationId`**: Invitation ID holding reservation
- **`ReservedAt`**: Reservation timestamp

### Workflow

**For "Invite" Type:**
```
1. Create invitation
2. Reserve codes:
   UPDATE SponsorshipCodes
   SET ReservedForInvitationId = InvitationId,
       ReservedAt = NOW
   WHERE Id IN (selected code IDs)

3. On acceptance:
   UPDATE SponsorshipCodes
   SET DealerId = dealer userId,
       TransferredAt = NOW,
       ReservedForInvitationId = NULL,
       ReservedAt = NULL
   WHERE ReservedForInvitationId = InvitationId
```

**For "AutoCreate" Type:**
- No reservation (direct transfer)
- Codes immediately assigned to dealer

### Expiry Handling
- Invitations expire after 7 days
- Reserved codes can be manually reclaimed
- Future: Auto-cleanup job for expired reservations

---

## 📊 Package Tiers

| Tier | Daily Limit | Monthly Limit | Price | Use Case |
|------|------------|---------------|-------|----------|
| **S** | 1 | 30 | ₺50 | Small farms, testing |
| **M** | 2 | 50 | ₺100 | Medium farms |
| **L** | 5 | 100 | ₺200 | Large farms |
| **XL** | 10 | 300 | ₺500 | Enterprise, dealers |

---

## 🔄 Migration Guide

### Database Migration Required

**File**: `claudedocs/Dealers/migrations/001_remove_purchaseid_add_packagetier_and_reservation.sql`

**Changes:**
1. `DealerInvitations.PurchaseId` → Nullable
2. `DealerInvitations.PackageTier` → Added (VARCHAR 10)
3. `SponsorshipCodes.ReservedForInvitationId` → Added (INT4)
4. `SponsorshipCodes.ReservedAt` → Added (TIMESTAMP)
5. Foreign key constraint added
6. 3 performance indexes created

**Migration Steps:**
```bash
# 1. Backup database
pg_dump -U postgres ziraai_production > backup_20251030.sql

# 2. Run migration script
psql -U postgres -d ziraai_production -f 001_remove_purchaseid_add_packagetier_and_reservation.sql

# 3. Verify changes
psql -U postgres -d ziraai_production -c "SELECT column_name, data_type FROM information_schema.columns WHERE table_name = 'DealerInvitations';"
```

---

## 🧪 Testing Guide

### Test Collection
**File**: `ZiraAI_Dealer_Invitation_PackageTier_v2.0.postman_collection.json`

### Test Scenarios

#### ✅ Test 1: Tier-Based Invitation
```bash
POST /api/v1/sponsorship/dealer/invite-via-sms
{
  "packageTier": "M",
  "codeCount": 5
}
```
**Expected**: 5 M-tier codes reserved, SMS sent

#### ✅ Test 2: Multi-Purchase Auto-Selection
```bash
POST /api/v1/sponsorship/dealer/invite-via-sms
{
  "codeCount": 10
}
```
**Expected**: 10 codes from multiple purchases, expiring soonest first

#### ✅ Test 3: Backward Compatibility
```bash
POST /api/v1/sponsorship/dealer/transfer-codes
{
  "purchaseId": 26,
  "dealerId": 158,
  "codeCount": 3
}
```
**Expected**: Works exactly like v1.0, only purchase #26 codes

#### ✅ Test 4: Reservation Transfer
```bash
POST /api/v1/sponsorship/dealer/accept-invitation
{
  "invitationToken": "abc123..."
}
```
**Expected**: Reserved codes transferred, reservation cleared

---

## 📝 Error Codes

| Code | Message | Cause | Solution |
|------|---------|-------|----------|
| 400 | "Yetersiz kod" | Not enough codes | Purchase more codes or reduce codeCount |
| 400 | "Geçersiz paket tier" | Invalid tier | Use S, M, L, or XL |
| 404 | "Davetiye bulunamadı" | Invalid token | Check invitation token |
| 400 | "Davetiye süresi doldu" | Invitation expired | Create new invitation |
| 401 | "Yetkisiz erişim" | Invalid/missing token | Login and get new token |

---

## 🚀 Best Practices

### For Sponsors

**✅ DO:**
- Use `packageTier` for flexible filtering
- Let system handle multi-purchase selection
- Reserve codes with invitations for safety
- Monitor invitation expiry dates

**❌ DON'T:**
- Hard-code purchaseId in requests
- Create invitations without checking available codes
- Ignore SMS delivery status

### For Dealers

**✅ DO:**
- Accept invitations within 7 days
- Distribute codes to farmers promptly
- Track code usage analytics

**❌ DON'T:**
- Share invitation links publicly
- Hoard codes without distribution

---

## 🔗 Related Documentation

- [Implementation Summary](./IMPLEMENTATION_SUMMARY_PURCHASEID_REMOVAL.md)
- [Migration Script](./migrations/001_remove_purchaseid_add_packagetier_and_reservation.sql)
- [Postman Collection](./ZiraAI_Dealer_Invitation_PackageTier_v2.0.postman_collection.json)
- [Main API Collection](../../ZiraAI_Complete_API_Collection_v6.1.postman_collection.json)

---

**Version**: 2.0
**Last Updated**: 2025-10-30
**Author**: ZiraAI Backend Team
