# ZiraAI Dealer Distribution - Postman Collection

**Version**: 1.0  
**Created**: 2025-10-28  
**Environment**: Staging  
**Status**: ✅ Production Ready

---

## 📦 What's Included

This package contains a complete Postman collection for end-to-end testing of the Dealer Code Distribution System.

### Files

1. **`ZiraAI_Dealer_Distribution_Complete_E2E.postman_collection.json`** (89 KB)
   - 28 pre-configured API requests
   - Organized in 5 logical folders
   - Automatic token capture and management
   - Built-in test scripts with console logging

2. **`ZiraAI_Dealer_Distribution_Staging.postman_environment.json`** (3 KB)
   - Staging environment variables
   - Pre-configured user credentials
   - 24 environment variables
   - Auto-capture variables for tokens and IDs

3. **`POSTMAN_COLLECTION_GUIDE.md`** (This file)
   - Complete usage guide
   - Step-by-step test scenarios
   - Troubleshooting tips
   - Request body templates

---

## 🎯 Test Coverage

### Complete E2E Flow
✅ Sponsor → Dealer → Farmer distribution chain  
✅ Code transfer, distribution, and redemption  
✅ Plant analysis with attribution tracking  
✅ Analytics and performance monitoring  
✅ Three dealer onboarding methods (A, B, C)  
✅ Code reclaim operations  

### Endpoints Covered (28 total)

| Category | Endpoints | Coverage |
|----------|-----------|----------|
| Authentication | 3 | Phone OTP login for all roles |
| Sponsor Operations | 7 | Code management, analytics, transfer |
| Dealer Operations | 3 | Code distribution, analysis tracking |
| Farmer Operations | 5 | Redemption, subscription, analysis |
| Dealer Invitations | 4 | Methods B (Invite) & C (AutoCreate) |

### User Roles Tested

| Role | UserId | Phone | Capabilities |
|------|--------|-------|--------------|
| Main Sponsor | 159 | 05411111114 | Purchase, transfer, reclaim, analytics |
| Dealer | 158 | 05411111113 | Distribute codes, view own analyses |
| Farmer | 165 | 05061111113 | Redeem codes, perform analyses |

---

## ⚡ Quick Start (5 minutes)

### Step 1: Import to Postman
```
1. Open Postman
2. Click Import → File
3. Select both JSON files:
   - ZiraAI_Dealer_Distribution_Complete_E2E.postman_collection.json
   - ZiraAI_Dealer_Distribution_Staging.postman_environment.json
4. Select "ZiraAI - Dealer Distribution (Staging)" environment
```

### Step 2: Login as Main Sponsor
```
1. Navigate to: 00 - Authentication → Login - Main Sponsor
2. Request OTP: POST /PhoneAuth/request-otp {"phoneNumber": "05411111114"}
3. Get OTP from SMS or database
4. Update request body with OTP
5. Send request
6. ✅ Token auto-captured to {{sponsor_token}}
```

### Step 3: Run First Test
```
1. Navigate to: 01 - Sponsor Operations → Get Sponsor Available Codes
2. Send request (uses {{sponsor_token}} automatically)
3. ✅ Purchase ID and codes auto-captured
4. Check Console for captured values
```

**You're ready!** Continue with remaining tests in order.

---

## 📋 Test Scenarios

### Scenario 1: Code Transfer (Method A - Manual)
**Duration**: 10 minutes  
**Steps**:
1. Login - Main Sponsor
2. Get Sponsor Available Codes
3. Search Dealer by Email
4. Transfer Codes to Dealer
5. Get Dealer Performance (verify transfer)

**Expected Result**:
- ✅ 5 codes transferred to dealer
- ✅ Dealer summary shows 1 dealer
- ✅ Dealer performance shows 5 codes received

---

### Scenario 2: Code Distribution & Analysis
**Duration**: 20 minutes (includes 2-5 min processing)  
**Steps**:
1. Login - Dealer
2. Get Dealer Received Codes
3. Send Code to Farmer (SMS)
4. Login - Farmer
5. Redeem Code
6. Create Plant Analysis (Async)
7. ⏳ Wait 2-5 minutes
8. Get Analysis Result
9. Verify attribution (sponsor, dealer, farmer)

**Expected Result**:
- ✅ Farmer subscription activated (Tier M)
- ✅ Analysis processed successfully
- ✅ DealerId = 158 in analysis record
- ✅ SponsorCompanyId = 159 in analysis record

---

### Scenario 3: Analysis Visibility
**Duration**: 5 minutes  
**Steps**:
1. Login - Dealer → Get Dealer Analyses
   - Should see ONLY analyses from dealer's codes
