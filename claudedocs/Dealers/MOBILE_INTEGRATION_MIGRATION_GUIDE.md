# Mobile Integration Migration Guide - Dealer Invitation v2.0

**Target Audience**: Mobile Development Team  
**Version**: 2.0  
**Date**: 2025-10-30  
**Migration Type**: Optional Parameter Addition (Backward Compatible)

---

## 🎯 Overview

Bu doküman, Dealer Invitation sisteminin v1.0'dan v2.0'a geçişi için mobil uygulamada yapılması gereken değişiklikleri detaylandırır.

**Ana Değişiklik**: `purchaseId` **zorunlu** olmaktan çıktı, yerine **opsiyonel** `packageTier` parametresi eklendi.

---

## ⚠️ Breaking Changes

**HAYIR** - Bu güncelleme **backward compatible**'dır!

- ✅ Mevcut kodunuz değişiklik yapmazsanız bile çalışmaya devam edecek
- ✅ `purchaseId` hala kullanılabilir (deprecated değil)
- ✅ Eski request formatları geçerli
- ⚡ Ancak yeni `packageTier` özelliğini kullanmanız önerilir

---

## 📱 Mobile Team'in İşleyişi

### Current Flow (Değişmeyen)
```
Frontend/Web → Creates invitation → Sends SMS/Email with deep link
                                    ↓
Mobile App ← Catches deep link ← User clicks link
Mobile App → Processes invitation token
Mobile App → Calls /accept-invitation endpoint
```

### What Changes for Mobile
Mobile tarafında **hiçbir zorunlu değişiklik yok**, ama:
- Invitation detaylarında artık `packageTier` görebilirsiniz
- Accept endpoint'i aynı şekilde çalışmaya devam eder
- Dilerseniz UI'da tier bilgisini gösterebilirsiniz

---

## 🔄 API Changes Summary

### 1. Accept Invitation Endpoint (Mobile'ın Kullandığı)

**Endpoint**: `POST /api/v1/sponsorship/dealer/accept-invitation`

#### ❌ PAYLOAD DEĞİŞMEDİ
```json
// v1.0 (ESKİ)
{
  "invitationToken": "abc123def456..."
}

// v2.0 (YENİ) - AYNI!
{
  "invitationToken": "abc123def456..."
}
```

#### ✅ RESPONSE DEĞİŞTİ (Yeni Alanlar Eklendi)
```json
// v1.0 Response
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

// v2.0 Response (YENİ ALANLAR)
{
  "success": true,
  "message": "Davetiye kabul edildi. 5 kod hesabınıza aktarıldı",
  "data": {
    "invitationId": 123,
    "dealerId": 158,
    "codesTransferred": 5,
    "dealerName": "ABC Tarım",
    "acceptedAt": "2025-10-30T14:00:00Z",
    
    // 🆕 YENİ ALANLAR (opsiyonel)
    "packageTier": "M",              // S, M, L, XL veya null
    "transferredCodeIds": [940, 941, 942, 943, 944]  // Aktarılan kod ID'leri
  }
}
```

---

## 📋 Mobile Development Tasks

### Task 1: Update Data Models (Opsiyonel)

#### Dart/Flutter Example
```dart
// invitation_response.dart

class DealerInvitationAcceptResponse {
  final int invitationId;
  final int dealerId;
  final int codesTransferred;
  final String dealerName;
  final DateTime acceptedAt;
  
  // 🆕 YENİ ALANLAR - nullable yapın
  final String? packageTier;  // "S", "M", "L", "XL" veya null
  final List<int>? transferredCodeIds;

  DealerInvitationAcceptResponse({
    required this.invitationId,
    required this.dealerId,
    required this.codesTransferred,
    required this.dealerName,
    required this.acceptedAt,
    this.packageTier,  // opsiyonel
    this.transferredCodeIds,  // opsiyonel
  });

  factory DealerInvitationAcceptResponse.fromJson(Map<String, dynamic> json) {
    return DealerInvitationAcceptResponse(
      invitationId: json['invitationId'],
      dealerId: json['dealerId'],
      codesTransferred: json['codesTransferred'],
      dealerName: json['dealerName'],
      acceptedAt: DateTime.parse(json['acceptedAt']),
      
      // 🆕 YENİ ALANLAR - null-safe parsing
      packageTier: json['packageTier'] as String?,
      transferredCodeIds: json['transferredCodeIds'] != null
          ? List<int>.from(json['transferredCodeIds'])
          : null,
    );
  }
}
```

#### Kotlin Example
```kotlin
// DealerInvitationAcceptResponse.kt

data class DealerInvitationAcceptResponse(
    val invitationId: Int,
    val dealerId: Int,
    val codesTransferred: Int,
    val dealerName: String,
    val acceptedAt: String,
    
    // 🆕 YENİ ALANLAR - nullable
    val packageTier: String? = null,  // "S", "M", "L", "XL"
    val transferredCodeIds: List<Int>? = null
)
```

