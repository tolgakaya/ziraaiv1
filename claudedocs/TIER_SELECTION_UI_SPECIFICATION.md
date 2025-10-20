# Tier Selection UI Specification

**Version:** 1.0
**Date:** 2025-10-12
**Target Audience:** Mobile & Web Frontend Teams
**Status:** ✅ Ready for Implementation

---

## 📋 Overview

This document specifies how to present subscription tier options to sponsors during the package purchase flow. It provides the data structure, API endpoints, and UI/UX recommendations for tier comparison and selection.

### Key Requirements

✅ Show tier-specific sponsorship features (not just subscription features)
✅ Enable side-by-side tier comparison
✅ Highlight data access percentages (30%/60%/100%)
✅ Display logo visibility rules per screen
✅ Show communication capabilities (messaging)
✅ Present Smart Links quota for XL tier
✅ Mobile-first design with responsive layout

---

## 🎯 Tier Comparison Matrix

### Complete Feature Breakdown by Tier

| Feature | S Tier | M Tier | L Tier | XL Tier |
|---------|--------|--------|--------|---------|
| **Pricing** |
| Monthly Price | 50 TRY | 100 TRY | 200 TRY | 500 TRY |
| Yearly Price | 500 TRY | 1000 TRY | 2000 TRY | 5000 TRY |
| **Data Access** |
| Farmer Data Visibility | 30% | 60% | 100% | 100% |
| Farmer Name/Contact | ❌ | ❌ | ✅ | ✅ |
| Location (City) | ✅ | ✅ | ✅ | ✅ |
| Location (District) | ❌ | ✅ | ✅ | ✅ |
| Location (Coordinates) | ❌ | ❌ | ✅ | ✅ |
| Crop Types | ❌ | ✅ | ✅ | ✅ |
| Disease Categories | ❌ | ✅ | ✅ | ✅ |
| Full Analysis Details | ❌ | ❌ | ✅ | ✅ |
| Analysis Images | ❌ | ❌ | ✅ | ✅ |
| AI Recommendations | ❌ | ❌ | ✅ | ✅ |
| **Logo Visibility** |
| Start Screen | ✅ | ✅ | ✅ | ✅ |
| Result Screen | ❌ | ✅ | ✅ | ✅ |
| Analysis Details Screen | ❌ | ❌ | ✅ | ✅ |
| Farmer Profile Screen | ❌ | ❌ | ✅ | ✅ |
| **Communication** |
| Send Messages to Farmers | ❌ | ❌ | ✅ | ✅ |
| View Message Conversations | ❌ | ❌ | ✅ | ✅ |
| Message Rate Limit | - | - | 10/day/farmer | 10/day/farmer |
| **Smart Links** |
| Create Smart Links | ❌ | ❌ | ❌ | ✅ |
| View Smart Link Analytics | ❌ | ❌ | ❌ | ✅ |
| Smart Link Quota | 0 | 0 | 0 | 50 |
| **Subscription Limits** |
| Daily Request Limit | 10 | 20 | 50 | 100 |
| Monthly Request Limit | 300 | 600 | 1500 | 3000 |
| **Support** |
| Priority Support | ❌ | ❌ | ✅ | ✅ |
| Response Time | 48h | 48h | 24h | 12h |
| **Purchase Options** |
| Min Purchase Quantity | 10 | 10 | 10 | 10 |
| Max Purchase Quantity | 10,000 | 10,000 | 10,000 | 10,000 |
| Recommended Quantity | 100 | 100 | 100 | 100 |
| Code Validity | 30 days | 30 days | 30 days | 30 days |

---

## 📦 Enhanced API Response Structure

### New DTO: `SponsorshipTierComparisonDto`

