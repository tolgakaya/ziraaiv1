# Toplu Dealer Davet Sistemi - RabbitMQ + WorkerService Tasarımı

**Doküman Versiyonu:** 2.0 (RabbitMQ)
**Tarih:** 2025-11-03
**Tasarımcı:** Claude Code
**Amaç:** Excel dosyası ile toplu dealer davet - PlantAnalysis Async pattern'i ile

---

## 📋 İçindekiler

1. [Mevcut PlantAnalysis Async Pattern](#1-mevcut-plantanalysis-async-pattern)
2. [RabbitMQ Yaklaşımı Tasarımı](#2-rabbitmq-yaklaşımı-tasarımı)
3. [Message Queue Yapısı](#3-message-queue-yapısı)
4. [WorkerService Consumer](#4-workerservice-consumer)
5. [API Endpoint Implementasyonu](#5-api-endpoint-implementasyonu)
6. [SignalR Real-time Notifications](#6-signalr-real-time-notifications)
7. [Hata Yönetimi ve Retry Logic](#7-hata-yönetimi-ve-retry-logic)
8. [İmplementasyon Planı](#8-i̇mplementasyon-planı)

---

## 1. Mevcut PlantAnalysis Async Pattern

### 1.1 Akış Şeması

```
┌────────────────────────────────────────────────────┐
│  API Endpoint: POST /api/plant-analysis/async     │
└───────────────────┬────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────────────────┐
│  PlantAnalysisAsyncService                         │
│  1. Generate AnalysisId & CorrelationId            │
│  2. Process & Upload Image                         │
│  3. Create PlantAnalysis entity (Status=Processing)│
│  4. Save to Database                               │
│  5. Create RabbitMQ message                        │
│  6. Publish to Queue: plant-analysis-requests      │
└───────────────────┬────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────────────────┐
│  RabbitMQ Queue: plant-analysis-requests           │
│  Message: PlantAnalysisAsyncRequestDto             │
└───────────────────┬────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────────────────┐
│  External N8N Service                              │
│  - Process AI analysis                             │
│  - Publish result to: plant-analysis-results       │
└───────────────────┬────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────────────────┐
│  RabbitMQ Queue: plant-analysis-results            │
│  Message: PlantAnalysisAsyncResponseDto            │
└───────────────────┬────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────────────────┐
│  PlantAnalysisWorkerService                        │
│  RabbitMQConsumerWorker (BackgroundService)        │
│  1. Consume message from results queue             │
│  2. Enqueue Hangfire job                           │
│  3. Ack message                                    │
└───────────────────┬────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────────────────┐
│  Hangfire Job: ProcessPlantAnalysisResultAsync     │
│  1. Update PlantAnalysis entity                    │
│  2. Save to database                               │
│  3. Send SignalR notification to user              │
└────────────────────────────────────────────────────┘
```

### 1.2 Anahtar Bileşenler

**1. API Service (PlantAnalysisAsyncService):**
- Image processing & upload
- Database entity creation (Status=Processing)
- RabbitMQ message publishing

**2. RabbitMQ Configuration (RabbitMQOptions):**
```csharp
public class QueueOptions
{
    public string PlantAnalysisRequest { get; set; } = "plant-analysis-requests";
    public string PlantAnalysisResult { get; set; } = "plant-analysis-results";
    public string Notification { get; set; } = "notifications";
}
```

**3. WorkerService (RabbitMQConsumerWorker):**
- BackgroundService implementation
- Consumes from results queue
- Enqueues Hangfire job for processing
- Auto-reconnection & retry logic

**4. Hangfire Job (PlantAnalysisJobService):**
- Actual processing logic
- Database updates
- SignalR notifications

---

## 2. RabbitMQ Yaklaşımı Tasarımı

### 2.1 Bulk Dealer Invitation Akışı

```
┌────────────────────────────────────────────────────┐
│  API Endpoint: POST /dealer/invite-bulk            │
│  - Upload Excel file (multipart/form-data)         │
└───────────────────┬────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────────────────┐
│  BulkDealerInvitationService                       │
│  1. Validate file (size, type, format)             │
│  2. Parse Excel → List<DealerInvitationRow>        │
│  3. Validate rows (email, phone, business rules)   │
│  4. Create BulkInvitationJob entity                │
│     - Status: "Pending"                            │
│     - TotalDealers, ProcessedDealers: 0            │
│  5. Save to database                               │
│  6. Create RabbitMQ messages (1 per dealer)        │
│  7. Publish to Queue: dealer-invitation-requests   │
│  8. Return JobId to client                         │
└───────────────────┬────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────────────────┐
│  RabbitMQ Queue: dealer-invitation-requests        │
│  Message: DealerInvitationQueueMessage             │
│  - One message per dealer                          │
│  - CorrelationId = JobId                           │
└───────────────────┬────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────────────────┐
│  DealerInvitationWorkerService                     │
│  DealerInvitationConsumerWorker (BackgroundService)│
│  1. Consume message from queue                     │
│  2. Enqueue Hangfire job                           │
│  3. Ack message                                    │
└───────────────────┬────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────────────────┐
│  Hangfire Job: ProcessDealerInvitationAsync        │
│  1. Create DealerInvitation entity                 │
│  2. Reserve/Transfer codes                         │
│  3. Send SMS (if enabled)                          │
│  4. Update BulkInvitationJob progress              │
│  5. Send SignalR notification (per dealer)         │
│  6. Send SignalR progress update (to sponsor)      │
└────────────────────────────────────────────────────┘
```

### 2.2 Neden Bu Yaklaşım?

**✅ Avantajlar:**
1. **Mevcut Altyapı Kullanımı**: PlantAnalysisWorkerService zaten var
2. **Scalability**: Her dealer için ayrı mesaj = paralel işleme
3. **Fault Tolerance**: Bir dealer başarısız olursa diğerleri etkilenmez
4. **Retry Logic**: RabbitMQ built-in retry + Hangfire retry
5. **Real-time Progress**: SignalR ile anlık ilerleme bildirimi
6. **No New Dependencies**: Hangfire'ı kaldırmıyoruz, daha efektif kullanıyoruz

**❌ Hangfire-Only Yaklaşımına Göre Farklar:**
- RabbitMQ queue buffer görevi görüyor (rate limiting, backpressure)
- WorkerService bağımsız process (API'dan ayrı restart edilebilir)
- Message durability (RabbitMQ durable queue)

---

## 3. Message Queue Yapısı

### 3.1 Yeni Queue Tanımları

**appsettings.json:**
```json
{
  "RabbitMQ": {
    "ConnectionString": "amqp://dev:devpass@localhost:5672/",
    "Queues": {
      "PlantAnalysisRequest": "plant-analysis-requests",
      "PlantAnalysisResult": "plant-analysis-results",
      "DealerInvitationRequest": "dealer-invitation-requests",
      "Notification": "notifications"
    },
    "RetrySettings": {
      "MaxRetryAttempts": 3,
      "RetryDelayMilliseconds": 1000
    }
  }
}
```

**RabbitMQOptions.cs Update:**
```csharp
public class QueueOptions
{
    public string PlantAnalysisRequest { get; set; } = "plant-analysis-requests";
    public string PlantAnalysisResult { get; set; } = "plant-analysis-results";
    public string DealerInvitationRequest { get; set; } = "dealer-invitation-requests";
    public string Notification { get; set; } = "notifications";
}
```

### 3.2 Message DTO: DealerInvitationQueueMessage

**Entities/Dtos/DealerInvitationQueueMessage.cs:**
```csharp
namespace Entities.Dtos
{
    /// <summary>
    /// RabbitMQ message for dealer invitation processing
    /// One message per dealer invitation
    /// </summary>
    public class DealerInvitationQueueMessage
    {
        // Tracking
        public string CorrelationId { get; set; }  // BulkInvitationJob.Id
        public int RowNumber { get; set; }          // Excel row number for error reporting

        // Bulk Job Reference
        public int BulkJobId { get; set; }
        public int SponsorId { get; set; }

        // Dealer Information
        public string Email { get; set; }
        public string Phone { get; set; }
        public string DealerName { get; set; }

        // Invitation Configuration
        public string InvitationType { get; set; }  // "Invite" or "AutoCreate"
        public string PackageTier { get; set; }     // S, M, L, XL (nullable)
        public int CodeCount { get; set; }

        // Settings
        public bool SendSms { get; set; }

        // Timestamp
        public DateTime QueuedAt { get; set; }
    }
}
```

### 3.3 Progress Update DTO

**Entities/Dtos/BulkInvitationProgressDto.cs:**
```csharp
namespace Entities.Dtos
{
    /// <summary>
    /// SignalR notification for bulk invitation progress
    /// </summary>
    public class BulkInvitationProgressDto
    {
        public int BulkJobId { get; set; }
        public int SponsorId { get; set; }
        public string Status { get; set; }  // "Processing", "Completed", "PartialSuccess"

        public int TotalDealers { get; set; }
        public int ProcessedDealers { get; set; }
        public int SuccessfulInvitations { get; set; }
        public int FailedInvitations { get; set; }
        public decimal ProgressPercentage { get; set; }

        // Latest processed dealer info
        public string LatestDealerEmail { get; set; }
        public bool LatestDealerSuccess { get; set; }
        public string LatestDealerError { get; set; }

        public DateTime LastUpdateTime { get; set; }
    }
}
```

---

## 4. WorkerService Consumer

### 4.1 Yeni Consumer: DealerInvitationConsumerWorker

**PlantAnalysisWorkerService/Services/DealerInvitationConsumerWorker.cs:**

```csharp
using Business.Services.MessageQueue;
using Core.Configuration;
using Entities.Dtos;
using Hangfire;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PlantAnalysisWorkerService.Jobs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text;

namespace PlantAnalysisWorkerService.Services
{
    /// <summary>
    /// Background service for consuming dealer invitation requests from RabbitMQ
    /// Pattern: Same as RabbitMQConsumerWorker for plant analysis
    /// </summary>
    public class DealerInvitationConsumerWorker : BackgroundService
    {
        private readonly ILogger<DealerInvitationConsumerWorker> _logger;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly IServiceProvider _serviceProvider;
        private IConnection? _connection;
        private IChannel? _channel;

        public DealerInvitationConsumerWorker(
            ILogger<DealerInvitationConsumerWorker> logger,
            IOptions<RabbitMQOptions> rabbitMQOptions,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _rabbitMQOptions = rabbitMQOptions.Value;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var startupStopwatch = Stopwatch.StartNew();
            _logger.LogInformation(
                "[DEALER_INVITATION_WORKER_START] Dealer Invitation Consumer Worker starting - QueueName: {QueueName}",
                _rabbitMQOptions.Queues.DealerInvitationRequest);

            try
            {
                await InitializeRabbitMQAsync();
                startupStopwatch.Stop();

                _logger.LogInformation(
                    "[DEALER_INVITATION_WORKER_INITIALIZED] Worker initialized - InitTime: {InitTime}ms",
                    startupStopwatch.ElapsedMilliseconds);

                await StartConsumingAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[DEALER_INVITATION_WORKER_CANCELLED] Worker cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DEALER_INVITATION_WORKER_FATAL_ERROR] Fatal error");
                throw;
            }
            finally
            {
                await CleanupAsync();
            }
        }

        private async Task InitializeRabbitMQAsync()
        {
            _logger.LogInformation(
                "[DEALER_INVITATION_INIT_START] Initializing RabbitMQ connection");

            try
            {
                var factory = new ConnectionFactory();
                factory.Uri = new Uri(_rabbitMQOptions.ConnectionString);
                factory.AutomaticRecoveryEnabled = true;
                factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(
                    _rabbitMQOptions.ConnectionSettings.NetworkRecoveryInterval);
                factory.RequestedHeartbeat = TimeSpan.FromSeconds(
                    _rabbitMQOptions.ConnectionSettings.RequestedHeartbeat);

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                // Declare queue (durable, not auto-delete)
                await _channel.QueueDeclareAsync(
                    queue: _rabbitMQOptions.Queues.DealerInvitationRequest,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _logger.LogInformation(
                    "[DEALER_INVITATION_INIT_SUCCESS] RabbitMQ initialized - Queue: {QueueName}",
                    _rabbitMQOptions.Queues.DealerInvitationRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DEALER_INVITATION_INIT_ERROR] Failed to initialize RabbitMQ");
                throw;
            }
        }

        private async Task StartConsumingAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var messageStopwatch = Stopwatch.StartNew();
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var correlationId = ea.BasicProperties?.CorrelationId ?? Guid.NewGuid().ToString("N")[..8];
                var deliveryTag = ea.DeliveryTag;

                _logger.LogInformation(
                    "[DEALER_INVITATION_MESSAGE_RECEIVED] Message received - Size: {Size}B, CorrelationId: {CorrelationId}",
                    body.Length, correlationId);

                try
                {
                    // Deserialize message
                    var invitationMessage = JsonConvert.DeserializeObject<DealerInvitationQueueMessage>(message);

                    if (invitationMessage == null)
                    {
                        _logger.LogWarning(
                            "[DEALER_INVITATION_DESERIALIZATION_FAILED] Null message - CorrelationId: {CorrelationId}",
                            correlationId);

                        await _channel.BasicNackAsync(deliveryTag, false, false);
                        return;
                    }

                    _logger.LogInformation(
                        "[DEALER_INVITATION_DESERIALIZATION_SUCCESS] Message parsed - BulkJobId: {BulkJobId}, Email: {Email}, Row: {RowNumber}",
                        invitationMessage.BulkJobId, invitationMessage.Email, invitationMessage.RowNumber);

                    // Enqueue Hangfire job for processing
                    var jobId = BackgroundJob.Enqueue<IDealerInvitationJobService>(
                        service => service.ProcessDealerInvitationAsync(invitationMessage, correlationId));

                    _logger.LogInformation(
                        "[DEALER_INVITATION_JOB_ENQUEUED] Hangfire job enqueued - JobId: {JobId}, BulkJobId: {BulkJobId}",
                        jobId, invitationMessage.BulkJobId);

                    // Acknowledge message
                    await _channel.BasicAckAsync(deliveryTag, false);

                    messageStopwatch.Stop();
                    _logger.LogInformation(
                        "[DEALER_INVITATION_MESSAGE_PROCESSED] Message processed - TotalTime: {TotalTime}ms",
                        messageStopwatch.ElapsedMilliseconds);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex,
                        "[DEALER_INVITATION_JSON_ERROR] JSON deserialization error - CorrelationId: {CorrelationId}",
                        correlationId);

                    await _channel.BasicNackAsync(deliveryTag, false, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[DEALER_INVITATION_PROCESSING_ERROR] Error processing message - CorrelationId: {CorrelationId}",
                        correlationId);

                    // Retry logic
                    var shouldRequeue = ShouldRetryMessage(ex);
                    await _channel.BasicNackAsync(deliveryTag, false, shouldRequeue);
                }
            };

            // Set prefetch count for controlled parallelism
            await _channel.BasicQosAsync(0, 5, false);  // Process 5 messages at a time

            await _channel.BasicConsumeAsync(
                queue: _rabbitMQOptions.Queues.DealerInvitationRequest,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("[DEALER_INVITATION_CONSUMER_STARTED] Started consuming messages");

            // Keep alive
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private bool ShouldRetryMessage(Exception ex)
        {
            // Don't retry JSON errors (malformed message)
            if (ex is JsonException) return false;

            // Retry for transient errors (database, network, etc.)
            return true;
        }

        private async Task CleanupAsync()
        {
            try
            {
                if (_channel?.IsOpen == true)
                {
                    await _channel.CloseAsync();
                    _channel?.Dispose();
                }

                if (_connection?.IsOpen == true)
                {
                    await _connection.CloseAsync();
                    _connection?.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during cleanup");
            }
        }

        public override void Dispose()
        {
            CleanupAsync().GetAwaiter().GetResult();
            base.Dispose();
        }
    }
}
```

### 4.2 Program.cs Update

**PlantAnalysisWorkerService/Program.cs:**
```csharp
// Add new worker service
builder.Services.AddHostedService<DealerInvitationConsumerWorker>();

// Register new job service
builder.Services.AddScoped<IDealerInvitationJobService, DealerInvitationJobService>();
```

---

## 5. API Endpoint Implementasyonu

### 5.1 Service: BulkDealerInvitationService

**Business/Services/Sponsorship/BulkDealerInvitationService.cs:**

```csharp
using Business.Services.MessageQueue;
using Core.Configuration;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public interface IBulkDealerInvitationService
    {
        Task<IDataResult<BulkInvitationJobDto>> QueueBulkInvitationsAsync(
            IFormFile excelFile,
            int sponsorId,
            string invitationType,
            string defaultTier,
            int defaultCodeCount,
            bool sendSms,
            bool useRowSpecificCounts);
    }

    public class BulkDealerInvitationService : IBulkDealerInvitationService
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly IBulkInvitationJobRepository _bulkJobRepository;
        private readonly ISponsorshipCodeRepository _codeRepository;
        private readonly IUserRepository _userRepository;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly ILogger<BulkDealerInvitationService> _logger;

        private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        private const int MaxRowCount = 2000;

        public BulkDealerInvitationService(
            IMessageQueueService messageQueueService,
            IBulkInvitationJobRepository bulkJobRepository,
            ISponsorshipCodeRepository codeRepository,
            IUserRepository userRepository,
            IOptions<RabbitMQOptions> rabbitMQOptions,
            ILogger<BulkDealerInvitationService> logger)
        {
            _messageQueueService = messageQueueService;
            _bulkJobRepository = bulkJobRepository;
            _codeRepository = codeRepository;
            _userRepository = userRepository;
            _rabbitMQOptions = rabbitMQOptions.Value;
            _logger = logger;
        }

        public async Task<IDataResult<BulkInvitationJobDto>> QueueBulkInvitationsAsync(
            IFormFile excelFile,
            int sponsorId,
            string invitationType,
            string defaultTier,
            int defaultCodeCount,
            bool sendSms,
            bool useRowSpecificCounts)
        {
            try
            {
                _logger.LogInformation(
                    "📤 Starting bulk invitation - SponsorId: {SponsorId}, Type: {Type}, Tier: {Tier}, CodeCount: {CodeCount}",
                    sponsorId, invitationType, defaultTier ?? "Any", defaultCodeCount);

                // 1. Validate file
                var fileValidation = ValidateFile(excelFile);
                if (!fileValidation.Success)
                {
                    return new ErrorDataResult<BulkInvitationJobDto>(fileValidation.Message);
                }

                // 2. Parse Excel
                var rows = await ParseExcelAsync(excelFile, useRowSpecificCounts, defaultCodeCount, defaultTier);

                if (rows.Count == 0)
                {
                    return new ErrorDataResult<BulkInvitationJobDto>("Excel dosyasında geçerli satır bulunamadı.");
                }

                if (rows.Count > MaxRowCount)
                {
                    return new ErrorDataResult<BulkInvitationJobDto>(
                        $"Maksimum {MaxRowCount} dealer kaydı yüklenebilir. Dosyanızda {rows.Count} kayıt var.");
                }

                // 3. Validate rows
                var validationResult = await ValidateRowsAsync(rows, sponsorId);
                if (!validationResult.Success)
                {
                    return new ErrorDataResult<BulkInvitationJobDto>(validationResult.Message);
                }

                // 4. Check code availability
                var codeCheckResult = await CheckCodeAvailabilityAsync(rows, sponsorId);
                if (!codeCheckResult.Success)
                {
                    return new ErrorDataResult<BulkInvitationJobDto>(codeCheckResult.Message);
                }

                // 5. Create BulkInvitationJob entity
                var bulkJob = new BulkInvitationJob
                {
                    SponsorId = sponsorId,
                    InvitationType = invitationType,
                    DefaultTier = defaultTier,
                    DefaultCodeCount = defaultCodeCount,
                    SendSms = sendSms,
                    TotalDealers = rows.Count,
                    ProcessedDealers = 0,
                    SuccessfulInvitations = 0,
                    FailedInvitations = 0,
                    Status = "Pending",
                    CreatedDate = DateTime.Now,
                    OriginalFileName = excelFile.FileName,
                    FileSize = (int)excelFile.Length
                };

                _bulkJobRepository.Add(bulkJob);
                await _bulkJobRepository.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ BulkInvitationJob created - JobId: {JobId}, TotalDealers: {TotalDealers}",
                    bulkJob.Id, bulkJob.TotalDealers);

                // 6. Publish messages to RabbitMQ (one per dealer)
                var queueName = _rabbitMQOptions.Queues.DealerInvitationRequest;
                var publishedCount = 0;

                foreach (var row in rows)
                {
                    var queueMessage = new DealerInvitationQueueMessage
                    {
                        CorrelationId = bulkJob.Id.ToString(),
                        RowNumber = row.RowNumber,
                        BulkJobId = bulkJob.Id,
                        SponsorId = sponsorId,
                        Email = row.Email,
                        Phone = row.Phone,
                        DealerName = row.DealerName,
                        InvitationType = invitationType,
                        PackageTier = row.PackageTier ?? defaultTier,
                        CodeCount = row.CodeCount ?? defaultCodeCount,
                        SendSms = sendSms,
                        QueuedAt = DateTime.Now
                    };

                    var published = await _messageQueueService.PublishAsync(
                        queueName,
                        queueMessage,
                        bulkJob.Id.ToString());

                    if (published)
                    {
                        publishedCount++;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "⚠️ Failed to publish message - Row: {RowNumber}, Email: {Email}",
                            row.RowNumber, row.Email);
                    }
                }

                if (publishedCount == 0)
                {
                    bulkJob.Status = "Failed";
                    _bulkJobRepository.Update(bulkJob);
                    await _bulkJobRepository.SaveChangesAsync();

                    return new ErrorDataResult<BulkInvitationJobDto>(
                        "Hiçbir mesaj kuyruğa gönderilemedi. Lütfen tekrar deneyin.");
                }

                // Update job status to Processing
                bulkJob.Status = "Processing";
                bulkJob.StartedDate = DateTime.Now;
                _bulkJobRepository.Update(bulkJob);
                await _bulkJobRepository.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ Published {PublishedCount}/{TotalCount} messages to RabbitMQ - JobId: {JobId}",
                    publishedCount, rows.Count, bulkJob.Id);

                // 7. Return response
                var response = new BulkInvitationJobDto
                {
                    JobId = bulkJob.Id,
                    TotalDealers = bulkJob.TotalDealers,
                    Status = bulkJob.Status,
                    CreatedDate = bulkJob.CreatedDate,
                    StatusCheckUrl = $"/api/v1/sponsorship/dealer/bulk-status/{bulkJob.Id}"
                };

                return new SuccessDataResult<BulkInvitationJobDto>(
                    response,
                    $"Toplu davet işlemi başlatıldı. {publishedCount} dealer kuyruğa eklendi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in QueueBulkInvitationsAsync - SponsorId: {SponsorId}", sponsorId);
                return new ErrorDataResult<BulkInvitationJobDto>("Toplu davet işlemi başlatılamadı.");
            }
        }

        private IResult ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return new ErrorResult("Dosya yüklenmedi.");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return new ErrorResult($"Dosya boyutu çok büyük. Maksimum: {MaxFileSizeBytes / (1024 * 1024)} MB");
            }

            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                return new ErrorResult("Geçersiz dosya formatı. Sadece .xlsx ve .xls desteklenir.");
            }

            return new SuccessResult();
        }

        private async Task<List<DealerInvitationRow>> ParseExcelAsync(
            IFormFile file,
            bool useRowSpecificCounts,
            int defaultCodeCount,
            string defaultTier)
        {
            var rows = new List<DealerInvitationRow>();

            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);

            var worksheet = package.Workbook.Worksheets[0];
            var rowCount = worksheet.Dimension?.End.Row ?? 0;

            // Row 1 is header, start from row 2
            for (int row = 2; row <= rowCount; row++)
            {
                var email = worksheet.Cells[row, 1].Text?.Trim();
                var phone = worksheet.Cells[row, 2].Text?.Trim();
                var dealerName = worksheet.Cells[row, 3].Text?.Trim();
                var codeCountText = worksheet.Cells[row, 4].Text?.Trim();
                var tier = worksheet.Cells[row, 5].Text?.Trim();

                // Skip empty rows
                if (string.IsNullOrWhiteSpace(email) &&
                    string.IsNullOrWhiteSpace(phone) &&
                    string.IsNullOrWhiteSpace(dealerName))
                {
                    continue;
                }

                var invitationRow = new DealerInvitationRow
                {
                    RowNumber = row,
                    Email = email,
                    Phone = phone,
                    DealerName = dealerName,
                    CodeCount = useRowSpecificCounts && int.TryParse(codeCountText, out var count)
                        ? count
                        : (int?)null,
                    PackageTier = !string.IsNullOrWhiteSpace(tier) ? tier.ToUpper() : null
                };

                rows.Add(invitationRow);
            }

            return rows;
        }

        private async Task<IResult> ValidateRowsAsync(List<DealerInvitationRow> rows, int sponsorId)
        {
            var errors = new List<string>();
            var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var phones = new HashSet<string>();

            foreach (var row in rows)
            {
                // Email validation
                if (string.IsNullOrWhiteSpace(row.Email) || !IsValidEmail(row.Email))
                {
                    errors.Add($"Satır {row.RowNumber}: Geçersiz email - {row.Email}");
                    continue;
                }

                // Phone validation
                if (string.IsNullOrWhiteSpace(row.Phone) || !IsValidPhone(row.Phone))
                {
                    errors.Add($"Satır {row.RowNumber}: Geçersiz telefon - {row.Phone}");
                    continue;
                }

                // DealerName validation
                if (string.IsNullOrWhiteSpace(row.DealerName) || row.DealerName.Length > 200)
                {
                    errors.Add($"Satır {row.RowNumber}: Geçersiz dealer ismi");
                    continue;
                }

                // Duplicate check (in file)
                if (emails.Contains(row.Email))
                {
                    errors.Add($"Satır {row.RowNumber}: Duplicate email - {row.Email}");
                    continue;
                }

                if (phones.Contains(row.Phone))
                {
                    errors.Add($"Satır {row.RowNumber}: Duplicate telefon - {row.Phone}");
                    continue;
                }

                emails.Add(row.Email);
                phones.Add(row.Phone);
            }

            if (errors.Any())
            {
                return new ErrorResult(string.Join("\n", errors.Take(10)) +
                    (errors.Count > 10 ? $"\n... ve {errors.Count - 10} hata daha" : ""));
            }

            // Check existing dealers in database
            var existingDealers = await _userRepository.GetListAsync(u =>
                emails.Contains(u.Email));

            if (existingDealers.Any())
            {
                var existingEmails = string.Join(", ", existingDealers.Select(u => u.Email).Take(5));
                return new ErrorResult($"Bu email adresleri zaten kullanılıyor: {existingEmails}");
            }

            return new SuccessResult();
        }

        private async Task<IResult> CheckCodeAvailabilityAsync(List<DealerInvitationRow> rows, int sponsorId)
        {
            // Group by tier and sum required codes
            var requiredCodesByTier = rows
                .GroupBy(r => r.PackageTier ?? "Any")
                .ToDictionary(g => g.Key, g => g.Sum(r => r.CodeCount ?? 0));

            // Get available codes
            var availableCodes = await _codeRepository.GetListAsync(c =>
                c.SponsorId == sponsorId &&
                !c.IsUsed &&
                c.DealerId == null &&
                c.ReservedForInvitationId == null &&
                c.ExpiryDate > DateTime.Now);

            var codesByTier = availableCodes
                .GroupBy(c => c.TierName ?? "Any")
                .ToDictionary(g => g.Key, g => g.Count());

            // Check sufficiency
            foreach (var required in requiredCodesByTier)
            {
                var tier = required.Key;
                var count = required.Value;
                var available = codesByTier.GetValueOrDefault(tier, 0);

                if (available < count)
                {
                    return new ErrorResult(
                        $"Yetersiz kod ({tier} tier). Gerekli: {count}, Mevcut: {available}");
                }
            }

            return new SuccessResult();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhone(string phone)
        {
            var normalized = phone.Replace(" ", "").Replace("-", "")
                                  .Replace("(", "").Replace(")", "");

            // Turkish formats: +905xx, 905xx, 05xx
            if (normalized.StartsWith("+90") && normalized.Length == 13) return true;
            if (normalized.StartsWith("90") && normalized.Length == 12) return true;
            if (normalized.StartsWith("0") && normalized.Length == 11) return true;

            return false;
        }
    }
}
```

### 5.2 Command + Handler

**Business/Handlers/Sponsorship/Commands/BulkDealerInvitationCommand.cs:**
```csharp
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Business.Handlers.Sponsorship.Commands
{
    public class BulkDealerInvitationCommand : IRequest<IDataResult<BulkInvitationJobDto>>
    {
        public int SponsorId { get; set; }
        public IFormFile ExcelFile { get; set; }
        public string InvitationType { get; set; }  // "Invite" or "AutoCreate"
        public string DefaultTier { get; set; }      // S, M, L, XL (optional)
        public int DefaultCodeCount { get; set; }
        public bool SendSms { get; set; } = true;
        public bool UseRowSpecificCounts { get; set; } = false;
    }
}
```

**Business/Handlers/Sponsorship/Commands/BulkDealerInvitationCommandHandler.cs:**
```csharp
using Business.BusinessAspects;
using Business.Services.Sponsorship;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Commands
{
    public class BulkDealerInvitationCommandHandler
        : IRequestHandler<BulkDealerInvitationCommand, IDataResult<BulkInvitationJobDto>>
    {
        private readonly IBulkDealerInvitationService _bulkInvitationService;

        public BulkDealerInvitationCommandHandler(IBulkDealerInvitationService bulkInvitationService)
        {
            _bulkInvitationService = bulkInvitationService;
        }

        [SecuredOperation(Priority = 1)]
        public async Task<IDataResult<BulkInvitationJobDto>> Handle(
            BulkDealerInvitationCommand request,
            CancellationToken cancellationToken)
        {
            return await _bulkInvitationService.QueueBulkInvitationsAsync(
                request.ExcelFile,
                request.SponsorId,
                request.InvitationType,
                request.DefaultTier,
                request.DefaultCodeCount,
                request.SendSms,
                request.UseRowSpecificCounts);
        }
    }
}
```

---

## 6. SignalR Real-time Notifications

### 6.1 Progress Notification Service

**Business/Services/Notification/BulkInvitationNotificationService.cs:**
```csharp
using Business.Hubs;
using Entities.Dtos;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Business.Services.Notification
{
    public interface IBulkInvitationNotificationService
    {
        Task NotifyProgressAsync(BulkInvitationProgressDto progress);
        Task NotifyCompletedAsync(int bulkJobId, int sponsorId, string status, int successCount, int failedCount);
    }

    public class BulkInvitationNotificationService : IBulkInvitationNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<BulkInvitationNotificationService> _logger;

        public BulkInvitationNotificationService(
            IHubContext<NotificationHub> hubContext,
            ILogger<BulkInvitationNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyProgressAsync(BulkInvitationProgressDto progress)
        {
            try
            {
                var sponsorGroup = $"sponsor_{progress.SponsorId}";

                await _hubContext.Clients.Group(sponsorGroup).SendAsync(
                    "BulkInvitationProgress",
                    progress);

                _logger.LogInformation(
                    "📊 Progress notification sent - SponsorId: {SponsorId}, Progress: {Progress}%",
                    progress.SponsorId, progress.ProgressPercentage);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "⚠️ Failed to send progress notification - SponsorId: {SponsorId}",
                    progress.SponsorId);
            }
        }

        public async Task NotifyCompletedAsync(
            int bulkJobId,
            int sponsorId,
            string status,
            int successCount,
            int failedCount)
        {
            try
            {
                var sponsorGroup = $"sponsor_{sponsorId}";

                var completedData = new
                {
                    BulkJobId = bulkJobId,
                    Status = status,
                    SuccessCount = successCount,
                    FailedCount = failedCount,
                    CompletedAt = DateTime.Now
                };

                await _hubContext.Clients.Group(sponsorGroup).SendAsync(
                    "BulkInvitationCompleted",
                    completedData);

                _logger.LogInformation(
                    "✅ Completion notification sent - SponsorId: {SponsorId}, Status: {Status}",
                    sponsorId, status);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "⚠️ Failed to send completion notification - SponsorId: {SponsorId}",
                    sponsorId);
            }
        }
    }
}
```

### 6.2 NotificationHub Update

**Business/Hubs/NotificationHub.cs (Update):**
```csharp
public override async Task OnConnectedAsync()
{
    var httpContext = Context.GetHttpContext();
    var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (!string.IsNullOrEmpty(userId))
    {
        // Add to user-specific group for notifications
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        // Add to sponsor group if user is sponsor
        if (httpContext.User.IsInRole("Sponsor"))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"sponsor_{userId}");
        }

        _logger.LogInformation(
            "SignalR connected - UserId: {UserId}, ConnectionId: {ConnectionId}",
            userId, Context.ConnectionId);
    }

    await base.OnConnectedAsync();
}
```

---

## 7. Hata Yönetimi ve Retry Logic

### 7.1 Retry Strategy

**WorkerService Level (RabbitMQ):**
- JSON deserialization error → **No retry** (Nack without requeue)
- Transient errors (DB, network) → **Retry** (Nack with requeue)
- Max retry: Controlled by RabbitMQ's `x-max-retries` header

**Hangfire Level:**
- Automatic retry for failed jobs
- Exponential backoff: 1 min, 5 min, 15 min, 30 min
- Max 4 retry attempts

### 7.2 Error Tracking

**BulkInvitationJob.ErrorSummary (JSON):**
```json
{
  "errors": [
    {
      "rowNumber": 12,
      "email": "invalid@email.com",
      "error": "Geçersiz email formatı",
      "timestamp": "2025-11-03T15:30:00Z"
    },
    {
      "rowNumber": 45,
      "email": "existing@dealer.com",
      "error": "Bu email ile zaten bir dealer mevcut",
      "timestamp": "2025-11-03T15:31:00Z"
    }
  ]
}
```

---

## 8. İmplementasyon Planı

### Aşama 1: Core Components (2 gün)

**Day 1: Entities & DTOs**
- [ ] `BulkInvitationJob` entity
- [ ] `DealerInvitationQueueMessage` DTO
- [ ] `BulkInvitationProgressDto` DTO
- [ ] `DealerInvitationRow` DTO
- [ ] `BulkInvitationJobDto` DTO
- [ ] Repository interface & implementation
- [ ] Database migration

**Day 2: RabbitMQ Configuration**
- [ ] Update `RabbitMQOptions` with new queue
- [ ] Test queue declaration
- [ ] Message serialization tests

---

### Aşama 2: API Service Layer (2 gün)

**Day 3: Excel Processing**
- [ ] `BulkDealerInvitationService` interface
- [ ] File validation logic
- [ ] Excel parsing (EPPlus)
- [ ] Row validation logic
- [ ] Code availability check

**Day 4: Queue Publishing**
- [ ] RabbitMQ message publishing
- [ ] Bulk job entity creation
- [ ] Error handling
- [ ] Unit tests

---

### Aşama 3: WorkerService Consumer (2 gün)

**Day 5: Consumer Implementation**
- [ ] `DealerInvitationConsumerWorker` class
- [ ] RabbitMQ connection setup
- [ ] Message consumption logic
- [ ] Hangfire job enqueuing
- [ ] Register in Program.cs

**Day 6: Hangfire Job**
- [ ] `IDealerInvitationJobService` interface
- [ ] `DealerInvitationJobService` implementation
- [ ] `ProcessDealerInvitationAsync` method
- [ ] Code reservation/transfer logic
- [ ] SMS sending integration
- [ ] Progress tracking

---

### Aşama 4: SignalR Notifications (1 gün)

**Day 7: Real-time Updates**
- [ ] `BulkInvitationNotificationService`
- [ ] Progress notification
- [ ] Completion notification
- [ ] NotificationHub group management
- [ ] Testing with SignalR client

---

### Aşama 5: API Endpoints (1 gün)

**Day 8: Controller & Queries**
- [ ] `BulkDealerInvitationCommand` + Handler
- [ ] `POST /dealer/invite-bulk` endpoint
- [ ] `GET /dealer/bulk-status/{jobId}` query
- [ ] `GET /dealer/bulk-history` query
- [ ] Swagger annotations

---

### Aşama 6: Testing (2 gün)

**Day 9: Unit & Integration Tests**
- [ ] Excel parser tests
- [ ] Validation logic tests
- [ ] RabbitMQ publish tests
- [ ] Consumer tests
- [ ] Hangfire job tests

**Day 10: End-to-End Testing**
- [ ] 10 dealers (< 10 sec)
- [ ] 100 dealers (< 1 min)
- [ ] 500 dealers (< 3 min)
- [ ] Error scenarios
- [ ] SignalR notification verification

---

## Toplam Süre: 10 İş Günü

## Sonuç

Bu tasarım, mevcut PlantAnalysis async altyapısını kullanarak:

✅ **RabbitMQ queue-based processing**
✅ **Mevcut WorkerService'i extend ediyor**
✅ **Hangfire ile retry & reliability**
✅ **SignalR ile real-time progress**
✅ **Scalable & fault-tolerant**
✅ **No new dependencies (Hangfire zaten var)**

**Avantajlar:**
- Proven pattern (PlantAnalysis'te çalışıyor)
- Bağımsız WorkerService (API'dan ayrı scale edilebilir)
- Message durability (RabbitMQ persistent queue)
- Rate limiting & backpressure (RabbitMQ prefetch)
- Comprehensive error handling & logging

**İlgili Dosyalar:**
- Önceki Tasarım: `claudedocs/Dealers/BULK_DEALER_INVITATION_DESIGN.md`
- PlantAnalysis Async: `Business/Services/PlantAnalysis/PlantAnalysisAsyncService.cs`
- Worker: `PlantAnalysisWorkerService/Services/RabbitMQConsumerWorker.cs`
