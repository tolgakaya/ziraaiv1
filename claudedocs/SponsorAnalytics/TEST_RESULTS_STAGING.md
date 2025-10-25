# Sponsor Analytics API - Staging Test Results

**Test Date:** 2025-01-25
**Environment:** Staging (https://ziraai-api-sit.up.railway.app)
**Test User:** 05411111114 (User 1114)
**User Roles:** Farmer, Sponsor
**User ID:** 159

---

## Authentication

### Request
```http
POST /api/v1/Auth/login-phone
Content-Type: application/json

{
  "mobilePhone": "05411111114"
}
```

### Response
```json
{
  "data": {
    "status": "Ok",
    "message": "SendMobileCode649953"
  },
  "success": true
}
```

### Verify OTP

**Request:**
```http
POST /api/v1/Auth/verify-phone-otp
Content-Type: application/json

{
  "mobilePhone": "05411111114",
  "code": "649953"
}
```

**Response:**
```json
{
  "data": {
    "provider": "Phone",
    "token": "eyJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjE1OSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJVc2VyIDExMTQiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOlsiRmFybWVyIiwiU3BvbnNvciJdLCJuYmYiOjE3NjE0MjEzNjgsImV4cCI6MTc2MTQyNDk2OCwiaXNzIjoiWmlyYUFJX1N0YWdpbmciLCJhdWQiOiJaaXJhQUlfU3RhZ2luZ19Vc2VycyJ9.lMVIzbHEoyCIaSI-JdzyrjdwAclbHI5fWRZiAcW_16s",
    "expiration": "2025-10-25T20:42:48.4020625+00:00",
    "refreshToken": "8xsrQJcWzrbQy2QNg2saa0gueuanhY4F9Ufw2VbhJJY="
  },
  "success": true,
  "message": "SuccessfulLogin"
}
```

**Token Expiry:** 2025-10-25T20:42:48 UTC (1 hour)

---

## Test Scenario 1: Dashboard Summary (Kategori 1)

### Request
```http
GET /api/v1/sponsorship/dashboard-summary
Authorization: Bearer eyJhbGci...
x-dev-arch-version: 1.0
```

### Response
```json
{
  "data": {
    "totalCodesCount": 560,
    "sentCodesCount": 14,
    "sentCodesPercentage": 2.50,
    "totalAnalysesCount": 6,
    "purchasesCount": 9,
    "totalSpent": 31199.10,
    "currency": "TRY",
    "activePackages": [
      {
        "tierName": "M",
        "tierDisplayName": "Medium",
        "totalCodes": 505,
        "sentCodes": 8,
        "unsentCodes": 497,
        "usedCodes": 0,
        "unusedSentCodes": 8,
        "remainingCodes": 497,
        "usagePercentage": 0,
        "distributionPercentage": 1.58,
        "uniqueFarmers": 0,
        "analysesCount": 0
      },
      {
        "tierName": "L",
        "tierDisplayName": "Large",
        "totalCodes": 55,
        "sentCodes": 6,
        "unsentCodes": 49,
        "usedCodes": 8,
        "unusedSentCodes": 6,
        "remainingCodes": 49,
        "usagePercentage": 133.33,
        "distributionPercentage": 10.91,
        "uniqueFarmers": 6,
        "analysesCount": 6
      }
    ],
    "overallStats": {
      "smsDistributions": 14,
      "whatsAppDistributions": 0,
      "overallRedemptionRate": 57.14,
      "averageRedemptionTime": 0,
      "totalUniqueFarmers": 6,
      "lastPurchaseDate": "2025-10-23T19:01:47.296658",
      "lastDistributionDate": "2025-10-23T07:40:30.211252"
    }
  },
  "success": true,
  "message": "Dashboard summary retrieved successfully"
}
```

**Status:** ✅ PASS
**Response Time:** < 500ms
**Cache:** 24 hours

**Key Metrics:**
- Total Codes: 560
- Sent Codes: 14 (2.50%)
- Total Analyses: 6
- Total Investment: 31,199.10 TRY
- Purchases: 9
- Unique Farmers: 6
- Redemption Rate: 57.14%

**Observations:**
- User has 2 active tiers: Medium (505 codes) and Large (55 codes)
- Low distribution rate (2.50%) - most codes not yet sent
- Medium tier: 0% usage rate (8 codes sent, 0 redeemed)
- Large tier: 133.33% usage rate (unusual - needs investigation: 8 codes redeemed from 6 sent)
- Good redemption rate overall (57.14%)

---

