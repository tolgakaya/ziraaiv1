# Crop-Disease Matrix Analytics API Documentation

## 1. Endpoint Metadata

| Property | Value |
|----------|-------|
| **Endpoint** | `GET /api/v1/sponsorship/crop-disease-matrix` |
| **Method** | GET |
| **Authorization** | Required (JWT Bearer) |
| **Roles** | Sponsor, Admin |
| **Cache TTL** | 6 hours (360 minutes) |
| **Operation Claim** | `GetCropDiseaseMatrixQuery` (ID: 166) |
| **Alias** | `sponsorship.analytics.crop-disease-matrix` |
| **Rate Limiting** | Standard API rate limits apply |
| **Version** | API v1 |

## 2. Purpose & Use Cases

### Purpose
Provides comprehensive crop-disease correlation analytics that help sponsors identify market opportunities based on disease patterns across different crop types. The endpoint analyzes historical plant analysis data to reveal:
- Which crops are most affected by specific diseases
- Seasonal patterns of disease occurrence
- Geographic distribution of disease outbreaks
- Product recommendation opportunities
- Actionable market intelligence

### Use Cases

**For Sponsors:**
1. **Market Intelligence**: Identify which crop-disease combinations represent the largest market opportunities
2. **Product Positioning**: Understand which products (fungicides, insecticides, etc.) are in highest demand
3. **Regional Targeting**: Focus marketing efforts on geographic regions with highest disease incidence
4. **Seasonal Planning**: Plan inventory and campaigns around seasonal disease peaks
5. **Competitive Analysis**: Understand disease patterns in specific crop markets

**For Admins:**
1. **Platform Analytics**: View aggregate crop-disease patterns across all sponsors
2. **Market Research**: Identify emerging disease trends and market gaps
3. **Platform Health**: Monitor disease distribution and analysis patterns

### Business Value
- **Data-Driven Decisions**: Replace guesswork with actual disease correlation data
- **Revenue Optimization**: Focus on high-volume, high-value crop-disease combinations
- **Market Timing**: Launch campaigns during seasonal disease peaks
- **ROI Improvement**: Target regions and crops with documented disease challenges

## 3. Request Structure

### HTTP Request
```http
GET /api/v1/sponsorship/crop-disease-matrix HTTP/1.1
Host: api.ziraai.com
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

### Request Headers
| Header | Required | Description |
|--------|----------|-------------|
| `Authorization` | Yes | JWT Bearer token obtained from login |
| `Content-Type` | Yes | Must be `application/json` |

### Query Parameters
**None** - This endpoint does not accept query parameters. Data filtering is automatic based on user role:
- **Sponsors**: Automatically filtered to show only analyses linked to their company
- **Admins**: Shows aggregate data across all sponsors

### Authentication Context
The endpoint uses the authenticated user's JWT token to determine:
- User role (Sponsor or Admin)
- User ID for filtering data (for Sponsors)
- Authorization via SecuredOperation aspect

### Request Example
```bash
# Sponsor Request
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/crop-disease-matrix" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json"

# Admin Request (identical, role determined by JWT)
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/crop-disease-matrix" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json"
```

## 4. Response Structure

### Success Response (200 OK)
```json
{
  "success": true,
  "message": "Crop-disease matrix retrieved successfully",
  "data": {
    "matrix": [
      {
        "cropType": "Domates",
        "totalAnalyses": 687,
        "diseaseBreakdown": [
          {
            "disease": "Alternaria Yaprak Lekesi",
            "occurrences": 127,
            "percentage": 18.49,
            "averageSeverity": "Moderate",
            "seasonalPeak": "May-June",
            "affectedRegions": ["Adana", "Mersin", "Antalya"],
            "recommendedProducts": [
              {
                "productCategory": "Fungicide",
                "targetDisease": "Alternaria Yaprak Lekesi",
                "estimatedMarketSize": 31750,
                "confidence": "High"
              }
            ]
          }
        ]
      }
    ],
    "topOpportunities": [
      {
        "rank": 1,
        "cropType": "Domates",
        "disease": "Alternaria Yaprak Lekesi",
        "totalOccurrences": 127,
        "estimatedValue": 31750,
        "growthPotential": "High",
        "actionableInsight": "High-volume opportunity in Adana, Mersin, Antalya regions. Focus on fungicide solutions during May-June peak season."
      }
    ],
    "sponsorId": 42,
    "generatedAt": "2025-11-12T14:30:00Z"
  }
}
```

### Error Responses

#### 401 Unauthorized
```json
{
  "success": false,
  "message": "Authentication required",
  "data": null
}
```

#### 403 Forbidden
```json
{
  "success": false,
  "message": "User does not have required operation claim: GetCropDiseaseMatrixQuery",
  "data": null
}
```

#### 500 Internal Server Error
```json
{
  "success": false,
  "message": "An error occurred while retrieving crop-disease matrix",
  "data": null
}
```

## 5. Data Models (DTOs)

### CropDiseaseMatrixDto
Main response container for crop-disease correlation analytics.

```csharp
public class CropDiseaseMatrixDto
{
    /// <summary>Matrix of crop types with their disease breakdowns</summary>
    public List<CropAnalysisDto> Matrix { get; set; }

