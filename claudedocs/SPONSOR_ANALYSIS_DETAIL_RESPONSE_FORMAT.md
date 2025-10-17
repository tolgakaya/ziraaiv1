# Sponsor Analysis Detail Endpoint - Response Format Documentation

**Endpoint**: `GET /api/v1/sponsorship/analysis/{plantAnalysisId}`
**Date**: 2025-10-16
**Status**: ✅ UPDATED - Now includes tier metadata and sponsor info

---

## 🎯 Overview

Detail endpoint now returns **wrapped response** with tier metadata, matching list endpoint's approach. This provides mobile app with all necessary information for UI logic and feature toggles.

### Response Structure

```json
{
  "success": true,
  "data": {
    "analysis": { /* Filtered PlantAnalysis entity */ },
    "tierMetadata": { /* Tier info, permissions, sponsor info */ }
  }
}
```

---

## 📦 Response Schema

### Root Response
```typescript
{
  success: boolean,
  data: SponsoredAnalysisDetailDto,
  message?: string
}
```

### SponsoredAnalysisDetailDto
```typescript
{
  analysis: PlantAnalysisDetailDto,  // Rich parsed DTO (filtered based on tier: 30%, 60%, 100%)
  tierMetadata: AnalysisTierMetadata  // UI logic and permissions
}
```

### AnalysisTierMetadata
```typescript
{
  tierName: string,              // "S/M", "L", "XL"
  accessPercentage: number,      // 30, 60, 100
  canMessage: boolean,           // Can sponsor message farmer?
  canViewLogo: boolean,          // Can display sponsor logo?
  sponsorInfo: SponsorDisplayInfoDto,
  accessibleFields: AccessibleFieldsInfo
}
```

### SponsorDisplayInfoDto
```typescript
{
  sponsorId: number,
  companyName: string,
  logoUrl?: string,
  websiteUrl?: string
}
```

### AccessibleFieldsInfo
```typescript
{
  // 30% Access
  canViewBasicInfo: boolean,
  canViewHealthScore: boolean,
  canViewImages: boolean,

  // 60% Access
  canViewDetailedHealth: boolean,
  canViewDiseases: boolean,
  canViewNutrients: boolean,
  canViewRecommendations: boolean,
  canViewLocation: boolean,

  // 100% Access
  canViewFarmerContact: boolean,
  canViewFieldData: boolean,
  canViewProcessingData: boolean
}
```

---

## 📊 Complete Response Examples

### Example 1: S/M Tier Sponsor (30% Access)

**Request**:
```bash
GET /api/v1/sponsorship/analysis/52
Authorization: Bearer {S_TIER_SPONSOR_TOKEN}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "analysis": {
      // Core fields (always visible)
      "id": 52,
      "analysisDate": "2025-10-15T19:05:03.863",
      "analysisStatus": "Completed",
      "cropType": "Domates",
      "farmerId": "F165",
      "sponsorId": "S159",
      "sponsorUserId": 159,

      // 30% Access fields ✅ - Rich parsed objects
      "summary": {
        "overallHealthScore": 4,
        "overallHealthDescription": "Orta düzey sağlık"
      },
      "plantIdentification": {
        "species": "Bilinmiyor (muhtemelen Solanaceae familyasından)",
        "variety": "bilinmiyor",
        "growthStage": "vejetatif",
        "ageEstimate": "4-6 haftalık"
      },
      "imageInfo": {
        "imageUrl": "https://iili.io/KkT78Dg.jpg",
        "captureDate": "2025-10-15T19:05:03.863"
      },

      // 60% Access fields ❌ NULL (parsed objects not available)
      "healthAssessment": null,
      "nutrientStatus": null,
      "recommendations": null,
      "pestDisease": null,
      "environmentalStress": null,
      "location": null,
      "latitude": null,
      "longitude": null,

      // 100% Access fields ❌ NULL
      "contactPhone": null,
      "contactEmail": null,
      "fieldId": null,
      "plantingDate": null,
      "processingInfo": null
    },
    "tierMetadata": {
      "tierName": "S/M",
      "accessPercentage": 30,
      "canMessage": true,
      "canViewLogo": true,
      "sponsorInfo": {
        "sponsorId": 159,
        "companyName": "dort tarim",
        "logoUrl": "https://api.ziraai.com/logos/159.png",
        "websiteUrl": "https://dorttarim.com"
      },
      "accessibleFields": {
        // 30% Access ✅
        "canViewBasicInfo": true,
        "canViewHealthScore": true,
        "canViewImages": true,

        // 60% Access ❌
        "canViewDetailedHealth": false,
        "canViewDiseases": false,
        "canViewNutrients": false,
        "canViewRecommendations": false,
        "canViewLocation": false,

        // 100% Access ❌
        "canViewFarmerContact": false,
        "canViewFieldData": false,
        "canViewProcessingData": false
      }
    }
  }
}
```

