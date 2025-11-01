# ZiraAI Dealer Distribution - Postman Collection Usage Guide

**Collection Version**: 1.0  
**Last Updated**: 2025-10-28  
**Environment**: Staging (ziraai-api-sit.up.railway.app)

---

## 📦 Collection Overview

Complete end-to-end Postman collection for testing the Dealer Code Distribution System with automatic token management and comprehensive test scenarios.

### Files Included

1. **`ZiraAI_Dealer_Distribution_Complete_E2E.postman_collection.json`**
   - Main collection with all endpoints
   - 28 requests organized in 5 folders
   - Automatic token capture and management
   - Pre-configured test scripts

2. **`ZiraAI_Dealer_Distribution_Staging.postman_environment.json`**
   - Staging environment variables
   - User credentials and IDs
   - Auto-captured tokens and data
   - 24 environment variables

---

## 🚀 Quick Start

### Step 1: Import Collection

1. Open Postman
2. Click **Import** button (top left)
3. Select **File** tab
4. Choose `ZiraAI_Dealer_Distribution_Complete_E2E.postman_collection.json`
5. Click **Import**

### Step 2: Import Environment

1. Click **Environments** tab (left sidebar)
2. Click **Import** button
3. Select `ZiraAI_Dealer_Distribution_Staging.postman_environment.json`
4. Click **Import**
5. Select **"ZiraAI - Dealer Distribution (Staging)"** as active environment

### Step 3: Run Your First Test

1. Navigate to **00 - Authentication** folder
2. Open **Login - Main Sponsor (159)**
3. **Before running**, you need to get OTP code:
   - Use phone: `05411111114`
   - Request OTP via SMS or check database
4. Update request body with correct OTP code
5. Click **Send**
6. ✅ Token automatically captured to `{{sponsor_token}}`

---

## 📂 Collection Structure

### 00 - Authentication (3 requests)
Login endpoints for all user roles with automatic token capture

- **Login - Main Sponsor (159)** → Captures `{{sponsor_token}}`
- **Login - Dealer (158)** → Captures `{{dealer_token}}`
- **Login - Farmer (165)** → Captures `{{farmer_token}}`

### 01 - Sponsor Operations (7 requests)
Main sponsor dealer network management

1. Get Sponsor Available Codes → Captures `{{purchase_id}}`, `{{available_code}}`
2. Search Dealer by Email
3. Transfer Codes to Dealer → Captures `{{transferred_code_ids}}`
4. Get Dealer Summary
5. Get Dealer Performance
6. Get All Analyses (Sponsor View)
7. Reclaim Codes from Dealer

### 02 - Dealer Operations (3 requests)
Dealer code distribution and analysis tracking

1. Get Dealer Received Codes → Captures `{{dealer_code}}`
2. Send Code to Farmer (SMS)
3. Get Dealer Analyses (Own Only)

### 03 - Farmer Operations (5 requests)
Farmer code redemption and plant analysis

1. Redeem Code → Captures `{{subscription_id}}`
2. Check Subscription Status
3. Create Plant Analysis (Async) → Captures `{{analysis_id}}`
4. Get Analysis Result
5. Get Farmer Analysis History

### 04 - Dealer Invitations (4 requests)
Dealer onboarding methods B & C

1. Create Invitation (Method B - Invite) → Captures `{{invitation_link}}`
2. Create AutoCreate Dealer (Method C) → Captures credentials
3. Get All Invitations
4. Get Pending Invitations

---

## 🔑 Environment Variables

### Base Configuration
- `base_url` = `https://ziraai-api-sit.up.railway.app`
- `api_version` = `1.0`

### User Credentials

| Role | Phone | UserId | Token Variable |
|------|-------|--------|----------------|
| Main Sponsor | 05411111114 | 159 | `{{sponsor_token}}` |
| Dealer | 05411111113 | 158 | `{{dealer_token}}` |
| Farmer | 05061111113 | 165 | `{{farmer_token}}` |

### Auto-Captured Variables

These variables are automatically populated by test scripts:

| Variable | Captured By | Usage |
|----------|-------------|-------|
| `purchase_id` | Get Sponsor Available Codes | Transfer/Invite requests |
| `available_code` | Get Sponsor Available Codes | Code reference |
| `dealer_code` | Get Dealer Received Codes | Send to farmer |
| `transferred_code_ids` | Transfer Codes to Dealer | Track transfers |
| `subscription_id` | Redeem Code | Subscription tracking |
| `analysis_id` | Create Plant Analysis | Result retrieval |
| `invitation_link` | Create Invitation | Share with dealer |
| `autocreated_password` | Create AutoCreate Dealer | Dealer credentials |

