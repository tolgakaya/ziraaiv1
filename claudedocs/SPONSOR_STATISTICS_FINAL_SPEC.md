# Sponsor İstatistikleri - Final Specification

**Tarih:** 2025-01-25  
**Branch:** feature/sponsor-statistics  
**Durum:** User Feedback Uygulandı ✅

---

## Executive Summary

User feedback'i doğrultusunda **4 core endpoint** belirlendi:
1. ✅ **Messaging Analytics** (Priority 1 - Quick Win)
2. ✅ **Impact Analytics** (Priority 2 - High Value)
3. ✅ **Temporal Analytics** (Priority 2 - High Value)
4. ✅ **ROI Analytics** (Priority 2 - Simplified, kanal maliyetleri yok)

**Mevcut 4 endpoint korunuyor:**
- Dashboard Summary
- Package Distribution
- Code Analysis
- Link Statistics

**Çıkarılanlar (User Request):**
- ❌ Dashboard trend indicators, alerts
- ❌ Link statistics performance metrics
- ❌ Channel cost tracking (SMS/WhatsApp costs)
- ❌ Background jobs (Hangfire)
- ❌ Benchmark Analytics (gelecekte opsiyonel)

---

## 📊 YENİ Endpoint 1: Messaging Analytics (Priority 1) 🚀

### Endpoint: `GET /api/v1/sponsorship/messaging-analytics`
**Effort:** 2-3 gün  
**Value:** ⭐⭐⭐⭐⭐ YÜKSEK  
**Complexity:** DÜŞÜK

### Sunulacak İstatistikler

#### Message Volume Metrics
```csharp
public class SponsorMessagingAnalyticsDto
{
    // Volume
    public int TotalMessagesSent { get; set; }           // Sponsor → Farmer
    public int TotalMessagesReceived { get; set; }       // Farmer → Sponsor
    public int UnreadMessagesCount { get; set; }         // Okunmamış mesajlar
    
    // Response Metrics
    public decimal AverageResponseTimeHours { get; set; }  // Ortalama yanıt süresi (saat)
    public decimal ResponseRate { get; set; }              // Yanıt oranı %
    
    // Conversation Metrics  
    public int TotalConversations { get; set; }            // Toplam konuşma sayısı
    public int ActiveConversations { get; set; }           // Son 7 günde aktif
    public decimal AverageMessagesPerConvo { get; set; }   // Konuşma başına mesaj
    
    // Content Types
    public int TextMessageCount { get; set; }
    public int VoiceMessageCount { get; set; }
    public int AttachmentCount { get; set; }
    
    // Satisfaction
    public decimal AverageMessageRating { get; set; }      // 1-5 ortalama
    public int PositiveRatingsCount { get; set; }          // 4-5 puan
    public int NegativeRatingsCount { get; set; }          // 1-2 puan
    
    // Most Active Conversations (Top 10)
    public List<ConversationSummary> MostActiveConversations { get; set; }
}

public class ConversationSummary
{
    public int AnalysisId { get; set; }
    public string FarmerName { get; set; }              // Tier-based privacy
    public int MessageCount { get; set; }
    public DateTime LastMessageDate { get; set; }
    public int UnreadCount { get; set; }
    public decimal? AverageRating { get; set; }
}
```

### Data Source
`AnalysisMessage` entity - mevcut

### Implementation Notes
- Cache: 15 dakika
- Privacy: Tier-based farmer name filtering
- Query optimization: Index on (PlantAnalysisId, SentDate)

---

## 📊 YENİ Endpoint 2: Impact Analytics (Priority 2) 🎯

### Endpoint: `GET /api/v1/sponsorship/impact-analytics`
**Effort:** 5-7 gün  
**Value:** ⭐⭐⭐⭐⭐ ÇOK YÜKSEK  
**Complexity:** ORTA

### Sunulacak İstatistikler