**Mobile App UI Logic**:
```dart
if (response.tierMetadata.accessibleFields.canViewRecommendations) {
  // Show recommendations section
  showRecommendations(response.analysis.recommendations);
} else {
  // Show upgrade prompt
  showUpgradePrompt("Tavsiyeler için L tier gerekli");
}

if (response.tierMetadata.canMessage) {
  // Show message button
  showMessageButton();
}
```

---

### Example 2: L Tier Sponsor (60% Access)

**Request**:
```bash
GET /api/v1/sponsorship/analysis/52
Authorization: Bearer {L_TIER_SPONSOR_TOKEN}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "analysis": {
      // Core fields
      "id": 52,
      "analysisDate": "2025-10-15T19:05:03.863",
      "analysisStatus": "Completed",
      "cropType": "Domates",

      // 30% Access ✅ - Rich parsed objects
      "summary": {
        "overallHealthScore": 4,
        "overallHealthDescription": "Orta düzey sağlık"
      },
      "plantIdentification": {
        "species": "Bilinmiyor (muhtemelen Solanaceae familyasından)",
        "variety": "bilinmiyor",
        "growthStage": "vejetatif"
      },
      "imageInfo": {
        "imageUrl": "https://iili.io/KkT78Dg.jpg"
      },

      // 60% Access ✅ NOW VISIBLE - Rich parsed objects
      "healthAssessment": {
        "vigorScore": 4,
        "severity": "orta",
        "primaryConcern": "potasyum eksikliği ilişkili yaprak kenarı nekrozu",
        "prognosis": "orta"
      },
      "nutrientStatus": {
        "deficiencies": [
          {"nutrient": "Potasyum", "severity": "Eksik"},
          {"nutrient": "Azot", "severity": "Eksik"}
        ],
        "primaryDeficiency": "potasyum"
      },
      "recommendations": {
        "immediate": [
          {"action": "Toprak ve yaprak analizi yaptırın", "priority": "Yüksek"}
        ]
      },
      "location": "Antalya, Türkiye",
      "latitude": 36.8969,
      "longitude": 30.7133,

      // 100% Access ❌ STILL NULL
      "contactPhone": null,
      "contactEmail": null,
      "fieldId": null,
      "processingInfo": null
    },
    "tierMetadata": {
      "tierName": "L",
      "accessPercentage": 60,
      "canMessage": true,
      "canViewLogo": true,
      "sponsorInfo": {
        "sponsorId": 159,
        "companyName": "dort tarim",
        "logoUrl": "https://api.ziraai.com/logos/159.png",
        "websiteUrl": "https://dorttarim.com"
      },
      "accessibleFields": {
        // 30% Access ✅
        "canViewBasicInfo": true,
        "canViewHealthScore": true,
        "canViewImages": true,

        // 60% Access ✅
        "canViewDetailedHealth": true,
        "canViewDiseases": true,
        "canViewNutrients": true,
        "canViewRecommendations": true,
        "canViewLocation": true,

        // 100% Access ❌
        "canViewFarmerContact": false,
        "canViewFieldData": false,
        "canViewProcessingData": false
      }
    }
  }
}
```

---

### Example 3: XL Tier Sponsor (100% Full Access)

