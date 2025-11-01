# Dealer Send-Link Error Analysis

**Date:** 2025-10-27  
**Issue:** Dealer cannot send codes via SMS, gets "Kod bulunamadı veya kullanılamaz durumda"  
**Endpoint:** `POST /api/v1/sponsorship/send-link`

---

## Error from Mobile Logs

### Request (Dealer ID 158)
```json
POST /api/v1/sponsorship/send-link
Authorization: Bearer <dealer_token_158>

{
  "recipients": [{
    "code": "AGRI-2025-8384DEE9",
    "phone": "+905866866386",
    "name": "Tolga KAYA"
  }],
  "channel": "SMS",
  "customMessage": null,
  "allowResendExpired": false
}
```

### Response (ERROR)
```json
{
  "data": {
    "totalSent": 1,
    "successCount": 0,
    "failureCount": 1,
    "results": [{
      "code": "AGRI-2025-8384DEE9",
      "phone": "+905866866386",
      "success": false,
      "errorMessage": "Kod bulunamadı veya kullanılamaz durumda",
      "deliveryStatus": "Failed - Invalid Code"
    }]
  },
  "success": true,
  "message": "📱 0 link başarıyla gönderildi via SMS"
}
```

### Code Details (From GET /codes response)
```json
{
  "id": 947,
  "code": "AGRI-2025-8384DEE9",
  "sponsorId": 159,              // ⚠️ Original sponsor
  "subscriptionTierId": 3,
  "sponsorshipPurchaseId": 26,
  "dealerId": 158,                // ✅ Dealer has this code
  "transferredAt": "2025-10-26T17:25:51.50519",
  "transferredByUserId": 159,
  "isUsed": false,
  "createdDate": "2025-10-12T17:40:11.764795",
  "expiryDate": "2025-11-11T17:40:11.764796",
  "isActive": true,
  "linkClickCount": 0,
  "linkDelivered": false
}
```

---

## Key Observations

### 1. Dealer Can SEE the Code
- GET /codes?onlyUnsent=true → Returns code 947
- Code shows `dealerId: 158` ✅
- Code is `isActive: true, isUsed: false` ✅

### 2. Dealer CANNOT Send the Code
- POST /send-link with code → "Kod bulunamadı veya kullanılamaz durumda"
- Error message suggests code not found or invalid

### 3. Code Ownership Structure
```
Code 947:
├─ sponsorId: 159 (Original owner - "dort tarim")
├─ dealerId: 158 (Transferred to dealer - "uc tarim")
├─ transferredAt: 2025-10-26
└─ transferredByUserId: 159
```

---

## Root Cause Hypothesis

**Likely Issue:** SendLinkCommand validation checks if code belongs to requesting user by checking `sponsorId`, but **ignores `dealerId`**.

**Expected Logic:**
```
User can send code IF:
  code.SponsorId == userId OR code.DealerId == userId
```

**Current Logic (WRONG):**
```
User can send code IF:
  code.SponsorId == userId  // ❌ Dealer fails this check!
```

When dealer (158) tries to send code 947:
- Code has `sponsorId: 159` (not 158) ❌
- Code has `dealerId: 158` ✅ (but not checked!)
- Validation fails → "Kod bulunamadı"

---

## Investigation Plan

### 1. Find SendLinkCommand Handler
```bash
grep -r "SendLinkCommand" Business/Handlers/
```

### 2. Check Validation Logic
Look for code ownership validation:
```csharp
var code = await _repository.GetAsync(c => c.Code == request.Code);

// ⚠️ SUSPECTED ISSUE:
if (code.SponsorId != userId) {
    return Error("Kod bulunamadı veya kullanılamaz durumda");
}

// ✅ SHOULD BE:
if (code.SponsorId != userId && code.DealerId != userId) {
    return Error("Kod bulunamadı veya kullanılamaz durumda");
}
```

### 3. Check Code Retrieval Query
Repository method might filter by SponsorId only:
```csharp
// ⚠️ WRONG:
GetAsync(c => c.Code == codeStr && c.SponsorId == userId)

// ✅ CORRECT:
GetAsync(c => c.Code == codeStr && (c.SponsorId == userId || c.DealerId == userId))
```

---

## Expected Fix

### Option 1: Fix Validation in Handler
```csharp
// In SendLinkCommandHandler
var code = await _codeRepository.GetAsync(c => c.Code == recipient.Code);

if (code == null) {
    return Error("Kod bulunamadı");
}

// ✅ FIX: Check both SponsorId AND DealerId
if (code.SponsorId != userId && code.DealerId != userId) {
    return Error("Bu kodu gönderme yetkiniz yok");
}
```

### Option 2: Fix Repository Query
```csharp
// In repository method (if filtering happens there)
public async Task<SponsorshipCode> GetCodeByUserIdAsync(string code, int userId)
{
    return await Context.SponsorshipCodes
        .FirstOrDefaultAsync(c => 
            c.Code == code && 
            c.IsActive && 
            (c.SponsorId == userId || c.DealerId == userId)); // ✅ FIX
}
```

---

## Related Scenarios