```csharp
public class SponsorImpactAnalyticsDto
{
    // Farmer Impact
    public int TotalFarmersReached { get; set; }          // Toplam ulaşılan farmer
    public int ActiveFarmers { get; set; }                // Son 30 günde aktif
    public decimal FarmerRetentionRate { get; set; }      // Retention % (MoM)
    public decimal AverageFarmerLifetimeDays { get; set; } // Ortalama lifetime
    
    // Agricultural Impact
    public int TotalCropsAnalyzed { get; set; }           // Analiz edilen ürün sayısı
    public int UniqueCropTypes { get; set; }              // Kaç farklı ürün
    public int DiseasesDetected { get; set; }             // Tespit edilen hastalık
    public int CriticalIssuesResolved { get; set; }       // Kritik sorunlar
    
    // Geographic Reach
    public int CitiesReached { get; set; }                // Kaç şehir
    public int DistrictsReached { get; set; }             // Kaç ilçe
    public List<CityImpact> TopCities { get; set; }       // En aktif şehirler
    
    // Severity Distribution
    public SeverityStats SeverityDistribution { get; set; }
    
    // Crop Distribution
    public List<CropStat> TopCrops { get; set; }          // En çok analiz edilen
    
    // Disease Distribution  
    public List<DiseaseStat> TopDiseases { get; set; }    // En çok görülen
}

public class CityImpact
{
    public string CityName { get; set; }
    public int FarmerCount { get; set; }
    public int AnalysisCount { get; set; }
}

public class SeverityStats
{
    public int LowCount { get; set; }
    public int ModerateCount { get; set; }
    public int HighCount { get; set; }
    public int CriticalCount { get; set; }
}

public class CropStat
{
    public string CropType { get; set; }
    public int AnalysisCount { get; set; }
    public decimal Percentage { get; set; }
}

public class DiseaseStat
{
    public string DiseaseName { get; set; }
    public string Category { get; set; }
    public int OccurrenceCount { get; set; }
    public decimal Percentage { get; set; }
}
```

### Data Source
- `PlantAnalysis` entity (SponsorCompanyId filtering)
- `UserSubscription` entity (farmer lifetime calculation)

### Implementation Notes
- Cache: 6 saat (heavy computation)
- Location parsing: Extract city/district from Location field
- Index optimization: (SponsorCompanyId, AnalysisDate, Location, CropType)

### Marketing Value
Bu endpoint sponsor'un impact story'sini oluşturur:
> "500 farmer'a ulaştık, 35 farklı şehirde, 150 kritik sorunu çözdük"

---

## 📊 YENİ Endpoint 3: Temporal Analytics (Priority 2) 📈

### Endpoint: `GET /api/v1/sponsorship/temporal-analytics`
**Query Parameters:**
- `startDate` (DateTime, required)
- `endDate` (DateTime, required)
- `groupBy` (enum: Day, Week, Month)

**Effort:** 5-7 gün  
**Value:** ⭐⭐⭐⭐⭐ ÇOK YÜKSEK  
**Complexity:** ORTA

### Sunulacak İstatistikler

```csharp
public class SponsorTemporalAnalyticsDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string GroupBy { get; set; }  // Day, Week, Month
    
    // Time Series Data
    public List<TimePeriodData> TimeSeriesData { get; set; }
    
    // Trend Summary
    public TrendSummary TrendAnalysis { get; set; }
    
    // Peak Performance
    public PeakPerformance PeakTimes { get; set; }
}

public class TimePeriodData
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string PeriodLabel { get; set; }       // "2025-01-01", "Week 3", "January 2025"
    
    // Metrics
    public int CodesDistributed { get; set; }
    public int CodesRedeemed { get; set; }
    public int AnalysesPerformed { get; set; }
    public int NewFarmers { get; set; }
    public int ActiveFarmers { get; set; }
    public int MessagesSent { get; set; }
    public int MessagesReceived { get; set; }
}

public class TrendSummary
{
    public string TrendDirection { get; set; }    // "Up", "Down", "Stable"
    public decimal PercentageChange { get; set; } // Son periyoda göre %
    
    public decimal CodesRedeemedGrowth { get; set; }
    public decimal AnalysisGrowth { get; set; }
    public decimal FarmerGrowth { get; set; }
    public decimal EngagementGrowth { get; set; }
}

public class PeakPerformance
{
    public DateTime PeakAnalysisDate { get; set; }
    public int PeakAnalysisCount { get; set; }
    
    public DateTime PeakRedemptionDate { get; set; }
    public int PeakRedemptionCount { get; set; }
}
```