**Request**:
```bash
GET /api/v1/sponsorship/analysis/52
Authorization: Bearer {XL_TIER_SPONSOR_TOKEN}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "analysis": {
      // Core fields
      "id": 52,
      "analysisDate": "2025-10-15T19:05:03.863",
      "analysisStatus": "Completed",
      "cropType": "Domates",

      // 30% Access ✅ - Rich parsed objects
      "summary": {
        "overallHealthScore": 4,
        "overallHealthDescription": "Orta düzey sağlık"
      },
      "plantIdentification": {
        "species": "Bilinmiyor",
        "variety": "bilinmiyor",
        "growthStage": "vejetatif"
      },
      "imageInfo": {
        "imageUrl": "https://iili.io/KkT78Dg.jpg"
      },

      // 60% Access ✅ - Rich parsed objects
      "healthAssessment": {
        "vigorScore": 4,
        "severity": "orta"
      },
      "nutrientStatus": {
        "deficiencies": [...]
      },
      "recommendations": {
        "immediate": [...],
        "shortTerm": [...],
        "preventive": [...]
      },
      "location": "Antalya, Türkiye",

      // 100% Access ✅ ALL FIELDS VISIBLE
      "contactPhone": "+905551234567",
      "contactEmail": "farmer@example.com",
      "fieldId": "FIELD-2024-001",
      "plantingDate": "2024-03-15T00:00:00",
      "expectedHarvestDate": "2024-07-15T00:00:00",
      "lastFertilization": "2024-09-01T00:00:00",
      "lastIrrigation": "2024-10-10T00:00:00",
      "previousTreatments": "[\"Fertilizer NPK\"]",
      "urgencyLevel": "Medium",
      "notes": "Farmer noted spots last week",
      "processingInfo": {
        "aiModel": "gpt-4o-2024-08-06",
        "totalTokens": 1250,
        "costUsd": 0.0375,
        "costTry": 1.25
      },
      "additionalInfo": {...}
    },
    "tierMetadata": {
      "tierName": "XL",
      "accessPercentage": 100,
      "canMessage": true,
      "canViewLogo": true,
      "sponsorInfo": {
        "sponsorId": 159,
        "companyName": "dort tarim",
        "logoUrl": "https://api.ziraai.com/logos/159.png",
        "websiteUrl": "https://dorttarim.com"
      },
      "accessibleFields": {
        // All fields accessible ✅
        "canViewBasicInfo": true,
        "canViewHealthScore": true,
        "canViewImages": true,
        "canViewDetailedHealth": true,
        "canViewDiseases": true,
        "canViewNutrients": true,
        "canViewRecommendations": true,
        "canViewLocation": true,
        "canViewFarmerContact": true,
        "canViewFieldData": true,
        "canViewProcessingData": true
      }
    }
  }
}
```

**Mobile App UI - Full Access**:
```dart
// Show farmer contact section (XL only)
if (response.tierMetadata.accessibleFields.canViewFarmerContact) {
  ContactCard(
    phone: response.analysis.contactPhone,
    email: response.analysis.contactEmail,
  );
}

// Show field management data (XL only)
if (response.tierMetadata.accessibleFields.canViewFieldData) {
  FieldDataSection(
    fieldId: response.analysis.fieldId,
    plantingDate: response.analysis.plantingDate,
    lastFertilization: response.analysis.lastFertilization,
  );
}

// Show processing costs (XL only)
if (response.tierMetadata.accessibleFields.canViewProcessingData) {
  ProcessingMetadata(
    aiModel: response.analysis.aiModel,
    cost: response.analysis.totalCostUsd,
  );
}
```

---

## 🔄 Comparison: List vs Detail Response

### List Endpoint (`/api/v1/sponsorship/analyses`)
- **DTO**: `SponsoredAnalysisSummaryDto` (lightweight)
- **Tier metadata**: At root level of each item
- **Purpose**: Quick overview with pagination
- **Recommendations**: ❌ Excluded (too large)
- **Field count**: ~15-20 fields

**Example**:
```json
{
  "items": [{
    "analysisId": 52,
    "overallHealthScore": 4,
    "tierName": "L",
    "accessPercentage": 60,
    "sponsorInfo": { "companyName": "dort tarim" }
  }]
}
```

### Detail Endpoint (`/api/v1/sponsorship/analysis/{id}`)
- **DTO**: `SponsoredAnalysisDetailDto` (wrapped rich PlantAnalysisDetailDto)
- **Tier metadata**: Separate `tierMetadata` object
- **Purpose**: Complete analysis data with parsed objects
- **Recommendations**: ✅ Included as parsed object (L, XL tiers)
- **Field count**: ~50+ parsed fields (if XL tier)

**Example**:
```json
{
  "data": {
    "analysis": { /* Rich PlantAnalysisDetailDto with parsed objects */ },
    "tierMetadata": { /* Tier info + accessible fields */ }
  }
}
```

---

## 📱 Mobile Integration Guide

### UI Rendering Strategy