2. Login - Main Sponsor → Get All Analyses
   - Should see ALL analyses (direct + dealer-distributed)
3. Login - Farmer → Get Farmer Analysis History
   - Should see ONLY own analyses

**Expected Result**:
- ✅ Dealer: 1-2 analyses (own distributed codes)
- ✅ Sponsor: 15-20 analyses (all from purchase)
- ✅ Farmer: 1-2 analyses (own only)

---

### Scenario 4: Dealer Invitations
**Duration**: 5 minutes  
**Steps**:

**Method B (Invite)**:
1. Login - Main Sponsor
2. Create Invitation (Method B)
3. Invitation link captured
4. Share link with dealer (external)
5. Get Dealer Invitations (verify pending)

**Method C (AutoCreate)**:
1. Login - Main Sponsor
2. Create AutoCreate Dealer
3. Dealer account created instantly
4. Credentials captured
5. Share credentials with dealer
6. Get Dealer Performance (verify codes transferred)

**Expected Result**:
- ✅ Method B: Invitation link generated
- ✅ Method C: Dealer ID + password returned
- ✅ Codes transferred immediately (Method C)

---

### Scenario 5: Code Reclaim
**Duration**: 5 minutes  
**Steps**:
1. Login - Main Sponsor
2. Get Dealer Summary (check dealer has codes)
3. Reclaim Codes from Dealer (2 codes)
4. Get Dealer Performance (verify reclaimed)
5. Get Sponsor Available Codes (verify returned)

**Expected Result**:
- ✅ 2 codes reclaimed from dealer
- ✅ Dealer available codes decreased by 2
- ✅ Sponsor available codes increased by 2

---

## 🔑 Key Features

### 1. Automatic Token Management
```javascript
// Login request auto-captures token
pm.environment.set("sponsor_token", jsonData.data.token.token);

// All requests use token automatically
Authorization: Bearer {{sponsor_token}}
```

### 2. Data Capture & Reuse
```javascript
// Capture from response
pm.environment.set("purchase_id", firstCode.purchaseId);
pm.environment.set("dealer_code", firstCode.code);

// Use in next request
{
  "code": "{{dealer_code}}",
  "purchaseId": {{purchase_id}}
}
```

### 3. Console Logging
```javascript
// Success indicators
console.log("✅ Code redeemed successfully");

// Warning indicators
console.log("⚠️ No codes available");

// Error indicators
console.log("❌ Request failed");
```

### 4. Smart Test Scripts
- Automatic validation of response status
- Data extraction and storage
- Progress tracking in console
- Error message display

---

## 📊 Request Organization

### Folder 1: Authentication (00)
Foundation for all tests - login all user roles

**Requests**:
- Login - Main Sponsor (159)
- Login - Dealer (158)
- Login - Farmer (165)

**Auto-Captured**:
- `{{sponsor_token}}`
- `{{dealer_token}}`
- `{{farmer_token}}`

---

### Folder 2: Sponsor Operations (01)
Main sponsor dealer network management

**Requests**:
1. Get Sponsor Available Codes
2. Search Dealer by Email
3. Transfer Codes to Dealer
4. Get Dealer Summary
5. Get Dealer Performance
6. Get All Analyses (Sponsor View)
7. Reclaim Codes from Dealer

**Auto-Captured**:
- `{{purchase_id}}`
- `{{available_code}}`
- `{{transferred_code_ids}}`

---

### Folder 3: Dealer Operations (02)
Dealer code distribution and tracking

**Requests**:
1. Get Dealer Received Codes
2. Send Code to Farmer (SMS)
3. Get Dealer Analyses (Own Only)

**Auto-Captured**:
- `{{dealer_code}}`
- `{{dealer_code_id}}`

---

### Folder 4: Farmer Operations (03)
Farmer subscription and analysis

**Requests**:
1. Redeem Code
2. Check Subscription Status
3. Create Plant Analysis (Async)
4. Get Analysis Result
5. Get Farmer Analysis History

**Auto-Captured**:
- `{{subscription_id}}`
- `{{subscription_tier}}`
- `{{analysis_id}}`

---

### Folder 5: Dealer Invitations (04)
Dealer onboarding methods B & C

**Requests**:
1. Create Invitation (Method B - Invite)
2. Create AutoCreate Dealer (Method C)
3. Get All Invitations
4. Get Pending Invitations

**Auto-Captured**:
- `{{invitation_link}}`
- `{{autocreated_dealer_id}}`
- `{{autocreated_password}}`

---

## 🎓 Usage Tips

### Tip 1: Run in Order
Requests are designed to run sequentially:
```
00 → 01 → 02 → 03 → (04 optional)
Auth → Sponsor → Dealer → Farmer → Invitations
```

