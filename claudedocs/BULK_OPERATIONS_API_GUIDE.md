# ⚡ Bulk Operations API - Kapsamlı Kullanım Rehberi

## 📊 Genel Bakış

Bulk Operations API'si, ZiraAI sponsorship sisteminde yüksek hacimli işlemleri yönetmek için tasarlanmış gelişmiş bir endpoint grubudur. Sponsorların yüzlerce veya binlerce çiftçiye aynı anda link gönderebilmesi ve kod üretebilmesi için queue-based background processing sistemi kullanır.

**Base URL**: `/api/v1/bulk-operations`  
**Authentication**: Bearer Token (Sponsor, Admin rolleri)  
**Architecture**: Asynchronous processing with real-time status tracking  
**Controller Location**: `WebAPI/Controllers/BulkOperationsController.cs`

---

## 🎯 API Endpoint'leri

### 1. 📤 Toplu Link Gönderimi
**Endpoint**: `POST /send-links`  
**Purpose**: WhatsApp veya SMS ile yüzlerce çiftçiye sponsorship linklerini toplu olarak gönderir.

#### 📥 Request Format
```json
{
  "operationName": "Tarım Kooperatifi 2025 Kampanyası",
  "recipients": [
    {
      "name": "Ahmet Çiftçi",
      "phoneNumber": "+905551234567",
      "email": "ahmet@email.com",
      "sponsorshipCode": "AGRI-2025-001",
      "personalizationData": {
        "region": "Antalya",
        "cropType": "domates"
      },
      "priority": 8,
      "customMessage": "Merhaba {name}, {region} bölgesi için özel sponsorluk!"
    }
  ],
  "messageTemplate": "Merhaba {name}! Tarım sponsorluğu programımıza katılmak için: {link}",
  "channel": "WhatsApp",
  "scheduling": {
    "isScheduled": true,
    "scheduledDate": "2025-08-20T09:00:00Z",
    "timeZone": "Europe/Istanbul",
    "spreadOverTime": true,
    "spreadDurationMinutes": 120,
    "preferredHours": ["09:00", "14:00", "18:00"],
    "avoidWeekdays": ["Saturday", "Sunday"]
  },
  "processing": {
    "batchSize": 25,
    "maxConcurrency": 3,
    "retryAttempts": 2,
    "retryDelaySeconds": 30,
    "stopOnFirstError": false,
    "sendProgressNotifications": true,
    "callbackUrl": "https://myapi.com/webhook/bulk-complete"
  },
  "metadata": {
    "campaignId": "CAMP-2025-Q3",
    "budgetLimit": 500,
    "costCenter": "Marketing"
  }
}
```

#### 📤 Response Format
```json
{
  "success": true,
  "data": {
    "operationId": "bulk_12345678-abcd-efgh",
    "operationType": "BulkLinkSend",
    "status": "Queued",
    "totalItems": 250,
    "createdAt": "2025-08-19T15:30:00Z",
    "estimatedCompletionTime": "2025-08-19T17:00:00Z",
    "statusUrl": "/api/v1/bulk-operations/status/bulk_12345678-abcd-efgh",
    "metrics": {
      "totalItems": 250,
      "processedItems": 0,
      "successfulItems": 0,
      "failedItems": 0,
      "progressPercentage": 0,
      "estimatedTimeRemaining": "01:30:00"
    },
    "validationWarnings": [
      "5 telefon numarası geçersiz format",
      "2 sponsorship kodu bulunamadı"
    ]
  }
}
```

#### 🎯 Kullanım Örnekleri

**Küçük Grup (10-50 mesaj)**:
```bash
curl -X POST "{{baseUrl}}/api/v1/bulk-operations/send-links" \
  -H "Authorization: Bearer {{token}}" \
  -H "Content-Type: application/json" \
  -d '{
    "recipients": [...],
    "channel": "WhatsApp",
    "processing": {
      "batchSize": 10,
      "maxConcurrency": 2
    }
  }'
```