    /// <summary>Top market opportunities based on disease-crop combinations</summary>
    public List<MarketOpportunityDto> TopOpportunities { get; set; }

    /// <summary>Sponsor ID for the analysis (null if admin view)</summary>
    public int? SponsorId { get; set; }

    /// <summary>Timestamp when the matrix was generated</summary>
    public DateTime GeneratedAt { get; set; }
}
```

### CropAnalysisDto
Analysis of a specific crop type with disease breakdown.

```csharp
public class CropAnalysisDto
{
    /// <summary>Crop type name (e.g., "Domates", "Biber")</summary>
    public string CropType { get; set; }

    /// <summary>Total number of analyses for this crop</summary>
    public int TotalAnalyses { get; set; }

    /// <summary>Breakdown of diseases affecting this crop</summary>
    public List<DiseaseBreakdownDto> DiseaseBreakdown { get; set; }
}
```

### DiseaseBreakdownDto
Detailed breakdown of a specific disease for a crop.

```csharp
public class DiseaseBreakdownDto
{
    /// <summary>Disease name</summary>
    public string Disease { get; set; }

    /// <summary>Number of times this disease occurred</summary>
    public int Occurrences { get; set; }

    /// <summary>Percentage of total analyses for this crop</summary>
    public decimal Percentage { get; set; }

    /// <summary>Average severity level (e.g., "Low", "Moderate", "High")</summary>
    public string AverageSeverity { get; set; }

    /// <summary>Peak season for this disease (e.g., "May-June")</summary>
    public string SeasonalPeak { get; set; }

    /// <summary>Geographic regions most affected</summary>
    public List<string> AffectedRegions { get; set; }

    /// <summary>Recommended product categories for treatment</summary>
    public List<RecommendedProductDto> RecommendedProducts { get; set; }
}
```

### RecommendedProductDto
Product recommendation based on disease analysis.

```csharp
public class RecommendedProductDto
{
    /// <summary>Product category (e.g., "Fungicide", "Insecticide")</summary>
    public string ProductCategory { get; set; }

    /// <summary>Disease this product targets</summary>
    public string TargetDisease { get; set; }

    /// <summary>Estimated market size in TL (occurrences × 250)</summary>
    public decimal EstimatedMarketSize { get; set; }

    /// <summary>Confidence level based on data volume</summary>
    public string Confidence { get; set; }
}
```

### MarketOpportunityDto
Ranked market opportunity based on crop-disease combination.

```csharp
public class MarketOpportunityDto
{
    /// <summary>Rank in opportunity list (1 = highest)</summary>
    public int Rank { get; set; }

    /// <summary>Crop type</summary>
    public string CropType { get; set; }

    /// <summary>Disease name</summary>
    public string Disease { get; set; }

    /// <summary>Total number of occurrences</summary>
    public int TotalOccurrences { get; set; }

    /// <summary>Estimated market value in TL</summary>
    public decimal EstimatedValue { get; set; }

    /// <summary>Growth potential rating</summary>
    public string GrowthPotential { get; set; }

    /// <summary>Business-actionable insight</summary>
    public string ActionableInsight { get; set; }
}
```

## 6. Frontend Integration Notes

### React/Angular Integration

```typescript
// TypeScript Interface Definitions
interface CropDiseaseMatrixDto {
  matrix: CropAnalysisDto[];
  topOpportunities: MarketOpportunityDto[];
  sponsorId: number | null;
  generatedAt: string;
}

