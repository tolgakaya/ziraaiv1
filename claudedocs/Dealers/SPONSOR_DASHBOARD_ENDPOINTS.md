# Sponsor Dashboard API Endpoints

**Last Updated**: 2025-10-31
**Purpose**: API endpoints for Sponsor Dashboard - Active Packages & Dealer Management

---

## 📋 Overview

Sponsor dashboard'da gösterilmesi gereken bilgiler için mevcut endpoint'ler:

### ✅ Mevcut Endpoint'ler

1. **Dealer Summary** - Tüm dealer'lara transfer edilen kodların özeti
2. **Dealer Invitations** - Gönderilen davetiyeler (Pending, Accepted, Expired)
3. **My Codes** - Sponsor'un kendi kodları (transfer edilmemiş)
4. **Dashboard Summary** - Genel sponsorluk istatistikleri

---

## 🎯 Önerilen Dashboard Yapısı

```
┌─────────────────────────────────────────────────┐
│  Aktif Sponsorluk Paketleriniz                  │
│  ──────────────────────────────────────────────  │
│  • Toplam Kod: 500                               │
│  • Transfer Edilmiş: 150 (30%)                   │
│  • Kullanılabilir: 350 (70%)                     │
│  • Kullanılmış: 75 (50% of transferred)         │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│  Dealer Dağıtım Özeti                            │
│  ──────────────────────────────────────────────  │
│  • Toplam Dealer: 5                              │
│  • Dealer'lara Transfer: 150 kod                 │
│  • Kullanılan: 75 kod                            │
│  • Mevcut: 50 kod (dealer'larda)                 │
│  • Geri Alınan: 25 kod                           │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│  Bekleyen Davetiyeler (3)                        │
│  ──────────────────────────────────────────────  │
│  📧 dealer1@example.com - 20 kod - 5 gün kaldı   │
│  📧 dealer2@example.com - 30 kod - 2 gün kaldı   │
│  📱 +905551234567      - 15 kod - 6 gün kaldı   │
└─────────────────────────────────────────────────┘
```

---

## 📡 API Endpoints

### 1. Dealer Summary (Transfer Edilen Kodlar)

**Purpose**: Tüm dealer'lara transfer edilen kodların toplam istatistikleri

#### Endpoint

```
GET /api/v1/sponsorship/dealer/summary
```

#### Request

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/summary" \
  -H "Authorization: Bearer {jwt_token}" \
  -H "x-dev-arch-version: 1.0"