```csharp
namespace Entities.Dtos
{
    /// <summary>
    /// Sponsorship-specific tier comparison DTO for purchase selection UI
    /// Extends SubscriptionTierDto with sponsor-specific features
    /// </summary>
    public class SponsorshipTierComparisonDto
    {
        // Basic Tier Info
        public int Id { get; set; }
        public string TierName { get; set; } // S, M, L, XL
        public string DisplayName { get; set; }
        public string Description { get; set; }

        // Pricing
        public decimal MonthlyPrice { get; set; }
        public decimal YearlyPrice { get; set; }
        public string Currency { get; set; }

        // Purchase Limits
        public int MinPurchaseQuantity { get; set; }
        public int MaxPurchaseQuantity { get; set; }
        public int RecommendedQuantity { get; set; }

        // Subscription Quotas
        public int DailyRequestLimit { get; set; }
        public int MonthlyRequestLimit { get; set; }

        // 🆕 Sponsorship-Specific Features
        public SponsorshipFeaturesDto SponsorshipFeatures { get; set; }

        // Display Metadata
        public bool IsPopular { get; set; } // Highlight M/L tiers
        public bool IsRecommended { get; set; } // Recommend based on business logic
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// Sponsorship-specific features (NOT subscription features)
    /// </summary>
    public class SponsorshipFeaturesDto
    {
        // Data Access
        public int DataAccessPercentage { get; set; } // 30, 60, 100
        public FarmerDataAccessDto DataAccess { get; set; }

        // Logo Display
        public LogoVisibilityDto LogoVisibility { get; set; }

        // Communication
        public CommunicationFeaturesDto Communication { get; set; }

        // Smart Links (XL exclusive)
        public SmartLinksFeaturesDto SmartLinks { get; set; }

        // Support
        public SupportFeaturesDto Support { get; set; }
    }

    /// <summary>
    /// What farmer data the sponsor can access
    /// </summary>
    public class FarmerDataAccessDto
    {
        public bool FarmerNameContact { get; set; }
        public bool LocationCity { get; set; }
        public bool LocationDistrict { get; set; }
        public bool LocationCoordinates { get; set; }
        public bool CropTypes { get; set; }
        public bool DiseaseCategories { get; set; }
        public bool FullAnalysisDetails { get; set; }
        public bool AnalysisImages { get; set; }
        public bool AiRecommendations { get; set; }
    }

    /// <summary>
    /// Where sponsor logo appears in farmer's app
    /// </summary>
    public class LogoVisibilityDto
    {
        public bool StartScreen { get; set; }
        public bool ResultScreen { get; set; }
        public bool AnalysisDetailsScreen { get; set; }
        public bool FarmerProfileScreen { get; set; }

        // For UI display
        public List<string> VisibleScreens { get; set; }
    }

    /// <summary>
    /// Communication capabilities with farmers
    /// </summary>
    public class CommunicationFeaturesDto
    {
        public bool MessagingEnabled { get; set; }
        public bool ViewConversations { get; set; }
        public int? MessageRateLimitPerDay { get; set; } // null if disabled, 10 if enabled
    }

    /// <summary>
    /// Smart Links capabilities (XL tier exclusive)
    /// </summary>
    public class SmartLinksFeaturesDto
    {
        public bool Enabled { get; set; }
        public int Quota { get; set; } // 0 for non-XL, 50 for XL
        public bool AnalyticsAccess { get; set; }
    }

    /// <summary>
    /// Support tier features
    /// </summary>
    public class SupportFeaturesDto
    {
        public bool PrioritySupport { get; set; }
        public int ResponseTimeHours { get; set; } // 48, 24, 12
    }
}
```

---

## 🔌 API Endpoint Enhancement

### Option 1: New Endpoint (Recommended)

**Endpoint:** `GET /api/v1/sponsorship/tiers-for-purchase`
**Authorization:** `Sponsor` or `Admin` role
**Purpose:** Get tier comparison specifically for purchase selection UI

**Response Example:**