### Task 2: Update UI (Opsiyonel - UX İyileştirmesi)

#### Öneri: Tier Badge Gösterimi
```dart
// invitation_detail_screen.dart

Widget buildInvitationDetails(DealerInvitationAcceptResponse response) {
  return Column(
    children: [
      Text('✅ ${response.codesTransferred} kod hesabınıza aktarıldı'),
      
      // 🆕 Tier badge gösterimi (opsiyonel)
      if (response.packageTier != null)
        PackageTierBadge(tier: response.packageTier!),
      
      // Kod listesi
      if (response.transferredCodeIds != null)
        CodeListView(codeIds: response.transferredCodeIds!),
    ],
  );
}

// Package tier badge widget
class PackageTierBadge extends StatelessWidget {
  final String tier;
  
  Color get tierColor {
    switch (tier) {
      case 'S': return Colors.blue;
      case 'M': return Colors.green;
      case 'L': return Colors.orange;
      case 'XL': return Colors.red;
      default: return Colors.grey;
    }
  }
  
  String get tierLabel {
    switch (tier) {
      case 'S': return 'Small (1 analiz/gün)';
      case 'M': return 'Medium (2 analiz/gün)';
      case 'L': return 'Large (5 analiz/gün)';
      case 'XL': return 'Extra Large (10 analiz/gün)';
      default: return tier;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: tierColor.withOpacity(0.1),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: tierColor),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.card_giftcard, size: 16, color: tierColor),
          SizedBox(width: 4),
          Text(
            tierLabel,
            style: TextStyle(color: tierColor, fontWeight: FontWeight.bold),
          ),
        ],
      ),
    );
  }
}
```

---

## 🧪 Testing Guide for Mobile

### Test Scenario 1: Accept Invitation (Same as Before)

#### Deep Link Format (Değişmedi)
```
ziraai://ref/DEALER-abc123def456...
```

#### Mobile Flow
```dart
// 1. Extract token from deep link
final token = deepLink.split('DEALER-')[1];

// 2. Call accept endpoint (AYNI)
final response = await apiService.acceptDealerInvitation(token);

// 3. Handle response
if (response.success) {
  // Show success message
  showDialog(
    title: 'Başarılı',
    message: '${response.data.codesTransferred} kod hesabınıza aktarıldı',
  );
  
  // 🆕 YENİ: Tier bilgisini göster (opsiyonel)
  if (response.data.packageTier != null) {
    showTierBadge(response.data.packageTier);
  }
  
  // Navigate to dealer dashboard
  navigateTo(DealerDashboard());
}
```

### Test Scenario 2: Verify Response Fields

```dart
void testAcceptInvitationResponse() {
  final jsonResponse = '''
  {
    "success": true,
    "data": {
      "invitationId": 123,
      "dealerId": 158,
      "codesTransferred": 5,
      "dealerName": "Test Dealer",
      "acceptedAt": "2025-10-30T14:00:00Z",
      "packageTier": "M",
      "transferredCodeIds": [940, 941, 942, 943, 944]
    }
  }
  ''';
  
  final response = DealerInvitationAcceptResponse.fromJson(
    jsonDecode(jsonResponse)['data']
  );
  
  // Existing fields
  expect(response.invitationId, 123);
  expect(response.codesTransferred, 5);
  
  // 🆕 NEW fields
  expect(response.packageTier, 'M');
  expect(response.transferredCodeIds?.length, 5);
}
```

### Test Scenario 3: Backward Compatibility

```dart
void testBackwardCompatibility() {
  // Old response without new fields
  final oldJsonResponse = '''
  {
    "success": true,
    "data": {
      "invitationId": 123,
      "dealerId": 158,
      "codesTransferred": 5,
      "dealerName": "Test Dealer",
      "acceptedAt": "2025-10-30T14:00:00Z"
    }
  }
  ''';
  
  final response = DealerInvitationAcceptResponse.fromJson(
    jsonDecode(oldJsonResponse)['data']
  );
  
  // Should not crash
  expect(response.packageTier, null);  // ✅ null is OK
  expect(response.transferredCodeIds, null);  // ✅ null is OK
  expect(response.codesTransferred, 5);  // ✅ Still works
}
```

---

## 📊 Package Tier Reference

Mobile UI'da göstermek isterseniz:

| Tier | Display Name | Daily Limit | Monthly Limit | Color | Icon |
|------|-------------|-------------|---------------|-------|------|
| **S** | Small | 1 analiz/gün | 30 analiz/ay | 🔵 Blue | 📦 |
| **M** | Medium | 2 analiz/gün | 50 analiz/ay | 🟢 Green | 📦📦 |
| **L** | Large | 5 analiz/gün | 100 analiz/ay | 🟠 Orange | 📦📦📦 |
| **XL** | Extra Large | 10 analiz/gün | 300 analiz/ay | 🔴 Red | 📦📦📦📦 |