**Büyük Kampanya (500+ mesaj)**:
```bash
curl -X POST "{{baseUrl}}/api/v1/bulk-operations/send-links" \
  -H "Authorization: Bearer {{token}}" \
  -H "Content-Type: application/json" \
  -d '{
    "recipients": [...], 
    "channel": "SMS",
    "scheduling": {
      "spreadOverTime": true,
      "spreadDurationMinutes": 180
    },
    "processing": {
      "batchSize": 50,
      "maxConcurrency": 5,
      "retryAttempts": 3
    }
  }'
```

### 2. 🔧 Toplu Kod Üretimi
**Endpoint**: `POST /generate-codes`  
**Purpose**: Büyük miktarlarda sponsorship kodlarını batch'ler halinde üretir.

#### 📥 Request Format
```json
{
  "operationName": "Q4 2025 Kod Üretimi",
  "subscriptionTierId": 3,
  "quantity": 1000,
  "codePrefix": "L2025",
  "validityDays": 180,
  "processing": {
    "batchSize": 100,
    "maxConcurrency": 3,
    "retryAttempts": 2
  },
  "batches": [
    {
      "batchName": "Antalya Bölgesi",
      "quantity": 300,
      "customPrefix": "L2025ANT",
      "batchMetadata": {
        "region": "Antalya",
        "targetAudience": "Sera çiftçileri"
      }
    },
    {
      "batchName": "İzmir Bölgesi", 
      "quantity": 400,
      "customPrefix": "L2025IZM",
      "batchMetadata": {
        "region": "İzmir",
        "targetAudience": "Zeytin üreticileri"
      }
    }
  ]
}
```

#### 📤 Response Format
```json
{
  "success": true,
  "data": {
    "operationId": "codegen_87654321-wxyz",
    "operationType": "BulkCodeGeneration",
    "status": "Processing",
    "totalItems": 1000,
    "createdAt": "2025-08-19T16:00:00Z",
    "statusUrl": "/api/v1/bulk-operations/status/codegen_87654321-wxyz",
    "metrics": {
      "totalItems": 1000,
      "processedItems": 350,
      "successfulItems": 345,
      "failedItems": 5,
      "progressPercentage": 35.0,
      "estimatedTimeRemaining": "00:45:00"
    }
  }
}
```

### 3. 📊 İşlem Durumu Takibi
**Endpoint**: `GET /status/{operationId}`  
**Purpose**: Bulk işlemin real-time durumunu ve detaylı metrikleri sorgular.

#### 📤 Response Format
```json
{
  "success": true,
  "data": {
    "operationId": "bulk_12345678-abcd-efgh",
    "status": "Processing",
    "metrics": {
      "totalItems": 250,
      "processedItems": 180,
      "successfulItems": 175,
      "failedItems": 5,
      "skippedItems": 0,
      "progressPercentage": 72.0,
      "successRate": 97.2,
      "averageProcessingTime": "00:00:02.5",
      "estimatedTimeRemaining": "00:12:30",
      "errors": [
        {
          "itemIndex": 23,
          "itemId": "recipient_23",
          "errorCode": "INVALID_PHONE",
          "errorMessage": "Telefon numarası formatı geçersiz",
          "errorCategory": "Validation",
          "isRetryable": false,
          "retryCount": 0
        }
      ]
    },
    "lastUpdated": "2025-08-19T16:45:30Z",
    "currentPhase": "Processing",
    "statusMessage": "Mesajlar gönderiliyor... (180/250)",
    "recentResults": [
      {
        "index": 180,
        "itemId": "recipient_180",
        "status": "Success",
        "resultData": "Message sent successfully",
        "processedAt": "2025-08-19T16:45:28Z",
        "processingDuration": "00:00:02.1"
      }
    ]
  }
}
```