```json
{
  "success": true,
  "message": "Sponsorship tiers retrieved successfully",
  "data": [
    {
      "id": 1,
      "tierName": "S",
      "displayName": "Small - Basic Visibility",
      "description": "Start screen logo and basic analytics",
      "monthlyPrice": 50.00,
      "yearlyPrice": 500.00,
      "currency": "TRY",
      "minPurchaseQuantity": 10,
      "maxPurchaseQuantity": 10000,
      "recommendedQuantity": 100,
      "dailyRequestLimit": 10,
      "monthlyRequestLimit": 300,
      "sponsorshipFeatures": {
        "dataAccessPercentage": 30,
        "dataAccess": {
          "farmerNameContact": false,
          "locationCity": true,
          "locationDistrict": false,
          "locationCoordinates": false,
          "cropTypes": false,
          "diseaseCategories": false,
          "fullAnalysisDetails": false,
          "analysisImages": false,
          "aiRecommendations": false
        },
        "logoVisibility": {
          "startScreen": true,
          "resultScreen": false,
          "analysisDetailsScreen": false,
          "farmerProfileScreen": false,
          "visibleScreens": ["Start Screen"]
        },
        "communication": {
          "messagingEnabled": false,
          "viewConversations": false,
          "messageRateLimitPerDay": null
        },
        "smartLinks": {
          "enabled": false,
          "quota": 0,
          "analyticsAccess": false
        },
        "support": {
          "prioritySupport": false,
          "responseTimeHours": 48
        }
      },
      "isPopular": false,
      "isRecommended": false,
      "displayOrder": 1
    },
    {
      "id": 2,
      "tierName": "M",
      "displayName": "Medium - Enhanced Visibility",
      "description": "Start + result screen logos and better analytics",
      "monthlyPrice": 100.00,
      "yearlyPrice": 1000.00,
      "currency": "TRY",
      "minPurchaseQuantity": 10,
      "maxPurchaseQuantity": 10000,
      "recommendedQuantity": 100,
      "dailyRequestLimit": 20,
      "monthlyRequestLimit": 600,
      "sponsorshipFeatures": {
        "dataAccessPercentage": 60,
        "dataAccess": {
          "farmerNameContact": false,
          "locationCity": true,
          "locationDistrict": true,
          "locationCoordinates": false,
          "cropTypes": true,
          "diseaseCategories": true,
          "fullAnalysisDetails": false,
          "analysisImages": false,
          "aiRecommendations": false
        },
        "logoVisibility": {
          "startScreen": true,
          "resultScreen": true,
          "analysisDetailsScreen": false,
          "farmerProfileScreen": false,
          "visibleScreens": ["Start Screen", "Result Screen"]
        },
        "communication": {
          "messagingEnabled": false,
          "viewConversations": false,
          "messageRateLimitPerDay": null
        },
        "smartLinks": {
          "enabled": false,
          "quota": 0,
          "analyticsAccess": false
        },
        "support": {
          "prioritySupport": false,
          "responseTimeHours": 48
        }
      },
      "isPopular": true,
      "isRecommended": true,
      "displayOrder": 2
    },
    {
      "id": 3,
      "tierName": "L",
      "displayName": "Large - Full Access + Messaging",
      "description": "All screens, full data access, direct farmer communication",
      "monthlyPrice": 200.00,
      "yearlyPrice": 2000.00,
      "currency": "TRY",
      "minPurchaseQuantity": 10,
      "maxPurchaseQuantity": 10000,
      "recommendedQuantity": 100,
      "dailyRequestLimit": 50,
      "monthlyRequestLimit": 1500,
      "sponsorshipFeatures": {
        "dataAccessPercentage": 100,
        "dataAccess": {
          "farmerNameContact": true,
          "locationCity": true,
          "locationDistrict": true,
          "locationCoordinates": true,
          "cropTypes": true,
          "diseaseCategories": true,
          "fullAnalysisDetails": true,
          "analysisImages": true,
          "aiRecommendations": true
        },
        "logoVisibility": {
          "startScreen": true,
          "resultScreen": true,
          "analysisDetailsScreen": true,
          "farmerProfileScreen": true,
          "visibleScreens": ["Start Screen", "Result Screen", "Analysis Details", "Farmer Profile"]
        },
        "communication": {
          "messagingEnabled": true,
          "viewConversations": true,
          "messageRateLimitPerDay": 10
        },
        "smartLinks": {
          "enabled": false,
          "quota": 0,
          "analyticsAccess": false
        },
        "support": {
          "prioritySupport": true,
          "responseTimeHours": 24
        }
      },
      "isPopular": true,
      "isRecommended": false,
      "displayOrder": 3
    },
    {
      "id": 4,
      "tierName": "XL",
      "displayName": "Extra Large - Premium + Smart Links",
      "description": "Everything in L + AI-powered product recommendations",
      "monthlyPrice": 500.00,
      "yearlyPrice": 5000.00,
      "currency": "TRY",
      "minPurchaseQuantity": 10,
      "maxPurchaseQuantity": 10000,
      "recommendedQuantity": 100,
      "dailyRequestLimit": 100,
      "monthlyRequestLimit": 3000,
      "sponsorshipFeatures": {
        "dataAccessPercentage": 100,
        "dataAccess": {
          "farmerNameContact": true,
          "locationCity": true,
          "locationDistrict": true,
          "locationCoordinates": true,
          "cropTypes": true,
          "diseaseCategories": true,
          "fullAnalysisDetails": true,
          "analysisImages": true,
          "aiRecommendations": true
        },
        "logoVisibility": {
          "startScreen": true,
          "resultScreen": true,
          "analysisDetailsScreen": true,
          "farmerProfileScreen": true,
          "visibleScreens": ["Start Screen", "Result Screen", "Analysis Details", "Farmer Profile"]
        },
        "communication": {
          "messagingEnabled": true,
          "viewConversations": true,
          "messageRateLimitPerDay": 10
        },
        "smartLinks": {
          "enabled": true,
          "quota": 50,
          "analyticsAccess": true
        },
        "support": {
          "prioritySupport": true,
          "responseTimeHours": 12
        }
      },
      "isPopular": false,
      "isRecommended": false,
      "displayOrder": 4
    }
  ]
}
```

