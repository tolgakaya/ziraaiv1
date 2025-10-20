# ⚠️ BREAKING CHANGE: Conversation Endpoint Parameter Update

**Date**: 2025-10-19
**Severity**: 🔴 **BREAKING CHANGE** - Immediate action required
**Affected Endpoint**: `GET /api/sponsorship/messages/conversation`
**Deploy Environment**: Staging (SIT), will be deployed to Production

---

## 🔄 What Changed?

The query parameter name has been renamed for clarity:

| Before | After |
|--------|-------|
| `farmerId` | `otherUserId` |

---

## ❌ OLD Usage (Will STOP working after deploy)

```http
GET /api/sponsorship/messages/conversation?farmerId=159&plantAnalysisId=60
Authorization: Bearer {token}
```

```dart
// OLD - Will return 400 Bad Request
final response = await http.get(
  Uri.parse('$baseUrl/api/v1/sponsorship/messages/conversation?farmerId=$userId&plantAnalysisId=$analysisId'),
  headers: {'Authorization': 'Bearer $token'},
);
```

---

## ✅ NEW Usage (Required after deploy)

```http
GET /api/sponsorship/messages/conversation?otherUserId=159&plantAnalysisId=60
Authorization: Bearer {token}
```

```dart
// NEW - Use otherUserId instead of farmerId
final response = await http.get(
  Uri.parse('$baseUrl/api/v1/sponsorship/messages/conversation?otherUserId=$userId&plantAnalysisId=$analysisId'),
  headers: {'Authorization': 'Bearer $token'},
);
```

---

## 🎯 Why This Change?

The old parameter name `farmerId` was **misleading** because:

- ❌ It suggested only farmers could be specified
- ❌ It didn't reflect that sponsors can also be the "other user"
- ✅ The endpoint works **bidirectionally** (farmer ↔ sponsor)

The new name `otherUserId` is **accurate**:

- ✅ Works for both sponsor-to-farmer and farmer-to-sponsor conversations
- ✅ Clearly indicates "the other participant in the conversation"
- ✅ Makes the API more intuitive

---

## 📱 Required Mobile App Changes

### **Step 1: Find all uses of `farmerId` parameter**

Search your codebase for:
- `farmerId=`
- `"farmerId"`
- `farmerId:`

Example locations:
```dart
// messaging_service.dart
// conversation_repository.dart
// chat_screen.dart
```

### **Step 2: Replace with `otherUserId`**

**Before**:
```dart
Future<List<Message>> getConversation({
  required int farmerId,
  required int plantAnalysisId,
}) async {
  final url = '$baseUrl/api/v1/sponsorship/messages/conversation'
              '?farmerId=$farmerId&plantAnalysisId=$plantAnalysisId';
  // ...
}
```

**After**:
```dart
Future<List<Message>> getConversation({
  required int otherUserId,  // ← Changed
  required int plantAnalysisId,
}) async {
  final url = '$baseUrl/api/v1/sponsorship/messages/conversation'
              '?otherUserId=$otherUserId&plantAnalysisId=$plantAnalysisId';  // ← Changed
  // ...
}
```

### **Step 3: Update caller code**

**Before**:
```dart
// When farmer views conversation with sponsor
final messages = await messagingService.getConversation(
  farmerId: sponsorUserId,  // ← Confusing variable name
  plantAnalysisId: analysisId,
);
```

**After**:
```dart
// When farmer views conversation with sponsor
final messages = await messagingService.getConversation(
  otherUserId: sponsorUserId,  // ← Clear variable name
  plantAnalysisId: analysisId,
);

// When sponsor views conversation with farmer
final messages = await messagingService.getConversation(
  otherUserId: farmerUserId,  // ← Also works for sponsor
  plantAnalysisId: analysisId,
);
```

---

## ✅ Testing Checklist

After updating your code:

- [ ] **Farmer viewing sponsor conversation**: Works ✅
- [ ] **Sponsor viewing farmer conversation**: Works ✅
- [ ] **Messages appear in both directions**: Verified ✅
- [ ] **Old `farmerId` parameter**: Returns 400 error (expected) ✅

---

## 🕒 Timeline

| Environment | Status | ETA |
|-------------|--------|-----|
| **Staging (SIT)** | 🟢 Deployed | Now |
| **Production** | 🟡 Pending | TBD |

**Action Required**: Update mobile app code **before** production deployment.

---

## 📞 Support

If you have questions or need clarification:

1. Check updated API documentation: `claudedocs/MOBILE_BACKEND_API_INTEGRATION.md`
2. Test on Staging: `https://ziraai-api-sit.up.railway.app`
3. Contact backend team if issues persist

---

## 📄 Summary

**What to change**: Query parameter name
**From**: `farmerId`
**To**: `otherUserId`
**Impact**: All conversation fetching in mobile app
**Urgency**: Update before production deployment

**Example Change**:
```diff
- GET /api/sponsorship/messages/conversation?farmerId=159&plantAnalysisId=60
+ GET /api/sponsorship/messages/conversation?otherUserId=159&plantAnalysisId=60
```

✅ **This change makes the API clearer and more accurate!**
