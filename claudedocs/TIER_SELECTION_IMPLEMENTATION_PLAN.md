# Tier Selection Implementation Plan

**Version:** 1.0
**Date:** 2025-10-12
**Branch:** `feature/sponsor-package-purchase-flow`
**Status:** 📋 Ready to Implement

---

## 📋 Overview

This document provides step-by-step implementation instructions for adding tier comparison functionality to the sponsorship purchase flow.

### Goals

✅ Create new endpoint for sponsor tier comparison
✅ Implement business logic for tier-specific feature mapping
✅ Provide mobile-optimized API response structure
✅ Enable frontend teams to build tier selection UI

---

## 🎯 Implementation Steps

### Step 1: Backend - Create DTOs ✅

**Status:** Complete
**Files Created:**
- `Entities/Dtos/SponsorshipTierComparisonDto.cs`

**DTOs Created:**
- `SponsorshipTierComparisonDto` - Main response DTO
- `SponsorshipFeaturesDto` - Sponsorship-specific features container
- `FarmerDataAccessDto` - Data access permissions by tier
- `LogoVisibilityDto` - Logo display rules
- `CommunicationFeaturesDto` - Messaging capabilities
- `SmartLinksFeaturesDto` - Smart links quota and access
- `SupportFeaturesDto` - Support tier details

---

### Step 2: Backend - Create Service Layer

**File:** `Business/Services/Sponsorship/SponsorshipTierMappingService.cs`