### Option 2: Enhance Existing Endpoint

**Endpoint:** `GET /api/v1/subscriptions/tiers?forSponsorship=true`
**Authorization:** Public or `Sponsor`
**Query Parameter:** `forSponsorship=true` → Returns enhanced response with sponsorship features

---

## 🎨 UI/UX Recommendations

### Mobile (Flutter) - Tier Selection Screen

```dart
class TierSelectionScreen extends StatefulWidget {
  @override
  _TierSelectionScreenState createState() => _TierSelectionScreenState();
}

class _TierSelectionScreenState extends State<TierSelectionScreen> {
  int? _selectedTierId;
  List<SponsorshipTierComparisonDto> _tiers = [];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Paket Seçimi'),
      ),
      body: SingleChildScrollView(
        child: Column(
          children: [
            // Header Section
            _buildHeaderSection(),

            // Tier Cards (Horizontal Scrollable)
            _buildTierCards(),

            // Feature Comparison Table
            _buildFeatureComparison(),

            // Continue Button
            _buildContinueButton(),
          ],
        ),
      ),
    );
  }

  Widget _buildHeaderSection() {
    return Container(
      padding: EdgeInsets.all(20),
      child: Column(
        children: [
          Text(
            'Size En Uygun Paketi Seçin',
            style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold),
          ),
          SizedBox(height: 8),
          Text(
            'Paketler, çiftçilere sağladığınız ayrıcalıkları belirler',
            style: TextStyle(fontSize: 14, color: Colors.grey[600]),
          ),
        ],
      ),
    );
  }

  Widget _buildTierCards() {
    return Container(
      height: 280,
      child: ListView.builder(
        scrollDirection: Axis.horizontal,
        padding: EdgeInsets.symmetric(horizontal: 16),
        itemCount: _tiers.length,
        itemBuilder: (context, index) {
          final tier = _tiers[index];
          final isSelected = _selectedTierId == tier.id;

          return GestureDetector(
            onTap: () => setState(() => _selectedTierId = tier.id),
            child: Container(
              width: 280,
              margin: EdgeInsets.only(right: 16),
              decoration: BoxDecoration(
                border: Border.all(
                  color: isSelected ? Colors.green : Colors.grey[300]!,
                  width: isSelected ? 3 : 1,
                ),
                borderRadius: BorderRadius.circular(16),
                color: isSelected ? Colors.green.shade50 : Colors.white,
              ),
              child: Stack(
                children: [
                  // Popular Badge
                  if (tier.isPopular)
                    Positioned(
                      top: 12,
                      right: 12,
                      child: Container(
                        padding: EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                        decoration: BoxDecoration(
                          color: Colors.orange,
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Text(
                          'En Popüler',
                          style: TextStyle(
                            color: Colors.white,
                            fontSize: 12,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                    ),

                  Padding(
                    padding: EdgeInsets.all(20),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        // Tier Name
                        Text(
                          tier.displayName,
                          style: TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        SizedBox(height: 8),

                        // Price
                        Row(
                          crossAxisAlignment: CrossAxisAlignment.baseline,
                          textBaseline: TextBaseline.alphabetic,
                          children: [
                            Text(
                              '${tier.monthlyPrice.toInt()}',
                              style: TextStyle(
                                fontSize: 32,
                                fontWeight: FontWeight.bold,
                                color: Colors.green,
                              ),
                            ),
                            Text(
                              ' ${tier.currency}/ay',
                              style: TextStyle(fontSize: 14, color: Colors.grey),
                            ),
                          ],
                        ),

                        SizedBox(height: 16),

                        // Key Features
                        _buildFeatureItem(
                          '📊 Veri Erişimi',
                          '${tier.sponsorshipFeatures.dataAccessPercentage}%',
                        ),
                        _buildFeatureItem(
                          '🖼️ Logo Görünürlüğü',
                          '${tier.sponsorshipFeatures.logoVisibility.visibleScreens.length} ekran',
                        ),
                        if (tier.sponsorshipFeatures.communication.messagingEnabled)
                          _buildFeatureItem('💬 Mesajlaşma', 'Aktif'),
                        if (tier.sponsorshipFeatures.smartLinks.enabled)
                          _buildFeatureItem('🔗 Akıllı Linkler', '${tier.sponsorshipFeatures.smartLinks.quota} adet'),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _buildFeatureItem(String label, String value) {
    return Padding(
      padding: EdgeInsets.only(bottom: 8),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(fontSize: 13)),
          Text(
            value,
            style: TextStyle(fontSize: 13, fontWeight: FontWeight.bold),
          ),
        ],
      ),
    );
  }

  Widget _buildFeatureComparison() {
    return Container(
      margin: EdgeInsets.all(16),
      child: ExpansionTile(
        title: Text('Detaylı Özellik Karşılaştırması'),
        children: [
          _buildComparisonTable(),
        ],
      ),
    );
  }

  Widget _buildComparisonTable() {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: DataTable(
        columnSpacing: 20,
        columns: [
          DataColumn(label: Text('Özellik', style: TextStyle(fontWeight: FontWeight.bold))),
          ..._tiers.map((t) => DataColumn(
            label: Text(t.tierName, style: TextStyle(fontWeight: FontWeight.bold)),
          )),
        ],
        rows: [
          _buildDataRow('Çiftçi İletişim', _tiers.map((t) =>
            t.sponsorshipFeatures.dataAccess.farmerNameContact).toList()),
          _buildDataRow('Konum (İlçe)', _tiers.map((t) =>
            t.sponsorshipFeatures.dataAccess.locationDistrict).toList()),
          _buildDataRow('Ürün Türleri', _tiers.map((t) =>
            t.sponsorshipFeatures.dataAccess.cropTypes).toList()),
          _buildDataRow('Tam Analiz Detayları', _tiers.map((t) =>
            t.sponsorshipFeatures.dataAccess.fullAnalysisDetails).toList()),
          _buildDataRow('Mesajlaşma', _tiers.map((t) =>
            t.sponsorshipFeatures.communication.messagingEnabled).toList()),
          _buildDataRow('Akıllı Linkler', _tiers.map((t) =>
            t.sponsorshipFeatures.smartLinks.enabled).toList()),
        ],
      ),
    );
  }

  DataRow _buildDataRow(String feature, List<bool> values) {
    return DataRow(
      cells: [
        DataCell(Text(feature)),
        ...values.map((v) => DataCell(
          v ? Icon(Icons.check_circle, color: Colors.green, size: 20)
            : Icon(Icons.cancel, color: Colors.grey, size: 20),
        )),
      ],
    );
  }

  Widget _buildContinueButton() {
    return Padding(
      padding: EdgeInsets.all(16),
      child: ElevatedButton(
        onPressed: _selectedTierId == null ? null : _onContinue,
        style: ElevatedButton.styleFrom(
          minimumSize: Size(double.infinity, 50),
          backgroundColor: Colors.green,
        ),
        child: Text('Devam Et', style: TextStyle(fontSize: 16)),
      ),
    );
  }

  void _onContinue() {
    final selectedTier = _tiers.firstWhere((t) => t.id == _selectedTierId);
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => PurchaseDetailsScreen(tier: selectedTier),
      ),
    );
  }
}
```