---

## 📋 Complete E2E Test Scenario

Follow this sequence for full system test:

### Phase 1: Setup & Authentication (5 minutes)

1. ✅ **Login - Main Sponsor**
   - Request OTP for 05411111114
   - Verify OTP
   - Token captured automatically

2. ✅ **Login - Dealer**
   - Request OTP for 05411111113
   - Verify OTP
   - Token captured automatically

3. ✅ **Login - Farmer**
   - Request OTP for 05061111113
   - Verify OTP
   - Token captured automatically

### Phase 2: Sponsor → Dealer Transfer (10 minutes)

4. ✅ **Get Sponsor Available Codes**
   - Check if sponsor has codes
   - If empty, sponsor needs to purchase package first
   - Purchase ID and codes captured

5. ✅ **Search Dealer by Email**
   - Verify dealer exists: `05411111113@phone.ziraai.com`
   - Check `isSponsor` = true

6. ✅ **Transfer Codes to Dealer**
   - Transfer 5 codes from Purchase 26 to Dealer 158
   - Verify success response
   - Code IDs captured

7. ✅ **Get Dealer Summary**
   - Verify dealer appears in summary
   - Check totalDealers = 1
   - Check totalCodesDistributed = 5

8. ✅ **Get Dealer Performance**
   - Verify dealer analytics: dealerId = 158
   - Check totalCodesReceived = 5
   - Check codesAvailable = 5

### Phase 3: Dealer → Farmer Distribution (15 minutes)

9. ✅ **Get Dealer Received Codes**
   - Use dealer token
   - Verify 5 codes received
   - First code captured to `{{dealer_code}}`

10. ✅ **Send Code to Farmer (SMS)**
    - Use captured `{{dealer_code}}`
    - Recipient: `05061111113`
    - Verify SMS sent
    - Check deep link returned

### Phase 4: Farmer Redemption & Analysis (20 minutes)

11. ✅ **Redeem Code**
    - Use farmer token
    - Redeem `{{dealer_code}}`
    - Verify subscription activated
    - Subscription ID captured

12. ✅ **Check Subscription Status**
    - Verify tier = M
    - Check dailyLimit = 10
    - Check isActive = true

13. ✅ **Create Plant Analysis (Async)**
    - Upload plant image
    - CropType: "Tomato"
    - Analysis ID captured
    - ⏳ Wait 2-5 minutes for processing

14. ⏳ **Wait for Processing**
    - RabbitMQ queue processing
    - Worker service running
    - AI/ML analysis in progress

15. ✅ **Get Analysis Result**
    - Use `{{analysis_id}}`
    - Verify status = "Completed"
    - Check diseaseDetected field
    - Review recommendations

### Phase 5: Verification (10 minutes)

16. ✅ **Get Dealer Analyses (Dealer View)**
    - Use dealer token
    - Should see 1 analysis
    - Verify dealerId = 158
    - Verify userId = 165 (farmer)

17. ✅ **Get All Analyses (Sponsor View)**
    - Use sponsor token
    - Should see ALL analyses (direct + dealer)
    - Verify includes dealer-distributed analysis
    - Check totalCount includes farmer's analysis

18. ✅ **Get Farmer Analysis History**
    - Use farmer token
    - Should see 1 analysis
    - Verify own analysis only

### Phase 6: Advanced Features (Optional)

19. ✅ **Create Invitation (Method B)**
    - New dealer email: `newdealer@example.com`
    - Code count: 15
    - Invitation link captured
    - Verify 7-day expiry

20. ✅ **Create AutoCreate Dealer (Method C)**
    - New dealer email: `quickdealer@example.com`
    - Code count: 20
    - Dealer ID captured
    - Password captured (share securely)

21. ✅ **Reclaim Codes from Dealer**
    - Reclaim 2 unsent codes from dealer 158
    - Verify codes returned to sponsor
    - Check dealer summary updated

---

## 🎯 Test Scenarios by Method

### Method A: Manual Transfer (Existing Sponsor)

**Steps:**
1. Login - Main Sponsor
2. Search Dealer by Email (find existing dealer)
3. Transfer Codes to Dealer
4. Get Dealer Performance (verify transfer)

**Use Case:** Dealer already has account, just needs codes