interface CropAnalysisDto {
  cropType: string;
  totalAnalyses: number;
  diseaseBreakdown: DiseaseBreakdownDto[];
}

interface DiseaseBreakdownDto {
  disease: string;
  occurrences: number;
  percentage: number;
  averageSeverity: string;
  seasonalPeak: string;
  affectedRegions: string[];
  recommendedProducts: RecommendedProductDto[];
}

interface RecommendedProductDto {
  productCategory: string;
  targetDisease: string;
  estimatedMarketSize: number;
  confidence: string;
}

interface MarketOpportunityDto {
  rank: number;
  cropType: string;
  disease: string;
  totalOccurrences: number;
  estimatedValue: number;
  growthPotential: string;
  actionableInsight: string;
}

// API Service
class SponsorshipAnalyticsService {
  async getCropDiseaseMatrix(): Promise<ApiResponse<CropDiseaseMatrixDto>> {
    const response = await fetch('/api/v1/sponsorship/crop-disease-matrix', {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${this.getToken()}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error('Failed to fetch crop-disease matrix');
    }

    return await response.json();
  }
}
```

### Flutter/Mobile Integration

```dart
// Dart Model Classes
class CropDiseaseMatrixDto {
  final List<CropAnalysisDto> matrix;
  final List<MarketOpportunityDto> topOpportunities;
  final int? sponsorId;
  final DateTime generatedAt;

  CropDiseaseMatrixDto.fromJson(Map<String, dynamic> json)
    : matrix = (json['matrix'] as List).map((e) => CropAnalysisDto.fromJson(e)).toList(),
      topOpportunities = (json['topOpportunities'] as List).map((e) => MarketOpportunityDto.fromJson(e)).toList(),
      sponsorId = json['sponsorId'],
      generatedAt = DateTime.parse(json['generatedAt']);
}

// API Service
class SponsorshipAnalyticsService {
  final ApiClient _apiClient;