---

## 📐 Web (Angular/React) - Tier Comparison Grid

### Design Concept

```
┌─────────────────────────────────────────────────────────────┐
│                    Paket Seçimi                             │
│         Size en uygun sponsorluk paketini seçin             │
└─────────────────────────────────────────────────────────────┘

┌────────────┬────────────┬────────────┬────────────┐
│   S Tier   │   M Tier   │   L Tier   │  XL Tier   │
│            │  [POPULAR] │            │            │
├────────────┼────────────┼────────────┼────────────┤
│  50 TRY/ay │ 100 TRY/ay │ 200 TRY/ay │ 500 TRY/ay │
├────────────┼────────────┼────────────┼────────────┤
│ Veri: 30%  │ Veri: 60%  │ Veri: 100% │ Veri: 100% │
│ Logo: 1    │ Logo: 2    │ Logo: 4    │ Logo: 4    │
│ Mesaj: ❌  │ Mesaj: ❌  │ Mesaj: ✅  │ Mesaj: ✅  │
│ Link: ❌   │ Link: ❌   │ Link: ❌   │ Link: ✅   │
├────────────┼────────────┼────────────┼────────────┤
│  [Seç]     │  [Seç]     │  [Seç]     │  [Seç]     │
└────────────┴────────────┴────────────┴────────────┘

▼ Detaylı Özellik Karşılaştırması
```

