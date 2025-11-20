# Admin Non-Sponsored Analysis Detail API

## Overview

This endpoint allows administrators to view complete analysis details for non-sponsored farmers, displaying the same comprehensive view that farmers see in their own analysis reports.

**Purpose**: Enable admins to thoroughly review analysis details before making sponsorship recommendations or evaluating farmer engagement.

**Endpoint**: `GET /api/admin/sponsorship/non-sponsored/analyses/{plantAnalysisId}`

**Authorization**: Required - JWT Bearer token with Administrator role

**Operation Claim**: `GetNonSponsoredAnalysisDetailQuery` (ID: 140)

---

## Use Cases

1. **Sponsorship Evaluation**: Review detailed analysis reports to assess farmer needs before sponsor matching
2. **Quality Assessment**: Evaluate AI analysis quality and accuracy for non-sponsored users
3. **Support & Guidance**: Understand farmer's specific plant health issues to provide targeted support
4. **Sponsorship Proposals**: Prepare detailed context for sponsor pitch presentations
5. **Farmer Engagement**: Analyze farmer activity patterns and engagement with analysis features

---

## Request Specification

### Endpoint

```http
GET /api/admin/sponsorship/non-sponsored/analyses/{plantAnalysisId}
```

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| plantAnalysisId | integer | Yes | The unique ID of the plant analysis |

### Headers

```http
Authorization: Bearer {admin-jwt-token}
Content-Type: application/json
```

### Query Parameters

None required.

---

## Request Examples

### Example 1: Get Analysis Detail

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/non-sponsored/analyses/5678" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Example 2: PowerShell Request

```powershell
$token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
$headers = @{
    "Authorization" = "Bearer $token"
}

Invoke-RestMethod -Uri "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/non-sponsored/analyses/5678" `
                  -Method Get `
                  -Headers $headers
```

### Example 3: JavaScript/Fetch

```javascript
const response = await fetch(
  'https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/non-sponsored/analyses/5678',
  {
    method: 'GET',
    headers: {
      'Authorization': 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...',
      'Content-Type': 'application/json'
    }
  }
);

const data = await response.json();
console.log('Analysis Detail:', data);
```

---

## Response Specification

### Success Response (200 OK)

Returns complete `PlantAnalysisDetailDto` with all analysis information.

### Response Structure