#### 💡 Polling Strategy Örneği
```javascript
async function trackBulkOperation(operationId) {
  const pollInterval = 5000; // 5 saniye
  const maxPolls = 120; // 10 dakika timeout
  
  for (let i = 0; i < maxPolls; i++) {
    const response = await fetch(`/api/v1/bulk-operations/status/${operationId}`);
    const status = await response.json();
    
    console.log(`Progress: ${status.data.metrics.progressPercentage}%`);
    
    if (['Completed', 'Failed', 'Cancelled'].includes(status.data.status)) {
      return status;
    }
    
    await new Promise(resolve => setTimeout(resolve, pollInterval));
  }
  
  throw new Error('Polling timeout');
}
```

### 4. 📋 İşlem Geçmişi
**Endpoint**: `GET /history?pageSize=50`  
**Purpose**: Sponsor'ın geçmiş bulk işlemlerinin sayfalanmış listesini getirir.

#### 📤 Response Format
```json
{
  "success": true,
  "data": [
    {
      "operationId": "bulk_12345678-abcd-efgh",
      "operationName": "Tarım Kooperatifi 2025 Kampanyası",
      "operationType": "BulkLinkSend",
      "status": "Completed",
      "totalItems": 250,
      "successfulItems": 245,
      "failedItems": 5,
      "createdAt": "2025-08-19T15:30:00Z",
      "completedAt": "2025-08-19T16:45:00Z",
      "duration": "01:15:00",
      "createdBy": "sponsor_user_123",
      "summary": {
        "channel": "WhatsApp",
        "totalCost": 12.25,
        "averageDeliveryTime": "00:00:08.5"
      }
    }
  ],
  "pagination": {
    "pageSize": 50,
    "hasMore": true,
    "totalCount": 127
  }
}
```

### 5. ❌ İşlem İptali
**Endpoint**: `POST /cancel/{operationId}`  
**Purpose**: Çalışan bulk işlemi iptal eder.

#### 📤 Response Format
```json
{
  "success": true,
  "message": "İşlem başarıyla iptal edildi",
  "data": {
    "operationId": "bulk_12345678-abcd-efgh",
    "finalStatus": "Cancelled",
    "processedItems": 85,
    "remainingItems": 165,
    "cancellationTime": "2025-08-19T16:20:00Z"
  }
}
```

### 6. 🔄 Başarısız Öğeleri Yeniden Dene
**Endpoint**: `POST /retry/{operationId}`  
**Purpose**: Başarısız olan mesajları tekrar göndermeyi dener.

#### 📤 Response Format
```json
{
  "success": true,
  "message": "5 başarısız öğe yeniden işleme alındı",
  "data": {
    "originalOperationId": "bulk_12345678-abcd-efgh", 
    "retryOperationId": "retry_12345678-abcd-efgh",
    "failedItemsCount": 5,
    "retryableItemsCount": 3,
    "nonRetryableItemsCount": 2
  }
}
```

### 7. 📋 İşlem Şablonları
**Endpoint**: `GET /templates`  
**Purpose**: Yaygın kullanım senaryoları için hazır şablonlar sunar.

#### 📤 Response Format
```json
{
  "success": true,
  "data": {
    "link_send_templates": [
      {
        "name": "Sponsorship Invitation",
        "channel": "WhatsApp",
        "message": "Merhaba {name}! Tarım sponsorluğu programımıza katılmak için bu linki kullanın: {link}",
        "processing_options": {
          "batch_size": 25,
          "max_concurrency": 3,
          "retry_attempts": 2
        }
      },
      {
        "name": "SMS Campaign", 
        "channel": "SMS",
        "message": "Değerli çiftçimiz {name}, ücretsiz tarım analizi için: {link}. Kod: {code}",
        "processing_options": {
          "batch_size": 50,
          "max_concurrency": 5,
          "retry_attempts": 3
        }
      }
    ],
    "code_generation_templates": [
      {
        "name": "Small Batch (S Tier)",
        "quantity": 100,
        "subscription_tier_id": 1,
        "code_prefix": "SMLS",
        "validity_days": 90
      },
      {
        "name": "Large Batch (L Tier)",
        "quantity": 500,
        "subscription_tier_id": 3,
        "code_prefix": "LRGL", 
        "validity_days": 180
      },
      {
        "name": "Enterprise (XL Tier)",
        "quantity": 1000,
        "subscription_tier_id": 4,
        "code_prefix": "XLEN",
        "validity_days": 365
      }
    ]
  }
}
```