---

## ✅ Implementation Checklist

### Backend Tasks

- [ ] Create `SponsorshipTierComparisonDto` and related DTOs in `Entities/Dtos/`
- [ ] Create new endpoint `GET /api/v1/sponsorship/tiers-for-purchase` in `SponsorshipController`
- [ ] Implement business logic to map `SubscriptionTier` → `SponsorshipTierComparisonDto`
- [ ] Add tier-specific feature mapping based on tier name (S/M/L/XL)
- [ ] Add unit tests for tier comparison logic
- [ ] Update Swagger documentation with new endpoint

### Frontend Tasks (Mobile)

- [ ] Create `SponsorshipTierComparisonDto` model in Flutter
- [ ] Create `TierSelectionScreen` widget
- [ ] Implement horizontal scrollable tier cards
- [ ] Add feature comparison table
- [ ] Add tier selection state management
- [ ] Integrate with purchase flow
- [ ] Add loading states and error handling
- [ ] Test on multiple screen sizes (phones/tablets)

### Frontend Tasks (Web)

- [ ] Create tier comparison component (Angular/React)
- [ ] Implement responsive grid layout
- [ ] Add tier selection logic
- [ ] Connect to purchase workflow
- [ ] Add animations and transitions
- [ ] Implement accessibility features (ARIA labels)

---

## 🔗 Related Documents

- [Sponsorship System Complete Documentation](./SPONSORSHIP_SYSTEM_COMPLETE_DOCUMENTATION.md)
- [Sponsor Persona Complete Journey Report](./SPONSOR_PERSONA_COMPLETE_JOURNEY_REPORT.md)
- [Mobile Sponsorship Integration Guide](./MOBILE_SPONSORSHIP_INTEGRATION_GUIDE.md)

---

**Status:** ✅ Ready for Development
**Next Steps:** Backend team implements DTOs and endpoint, then frontend teams integrate UI
**Contact:** Backend & Frontend Teams - ZiraAI Platform