```json
{
  "data": {
    "id": 5678,
    "analysisId": "ZIRA-2025-11-10-5678",
    "analysisDate": "2025-11-10T14:30:00",
    "analysisStatus": "completed",

    "userId": 789,
    "farmerId": "FARMER-789",
    "sponsorId": null,
    "sponsorUserId": null,
    "sponsorshipCodeId": null,

    "location": "İzmir, Kemalpaşa",
    "latitude": 38.4237,
    "longitude": 27.1428,
    "altitude": 120,

    "fieldId": "FIELD-2025-001",
    "cropType": "Tomato",
    "plantingDate": "2025-09-15T00:00:00",
    "expectedHarvestDate": "2025-12-15T00:00:00",
    "lastFertilization": "2025-10-20T00:00:00",
    "lastIrrigation": "2025-11-08T00:00:00",
    "previousTreatments": ["Fungicide spray - October 10", "Organic fertilizer - October 20"],

    "weatherConditions": "Partly cloudy",
    "temperature": 22.5,
    "humidity": 65.0,
    "soilType": "Clay loam",

    "contactPhone": "+905559876543",
    "contactEmail": "farmer@example.com",

    "urgencyLevel": "Medium",
    "notes": "Lower leaves showing yellowing, need immediate attention",

    "plantIdentification": {
      "species": "Solanum lycopersicum",
      "variety": "Roma Tomato",
      "growthStage": "Vegetative - Early Flowering",
      "confidence": 0.95,
      "identifyingFeatures": [
        "Compound pinnate leaves",
        "Yellow flowers in clusters",
        "Characteristic tomato aroma"
      ],
      "visibleParts": ["Leaves", "Stem", "Flowers", "Developing fruits"]
    },

    "healthAssessment": {
      "vigorScore": 65,
      "leafColor": "Slightly yellowing on lower leaves",
      "leafTexture": "Normal texture, some leaf curl",
      "growthPattern": "Normal vertical growth",
      "structuralIntegrity": "Good stem strength",
      "severity": "Moderate",
      "stressIndicators": [
        "Chlorosis on lower leaves",
        "Minor leaf curling",
        "Reduced new growth rate"
      ],
      "diseaseSymptoms": [
        "Yellowing lower leaves with brown spots",
        "Necrotic lesions on older foliage",
        "Target-like patterns on affected leaves"
      ]
    },

    "nutrientStatus": {
      "nitrogen": "Deficient",
      "phosphorus": "Adequate",
      "potassium": "Adequate",
      "calcium": "Adequate",
      "magnesium": "Slightly Low",
      "sulfur": "Adequate",
      "iron": "Adequate",
      "zinc": "Adequate",
      "manganese": "Adequate",
      "boron": "Adequate",
      "copper": "Adequate",
      "molybdenum": "Adequate",
      "chlorine": "Adequate",
      "nickel": "Adequate",
      "primaryDeficiency": "Nitrogen",
      "secondaryDeficiencies": ["Magnesium"],
      "severity": "Moderate"
    },

    "pestDisease": {
      "pestsDetected": [
        {
          "type": "Aphids",
          "category": "Insect",
          "severity": "Minor",
          "affectedParts": ["New shoots", "Flower buds"],
          "confidence": 0.82
        }
      ],
      "diseasesDetected": [
        {
          "type": "Early Blight (Alternaria solani)",
          "category": "Fungal",
          "severity": "Moderate",
          "affectedParts": ["Lower leaves", "Stems"],
          "confidence": 0.88
        }
      ],
      "damagePattern": "Concentric ring patterns on leaves, spreading upward",
      "affectedAreaPercentage": 15.5,
      "spreadRisk": "Medium - Can spread rapidly in humid conditions",
      "primaryIssue": "Early Blight (Alternaria solani)"
    },

    "environmentalStress": {
      "waterStatus": "Adequate - Regular irrigation schedule",
      "temperatureStress": "None - Within optimal range",
      "lightStress": "None - Adequate sunlight exposure",
      "physicalDamage": "Minor - Some wind damage on edges",
      "chemicalDamage": "None detected",
      "physiologicalDisorders": [
        {
          "type": "Blossom End Rot Risk",
          "severity": "Low",
          "notes": "Monitor calcium uptake and water consistency"
        }
      ],
      "soilHealthIndicators": {
        "salinity": "Normal",
        "phIssue": "Slightly alkaline (pH 7.5) - may affect nutrient availability",
        "organicMatter": "Moderate - Could benefit from additional compost"
      },
      "primaryStressor": "Soil pH slightly alkaline"
    },

    "summary": {
      "overallHealthScore": 65,
      "primaryConcern": "Nitrogen deficiency with secondary Early Blight infection",
      "secondaryConcerns": [
        "Magnesium deficiency developing",
        "Minor aphid infestation",
        "Soil pH affecting nutrient availability"
      ],
      "criticalIssuesCount": 2,
      "confidenceLevel": 0.91,
      "prognosis": "Good - Issues are treatable with immediate intervention",
      "estimatedYieldImpact": "15-20% reduction if left untreated, full recovery possible with treatment"
    },

    "crossFactorInsights": [
      {
        "insight": "Alkaline soil pH is reducing nitrogen uptake efficiency, compounding the deficiency",
        "confidence": 0.87,
        "affectedAspects": ["Nutrient Absorption", "Plant Vigor", "Disease Resistance"],
        "impactLevel": "Moderate"
      },
      {
        "insight": "Early blight combined with nutrient stress creates vulnerability to secondary infections",
        "confidence": 0.84,
        "affectedAspects": ["Disease Spread", "Plant Health", "Yield"],
        "impactLevel": "High"
      },
      {
        "insight": "Recent irrigation schedule is adequate but may need adjustment during treatment period",
        "confidence": 0.79,
        "affectedAspects": ["Fungicide Effectiveness", "Nutrient Uptake"],
        "impactLevel": "Low"
      }
    ],

    "recommendations": {
      "immediate": [
        {
          "action": "Apply nitrogen-rich fertilizer",
          "details": "Use 20-10-10 NPK fertilizer at 2kg per 100m². Apply as side-dressing 15cm from plant base, water thoroughly after application",
          "timeline": "Within 24-48 hours",
          "priority": "High"
        },
        {
          "action": "Begin fungicide treatment for Early Blight",
          "details": "Apply copper-based fungicide (e.g., Copper oxychloride 50% WP at 2g/L). Spray thoroughly covering lower leaves and stems. Apply early morning or evening",
          "timeline": "Within 3 days, repeat every 7 days for 3 weeks",
          "priority": "High"
        },
        {
          "action": "Remove severely infected leaves",
          "details": "Sanitarily remove and destroy leaves with >50% infection. Disinfect tools between cuts. Do not compost infected material",
          "timeline": "Immediately",
          "priority": "High"
        }
      ],
      "shortTerm": [
        {
          "action": "Apply magnesium supplement",
          "details": "Foliar spray of Epsom salt solution (1 tablespoon per liter) every 2 weeks for 6 weeks. Combine with regular watering schedule",
          "timeline": "Starting week 2, continue for 6 weeks",
          "priority": "Medium"
        },
        {
          "action": "Soil pH adjustment",
          "details": "Apply sulfur or acidifying fertilizer to lower pH to 6.5-6.8 range. Test soil before and after treatment. Apply at 50g per m²",
          "timeline": "Week 2-3, retest after 4 weeks",
          "priority": "Medium"
        },
        {
          "action": "Aphid control",
          "details": "Apply neem oil spray (5ml per liter) or insecticidal soap. Target new growth and flower buds. Repeat every 5-7 days if needed",
          "timeline": "Week 1-3",
          "priority": "Medium"
        },
        {
          "action": "Improve air circulation",
          "details": "Prune excess foliage to improve airflow. Stake plants properly. Space adequately for disease prevention",
          "timeline": "Week 2-3",
          "priority": "Low"
        }
      ],
      "preventive": [
        {
          "action": "Regular soil testing schedule",
          "details": "Test soil NPK and pH levels every 2 months. Adjust fertilization based on results. Maintain detailed records",
          "timeline": "Ongoing - Next test in 8 weeks",
          "priority": "Medium"
        },
        {
          "action": "Implement crop rotation",
          "details": "Plan 3-4 year rotation cycle. Avoid planting tomatoes or related crops (peppers, potatoes) in same location next season",
          "timeline": "For next planting season",
          "priority": "Medium"
        },
        {
          "action": "Organic matter incorporation",
          "details": "Add well-composted organic matter (5-7kg per m²) annually. Improves soil structure, pH buffering, and nutrient retention",
          "timeline": "Before next planting season",
          "priority": "Medium"
        },
        {
          "action": "Disease monitoring protocol",
          "details": "Weekly inspection of lower leaves for early disease signs. Early detection enables faster treatment and prevents spread",
          "timeline": "Weekly throughout growing season",
          "priority": "High"
        }
      ],
      "monitoring": [
        {
          "parameter": "Leaf color progression",
          "frequency": "Every 3 days",
          "threshold": "New growth should show normal green color within 10 days of fertilization"
        },
        {
          "parameter": "Disease spread",
          "frequency": "Every 5 days",
          "threshold": "No new lesions appearing after 2nd fungicide application"
        },
        {
          "parameter": "Overall plant vigor",
          "frequency": "Weekly",
          "threshold": "Vigor score should increase to 75+ within 3 weeks"
        },
        {
          "parameter": "Soil pH",
          "frequency": "Every 4 weeks",
          "threshold": "Target pH 6.5-6.8 after treatment"
        }
      ],
      "resourceEstimation": {
        "waterRequiredLiters": "120-150L per week for this plot size during treatment period",
        "fertilizerCostEstimateUsd": "$15-20 for NPK, $5-8 for magnesium supplement",
        "laborHoursEstimate": "Initial treatment: 2-3 hours, Weekly maintenance: 1 hour"
      },
      "localizedRecommendations": {
        "region": "İzmir, Aegean Region",
        "preferredPractices": [
          "Use locally available sulfur products for pH adjustment",
          "Aegean climate ideal for organic fungicide options",
          "Consider local compost sources from municipal programs"
        ],
        "restrictedMethods": [
          "Some systemic fungicides may be restricted in organic zones",
          "Check local agricultural office for approved product list"
        ]
      }
    },

    "imageInfo": {
      "imageUrl": "https://storage.example.com/plant-analysis/2025/11/analysis-5678.jpg",
      "imagePath": "/uploads/plant-images/2025/11/analysis-5678.jpg",
      "format": "jpeg",
      "sizeBytes": 251238,
      "sizeKb": 245.35,
      "sizeMb": 0.24,
      "uploadTimestamp": "2025-11-10T14:29:45"
    },

    "processingInfo": {
      "aiModel": "GPT-4o",
      "workflowVersion": "v2.1.3",
      "processingTimestamp": "2025-11-10T14:30:28",
      "processingTimeMs": 8234,
      "parseSuccess": true,
      "correlationId": "550e8400-e29b-41d4-a716-446655440000",
      "retryCount": 0
    },

    "riskAssessment": {
      "yieldLossProbability": "Moderate (30-40%) if untreated, Low (5-10%) with treatment",
      "timelineToWorsen": "7-14 days - Disease will spread to upper foliage",
      "spreadPotential": "Medium - Can spread to nearby plants through water splash and wind"
    },

    "confidenceNotes": [
      {
        "aspect": "Disease Identification",
        "confidence": 0.88,
        "reason": "Classic early blight symptoms clearly visible with target-like lesion patterns"
      },
      {
        "aspect": "Nutrient Diagnosis",
        "confidence": 0.91,
        "reason": "Chlorosis pattern and location consistent with nitrogen deficiency"
      },
      {
        "aspect": "Treatment Effectiveness",
        "confidence": 0.85,
        "reason": "Based on typical response rates for this disease stage and nutrient condition"
      }
    ],

    "farmerFriendlySummary": "Domates bitkinizde azot eksikliği ve erken yanıklık hastalığı tespit edildi. Alt yapraklarda sararma ve kahverengi lekeler görülüyor. 24-48 saat içinde azot gübresi ve 3 gün içinde fungusit uygulaması yapılması önemli. Hastalıklı yapraklar temiz bir şekilde uzaklaştırılmalı. Doğru müdahale ile tam iyileşme mümkün ve verim kaybı önlenebilir.",

    "tokenUsage": {
      "totalTokens": 15234,
      "promptTokens": 4532,
      "completionTokens": 10702,
      "costUsd": 0.0847,
      "costTry": 2.8745
    },

    "requestMetadata": {
      "userAgent": "ZiraAI-Mobile/2.1.0 (iOS 17.0)",
      "ipAddress": "185.123.45.67",
      "requestTimestamp": "2025-11-10T14:29:30",
      "requestId": "req-5678-2025-11-10",
      "apiVersion": "v1"
    },

    "sponsorshipMetadata": null,

    "success": true,
    "message": "Success",
    "error": false,
    "errorMessage": null
  },
  "success": true,
  "message": "Analysis detail retrieved successfully"
}
```