### 8. 📈 Dashboard İstatistikleri
**Endpoint**: `GET /statistics`  
**Purpose**: Sponsor'ın bulk işlem performansı için dashboard metrikleri.

#### 📤 Response Format (Mock Data)
```json
{
  "success": true,
  "data": {
    "total_operations": 24,
    "successful_operations": 22,
    "failed_operations": 1,
    "cancelled_operations": 1,
    "total_items_processed": 3247,
    "total_successful_items": 3089,
    "overall_success_rate": 95.1,
    "average_processing_time_minutes": 8.5,
    "last_30_days": {
      "operations_count": 15,
      "links_sent": 2156,
      "codes_generated": 890,
      "success_rate": 96.2
    },
    "channel_breakdown": {
      "whatsapp": { "count": 1845, "success_rate": 97.1 },
      "sms": { "count": 1402, "success_rate": 93.8 }
    },
    "popular_times": [
      { "hour": 9, "operations": 8, "success_rate": 98.2 },
      { "hour": 14, "operations": 6, "success_rate": 96.8 },
      { "hour": 16, "operations": 5, "success_rate": 94.5 }
    ]
  }
}
```

---

## 🏗️ Teknik Implementasyon Durumu

### ✅ Tamamlanmış Bileşenler

#### 1. Controller Layer
**Dosya**: `WebAPI/Controllers/BulkOperationsController.cs`
- 8 endpoint tanımı mevcut
- Role-based authorization (Sponsor, Admin)
- Error handling framework
- Input validation

#### 2. Service Interface
**Dosya**: `Business/Services/Queue/IBulkOperationService.cs`
- Kapsamlı interface tanımı
- 18 farklı DTO sınıfı
- Advanced configuration options
- Progress tracking structures

#### 3. DTO Architecture
**Ana Sınıflar**:
- `BulkLinkSendRequest` - Link gönderim isteği
- `BulkCodeGenerationRequest` - Kod üretim isteği  
- `BulkOperationResponse` - İşlem başlatma yanıtı
- `BulkProcessingMetrics` - Detaylı metrikleri
- `BulkOperationSummary` - Geçmiş özeti

**Configuration Classes**:
- `BulkSchedulingOptions` - Zamanlama seçenekleri
- `BulkProcessingOptions` - İşlem ayarları
- `BulkError` - Hata yönetimi

**Status Enums**:
```csharp
public enum BulkOperationStatus
{
    Queued,
    Validating,
    Processing,
    Paused,
    Completed,
    Failed,
    Cancelled,
    PartiallyCompleted
}

public enum BulkItemStatus
{
    Pending,
    Processing,
    Success,
    Failed,
    Skipped,
    Retrying
}
```

### 🚫 Eksik Implementasyonlar

#### 1. **KRİTİK**: Service Implementation Eksik
```csharp
// ARANMAKTA: Business/Services/Queue/BulkOperationService.cs
public class BulkOperationService : IBulkOperationService
{
    // 6 method implementation gerekli:
    // - ProcessBulkLinkSendAsync
    // - ProcessBulkCodeGenerationAsync
    // - GetBulkOperationStatusAsync
    // - GetBulkOperationHistoryAsync
    // - CancelBulkOperationAsync
    // - RetryFailedBulkItemsAsync
}
```

