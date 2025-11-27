# Multi-Image Plant Analysis Integration Guide

**Version:** 1.0
**Last Updated:** 2025-11-27
**Audiences:** Mobile (Flutter), Web (Angular), Backend Integrators

---

## Table of Contents

1. [Overview](#overview)
2. [Feature Purpose & Benefits](#feature-purpose--benefits)
3. [Single vs Multi-Image Comparison](#single-vs-multi-image-comparison)
4. [API Endpoints Reference](#api-endpoints-reference)
5. [Request/Response Structures](#requestresponse-structures)
6. [Authentication & Authorization](#authentication--authorization)
7. [Migration Guide](#migration-guide)
8. [Code Examples](#code-examples)
9. [SignalR Real-Time Notifications](#signalr-real-time-notifications)
10. [Error Handling](#error-handling)

---

## Overview

The Multi-Image Plant Analysis feature enhances the existing single-image analysis by supporting **up to 5 images per analysis request**:

- **1 Main Image** (required)
- **4 Optional Detail Images:**
  - Leaf Top View
  - Leaf Bottom View
  - Plant Overview
  - Root System

This provides AI with comprehensive visual context for more accurate diagnoses and recommendations.

### Key Design Decisions

✅ **Backward Compatible** - No breaking changes to existing endpoints
✅ **Same Endpoint Strategy** - No new detail endpoints, extended response fields
✅ **SignalR Support** - Multi-image analyses trigger same notifications as single-image
✅ **Async Processing** - Separate queue (`plant-analysis-multi-image-requests/results`)

---

## Feature Purpose & Benefits

### For Farmers
- **More Accurate Diagnoses** - AI analyzes multiple angles and plant parts
- **Comprehensive Insights** - Leaf top/bottom comparison reveals hidden issues
- **Root Analysis** - Underground problems visible through root images
- **Better Recommendations** - Context-aware treatment suggestions

### For Sponsors
- **Enhanced Value** - Demonstrate advanced AI capabilities to sponsored farmers
- **Detailed Reports** - More comprehensive analysis data for sponsorship ROI
- **Professional Imagery** - Multiple images showcase thorough inspection process

### Technical Benefits
- **URL-Based Processing** - 99.6% token reduction vs base64 (S3/CloudFront integration)
- **Separate Queue** - Multi-image processing isolated from single-image workflow
- **Incremental Adoption** - Existing clients continue working without changes

---

## Single vs Multi-Image Comparison

| Feature | Single-Image Analysis | Multi-Image Analysis |
|---------|----------------------|---------------------|
| **Endpoint** | `POST /api/v1/plantanalyses/analyze-async` | `POST /api/v1/plantanalyses/analyze-multi-async` |
| **Required Images** | 1 (Main image) | 1 (Main image) |
| **Optional Images** | None | 4 (LeafTop, LeafBottom, PlantOverview, Root) |
| **Request DTO** | `PlantAnalysisRequestDto` | `PlantAnalysisMultiImageRequestDto` |
| **Queue** | `plant-analysis-requests` | `plant-analysis-multi-image-requests` |
| **Processing Time** | 2-5 minutes | 3-7 minutes |
| **Detail Endpoint** | `GET /api/v1/plantanalyses/{id}/detail` | Same endpoint (backward compatible) |
| **Response Fields** | Standard `ImageMetadata` | Extended `ImageMetadata` with multi-image fields |
| **SignalR Notification** | ✅ Supported | ✅ Supported (same event) |
| **Subscription Cost** | 1 analysis quota | 1 analysis quota (same as single) |

---

## API Endpoints Reference

All endpoints verified from `WebAPI/Controllers/PlantAnalysesController.cs`

### Analysis Submission Endpoints

#### 1️⃣ Single-Image Async Analysis
```
POST /api/v1/plantanalyses/analyze-async
Authorization: Bearer {jwt_token}
Roles: Farmer, Admin
```

**Request Body:** `PlantAnalysisRequestDto`
**Response:** HTTP 202 Accepted with analysis tracking info

#### 2️⃣ Multi-Image Async Analysis
```
POST /api/v1/plantanalyses/analyze-multi-async
Authorization: Bearer {jwt_token}
Roles: Farmer, Admin
```

**Request Body:** `PlantAnalysisMultiImageRequestDto`
**Response:** HTTP 202 Accepted with analysis tracking info

#### 3️⃣ Synchronous Analysis (Deprecated - Legacy Support)
```
POST /api/v1/plantanalyses/analyze
Authorization: Bearer {jwt_token}
Roles: Farmer, Admin
```

**⚠️ Note:** Synchronous endpoint does not support multi-image. Use async endpoints for production.

---

### Analysis Retrieval Endpoints

#### 4️⃣ Get Analysis by ID (Farmer/Sponsor)
```
GET /api/v1/plantanalyses/{id}
Authorization: Bearer {jwt_token}
Roles: Farmer, Admin, Sponsor (if sponsored analysis)
```

**Response:** `PlantAnalysisResponseDto` (summary view)

#### 5️⃣ Get Detailed Analysis by ID (Farmer/Sponsor)
```
GET /api/v1/plantanalyses/{id}/detail
Authorization: Bearer {jwt_token}
Roles: Farmer, Admin, Sponsor (if sponsored analysis)
```

**Response:** `PlantAnalysisDetailDto` (full structured analysis)

**🆕 Multi-Image Support:**
- Response includes `ImageMetadata` with optional multi-image fields
- Single-image analyses: Multi-image fields are `null`
- Multi-image analyses: Populated with additional image URLs and metadata

#### 6️⃣ Get My Analyses (Farmer)
```
GET /api/v1/plantanalyses/my-analyses
Authorization: Bearer {jwt_token}
Roles: Farmer, Admin
```

**Response:** `List<PlantAnalysisResponseDto>`

#### 7️⃣ Get Paginated Analyses (Farmer - Mobile Optimized)
```
GET /api/v1/plantanalyses/list
Authorization: Bearer {jwt_token}
Roles: Farmer
Query Parameters:
  - page: int (default: 1)
  - pageSize: int (default: 20, max: 50)
  - status: string (optional: "Completed", "Processing", "Failed")
  - fromDate: DateTime (optional: YYYY-MM-DD)
  - toDate: DateTime (optional: YYYY-MM-DD)
  - cropType: string (optional)
  - sortBy: string (default: "date", options: "date", "status", "cropType")
  - sortOrder: string (default: "desc", options: "asc", "desc")
```

**Response:** `PlantAnalysisListResponseDto` (paginated with metadata)

#### 8️⃣ Get Sponsored Analyses (Admin/Sponsor)
```
GET /api/v1/plantanalyses/sponsored-analyses
Authorization: Bearer {jwt_token}
Roles: Admin
```

**Response:** `List<PlantAnalysisResponseDto>` (all analyses sponsored by current sponsor)

#### 9️⃣ Get All Analyses (Admin Only)
```
GET /api/v1/plantanalyses
Authorization: Bearer {jwt_token}
Roles: Admin
```

**Response:** `List<PlantAnalysisResponseDto>` (all analyses in system)

---

## Request/Response Structures

### Request DTOs

#### PlantAnalysisRequestDto (Single-Image)

```json
{
  "image": "data:image/jpeg;base64,/9j/4AAQSkZJRg...",  // REQUIRED: Base64 data URI

  // Auto-populated by server (DO NOT send from client)
  "userId": null,
  "farmerId": null,
  "sponsorId": null,
  "sponsorUserId": null,
  "sponsorshipCodeId": null,

  // Optional metadata (all nullable)
  "fieldId": "FIELD-001",
  "cropType": "Tomato",
  "location": "Greenhouse A1",
  "gpsCoordinates": {
    "lat": 39.9334,
    "lng": 32.8597
  },
  "altitude": 850,
  "plantingDate": "2025-01-15T00:00:00Z",
  "expectedHarvestDate": "2025-04-15T00:00:00Z",
  "lastFertilization": "2025-02-01T00:00:00Z",
  "lastIrrigation": "2025-02-25T00:00:00Z",
  "previousTreatments": [
    "Organic fertilizer applied 3 weeks ago",
    "Pest control spray 1 week ago"
  ],
  "weatherConditions": "Partly cloudy",
  "temperature": 22.5,
  "humidity": 65,
  "soilType": "Loamy",
  "urgencyLevel": "Medium",
  "notes": "Yellow spots appearing on lower leaves",
  "contactInfo": {
    "phone": "+905551234567",
    "email": "farmer@example.com"
  },
  "additionalInfo": {
    "irrigationMethod": "Drip",
    "greenhouse": true,
    "organicCertified": false
  }
}
```

**Image Validation:**
- Max size: 500 MB (before optimization)
- Target size: 0.25 MB (after server-side optimization)
- Supported formats: JPEG, PNG, GIF, WebP, BMP, SVG, TIFF
- Encoding: Base64 data URI format (`data:image/{format};base64,{data}`)

#### PlantAnalysisMultiImageRequestDto (Multi-Image)

```json
{
  "image": "data:image/jpeg;base64,/9j/4AAQSkZJRg...",  // REQUIRED: Main image

  // 🆕 Optional additional images (all nullable)
  "leafTopImage": "data:image/jpeg;base64,/9j/4AAQSkZJRg...",
  "leafBottomImage": "data:image/jpeg;base64,/9j/4AAQSkZJRg...",
  "plantOverviewImage": "data:image/jpeg;base64,/9j/4AAQSkZJRg...",
  "rootImage": "data:image/jpeg;base64,/9j/4AAQSkZJRg...",

  // All other fields identical to PlantAnalysisRequestDto
  "fieldId": "FIELD-001",
  "cropType": "Tomato",
  // ... (same as single-image request)
}
```

**Multi-Image Validation:**
- Each image independently validated (max 500 MB, optimized to ~0.25 MB)
- Only `image` field is required, others optional
- Minimum 1 image, maximum 5 images per request

---

### Response DTOs

#### Analysis Submission Response (HTTP 202 Accepted)

**Single-Image Response:**
```json
{
  "success": true,
  "message": "Plant analysis has been queued for processing",
  "analysis_id": 12345,
  "estimated_processing_time": "2-5 minutes",
  "status_check_endpoint": "/api/plantanalyses/status/12345",
  "notification_info": "You will receive a notification when analysis is complete"
}
```

**Multi-Image Response:**
```json
{
  "success": true,
  "message": "Multi-image plant analysis queued with 5 image(s)",
  "analysis_id": 12346,
  "image_count": 5,  // 🆕 Number of images submitted
  "estimated_processing_time": "3-7 minutes",
  "status_check_endpoint": "/api/plantanalyses/status/12346",
  "notification_info": "You will receive a notification when analysis is complete"
}
```

#### PlantAnalysisResponseDto (Summary View)

Returned by: `GET /api/v1/plantanalyses/{id}`, `GET /api/v1/plantanalyses/my-analyses`

```json
{
  "id": 12346,
  "analysisId": "async_multi_analysis_20251127_114429_a502d603",
  "imagePath": "/uploads/plant-images/12346.jpg",
  "imageUrl": "https://cdn-staging.ziraai.com/plant-images/12346.jpg",
  "analysisDate": "2025-11-27T11:44:30.665Z",
  "status": "Completed",
  "userId": 190,
  "farmerId": "F190",
  "sponsorId": "S159",
  "sponsorUserId": 159,
  "sponsorshipCodeId": 42,

  // Core metadata
  "location": "Greenhouse A1",
  "gpsCoordinates": {
    "lat": 39.9334,
    "lng": 32.8597
  },
  "fieldId": "FIELD-001",
  "cropType": "Tomato",

  // Summary-level analysis (structured DTOs)
  "plantIdentification": {
    "species": "Domates (Solanum lycopersicum)",
    "variety": "bilinmiyor",
    "growthStage": "vejetatif",
    "confidence": 90
  },
  "healthAssessment": {
    "vigorScore": 4,
    "severity": "orta"
  },
  "summary": {
    "overallHealthScore": 4,
    "primaryConcern": "magnezyum eksikliği ile ilişkili yaprak klorozu",
    "prognosis": "orta",
    "estimatedYieldImpact": "orta"
  },

  // 🆕 Multi-image metadata (backward compatible)
  "imageMetadata": {
    "source": "url",
    "imageUrl": "https://cdn-staging.ziraai.com/plant-images/12346.jpg",
    "hasImageExtension": true,
    "uploadTimestamp": "2025-11-27T11:44:30.665Z",

    // 🆕 Multi-image fields (null for single-image analyses)
    "totalImages": 5,
    "imagesProvided": ["main", "leaf_top", "leaf_bottom", "plant_overview", "root"],
    "hasLeafTop": true,
    "hasLeafBottom": true,
    "hasPlantOverview": true,
    "hasRoot": true,
    "leafTopImageUrl": "https://cdn-staging.ziraai.com/plant-images/12346-leaf-top.jpg",
    "leafBottomImageUrl": "https://cdn-staging.ziraai.com/plant-images/12346-leaf-bottom.jpg",
    "plantOverviewImageUrl": "https://cdn-staging.ziraai.com/plant-images/12346-plant-overview.jpg",
    "rootImageUrl": "https://cdn-staging.ziraai.com/plant-images/12346-root.jpg"
  },

  "farmerFriendlySummary": "Yapraklarda damarlar daha yeşil kalarak arada sararma ve bazı kahverengi lezyonlar var. Bu büyük olasılıkla magnezyum eksikliği..."
}
```

**Backward Compatibility:**
- Existing clients ignore unknown/null fields automatically
- Single-image analyses: All multi-image fields are `null`
- Multi-image analyses: Multi-image fields populated with data

#### PlantAnalysisDetailDto (Full Structured Analysis)

Returned by: `GET /api/v1/plantanalyses/{id}/detail`

```json
{
  "id": 12346,
  "analysisId": "async_multi_analysis_20251127_114429_a502d603",
  "analysisDate": "2025-11-27T11:44:30.665Z",
  "analysisStatus": "Completed",

  // User & Sponsor Info
  "userId": 190,
  "farmerId": "F190",
  "sponsorId": "S159",
  "sponsorUserId": 159,
  "sponsorshipCodeId": 42,

  // Location
  "location": "Greenhouse A1",
  "latitude": 39.9334,
  "longitude": 32.8597,
  "altitude": 850,

  // Field & Crop
  "fieldId": "FIELD-001",
  "cropType": "Tomato",
  "plantingDate": "2025-01-15T00:00:00Z",
  "expectedHarvestDate": "2025-04-15T00:00:00Z",

  // Plant Identification
  "plantIdentification": {
    "species": "Domates (Solanum lycopersicum)",
    "variety": "bilinmiyor",
    "growthStage": "vejetatif",
    "confidence": 90,
    "identifyingFeatures": [
      "parçalı (loblu) yaprak yapısı",
      "ince sap ve domates tipi yaprak damar yapısı"
    ],
    "visibleParts": ["yapraklar", "gövde"]
  },

  // Health Assessment
  "healthAssessment": {
    "vigorScore": 4,
    "leafColor": "yapraklarda interveinal sararma ve bazı alanlarda koyu nekrotik lekeler",
    "leafTexture": "bir kısmı ince, kıvrılmış ve kısmen kuruymuş görünüyor",
    "growthPattern": "anormal - yapraklarda düzensiz sararma ve nekrozla birlikte normalin altında bitki canlılığı",
    "structuralIntegrity": "orta - gövde sağlam görünse de yaprak dokusunda bozulma var",
    "severity": "orta",
    "stressIndicators": [
      "interveinal sararma (yaprak damarları daha yeşil kalmış)",
      "noktasal nekrozlu lekeler",
      "yaprak kıvrılması/mahmuzlanma"
    ],
    "diseaseSymptoms": [
      "yaprak lekeleri (küçük nekrotik noktalar/lekeler)",
      "genel yaprak sararması (kloroz)"
    ]
  },

  // Nutrient Status
  "nutrientStatus": {
    "nitrogen": "eksik",
    "phosphorus": "normal",
    "potassium": "eksik",
    "magnesium": "eksik",
    "primaryDeficiency": "magnezyum eksikliği",
    "secondaryDeficiencies": ["azot eksikliği", "potasyum eksikliği"],
    "severity": "orta"
  },

  // Pest & Disease
  "pestDisease": {
    "pestsDetected": [
      {
        "name": "örümcek akarı (olasılık)",
        "category": "akar",
        "severity": "düşük",
        "confidence": 40
      }
    ],
    "diseasesDetected": [
      {
        "type": "yaprak lekesi/erken yaprak hastalığı",
        "category": "fungal",
        "severity": "orta",
        "affectedParts": ["yapraklar"],
        "confidence": 55
      }
    ],
    "damagePattern": "interveinal kloroz + dağınık nekrotik lekeler",
    "affectedAreaPercentage": 15,
    "spreadRisk": "orta",
    "primaryIssue": "besin eksikliği (özellikle Mg) ile birlikte yüzeysel fungal yaprak lezyonları"
  },

  // Environmental Stress
  "environmentalStress": {
    "waterStatus": "hafif kurak",
    "temperatureStress": "yok",
    "lightStress": "yok",
    "physicalDamage": "yok",
    "chemicalDamage": "şüpheli - yakın zamanda yapılan püskürtme sonrası hafif fitotoksik reaksiyon ihtimali",
    "primaryStressor": "besin eksikliği (magnezyum öncelikli)",
    "soilHealthIndicators": {
      "salinity": "yok",
      "phIssue": "optimal",
      "organicMatter": "orta"
    }
  },

  // Summary
  "summary": {
    "overallHealthScore": 4,
    "primaryConcern": "magnezyum eksikliği ile ilişkili yaprak klorozu ve buna bağlı zayıflamış doku",
    "secondaryConcerns": [
      "azot/potasyum eksikliği olasılığı",
      "yüzeysel fungal yaprak lezyonları",
      "örümcek akarı başlangıcı (olasılık)"
    ],
    "criticalIssuesCount": 0,
    "confidenceLevel": 65,
    "prognosis": "orta",
    "estimatedYieldImpact": "orta"
  },

  // Recommendations
  "recommendations": {
    "immediate": [
      {
        "action": "etkilenmiş yaprakları temizleyin",
        "details": "Şiddetli lezyonlu ve kurumuş yaprakları makas ile kesin ve sera dışına atın",
        "timeline": "24 saat içinde",
        "priority": "kritik"
      },
      {
        "action": "hızlı Mg takviyesi (foliar)",
        "details": "Epsom tuzu (magnezyum sülfat) ile 2-3 g/L konsantrasyonunda hafif bir yaprak spreyi",
        "timeline": "48 saat içinde",
        "priority": "yüksek"
      }
    ],
    "shortTerm": [
      {
        "action": "toprak ve çözeltide analiz yaptırın",
        "details": "pH, EC, makro ve mikro element analizi için toprak/stiok örnekleri alın",
        "timeline": "7-14 gün",
        "priority": "yüksek"
      }
    ],
    "preventive": [
      {
        "action": "düzenli izleme ve hijyen",
        "details": "Sera içi taşıyıcı organik materyali azaltın, hasta yaprakları hemen uzaklaştırın",
        "timeline": "sürekli",
        "priority": "orta"
      }
    ],
    "monitoring": [
      {
        "parameter": "yaprak semptomları ve lezyon yayılımı",
        "frequency": "haftada 1-2 kez",
        "threshold": "%10 yaprak etkilenmesi -> müdahale"
      }
    ],
    "resourceEstimation": {
      "waterRequiredLiters": "5",
      "fertilizerCostEstimateUsd": "15",
      "laborHoursEstimate": "2"
    },
    "localizedRecommendations": {
      "region": "Ankara (39.9334,32.8597)",
      "preferredPractices": [
        "damla sulama ile kontrollü gübreleme (fertirigasyon)",
        "sera içi hijyen ve düzenli yaprak kontrolleri"
      ],
      "restrictedMethods": [
        "izin ve etiket talimatı olmadan sistemik fungisitlerin rastgele kullanımı"
      ]
    }
  },

  // Risk Assessment
  "riskAssessment": {
    "yieldLossProbability": "orta",
    "timelineToWorsen": "hafta",
    "spreadPotential": "lokal"
  },

  // Cross-Factor Insights
  "crossFactorInsights": [
    {
      "insight": "Magnezyum eksikliği interveinal sararmaya neden olarak yaprak dokusunu zayıflatır; zayıf dokular daha kolay fungal patojen saldırısına açık hale gelir.",
      "confidence": 0.75,
      "affectedAspects": ["nutrient_status", "pest_disease"],
      "impactLevel": "yüksek"
    }
  ],

  // Image Info (🆕 Multi-image support)
  "imageInfo": {
    "imageUrl": "https://cdn-staging.ziraai.com/plant-images/12346.jpg",
    "imagePath": "/uploads/plant-images/12346.jpg",
    "format": "url",
    "uploadTimestamp": "2025-11-27T11:44:30.665Z"
  },

  // Processing Info
  "processingInfo": {
    "aiModel": "gpt-4o-mini",
    "workflowVersion": "2.0-url-based",
    "processingTimestamp": "2025-11-27T11:45:42.180Z",
    "parseSuccess": true
  },

  // Token Usage
  "tokenUsage": {
    "totalTokens": 5992,
    "promptTokens": 4149,
    "completionTokens": 1843,
    "costUsd": 0.004723,
    "costTry": 0.2362
  },

  "farmerFriendlySummary": "Yapraklarda damarlar daha yeşil kalarak arada sararma ve bazı kahverengi lezyonlar var...",

  "success": true,
  "message": "Analysis retrieved successfully"
}
```

---

## Authentication & Authorization

### JWT Bearer Token

All analysis endpoints require JWT Bearer authentication:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Role-Based Access Control

| Endpoint | Farmer | Admin | Sponsor |
|----------|--------|-------|---------|
| `POST /analyze-async` | ✅ | ✅ | ❌ |
| `POST /analyze-multi-async` | ✅ | ✅ | ❌ |
| `GET /{id}` | ✅ (own) | ✅ (all) | ✅ (sponsored) |
| `GET /{id}/detail` | ✅ (own) | ✅ (all) | ✅ (sponsored) |
| `GET /my-analyses` | ✅ | ✅ | ❌ |
| `GET /list` | ✅ | ❌ | ❌ |
| `GET /sponsored-analyses` | ❌ | ✅ | ❌ |
| `GET /` | ❌ | ✅ | ❌ |

**Authorization Rules:**
- **Farmers:** Can only submit and view their own analyses
- **Sponsors:** Can view analyses they sponsor (via `SponsorId` claim)
- **Admins:** Full access to all analyses

### Auto-Populated Security Fields

The following fields are **automatically populated by the server** from JWT claims and should **NOT** be sent from clients:

```json
{
  "userId": null,        // Populated from JWT NameIdentifier claim
  "farmerId": null,      // Auto-formatted: F{userId:D3} (e.g., F001, F190)
  "sponsorId": null,     // Retrieved from active sponsorship code
  "sponsorUserId": null, // Actual sponsor user ID
  "sponsorshipCodeId": null  // SponsorshipCode table ID
}
```

**Client Responsibility:** Only send `image`, `leafTopImage`, etc., and optional metadata fields. Never include user/sponsor identification fields.

---

## Migration Guide

### For Existing Mobile/Web Clients

#### Phase 1: No Changes Required (Immediate Compatibility)

Existing clients using single-image endpoints **continue working without any code changes**:

```typescript
// ✅ Existing code continues working
const result = await http.post('/api/v1/plantanalyses/analyze-async', {
  image: base64Image,
  cropType: 'Tomato',
  // ... other fields
});
```

When fetching analysis details:

```typescript
// ✅ Existing code continues working
const detail = await http.get(`/api/v1/plantanalyses/${id}/detail`);

// Existing fields still available
console.log(detail.summary.primaryConcern);
console.log(detail.imageInfo.imageUrl);

// Multi-image fields safely ignored if null
console.log(detail.imageInfo.totalImages);  // null for single-image
```

#### Phase 2: Opt-In Multi-Image Support (Optional Enhancement)

Add UI for optional additional images:

```typescript
// 🆕 New multi-image request
const result = await http.post('/api/v1/plantanalyses/analyze-multi-async', {
  image: mainImage,           // Required
  leafTopImage: leafTop,      // Optional
  leafBottomImage: leafBottom, // Optional
  // ... other fields
});
```

Handle multi-image response:

```typescript
const detail = await http.get(`/api/v1/plantanalyses/${id}/detail`);

// Check if multi-image analysis
if (detail.imageInfo.totalImages && detail.imageInfo.totalImages > 1) {
  // Display image gallery
  const images = [
    { type: 'Main', url: detail.imageInfo.imageUrl },
    ...(detail.imageInfo.hasLeafTop ?
      [{ type: 'Leaf Top', url: detail.imageInfo.leafTopImageUrl }] : []),
    ...(detail.imageInfo.hasLeafBottom ?
      [{ type: 'Leaf Bottom', url: detail.imageInfo.leafBottomImageUrl }] : []),
    ...(detail.imageInfo.hasPlantOverview ?
      [{ type: 'Plant Overview', url: detail.imageInfo.plantOverviewImageUrl }] : []),
    ...(detail.imageInfo.hasRoot ?
      [{ type: 'Root', url: detail.imageInfo.rootImageUrl }] : [])
  ];

  renderImageGallery(images);
} else {
  // Single image - existing logic
  renderSingleImage(detail.imageInfo.imageUrl);
}
```

---

## Code Examples

### Flutter (Mobile)

#### Submit Single-Image Analysis

```dart
import 'dart:convert';
import 'package:http/http.dart' as http;

Future<Map<String, dynamic>> submitSingleImageAnalysis({
  required String base64Image,
  required String jwtToken,
  String? cropType,
  String? fieldId,
}) async {
  final url = Uri.parse('https://api.ziraai.com/api/v1/plantanalyses/analyze-async');

  final response = await http.post(
    url,
    headers: {
      'Authorization': 'Bearer $jwtToken',
      'Content-Type': 'application/json',
    },
    body: jsonEncode({
      'image': 'data:image/jpeg;base64,$base64Image',
      'cropType': cropType,
      'fieldId': fieldId,
    }),
  );

  if (response.statusCode == 202) {
    return jsonDecode(response.body);
  } else {
    throw Exception('Failed to submit analysis: ${response.body}');
  }
}
```

#### Submit Multi-Image Analysis

```dart
Future<Map<String, dynamic>> submitMultiImageAnalysis({
  required String mainImage,
  String? leafTopImage,
  String? leafBottomImage,
  String? plantOverviewImage,
  String? rootImage,
  required String jwtToken,
  String? cropType,
}) async {
  final url = Uri.parse('https://api.ziraai.com/api/v1/plantanalyses/analyze-multi-async');

  final body = {
    'image': 'data:image/jpeg;base64,$mainImage',
    if (leafTopImage != null) 'leafTopImage': 'data:image/jpeg;base64,$leafTopImage',
    if (leafBottomImage != null) 'leafBottomImage': 'data:image/jpeg;base64,$leafBottomImage',
    if (plantOverviewImage != null) 'plantOverviewImage': 'data:image/jpeg;base64,$plantOverviewImage',
    if (rootImage != null) 'rootImage': 'data:image/jpeg;base64,$rootImage',
    if (cropType != null) 'cropType': cropType,
  };

  final response = await http.post(
    url,
    headers: {
      'Authorization': 'Bearer $jwtToken',
      'Content-Type': 'application/json',
    },
    body: jsonEncode(body),
  );

  if (response.statusCode == 202) {
    return jsonDecode(response.body);
  } else {
    throw Exception('Failed to submit multi-image analysis: ${response.body}');
  }
}
```

#### Fetch Analysis Detail with Multi-Image Support

```dart
class AnalysisDetail {
  final int id;
  final String status;
  final ImageMetadata imageInfo;
  final Summary summary;
  // ... other fields

  factory AnalysisDetail.fromJson(Map<String, dynamic> json) {
    return AnalysisDetail(
      id: json['id'],
      status: json['analysisStatus'],
      imageInfo: ImageMetadata.fromJson(json['imageInfo']),
      summary: Summary.fromJson(json['summary']),
    );
  }
}

class ImageMetadata {
  final String imageUrl;
  final int? totalImages;  // Nullable - null for single-image
  final List<String>? imagesProvided;
  final String? leafTopImageUrl;
  final String? leafBottomImageUrl;
  final String? plantOverviewImageUrl;
  final String? rootImageUrl;

  factory ImageMetadata.fromJson(Map<String, dynamic> json) {
    return ImageMetadata(
      imageUrl: json['imageUrl'],
      totalImages: json['totalImages'],
      imagesProvided: json['imagesProvided']?.cast<String>(),
      leafTopImageUrl: json['leafTopImageUrl'],
      leafBottomImageUrl: json['leafBottomImageUrl'],
      plantOverviewImageUrl: json['plantOverviewImageUrl'],
      rootImageUrl: json['rootImageUrl'],
    );
  }

  bool get isMultiImage => totalImages != null && totalImages! > 1;

  List<ImageItem> getImageList() {
    final images = [ImageItem(type: 'Main', url: imageUrl)];

    if (leafTopImageUrl != null) {
      images.add(ImageItem(type: 'Leaf Top', url: leafTopImageUrl!));
    }
    if (leafBottomImageUrl != null) {
      images.add(ImageItem(type: 'Leaf Bottom', url: leafBottomImageUrl!));
    }
    if (plantOverviewImageUrl != null) {
      images.add(ImageItem(type: 'Plant Overview', url: plantOverviewImageUrl!));
    }
    if (rootImageUrl != null) {
      images.add(ImageItem(type: 'Root', url: rootImageUrl!));
    }

    return images;
  }
}

class ImageItem {
  final String type;
  final String url;

  ImageItem({required this.type, required this.url});
}

Future<AnalysisDetail> fetchAnalysisDetail({
  required int analysisId,
  required String jwtToken,
}) async {
  final url = Uri.parse('https://api.ziraai.com/api/v1/plantanalyses/$analysisId/detail');

  final response = await http.get(
    url,
    headers: {'Authorization': 'Bearer $jwtToken'},
  );

  if (response.statusCode == 200) {
    final json = jsonDecode(response.body);
    return AnalysisDetail.fromJson(json['data']);
  } else {
    throw Exception('Failed to fetch analysis detail');
  }
}
```

#### UI Widget for Image Gallery

```dart
import 'package:flutter/material.dart';
import 'package:cached_network_image/cached_network_image.dart';

class AnalysisImageGallery extends StatelessWidget {
  final ImageMetadata imageMetadata;

  const AnalysisImageGallery({Key? key, required this.imageMetadata}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    if (!imageMetadata.isMultiImage) {
      // Single image display
      return CachedNetworkImage(
        imageUrl: imageMetadata.imageUrl,
        fit: BoxFit.cover,
        placeholder: (context, url) => CircularProgressIndicator(),
        errorWidget: (context, url, error) => Icon(Icons.error),
      );
    }

    // Multi-image gallery
    final images = imageMetadata.getImageList();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          '${imageMetadata.totalImages} images',
          style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
        ),
        SizedBox(height: 8),
        SizedBox(
          height: 150,
          child: ListView.builder(
            scrollDirection: Axis.horizontal,
            itemCount: images.length,
            itemBuilder: (context, index) {
              final image = images[index];
              return Padding(
                padding: EdgeInsets.only(right: 8),
                child: Column(
                  children: [
                    ClipRRect(
                      borderRadius: BorderRadius.circular(8),
                      child: CachedNetworkImage(
                        imageUrl: image.url,
                        width: 120,
                        height: 120,
                        fit: BoxFit.cover,
                      ),
                    ),
                    SizedBox(height: 4),
                    Text(image.type, style: TextStyle(fontSize: 12)),
                  ],
                ),
              );
            },
          ),
        ),
      ],
    );
  }
}
```

---

### Angular (Web)

#### Service for Analysis API

```typescript
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PlantAnalysisRequest {
  image: string;  // Base64 data URI
  cropType?: string;
  fieldId?: string;
  location?: string;
  // ... other optional fields
}

export interface PlantAnalysisMultiImageRequest extends PlantAnalysisRequest {
  leafTopImage?: string;
  leafBottomImage?: string;
  plantOverviewImage?: string;
  rootImage?: string;
}

export interface AnalysisSubmissionResponse {
  success: boolean;
  message: string;
  analysis_id: number;
  image_count?: number;  // Present for multi-image
  estimated_processing_time: string;
  status_check_endpoint: string;
}

export interface ImageMetadata {
  imageUrl: string;
  totalImages?: number;
  imagesProvided?: string[];
  hasLeafTop?: boolean;
  hasLeafBottom?: boolean;
  hasPlantOverview?: boolean;
  hasRoot?: boolean;
  leafTopImageUrl?: string;
  leafBottomImageUrl?: string;
  plantOverviewImageUrl?: string;
  rootImageUrl?: string;
}

export interface AnalysisDetail {
  id: number;
  analysisStatus: string;
  imageInfo: ImageMetadata;
  summary: any;
  // ... other fields
}

@Injectable({
  providedIn: 'root'
})
export class PlantAnalysisService {
  private apiUrl = 'https://api.ziraai.com/api/v1/plantanalyses';

  constructor(private http: HttpClient) {}

  private getHeaders(token: string): HttpHeaders {
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    });
  }

  submitSingleImageAnalysis(
    request: PlantAnalysisRequest,
    token: string
  ): Observable<AnalysisSubmissionResponse> {
    return this.http.post<AnalysisSubmissionResponse>(
      `${this.apiUrl}/analyze-async`,
      request,
      { headers: this.getHeaders(token) }
    );
  }

  submitMultiImageAnalysis(
    request: PlantAnalysisMultiImageRequest,
    token: string
  ): Observable<AnalysisSubmissionResponse> {
    return this.http.post<AnalysisSubmissionResponse>(
      `${this.apiUrl}/analyze-multi-async`,
      request,
      { headers: this.getHeaders(token) }
    );
  }

  getAnalysisDetail(
    analysisId: number,
    token: string
  ): Observable<{ data: AnalysisDetail }> {
    return this.http.get<{ data: AnalysisDetail }>(
      `${this.apiUrl}/${analysisId}/detail`,
      { headers: this.getHeaders(token) }
    );
  }
}
```

#### Component for Multi-Image Upload

```typescript
import { Component } from '@angular/core';
import { PlantAnalysisService, PlantAnalysisMultiImageRequest } from './plant-analysis.service';

@Component({
  selector: 'app-multi-image-upload',
  template: `
    <div class="upload-container">
      <h2>Plant Analysis - Multi-Image</h2>

      <div class="image-upload">
        <label>Main Image (Required)</label>
        <input type="file" accept="image/*" (change)="onMainImageSelected($event)" />
        <img *ngIf="mainImagePreview" [src]="mainImagePreview" class="preview" />
      </div>

      <div class="optional-images">
        <div class="image-upload">
          <label>Leaf Top (Optional)</label>
          <input type="file" accept="image/*" (change)="onLeafTopSelected($event)" />
          <img *ngIf="leafTopPreview" [src]="leafTopPreview" class="preview" />
        </div>

        <div class="image-upload">
          <label>Leaf Bottom (Optional)</label>
          <input type="file" accept="image/*" (change)="onLeafBottomSelected($event)" />
          <img *ngIf="leafBottomPreview" [src]="leafBottomPreview" class="preview" />
        </div>

        <div class="image-upload">
          <label>Plant Overview (Optional)</label>
          <input type="file" accept="image/*" (change)="onPlantOverviewSelected($event)" />
          <img *ngIf="plantOverviewPreview" [src]="plantOverviewPreview" class="preview" />
        </div>

        <div class="image-upload">
          <label>Root System (Optional)</label>
          <input type="file" accept="image/*" (change)="onRootSelected($event)" />
          <img *ngIf="rootPreview" [src]="rootPreview" class="preview" />
        </div>
      </div>

      <button
        [disabled]="!mainImageBase64 || submitting"
        (click)="submitAnalysis()">
        {{ submitting ? 'Submitting...' : 'Analyze Plant' }}
      </button>

      <div *ngIf="submissionResult" class="result">
        <p>{{ submissionResult.message }}</p>
        <p>Analysis ID: {{ submissionResult.analysis_id }}</p>
        <p *ngIf="submissionResult.image_count">
          Images: {{ submissionResult.image_count }}
        </p>
      </div>
    </div>
  `
})
export class MultiImageUploadComponent {
  mainImageBase64?: string;
  leafTopBase64?: string;
  leafBottomBase64?: string;
  plantOverviewBase64?: string;
  rootBase64?: string;

  mainImagePreview?: string;
  leafTopPreview?: string;
  leafBottomPreview?: string;
  plantOverviewPreview?: string;
  rootPreview?: string;

  submitting = false;
  submissionResult?: any;

  constructor(private analysisService: PlantAnalysisService) {}

  onMainImageSelected(event: any): void {
    this.convertToBase64(event.target.files[0], (base64) => {
      this.mainImageBase64 = base64;
      this.mainImagePreview = base64;
    });
  }

  onLeafTopSelected(event: any): void {
    this.convertToBase64(event.target.files[0], (base64) => {
      this.leafTopBase64 = base64;
      this.leafTopPreview = base64;
    });
  }

  onLeafBottomSelected(event: any): void {
    this.convertToBase64(event.target.files[0], (base64) => {
      this.leafBottomBase64 = base64;
      this.leafBottomPreview = base64;
    });
  }

  onPlantOverviewSelected(event: any): void {
    this.convertToBase64(event.target.files[0], (base64) => {
      this.plantOverviewBase64 = base64;
      this.plantOverviewPreview = base64;
    });
  }

  onRootSelected(event: any): void {
    this.convertToBase64(event.target.files[0], (base64) => {
      this.rootBase64 = base64;
      this.rootPreview = base64;
    });
  }

  private convertToBase64(file: File, callback: (base64: string) => void): void {
    const reader = new FileReader();
    reader.onload = () => {
      callback(reader.result as string);
    };
    reader.readAsDataURL(file);
  }

  submitAnalysis(): void {
    if (!this.mainImageBase64) return;

    this.submitting = true;

    const request: PlantAnalysisMultiImageRequest = {
      image: this.mainImageBase64,
      leafTopImage: this.leafTopBase64,
      leafBottomImage: this.leafBottomBase64,
      plantOverviewImage: this.plantOverviewBase64,
      rootImage: this.rootBase64,
      cropType: 'Tomato'  // Example
    };

    // Get JWT token from auth service (not shown)
    const token = 'your-jwt-token';

    this.analysisService.submitMultiImageAnalysis(request, token)
      .subscribe({
        next: (result) => {
          this.submissionResult = result;
          this.submitting = false;
        },
        error: (error) => {
          console.error('Submission failed:', error);
          this.submitting = false;
        }
      });
  }
}
```

#### Component for Analysis Detail Display

```typescript
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PlantAnalysisService, AnalysisDetail } from './plant-analysis.service';

@Component({
  selector: 'app-analysis-detail',
  template: `
    <div *ngIf="analysis" class="analysis-detail">
      <h2>Analysis Detail</h2>

      <!-- Image Gallery -->
      <div class="image-section">
        <div *ngIf="!isMultiImage">
          <img [src]="analysis.imageInfo.imageUrl" class="single-image" />
        </div>

        <div *ngIf="isMultiImage" class="image-gallery">
          <h3>{{ analysis.imageInfo.totalImages }} Images</h3>
          <div class="gallery-grid">
            <div class="gallery-item">
              <img [src]="analysis.imageInfo.imageUrl" />
              <p>Main Image</p>
            </div>
            <div *ngIf="analysis.imageInfo.leafTopImageUrl" class="gallery-item">
              <img [src]="analysis.imageInfo.leafTopImageUrl" />
              <p>Leaf Top</p>
            </div>
            <div *ngIf="analysis.imageInfo.leafBottomImageUrl" class="gallery-item">
              <img [src]="analysis.imageInfo.leafBottomImageUrl" />
              <p>Leaf Bottom</p>
            </div>
            <div *ngIf="analysis.imageInfo.plantOverviewImageUrl" class="gallery-item">
              <img [src]="analysis.imageInfo.plantOverviewImageUrl" />
              <p>Plant Overview</p>
            </div>
            <div *ngIf="analysis.imageInfo.rootImageUrl" class="gallery-item">
              <img [src]="analysis.imageInfo.rootImageUrl" />
              <p>Root System</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Analysis Summary -->
      <div class="summary-section">
        <h3>Summary</h3>
        <p><strong>Health Score:</strong> {{ analysis.summary.overallHealthScore }}/10</p>
        <p><strong>Primary Concern:</strong> {{ analysis.summary.primaryConcern }}</p>
        <p><strong>Prognosis:</strong> {{ analysis.summary.prognosis }}</p>
      </div>

      <!-- Additional sections (recommendations, nutrients, etc.) -->
    </div>
  `,
  styles: [`
    .image-gallery .gallery-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
    }

    .gallery-item img {
      width: 100%;
      height: 200px;
      object-fit: cover;
      border-radius: 8px;
    }
  `]
})
export class AnalysisDetailComponent implements OnInit {
  analysis?: AnalysisDetail;

  constructor(
    private route: ActivatedRoute,
    private analysisService: PlantAnalysisService
  ) {}

  ngOnInit(): void {
    const analysisId = Number(this.route.snapshot.paramMap.get('id'));
    const token = 'your-jwt-token';  // Get from auth service

    this.analysisService.getAnalysisDetail(analysisId, token)
      .subscribe(response => {
        this.analysis = response.data;
      });
  }

  get isMultiImage(): boolean {
    return !!this.analysis?.imageInfo.totalImages &&
           this.analysis.imageInfo.totalImages > 1;
  }
}
```

---

## SignalR Real-Time Notifications

Both single-image and multi-image analyses trigger **identical SignalR notifications** when processing completes.

### Connection Setup

**Hub URL:** `https://api.ziraai.com/analysishub`

**Event Name:** `ReceiveAnalysisUpdate`

### Flutter SignalR Implementation

```dart
import 'package:signalr_netcore/signalr_client.dart';

class AnalysisNotificationService {
  late HubConnection _hubConnection;

  Future<void> connect(String jwtToken) async {
    _hubConnection = HubConnectionBuilder()
        .withUrl(
          'https://api.ziraai.com/analysishub',
          HttpConnectionOptions(
            accessTokenFactory: () => Future.value(jwtToken),
          ),
        )
        .withAutomaticReconnect()
        .build();

    // Listen for analysis updates
    _hubConnection.on('ReceiveAnalysisUpdate', _handleAnalysisUpdate);

    await _hubConnection.start();
    print('SignalR connected');
  }

  void _handleAnalysisUpdate(List<Object>? arguments) {
    if (arguments == null || arguments.isEmpty) return;

    final notification = arguments[0] as Map<String, dynamic>;
    final int analysisId = notification['analysisId'];
    final String status = notification['status'];
    final String message = notification['message'];

    print('Analysis $analysisId: $status - $message');

    // Update UI, trigger fetch, show notification, etc.
    if (status == 'Completed') {
      _fetchAnalysisDetail(analysisId);
    }
  }

  Future<void> disconnect() async {
    await _hubConnection.stop();
  }
}
```

### Angular SignalR Implementation

```typescript
import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';

export interface AnalysisNotification {
  analysisId: number;
  status: string;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection?: signalR.HubConnection;

  public startConnection(jwtToken: string): Promise<void> {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://api.ziraai.com/analysishub', {
        accessTokenFactory: () => jwtToken
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveAnalysisUpdate', (notification: AnalysisNotification) => {
      console.log('Analysis update:', notification);

      if (notification.status === 'Completed') {
        // Fetch complete analysis
        this.onAnalysisCompleted(notification.analysisId);
      } else if (notification.status === 'Failed') {
        // Show error notification
        this.onAnalysisFailed(notification);
      }
    });

    return this.hubConnection.start();
  }

  public stopConnection(): Promise<void> {
    return this.hubConnection?.stop() ?? Promise.resolve();
  }

  private onAnalysisCompleted(analysisId: number): void {
    // Implement: Fetch analysis detail, update UI, show notification
    console.log(`Analysis ${analysisId} completed - fetching details`);
  }

  private onAnalysisFailed(notification: AnalysisNotification): void {
    // Implement: Show error message to user
    console.error('Analysis failed:', notification.message);
  }
}
```

### Notification Payload Structure

```json
{
  "analysisId": 12346,
  "status": "Completed",  // or "Processing", "Failed"
  "message": "Analysis completed successfully"
}
```

**Important:** Multi-image and single-image analyses send **identical notification payloads**. Clients cannot distinguish analysis type from notification alone - fetch detail to determine if multi-image.

---

## Error Handling

### Common Error Responses

#### 400 Bad Request - Validation Failure

```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    "The Image field is required.",
    "Image size exceeds maximum allowed (500 MB)."
  ]
}
```

#### 401 Unauthorized - Missing/Invalid JWT

```json
{
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authorization header is missing or invalid"
}
```

#### 403 Forbidden - Quota Exceeded

```json
{
  "success": false,
  "message": "Daily analysis limit reached. You have used 2/2 analyses for today.",
  "subscriptionStatus": {
    "tierName": "Trial",
    "dailyLimit": 2,
    "dailyUsed": 2,
    "monthlyLimit": 5,
    "monthlyUsed": 5
  },
  "upgradeMessage": "Upgrade to Small plan for 5 daily analyses at ₺99.99/month!"
}
```

#### 404 Not Found - Analysis Not Found

```json
{
  "success": false,
  "message": "Plant analysis not found",
  "data": null
}
```

#### 503 Service Unavailable - Queue Down

```json
{
  "success": false,
  "message": "Message queue service is currently unavailable. Please try again later."
}
```

### Client Error Handling Strategy

```typescript
async function submitAnalysisWithErrorHandling(request: any, token: string) {
  try {
    const response = await http.post('/api/v1/plantanalyses/analyze-multi-async', request, {
      headers: { 'Authorization': `Bearer ${token}` }
    });

    return { success: true, data: response.data };

  } catch (error: any) {
    if (error.response) {
      switch (error.response.status) {
        case 400:
          // Validation errors - show to user
          return {
            success: false,
            message: 'Invalid request',
            errors: error.response.data.errors
          };

        case 401:
          // Unauthorized - refresh token or redirect to login
          await refreshAuthToken();
          return submitAnalysisWithErrorHandling(request, newToken);

        case 403:
          // Quota exceeded - prompt upgrade
          return {
            success: false,
            message: error.response.data.message,
            requiresUpgrade: true,
            upgradeMessage: error.response.data.upgradeMessage
          };

        case 404:
          // Not found
          return {
            success: false,
            message: 'Analysis not found'
          };

        case 503:
          // Service unavailable - retry after delay
          await delay(5000);
          return submitAnalysisWithErrorHandling(request, token);

        default:
          return {
            success: false,
            message: 'Unexpected error occurred'
          };
      }
    } else if (error.request) {
      // Network error - no response received
      return {
        success: false,
        message: 'Network error - please check your connection'
      };
    } else {
      // Client-side error
      return {
        success: false,
        message: error.message
      };
    }
  }
}
```

---

## Best Practices

### Image Optimization

**Client-Side Pre-Processing:**
- Compress images before upload to reduce network transfer time
- Target ~2-5 MB per image for optimal balance
- Server will further optimize to ~0.25 MB for AI processing

```dart
import 'package:flutter_image_compress/flutter_image_compress.dart';

Future<String> compressImageToBase64(File imageFile) async {
  final compressedBytes = await FlutterImageCompress.compressWithFile(
    imageFile.absolute.path,
    quality: 85,
    minWidth: 1920,
    minHeight: 1080,
  );

  return base64Encode(compressedBytes!);
}
```

### Progressive Enhancement

**Approach:** Start with single-image support, gradually enable multi-image features.

1. **Phase 1:** Implement single-image submission and detail view
2. **Phase 2:** Add UI for optional additional images
3. **Phase 3:** Enhance detail view to display image galleries
4. **Phase 4:** Add camera/gallery picker for each image type

### Offline Support

**Strategy:** Queue analysis requests when offline, sync when online.

```typescript
interface QueuedAnalysis {
  id: string;
  request: PlantAnalysisMultiImageRequest;
  timestamp: Date;
  syncStatus: 'pending' | 'syncing' | 'synced' | 'failed';
}

class OfflineQueueService {
  private queue: QueuedAnalysis[] = [];

  async queueAnalysis(request: PlantAnalysisMultiImageRequest): Promise<void> {
    const queued: QueuedAnalysis = {
      id: generateUUID(),
      request,
      timestamp: new Date(),
      syncStatus: 'pending'
    };

    this.queue.push(queued);
    await this.saveQueueToStorage();

    if (navigator.onLine) {
      await this.syncQueue();
    }
  }

  async syncQueue(): Promise<void> {
    for (const item of this.queue.filter(q => q.syncStatus === 'pending')) {
      item.syncStatus = 'syncing';

      try {
        const result = await submitMultiImageAnalysis(item.request, token);
        item.syncStatus = 'synced';
      } catch (error) {
        item.syncStatus = 'failed';
      }

      await this.saveQueueToStorage();
    }
  }
}
```

---

## Testing Checklist

### Single-Image Compatibility (Regression Testing)

- [ ] Existing single-image submissions work without changes
- [ ] Single-image detail response structure unchanged
- [ ] Multi-image fields are `null` for single-image analyses
- [ ] SignalR notifications work for single-image analyses

### Multi-Image Functionality

- [ ] Submit analysis with only main image (1 image total)
- [ ] Submit analysis with main + leaf top (2 images)
- [ ] Submit analysis with all 5 images
- [ ] Verify `image_count` in submission response
- [ ] Verify detail response contains all image URLs
- [ ] Verify `totalImages` matches submitted images
- [ ] Verify `imagesProvided` array is correct

### Authorization & Security

- [ ] Cannot submit analysis without JWT token
- [ ] Farmers can only view their own analyses
- [ ] Sponsors can view sponsored analyses
- [ ] Admins can view all analyses
- [ ] Auto-populated fields (userId, farmerId, sponsorId) correct

### Error Handling

- [ ] Validation errors returned for missing required fields
- [ ] Quota exceeded returns 403 with upgrade message
- [ ] Service unavailable returns 503 when queue down
- [ ] Invalid JWT returns 401

### SignalR Notifications

- [ ] Receive notification when single-image analysis completes
- [ ] Receive notification when multi-image analysis completes
- [ ] Notification payload contains correct analysisId and status
- [ ] Automatic reconnection works after disconnect

---

## Support & Troubleshooting

### Common Issues

**Q: Why are multi-image fields `null` in my detail response?**
A: You likely submitted a single-image analysis. Multi-image fields are only populated for analyses submitted via `/analyze-multi-async`.

**Q: Can I mix single and multi-image analyses in the same app?**
A: Yes! Use `/analyze-async` for single-image, `/analyze-multi-async` for multi-image. Detail endpoint works for both.

**Q: Do multi-image analyses cost more quota?**
A: No. Both single and multi-image analyses consume 1 analysis quota from your subscription.

**Q: Why is my image taking longer than estimated time?**
A: Processing time depends on queue load and image sizes. Multi-image analyses may take 5-7 minutes during peak times.

**Q: Can I retrieve just the image URLs without full analysis detail?**
A: Use `GET /api/v1/plantanalyses/{id}` for summary view (includes ImageMetadata with URLs).

### API Versioning

Current API version: **v1**

All endpoints use versioned routes: `/api/v1/plantanalyses/*`

Future breaking changes will increment version (v2, v3, etc.) to maintain backward compatibility.

---

## Changelog

### Version 1.0 (2025-11-27)

**Added:**
- Multi-image analysis support with up to 5 images
- New endpoint: `POST /api/v1/plantanalyses/analyze-multi-async`
- Extended `ImageMetadataDto` with backward-compatible multi-image fields
- Separate RabbitMQ queue for multi-image processing
- SignalR notification support for multi-image analyses

**Backward Compatible:**
- All existing single-image endpoints unchanged
- Single-image detail responses maintain structure
- Multi-image fields nullable - ignored by existing clients

---

## Contact

**API Documentation:** https://api.ziraai.com/swagger
**Support Email:** support@ziraai.com
**Developer Slack:** #ziraai-developers

---

**Document Version:** 1.0
**Last Updated:** 2025-11-27
**Verified Against:** Commit `ef46e44`, `2ebd475`