### Working: Sponsor Sends Code
```
Sponsor (159) sends code 947:
  code.SponsorId (159) == userId (159) ✅ → Success
```

### Broken: Dealer Sends Code
```
Dealer (158) sends code 947:
  code.SponsorId (159) != userId (158) ❌
  code.DealerId (158) == userId (158) ✅ (but not checked!)
  → Failure: "Kod bulunamadı"
```

---

## Test Cases Needed

### Test 1: Dealer Sends Transferred Code
**Setup:**
- Code 947: sponsorId=159, dealerId=158, isUsed=false
- User: dealer (158)

**Expected:** ✅ Success (dealer can send their transferred codes)

### Test 2: Dealer Sends Non-Transferred Code
**Setup:**
- Code 940: sponsorId=159, dealerId=NULL, isUsed=false
- User: dealer (158)

**Expected:** ❌ Error (dealer cannot send codes not transferred to them)

### Test 3: Sponsor Sends Own Code
**Setup:**
- Code 940: sponsorId=159, dealerId=NULL, isUsed=false
- User: sponsor (159)

**Expected:** ✅ Success (existing behavior - must not break)

### Test 4: Sponsor Sends Transferred Code
**Setup:**
- Code 947: sponsorId=159, dealerId=158, isUsed=false
- User: sponsor (159)

**Expected:** ✅ Success (sponsor can still send transferred codes - oversight)

---

## Next Steps

1. Find SendLinkCommandHandler file
2. Locate code ownership validation logic
3. Add dealer check: `|| code.DealerId == userId`
4. Test all 4 scenarios above
5. Ensure sponsor can still send codes (no regression)

---

## ✅ ROOT CAUSE IDENTIFIED

### File: `Business/Handlers/Sponsorship/Commands/SendSponsorshipLinkCommand.cs`
### Lines: 78-88

**Problem Code:**
```csharp
var validCodes = request.AllowResendExpired
    ? await _codeRepository.GetListAsync(c => 
        codes.Contains(c.Code) && 
        c.SponsorId == request.SponsorId &&  // ❌ ONLY checks SponsorId!
        !c.IsUsed)
    : await _codeRepository.GetListAsync(c => 
        codes.Contains(c.Code) && 
        c.SponsorId == request.SponsorId &&  // ❌ ONLY checks SponsorId!
        !c.IsUsed && 
        c.ExpiryDate > DateTime.Now);
```

**What Happens:**
1. Dealer (158) tries to send code "AGRI-2025-8384DEE9"
2. Query filters: `c.SponsorId == 158`
3. But code has `sponsorId: 159, dealerId: 158`
4. Filter fails → empty list → "Kod bulunamadı"

---

## ✅ SOLUTION IMPLEMENTED

**Fixed Code:**
```csharp
// ✅ FIX: Support both sponsor (original owner) and dealer (transferred to)
var validCodes = request.AllowResendExpired
    ? await _codeRepository.GetListAsync(c => 
        codes.Contains(c.Code) && 
        (c.SponsorId == request.SponsorId || c.DealerId == request.SponsorId) &&  // ✅ Check both!
        !c.IsUsed)
    : await _codeRepository.GetListAsync(c => 
        codes.Contains(c.Code) && 
        (c.SponsorId == request.SponsorId || c.DealerId == request.SponsorId) &&  // ✅ Check both!
        !c.IsUsed && 
        c.ExpiryDate > DateTime.Now);
```

**Now:**
1. Dealer (158) tries to send code "AGRI-2025-8384DEE9"
2. Query filters: `c.SponsorId == 158 OR c.DealerId == 158`
3. Code has `dealerId: 158` → ✅ Match!
4. Code validated → SMS sent successfully

---

## Test Results (Expected After Deployment)

### Test 1: Dealer Sends Transferred Code ✅
```
Code: AGRI-2025-8384DEE9
  - sponsorId: 159
  - dealerId: 158
User: 158 (dealer)
Result: ✅ SUCCESS (dealerId matches)
```

### Test 2: Dealer Sends Non-Transferred Code ❌
```
Code: AGRI-2025-12345678
  - sponsorId: 159
  - dealerId: NULL
User: 158 (dealer)
Result: ❌ "Kod bulunamadı" (neither sponsorId nor dealerId matches)
```

### Test 3: Sponsor Sends Own Code ✅
```
Code: AGRI-2025-12345678
  - sponsorId: 159
  - dealerId: NULL
User: 159 (sponsor)
Result: ✅ SUCCESS (sponsorId matches - backward compatible)
```

### Test 4: Sponsor Sends Transferred Code ✅
```
Code: AGRI-2025-8384DEE9
  - sponsorId: 159
  - dealerId: 158
User: 159 (sponsor)
Result: ✅ SUCCESS (sponsorId matches - sponsor retains oversight)
```

---

**Status:** ✅ FIXED  
**Priority:** 🔴 HIGH (Blocks dealer functionality)  
**Impact:** Dealers can now distribute codes via SMS/WhatsApp  
**Build:** ✅ Succeeded  
**Backward Compatible:** ✅ Yes (sponsor functionality unchanged)