### Tip 2: Check Console
All requests log progress to console:
```
View → Show Postman Console (Alt+Ctrl+C)
```

### Tip 3: Wait for Processing
Plant analysis is asynchronous:
```
1. Create Plant Analysis
2. ⏳ Wait 2-5 minutes
3. Get Analysis Result
```

### Tip 4: Token Expiry
JWT tokens expire in 60 minutes:
```
If you get 401 error → Re-run login request
```

### Tip 5: Variable Inspection
Check captured values anytime:
```
Environments → ZiraAI Staging → Current Value column
```

---

## 🔧 Troubleshooting

### Problem: "Not enough available codes"

**Cause**: Sponsor has 0 codes in Purchase 26

**Solutions**:
```
Option 1: Purchase new package (sponsor account)
Option 2: Use different purchase ID with codes
Option 3: Reclaim codes from dealer first
```

---

### Problem: Analysis stuck in "Processing"

**Cause**: Worker service or N8N webhook issue

**Check**:
```bash
# Check RabbitMQ
docker logs ziraai-rabbitmq

# Check Worker Service
docker logs ziraai-worker

# Check N8N
http://localhost:5678/workflow/plant-analysis
```

---

### Problem: Token not captured

**Cause**: Test script didn't run or response failed

**Solution**:
```
1. Check response status = 200
2. Check test script tab for errors
3. Manually copy token from response
4. Paste to environment variable
```

---

### Problem: Dealer not found

**Cause**: Email format incorrect

**Correct Format**:
```
Phone: 05411111113
Email: 05411111113@phone.ziraai.com
```

---

## 📈 Success Metrics

After running complete test suite, you should have:

### Data Created
- ✅ 1 dealer relationship (Sponsor → Dealer)
- ✅ 5 codes transferred to dealer
- ✅ 1+ codes distributed to farmer
- ✅ 1+ farmer subscriptions activated
- ✅ 1+ plant analyses completed
- ✅ Complete attribution chain verified

### Verifications
- ✅ Dealer sees ONLY own analyses
- ✅ Sponsor sees ALL analyses
- ✅ Farmer sees ONLY own analyses
- ✅ Analytics accurate for all parties
- ✅ Code states tracked correctly

### Performance
- ✅ All requests < 1s (except analysis)
- ✅ Analysis processing 2-5 minutes
- ✅ No 500 errors
- ✅ Clear error messages for validation failures

---

## 📚 Related Documentation

### Core Documentation
- **README**: `claudedocs/Dealers/README.md`
- **API Docs**: `claudedocs/Dealers/API_DOCUMENTATION.md`
- **Flow Guide**: `claudedocs/Dealers/SPONSOR_DEALER_FARMER_FLOW_GUIDE.md`

### Test Reports
- **E2E Test**: `claudedocs/Dealers/E2E_TEST_PROGRESS_REPORT.md`
- **Endpoint Tests**: `claudedocs/Dealers/ENDPOINT_TEST_RESULTS.md`
- **Dealer Tests**: `claudedocs/Dealers/DEALER_ENDPOINTS_TEST_REPORT.md`

### Development Docs
- **Tracker**: `claudedocs/Dealers/DEVELOPMENT_TRACKER.md`
- **Checklist**: `claudedocs/Dealers/TESTING_CHECKLIST.md`

---

## 🆘 Support

### Need Help?

**Documentation Issues**:
- Check `POSTMAN_COLLECTION_GUIDE.md` for detailed instructions
- Review related documentation files

**API Issues**:
- Check Swagger UI: `https://ziraai-api-sit.up.railway.app/swagger`
- Review application logs

**Database Issues**:
```sql
-- Check dealer codes
SELECT * FROM "SponsorshipCodes" WHERE "DealerId" = 158;

-- Check analyses
SELECT * FROM "PlantAnalyses" WHERE "DealerId" = 158;
```

---

## ✅ Ready to Test

**Prerequisites**:
- ✅ Postman installed
- ✅ Collection and environment imported
- ✅ Staging environment selected
- ✅ OTP access (SMS or database)

**Start Testing**:
1. Open collection in Postman
2. Start with folder `00 - Authentication`
3. Follow scenarios in order
4. Check console for progress
5. Verify results in database

**Expected Total Time**: 60-90 minutes for complete E2E test

---

**Collection Version**: 1.0  
**Created By**: Claude Code  
**Last Updated**: 2025-10-28  
**Environment**: Staging (ziraai-api-sit.up.railway.app)  
**Status**: ✅ Production Ready

**Happy Testing! 🚀**