```csharp
using Entities.Concrete;
using Entities.Dtos;
using System.Collections.Generic;
using System.Linq;

namespace Business.Services.Sponsorship
{
    /// <summary>
    /// Maps SubscriptionTier entities to SponsorshipTierComparisonDto
    /// with tier-specific sponsorship features
    /// </summary>
    public interface ISponsorshipTierMappingService
    {
        SponsorshipTierComparisonDto MapToComparisonDto(SubscriptionTier tier);
        List<SponsorshipTierComparisonDto> MapToComparisonDtos(List<SubscriptionTier> tiers);
    }

    public class SponsorshipTierMappingService : ISponsorshipTierMappingService
    {
        public SponsorshipTierComparisonDto MapToComparisonDto(SubscriptionTier tier)
        {
            return new SponsorshipTierComparisonDto
            {
                Id = tier.Id,
                TierName = tier.TierName,
                DisplayName = tier.DisplayName,
                Description = tier.Description,
                MonthlyPrice = tier.MonthlyPrice,
                YearlyPrice = tier.YearlyPrice ?? 0,
                Currency = tier.Currency,
                MinPurchaseQuantity = tier.MinPurchaseQuantity,
                MaxPurchaseQuantity = tier.MaxPurchaseQuantity,
                RecommendedQuantity = tier.RecommendedQuantity,
                DailyRequestLimit = tier.DailyRequestLimit,
                MonthlyRequestLimit = tier.MonthlyRequestLimit,
                SponsorshipFeatures = GetSponsorshipFeatures(tier.TierName),
                IsPopular = tier.TierName == "M" || tier.TierName == "L",
                IsRecommended = tier.TierName == "M",
                DisplayOrder = tier.DisplayOrder
            };
        }

        public List<SponsorshipTierComparisonDto> MapToComparisonDtos(List<SubscriptionTier> tiers)
        {
            return tiers.Select(MapToComparisonDto)
                       .OrderBy(t => t.DisplayOrder)
                       .ToList();
        }

        /// <summary>
        /// Maps tier name to sponsorship-specific features
        /// Based on SPONSOR_PERSONA_COMPLETE_JOURNEY_REPORT.md lines 1186-1245
        /// </summary>
        private SponsorshipFeaturesDto GetSponsorshipFeatures(string tierName)
        {
            return tierName switch
            {
                "S" => new SponsorshipFeaturesDto
                {
                    DataAccessPercentage = 30,
                    DataAccess = new FarmerDataAccessDto
                    {
                        FarmerNameContact = false,
                        LocationCity = true,
                        LocationDistrict = false,
                        LocationCoordinates = false,
                        CropTypes = false,
                        DiseaseCategories = false,
                        FullAnalysisDetails = false,
                        AnalysisImages = false,
                        AiRecommendations = false
                    },
                    LogoVisibility = new LogoVisibilityDto
                    {
                        StartScreen = true,
                        ResultScreen = false,
                        AnalysisDetailsScreen = false,
                        FarmerProfileScreen = false,
                        VisibleScreens = new List<string> { "Start Screen" }
                    },
                    Communication = new CommunicationFeaturesDto
                    {
                        MessagingEnabled = false,
                        ViewConversations = false,
                        MessageRateLimitPerDay = null
                    },
                    SmartLinks = new SmartLinksFeaturesDto
                    {
                        Enabled = false,
                        Quota = 0,
                        AnalyticsAccess = false
                    },
                    Support = new SupportFeaturesDto
                    {
                        PrioritySupport = false,
                        ResponseTimeHours = 48
                    }
                },

                "M" => new SponsorshipFeaturesDto
                {
                    DataAccessPercentage = 60,
                    DataAccess = new FarmerDataAccessDto
                    {
                        FarmerNameContact = false,
                        LocationCity = true,
                        LocationDistrict = true,
                        LocationCoordinates = false,
                        CropTypes = true,
                        DiseaseCategories = true,
                        FullAnalysisDetails = false,
                        AnalysisImages = false,
                        AiRecommendations = false
                    },
                    LogoVisibility = new LogoVisibilityDto
                    {
                        StartScreen = true,
                        ResultScreen = true,
                        AnalysisDetailsScreen = false,
                        FarmerProfileScreen = false,
                        VisibleScreens = new List<string> { "Start Screen", "Result Screen" }
                    },
                    Communication = new CommunicationFeaturesDto
                    {
                        MessagingEnabled = false,
                        ViewConversations = false,
                        MessageRateLimitPerDay = null
                    },
                    SmartLinks = new SmartLinksFeaturesDto
                    {
                        Enabled = false,
                        Quota = 0,
                        AnalyticsAccess = false
                    },
                    Support = new SupportFeaturesDto
                    {
                        PrioritySupport = false,
                        ResponseTimeHours = 48
                    }
                },

                "L" => new SponsorshipFeaturesDto
                {
                    DataAccessPercentage = 100,
                    DataAccess = new FarmerDataAccessDto
                    {
                        FarmerNameContact = true,
                        LocationCity = true,
                        LocationDistrict = true,
                        LocationCoordinates = true,
                        CropTypes = true,
                        DiseaseCategories = true,
                        FullAnalysisDetails = true,
                        AnalysisImages = true,
                        AiRecommendations = true
                    },
                    LogoVisibility = new LogoVisibilityDto
                    {
                        StartScreen = true,
                        ResultScreen = true,
                        AnalysisDetailsScreen = true,
                        FarmerProfileScreen = true,
                        VisibleScreens = new List<string>
                        {
                            "Start Screen", "Result Screen",
                            "Analysis Details", "Farmer Profile"
                        }
                    },
                    Communication = new CommunicationFeaturesDto
                    {
                        MessagingEnabled = true,
                        ViewConversations = true,
                        MessageRateLimitPerDay = 10
                    },
                    SmartLinks = new SmartLinksFeaturesDto
                    {
                        Enabled = false,
                        Quota = 0,
                        AnalyticsAccess = false
                    },
                    Support = new SupportFeaturesDto
                    {
                        PrioritySupport = true,
                        ResponseTimeHours = 24
                    }
                },

                "XL" => new SponsorshipFeaturesDto
                {
                    DataAccessPercentage = 100,
                    DataAccess = new FarmerDataAccessDto
                    {
                        FarmerNameContact = true,
                        LocationCity = true,
                        LocationDistrict = true,
                        LocationCoordinates = true,
                        CropTypes = true,
                        DiseaseCategories = true,
                        FullAnalysisDetails = true,
                        AnalysisImages = true,
                        AiRecommendations = true
                    },
                    LogoVisibility = new LogoVisibilityDto
                    {
                        StartScreen = true,
                        ResultScreen = true,
                        AnalysisDetailsScreen = true,
                        FarmerProfileScreen = true,
                        VisibleScreens = new List<string>
                        {
                            "Start Screen", "Result Screen",
                            "Analysis Details", "Farmer Profile"
                        }
                    },
                    Communication = new CommunicationFeaturesDto
                    {
                        MessagingEnabled = true,
                        ViewConversations = true,
                        MessageRateLimitPerDay = 10
                    },
                    SmartLinks = new SmartLinksFeaturesDto
                    {
                        Enabled = true,
                        Quota = 50,
                        AnalyticsAccess = true
                    },
                    Support = new SupportFeaturesDto
                    {
                        PrioritySupport = true,
                        ResponseTimeHours = 12
                    }
                },

                _ => throw new System.ArgumentException($"Unknown tier name: {tierName}")
            };
        }
    }
}
```