  Future<ApiResponse<CropDiseaseMatrixDto>> getCropDiseaseMatrix() async {
    final response = await _apiClient.get('/api/v1/sponsorship/crop-disease-matrix');

    if (response.statusCode == 200) {
      return ApiResponse.fromJson(
        response.data,
        (data) => CropDiseaseMatrixDto.fromJson(data)
      );
    }

    throw ApiException(response.statusCode, response.data['message']);
  }
}
```

### Caching Strategy
- **Client-Side**: Cache response for 5 minutes to reduce unnecessary requests
- **Server-Side**: Automatic 6-hour Redis cache (handled by backend)
- **Cache Invalidation**: Consider invalidating when new plant analyses are added (optional)

### UI Recommendations

1. **Matrix Visualization**
   - Use a heat map or matrix grid to show crop-disease correlations
   - Color intensity based on occurrence frequency
   - Interactive tooltips showing detailed breakdown

2. **Top Opportunities**
   - Display as a ranked list or card layout
   - Highlight estimated market value prominently
   - Show actionable insights clearly
   - Include "View Details" button linking to disease breakdown

3. **Filtering Options**
   - Filter by crop type
   - Filter by severity level
   - Filter by seasonal peak
   - Filter by region (from affectedRegions)

4. **Data Visualization**
   - Pie charts for disease distribution per crop
   - Bar charts for seasonal peak analysis
   - Geographic heat map for affected regions
   - Trend lines for growth potential

5. **Empty State**
   - Show friendly message if no data available
   - Suggest waiting for more plant analyses
   - Provide guidance on how data is collected

### Performance Considerations
- Response size typically 50-200KB depending on data volume
- Consider pagination if displaying all matrix data at once
- Use virtual scrolling for large lists
- Lazy load detailed breakdowns

## 7. Complete Examples

### Example 1: Sponsor Request with Comprehensive Data

**Request:**
```bash
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/crop-disease-matrix" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiI0MiIsInJvbGUiOiJTcG9uc29yIn0..." \
  -H "Content-Type: application/json"
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Crop-disease matrix retrieved successfully",
  "data": {
    "matrix": [
      {
        "cropType": "Domates",
        "totalAnalyses": 687,
        "diseaseBreakdown": [
          {
            "disease": "Alternaria Yaprak Lekesi",
            "occurrences": 127,
            "percentage": 18.49,
            "averageSeverity": "Moderate",
            "seasonalPeak": "May-June",
            "affectedRegions": ["Adana", "Mersin", "Antalya"],
            "recommendedProducts": [
              {
                "productCategory": "Fungicide",
                "targetDisease": "Alternaria Yaprak Lekesi",
                "estimatedMarketSize": 31750,
                "confidence": "High"
              }
            ]
          },
          {
            "disease": "Fusarium Solgunluğu",
            "occurrences": 89,
            "percentage": 12.95,
            "averageSeverity": "High",
            "seasonalPeak": "July-August",
            "affectedRegions": ["İzmir", "Manisa"],
            "recommendedProducts": [
              {
                "productCategory": "Fungicide",
                "targetDisease": "Fusarium Solgunluğu",
                "estimatedMarketSize": 22250,
                "confidence": "High"
              }
            ]
          },
          {
            "disease": "Beyaz Sinek",
            "occurrences": 76,
            "percentage": 11.06,
            "averageSeverity": "Moderate",
            "seasonalPeak": "June",
            "affectedRegions": ["Adana", "Hatay"],
            "recommendedProducts": [
              {
                "productCategory": "Insecticide",
                "targetDisease": "Beyaz Sinek",
                "estimatedMarketSize": 19000,
                "confidence": "High"
              }
            ]
          }
        ]
      },
      {
        "cropType": "Biber",
        "totalAnalyses": 423,
        "diseaseBreakdown": [
          {
            "disease": "Antraknoz",
            "occurrences": 94,
            "percentage": 22.22,
            "averageSeverity": "High",
            "seasonalPeak": "August",
            "affectedRegions": ["Gaziantep", "Kahramanmaraş"],
            "recommendedProducts": [
              {
                "productCategory": "Fungicide",
                "targetDisease": "Antraknoz",
                "estimatedMarketSize": 23500,
                "confidence": "High"
              }
            ]
          },
          {
            "disease": "Trips",
            "occurrences": 67,
            "percentage": 15.84,
            "averageSeverity": "Moderate",
            "seasonalPeak": "June-July",
            "affectedRegions": ["Adana", "Mersin"],
            "recommendedProducts": [
              {
                "productCategory": "Insecticide",
                "targetDisease": "Trips",
                "estimatedMarketSize": 16750,
                "confidence": "High"
              }
            ]
          }
        ]
      },
      {
        "cropType": "Salatalık",
        "totalAnalyses": 312,
        "diseaseBreakdown": [
          {
            "disease": "Külleme",
            "occurrences": 82,
            "percentage": 26.28,
            "averageSeverity": "Moderate",
            "seasonalPeak": "May",
            "affectedRegions": ["Antalya", "Muğla"],
            "recommendedProducts": [
              {
                "productCategory": "Fungicide",
                "targetDisease": "Külleme",
                "estimatedMarketSize": 20500,
                "confidence": "High"
              }
            ]
          }
        ]
      }
    ],
    "topOpportunities": [
      {
        "rank": 1,
        "cropType": "Domates",
        "disease": "Alternaria Yaprak Lekesi",
        "totalOccurrences": 127,
        "estimatedValue": 31750,
        "growthPotential": "High",
        "actionableInsight": "High-volume opportunity in Adana, Mersin, Antalya regions with 127 occurrences. Focus on fungicide solutions during May-June peak season. Moderate severity level indicates consistent demand."
      },
      {
        "rank": 2,
        "cropType": "Biber",
        "disease": "Antraknoz",
        "totalOccurrences": 94,
        "estimatedValue": 23500,
        "growthPotential": "High",
        "actionableInsight": "Strong opportunity in Gaziantep, Kahramanmaraş regions with 94 occurrences. Target fungicide products for August peak season. High severity level suggests urgent treatment needs."
      },
      {
        "rank": 3,
        "cropType": "Domates",
        "disease": "Fusarium Solgunluğu",
        "totalOccurrences": 89,
        "estimatedValue": 22250,
        "growthPotential": "Medium",
        "actionableInsight": "Moderate opportunity in İzmir, Manisa regions with 89 occurrences. Focus on fungicide solutions during July-August peak season. High severity level indicates critical treatment timing."
      },
      {
        "rank": 4,
        "cropType": "Salatalık",
        "disease": "Külleme",
        "totalOccurrences": 82,
        "estimatedValue": 20500,
        "growthPotential": "Medium",
        "actionableInsight": "Solid opportunity in Antalya, Muğla regions with 82 occurrences. Target fungicide products for May peak season. Moderate severity suggests consistent market demand."
      },
      {
        "rank": 5,
        "cropType": "Domates",
        "disease": "Beyaz Sinek",
        "totalOccurrences": 76,
        "estimatedValue": 19000,
        "growthPotential": "Medium",
        "actionableInsight": "Good opportunity in Adana, Hatay regions with 76 occurrences. Focus on insecticide solutions during June peak season. Moderate severity indicates steady treatment needs."
      }
    ],
    "sponsorId": 42,
    "generatedAt": "2025-11-12T14:30:00Z"
  }
}
```

### Example 2: Admin Request (All Sponsors Aggregate)

**Request:**
```bash
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/crop-disease-matrix" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiIxIiwicm9sZSI6IkFkbWluIn0..." \
  -H "Content-Type: application/json"
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Crop-disease matrix retrieved successfully",
  "data": {
    "matrix": [
      {
        "cropType": "Domates",
        "totalAnalyses": 2847,
        "diseaseBreakdown": [
          {
            "disease": "Alternaria Yaprak Lekesi",
            "occurrences": 523,
            "percentage": 18.37,
            "averageSeverity": "Moderate",
            "seasonalPeak": "May-June",
            "affectedRegions": ["Adana", "Mersin", "Antalya", "Hatay", "İzmir"],
            "recommendedProducts": [
              {
                "productCategory": "Fungicide",
                "targetDisease": "Alternaria Yaprak Lekesi",
                "estimatedMarketSize": 130750,
                "confidence": "Very High"
              }
            ]
          }
        ]
      }
    ],
    "topOpportunities": [
      {
        "rank": 1,
        "cropType": "Domates",
        "disease": "Alternaria Yaprak Lekesi",
        "totalOccurrences": 523,
        "estimatedValue": 130750,
        "growthPotential": "Very High",
        "actionableInsight": "Platform-wide high-volume opportunity with 523 total occurrences across Adana, Mersin, Antalya, Hatay, İzmir regions. Peak season May-June. Significant market for fungicide products."
      }
    ],
    "sponsorId": null,
    "generatedAt": "2025-11-12T14:30:00Z"
  }
}
```

**Note**: Admin response shows `sponsorId: null` and aggregates data across all sponsors.

### Example 3: No Data Available (New Sponsor)

**Request:**
```bash
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/crop-disease-matrix" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiI5OTkiLCJyb2xlIjoiU3BvbnNvciJ9..." \
  -H "Content-Type: application/json"
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Crop-disease matrix retrieved successfully",
  "data": {
    "matrix": [],
    "topOpportunities": [],
    "sponsorId": 999,
    "generatedAt": "2025-11-12T14:30:00Z"
  }
}
```

**Note**: Empty arrays indicate no plant analyses linked to this sponsor yet. UI should handle this gracefully with an empty state message.

## 8. Error Handling

### Common Error Scenarios

#### 1. Missing or Invalid JWT Token
**Scenario**: User not authenticated or token expired
**HTTP Status**: 401 Unauthorized
```json
{
  "success": false,
  "message": "Authentication required",
  "data": null
}
```
**Client Action**: Redirect to login page, clear stored token

#### 2. Insufficient Permissions
**Scenario**: User role is not Sponsor or Admin
**HTTP Status**: 403 Forbidden
```json
{
  "success": false,
  "message": "User does not have required operation claim: GetCropDiseaseMatrixQuery",
  "data": null
}
```
**Client Action**: Show permission denied message, hide analytics features

#### 3. Operation Claim Not Assigned
**Scenario**: User's group doesn't have GetCropDiseaseMatrixQuery claim
**HTTP Status**: 403 Forbidden
```json
{
  "success": false,
  "message": "User does not have required operation claim: GetCropDiseaseMatrixQuery",
  "data": null
}
```
**Client Action**: Contact support message, verify database migration ran successfully

#### 4. Database Connection Error
**Scenario**: Database unavailable or connection timeout
**HTTP Status**: 500 Internal Server Error
```json
{
  "success": false,
  "message": "An error occurred while retrieving crop-disease matrix",
  "data": null
}
```
**Client Action**: Show retry button, display friendly error message

#### 5. Cache Service Error
**Scenario**: Redis cache unavailable (non-critical, falls back to database)
**HTTP Status**: 200 OK (transparent to client)
**Behavior**: Endpoint continues to work without caching layer
**Client Action**: None needed, performance may be slightly slower

### Error Handling Best Practices

**For Frontend Developers:**

```typescript
async function fetchCropDiseaseMatrix() {
  try {
    const response = await apiService.getCropDiseaseMatrix();

    if (response.success) {
      // Handle success
      displayMatrix(response.data);
    } else {
      // Handle API-level error
      showError(response.message);
    }
  } catch (error) {
    if (error.status === 401) {
      // Redirect to login
      redirectToLogin();
    } else if (error.status === 403) {
      // Permission denied
      showPermissionDenied();
    } else if (error.status === 500) {
      // Server error - show retry option
      showRetryDialog();
    } else {
      // Network or unknown error
      showGenericError();
    }
  }
}
```

**For Mobile Developers:**

```dart
Future<void> fetchCropDiseaseMatrix() async {
  try {
    final response = await _analyticsService.getCropDiseaseMatrix();

    if (response.success) {
      setState(() {
        _matrixData = response.data;
      });
    } else {
      _showError(response.message);
    }
  } on ApiException catch (e) {
    if (e.statusCode == 401) {
      _navigateToLogin();
    } else if (e.statusCode == 403) {
      _showPermissionDenied();
    } else if (e.statusCode == 500) {
      _showRetryDialog();
    } else {
      _showGenericError();
    }
  }
}
```

### Logging and Debugging

**Server-Side Logging:**
- All errors logged with correlation ID for tracking
- Log format: `[CropDiseaseMatrix] Error retrieving matrix for sponsor {sponsorId}: {errorMessage}`
- Critical errors trigger alerts in monitoring system

**Client-Side Logging:**
```typescript
// Log API calls for debugging
console.log('[Analytics] Fetching crop-disease matrix', {
  timestamp: new Date().toISOString(),
  userId: currentUser.id,
  role: currentUser.role
});