---

## 🔄 Migration Checklist for Mobile Team

### Zorunlu Değişiklikler
- [ ] **HAYIR - HİÇBİR ZORUNLU DEĞİŞİKLİK YOK**

### Önerilen İyileştirmeler
- [ ] Data modellerini güncelle (`packageTier`, `transferredCodeIds` ekle)
- [ ] Response parsing'i test et (backward compatibility)
- [ ] UI'da tier badge gösterimi ekle (opsiyonel)
- [ ] Kod listesi gösterimi ekle (opsiyonel)
- [ ] Test senaryolarını çalıştır

---

## 📞 API Endpoint Reference (Mobile'ın Kullandığı)

### Accept Dealer Invitation

**Endpoint**: `POST /api/v1/sponsorship/dealer/accept-invitation`

**Headers**:
```
Content-Type: application/json
x-dev-arch-version: 1.0
```

**Request**:
```json
{
  "invitationToken": "abc123def456..."
}
```

**Success Response (200)**:
```json
{
  "success": true,
  "message": "Davetiye kabul edildi. 5 kod hesabınıza aktarıldı",
  "data": {
    "invitationId": 123,
    "dealerId": 158,
    "codesTransferred": 5,
    "dealerName": "ABC Tarım",
    "acceptedAt": "2025-10-30T14:00:00Z",
    "packageTier": "M",  // 🆕 nullable
    "transferredCodeIds": [940, 941, 942, 943, 944]  // 🆕 nullable
  }
}
```

**Error Response (400)**:
```json
{
  "success": false,
  "message": "Davetiye bulunamadı veya süresi dolmuş"
}
```

**Error Response (410)**:
```json
{
  "success": false,
  "message": "Davetiye zaten kabul edilmiş"
}
```

---

## 🧪 Test Environment

### Staging API
```
Base URL: https://ziraai-api-sit.up.railway.app
```

### Test Deep Links
```
# Tier M invitation
ziraai://ref/DEALER-{token_from_sms}

# Multi-tier invitation (no tier filter)
ziraai://ref/DEALER-{token_from_sms}
```

### Test Token Generation
Backend'den test davetiyesi oluşturun:
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/invite-via-sms" \
  -H "Authorization: Bearer YOUR_SPONSOR_TOKEN" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "email": "test@mobile.com",
    "phone": "+905551234567",
    "dealerName": "Mobile Test Dealer",
    "packageTier": "M",
    "codeCount": 5
  }'

# Response'dan alacağınız token ile test edin
```

---

## 🚀 Deployment Timeline

### Önerilen Strateji: Phased Rollout

#### Phase 1: Backend Deploy (✅ TAMAMLANDI)
- Migration çalıştırıldı
- API v2.0 yayında
- Backward compatibility aktif

#### Phase 2: Mobile Update (Opsiyonel)
- **Zorunlu değil**: Mevcut kodunuz çalışmaya devam eder
- **Önerilir**: Yeni alanları parse edecek şekilde güncelleyin
- Test edin, kademeli olarak yayınlayın

#### Phase 3: UI Enhancements (İsteğe Bağlı)
- Tier badge gösterimi
- Kod listesi görünümü
- Detaylı analiz limitleri bilgisi

---

## ❓ FAQ

### Q: Mevcut kodumuz çalışmaya devam eder mi?
**A**: Evet! Hiçbir değişiklik yapmasanız bile çalışır.

### Q: `packageTier` ve `transferredCodeIds` null olabilir mi?
**A**: Evet, nullable olarak tanımlamalısınız. Eski davetiyeler için null olabilir.

### Q: Deep link formatı değişti mi?
**A**: Hayır, tamamen aynı: `ziraai://ref/DEALER-{token}`

### Q: Accept endpoint'inin payloadı değişti mi?
**A**: Hayır, hala sadece `invitationToken` gönderiyor.

### Q: Yeni alanları UI'da göstermek zorunda mıyız?
**A**: Hayır, opsiyonel. Ancak UX açısından önerilir.

### Q: Test nasıl yapmalıyız?
**A**: Staging environment'ta test davetiyesi oluşturun, deep link ile açın, accept edin.

---

## 📞 Support

### Backend Team Contact
- **Documentation**: `claudedocs/Dealers/`
- **Postman Collection**: `ZiraAI_Dealer_Invitation_PackageTier_v2.0.postman_collection.json`
- **Migration Guide**: `MIGRATION_TESTING_GUIDE.md`

### Questions?
Herhangi bir soru için backend ekibiyle iletişime geçin.

---

**Version**: 2.0  
**Last Updated**: 2025-10-30  
**Author**: ZiraAI Backend Team  
**For**: Mobile Development Team