### Data Source
- `SponsorshipCode` (distribution, redemption dates)
- `PlantAnalysis` (analysis dates)
- `AnalysisMessage` (message dates)

### Implementation Notes
- Cache: 1 saat
- Dynamic grouping: SQL GROUP BY DATEPART based on groupBy parameter
- Moving average: 7-day and 30-day calculations

### Use Cases
1. Monthly performance tracking
2. Seasonal pattern detection
3. Budget forecasting
4. Campaign timing optimization

---

## 📊 YENİ Endpoint 4: ROI Analytics (Priority 2) 💰

### Endpoint: `GET /api/v1/sponsorship/roi-analytics`
**Effort:** 3-5 gün  
**Value:** ⭐⭐⭐⭐⭐ ÇOK YÜKSEK  
**Complexity:** DÜŞÜK-ORTA

### Sunulacak İstatistikler

**NOT:** Kanal maliyetleri (SMS, WhatsApp) KULLANILMIYOR. Sadece purchase cost ve analysis value ile ROI hesaplanıyor.

```csharp
public class SponsorROIAnalyticsDto
{
    // Cost Metrics
    public decimal TotalInvestment { get; set; }          // SponsorshipPurchase.TotalAmount toplamı
    public decimal CostPerCode { get; set; }              // TotalInvestment / TotalCodes
    public decimal CostPerRedemption { get; set; }        // TotalInvestment / RedeemedCodes
    public decimal CostPerAnalysis { get; set; }          // TotalInvestment / TotalAnalyses
    public decimal CostPerFarmer { get; set; }            // TotalInvestment / UniqueFarmers
    
    // Value Metrics
    public decimal TotalAnalysesValue { get; set; }       // TotalAnalyses * AnalysisUnitValue
    public decimal AverageAnalysisValue { get; set; }     // AnalysisUnitValue (from config)
    public decimal LifetimeValuePerFarmer { get; set; }   // AvgAnalysesPerFarmer * AnalysisUnitValue
    public decimal ValuePerCode { get; set; }             // AnalysesPerCode * AnalysisUnitValue
    
    // ROI Calculations
    public decimal OverallROI { get; set; }               // (Value - Cost) / Cost * 100
    public List<TierROI> ROIPerTier { get; set; }         // S, M, L, XL ROI
    public List<decimal> ROITrend { get; set; }           // Last 3, 6, 12 months
    
    // Efficiency Metrics
    public decimal UtilizationRate { get; set; }          // Redeemed / Purchased * 100
    public decimal WasteRate { get; set; }                // Expired / Purchased * 100
    public int BreakEvenAnalysisCount { get; set; }       // TotalInvestment / AnalysisUnitValue
    public decimal PaybackPeriodDays { get; set; }        // Days to reach breakeven
}

public class TierROI
{
    public string TierName { get; set; }
    public decimal Investment { get; set; }
    public decimal Value { get; set; }
    public decimal ROI { get; set; }
    public int AnalysisCount { get; set; }
}
```

### Data Source
- `SponsorshipPurchase` (cost data)
- `PlantAnalysis` (value data - count * unit price)
- `SponsorshipCode` (utilization, waste calculation)

### Configuration Required
```json
{
  "Sponsorship": {
    "AnalysisUnitValue": 50.00
  }
}
```

### Implementation Notes
- Cache: 12 saat
- No channel costs tracked
- Simple ROI formula: (Analysis Count * Unit Value - Total Purchase Cost) / Total Purchase Cost * 100

