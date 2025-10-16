# Mobile Tier Selection Integration Guide

**Version:** 1.0
**Date:** 2025-10-12
**Audience:** Mobile Development Team (Flutter)

---

## Endpoint

**GET** `/api/v1/sponsorship/tiers-for-purchase`

- **Authentication:** None required (`[AllowAnonymous]`)
- **Method:** GET
- **Base URL:** `https://your-api-domain.com`

---

## Request Example

```dart
// No authentication needed
final response = await http.get(
  Uri.parse('$baseUrl/api/v1/sponsorship/tiers-for-purchase'),
  headers: {
    'x-dev-arch-version': '1.0',
    'Accept': 'application/json',
  },
);
```

---

## Response Structure

```json
{
  "data": [
    {
      "id": 2,
      "tierName": "S",
      "displayName": "Small - Basic Visibility",
      "description": "Entry-level sponsorship package",
      "monthlyPrice": 50.00,
      "yearlyPrice": 540.00,
      "currency": "TRY",
      "minPurchaseQuantity": 10,
      "maxPurchaseQuantity": 50,
      "recommendedQuantity": 25,
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
      "id": 3,
      "tierName": "M",
      "displayName": "Medium - Enhanced Visibility",
      "monthlyPrice": 100.00,
      "sponsorshipFeatures": {
        "dataAccessPercentage": 60,
        "logoVisibility": {
          "visibleScreens": ["Start Screen", "Result Screen"]
        }
      },
      "isPopular": true,
      "isRecommended": true
    },
    {
      "id": 4,
      "tierName": "L",
      "displayName": "Large - Premium Access",
      "monthlyPrice": 200.00,
      "sponsorshipFeatures": {
        "dataAccessPercentage": 100,
        "communication": {
          "messagingEnabled": true,
          "messageRateLimitPerDay": 10
        },
        "logoVisibility": {
          "visibleScreens": ["Start Screen", "Result Screen", "Analysis Details", "Farmer Profile"]
        }
      },
      "isPopular": true
    },
    {
      "id": 5,
      "tierName": "XL",
      "displayName": "XL - Premium + Smart Links",
      "monthlyPrice": 500.00,
      "sponsorshipFeatures": {
        "dataAccessPercentage": 100,
        "communication": {
          "messagingEnabled": true
        },
        "smartLinks": {
          "enabled": true,
          "quota": 50,
          "analyticsAccess": true
        }
      }
    }
  ],
  "success": true,
  "message": "Sponsorship tiers retrieved successfully"
}
```

---

## Dart Models

```dart
class SponsorshipTierComparisonDto {
  final int id;
  final String tierName; // "S", "M", "L", "XL"
  final String displayName;
  final String? description;
  final double monthlyPrice;
  final double yearlyPrice;
  final String currency;
  final int minPurchaseQuantity;
  final int maxPurchaseQuantity;
  final int recommendedQuantity;
  final int dailyRequestLimit;
  final int monthlyRequestLimit;
  final SponsorshipFeaturesDto sponsorshipFeatures;
  final bool isPopular;
  final bool isRecommended;
  final int displayOrder;

  SponsorshipTierComparisonDto({
    required this.id,
    required this.tierName,
    required this.displayName,
    this.description,
    required this.monthlyPrice,
    required this.yearlyPrice,
    required this.currency,
    required this.minPurchaseQuantity,
    required this.maxPurchaseQuantity,
    required this.recommendedQuantity,
    required this.dailyRequestLimit,
    required this.monthlyRequestLimit,
    required this.sponsorshipFeatures,
    required this.isPopular,
    required this.isRecommended,
    required this.displayOrder,
  });

  factory SponsorshipTierComparisonDto.fromJson(Map<String, dynamic> json) {
    return SponsorshipTierComparisonDto(
      id: json['id'],
      tierName: json['tierName'],
      displayName: json['displayName'],
      description: json['description'],
      monthlyPrice: (json['monthlyPrice'] as num).toDouble(),
      yearlyPrice: (json['yearlyPrice'] as num).toDouble(),
      currency: json['currency'],
      minPurchaseQuantity: json['minPurchaseQuantity'],
      maxPurchaseQuantity: json['maxPurchaseQuantity'],
      recommendedQuantity: json['recommendedQuantity'],
      dailyRequestLimit: json['dailyRequestLimit'],
      monthlyRequestLimit: json['monthlyRequestLimit'],
      sponsorshipFeatures: SponsorshipFeaturesDto.fromJson(json['sponsorshipFeatures']),
      isPopular: json['isPopular'],
      isRecommended: json['isRecommended'],
      displayOrder: json['displayOrder'],
    );
  }
}

class SponsorshipFeaturesDto {
  final int dataAccessPercentage; // 30, 60, 100
  final FarmerDataAccessDto dataAccess;
  final LogoVisibilityDto logoVisibility;
  final CommunicationFeaturesDto communication;
  final SmartLinksFeaturesDto smartLinks;
  final SupportFeaturesDto support;

  SponsorshipFeaturesDto({
    required this.dataAccessPercentage,
    required this.dataAccess,
    required this.logoVisibility,
    required this.communication,
    required this.smartLinks,
    required this.support,
  });

  factory SponsorshipFeaturesDto.fromJson(Map<String, dynamic> json) {
    return SponsorshipFeaturesDto(
      dataAccessPercentage: json['dataAccessPercentage'],
      dataAccess: FarmerDataAccessDto.fromJson(json['dataAccess']),
      logoVisibility: LogoVisibilityDto.fromJson(json['logoVisibility']),
      communication: CommunicationFeaturesDto.fromJson(json['communication']),
      smartLinks: SmartLinksFeaturesDto.fromJson(json['smartLinks']),
      support: SupportFeaturesDto.fromJson(json['support']),
    );
  }
}

class LogoVisibilityDto {
  final bool startScreen;
  final bool resultScreen;
  final bool analysisDetailsScreen;
  final bool farmerProfileScreen;
  final List<String> visibleScreens;

  LogoVisibilityDto({
    required this.startScreen,
    required this.resultScreen,
    required this.analysisDetailsScreen,
    required this.farmerProfileScreen,
    required this.visibleScreens,
  });

  factory LogoVisibilityDto.fromJson(Map<String, dynamic> json) {
    return LogoVisibilityDto(
      startScreen: json['startScreen'],
      resultScreen: json['resultScreen'],
      analysisDetailsScreen: json['analysisDetailsScreen'],
      farmerProfileScreen: json['farmerProfileScreen'],
      visibleScreens: List<String>.from(json['visibleScreens']),
    );
  }
}

class CommunicationFeaturesDto {
  final bool messagingEnabled;
  final bool viewConversations;
  final int? messageRateLimitPerDay; // null for disabled tiers

  CommunicationFeaturesDto({
    required this.messagingEnabled,
    required this.viewConversations,
    this.messageRateLimitPerDay,
  });

  factory CommunicationFeaturesDto.fromJson(Map<String, dynamic> json) {
    return CommunicationFeaturesDto(
      messagingEnabled: json['messagingEnabled'],
      viewConversations: json['viewConversations'],
      messageRateLimitPerDay: json['messageRateLimitPerDay'],
    );
  }
}

class SmartLinksFeaturesDto {
  final bool enabled;
  final int quota; // 0 if disabled, 50 for XL tier
  final bool analyticsAccess;

  SmartLinksFeaturesDto({
    required this.enabled,
    required this.quota,
    required this.analyticsAccess,
  });

  factory SmartLinksFeaturesDto.fromJson(Map<String, dynamic> json) {
    return SmartLinksFeaturesDto(
      enabled: json['enabled'],
      quota: json['quota'],
      analyticsAccess: json['analyticsAccess'],
    );
  }
}
```