---

### Step 3: Backend - Register Service

**File:** `Business/DependencyResolvers/AutofacBusinessModule.cs`

Add registration:

```csharp
builder.RegisterType<SponsorshipTierMappingService>().As<ISponsorshipTierMappingService>().InstancePerLifetimeScope();
```

---

### Step 4: Backend - Create Controller Endpoint

**File:** `WebAPI/Controllers/SponsorshipController.cs`

Add new endpoint:

```csharp
private readonly ISponsorshipTierMappingService _tierMappingService;

// Add to constructor
public SponsorshipController(
    ISponsorshipService sponsorshipService,
    ISponsorshipTierMappingService tierMappingService, // NEW
    IMediator mediator,
    ILogger<SponsorshipController> logger)
{
    _sponsorshipService = sponsorshipService;
    _tierMappingService = tierMappingService; // NEW
    _mediator = mediator;
    _logger = logger;
}

/// <summary>
/// Get tier comparison for sponsor package purchase selection
/// Returns sponsorship-specific features for UI display
/// </summary>
[HttpGet("tiers-for-purchase")]
[AllowAnonymous] // Allow before authentication for purchase preview
[ProducesResponseType(StatusCodes.Status200OK)]
public async Task<IActionResult> GetTiersForPurchase()
{
    // Get active tiers from subscription tier repository
    var tiers = await _subscriptionTierRepository.GetActiveTiersAsync();

    // Map to sponsorship comparison DTOs
    var comparisonDtos = _tierMappingService.MapToComparisonDtos(tiers.ToList());

    _logger.LogInformation("📊 Retrieved {Count} tier options for purchase selection", comparisonDtos.Count);

    return Ok(new SuccessDataResult<List<SponsorshipTierComparisonDto>>(
        comparisonDtos,
        "Sponsorship tiers retrieved successfully"
    ));
}
```

**Note:** Add `ISubscriptionTierRepository` dependency to controller if not already present.

---

### Step 5: Backend - Add Repository Dependency

If `SponsorshipController` doesn't have `ISubscriptionTierRepository`, add it:

```csharp
private readonly ISubscriptionTierRepository _subscriptionTierRepository;

public SponsorshipController(
    ISponsorshipService sponsorshipService,
    ISponsorshipTierMappingService tierMappingService,
    ISubscriptionTierRepository subscriptionTierRepository, // NEW
    IMediator mediator,
    ILogger<SponsorshipController> logger)
{
    _sponsorshipService = sponsorshipService;
    _tierMappingService = tierMappingService;
    _subscriptionTierRepository = subscriptionTierRepository; // NEW
    _mediator = mediator;
    _logger = logger;
}
```

---

### Step 6: Testing - Postman Request

**Endpoint:** `GET {{baseUrl}}/api/v1/sponsorship/tiers-for-purchase`
**Authorization:** None (AllowAnonymous)
**Headers:**
```
x-dev-arch-version: 1.0
Accept: application/json
```