#### 2. **KRİTİK**: Database Schema Eksik
```sql
-- Gerekli tablolar
CREATE TABLE BulkOperations (
    Id SERIAL PRIMARY KEY,
    OperationId VARCHAR(50) UNIQUE,
    OperationType VARCHAR(50), -- LinkSend, CodeGeneration
    SponsorId INT REFERENCES Users(UserId),
    OperationName VARCHAR(200),
    Status VARCHAR(20), -- Queued, Processing, Completed, Failed
    TotalItems INT,
    ProcessedItems INT DEFAULT 0,
    SuccessfulItems INT DEFAULT 0,
    FailedItems INT DEFAULT 0,
    SkippedItems INT DEFAULT 0,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    StartedAt TIMESTAMP NULL,
    CompletedAt TIMESTAMP NULL,
    EstimatedCompletionTime TIMESTAMP NULL,
    ConfigurationJson TEXT, -- Request parameters as JSON
    ResultsJson TEXT, -- Final results as JSON
    MetadataJson TEXT -- Custom metadata as JSON
);

CREATE TABLE BulkOperationItems (
    Id SERIAL PRIMARY KEY,
    BulkOperationId INT REFERENCES BulkOperations(Id),
    ItemIndex INT,
    ItemId VARCHAR(100),
    Status VARCHAR(20), -- Pending, Processing, Success, Failed, Skipped
    InputData TEXT, -- Item-specific data as JSON  
    ResultData TEXT, -- Processing result as JSON
    ErrorCode VARCHAR(50) NULL,
    ErrorMessage TEXT NULL,
    ErrorCategory VARCHAR(50) NULL,
    IsRetryable BOOLEAN DEFAULT TRUE,
    RetryCount INT DEFAULT 0,
    ProcessedAt TIMESTAMP NULL,
    ProcessingDurationMs INT NULL
);

-- Indexes
CREATE INDEX IX_BulkOperations_SponsorId ON BulkOperations(SponsorId);
CREATE INDEX IX_BulkOperations_Status ON BulkOperations(Status);
CREATE INDEX IX_BulkOperations_CreatedAt ON BulkOperations(CreatedAt);
CREATE INDEX IX_BulkOperationItems_BulkOperationId ON BulkOperationItems(BulkOperationId);
CREATE INDEX IX_BulkOperationItems_Status ON BulkOperationItems(Status);
```

#### 3. **ORTA**: Queue Infrastructure Eksik
- RabbitMQ/Hangfire queue setup
- Background job processing
- Progress notification system
- Retry mechanism implementation

#### 4. **DÜŞÜK**: Monitoring & Analytics
- Real-time dashboard implementation
- Performance metrics collection
- Cost tracking system
- Alert mechanisms

---

## 💼 Kullanım Senaryoları

### Senaryo 1: Küçük Kooperatif (50 çiftçi)
```bash
# 1. Kod üretimi
POST /api/v1/bulk-operations/generate-codes
{
  "quantity": 50,
  "subscriptionTierId": 2,
  "processing": { "batchSize": 10 }
}

# 2. Link gönderimi  
POST /api/v1/bulk-operations/send-links
{
  "recipients": [...50 recipients],
  "channel": "WhatsApp"
}

# 3. Durum takibi
GET /api/v1/bulk-operations/status/{operationId}
```

### Senaryo 2: Büyük Kampanya (1000+ çiftçi)
```bash
# 1. Scheduled processing
POST /api/v1/bulk-operations/send-links
{
  "recipients": [...1000+ recipients],
  "scheduling": {
    "isScheduled": true,
    "scheduledDate": "2025-08-21T09:00:00Z",
    "spreadOverTime": true,
    "spreadDurationMinutes": 360
  },
  "processing": {
    "batchSize": 50,
    "maxConcurrency": 5
  }
}

# 2. Real-time monitoring
while (status !== 'Completed') {
  GET /api/v1/bulk-operations/status/{operationId}
  sleep(10 seconds)
}

# 3. Retry failed items
POST /api/v1/bulk-operations/retry/{operationId}
```