---

## Response Field Descriptions

### Basic Information

| Field | Type | Description |
|-------|------|-------------|
| id | integer | Internal database ID |
| analysisId | string | Unique analysis identifier (ZIRA-YYYY-MM-DD-ID) |
| analysisDate | datetime | When the analysis was performed |
| analysisStatus | string | Status: `pending`, `completed`, `failed` |

### User & Farmer Information

| Field | Type | Description |
|-------|------|-------------|
| userId | integer | Farmer's user ID |
| farmerId | string | Farmer identifier |
| sponsorId | string | null for non-sponsored analyses |
| sponsorUserId | integer | null for non-sponsored analyses |
| sponsorshipCodeId | integer | null for non-sponsored analyses |

### Location Information

| Field | Type | Description |
|-------|------|-------------|
| location | string | Geographic location (city, district) |
| latitude | decimal | GPS latitude coordinate |
| longitude | decimal | GPS longitude coordinate |
| altitude | integer | Altitude in meters |

### Field & Crop Information

| Field | Type | Description |
|-------|------|-------------|
| fieldId | string | Field identifier |
| cropType | string | Type of crop being analyzed |
| plantingDate | datetime | When the crop was planted |
| expectedHarvestDate | datetime | Expected harvest date |
| lastFertilization | datetime | Last fertilizer application |
| lastIrrigation | datetime | Last irrigation date |
| previousTreatments | string[] | History of treatments applied |