---

## Usage in Flutter

```dart
// Service method
Future<List<SponsorshipTierComparisonDto>> getTiersForPurchase() async {
  try {
    final response = await http.get(
      Uri.parse('$baseUrl/api/v1/sponsorship/tiers-for-purchase'),
      headers: {
        'x-dev-arch-version': '1.0',
        'Accept': 'application/json',
      },
    );

    if (response.statusCode == 200) {
      final Map<String, dynamic> jsonResponse = json.decode(response.body);
      final List<dynamic> data = jsonResponse['data'];
      return data.map((json) => SponsorshipTierComparisonDto.fromJson(json)).toList();
    } else {
      throw Exception('Failed to load tiers');
    }
  } catch (e) {
    print('Error fetching tiers: $e');
    rethrow;
  }
}

// UI usage
Widget buildTierCards() {
  return FutureBuilder<List<SponsorshipTierComparisonDto>>(
    future: getTiersForPurchase(),
    builder: (context, snapshot) {
      if (snapshot.hasData) {
        return ListView.builder(
          itemCount: snapshot.data!.length,
          itemBuilder: (context, index) {
            final tier = snapshot.data![index];
            return TierCard(
              tier: tier,
              isPopular: tier.isPopular,
              isRecommended: tier.isRecommended,
              onTap: () => selectTier(tier),
            );
          },
        );
      } else if (snapshot.hasError) {
        return ErrorWidget(snapshot.error.toString());
      }
      return CircularProgressIndicator();
    },
  );
}
```

---

## Key Features by Tier

| Feature | S | M | L | XL |
|---------|---|---|---|-----|
| Data Access | 30% | 60% | 100% | 100% |
| Logo Screens | 1 | 2 | 4 | 4 |
| Messaging | ❌ | ❌ | ✅ | ✅ |
| Smart Links | ❌ | ❌ | ❌ | ✅ (50 quota) |
| Priority Support | ❌ | ❌ | ✅ (24h) | ✅ (12h) |

---

## UI Guidelines

1. **Highlight Popular Tiers**: Show badge on `isPopular: true` tiers (M, L)
2. **Recommend M Tier**: Default selection when `isRecommended: true`
3. **Display Order**: Sort by `displayOrder` field (ascending)
4. **Feature Comparison**: Use `sponsorshipFeatures` to show capabilities
5. **Smart Links Badge**: Only XL tier has `smartLinks.enabled: true`

---

## Testing

```bash
# Test endpoint (Railway deployment)
curl -X GET "https://your-railway-url/api/v1/sponsorship/tiers-for-purchase" \
  -H "x-dev-arch-version: 1.0" \
  -H "Accept: application/json"
```

---

## Questions?

Contact backend team or refer to:
- Full API documentation: Swagger UI at `/swagger`
- Postman collection: `ZiraAI_Complete_API_Collection_v6.1.json`

**Status:** ✅ Ready for Integration
**Branch:** `feature/sponsor-package-purchase-flow`