### Senaryo 3: Enterprise Management
```bash
# 1. Dashboard overview
GET /api/v1/bulk-operations/statistics

# 2. Historical analysis
GET /api/v1/bulk-operations/history?pageSize=100

# 3. Template usage
GET /api/v1/bulk-operations/templates
```

---

## 🔒 Security & Performance

### Authentication & Authorization
- **JWT Bearer Token**: Tüm endpoint'ler için zorunlu
- **Role-Based Access**: Sadece Sponsor ve Admin rolleri
- **Sponsor Isolation**: Her sponsor sadece kendi işlemlerini görebilir

### Rate Limiting & Quotas
```json
{
  "bulkOperationLimits": {
    "maxConcurrentOperations": 3,
    "maxItemsPerOperation": 5000,
    "maxOperationsPerDay": 10,
    "maxItemsPerDay": 50000
  }
}
```

### Performance Considerations
- **Batch Processing**: Configurable batch sizes (10-100)
- **Concurrency Control**: Max 5 parallel jobs per sponsor
- **Database Optimization**: Indexed queries, connection pooling
- **Memory Management**: Stream processing for large datasets

---

## 🚨 Error Handling & Troubleshooting

### Common Error Scenarios

#### 1. Validation Errors
```json
{
  "success": false,
  "error": "VALIDATION_FAILED",
  "details": {
    "invalidPhoneNumbers": 5,
    "missingCodes": 2,
    "duplicateRecipients": 1
  }
}
```

#### 2. Processing Errors
```json
{
  "success": false,
  "error": "PROCESSING_FAILED", 
  "message": "WhatsApp rate limit exceeded",
  "retryAfter": "00:15:00"
}
```

#### 3. System Errors
```json
{
  "success": false,
  "error": "SYSTEM_ERROR",
  "message": "Queue service unavailable",
  "operationId": "bulk_12345",
  "supportTicket": "TKT-789123"
}
```

### Error Categories
- **Validation**: Invalid input data
- **Processing**: External service failures
- **Network**: Connectivity issues
- **System**: Internal server errors
- **Business**: Business rule violations

---

## 📊 Data Transfer Objects (DTOs)

### Request DTOs