### Financial Insights
- Tier comparison: Hangi tier daha karlı?
- Utilization optimization: Waste rate azaltma
- Breakeven analysis: Kaç analiz yapılması gerekiyor?

---

## 📋 Implementation Checklist

### Phase 1: Messaging Analytics (Week 1-2)
- [ ] `SponsorMessagingAnalyticsDto.cs` oluştur
- [ ] `GetSponsorMessagingAnalyticsQuery.cs` implement et
- [ ] `SponsorshipController` → endpoint ekle
- [ ] Unit tests yaz
- [ ] Postman collection update

### Phase 2: Impact & Temporal & ROI (Week 3-6)
- [ ] `SponsorImpactAnalyticsDto.cs` oluştur
- [ ] `GetSponsorImpactAnalyticsQuery.cs` implement et
- [ ] `SponsorTemporalAnalyticsDto.cs` oluştur
- [ ] `GetSponsorTemporalAnalyticsQuery.cs` implement et
- [ ] `SponsorROIAnalyticsDto.cs` oluştur
- [ ] `GetSponsorROIAnalyticsQuery.cs` implement et
- [ ] Controller endpoints ekle
- [ ] Unit tests + integration tests
- [ ] Postman collection update

### Database Optimization
- [ ] PlantAnalysis indexes:
  ```sql
  CREATE INDEX IX_PlantAnalysis_SponsorCompanyId_AnalysisDate 
  ON PlantAnalyses (SponsorCompanyId, AnalysisDate);
  
  CREATE INDEX IX_PlantAnalysis_Location 
  ON PlantAnalyses (Location);
  
  CREATE INDEX IX_PlantAnalysis_CropType 
  ON PlantAnalyses (CropType);
  ```

### Configuration
- [ ] `appsettings.json` → Sponsorship section
  ```json
  {
    "Sponsorship": {
      "AnalysisUnitValue": 50.00,
      "CacheSettings": {
        "MessagingAnalyticsTTL": 15,
        "ImpactAnalyticsTTL": 360,
        "TemporalAnalyticsTTL": 60,
        "ROIAnalyticsTTL": 720
      }
    }
  }
  ```

---

## 🎨 Mobile App Dashboard Design

### Widget Layout
```
┌─────────────────────────────────────┐
│ 📊 Overview Card                    │
│ Total Codes: 1,000                  │
│ Analyses: 2,450 ↑ 15%              │
│ Active Farmers: 89                  │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ 🌍 Impact Card                      │
│ Farmers Reached: 500                │
│ Cities: 35 | Districts: 120         │
│ Critical Issues Resolved: 150       │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ 💰 ROI Card                         │
│ Overall ROI: +185%                  │
│ Cost per Analysis: 15 TRY           │
│ Value Generated: 122,500 TRY        │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ 💬 Messaging Card                   │
│ Active Conversations: 23            │
│ Avg Response Time: 4.5 hours        │
│ Unread Messages: 8                  │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ 📈 Trend Chart (Line)               │
│ Last 30 Days Analyses               │
│ [Chart visualization]               │
└─────────────────────────────────────┘
```

### Charts
1. **Line Chart:** Temporal analytics (30-day trend)
2. **Bar Chart:** Top crops distribution
3. **Pie Chart:** Tier breakdown
4. **Gauge Chart:** ROI percentage

---

## 📊 Export Functionality

### CSV Export
```
Endpoint: GET /api/v1/sponsorship/{endpoint}?format=csv
Encoding: UTF-8 with BOM
All 4 new endpoints support CSV export
```

### Excel Export (Optional - Phase 3)
```
Format: Multiple sheets
Libraries: EPPlus or ClosedXML
Sheets:
  - Messaging Analytics
  - Impact Summary
  - Temporal Trends
  - ROI Analysis
```

---

## 🔐 Security & Privacy