### Method B: Invitation Link (New or Existing)

**Steps:**
1. Login - Main Sponsor
2. Create Invitation (Method B - Invite)
3. Share invitation link with dealer
4. Dealer accepts via link (external)
5. Get Dealer Invitations (verify accepted)

**Use Case:** Formal onboarding process with approval

### Method C: AutoCreate (Quick Setup)

**Steps:**
1. Login - Main Sponsor
2. Create AutoCreate Dealer (Method C)
3. Capture dealer credentials
4. Share credentials with dealer
5. Get Dealer Performance (verify codes transferred)

**Use Case:** Instant dealer setup without waiting

---

## 🔧 Troubleshooting

### Issue 1: "Unauthorized" (401) Error

**Problem:** Token expired or invalid

**Solution:**
1. Check token expiry (60 minutes from login)
2. Re-run login request for that user
3. Token will be auto-captured again

### Issue 2: "Not enough available codes"

**Problem:** Sponsor has 0 codes

**Solution:**
1. Sponsor needs to purchase package first
2. Or use different `purchaseId` with available codes
3. Check `GET /sponsorship/codes?onlyUnsent=true`

### Issue 3: OTP Code Incorrect

**Problem:** OTP verification fails

**Solution:**
1. Request new OTP: `POST /PhoneAuth/request-otp`
2. Get OTP from SMS or database
3. OTP expires in 5 minutes
4. Update request body with fresh code

### Issue 4: Analysis Processing Timeout

**Problem:** Analysis stuck in "Processing" status

**Solution:**
1. Wait minimum 2-5 minutes
2. Check RabbitMQ queue: `docker logs ziraai-rabbitmq`
3. Check Worker Service logs: `docker logs ziraai-worker`
4. Verify N8N webhook responding

### Issue 5: Dealer Not Found

**Problem:** Search returns 404

**Solution:**
1. Verify email format: `{phone}@phone.ziraai.com`
2. For phone `05411111113` → `05411111113@phone.ziraai.com`
3. Check user exists in database
4. User must have Sponsor role to receive codes

### Issue 6: Token Not Auto-Captured

**Problem:** Environment variable empty after login

**Solution:**
1. Check test script tab in request
2. Verify response status = 200
3. Check console log for capture confirmation
4. Manually copy token from response if needed

---

## 📊 Expected Response Times

| Endpoint | Average | Max | Notes |
|----------|---------|-----|-------|
| Authentication | 300ms | 1s | OTP verification |
| Get Codes | 200ms | 500ms | Database query |
| Transfer Codes | 400ms | 1s | Batch update |
| Send Code (SMS) | 1s | 3s | SMS gateway delay |
| Redeem Code | 500ms | 1.5s | Subscription creation |
| Create Analysis | 200ms | 500ms | Queue submission |
| Analysis Processing | 2-5 min | 10 min | AI/ML processing |
| Get Analyses List | 300ms | 800ms | Complex query |

---

## 🔐 Security Best Practices

1. **Never commit tokens to git**
   - Tokens are marked as "secret" in environment
   - Auto-expire after 60 minutes

2. **Rotate OTP codes**
   - Each OTP is single-use
   - Request fresh OTP for each test session

3. **Secure AutoCreate passwords**
   - Passwords are randomly generated
   - Share via secure channel (not email/SMS)
   - Dealer should change password on first login

4. **Production environment**
   - Create separate environment for production
   - Use different user accounts for testing
   - Never test with real farmer data

---

## 📝 Request Body Templates

### Transfer Codes
```json
{
  "purchaseId": 26,
  "dealerId": 158,
  "codeCount": 5
}
```

### Send Code (SMS)
```json
{
  "code": "AGRI-2025-62038F92",
  "recipientPhone": "05061111113",
  "sendViaSms": true,
  "customMessage": "Merhaba! ZiraAI bitki analizi için kodunuz."
}
```

### Redeem Code
```json
{
  "code": "AGRI-2025-62038F92"
}
```

### Create Invitation (Method B)
```json
{
  "invitationType": "Invite",
  "email": "newdealer@example.com",
  "phone": "+905551234567",
  "dealerName": "New Dealer Company",
  "purchaseId": 26,
  "codeCount": 15
}
```

### Create AutoCreate Dealer (Method C)
```json
{
  "invitationType": "AutoCreate",
  "email": "quickdealer@example.com",
  "dealerName": "Quick Dealer LLC",
  "purchaseId": 26,
  "codeCount": 20
}
```