**Expected Response:**
```json
{
  "success": true,
  "message": "Sponsorship tiers retrieved successfully",
  "data": [
    {
      "id": 1,
      "tierName": "S",
      "displayName": "Small - Basic Visibility",
      "monthlyPrice": 50.00,
      "sponsorshipFeatures": {
        "dataAccessPercentage": 30,
        "dataAccess": { "farmerNameContact": false, "locationCity": true, ... },
        "logoVisibility": { "startScreen": true, "visibleScreens": ["Start Screen"] },
        "communication": { "messagingEnabled": false },
        "smartLinks": { "enabled": false, "quota": 0 }
      },
      "isPopular": false,
      "isRecommended": false
    },
    // ... M, L, XL tiers
  ]
}
```

---

### Step 7: Frontend Integration (Mobile - Flutter)

**Service Layer:**

```dart
// lib/services/sponsorship_service.dart

class SponsorshipService {
  Future<List<SponsorshipTierComparison>> getTiersForPurchase() async {
    try {
      final response = await _apiClient.get('/sponsorship/tiers-for-purchase');

      if (response['success'] == true) {
        final List<dynamic> data = response['data'];
        return data.map((json) => SponsorshipTierComparison.fromJson(json)).toList();
      }

      throw Exception('Failed to load tiers');
    } catch (e) {
      print('Error fetching tiers for purchase: $e');
      rethrow;
    }
  }
}
```

**Model:**

```dart
// lib/models/sponsorship_tier_comparison.dart

class SponsorshipTierComparison {
  final int id;
  final String tierName;
  final String displayName;
  final String description;
  final double monthlyPrice;
  final double yearlyPrice;
  final String currency;
  final int minPurchaseQuantity;
  final int maxPurchaseQuantity;
  final int recommendedQuantity;
  final int dailyRequestLimit;
  final int monthlyRequestLimit;
  final SponsorshipFeatures sponsorshipFeatures;
  final bool isPopular;
  final bool isRecommended;
  final int displayOrder;

  SponsorshipTierComparison({
    required this.id,
    required this.tierName,
    required this.displayName,
    required this.description,
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

  factory SponsorshipTierComparison.fromJson(Map<String, dynamic> json) {
    return SponsorshipTierComparison(
      id: json['id'],
      tierName: json['tierName'],
      displayName: json['displayName'],
      description: json['description'] ?? '',
      monthlyPrice: (json['monthlyPrice'] as num).toDouble(),
      yearlyPrice: (json['yearlyPrice'] as num).toDouble(),
      currency: json['currency'],
      minPurchaseQuantity: json['minPurchaseQuantity'],
      maxPurchaseQuantity: json['maxPurchaseQuantity'],
      recommendedQuantity: json['recommendedQuantity'],
      dailyRequestLimit: json['dailyRequestLimit'],
      monthlyRequestLimit: json['monthlyRequestLimit'],
      sponsorshipFeatures: SponsorshipFeatures.fromJson(json['sponsorshipFeatures']),
      isPopular: json['isPopular'],
      isRecommended: json['isRecommended'],
      displayOrder: json['displayOrder'],
    );
  }
}

class SponsorshipFeatures {
  final int dataAccessPercentage;
  final FarmerDataAccess dataAccess;
  final LogoVisibility logoVisibility;
  final CommunicationFeatures communication;
  final SmartLinksFeatures smartLinks;
  final SupportFeatures support;

  SponsorshipFeatures({
    required this.dataAccessPercentage,
    required this.dataAccess,
    required this.logoVisibility,
    required this.communication,
    required this.smartLinks,
    required this.support,
  });

  factory SponsorshipFeatures.fromJson(Map<String, dynamic> json) {
    return SponsorshipFeatures(
      dataAccessPercentage: json['dataAccessPercentage'],
      dataAccess: FarmerDataAccess.fromJson(json['dataAccess']),
      logoVisibility: LogoVisibility.fromJson(json['logoVisibility']),
      communication: CommunicationFeatures.fromJson(json['communication']),
      smartLinks: SmartLinksFeatures.fromJson(json['smartLinks']),
      support: SupportFeatures.fromJson(json['support']),
    );
  }
}

// ... Continue with other nested classes (FarmerDataAccess, LogoVisibility, etc.)
```