### Authorization
```csharp
[Authorize(Roles = "Sponsor,Admin")]
- Sponsor: Sadece kendi SponsorId'si için data
- Admin: Tüm sponsor'lar için data (admin panel)

Validation:
if (User.IsInRole("Sponsor"))
{
    if (sponsorId != GetUserId())
        return Forbidden();
}
```

### Tier-Based Privacy (Impact Analytics)
```csharp
S Tier: FarmerName = "Anonymous"
M Tier: FarmerName = "Anonymous" 
L/XL Tier: FarmerName = Actual name
```

### Rate Limiting
```csharp
All analytics endpoints:
- 50 requests/hour per sponsor
- Cache kullanımı ile optimize edilmiş
```

---

## 🎯 Success Metrics

### Adoption
- Target: 70% sponsors use analytics monthly
- Metric: Unique sponsor count accessing endpoints

### Engagement
- Target: 3+ analytics views per sponsor per week
- Metric: Endpoint usage frequency

### Value
- Target: Sponsors increase ROI by 20% QoQ
- Metric: Average ROI trend improvement

---

## 🚀 Implementation Timeline

### Week 1-2: Messaging Analytics
- [x] Requirement finalized
- [ ] DTO + Query Handler
- [ ] Controller endpoint
- [ ] Unit tests
- [ ] Deploy to staging
- [ ] User testing

### Week 3-4: Impact Analytics
- [ ] DTO + Query Handler
- [ ] Location parsing logic
- [ ] Controller endpoint
- [ ] Unit tests
- [ ] Deploy to staging

### Week 5-6: Temporal + ROI Analytics
- [ ] Both DTO + Query Handlers
- [ ] Controller endpoints
- [ ] Unit tests
- [ ] Deploy to staging
- [ ] Comprehensive testing

### Week 7: Mobile App Integration
- [ ] API integration
- [ ] Dashboard widgets
- [ ] Charts implementation
- [ ] Deploy to production

---

## 📚 Related Files

### Code Files
- `Entities/Dtos/SponsorMessagingAnalyticsDto.cs`
- `Entities/Dtos/SponsorImpactAnalyticsDto.cs`
- `Entities/Dtos/SponsorTemporalAnalyticsDto.cs`
- `Entities/Dtos/SponsorROIAnalyticsDto.cs`
- `Business/Handlers/Sponsorship/Queries/GetSponsorMessagingAnalyticsQuery.cs`
- `Business/Handlers/Sponsorship/Queries/GetSponsorImpactAnalyticsQuery.cs`
- `Business/Handlers/Sponsorship/Queries/GetSponsorTemporalAnalyticsQuery.cs`
- `Business/Handlers/Sponsorship/Queries/GetSponsorROIAnalyticsQuery.cs`
- `WebAPI/Controllers/SponsorshipController.cs`

### Documentation
- Previous analysis: `claudedocs/SPONSOR_STATISTICS_COMPREHENSIVE_ANALYSIS.md`
- Memory: `sponsor_statistics_comprehensive_analysis_2025_01_25`

---

## ✅ Conclusion

**User feedback uygulandı:**
- ❌ Dashboard enhancements, alerts, trend indicators → Çıkarıldı
- ❌ Link statistics performance metrics → Çıkarıldı
- ❌ Channel cost tracking → Çıkarıldı
- ❌ Background jobs (Hangfire) → Çıkarıldı
- ✅ 4 Core endpoint belirlendi: Messaging, Impact, Temporal, ROI

**Next Action:** Phase 1 (Messaging Analytics) ile başla, 2 hafta içinde production'a al.

**Estimated Total Effort:** 6-7 hafta
**Business Value:** ⭐⭐⭐⭐⭐ ÇOK YÜKSEK
**Risk:** DÜŞÜK (mevcut entity'ler kullanılıyor)

---

**Hazırlayan:** Claude (Serena MCP Integration)  
**Tarih:** 2025-01-25  
**Versiyon:** 2.0 (User Feedback Applied)  
**Status:** FINALIZED & READY FOR IMPLEMENTATION ✅
