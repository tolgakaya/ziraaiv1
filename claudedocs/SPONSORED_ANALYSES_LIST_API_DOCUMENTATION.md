# Sponsored Analyses List API - Complete Documentation

**Version**: 1.0
**Date**: 2025-10-15
**Status**: ✅ PRODUCTION READY
**Endpoint**: `GET /api/v1/sponsorship/analyses`

---

## Table of Contents

1. [Overview](#overview)
2. [Authentication](#authentication)
3. [Endpoint Details](#endpoint-details)
4. [Request Parameters](#request-parameters)
5. [Response Structure](#response-structure)
6. [Tier-Based Privacy Rules](#tier-based-privacy-rules)
7. [Complete Request/Response Examples](#complete-requestresponse-examples)
8. [Error Handling](#error-handling)
9. [Business Logic](#business-logic)
10. [Testing Guide](#testing-guide)

---

## Overview

This endpoint allows sponsors to retrieve a paginated list of plant analyses performed by farmers they have sponsored. The data visibility is controlled by the sponsor's subscription tier level, implementing a privacy-first approach.

### Key Features

- **Pagination**: Efficient data retrieval with configurable page size
- **Sorting**: Multiple sort criteria (date, health score, crop type)
- **Filtering**: By crop type and date range
- **Tier-Based Privacy**: Automatic field filtering based on sponsor's tier (30%, 60%, 100% access)
- **Summary Statistics**: Aggregated insights (average health, top crops, monthly count)
- **Sponsor Branding**: Logo and company information included
- **Messaging Capability**: Indicates if sponsor can message farmers (M, L, XL tiers)

---

## Authentication

**Required**: Yes
**Type**: JWT Bearer Token
**Roles**: `Sponsor`, `Admin`

### Headers

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
x-dev-arch-version: 1
```

### Authorization Logic

- User must have `Sponsor` or `Admin` role
- `SponsorId` is automatically extracted from the authenticated user's JWT claims
- Endpoint validates that the sponsor profile exists and is active

---

## Endpoint Details

### HTTP Method
```
GET /api/v1/sponsorship/analyses
```

### Base URLs

| Environment | URL |
|-------------|-----|
| **Development** | `https://localhost:5001/api/v1/sponsorship/analyses` |
| **Staging** | `https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/analyses` |
| **Production** | `https://ziraai.com/api/v1/sponsorship/analyses` |

---

## Request Parameters

All parameters are **optional** and passed as query strings.

### Pagination Parameters

| Parameter | Type | Default | Description | Example |
|-----------|------|---------|-------------|---------|
| `page` | `int` | `1` | Page number (1-based indexing) | `?page=2` |
| `pageSize` | `int` | `20` | Number of items per page (max: 100) | `?pageSize=50` |

### Sorting Parameters

| Parameter | Type | Default | Valid Values | Description |
|-----------|------|---------|--------------|-------------|
| `sortBy` | `string` | `"date"` | `date`, `healthScore`, `cropType` | Field to sort by |
| `sortOrder` | `string` | `"desc"` | `asc`, `desc` | Sort direction |

### Filtering Parameters

| Parameter | Type | Default | Description | Example |
|-----------|------|---------|-------------|---------|
| `filterByCropType` | `string` | `null` | Crop type (case-insensitive partial match) | `?filterByCropType=wheat` |
| `startDate` | `DateTime?` | `null` | Analysis start date (ISO 8601) | `?startDate=2025-01-01` |
| `endDate` | `DateTime?` | `null` | Analysis end date (ISO 8601) | `?endDate=2025-12-31` |
| `filterByTier` | `string` | `null` | Filter by tier (S, M, L, XL) - Reserved for future use | `?filterByTier=XL` |

### Complete Request URL Examples

**Example 1: Default (First Page, Latest First)**
```
GET /api/v1/sponsorship/analyses
```

**Example 2: Pagination**
```
GET /api/v1/sponsorship/analyses?page=3&pageSize=10
```

**Example 3: Sort by Health Score (Lowest First)**
```
GET /api/v1/sponsorship/analyses?sortBy=healthScore&sortOrder=asc
```

**Example 4: Filter by Crop Type**
```
GET /api/v1/sponsorship/analyses?filterByCropType=tomato
```

**Example 5: Date Range Filter**
```
GET /api/v1/sponsorship/analyses?startDate=2025-09-01&endDate=2025-10-15
```

**Example 6: Combined Parameters**
```
GET /api/v1/sponsorship/analyses?page=2&pageSize=25&sortBy=date&sortOrder=desc&filterByCropType=corn&startDate=2025-08-01
```

---

## Response Structure

### Success Response (HTTP 200)

```json
{
  "data": {
    "items": [...],           // Array of SponsoredAnalysisSummaryDto
    "totalCount": 150,         // Total matching analyses
    "page": 1,                 // Current page number
    "pageSize": 20,            // Items per page
    "totalPages": 8,           // Total pages available
    "hasNextPage": true,       // Can fetch next page
    "hasPreviousPage": false,  // Can fetch previous page
    "summary": {...}           // Aggregated statistics
  },
  "success": true,
  "message": "Retrieved 20 analyses (page 1 of 8)"
}
```

### SponsoredAnalysisSummaryDto Structure

#### Core Fields (Always Available - All Tiers)

```json
{
  "analysisId": 12345,
  "analysisDate": "2025-10-15T14:30:00Z",
  "analysisStatus": "Completed",
  "cropType": "Wheat",

  // Tier & Permission Info
  "tierName": "L",
  "accessPercentage": 60,
  "canMessage": true,
  "canViewLogo": true,

  // Sponsor Display Info
  "sponsorInfo": {
    "sponsorId": 789,
    "companyName": "Chimera Tarım A.Ş.",
    "logoUrl": "https://storage.ziraai.com/logos/chimera.png",
    "websiteUrl": "https://chimeraagro.com"
  }
}
```

#### 30% Access Fields (S & M Tiers)

```json
{
  // ... core fields ...

  "overallHealthScore": 75.5,
  "plantSpecies": "Triticum aestivum",
  "plantVariety": "Winter Wheat",
  "growthStage": "Flowering",
  "imageThumbnailUrl": "https://storage.ziraai.com/images/analysis_12345_thumb.jpg"
}
```

#### 60% Access Fields (L Tier)

```json
{
  // ... core + 30% fields ...

  "vigorScore": 82.3,
  "healthSeverity": "Moderate",
  "primaryConcern": "Leaf Rust Detected",
  "location": "Konya, Turkey",
  "recommendations": "Apply fungicide treatment within 48 hours. Monitor moisture levels."
}
```

#### 100% Access Fields (XL Tier)

```json
{
  // ... core + 30% + 60% fields ...

  "farmerName": "Mehmet Yılmaz",
  "farmerPhone": "+905551234567",
  "farmerEmail": "mehmet.yilmaz@example.com"
}
```

### Summary Statistics Structure

```json
{
  "summary": {
    "totalAnalyses": 150,
    "averageHealthScore": 78.45,
    "topCropTypes": ["Wheat", "Corn", "Tomato", "Barley", "Cotton"],
    "analysesThisMonth": 42
  }
}
```

---

## Tier-Based Privacy Rules

### Access Levels

| Tier | Access % | Data Visibility | Messaging | Logo Display |
|------|----------|-----------------|-----------|--------------|
| **S (Small)** | 30% | Basic health metrics, plant info, images | ❌ No | ✅ Yes |
| **M (Medium)** | 30% | Basic health metrics, plant info, images | ✅ Yes | ✅ Yes |
| **L (Large)** | 60% | + Detailed analysis, location, recommendations | ✅ Yes | ✅ Yes |
| **XL (Extra Large)** | 100% | + Farmer contact information | ✅ Yes | ✅ Yes |

### Field Visibility Matrix

| Field | S Tier | M Tier | L Tier | XL Tier |
|-------|--------|--------|--------|---------|
| Analysis ID, Date, Status, Crop Type | ✅ | ✅ | ✅ | ✅ |
| Overall Health Score | ✅ | ✅ | ✅ | ✅ |
| Plant Species, Variety, Growth Stage | ✅ | ✅ | ✅ | ✅ |
| Image Thumbnail | ✅ | ✅ | ✅ | ✅ |
| Vigor Score | ❌ | ❌ | ✅ | ✅ |
| Health Severity, Primary Concern | ❌ | ❌ | ✅ | ✅ |
| Location, Recommendations | ❌ | ❌ | ✅ | ✅ |
| Farmer Name, Phone, Email | ❌ | ❌ | ❌ | ✅ |
| Messaging Capability | ❌ | ✅ | ✅ | ✅ |

### Automatic Tier Detection

The endpoint automatically determines the sponsor's tier by:
1. Querying all sponsorship packages purchased by the sponsor
2. Finding the highest tier package
3. Mapping tier to access percentage (S/M → 30%, L → 60%, XL → 100%)
4. Filtering response fields accordingly

---

## Complete Request/Response Examples

### Example 1: S Tier Sponsor - Default Request

#### Request
```http
GET /api/v1/sponsorship/analyses HTTP/1.1
Host: ziraai-api-sit.up.railway.app
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
x-dev-arch-version: 1
```

#### Response (HTTP 200)
```json
{
  "data": {
    "items": [
      {
        "analysisId": 12345,
        "analysisDate": "2025-10-15T14:30:00Z",
        "analysisStatus": "Completed",
        "cropType": "Wheat",
        "overallHealthScore": 75.5,
        "plantSpecies": "Triticum aestivum",
        "plantVariety": "Winter Wheat",
        "growthStage": "Flowering",
        "imageThumbnailUrl": "https://storage.ziraai.com/images/12345_thumb.jpg",
        "vigorScore": null,
        "healthSeverity": null,
        "primaryConcern": null,
        "location": null,
        "recommendations": null,
        "farmerName": null,
        "farmerPhone": null,
        "farmerEmail": null,
        "tierName": "S/M",
        "accessPercentage": 30,
        "canMessage": false,
        "canViewLogo": true,
        "sponsorInfo": {
          "sponsorId": 789,
          "companyName": "Chimera Tarım A.Ş.",
          "logoUrl": "https://storage.ziraai.com/logos/chimera.png",
          "websiteUrl": "https://chimeraagro.com"
        }
      },
      {
        "analysisId": 12344,
        "analysisDate": "2025-10-14T09:15:00Z",
        "analysisStatus": "Completed",
        "cropType": "Corn",
        "overallHealthScore": 88.2,
        "plantSpecies": "Zea mays",
        "plantVariety": "Sweet Corn",
        "growthStage": "Vegetative",
        "imageThumbnailUrl": "https://storage.ziraai.com/images/12344_thumb.jpg",
        "vigorScore": null,
        "healthSeverity": null,
        "primaryConcern": null,
        "location": null,
        "recommendations": null,
        "farmerName": null,
        "farmerPhone": null,
        "farmerEmail": null,
        "tierName": "S/M",
        "accessPercentage": 30,
        "canMessage": false,
        "canViewLogo": true,
        "sponsorInfo": {
          "sponsorId": 789,
          "companyName": "Chimera Tarım A.Ş.",
          "logoUrl": "https://storage.ziraai.com/logos/chimera.png",
          "websiteUrl": "https://chimeraagro.com"
        }
      }
    ],
    "totalCount": 150,
    "page": 1,
    "pageSize": 20,
    "totalPages": 8,
    "hasNextPage": true,
    "hasPreviousPage": false,
    "summary": {
      "totalAnalyses": 150,
      "averageHealthScore": 78.45,
      "topCropTypes": ["Wheat", "Corn", "Tomato", "Barley", "Cotton"],
      "analysesThisMonth": 42
    }
  },
  "success": true,
  "message": "Retrieved 20 analyses (page 1 of 8)"
}
```

---

### Example 2: L Tier Sponsor - Filtered by Crop Type

#### Request
```http
GET /api/v1/sponsorship/analyses?filterByCropType=tomato&page=1&pageSize=10 HTTP/1.1
Host: ziraai-api-sit.up.railway.app
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
x-dev-arch-version: 1
```

#### Response (HTTP 200)
```json
{
  "data": {
    "items": [
      {
        "analysisId": 12350,
        "analysisDate": "2025-10-15T16:45:00Z",
        "analysisStatus": "Completed",
        "cropType": "Tomato",
        "overallHealthScore": 65.8,
        "plantSpecies": "Solanum lycopersicum",
        "plantVariety": "Roma",
        "growthStage": "Fruiting",
        "imageThumbnailUrl": "https://storage.ziraai.com/images/12350_thumb.jpg",
        "vigorScore": 70.5,
        "healthSeverity": "Moderate",
        "primaryConcern": "Early Blight Detected",
        "location": "Antalya, Turkey",
        "recommendations": "Remove infected leaves. Apply copper-based fungicide. Improve air circulation.",
        "farmerName": null,
        "farmerPhone": null,
        "farmerEmail": null,
        "tierName": "L",
        "accessPercentage": 60,
        "canMessage": true,
        "canViewLogo": true,
        "sponsorInfo": {
          "sponsorId": 890,
          "companyName": "AgriTech Solutions Ltd.",
          "logoUrl": "https://storage.ziraai.com/logos/agritech.png",
          "websiteUrl": "https://agritech-solutions.com"
        }
      },
      {
        "analysisId": 12349,
        "analysisDate": "2025-10-14T11:20:00Z",
        "analysisStatus": "Completed",
        "cropType": "Tomato",
        "overallHealthScore": 92.3,
        "plantSpecies": "Solanum lycopersicum",
        "plantVariety": "Cherry",
        "growthStage": "Flowering",
        "imageThumbnailUrl": "https://storage.ziraai.com/images/12349_thumb.jpg",
        "vigorScore": 95.1,
        "healthSeverity": "Healthy",
        "primaryConcern": "None",
        "location": "İzmir, Turkey",
        "recommendations": "Continue current care routine. Monitor for pests.",
        "farmerName": null,
        "farmerPhone": null,
        "farmerEmail": null,
        "tierName": "L",
        "accessPercentage": 60,
        "canMessage": true,
        "canViewLogo": true,
        "sponsorInfo": {
          "sponsorId": 890,
          "companyName": "AgriTech Solutions Ltd.",
          "logoUrl": "https://storage.ziraai.com/logos/agritech.png",
          "websiteUrl": "https://agritech-solutions.com"
        }
      }
    ],
    "totalCount": 45,
    "page": 1,
    "pageSize": 10,
    "totalPages": 5,
    "hasNextPage": true,
    "hasPreviousPage": false,
    "summary": {
      "totalAnalyses": 45,
      "averageHealthScore": 82.15,
      "topCropTypes": ["Tomato"],
      "analysesThisMonth": 18
    }
  },
  "success": true,
  "message": "Retrieved 10 analyses (page 1 of 5)"
}
```

---

### Example 3: XL Tier Sponsor - Date Range with Sorting

#### Request
```http
GET /api/v1/sponsorship/analyses?startDate=2025-09-01&endDate=2025-10-15&sortBy=healthScore&sortOrder=asc&page=1&pageSize=5 HTTP/1.1
Host: ziraai-api-sit.up.railway.app
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
x-dev-arch-version: 1
```

#### Response (HTTP 200)
```json
{
  "data": {
    "items": [
      {
        "analysisId": 12355,
        "analysisDate": "2025-09-20T08:30:00Z",
        "analysisStatus": "Completed",
        "cropType": "Barley",
        "overallHealthScore": 45.2,
        "plantSpecies": "Hordeum vulgare",
        "plantVariety": "Spring Barley",
        "growthStage": "Heading",
        "imageThumbnailUrl": "https://storage.ziraai.com/images/12355_thumb.jpg",
        "vigorScore": 38.5,
        "healthSeverity": "Critical",
        "primaryConcern": "Severe Powdery Mildew Infection",
        "location": "Ankara, Turkey",
        "recommendations": "Immediate fungicide application required. Consider crop rotation for next season.",
        "farmerName": "Ali Demir",
        "farmerPhone": "+905551234567",
        "farmerEmail": "ali.demir@example.com",
        "tierName": "XL",
        "accessPercentage": 100,
        "canMessage": true,
        "canViewLogo": true,
        "sponsorInfo": {
          "sponsorId": 901,
          "companyName": "Global Seeds International",
          "logoUrl": "https://storage.ziraai.com/logos/globalseeds.png",
          "websiteUrl": "https://globalseeds.com"
        }
      },
      {
        "analysisId": 12352,
        "analysisDate": "2025-10-05T13:15:00Z",
        "analysisStatus": "Completed",
        "cropType": "Wheat",
        "overallHealthScore": 58.7,
        "plantSpecies": "Triticum aestivum",
        "plantVariety": "Spring Wheat",
        "growthStage": "Grain Filling",
        "imageThumbnailUrl": "https://storage.ziraai.com/images/12352_thumb.jpg",
        "vigorScore": 55.3,
        "healthSeverity": "Moderate",
        "primaryConcern": "Nitrogen Deficiency",
        "location": "Konya, Turkey",
        "recommendations": "Apply nitrogen fertilizer at 50 kg/ha. Monitor soil moisture.",
        "farmerName": "Fatma Kaya",
        "farmerPhone": "+905559876543",
        "farmerEmail": "fatma.kaya@example.com",
        "tierName": "XL",
        "accessPercentage": 100,
        "canMessage": true,
        "canViewLogo": true,
        "sponsorInfo": {
          "sponsorId": 901,
          "companyName": "Global Seeds International",
          "logoUrl": "https://storage.ziraai.com/logos/globalseeds.png",
          "websiteUrl": "https://globalseeds.com"
        }
      },
      {
        "analysisId": 12348,
        "analysisDate": "2025-09-15T10:45:00Z",
        "analysisStatus": "Completed",
        "cropType": "Cotton",
        "overallHealthScore": 72.4,
        "plantSpecies": "Gossypium hirsutum",
        "plantVariety": "Upland Cotton",
        "growthStage": "Flowering",
        "imageThumbnailUrl": "https://storage.ziraai.com/images/12348_thumb.jpg",
        "vigorScore": 78.2,
        "healthSeverity": "Healthy",
        "primaryConcern": "Minor Aphid Presence",
        "location": "Şanlıurfa, Turkey",
        "recommendations": "Monitor aphid population. Natural predators present. No immediate action needed.",
        "farmerName": "Hasan Yıldız",
        "farmerPhone": "+905553216547",
        "farmerEmail": "hasan.yildiz@example.com",
        "tierName": "XL",
        "accessPercentage": 100,
        "canMessage": true,
        "canViewLogo": true,
        "sponsorInfo": {
          "sponsorId": 901,
          "companyName": "Global Seeds International",
          "logoUrl": "https://storage.ziraai.com/logos/globalseeds.png",
          "websiteUrl": "https://globalseeds.com"
        }
      },
      {
        "analysisId": 12347,
        "analysisDate": "2025-10-10T15:00:00Z",
        "analysisStatus": "Completed",
        "cropType": "Corn",
        "overallHealthScore": 85.9,
        "plantSpecies": "Zea mays",
        "plantVariety": "Field Corn",
        "growthStage": "Tasseling",
        "imageThumbnailUrl": "https://storage.ziraai.com/images/12347_thumb.jpg",
        "vigorScore": 88.7,
        "healthSeverity": "Healthy",
        "primaryConcern": "None",
        "location": "Adana, Turkey",
        "recommendations": "Excellent crop health. Maintain irrigation schedule.",
        "farmerName": "Zeynep Arslan",
        "farmerPhone": "+905554567890",
        "farmerEmail": "zeynep.arslan@example.com",
        "tierName": "XL",
        "accessPercentage": 100,
        "canMessage": true,
        "canViewLogo": true,
        "sponsorInfo": {
          "sponsorId": 901,
          "companyName": "Global Seeds International",
          "logoUrl": "https://storage.ziraai.com/logos/globalseeds.png",
          "websiteUrl": "https://globalseeds.com"
        }
      },
      {
        "analysisId": 12346,
        "analysisDate": "2025-09-25T12:30:00Z",
        "analysisStatus": "Completed",
        "cropType": "Wheat",
        "overallHealthScore": 91.5,
        "plantSpecies": "Triticum aestivum",
        "plantVariety": "Winter Wheat",
        "growthStage": "Maturity",
        "imageThumbnailUrl": "https://storage.ziraai.com/images/12346_thumb.jpg",
        "vigorScore": 93.8,
        "healthSeverity": "Healthy",
        "primaryConcern": "None",
        "location": "Edirne, Turkey",
        "recommendations": "Ready for harvest. Optimal moisture content.",
        "farmerName": "Mehmet Çelik",
        "farmerPhone": "+905556789012",
        "farmerEmail": "mehmet.celik@example.com",
        "tierName": "XL",
        "accessPercentage": 100,
        "canMessage": true,
        "canViewLogo": true,
        "sponsorInfo": {
          "sponsorId": 901,
          "companyName": "Global Seeds International",
          "logoUrl": "https://storage.ziraai.com/logos/globalseeds.png",
          "websiteUrl": "https://globalseeds.com"
        }
      }
    ],
    "totalCount": 87,
    "page": 1,
    "pageSize": 5,
    "totalPages": 18,
    "hasNextPage": true,
    "hasPreviousPage": false,
    "summary": {
      "totalAnalyses": 87,
      "averageHealthScore": 74.32,
      "topCropTypes": ["Wheat", "Corn", "Barley", "Cotton", "Tomato"],
      "analysesThisMonth": 32
    }
  },
  "success": true,
  "message": "Retrieved 5 analyses (page 1 of 18)"
}
```

---

### Example 4: Empty Result Set

#### Request
```http
GET /api/v1/sponsorship/analyses?filterByCropType=banana&startDate=2025-01-01&endDate=2025-01-31 HTTP/1.1
Host: ziraai-api-sit.up.railway.app
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
x-dev-arch-version: 1
```

#### Response (HTTP 200)
```json
{
  "data": {
    "items": [],
    "totalCount": 0,
    "page": 1,
    "pageSize": 20,
    "totalPages": 0,
    "hasNextPage": false,
    "hasPreviousPage": false,
    "summary": {
      "totalAnalyses": 0,
      "averageHealthScore": 0,
      "topCropTypes": [],
      "analysesThisMonth": 0
    }
  },
  "success": true,
  "message": "Retrieved 0 analyses (page 1 of 0)"
}
```

---

## Error Handling

### Error Response Structure

```json
{
  "data": null,
  "success": false,
  "message": "Error description"
}
```

### Common Error Scenarios

#### 1. Unauthorized (HTTP 401)

**Cause**: Missing or invalid JWT token

```json
{
  "data": null,
  "success": false,
  "message": "Unauthorized access"
}
```

**Solution**: Include valid Bearer token in Authorization header

---

#### 2. Forbidden (HTTP 403)

**Cause**: User doesn't have Sponsor or Admin role

```json
{
  "data": null,
  "success": false,
  "message": "Insufficient permissions"
}
```

**Solution**: Ensure authenticated user has `Sponsor` or `Admin` role

---

#### 3. Sponsor Profile Not Found (HTTP 200)

**Cause**: Sponsor profile doesn't exist or is inactive

```json
{
  "data": null,
  "success": false,
  "message": "Sponsor profile not found or inactive"
}
```

**Solution**:
- Verify sponsor profile exists in database
- Check `IsActive` flag is `true`
- Contact administrator to activate sponsor profile

---

#### 4. Invalid Parameters (HTTP 400)

**Cause**: Invalid date format, negative page numbers, etc.

```json
{
  "data": null,
  "success": false,
  "message": "Invalid request parameters"
}
```

**Solution**: Validate input parameters:
- `page` must be >= 1
- `pageSize` must be between 1 and 100
- Dates must be valid ISO 8601 format
- `sortBy` must be: `date`, `healthScore`, or `cropType`
- `sortOrder` must be: `asc` or `desc`

---

#### 5. Server Error (HTTP 500)

**Cause**: Internal server error

```json
{
  "data": null,
  "success": false,
  "message": "An error occurred while processing your request"
}
```

**Solution**:
- Check server logs for details
- Retry request
- Contact support if issue persists

---

## Business Logic

### Query Flow

1. **Authentication**: Validate JWT token and extract `SponsorId`
2. **Sponsor Validation**: Verify sponsor profile exists and is active
3. **Tier Detection**: Determine sponsor's highest tier package (S/M/L/XL)
4. **Data Query**: Fetch analyses where `SponsorUserId == SponsorId`
5. **Filtering**: Apply crop type and date range filters
6. **Sorting**: Order by requested field and direction
7. **Pagination**: Calculate total pages, apply skip/take
8. **Privacy Filtering**: Map to DTOs with tier-based field visibility
9. **Summary Calculation**: Compute aggregate statistics
10. **Response Building**: Construct paginated response with metadata

### Tier Determination Logic

```csharp
// Pseudo-code
var sponsorPackages = await GetAllSponsorPackages(sponsorId);
var highestTier = sponsorPackages
    .OrderByDescending(p => p.AccessPercentage)
    .FirstOrDefault();

int accessPercentage = highestTier?.AccessPercentage ?? 0;

// Map to tier name
string tierName = accessPercentage switch
{
    30 => "S/M",
    60 => "L",
    100 => "XL",
    _ => "Unknown"
};
```

### Privacy Filtering Logic

```csharp
// Pseudo-code
var dto = new SponsoredAnalysisSummaryDto
{
    // Core fields (always)
    AnalysisId = analysis.Id,
    AnalysisDate = analysis.AnalysisDate,
    // ...
};

// 30% Access Fields
if (accessPercentage >= 30)
{
    dto.OverallHealthScore = analysis.OverallHealthScore;
    dto.PlantSpecies = analysis.PlantSpecies;
    // ...
}

// 60% Access Fields
if (accessPercentage >= 60)
{
    dto.VigorScore = analysis.VigorScore;
    dto.Location = analysis.Location;
    // ...
}

// 100% Access Fields
if (accessPercentage >= 100)
{
    dto.FarmerName = analysis.FarmerName;
    dto.FarmerPhone = analysis.FarmerPhone;
    // ...
}
```

---

## Testing Guide

### Manual Testing with Postman

#### Step 1: Authentication

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "mobilePhone": "+905551234567",
  "password": "YourPassword123"
}
```

**Response**:
```json
{
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh_token_here"
  },
  "success": true
}
```

Copy the `token` value for subsequent requests.

---

#### Step 2: Basic Request (Default Parameters)

```http
GET /api/v1/sponsorship/analyses
Authorization: Bearer YOUR_TOKEN_HERE
x-dev-arch-version: 1
```

**Expected**: HTTP 200, paginated list of analyses

---

#### Step 3: Test Pagination

```http
GET /api/v1/sponsorship/analyses?page=2&pageSize=10
Authorization: Bearer YOUR_TOKEN_HERE
x-dev-arch-version: 1
```

**Verify**:
- `page` is `2`
- `items` array has max 10 elements
- `hasPreviousPage` is `true`

---

#### Step 4: Test Sorting

```http
GET /api/v1/sponsorship/analyses?sortBy=healthScore&sortOrder=asc
Authorization: Bearer YOUR_TOKEN_HERE
x-dev-arch-version: 1
```

**Verify**: Items are sorted by `overallHealthScore` in ascending order

---

#### Step 5: Test Filtering

```http
GET /api/v1/sponsorship/analyses?filterByCropType=wheat&startDate=2025-09-01&endDate=2025-10-15
Authorization: Bearer YOUR_TOKEN_HERE
x-dev-arch-version: 1
```

**Verify**:
- All items have `cropType` containing "wheat" (case-insensitive)
- All `analysisDate` values are between 2025-09-01 and 2025-10-15

---

#### Step 6: Test Tier-Based Privacy

Create test accounts for each tier:
- S tier sponsor → Verify only 30% fields are populated
- M tier sponsor → Verify 30% fields + `canMessage: true`
- L tier sponsor → Verify 60% fields are populated
- XL tier sponsor → Verify 100% fields (farmer contact info) are populated

---

### Automated Testing (Unit Tests)

```csharp
[Test]
public async Task GetSponsoredAnalysesList_WithValidSponsor_ReturnsPagedResults()
{
    // Arrange
    var query = new GetSponsoredAnalysesListQuery
    {
        SponsorId = 789,
        Page = 1,
        PageSize = 20
    };

    // Act
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    Assert.IsTrue(result.Success);
    Assert.IsNotNull(result.Data);
    Assert.AreEqual(1, result.Data.Page);
    Assert.LessOrEqual(result.Data.Items.Length, 20);
}

[Test]
public async Task GetSponsoredAnalysesList_STierSponsor_Returns30PercentData()
{
    // Arrange
    var query = new GetSponsoredAnalysesListQuery { SponsorId = 100 }; // S tier sponsor

    // Act
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    var firstItem = result.Data.Items.First();
    Assert.AreEqual(30, firstItem.AccessPercentage);
    Assert.IsNotNull(firstItem.OverallHealthScore);
    Assert.IsNull(firstItem.VigorScore); // L tier field
    Assert.IsNull(firstItem.FarmerName); // XL tier field
}

[Test]
public async Task GetSponsoredAnalysesList_WithCropTypeFilter_ReturnsFilteredResults()
{
    // Arrange
    var query = new GetSponsoredAnalysesListQuery
    {
        SponsorId = 789,
        FilterByCropType = "wheat"
    };

    // Act
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    Assert.IsTrue(result.Data.Items.All(x =>
        x.CropType.Contains("wheat", StringComparison.OrdinalIgnoreCase)));
}
```

---

## Performance Considerations

### Database Indexing

Ensure the following indexes exist for optimal performance:

```sql
-- Analysis queries
CREATE INDEX IX_PlantAnalysis_SponsorUserId ON PlantAnalysis(SponsorUserId);
CREATE INDEX IX_PlantAnalysis_AnalysisDate ON PlantAnalysis(AnalysisDate);
CREATE INDEX IX_PlantAnalysis_CropType ON PlantAnalysis(CropType);
CREATE INDEX IX_PlantAnalysis_OverallHealthScore ON PlantAnalysis(OverallHealthScore);

-- Composite index for common query patterns
CREATE INDEX IX_PlantAnalysis_Sponsor_Date ON PlantAnalysis(SponsorUserId, AnalysisDate DESC);
```

### Caching Strategy

Consider implementing caching for:
- Sponsor tier information (15-minute TTL)
- Summary statistics (5-minute TTL)
- Sponsor profile data (30-minute TTL)

### Pagination Best Practices

- Default `pageSize`: 20 items (balanced between performance and UX)
- Maximum `pageSize`: 100 items (prevent excessive data transfer)
- Recommended client-side: Implement infinite scroll with `pageSize=25`

---

## Security Considerations

### Authorization
- Endpoint validates user has `Sponsor` or `Admin` role
- Sponsor can only access analyses from farmers they sponsored
- No cross-sponsor data leakage

### Data Privacy
- Automatic field filtering based on tier prevents unauthorized data access
- Farmer contact information (phone, email) only visible to XL tier sponsors
- Logo and branding information sanitized before display

### Rate Limiting
- Recommended: 60 requests per minute per sponsor
- Implement Redis-based rate limiting for production

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-10-15 | Initial release with tier-based privacy filtering |

---

## Support

**Questions or Issues?**
- API Documentation: [https://ziraai.com/docs/api](https://ziraai.com/docs/api)
- Support Email: support@ziraai.com
- Developer Portal: [https://developers.ziraai.com](https://developers.ziraai.com)

---

**End of Documentation**