**UI Screen:**

See `TIER_SELECTION_UI_SPECIFICATION.md` for complete Flutter widget implementation.

---

## 📋 Implementation Checklist

### Backend
- [x] Create DTOs (`SponsorshipTierComparisonDto` and related)
- [ ] Create service layer (`SponsorshipTierMappingService`)
- [ ] Register service in Autofac
- [ ] Add controller endpoint (`GET /api/v1/sponsorship/tiers-for-purchase`)
- [ ] Add repository dependency to controller
- [ ] Test endpoint with Postman
- [ ] Update Swagger documentation
- [ ] Add unit tests for mapping service

### Frontend (Mobile)
- [ ] Create Dart models for all DTOs
- [ ] Update `SponsorshipService` with `getTiersForPurchase()` method
- [ ] Create `TierSelectionScreen` widget
- [ ] Implement horizontal scrollable tier cards
- [ ] Add feature comparison table
- [ ] Integrate with purchase flow
- [ ] Test on multiple devices
- [ ] Add error handling and loading states

### Frontend (Web)
- [ ] Create TypeScript interfaces for DTOs
- [ ] Create tier comparison component
- [ ] Implement responsive grid layout
- [ ] Add tier selection logic
- [ ] Connect to purchase workflow

---

## 🧪 Testing Strategy

### Unit Tests

```csharp
[Fact]
public void MapToComparisonDto_STier_Returns30PercentDataAccess()
{
    // Arrange
    var service = new SponsorshipTierMappingService();
    var tier = new SubscriptionTier
    {
        Id = 1,
        TierName = "S",
        DisplayName = "Small",
        MonthlyPrice = 50
    };

    // Act
    var result = service.MapToComparisonDto(tier);

    // Assert
    Assert.Equal(30, result.SponsorshipFeatures.DataAccessPercentage);
    Assert.False(result.SponsorshipFeatures.DataAccess.FarmerNameContact);
    Assert.True(result.SponsorshipFeatures.DataAccess.LocationCity);
    Assert.False(result.SponsorshipFeatures.Communication.MessagingEnabled);
}

[Fact]
public void MapToComparisonDto_XLTier_EnablesSmartLinks()
{
    // Arrange
    var service = new SponsorshipTierMappingService();
    var tier = new SubscriptionTier { TierName = "XL" };

    // Act
    var result = service.MapToComparisonDto(tier);

    // Assert
    Assert.True(result.SponsorshipFeatures.SmartLinks.Enabled);
    Assert.Equal(50, result.SponsorshipFeatures.SmartLinks.Quota);
}
```

### Integration Tests

```csharp
[Fact]
public async Task GetTiersForPurchase_ReturnsAllActiveTiers()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/v1/sponsorship/tiers-for-purchase");

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<ApiResponse<List<SponsorshipTierComparisonDto>>>(content);

    Assert.True(result.Success);
    Assert.Equal(4, result.Data.Count); // S, M, L, XL
    Assert.Contains(result.Data, t => t.TierName == "XL" && t.SponsorshipFeatures.SmartLinks.Enabled);
}
```

---

## 🔗 Related Documents

- [Tier Selection UI Specification](./TIER_SELECTION_UI_SPECIFICATION.md)
- [Sponsorship System Complete Documentation](./SPONSORSHIP_SYSTEM_COMPLETE_DOCUMENTATION.md)
- [Sponsor Persona Complete Journey Report](./SPONSOR_PERSONA_COMPLETE_JOURNEY_REPORT.md)

---

**Status:** 📋 Ready to Implement
**Next Steps:**
1. Create `SponsorshipTierMappingService`
2. Register service in Autofac
3. Add controller endpoint
4. Test with Postman
5. Hand off to frontend teams

**Contact:** Backend Team - ZiraAI Platform