```

#### Response Structure

```json
{
  "data": {
    "totalDealers": 5,
    "totalCodesDistributed": 150,
    "totalCodesUsed": 75,
    "totalCodesAvailable": 50,
    "totalCodesReclaimed": 25,
    "overallUsageRate": 50.0,
    "dealers": [
      {
        "dealerId": 158,
        "dealerName": "Dealer 1",
        "dealerEmail": "dealer1@example.com",
        "totalCodesReceived": 50,
        "codesSent": 40,
        "codesUsed": 30,
        "codesAvailable": 10,
        "codesReclaimed": 0,
        "usageRate": 75.0,
        "uniqueFarmersReached": 15,
        "totalAnalyses": 30,
        "firstTransferDate": "2025-10-01T10:00:00",
        "lastTransferDate": "2025-10-30T15:30:00"
      }
    ]
  },
  "success": true,
  "message": "Dealer summary retrieved successfully"
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `totalDealers` | int | Toplam dealer sayısı |
| `totalCodesDistributed` | int | Dealer'lara transfer edilen toplam kod |
| `totalCodesUsed` | int | Farmer'lar tarafından kullanılan kod |
| `totalCodesAvailable` | int | Dealer'larda kullanılabilir kod |
| `totalCodesReclaimed` | int | Geri alınan kod sayısı |
| `overallUsageRate` | decimal | Genel kullanım oranı (%) |
| `dealers` | array | Dealer detayları |

#### Dashboard'da Kullanım

```dart
// Transfer edilmiş kodlar için
final dealerSummary = response.data['totalCodesDistributed'];
final dealerCodesUsed = response.data['totalCodesUsed'];
final dealerCodesAvailable = response.data['totalCodesAvailable'];
```

---

### 2. Dealer Invitations (Pending Davetiyeler)

**Purpose**: Sponsor'un gönderdiği davetiyeler (status filtrelemesi ile)

#### Endpoint

```
GET /api/v1/sponsorship/dealer/invitations?status=Pending
```

#### Request

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/invitations?status=Pending" \
  -H "Authorization: Bearer {jwt_token}" \
  -H "x-dev-arch-version: 1.0"
```

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `status` | string | No | Filter: "Pending", "Accepted", "Expired", "Cancelled" (boş ise tümü) |

#### Response Structure

```json
{
  "data": [
    {
      "invitationId": 7,
      "invitationToken": "b62885cf69894896b3ebc668d6a7f649",
      "sponsorId": 159,
      "dealerEmail": "dealer@example.com",
      "dealerPhone": "+905556866386",
      "dealerName": "Dealer Name",
      "codeCount": 12,
      "packageTier": null,
      "status": "Pending",
      "invitationType": "Invite",
      "createdDate": "2025-10-31T08:31:22.790",
      "expiryDate": "2025-11-07T08:31:22.790",
      "linkSentDate": "2025-10-31T08:31:23.065",
      "linkSentVia": "SMS",
      "linkDelivered": true,
      "acceptedDate": null
    }
  ],
  "success": true,
  "message": "Invitations retrieved successfully"
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `invitationId` | int | Davetiye ID |
| `invitationToken` | string | 32-char token |
| `dealerEmail` | string | Dealer email |
| `dealerPhone` | string | Dealer telefon |
| `dealerName` | string | Dealer ismi |
| `codeCount` | int | Transfer edilecek kod sayısı |
| `packageTier` | string | Paket tier (null = any) |
| `status` | string | "Pending", "Accepted", "Expired", "Cancelled" |
| `createdDate` | DateTime | Oluşturulma tarihi |
| `expiryDate` | DateTime | Son geçerlilik tarihi |
| `acceptedDate` | DateTime? | Kabul edilme tarihi |

#### Dashboard'da Kullanım

```dart
// Pending invitations için
final pendingInvitations = response.data.where((inv) => inv['status'] == 'Pending').toList();
final pendingCount = pendingInvitations.length;

// Her invitation için kalan gün hesaplama
final expiryDate = DateTime.parse(inv['expiryDate']);
final remainingDays = expiryDate.difference(DateTime.now()).inDays;
```

---

### 3. My Codes (Kendi Kodları)

**Purpose**: Sponsor'un henüz transfer etmediği kendi kodları

#### Endpoint

```
GET /api/v1/sponsorship/codes?page=1&pageSize=1000&onlyUnsent=true
```

#### Request

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/codes?page=1&pageSize=1000&onlyUnsent=true" \
  -H "Authorization: Bearer {jwt_token}" \
  -H "x-dev-arch-version: 1.0"
```

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `page` | int | No | Sayfa numarası (default: 1) |
| `pageSize` | int | No | Sayfa başına kayıt (default: 10) |
| `onlyUnsent` | bool | No | Sadece gönderilmemiş kodlar |
| `status` | string | No | "available", "used", "expired" |

#### Response Structure

```json
{
  "data": {
    "codes": [
      {
        "id": 940,
        "code": "AGRI-2025-62038F92",
        "subscriptionTier": "L",
        "isUsed": false,
        "isActive": true,
        "expiryDate": "2026-01-29T07:29:31.763",
        "createdDate": "2025-10-29T07:29:31.763",
        "distributionDate": null,
        "dealerId": null
      }
    ],
    "totalCount": 350,
    "page": 1,
    "pageSize": 1000
  },
  "success": true
}
```

#### Dashboard'da Kullanım

```dart
// Kullanılabilir kodlar için
final totalAvailableCodes = response.data['totalCount'];

// Tier bazında gruplandırma
final codesByTier = {
  'S': codes.where((c) => c['subscriptionTier'] == 'S').length,
  'M': codes.where((c) => c['subscriptionTier'] == 'M').length,
  'L': codes.where((c) => c['subscriptionTier'] == 'L').length,
  'XL': codes.where((c) => c['subscriptionTier'] == 'XL').length,
};
```

---

### 4. Dashboard Summary (Genel İstatistikler)

**Purpose**: Sponsor'un genel sponsorluk istatistikleri

#### Endpoint

```
GET /api/v1/sponsorship/dashboard-summary
```

#### Request

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dashboard-summary" \
  -H "Authorization: Bearer {jwt_token}" \
  -H "x-dev-arch-version: 1.0"
```

#### Response Structure

```json
{
  "data": {
    "totalPurchases": 5,
    "totalCodesOwned": 500,
    "codesUsed": 75,
    "codesAvailable": 425,
    "totalFarmersReached": 50,
    "totalAnalyses": 75,
    "averageUsageRate": 15.0,
    "recentActivity": []
  },
  "success": true
}
```

---

## 🎨 Dashboard UI Implementation

### Flutter Example

```dart
class SponsorDashboard extends StatefulWidget {
  @override
  _SponsorDashboardState createState() => _SponsorDashboardState();
}

class _SponsorDashboardState extends State<SponsorDashboard> {
  late Future<DashboardData> _dashboardData;

  @override
  void initState() {
    super.initState();
    _dashboardData = _loadDashboardData();
  }

  Future<DashboardData> _loadDashboardData() async {
    // Parallel API calls
    final results = await Future.wait([
      _apiService.getDealerSummary(),
      _apiService.getDealerInvitations(status: 'Pending'),
      _apiService.getMyCodes(onlyUnsent: true),
      _apiService.getDashboardSummary(),
    ]);

    return DashboardData(
      dealerSummary: results[0],
      pendingInvitations: results[1],
      availableCodes: results[2],
      dashboardSummary: results[3],
    );
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<DashboardData>(
      future: _dashboardData,
      builder: (context, snapshot) {
        if (!snapshot.hasData) {
          return CircularProgressIndicator();
        }

        final data = snapshot.data!;

        return ListView(
          children: [
            // 1. Aktif Sponsorluk Paketleri
            _buildActivePackagesCard(data),

            // 2. Dealer Dağıtım Özeti
            _buildDealerSummaryCard(data),

            // 3. Bekleyen Davetiyeler
            _buildPendingInvitationsCard(data),
          ],
        );
      },
    );
  }

  Widget _buildActivePackagesCard(DashboardData data) {
    final totalCodes = data.dashboardSummary['totalCodesOwned'];
    final transferredCodes = data.dealerSummary['totalCodesDistributed'];
    final availableCodes = data.availableCodes['totalCount'];
    final usedCodes = data.dealerSummary['totalCodesUsed'];

    return Card(
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Aktif Sponsorluk Paketleriniz',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            SizedBox(height: 12),
            _buildStatRow('Toplam Kod', totalCodes),
            _buildStatRow('Transfer Edilmiş',
                '$transferredCodes (${_percentage(transferredCodes, totalCodes)}%)'),
            _buildStatRow('Kullanılabilir',
                '$availableCodes (${_percentage(availableCodes, totalCodes)}%)'),
            _buildStatRow('Kullanılmış',
                '$usedCodes (${_percentage(usedCodes, transferredCodes)}% of transferred)'),
          ],
        ),
      ),
    );
  }

  Widget _buildDealerSummaryCard(DashboardData data) {
    final summary = data.dealerSummary;

    return Card(
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Dealer Dağıtım Özeti',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            SizedBox(height: 12),
            _buildStatRow('Toplam Dealer', summary['totalDealers']),
            _buildStatRow("Dealer'lara Transfer", summary['totalCodesDistributed']),
            _buildStatRow('Kullanılan', summary['totalCodesUsed']),
            _buildStatRow('Mevcut (Dealer\'larda)', summary['totalCodesAvailable']),
            _buildStatRow('Geri Alınan', summary['totalCodesReclaimed']),
            Divider(),
            _buildStatRow('Kullanım Oranı', '${summary['overallUsageRate']}%',
                isHighlight: true),
          ],
        ),
      ),
    );
  }

  Widget _buildPendingInvitationsCard(DashboardData data) {
    final invitations = data.pendingInvitations;

    return Card(
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Bekleyen Davetiyeler',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                Chip(
                  label: Text('${invitations.length}'),
                  backgroundColor: Colors.orange,
                ),
              ],
            ),
            SizedBox(height: 12),
            if (invitations.isEmpty)
              Text('Bekleyen davetiye yok', style: TextStyle(color: Colors.grey))
            else
              ...invitations.map((inv) => _buildInvitationTile(inv)).toList(),
          ],
        ),
      ),
    );
  }

  Widget _buildInvitationTile(Map<String, dynamic> invitation) {
    final expiryDate = DateTime.parse(invitation['expiryDate']);
    final remainingDays = expiryDate.difference(DateTime.now()).inDays;
    final contact = invitation['dealerEmail'] ?? invitation['dealerPhone'];

    return ListTile(
      leading: Icon(
        invitation['dealerEmail'] != null ? Icons.email : Icons.phone,
        color: Colors.blue,
      ),
      title: Text(contact),
      subtitle: Text('${invitation['codeCount']} kod - $remainingDays gün kaldı'),
      trailing: IconButton(
        icon: Icon(Icons.cancel),
        onPressed: () => _cancelInvitation(invitation['invitationId']),
      ),
    );
  }

  Widget _buildStatRow(String label, dynamic value, {bool isHighlight = false}) {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(color: Colors.grey[700])),
          Text(
            value.toString(),
            style: TextStyle(
              fontWeight: isHighlight ? FontWeight.bold : FontWeight.normal,
              color: isHighlight ? Colors.green : Colors.black,
              fontSize: isHighlight ? 18 : 14,
            ),
          ),
        ],
      ),
    );
  }

  int _percentage(int part, int total) {
    if (total == 0) return 0;
    return ((part / total) * 100).round();
  }

  void _cancelInvitation(int invitationId) {
    // TODO: Implement cancel invitation
  }
}

class DashboardData {
  final Map<String, dynamic> dealerSummary;
  final List<dynamic> pendingInvitations;
  final Map<String, dynamic> availableCodes;
  final Map<String, dynamic> dashboardSummary;

  DashboardData({
    required this.dealerSummary,
    required this.pendingInvitations,
    required this.availableCodes,
    required this.dashboardSummary,
  });
}
```

---

## 📊 Data Flow

```
┌───────────────┐
│   Dashboard   │
│   Component   │
└───────┬───────┘
        │
        ├─► GET /dealer/summary ──────► Transfer edilmiş kodlar
        │
        ├─► GET /dealer/invitations?status=Pending ──► Bekleyen davetiyeler
        │
        ├─► GET /codes?onlyUnsent=true ──► Kullanılabilir kodlar
        │
        └─► GET /dashboard-summary ──► Genel istatistikler
```

---

## ✅ Sonuç

### Mevcut Endpoint'ler Yeterli ✅

Sponsor dashboard için ihtiyaç duyulan tüm bilgiler mevcut endpoint'lerle elde edilebilir:

1. ✅ **Transfer edilmiş kodlar** → `GET /dealer/summary`
2. ✅ **Pending invitations** → `GET /dealer/invitations?status=Pending`
3. ✅ **Kullanılabilir kodlar** → `GET /codes?onlyUnsent=true`
4. ✅ **Genel istatistikler** → `GET /dashboard-summary`

### Yeni Endpoint Gerekmez ❌

Tüm veriler mevcut endpoint'lerle alınabilir. Sadece mobile tarafta:
- Paralel API çağrıları yaparak performans iyileştirilebilir
- Response'ları birleştirerek dashboard UI oluşturulabilir
- Cache mekanizması ile API çağrıları azaltılabilir

---

**Document Version**: 1.0
**Created**: 2025-10-31
**Status**: ✅ Endpoint'ler Mevcut - Yeni Geliştirme Gerekmez