#### BulkLinkSendRequest
```csharp
public class BulkLinkSendRequest
{
    public string OperationName { get; set; } = "Bulk Link Send";
    public int SponsorId { get; set; }
    public List<BulkLinkRecipient> Recipients { get; set; } = new();
    public string MessageTemplate { get; set; }
    public string Channel { get; set; } = "SMS"; // SMS, WhatsApp, Email
    public BulkSchedulingOptions Scheduling { get; set; } = new();
    public BulkProcessingOptions Processing { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

#### BulkCodeGenerationRequest
```csharp
public class BulkCodeGenerationRequest
{
    public string OperationName { get; set; } = "Bulk Code Generation";
    public int SponsorId { get; set; }
    public int SubscriptionTierId { get; set; }
    public int Quantity { get; set; }
    public string CodePrefix { get; set; } = "BULK";
    public int ValidityDays { get; set; } = 365;
    public BulkProcessingOptions Processing { get; set; } = new();
    public List<CodeGenerationBatch> Batches { get; set; } = new();
}
```

### Response DTOs

#### BulkOperationResponse
```csharp
public class BulkOperationResponse
{
    public string OperationId { get; set; }
    public string OperationType { get; set; }
    public BulkOperationStatus Status { get; set; } = BulkOperationStatus.Queued;
    public int TotalItems { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? StartedAt { get; set; }
    public DateTime? EstimatedCompletionTime { get; set; }
    public string StatusUrl { get; set; }
    public BulkProcessingMetrics Metrics { get; set; } = new();
    public List<string> ValidationWarnings { get; set; } = new();
}
```

#### BulkProcessingMetrics
```csharp
public class BulkProcessingMetrics
{
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int SuccessfulItems { get; set; }
    public int FailedItems { get; set; }
    public int SkippedItems { get; set; }
    public double ProgressPercentage => TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100 : 0;
    public double SuccessRate => ProcessedItems > 0 ? (double)SuccessfulItems / ProcessedItems * 100 : 0;
    public TimeSpan? AverageProcessingTime { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public List<BulkError> Errors { get; set; } = new();
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}
```

---

## 🔧 Configuration Options

### Scheduling Options
```csharp
public class BulkSchedulingOptions
{
    public bool IsScheduled { get; set; } = false;
    public DateTime? ScheduledDate { get; set; }
    public string TimeZone { get; set; } = "Europe/Istanbul";
    public bool SpreadOverTime { get; set; } = false;
    public int SpreadDurationMinutes { get; set; } = 60;
    public List<string> PreferredHours { get; set; } = new(); // "09:00", "14:00"
    public List<string> AvoidWeekdays { get; set; } = new(); // "Saturday", "Sunday"
}
```

### Processing Options
```csharp
public class BulkProcessingOptions
{
    public int BatchSize { get; set; } = 50; // Process items in batches
    public int MaxConcurrency { get; set; } = 5; // Max parallel processing
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 30;
    public bool StopOnFirstError { get; set; } = false;
    public bool SendProgressNotifications { get; set; } = true;
    public string CallbackUrl { get; set; }
    public Dictionary<string, object> ProcessingSettings { get; set; } = new();
}
```

---

## 📋 Production Readiness Status

### ✅ Complete (Ready for Production)
- Controller endpoints and routing
- DTO structures and validation
- Interface definitions
- Authentication and authorization
- Error handling framework
- API documentation

### 🔄 In Progress (Needs Implementation)
- Service layer concrete implementation
- Database schema and repositories
- Queue processing infrastructure
- Progress tracking mechanism

### ❌ Missing (Critical for Production)
- Background job processing (Hangfire/RabbitMQ)
- Real-time status updates
- Performance monitoring
- Cost tracking system
- Webhook notification system

### 🎯 Implementation Priority
1. **Critical (Week 1)**: Service implementation, database schema
2. **Important (Week 2)**: Queue processing, progress tracking
3. **Nice-to-Have (Week 3)**: Advanced features, monitoring

---

## 📖 Usage Best Practices

### Optimal Batch Sizes
- **WhatsApp**: 25-50 items per batch (rate limit compliance)
- **SMS**: 50-100 items per batch (higher throughput)
- **Email**: 100+ items per batch (no strict rate limits)

### Concurrency Guidelines
- **Small Operations** (<100 items): 1-2 concurrent workers
- **Medium Operations** (100-500 items): 3-5 concurrent workers
- **Large Operations** (500+ items): 5-10 concurrent workers

### Error Handling Strategy
1. **Validation Errors**: Fix data and resubmit
2. **Rate Limit Errors**: Reduce batch size and retry
3. **Network Errors**: Enable auto-retry with exponential backoff
4. **System Errors**: Contact support with operation ID

### Monitoring Recommendations
- Monitor operation progress every 10-30 seconds
- Set alerts for operations stuck in "Processing" status >1 hour
- Track success rates and identify patterns in failures
- Monitor cost per successful delivery for budget optimization

---

**Dokümantasyon Versiyonu**: 1.0  
**Son Güncelleme**: Ağustos 2025  
**Sorumlu**: ZiraAI Development Team  
**Status**: Interface Ready, Implementation Pending

---

*Bu dokümantasyon Bulk Operations API'sinin mevcut durumunu ve gelecek implementasyon planını detaylandırmaktadır. Production kullanımı için service layer implementasyonu gereklidir.*