### Environmental Conditions

| Field | Type | Description |
|-------|------|-------------|
| weatherConditions | string | Current weather description |
| temperature | decimal | Temperature in Celsius |
| humidity | decimal | Relative humidity percentage |
| soilType | string | Soil type classification |

### Plant Identification Object

| Field | Type | Description |
|-------|------|-------------|
| species | string | Scientific species name |
| variety | string | Plant variety |
| growthStage | string | Current growth stage |
| confidence | decimal | AI confidence (0-1) |
| identifyingFeatures | string[] | Key identifying characteristics |
| visibleParts | string[] | Plant parts visible in image |

### Health Assessment Object

| Field | Type | Description |
|-------|------|-------------|
| vigorScore | integer | Plant vigor score (0-100) |
| leafColor | string | Leaf color description |
| leafTexture | string | Leaf texture description |
| growthPattern | string | Growth pattern assessment |
| structuralIntegrity | string | Structural strength |
| severity | string | Overall severity: `Minor`, `Moderate`, `Severe` |
| stressIndicators | string[] | Visible stress indicators |
| diseaseSymptoms | string[] | Disease symptoms observed |

### Nutrient Status Object

Contains assessment for all macro and micronutrients:
- **Macronutrients**: Nitrogen, Phosphorus, Potassium, Calcium, Magnesium, Sulfur
- **Micronutrients**: Iron, Zinc, Manganese, Boron, Copper, Molybdenum, Chlorine, Nickel
- **Summary Fields**: primaryDeficiency, secondaryDeficiencies[], severity