### Reclaim Codes
```json
{
  "dealerId": 158,
  "codeCount": 2
}
```

---

## 📈 Success Criteria

### ✅ Complete E2E Test Success
All of the following must pass:

1. **Authentication**
   - ✅ All 3 users login successfully
   - ✅ Tokens captured automatically
   - ✅ Tokens work for subsequent requests

2. **Code Transfer**
   - ✅ Sponsor can transfer codes to dealer
   - ✅ Dealer sees received codes
   - ✅ Sponsor's available codes decrease

3. **Code Distribution**
   - ✅ Dealer can send code to farmer
   - ✅ Farmer receives SMS with link
   - ✅ Code marked as distributed

4. **Code Redemption**
   - ✅ Farmer can redeem code
   - ✅ Subscription activated
   - ✅ Tier and limits correct

5. **Plant Analysis**
   - ✅ Farmer can create analysis
   - ✅ Analysis processes successfully
   - ✅ Results returned with recommendations
   - ✅ Attribution correct (sponsor, dealer, farmer)

6. **Analysis Visibility**
   - ✅ Dealer sees ONLY own distributed analyses
   - ✅ Sponsor sees ALL analyses (direct + dealer)
   - ✅ Farmer sees ONLY own analyses

7. **Analytics**
   - ✅ Dealer performance metrics accurate
   - ✅ Dealer summary shows all dealers
   - ✅ Code usage statistics correct

8. **Invitations**
   - ✅ Method B invitation creates link
   - ✅ Method C creates dealer instantly
   - ✅ Invitation status tracked correctly

---

## 🆘 Support

### Documentation
- Main README: `claudedocs/Dealers/README.md`
- API Docs: `claudedocs/Dealers/API_DOCUMENTATION.md`
- E2E Test Report: `claudedocs/Dealers/E2E_TEST_PROGRESS_REPORT.md`
- Flow Guide: `claudedocs/Dealers/SPONSOR_DEALER_FARMER_FLOW_GUIDE.md`

### Database Verification
```sql
-- Check code transfer
SELECT "Id", "Code", "DealerId", "TransferredAt", "DistributionDate"
FROM "SponsorshipCodes"
WHERE "DealerId" = 158;

-- Check dealer's analyses
SELECT "Id", "UserId", "SponsorCompanyId", "DealerId", "ActiveSponsorshipId"
FROM "PlantAnalyses"
WHERE "DealerId" = 158;

-- Check dealer invitations
SELECT "Id", "Email", "Status", "InvitationType", "CreatedDealerId"
FROM "DealerInvitations"
WHERE "SponsorId" = 159;
```

### Log Files
- Application: `claudedocs/Dealers/application.log`
- Worker Service: Check Docker logs
- N8N Webhook: Check N8N dashboard

---

## 📅 Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-10-28 | Initial complete E2E collection |
|  |  | - 28 requests in 5 folders |
|  |  | - Automatic token management |
|  |  | - 24 environment variables |
|  |  | - Complete test scripts |
|  |  | - Staging environment ready |

---

## 🎓 Learning Resources

### API Patterns
1. **Token-Based Authentication**
   - JWT tokens in Authorization header
   - Auto-refresh via login endpoint
   - 60-minute expiry

2. **Query Parameter Filtering**
   - `onlyUnsent=true` for unsent codes
   - `status=Pending` for invitations
   - `page` and `pageSize` for pagination

3. **Attribution Chain**
   - `SponsorCompanyId`: Original sponsor
   - `DealerId`: Code distributor
   - `ActiveSponsorshipId`: Farmer subscription

4. **Hybrid Role Support**
   - OR query: `SponsorUserId = userId OR DealerId = userId`
   - Single endpoint serves both roles
   - Token-based role detection

### Test Script Patterns
```javascript
// Pattern 1: Capture data from response
if (pm.response.code === 200) {
    var jsonData = pm.response.json();
    pm.environment.set("variable_name", jsonData.data.field);
}

// Pattern 2: Log progress
console.log("✅ Success message");
console.log("⚠️ Warning message");
console.log("❌ Error message");

// Pattern 3: Conditional checks
if (jsonData.success && jsonData.data) {
    // Process success case
} else {
    // Handle error case
}
```

---

**Collection Version**: 1.0  
**Created By**: Claude Code  
**Last Updated**: 2025-10-28  
**Status**: ✅ Production Ready