// Log errors with context
console.error('[Analytics] Failed to fetch matrix', {
  error: error.message,
  statusCode: error.status,
  userId: currentUser.id
});
```

### Retry Strategy

**Recommended Retry Logic:**
- **401/403**: Do NOT retry, redirect to login or show permission error
- **500/502/503**: Retry up to 3 times with exponential backoff (1s, 2s, 4s)
- **Network errors**: Retry up to 3 times with exponential backoff
- **429 Rate Limit**: Respect Retry-After header, then retry

```typescript
async function fetchWithRetry(maxRetries = 3) {
  for (let i = 0; i < maxRetries; i++) {
    try {
      return await apiService.getCropDiseaseMatrix();
    } catch (error) {
      if (error.status === 401 || error.status === 403) {
        throw error; // Don't retry auth errors
      }

      if (i === maxRetries - 1) throw error; // Last attempt failed

      const delay = Math.pow(2, i) * 1000; // Exponential backoff
      await sleep(delay);
    }
  }
}
```

---

## Database Migration Instructions

To enable this endpoint, the following SQL migration must be executed:

**File**: `claudedocs/AdminOperations/migrations/166_add_crop_disease_matrix_operation_claim.sql`

```sql
-- Step 1: Add OperationClaim
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description", "CreatedDate")
VALUES (166, 'GetCropDiseaseMatrixQuery', 'sponsorship.analytics.crop-disease-matrix',
        'View crop-disease correlation analytics for sponsors with market opportunities', NOW());

-- Step 2: Assign to Admin (GroupId=1)
INSERT INTO "GroupClaims" ("GroupId", "ClaimId", "CreatedDate") VALUES (1, 166, NOW());

-- Step 3: Assign to Sponsor (GroupId=3)
INSERT INTO "GroupClaims" ("GroupId", "ClaimId", "CreatedDate") VALUES (3, 166, NOW());
```

**Verification Query:**
```sql
SELECT gc.*, g."GroupName", oc."Name", oc."Alias"
FROM "GroupClaims" gc
JOIN "Groups" g ON gc."GroupId" = g."Id"
JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE oc."Id" = 166;
```

---

**Document Version**: 1.0
**Last Updated**: 2025-11-12
**Author**: ZiraAI Development Team
**Status**: Ready for Implementation