### Pest & Disease Object

| Field | Type | Description |
|-------|------|-------------|
| pestsDetected[] | array | Array of detected pests with details |
| diseasesDetected[] | array | Array of detected diseases with details |
| damagePattern | string | Pattern of damage observed |
| affectedAreaPercentage | decimal | Percentage of plant affected |
| spreadRisk | string | Risk of spread: `Low`, `Medium`, `High` |
| primaryIssue | string | Main pest/disease concern |

### Environmental Stress Object

| Field | Type | Description |
|-------|------|-------------|
| waterStatus | string | Water availability assessment |
| temperatureStress | string | Temperature stress level |
| lightStress | string | Light stress level |
| physicalDamage | string | Physical damage description |
| chemicalDamage | string | Chemical damage description |
| physiologicalDisorders[] | array | Physiological disorder details |
| soilHealthIndicators | object | Soil health metrics (salinity, pH, organic matter) |
| primaryStressor | string | Main environmental stressor |

### Summary Object

| Field | Type | Description |
|-------|------|-------------|
| overallHealthScore | integer | Overall health score (0-100) |
| primaryConcern | string | Main issue identified |
| secondaryConcerns | string[] | Additional concerns |
| criticalIssuesCount | integer | Number of critical issues |
| confidenceLevel | decimal | Analysis confidence (0-1) |
| prognosis | string | Expected outcome |
| estimatedYieldImpact | string | Impact on yield if untreated |

### Recommendations Object

Contains four categories of actionable recommendations:

1. **immediate[]**: Urgent actions (24-72 hours)
2. **shortTerm[]**: Short-term actions (1-4 weeks)
3. **preventive[]**: Long-term preventive measures
4. **monitoring[]**: Parameters to monitor with frequency

Each recommendation includes:
- `action`: What to do
- `details`: Detailed instructions
- `timeline`: When to do it
- `priority`: High/Medium/Low

Additional recommendation fields:
- `resourceEstimation`: Water, fertilizer, and labor estimates
- `localizedRecommendations`: Region-specific practices

### Processing Information

| Field | Type | Description |
|-------|------|-------------|
| aiModel | string | AI model used (e.g., GPT-4o) |
| workflowVersion | string | Analysis workflow version |
| processingTimestamp | datetime | When processing completed |
| processingTimeMs | integer | Processing time in milliseconds |
| parseSuccess | boolean | Whether parsing succeeded |
| correlationId | string | Correlation ID for tracking |
| retryCount | integer | Number of retries attempted |

---

## Error Responses

### 401 Unauthorized

Missing or invalid JWT token.

```json
{
  "success": false,
  "message": "Unauthorized access"
}
```

### 403 Forbidden

User lacks required operation claim (GetNonSponsoredAnalysisDetailQuery - ID: 140).

```json
{
  "success": false,
  "message": "You do not have permission to access this resource"
}
```

### 404 Not Found

Analysis not found or is not a non-sponsored analysis.

```json
{
  "data": null,
  "success": false,
  "message": "Non-sponsored analysis not found"
}
```

**Possible Reasons**:
- Analysis ID doesn't exist
- Analysis is sponsored (has SponsorId, SponsorshipCodeId, or SponsorUserId)
- Analysis is inactive (Status = false)

### 500 Internal Server Error

Server-side error during processing.

```json
{
  "success": false,
  "message": "An error occurred while processing the request"
}
```

---

## Implementation Details

### Security

- **Authorization**: SecuredOperation aspect validates admin claims
- **Verification**: Ensures analysis is non-sponsored before returning
- **Logging**: All requests logged via LogAspect
- **Performance**: PerformanceAspect tracks slow queries (>5 seconds)

### Query Logic

```csharp
// Verification: Analysis must be non-sponsored
var analysis = await _plantAnalysisRepository.GetAsync(p =>
    p.Id == request.PlantAnalysisId &&
    p.Status &&
    string.IsNullOrEmpty(p.SponsorId) &&
    p.SponsorshipCodeId == null &&
    p.SponsorUserId == null);
```