```dart
class AnalysisDetailScreen extends StatelessWidget {
  Widget build(BuildContext context, SponsoredAnalysisDetailDto data) {
    return Column(
      children: [
        // Always show core info
        CoreInfoSection(data.analysis),

        // 30% Access sections
        if (data.tierMetadata.accessibleFields.canViewHealthScore)
          HealthScoreSection(data.analysis),

        // 60% Access sections
        if (data.tierMetadata.accessibleFields.canViewRecommendations)
          RecommendationsSection(
            recommendations: jsonDecode(data.analysis.recommendations)
          ),

        if (data.tierMetadata.accessibleFields.canViewNutrients)
          NutrientStatusSection(
            nutrients: jsonDecode(data.analysis.nutrientStatus)
          ),

        // 100% Access sections
        if (data.tierMetadata.accessibleFields.canViewFarmerContact)
          FarmerContactCard(
            phone: data.analysis.contactPhone,
            email: data.analysis.contactEmail,
          ),

        // Feature buttons
        if (data.tierMetadata.canMessage)
          MessageFarmerButton(),

        // Sponsor branding
        if (data.tierMetadata.canViewLogo)
          SponsorLogoSection(data.tierMetadata.sponsorInfo),
      ],
    );
  }
}
```

### Upgrade Prompts

```dart
Widget buildSection(String title, Widget content, bool canView) {
  if (canView) {
    return Section(title: title, child: content);
  } else {
    return UpgradePrompt(
      title: title,
      message: "Bu bölümü görmek için tier yükseltin",
      requiredTier: getRequiredTier(title),
    );
  }
}
```

---

## ✅ Benefits of New Response Structure

### 1. Consistent API Design
- List and detail endpoints now use same metadata structure
- `tierName`, `accessPercentage`, `sponsorInfo` in both

### 2. Explicit Permissions
- `accessibleFields` object clearly shows what UI sections to render
- No need to guess based on null checks

### 3. Future-Proof
- Easy to add new permission flags (`canExportData`, `canShareAnalysis`)
- Tier changes only affect backend, mobile uses permission flags

### 4. Better UX
- Mobile can show upgrade prompts based on `canView*` flags
- Feature discovery: "Unlock recommendations with L tier"

### 5. Simplified Mobile Logic
```dart
// Old approach (error-prone)
if (analysis.recommendations != null && analysis.recommendations.isNotEmpty) {
  showRecommendations(); // Is null because of tier or missing data?
}

// New approach (clear)
if (tierMetadata.accessibleFields.canViewRecommendations) {
  if (analysis.recommendations != null) {
    showRecommendations();
  } else {
    showNoDataMessage(); // Data not available
  }
} else {
  showUpgradePrompt(); // Tier restriction
}
```

---

## 🔗 Related Endpoints

### Get Analysis List
```
GET /api/v1/sponsorship/analyses
Returns: SponsoredAnalysesListResponseDto
Purpose: Paginated summary view
```

### Get Analysis Detail
```
GET /api/v1/sponsorship/analysis/{id}
Returns: SponsoredAnalysisDetailDto
Purpose: Complete analysis data with tier filtering
```

### Send Message to Farmer
```
POST /api/v1/sponsorship/messages
Requires: canMessage = true (M, L, XL tiers)
```

---

## 📚 Related Documentation

- **Tier Restrictions Guide**: `SPONSOR_ANALYSIS_DETAIL_TIER_RESTRICTIONS.md`
- **List Endpoint**: `SPONSORED_ANALYSES_LIST_API_DOCUMENTATION.md`
- **Mobile Integration**: `MOBILE_SPONSORED_ANALYSES_INTEGRATION_GUIDE.md`
- **Sponsorship Business Logic**: `SPONSORSHIP_BUSINESS_LOGIC.md`

---

## 🎯 Summary

Detail endpoint now returns **tier-aware response** with:
- ✅ Rich `PlantAnalysisDetailDto` with parsed objects (same as farmer endpoint)
- ✅ Tier-based filtering applied to parsed DTO (30%, 60%, 100% fields)
- ✅ Tier metadata (`tierName`, `accessPercentage`)
- ✅ Sponsor branding info (`sponsorInfo`)
- ✅ Explicit permission flags (`accessibleFields`)
- ✅ Feature toggles (`canMessage`, `canViewLogo`)

**Key Benefits**:
- Same rich data structure as farmer endpoint (frontend code reuse)
- Parsed JSON objects instead of raw strings
- Consistent tier metadata across list and detail endpoints

**Mobile team can now**:
- Use same data model for farmer and sponsor analysis details
- Parse JSON once in shared code, not per-screen
- Render UI sections based on permission flags
- Show upgrade prompts for restricted features
- Display sponsor branding consistently
- Enable/disable messaging feature correctly