### Data Source

Reuses existing `GetPlantAnalysisDetailQuery` to ensure consistency with farmer's view:

```csharp
// Delegates to farmer-facing query for consistent view
var farmerQuery = new GetPlantAnalysisDetailQuery { Id = request.PlantAnalysisId };
var result = await _mediator.Send(farmerQuery, cancellationToken);
```

---

## Integration Notes

### Frontend Integration

```javascript
// Example: Fetch analysis detail on button click
async function viewAnalysisDetail(plantAnalysisId) {
  try {
    const response = await fetch(
      `${API_BASE_URL}/api/admin/sponsorship/non-sponsored/analyses/${plantAnalysisId}`,
      {
        headers: {
          'Authorization': `Bearer ${getAdminToken()}`,
          'Content-Type': 'application/json'
        }
      }
    );

    if (!response.ok) {
      if (response.status === 404) {
        alert('Analysis not found or is already sponsored');
      } else if (response.status === 403) {
        alert('You do not have permission to view this analysis');
      }
      return;
    }

    const { data } = await response.json();

    // Display analysis details
    displayAnalysisDetail(data);

  } catch (error) {
    console.error('Error fetching analysis detail:', error);
    alert('Failed to load analysis details');
  }
}

function displayAnalysisDetail(analysis) {
  // Health Score
  console.log(`Health Score: ${analysis.summary.overallHealthScore}/100`);

  // Primary Concern
  console.log(`Primary Concern: ${analysis.summary.primaryConcern}`);

  // Immediate Actions
  analysis.recommendations.immediate.forEach(rec => {
    console.log(`⚠️ ${rec.action} - ${rec.timeline}`);
    console.log(`   ${rec.details}`);
  });

  // Farmer Information
  console.log(`Farmer: ${analysis.userId} - Contact: ${analysis.contactPhone}`);
}
```

### Mobile Integration

```dart
// Flutter/Dart example
class AdminAnalysisDetailService {
  Future<PlantAnalysisDetail?> getAnalysisDetail(int plantAnalysisId) async {
    final token = await getAdminToken();
    final url = '$baseUrl/api/admin/sponsorship/non-sponsored/analyses/$plantAnalysisId';

    final response = await http.get(
      Uri.parse(url),
      headers: {
        'Authorization': 'Bearer $token',
        'Content-Type': 'application/json',
      },
    );

    if (response.statusCode == 200) {
      final json = jsonDecode(response.body);
      return PlantAnalysisDetail.fromJson(json['data']);
    } else if (response.statusCode == 404) {
      throw Exception('Analysis not found or already sponsored');
    } else if (response.statusCode == 403) {
      throw Exception('Insufficient permissions');
    }

    return null;
  }
}
```

---

## Testing Checklist

### Functional Tests

- [ ] Successfully retrieve analysis detail for valid non-sponsored analysis
- [ ] Return 404 for non-existent analysis ID
- [ ] Return 404 for sponsored analysis (with SponsorId set)
- [ ] Return 403 for user without admin claim
- [ ] Return 401 for missing/invalid token
- [ ] Verify all response fields are populated correctly
- [ ] Verify recommendations array contains immediate/short-term/preventive items
- [ ] Verify image URLs are accessible
- [ ] Verify farmer contact information is included

### Security Tests

- [ ] Non-admin users cannot access endpoint
- [ ] JWT token validation works correctly
- [ ] Operation claim (140) is properly validated
- [ ] Sponsored analyses are not accessible via this endpoint

### Performance Tests

- [ ] Response time < 2 seconds for typical request
- [ ] Performance aspect logs slow queries (>5 seconds)
- [ ] No N+1 query issues when fetching related data

---

## Related Documentation

- [Admin Sponsor View API Documentation](ADMIN_SPONSOR_VIEW_API_DOCUMENTATION.md) - Complete admin API guide
- [Deployment Checklist](DEPLOYMENT_CHECKLIST.md) - Admin operations deployment guide
- [Non-Sponsored Analyses List Endpoint](ADMIN_SPONSOR_VIEW_API_DOCUMENTATION.md#5-get-non-sponsored-analyses) - List view endpoint

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-11-10 | Initial release - Non-sponsored analysis detail endpoint |

---

## Support

For technical support or questions:
- Check related documentation above
- Review error responses section for common issues
- Verify operation claim 140 is assigned to admin group
- Ensure analysis is truly non-sponsored (no SponsorId, SponsorshipCodeId, or SponsorUserId)
