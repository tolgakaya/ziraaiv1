# ZiraAI Platform - Production Readiness Implementation Plan

**Proje:** 1 Milyon Günlük Analiz Kapasitesi için Sistem Modernizasyonu
**Versiyon:** 1.0
**Tarih:** Kasım 2025
**Durum:** Production Readiness Phase

---

## İçindekiler

1. [Yönetici Özeti](#1-yönetici-özeti)
2. [Teknik Yaklaşım](#2-teknik-yaklaşım)
3. [Proje Yapısı](#3-proje-yapısı)
4. [Phase 1: Temel Altyapı (Hafta 1-2)](#4-phase-1-temel-altyapı-hafta-1-2)
5. [Phase 2: Multi-Provider (Hafta 3-4)](#5-phase-2-multi-provider-hafta-3-4)
6. [Phase 3: Admin Panel ve Scale (Hafta 5-6)](#6-phase-3-admin-panel-ve-scale-hafta-5-6)
7. [Phase 4: Production Hardening (Hafta 7-8)](#7-phase-4-production-hardening-hafta-7-8)
8. [Test Stratejisi](#8-test-stratejisi)
9. [Deployment Stratejisi](#9-deployment-stratejisi)
10. [Monitoring ve Operations](#10-monitoring-ve-operations)
11. [Risk Analizi](#11-risk-analizi)
12. [Success Criteria](#12-success-criteria)

---

## 1. Yönetici Özeti

### 1.1 Proje Hedefleri

| Metrik | Mevcut | Hedef | Artış |
|--------|--------|-------|-------|
| Günlük Analiz | ~1,200 | 1,000,000 | 833x |
| Dakikalık Throughput | ~0.85 | 694 | 816x |
| Concurrent İşlem | 1 | ~810 | 810x |
| Response Time | ~70 sn | ~70 sn | Korunacak |

### 1.2 Teknik Yaklaşım

**Hybrid Mimari:**
- **Korunacak:** .NET WebAPI, .NET Result Worker, PostgreSQL, RabbitMQ, Redis
- **Eklenecek:** TypeScript AI Workers, TypeScript Dispatcher, Next.js Admin Panel
- **Kaldırılacak:** n8n workflow (bottleneck)

**Temel Değişiklikler:**
- n8n → Native TypeScript workers
- Single provider (OpenAI) → Multi-provider (OpenAI + Gemini + Anthropic)
- No rate limiting → Redis-based centralized rate limiting
- No failover → Automatic circuit breaker and failover

### 1.3 Timeline

- **Phase 1:** Hafta 1-2 (Temel altyapı, OpenAI worker)
- **Phase 2:** Hafta 3-4 (Multi-provider, dispatcher)
- **Phase 3:** Hafta 5-6 (Admin panel, scale management)
- **Phase 4:** Hafta 7-8 (Production hardening, rollout)

**Toplam:** 8 hafta (50 iş günü)

### 1.4 Maliyet Analizi

**Aylık AI API Maliyeti (1M/gün):**
```
OpenAI (333K):     $4,995
Gemini (334K):     $4,008
Anthropic (333K):  $4,329
─────────────────────────
Günlük Toplam:     $13,332
Aylık Toplam:      ~$400,000
```

**Infrastructure Maliyeti (Railway):**
```
AI Workers (15):       $150/mo
Other Services (8):    $160/mo
────────────────────────────
Toplam:               ~$310/mo
```

**Toplam Aylık Maliyet:** ~$400,310

**Cost per Analysis:** $0.40

---

## 2. Teknik Yaklaşım

### 2.1 Geliştirme Ortamı

**ÖNEMLİ:** Tüm geliştirme, test ve implementasyon Railway Staging ortamında yapılacaktır.

**Ortam Stratejisi:**
- **Staging (Railway):** Tüm yeni özellikler burada geliştirilip test edilecek
- **Production (Railway):** Sadece Staging'de onaylanmış değişiklikler deploy edilecek
- **Local Development:** Minimal kullanım, sadece IDE-based kod yazımı için

**Railway Staging Özellikleri:**
- Dedicated RabbitMQ instance
- Dedicated Redis instance
- PostgreSQL database (staging schema)
- Cloudflare R2 (staging bucket: ziraai-messages-staging)
- Environment variables yönetimi
- Real-time logs ve monitoring
- Container-based deployment

### 2.2 Mimari Karşılaştırma

**Mevcut Mimari (Production):**
```
Client → WebAPI → RabbitMQ → n8n (parallelMessages: 1) → OpenAI
                                ↓
                         RabbitMQ (result)
                                ↓
                    PlantAnalysisWorkerService
                                ↓
                          PostgreSQL
```

**Problem:** n8n bottleneck - aynı anda sadece 1 mesaj işliyor!

**Yeni Mimari:**
```
Client → WebAPI (.NET) → raw-analysis-queue
                              ↓
                        Dispatcher (TS)
                         /    |    \
                        /     |     \
            OpenAI-Q  Gemini-Q  Claude-Q
                |        |        |
          OpenAI×5  Gemini×5  Claude×5  (TS Workers)
                \      |       /
                 \     |      /
              analysis-results queue
                      ↓
          PlantAnalysisWorkerService (.NET)
                      ↓
                PostgreSQL
```

**Avantajlar:**
- 15 worker aynı anda çalışıyor (vs 1)
- 3 AI provider (vs 1)
- Merkezi rate limiting
- Automatic failover
- Real-time monitoring

### 2.2 Yeni Projeler

**1. TypeScript Analysis Worker** (`workers/analysis-worker/`)
- **Sorumluluk:** AI provider'lara çağrı yapma, analiz döndürme
- **Teknoloji:** TypeScript, Node.js 20, Docker
- **Concurrency:** 60-70 per worker
- **Providers:** OpenAI, Gemini, Anthropic

**2. TypeScript Dispatcher** (`workers/dispatcher/`)
- **Sorumluluk:** Provider seçimi, rate limit kontrolü, routing
- **Teknoloji:** TypeScript, Node.js 20, Docker
- **Algoritma:** Capacity-based + health-based selection

**3. Next.js Admin Panel** (`admin-panel/`)
- **Sorumluluk:** Monitoring, scale control, alerting
- **Teknoloji:** Next.js 14, React, Tailwind CSS, shadcn/ui
- **Features:** Real-time dashboard, manual scaling, cost tracking

### 2.3 Mevcut Proje Değişiklikleri

**WebAPI (.NET) - Minimal Değişiklik**
- `Business/Services/PlantAnalysisAsyncService.cs` güncelleme
- Yeni queue'a publish: `raw-analysis-queue`
- Feature flag desteği (yeni/eski sistem toggle)
- Message format: `RawAnalysisMessage` DTO

**PlantAnalysisWorkerService (.NET) - Minimal Değişiklik**
- Message format uyumlulaştırma: `ResultQueueMessage`
- Provider metadata'yı DB'ye kaydetme
- Token usage tracking
- Cost tracking

**Database (PostgreSQL) - Migration**
```sql
-- PlantAnalyses tablosu güncelleme
ALTER TABLE "PlantAnalyses"
ADD COLUMN "Provider" VARCHAR(20),
ADD COLUMN "ProcessingTimeMs" INTEGER,
ADD COLUMN "TokenUsage" JSONB,
ADD COLUMN "CostUsd" DECIMAL(10,6);

CREATE INDEX "IX_PlantAnalyses_Provider"
ON "PlantAnalyses" ("Provider");

-- Yeni tablo: DailyMetrics
CREATE TABLE "DailyMetrics" (
    "Id" SERIAL PRIMARY KEY,
    "Date" DATE NOT NULL,
    "Provider" VARCHAR(20) NOT NULL,
    "TotalRequests" INTEGER DEFAULT 0,
    "SuccessfulRequests" INTEGER DEFAULT 0,
    "FailedRequests" INTEGER DEFAULT 0,
    "TotalTokens" INTEGER DEFAULT 0,
    "TotalCostUsd" DECIMAL(12,4) DEFAULT 0,
    "AvgProcessingTimeMs" INTEGER,
    "P95ProcessingTimeMs" INTEGER,
    UNIQUE("Date", "Provider")
);
```

---

## 3. Proje Yapısı

### 3.1 Klasör Organizasyonu

```
ziraai/
├── WebAPI/                          # .NET 9.0 (MEVCUT - minimal değişiklik)
├── PlantAnalysisWorkerService/      # .NET 9.0 (MEVCUT - minimal değişiklik)
├── Business/                        # .NET (MEVCUT)
├── DataAccess/                      # .NET (MEVCUT)
├── Core/                            # .NET (MEVCUT)
├── Entities/                        # .NET (MEVCUT)
│
├── workers/                         # YENİ - TypeScript AI Workers
│   ├── dispatcher/                  # Provider seçimi ve routing
│   │   ├── src/
│   │   │   ├── index.ts
│   │   │   ├── dispatcher.ts
│   │   │   ├── provider-selector.ts
│   │   │   ├── config.ts
│   │   │   └── utils/
│   │   ├── Dockerfile
│   │   ├── package.json
│   │   └── tsconfig.json
│   │
│   ├── analysis-worker/             # AI provider worker'ları
│   │   ├── src/
│   │   │   ├── index.ts
│   │   │   ├── worker.ts
│   │   │   ├── config.ts
│   │   │   ├── providers/
│   │   │   │   ├── base.ts
│   │   │   │   ├── openai.ts
│   │   │   │   ├── gemini.ts
│   │   │   │   └── anthropic.ts
│   │   │   ├── utils/
│   │   │   │   ├── rabbitmq.ts
│   │   │   │   ├── redis.ts
│   │   │   │   ├── rate-limiter.ts
│   │   │   │   ├── prompt-builder.ts
│   │   │   │   └── logger.ts
│   │   │   └── health-check.ts
│   │   ├── tests/
│   │   │   ├── unit/
│   │   │   └── integration/
│   │   ├── Dockerfile
│   │   ├── package.json
│   │   └── tsconfig.json
│   │
│   └── shared/                      # Ortak tipler ve utilities
│       ├── types/
│       │   ├── messages.ts
│       │   ├── config.ts
│       │   └── analysis.ts
│       └── utils/
│           ├── rate-limiter.ts
│           ├── circuit-breaker.ts
│           └── retry.ts
│
├── admin-panel/                     # YENİ - Next.js Admin Panel
│   ├── src/
│   │   ├── app/
│   │   │   ├── layout.tsx
│   │   │   ├── page.tsx
│   │   │   ├── dashboard/
│   │   │   └── api/
│   │   │       ├── status/
│   │   │       ├── scale/
│   │   │       ├── metrics/
│   │   │       └── health/
│   │   ├── components/
│   │   │   ├── dashboard/
│   │   │   ├── charts/
│   │   │   └── scale-control/
│   │   └── lib/
│   ├── public/
│   ├── package.json
│   ├── tsconfig.json
│   └── next.config.js
│
├── scripts/                         # YENİ - Deployment ve ops scripts
│   ├── deploy.sh
│   ├── scale.sh
│   ├── health-check.sh
│   └── load-test.js
│
├── claudedocs/                      # Dökümanlar
│   ├── ziraai-platform-analysis.md
│   ├── PRODUCTION_READINESS_IMPLEMENTATION_PLAN.md
│   └── ...
│
└── railway.staging.json             # Railway Staging configuration
```

### 3.2 RabbitMQ Queue Yapısı

| Queue | Producer | Consumer | Durable | TTL | DLX |
|-------|----------|----------|---------|-----|-----|
| `raw-analysis-queue` | WebAPI | Dispatcher | ✅ | - | analysis-dlq |
| `openai-analysis-queue` | Dispatcher | OpenAI Worker | ✅ | 5dk | analysis-dlq |
| `gemini-analysis-queue` | Dispatcher | Gemini Worker | ✅ | 5dk | analysis-dlq |
| `claude-analysis-queue` | Dispatcher | Claude Worker | ✅ | 5dk | analysis-dlq |
| `analysis-results` | AI Workers | PlantAnalysisWorkerService | ✅ | - | result-dlq |
| `analysis-dlq` | - | Manual Review | ✅ | 7gün | - |

### 3.3 Redis Key Yapısı

```
# Rate Limiting
rate:openai:{minute_window}     = 245     # Current usage
rate:gemini:{minute_window}     = 180
rate:anthropic:{minute_window}  = 320

# Health Status
health:openai                   = "ok"    # ok | degraded | down
health:gemini                   = "ok"
health:anthropic                = "down"

# Error Tracking (Circuit Breaker)
errors:openai:{minute_window}   = 2       # Error count
errors:gemini:{minute_window}   = 0
errors:anthropic:{minute_window} = 5

# Scale Configuration
scale:openai                    = 5       # Target worker count
scale:gemini                    = 5
scale:anthropic                 = 5

# Metrics
metrics:success:openai          = 12450   # Total success count
metrics:success:gemini          = 8930
metrics:errors:openai           = 23
metrics:latency:openai          = 45000   # Avg latency ms
```

---

## 4. Phase 1: Temel Altyapı (Hafta 1-2)

**🚀 Deployment Ortamı:** Railway Staging
**📦 Container:** Railway container-based deployment
**🔧 Tools:** Railway CLI, Git push deployment

### 4.1 Amaç

Railway Staging ortamında n8n'siz çalışan minimal sistem oluşturmak. Sadece OpenAI provider ile ~50K/gün kapasite.

### 4.2 Adımlar (Test Edilebilir)

#### Gün 1-2: Proje Yapısı Oluşturma ✅ **TAMAMLANDI** (30 Kasım 2025)

**Adım 1.1: TypeScript Worker Projesi** ✅
```bash
# workers/ klasörü oluştur
mkdir -p workers/analysis-worker/src/providers
mkdir -p workers/shared/types
mkdir -p workers/shared/utils

# Package.json oluştur
cd workers/analysis-worker
npm init -y
npm install --save typescript @types/node amqplib ioredis openai pino
npm install --save-dev @types/amqplib ts-node nodemon jest @types/jest
```

**Test:** ✅ `npm install` ve `npm run build` başarılı (0 errors, 0 warnings)

**Tamamlanan İşler:**
- ✅ Message type definitions (RawAnalysisMessage, AnalysisResultMessage)
- ✅ OpenAI provider implementation (794 lines)
- ✅ Multi-image support (5 images: main, leaf_top, leaf_bottom, plant_overview, root)
- ✅ Turkish system prompt (362 lines, exact n8n match)
- ✅ Token usage tracking (gpt-5-mini pricing)
- ✅ RabbitMQ service integration
- ✅ Rate limiting service structure
- ✅ Worker entry point (index.ts)
- ✅ TypeScript configuration
- ✅ Field naming convention (snake_case alignment with n8n)

**Dokumentasyon:**
- 📄 [PHASE1_DAY1_TYPESCRIPT_WORKER_IMPLEMENTATION.md](./PHASE1_DAY1_TYPESCRIPT_WORKER_IMPLEMENTATION.md)

**Kritik İyileştirmeler:**
- n8n flow tam eşleşme: %100 compliance
- Multi-image analysis: 5 görsel desteği
- Token cost tracking: Detaylı maliyet hesaplama
- Error handling: Comprehensive fallback systems

**Adım 1.2: TypeScript Configuration**
```json
// workers/analysis-worker/tsconfig.json
{
  "compilerOptions": {
    "target": "ES2020",
    "module": "commonjs",
    "lib": ["ES2020"],
    "outDir": "./dist",
    "rootDir": "./src",
    "strict": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "forceConsistentCasingInFileNames": true,
    "resolveJsonModule": true
  },
  "include": ["src/**/*"],
  "exclude": ["node_modules", "dist"]
}
```

**Adım 1.3: Admin Panel Projesi**
```bash
# admin-panel/ klasörü oluştur
cd ../../
npx create-next-app@latest admin-panel --typescript --tailwind --app --no-src-dir
cd admin-panel
npm install @radix-ui/react-icons recharts lucide-react
npx shadcn-ui@latest init
```

**Test:** `npm run dev` çalışıyor mu?

#### Gün 3-4: RabbitMQ Queue Setup

**Adım 1.4: Queue Oluşturma (Railway Staging RabbitMQ)**

Railway Staging RabbitMQ Management UI'da manuel olarak (CloudAMQP dashboard):

1. `raw-analysis-queue` oluştur:
   - Type: classic
   - Durability: Durable
   - Auto delete: No
   - Arguments: `x-dead-letter-exchange: analysis-dlx`

2. `openai-analysis-queue` oluştur:
   - Type: classic
   - Durability: Durable
   - Auto delete: No
   - Arguments:
     - `x-message-ttl: 300000` (5 dakika)
     - `x-dead-letter-exchange: analysis-dlx`

3. `analysis-dlq` oluştur (Dead Letter Queue)
   - Type: classic
   - Durability: Durable
   - Auto delete: No
   - Arguments: `x-message-ttl: 604800000` (7 gün)

**Test:** RabbitMQ UI'da queue'lar görünüyor mu?

**Adım 1.5: Test Mesajları (Railway Staging)**

Railway Staging RabbitMQ'ya test mesajı gönderme:
```bash
# Railway Staging RabbitMQ HTTP API ile mesaj publish
# URL ve credentials Railway dashboard'dan alınacak
curl -u $RABBITMQ_USER:$RABBITMQ_PASS -X POST \
  $RABBITMQ_MANAGEMENT_URL/api/exchanges/%2F/amq.default/publish \
  -H "content-type:application/json" \
  -d '{"properties":{},"routing_key":"raw-analysis-queue","payload":"{\"test\":\"message\"}","payload_encoding":"string"}'
```

**Test:** Queue'da mesaj görünüyor mu?

#### Gün 5-7: WebAPI Değişiklikleri

**Adım 1.6: DTO Oluşturma**

[Business/Services/PlantAnalysis/PlantAnalysisAsyncService.cs](cci:7://file:///c:/Users/Asus/Documents/Visual%20Studio%202022/ziraai/Business/Services/PlantAnalysis/PlantAnalysisAsyncService.cs:0:0-0:0) dosyasını oku ve mevcut yapıyı anla:

```bash
# Serena ile kod analizi
```

**Adım 1.7: Feature Flag Ekleme**

`appsettings.Development.json`:
```json
{
  "Features": {
    "UseNewWorkerSystem": false  // İlk başta false
  },
  "RabbitMQ": {
    "Queues": {
      "RawAnalysisQueue": "raw-analysis-queue",
      "PlantAnalysisRequest": "plant-analysis-multi-image-requests",
      "PlantAnalysisResult": "plant-analysis-results"
    }
  }
}
```

**Adım 1.8: Service Güncelleme**

```csharp
// Business/Services/PlantAnalysis/PlantAnalysisAsyncService.cs

public async Task<IResult> PublishAnalysisRequest(PlantAnalysisRequest request)
{
    if (_configuration.GetValue<bool>("Features:UseNewWorkerSystem"))
    {
        return await PublishToRawQueue(request);
    }
    else
    {
        return await PublishToN8nQueue(request); // Mevcut metod
    }
}

private async Task<IResult> PublishToRawQueue(PlantAnalysisRequest request)
{
    var message = new RawAnalysisMessage
    {
        AnalysisId = Guid.NewGuid().ToString(),
        Timestamp = DateTime.UtcNow,
        LeafTopUrl = request.LeafTopUrl,
        LeafBottomUrl = request.LeafBottomUrl,
        PlantOverviewUrl = request.PlantOverviewUrl,
        RootUrl = request.RootUrl,
        FarmerId = request.FarmerId,
        SponsorId = request.SponsorId,
        Location = request.Location,
        CropType = request.CropType,
        UrgencyLevel = request.UrgencyLevel ?? "normal",
        CreatedAt = DateTime.UtcNow
    };

    var queueName = _configuration["RabbitMQ:Queues:RawAnalysisQueue"];
    await _rabbitMQPublisher.PublishAsync(queueName, message);

    return new SuccessResult($"Analysis queued: {message.AnalysisId}");
}
```

**Test:**
- Unit test: Message doğru formatta mı?
- Integration test: Queue'ya publish ediliyor mu?
- Postman: `/api/v1/plant-analyses/analyze-async` endpoint test

#### Gün 8-12: OpenAI Worker Implementation

**Adım 1.9: Shared Types**

`workers/shared/types/messages.ts`:
```typescript
export interface RawAnalysisMessage {
  analysis_id: string;
  timestamp: string;

  // Images
  leaf_top_url?: string;
  leaf_bottom_url?: string;
  plant_overview_url?: string;
  root_url?: string;

  // User info
  farmer_id?: string;
  sponsor_id?: string;

  // Context
  location?: string;
  crop_type?: string;
  urgency_level: "low" | "normal" | "high" | "critical";

  // Internal
  _created_at: string;
}

export interface AnalysisResult {
  diseases?: Disease[];
  pests?: Pest[];
  deficiencies?: Deficiency[];
  environmental_stress?: EnvironmentalStress[];
  recommendations?: Recommendation[];
  summary: string;
}

export interface ResultQueueMessage {
  analysis_id: string;
  farmer_id?: string;
  sponsor_id?: string;
  result: AnalysisResult;
  processing_metadata: {
    provider: string;
    processing_time_ms: number;
    completed_at: string;
    token_usage: {
      prompt_tokens: number;
      completion_tokens: number;
      total_tokens: number;
    };
  };
}
```

**Adım 1.10: OpenAI Provider**

`workers/analysis-worker/src/providers/openai.ts`:
```typescript
import OpenAI from 'openai';
import { AIProvider, AnalysisRequest, AnalysisResult } from './base';
import { logger } from '../utils/logger';

export class OpenAIProvider implements AIProvider {
  private client: OpenAI;

  constructor(apiKey: string) {
    this.client = new OpenAI({ apiKey });
  }

  async analyze(request: AnalysisRequest): Promise<AnalysisResult> {
    const startTime = Date.now();

    try {
      const messages = [
        {
          role: 'user' as const,
          content: [
            {
              type: 'text' as const,
              text: this.buildPrompt(request)
            },
            ...this.buildImageContents(request)
          ]
        }
      ];

      logger.info({ analysisId: request.analysis_id }, 'Calling OpenAI API');

      const response = await this.client.chat.completions.create({
        model: 'gpt-4o-mini',
        max_tokens: 5000,
        messages,
        response_format: { type: 'json_object' }
      });

      const duration = Date.now() - startTime;
      logger.info({
        analysisId: request.analysis_id,
        duration,
        tokens: response.usage
      }, 'OpenAI API completed');

      return JSON.parse(response.choices[0].message.content!);
    } catch (error) {
      logger.error({
        analysisId: request.analysis_id,
        error
      }, 'OpenAI API failed');
      throw error;
    }
  }

  private buildPrompt(request: AnalysisRequest): string {
    // TODO: Mevcut n8n prompt'unu buraya kopyala
    return `You are an expert agricultural analyst...`;
  }

  private buildImageContents(request: AnalysisRequest) {
    const images = [];
    if (request.leaf_top_url) {
      images.push({
        type: 'image_url' as const,
        image_url: { url: request.leaf_top_url }
      });
    }
    if (request.leaf_bottom_url) {
      images.push({
        type: 'image_url' as const,
        image_url: { url: request.leaf_bottom_url }
      });
    }
    if (request.plant_overview_url) {
      images.push({
        type: 'image_url' as const,
        image_url: { url: request.plant_overview_url }
      });
    }
    if (request.root_url) {
      images.push({
        type: 'image_url' as const,
        image_url: { url: request.root_url }
      });
    }
    return images;
  }

  getProviderName(): string {
    return 'openai';
  }
}
```

**Adım 1.11: Worker Main Logic**

`workers/analysis-worker/src/worker.ts`:
```typescript
import * as amqp from 'amqplib';
import { Redis } from 'ioredis';
import { AIProvider } from './providers/base';
import { OpenAIProvider } from './providers/openai';
import { logger } from './utils/logger';
import { RateLimiter } from './utils/rate-limiter';
import { RawAnalysisMessage, ResultQueueMessage } from '../../shared/types/messages';

export class Worker {
  private connection: amqp.Connection;
  private channel: amqp.Channel;
  private redis: Redis;
  private provider: AIProvider;
  private rateLimiter: RateLimiter;

  constructor(private config: WorkerConfig) {
    this.redis = new Redis(config.redis.url);
    this.provider = new OpenAIProvider(config.openai.apiKey);
    this.rateLimiter = new RateLimiter(this.redis, {
      provider: 'openai',
      rateLimit: config.rateLimit
    });
  }

  async start() {
    // Connect to RabbitMQ
    this.connection = await amqp.connect(this.config.rabbitmq.url);
    this.channel = await this.connection.createChannel();

    // Setup consumer
    await this.channel.prefetch(this.config.concurrency);

    logger.info({
      queueName: this.config.queueName,
      concurrency: this.config.concurrency
    }, 'Worker started');

    await this.channel.consume(
      this.config.queueName,
      async (msg) => {
        if (!msg) return;

        try {
          await this.processMessage(msg);
          this.channel.ack(msg);
        } catch (error) {
          logger.error({ error }, 'Message processing failed');
          // Requeue with limit
          this.channel.nack(msg, false, msg.fields.redelivered === false);
        }
      }
    );
  }

  private async processMessage(msg: amqp.Message) {
    const request: RawAnalysisMessage = JSON.parse(msg.content.toString());

    logger.info({ analysisId: request.analysis_id }, 'Processing analysis');

    // Wait for rate limit
    await this.rateLimiter.waitForAvailability();

    const startTime = Date.now();

    // Call AI provider
    const result = await this.provider.analyze(request);

    const processingTime = Date.now() - startTime;

    // Publish result
    const resultMessage: ResultQueueMessage = {
      analysis_id: request.analysis_id,
      farmer_id: request.farmer_id,
      sponsor_id: request.sponsor_id,
      result,
      processing_metadata: {
        provider: this.provider.getProviderName(),
        processing_time_ms: processingTime,
        completed_at: new Date().toISOString(),
        token_usage: {
          prompt_tokens: 0, // TODO: Extract from API response
          completion_tokens: 0,
          total_tokens: 0
        }
      }
    };

    await this.channel.sendToQueue(
      this.config.resultQueue,
      Buffer.from(JSON.stringify(resultMessage)),
      { persistent: true }
    );

    logger.info({
      analysisId: request.analysis_id,
      processingTime
    }, 'Analysis completed');
  }

  async stop() {
    logger.info('Stopping worker');
    await this.channel.close();
    await this.connection.close();
    await this.redis.quit();
  }
}
```

**Adım 1.12: Entry Point**

`workers/analysis-worker/src/index.ts`:
```typescript
import { Worker } from './worker';
import { logger } from './utils/logger';

const config = {
  rabbitmq: {
    url: process.env.RABBITMQ_URL!,
  },
  redis: {
    url: process.env.REDIS_URL!,
  },
  queueName: process.env.QUEUE_NAME || 'openai-analysis-queue',
  resultQueue: process.env.RESULT_QUEUE || 'analysis-results',
  concurrency: parseInt(process.env.CONCURRENCY || '60'),
  rateLimit: parseInt(process.env.RATE_LIMIT || '350'),
  openai: {
    apiKey: process.env.OPENAI_API_KEY!,
  }
};

async function main() {
  const worker = new Worker(config);

  process.on('SIGTERM', async () => {
    logger.info('SIGTERM received, shutting down gracefully');
    await worker.stop();
    process.exit(0);
  });

  process.on('SIGINT', async () => {
    logger.info('SIGINT received, shutting down gracefully');
    await worker.stop();
    process.exit(0);
  });

  await worker.start();
}

main().catch(err => {
  logger.error({ err }, 'Worker failed to start');
  process.exit(1);
});
```

**Test:**
- Unit test: OpenAI provider mock ile test
- Integration test: Local RabbitMQ + Redis ile test
- E2E test: Gerçek OpenAI API ile test

#### Gün 13-14: Docker ve Local Dev

**Adım 1.13: Dockerfile**

`workers/analysis-worker/Dockerfile`:
```dockerfile
FROM node:20-alpine AS builder

WORKDIR /app

# Copy package files
COPY workers/shared/package*.json ./shared/
COPY workers/analysis-worker/package*.json ./

# Install dependencies
WORKDIR /app/shared
RUN npm ci

WORKDIR /app
RUN npm ci

# Copy source
COPY workers/shared /app/shared
COPY workers/analysis-worker /app

# Build
RUN npm run build

# Production image
FROM node:20-alpine

WORKDIR /app

ENV NODE_ENV=production

# Copy built artifacts
COPY --from=builder /app/dist ./dist
COPY --from=builder /app/node_modules ./node_modules
COPY --from=builder /app/shared ./shared

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD node dist/health-check.js || exit 1

CMD ["node", "dist/index.js"]
```

**Adım 1.14: Railway Deployment**

Railway'e worker service deploy etme:

1. `railway.json` oluştur (workers/analysis-worker dizininde):
```json
{
  "$schema": "https://railway.app/railway.schema.json",
  "build": {
    "builder": "DOCKERFILE",
    "dockerfilePath": "Dockerfile"
  },
  "deploy": {
    "numReplicas": 5,
    "sleepApplication": false,
    "restartPolicyType": "ON_FAILURE",
    "restartPolicyMaxRetries": 10
  }
}
```

2. Railway CLI ile deploy:
```bash
cd workers/analysis-worker
railway login
railway link  # ziraai-staging projesini seç
railway up    # Deploy
```

3. Environment variables ekle (Railway dashboard):
```
RABBITMQ_URL=${{RABBITMQ_URL}}  # Railway'den otomatik
REDIS_URL=${{REDIS_URL}}         # Railway'den otomatik
QUEUE_NAME=openai-analysis-queue
RESULT_QUEUE=analysis-results
CONCURRENCY=60
RATE_LIMIT=350
OPENAI_API_KEY=<your-key>
PROVIDER=openai
NODE_ENV=staging
```

**Test:**
```bash
railway logs  # Logs kontrol
railway status # Service durumu
```

#### Gün 15: End-to-End Test (Railway Staging)

**Adım 1.15: E2E Test Senaryosu**

1. Railway Staging'de tüm servislerin çalıştığını doğrula:
```bash
railway status --service webapi
railway status --service openai-worker
railway status --service plant-analysis-worker
```

2. Feature flag'i aç (Railway Staging environment variables):
```bash
# Railway dashboard üzerinden environment variable ekle:
FEATURE_USE_NEW_WORKER_SYSTEM=true

# Veya Railway CLI ile:
railway variables --set FEATURE_USE_NEW_WORKER_SYSTEM=true
```

3. appsettings.Staging.json'da feature flag oku:
```json
{
  "Features": {
    "UseNewWorkerSystem": "${FEATURE_USE_NEW_WORKER_SYSTEM:false}",
    "TrafficPercentage": 100
  }
}
```

4. Railway Staging WebAPI redeploy (environment variable değişikliği için):
```bash
railway up --service webapi
```

5. Postman ile Railway Staging API'ye test request:
```bash
POST https://ziraai-api-staging.up.railway.app/api/v1/plant-analyses/analyze-async
Authorization: Bearer <staging-jwt-token>
Content-Type: application/json

{
  "images": [
    {
      "url": "https://pub-xxxxx.r2.dev/leaf-top.jpg",
      "type": "leaf_top"
    },
    {
      "url": "https://pub-xxxxx.r2.dev/leaf-bottom.jpg",
      "type": "leaf_bottom"
    }
  ]
}
```

6. Railway logs takip et:
```bash
railway logs --service webapi
railway logs --service openai-worker
railway logs --service plant-analysis-worker
# WebAPI log
[INFO] Published to raw-analysis-queue: {analysisId}

# OpenAI Worker log
[INFO] Processing analysis: {analysisId}
[INFO] Calling OpenAI API
[INFO] OpenAI API completed: {duration}ms
[INFO] Analysis completed

# PlantAnalysisWorkerService log
[INFO] Consumed from analysis-results: {analysisId}
[INFO] Saved to database
```

7. Railway Staging PostgreSQL kontrolü:
```bash
# Railway CLI ile database bağlantısı
railway connect postgres

# SQL sorgusu
SELECT * FROM "PlantAnalyses"
WHERE "Id" = '{analysisId}'
ORDER BY "CreatedAt" DESC
LIMIT 1;
```

**Phase 1 Başarı Kriterleri (Railway Staging):**
- [ ] Railway'de 3 service deploy edildi (WebAPI, OpenAI Worker, PlantAnalysisWorker) ✅
- [ ] WebAPI → raw-analysis-queue publish ediyor ✅
- [ ] OpenAI worker queue'dan mesaj alıyor ✅
- [ ] OpenAI API çağrısı başarılı (Railway staging API key ile) ✅
- [ ] Result queue'ya publish ediliyor ✅
- [ ] PlantAnalysisWorkerService result'ı PostgreSQL'e kaydediyor ✅
- [ ] End-to-end flow < 90 saniye (Railway network latency dahil) ✅
- [ ] Error handling ve retry logic çalışıyor ✅
- [ ] Railway logs düzgün akıyor ✅
- [ ] CloudAMQP queues düzgün çalışıyor ✅

### 4.3 Phase 1 Çıktıları (Railway Staging)

- ✅ Railway Staging'de n8n'siz çalışan minimal sistem
- ✅ TypeScript worker infrastructure (Railway container)
- ✅ OpenAI provider integration
- ✅ Redis rate limiting (Railway Redis)
- ✅ Railway deployment pipeline kuruldu
- ✅ E2E flow Railway Staging'de doğrulandı
- ✅ ~50,000/gün kapasite (Railway Staging'de test edildi)
- ✅ CloudAMQP queues production-ready

---

## 5. Phase 2: Multi-Provider (Hafta 3-4)

**🚀 Deployment Ortamı:** Railway Staging
**📦 Yeni Services:** Dispatcher, Gemini Worker, Anthropic Worker
**🎯 Hedef:** 400K/gün kapasite

### 5.1 Amaç

Railway Staging'de 3 AI provider (OpenAI + Gemini + Anthropic) ile ~400,000/gün kapasite. Dispatcher servisi ve automatic failover mechanism.

### 5.2 Adımlar (Test Edilebilir)

#### Gün 16-18: Dispatcher Service

**Adım 2.1: Dispatcher Projesi**

```bash
mkdir -p workers/dispatcher/src/utils
cd workers/dispatcher
npm init -y
npm install --save typescript @types/node amqplib ioredis pino
npm install --save-dev ts-node nodemon
```

**Adım 2.2: Provider Selector Algorithm**

`workers/dispatcher/src/provider-selector.ts`:
```typescript
import { Redis } from 'ioredis';
import { logger } from './utils/logger';

export interface ProviderConfig {
  name: string;
  queue: string;
  rateLimit: number;
  priority: number; // 1 = highest (cheapest)
  costPerRequest: number;
}

export class ProviderSelector {
  private providers: ProviderConfig[] = [
    {
      name: 'openai',
      queue: 'openai-analysis-queue',
      rateLimit: 350,
      priority: 1,
      costPerRequest: 0.015
    },
    {
      name: 'gemini',
      queue: 'gemini-analysis-queue',
      rateLimit: 450,
      priority: 2,
      costPerRequest: 0.018
    },
    {
      name: 'anthropic',
      queue: 'claude-analysis-queue',
      rateLimit: 350,
      priority: 3,
      costPerRequest: 0.020
    }
  ];

  constructor(private redis: Redis) {}

  async selectProvider(): Promise<ProviderConfig | null> {
    const window = Math.floor(Date.now() / 60000);
    const candidates: Array<ProviderConfig & { score: number }> = [];

    for (const provider of this.providers) {
      // Health check
      const health = await this.redis.get(`health:${provider.name}`);
      if (health === 'down') {
        logger.warn({ provider: provider.name }, 'Provider is down, skipping');
        continue;
      }

      // Circuit breaker check
      const errors = parseInt(
        await this.redis.get(`errors:${provider.name}:${window}`) || '0'
      );
      if (errors >= 5) {
        logger.warn({ provider: provider.name, errors }, 'Circuit breaker open, skipping');
        continue;
      }

      // Rate limit check
      const usage = parseInt(
        await this.redis.get(`rate:${provider.name}:${window}`) || '0'
      );
      const remaining = provider.rateLimit - usage;

      if (remaining <= 0) {
        logger.warn({ provider: provider.name, usage }, 'Rate limit reached, skipping');
        continue;
      }

      // Calculate score (higher = better)
      // Factors: remaining capacity (70%), priority/cost (30%)
      const capacityScore = remaining / provider.rateLimit;
      const costScore = 1 / provider.priority;
      const score = (capacityScore * 0.7) + (costScore * 0.3);

      candidates.push({ ...provider, score });
    }

    if (candidates.length === 0) {
      logger.warn('No available providers');
      return null;
    }

    // Sort by score descending
    candidates.sort((a, b) => b.score - a.score);

    const selected = candidates[0];
    logger.info({
      provider: selected.name,
      score: selected.score,
      candidates: candidates.length
    }, 'Provider selected');

    return selected;
  }
}
```

**Adım 2.3: Dispatcher Main Logic**

`workers/dispatcher/src/dispatcher.ts`:
```typescript
import * as amqp from 'amqplib';
import { Redis } from 'ioredis';
import { ProviderSelector, ProviderConfig } from './provider-selector';
import { logger } from './utils/logger';
import { RawAnalysisMessage, ProviderQueueMessage } from '../../shared/types/messages';

export class Dispatcher {
  private connection: amqp.Connection;
  private channel: amqp.Channel;
  private redis: Redis;
  private selector: ProviderSelector;

  constructor(private config: DispatcherConfig) {
    this.redis = new Redis(config.redis.url);
    this.selector = new ProviderSelector(this.redis);
  }

  async start() {
    // Connect to RabbitMQ
    this.connection = await amqp.connect(this.config.rabbitmq.url);
    this.channel = await this.connection.createChannel();

    await this.channel.prefetch(this.config.prefetch);

    logger.info({
      rawQueue: this.config.rawQueue,
      prefetch: this.config.prefetch
    }, 'Dispatcher started');

    await this.channel.consume(
      this.config.rawQueue,
      async (msg) => {
        if (!msg) return;

        try {
          await this.processMessage(msg);
          this.channel.ack(msg);
        } catch (error) {
          logger.error({ error }, 'Dispatch failed');
          // Requeue with backoff
          await this.requeueWithBackoff(msg);
        }
      }
    );
  }

  private async processMessage(msg: amqp.Message) {
    const request: RawAnalysisMessage = JSON.parse(msg.content.toString());
    const retryCount = (request._retry_count || 0);

    logger.info({ analysisId: request.analysis_id, retryCount }, 'Dispatching analysis');

    // Select provider
    const provider = await this.selector.selectProvider();

    if (!provider) {
      if (retryCount >= this.config.maxRetries) {
        logger.error({ analysisId: request.analysis_id }, 'Max retries exceeded, sending to DLQ');
        await this.sendToDLQ(request);
        return;
      }

      // No available provider, requeue with backoff
      await this.requeueWithBackoff(msg);
      return;
    }

    // Publish to provider queue
    const providerMessage: ProviderQueueMessage = {
      ...request,
      _routing: {
        provider: provider.name,
        dispatched_at: Date.now(),
        rate_window: Math.floor(Date.now() / 60000)
      }
    };

    await this.channel.sendToQueue(
      provider.queue,
      Buffer.from(JSON.stringify(providerMessage)),
      { persistent: true }
    );

    // Increment rate counter
    const window = Math.floor(Date.now() / 60000);
    await this.redis.incr(`rate:${provider.name}:${window}`);
    await this.redis.expire(`rate:${provider.name}:${window}`, 120); // 2 min TTL

    logger.info({
      analysisId: request.analysis_id,
      provider: provider.name
    }, 'Dispatched to provider');
  }

  private async requeueWithBackoff(msg: amqp.Message) {
    const request: RawAnalysisMessage = JSON.parse(msg.content.toString());
    const retryCount = (request._retry_count || 0) + 1;

    // Exponential backoff: 1s, 2s, 4s, 8s, 16s, 32s (max)
    const delay = Math.min(
      this.config.retryDelay * Math.pow(2, retryCount - 1),
      32000
    );

    logger.warn({
      analysisId: request.analysis_id,
      retryCount,
      delay
    }, 'Requeueing with backoff');

    request._retry_count = retryCount;

    setTimeout(() => {
      this.channel.sendToQueue(
        this.config.rawQueue,
        Buffer.from(JSON.stringify(request)),
        { persistent: true }
      );
    }, delay);

    this.channel.ack(msg);
  }

  private async sendToDLQ(request: RawAnalysisMessage) {
    await this.channel.sendToQueue(
      this.config.dlqQueue,
      Buffer.from(JSON.stringify(request)),
      { persistent: true }
    );
  }
}
```

**Test:**
- Unit test: selectProvider() farklı senaryolar
- Integration test: Mesaj routing doğru çalışıyor mu?

#### Gün 19-21: Gemini Provider

**Adım 2.4: Gemini SDK Setup**

```bash
cd workers/analysis-worker
npm install --save @google/generative-ai
```

**Adım 2.5: Gemini Provider Implementation**

`workers/analysis-worker/src/providers/gemini.ts`:
```typescript
import { GoogleGenerativeAI, Part } from '@google/generative-ai';
import { AIProvider, AnalysisRequest, AnalysisResult } from './base';
import { logger } from '../utils/logger';

export class GeminiProvider implements AIProvider {
  private client: GoogleGenerativeAI;

  constructor(apiKey: string) {
    this.client = new GoogleGenerativeAI(apiKey);
  }

  async analyze(request: AnalysisRequest): Promise<AnalysisResult> {
    const startTime = Date.now();

    try {
      const model = this.client.getGenerativeModel({
        model: 'gemini-2.0-flash-exp'
      });

      const parts: Part[] = [
        { text: this.buildPrompt(request) },
        ...await this.buildImageParts(request)
      ];

      logger.info({ analysisId: request.analysis_id }, 'Calling Gemini API');

      const result = await model.generateContent(parts);
      const response = await result.response;
      const text = response.text();

      const duration = Date.now() - startTime;
      logger.info({
        analysisId: request.analysis_id,
        duration
      }, 'Gemini API completed');

      return JSON.parse(text);
    } catch (error) {
      logger.error({
        analysisId: request.analysis_id,
        error
      }, 'Gemini API failed');
      throw error;
    }
  }

  private buildPrompt(request: AnalysisRequest): string {
    // Aynı prompt (OpenAI ile)
    return `You are an expert agricultural analyst...`;
  }

  private async buildImageParts(request: AnalysisRequest): Promise<Part[]> {
    const parts: Part[] = [];

    if (request.leaf_top_url) {
      parts.push({
        inlineData: {
          mimeType: 'image/jpeg',
          data: await this.fetchImageAsBase64(request.leaf_top_url)
        }
      });
    }

    // ... diğer resimler

    return parts;
  }

  private async fetchImageAsBase64(url: string): Promise<string> {
    const response = await fetch(url);
    const buffer = await response.arrayBuffer();
    return Buffer.from(buffer).toString('base64');
  }

  getProviderName(): string {
    return 'gemini';
  }
}
```

**Test:**
- Unit test: Mock Gemini API
- Integration test: Gerçek Gemini API
- Comparison test: OpenAI vs Gemini sonuçları tutarlı mı?

#### Gün 22-24: Anthropic Provider

**Adım 2.6: Anthropic SDK Setup**

```bash
npm install --save @anthropic-ai/sdk
```

**Adım 2.7: Anthropic Provider Implementation**

`workers/analysis-worker/src/providers/anthropic.ts`:
```typescript
import Anthropic from '@anthropic-ai/sdk';
import { AIProvider, AnalysisRequest, AnalysisResult } from './base';
import { logger } from '../utils/logger';

export class AnthropicProvider implements AIProvider {
  private client: Anthropic;

  constructor(apiKey: string) {
    this.client = new Anthropic({ apiKey });
  }

  async analyze(request: AnalysisRequest): Promise<AnalysisResult> {
    const startTime = Date.now();

    try {
      const content = [
        ...await this.buildImageContents(request),
        {
          type: 'text' as const,
          text: this.buildPrompt(request)
        }
      ];

      logger.info({ analysisId: request.analysis_id }, 'Calling Anthropic API');

      const message = await this.client.messages.create({
        model: 'claude-sonnet-4-20250514',
        max_tokens: 5000,
        messages: [{
          role: 'user',
          content
        }]
      });

      const duration = Date.now() - startTime;
      logger.info({
        analysisId: request.analysis_id,
        duration,
        usage: message.usage
      }, 'Anthropic API completed');

      const text = message.content[0].type === 'text'
        ? message.content[0].text
        : '';

      return JSON.parse(text);
    } catch (error) {
      logger.error({
        analysisId: request.analysis_id,
        error
      }, 'Anthropic API failed');
      throw error;
    }
  }

  private buildPrompt(request: AnalysisRequest): string {
    // Aynı prompt
    return `You are an expert agricultural analyst...`;
  }

  private async buildImageContents(request: AnalysisRequest) {
    const images = [];

    if (request.leaf_top_url) {
      images.push({
        type: 'image' as const,
        source: {
          type: 'url' as const,
          url: request.leaf_top_url
        }
      });
    }

    // ... diğer resimler

    return images;
  }

  getProviderName(): string {
    return 'anthropic';
  }
}
```

**Test:**
- Unit test: Mock Anthropic API
- Integration test: Gerçek Anthropic API
- Comparison test: 3 provider sonuçları tutarlı mı?

#### Gün 25: Provider Factory ve Worker Güncelleme

**Adım 2.8: Provider Factory**

`workers/analysis-worker/src/providers/index.ts`:
```typescript
import { AIProvider } from './base';
import { OpenAIProvider } from './openai';
import { GeminiProvider } from './gemini';
import { AnthropicProvider } from './anthropic';

export function createProvider(
  providerName: string,
  config: ProviderConfig
): AIProvider {
  switch (providerName) {
    case 'openai':
      return new OpenAIProvider(config.openai.apiKey);
    case 'gemini':
      return new GeminiProvider(config.gemini.apiKey);
    case 'anthropic':
      return new AnthropicProvider(config.anthropic.apiKey);
    default:
      throw new Error(`Unknown provider: ${providerName}`);
  }
}
```

**Adım 2.9: Worker Config Update**

`workers/analysis-worker/src/index.ts`:
```typescript
const config = {
  provider: process.env.PROVIDER!, // "openai" | "gemini" | "anthropic"
  // ... diğer config
  openai: {
    apiKey: process.env.OPENAI_API_KEY!,
  },
  gemini: {
    apiKey: process.env.GEMINI_API_KEY!,
  },
  anthropic: {
    apiKey: process.env.ANTHROPIC_API_KEY!,
  }
};
```

#### Gün 26-28: Circuit Breaker ve Integration

**Adım 2.10: Circuit Breaker Implementation**

`workers/shared/utils/circuit-breaker.ts`:
```typescript
import { Redis } from 'ioredis';

export class CircuitBreaker {
  private provider: string;
  private redis: Redis;
  private threshold: number = 5;
  private resetTimeout: number = 60000; // 1 minute

  constructor(provider: string, redis: Redis) {
    this.provider = provider;
    this.redis = redis;
  }

  async recordSuccess() {
    await this.redis.set(`health:${this.provider}`, 'ok');
  }

  async recordFailure() {
    const window = Math.floor(Date.now() / 60000);
    const key = `errors:${this.provider}:${window}`;

    const errors = await this.redis.incr(key);
    await this.redis.expire(key, 120); // 2 min TTL

    if (errors >= this.threshold) {
      await this.redis.set(`health:${this.provider}`, 'down');

      // Auto-reset after timeout
      setTimeout(async () => {
        await this.redis.set(`health:${this.provider}`, 'ok');
      }, this.resetTimeout);
    }
  }
}
```

**Adım 2.11: Worker Integration**

`workers/analysis-worker/src/worker.ts` güncelle:
```typescript
private circuitBreaker: CircuitBreaker;

constructor(config: WorkerConfig) {
  // ... existing code
  this.circuitBreaker = new CircuitBreaker(
    this.provider.getProviderName(),
    this.redis
  );
}

private async processMessage(msg: amqp.Message) {
  try {
    // ... existing analysis code

    await this.circuitBreaker.recordSuccess();
  } catch (error) {
    await this.circuitBreaker.recordFailure();
    throw error;
  }
}
```

**Adım 2.12: Railway Multi-Worker Deployment**

Railway'de çoklu worker servisleri deploy etme:
```yaml
services:
  # ... existing services

  dispatcher:
    build:
      context: .
      dockerfile: workers/dispatcher/Dockerfile
    environment:
      - RABBITMQ_URL=amqp://guest:guest@rabbitmq:5672
      - REDIS_URL=redis://redis:6379
      - RAW_QUEUE=raw-analysis-queue
      - PREFETCH=100
    depends_on:
      - rabbitmq
      - redis

  openai-worker:
    # ... existing config

  gemini-worker:
    build:
      context: .
      dockerfile: workers/analysis-worker/Dockerfile
    environment:
      - PROVIDER=gemini
      - QUEUE_NAME=gemini-analysis-queue
      - CONCURRENCY=70
      - RATE_LIMIT=450
      - GEMINI_API_KEY=${GEMINI_API_KEY}
      # ... other env vars

  anthropic-worker:
    build:
      context: .
      dockerfile: workers/analysis-worker/Dockerfile
    environment:
      - PROVIDER=anthropic
      - QUEUE_NAME=claude-analysis-queue
      - CONCURRENCY=60
      - RATE_LIMIT=350
      - ANTHROPIC_API_KEY=${ANTHROPIC_API_KEY}
      # ... other env vars
```

#### Gün 29-30: Integration Testing

**Adım 2.13: Multi-Provider Test**

Test senaryoları:
1. Normal operation: 3 provider'a eşit dağılım
2. Provider down: Traffic diğer 2'ye kayıyor
3. Rate limit: Bir provider full olunca diğeri kullanılıyor
4. Circuit breaker: 5 error'dan sonra provider bypass
5. Recovery: Provider healthy olunca tekrar kullanılıyor

**Test Script:**
```typescript
// tests/integration/multi-provider.test.ts

describe('Multi-Provider Integration', () => {
  it('should distribute load across all providers', async () => {
    // Send 300 requests
    for (let i = 0; i < 300; i++) {
      await publishAnalysisRequest();
    }

    // Wait for processing
    await waitForCompletion();

    // Check distribution
    const openaiCount = await getProviderUsage('openai');
    const geminiCount = await getProviderUsage('gemini');
    const anthropicCount = await getProviderUsage('anthropic');

    // Should be roughly equal (±20%)
    expect(openaiCount).toBeGreaterThan(80);
    expect(geminiCount).toBeGreaterThan(80);
    expect(anthropicCount).toBeGreaterThan(80);
  });

  it('should failover when provider is down', async () => {
    // Simulate OpenAI down
    await redis.set('health:openai', 'down');

    // Send 100 requests
    for (let i = 0; i < 100; i++) {
      await publishAnalysisRequest();
    }

    await waitForCompletion();

    // OpenAI should receive 0
    const openaiCount = await getProviderUsage('openai');
    expect(openaiCount).toBe(0);

    // Others should handle all traffic
    const geminiCount = await getProviderUsage('gemini');
    const anthropicCount = await getProviderUsage('anthropic');
    expect(geminiCount + anthropicCount).toBe(100);
  });
});
```

### 5.3 Phase 2 Başarı Kriterleri

- [ ] Dispatcher 3 provider'a routing yapıyor ✅
- [ ] Gemini worker çalışıyor ✅
- [ ] Anthropic worker çalışıyor ✅
- [ ] Rate limiting 3 provider için çalışıyor ✅
- [ ] Circuit breaker tetikleniyor ✅
- [ ] Failover otomatik çalışıyor ✅
- [ ] 100 concurrent analysis başarılı ✅
- [ ] ~400,000/gün kapasite estimate ✅

### 5.4 Phase 2 Çıktıları

- ✅ 3 AI provider integration
- ✅ Dispatcher service
- ✅ Circuit breaker pattern
- ✅ Automatic failover
- ✅ ~400,000/gün kapasite

---

## 6. Phase 3: Admin Panel ve Scale (Hafta 5-6)

### 6.1 Amaç

Real-time monitoring dashboard ve manuel scale control. Cost tracking ve alerting.

### 6.2 Adımlar (Test Edilebilir)

#### Gün 31-33: Admin Panel Foundation

**Adım 3.1: Authentication Setup**

`admin-panel/src/middleware.ts`:
```typescript
import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

export function middleware(request: NextRequest) {
  const token = request.cookies.get('admin-token');

  if (!token && !request.nextUrl.pathname.startsWith('/login')) {
    return NextResponse.redirect(new URL('/login', request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/((?!api|_next/static|_next/image|favicon.ico|login).*)'],
};
```

**Adım 3.2: Login Page**

`admin-panel/src/app/login/page.tsx`:
```typescript
'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';

export default function LoginPage() {
  const [credentials, setCredentials] = useState({ username: '', password: '' });
  const router = useRouter();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();

    const res = await fetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(credentials)
    });

    if (res.ok) {
      router.push('/dashboard');
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center">
      <form onSubmit={handleLogin} className="w-96 space-y-4">
        <h1 className="text-2xl font-bold">ZiraAI Admin Panel</h1>
        <input
          type="text"
          placeholder="Username"
          value={credentials.username}
          onChange={(e) => setCredentials({ ...credentials, username: e.target.value })}
          className="w-full border p-2"
        />
        <input
          type="password"
          placeholder="Password"
          value={credentials.password}
          onChange={(e) => setCredentials({ ...credentials, password: e.target.value })}
          className="w-full border p-2"
        />
        <button type="submit" className="w-full bg-blue-600 text-white p-2">
          Login
        </button>
      </form>
    </div>
  );
}
```

**Test:**
- [ ] Login page render ediliyor ✅
- [ ] Authentication çalışıyor ✅
- [ ] Protected routes redirect ediliyor ✅

#### Gün 34-36: Dashboard UI

**Adım 3.3: Dashboard Layout**

`admin-panel/src/app/dashboard/page.tsx`:
```typescript
'use client';

import { useEffect, useState } from 'react';
import { SummaryCards } from '@/components/dashboard/summary-cards';
import { ProviderTable } from '@/components/dashboard/provider-table';
import { ThroughputChart } from '@/components/charts/throughput-chart';
import { QueueDepthChart } from '@/components/charts/queue-depth-chart';

export default function DashboardPage() {
  const [data, setData] = useState<DashboardData | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      const res = await fetch('/api/status');
      const data = await res.json();
      setData(data);
    };

    // Initial fetch
    fetchData();

    // Poll every 5 seconds
    const interval = setInterval(fetchData, 5000);

    return () => clearInterval(interval);
  }, []);

  if (!data) return <div>Loading...</div>;

  return (
    <div className="p-8 space-y-8">
      <h1 className="text-3xl font-bold">ZiraAI Platform Dashboard</h1>

      <SummaryCards data={data.summary} />

      <ProviderTable providers={data.providers} />

      <div className="grid grid-cols-2 gap-8">
        <ThroughputChart data={data.historical.throughput} />
        <QueueDepthChart data={data.historical.queueDepth} />
      </div>
    </div>
  );
}
```

**Adım 3.4: Summary Cards Component**

`admin-panel/src/components/dashboard/summary-cards.tsx`:
```typescript
interface SummaryCardsProps {
  data: {
    totalThroughput: number;
    totalWorkers: number;
    queueDepth: number;
    dailyCapacity: number;
  };
}

export function SummaryCards({ data }: SummaryCardsProps) {
  return (
    <div className="grid grid-cols-4 gap-4">
      <Card>
        <CardHeader>
          <CardTitle>Throughput</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-3xl font-bold">{data.totalThroughput}/min</p>
          <p className="text-sm text-gray-500">Current rate</p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Active Workers</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-3xl font-bold">{data.totalWorkers}</p>
          <p className="text-sm text-gray-500">Across all providers</p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Queue Depth</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-3xl font-bold">{data.queueDepth}</p>
          <p className="text-sm text-gray-500">Waiting messages</p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Daily Capacity</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-3xl font-bold">{(data.dailyCapacity / 1000).toFixed(0)}K</p>
          <p className="text-sm text-gray-500">Estimated max</p>
        </CardContent>
      </Card>
    </div>
  );
}
```

**Adım 3.5: Provider Table Component**

`admin-panel/src/components/dashboard/provider-table.tsx`:
```typescript
export function ProviderTable({ providers }: ProviderTableProps) {
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Provider</TableHead>
          <TableHead>Health</TableHead>
          <TableHead>Workers</TableHead>
          <TableHead>Throughput</TableHead>
          <TableHead>Rate Usage</TableHead>
          <TableHead>Queue Depth</TableHead>
          <TableHead>Error Rate</TableHead>
          <TableHead>Avg Latency</TableHead>
          <TableHead>Cost Today</TableHead>
          <TableHead>Actions</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {providers.map(provider => (
          <TableRow key={provider.name}>
            <TableCell className="font-medium">{provider.name}</TableCell>
            <TableCell>
              <Badge variant={provider.health === 'ok' ? 'success' : 'destructive'}>
                {provider.health}
              </Badge>
            </TableCell>
            <TableCell>{provider.activeWorkers}</TableCell>
            <TableCell>{provider.throughput}/min</TableCell>
            <TableCell>
              <Progress
                value={(provider.rateUsage / provider.rateLimit) * 100}
              />
              <span className="text-xs">
                {provider.rateUsage}/{provider.rateLimit}
              </span>
            </TableCell>
            <TableCell>{provider.queueDepth}</TableCell>
            <TableCell>{(provider.errorRate * 100).toFixed(2)}%</TableCell>
            <TableCell>{provider.avgLatency}ms</TableCell>
            <TableCell>${provider.costToday.toFixed(2)}</TableCell>
            <TableCell>
              <Button size="sm" onClick={() => handleScale(provider.name)}>
                Scale
              </Button>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
```

**Test:**
- [ ] Dashboard render ediliyor ✅
- [ ] Real-time updates çalışıyor ✅
- [ ] Charts görüntüleniyor ✅

#### Gün 37-39: Scale Control ve Admin API

**Adım 3.6: Scale Control Modal**

`admin-panel/src/components/scale-control/scale-modal.tsx`:
```typescript
export function ScaleModal({ provider, onClose }: ScaleModalProps) {
  const [targetCount, setTargetCount] = useState(5);
  const [loading, setLoading] = useState(false);

  const handleScale = async () => {
    setLoading(true);

    try {
      const res = await fetch('/api/scale', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ provider, targetCount })
      });

      if (res.ok) {
        toast.success(`Scaled ${provider} to ${targetCount} workers`);
        onClose();
      }
    } catch (error) {
      toast.error('Scale operation failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Dialog open onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Scale {provider} Workers</DialogTitle>
        </DialogHeader>

        <div className="space-y-4">
          <div>
            <Label>Target Worker Count</Label>
            <Slider
              value={[targetCount]}
              onValueChange={([value]) => setTargetCount(value)}
              min={1}
              max={10}
              step={1}
            />
            <p className="text-sm text-gray-500">{targetCount} workers</p>
          </div>

          <div>
            <Label>Presets</Label>
            <div className="flex gap-2">
              <Button variant="outline" onClick={() => setTargetCount(1)}>
                Minimum (1)
              </Button>
              <Button variant="outline" onClick={() => setTargetCount(2)}>
                Low (2)
              </Button>
              <Button variant="outline" onClick={() => setTargetCount(5)}>
                High (5)
              </Button>
              <Button variant="outline" onClick={() => setTargetCount(7)}>
                Maximum (7)
              </Button>
            </div>
          </div>

          <div className="border-t pt-4">
            <p className="text-sm">
              Estimated capacity: <strong>{targetCount * 70 * 60 * 24} analyses/day</strong>
            </p>
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button onClick={handleScale} disabled={loading}>
            {loading ? 'Scaling...' : 'Scale'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
```

**Adım 3.7: Admin API - Status Endpoint**

`admin-panel/src/app/api/status/route.ts`:
```typescript
import { Redis } from 'ioredis';
import { createClient, Channel } from 'amqplib';

const redis = new Redis(process.env.REDIS_URL!);

export async function GET() {
  try {
    // Get current window
    const window = Math.floor(Date.now() / 60000);

    // Fetch provider data
    const providers = await Promise.all([
      getProviderStatus('openai', window),
      getProviderStatus('gemini', window),
      getProviderStatus('anthropic', window)
    ]);

    // Calculate summary
    const summary = {
      totalThroughput: providers.reduce((sum, p) => sum + p.throughput, 0),
      totalWorkers: providers.reduce((sum, p) => sum + p.activeWorkers, 0),
      queueDepth: providers.reduce((sum, p) => sum + p.queueDepth, 0),
      dailyCapacity: providers.reduce((sum, p) =>
        sum + (p.activeWorkers * 60 * 60 * 24), 0
      )
    };

    // Fetch historical data (last 24h)
    const historical = await getHistoricalData();

    return Response.json({
      summary,
      providers,
      historical
    });
  } catch (error) {
    return Response.json({ error: 'Failed to fetch status' }, { status: 500 });
  }
}

async function getProviderStatus(provider: string, window: number) {
  const [health, rateUsage, errors, workers, queueDepth] = await Promise.all([
    redis.get(`health:${provider}`),
    redis.get(`rate:${provider}:${window}`),
    redis.get(`errors:${provider}:${window}`),
    redis.get(`scale:${provider}`),
    getQueueDepth(`${provider}-analysis-queue`)
  ]);

  return {
    name: provider,
    health: (health || 'ok') as 'ok' | 'degraded' | 'down',
    activeWorkers: parseInt(workers || '5'),
    throughput: parseInt(rateUsage || '0'),
    rateUsage: parseInt(rateUsage || '0'),
    rateLimit: provider === 'gemini' ? 450 : 350,
    queueDepth: queueDepth,
    errorRate: parseInt(errors || '0') / 100,
    avgLatency: 45000, // TODO: Calculate from metrics
    costToday: 0 // TODO: Calculate from DB
  };
}

async function getQueueDepth(queueName: string): Promise<number> {
  const connection = await createClient(process.env.RABBITMQ_URL!);
  const channel = await connection.createChannel();
  const { messageCount } = await channel.checkQueue(queueName);
  await channel.close();
  await connection.close();
  return messageCount;
}
```

**Adım 3.8: Admin API - Scale Endpoint**

`admin-panel/src/app/api/scale/route.ts`:
```typescript
import { exec } from 'child_process';
import { promisify } from 'util';

const execAsync = promisify(exec);

export async function POST(request: Request) {
  const { provider, targetCount } = await request.json();

  if (!['openai', 'gemini', 'anthropic'].includes(provider)) {
    return Response.json({ error: 'Invalid provider' }, { status: 400 });
  }

  if (targetCount < 1 || targetCount > 10) {
    return Response.json({ error: 'Invalid target count' }, { status: 400 });
  }

  try {
    // Update Redis config
    await redis.set(`scale:${provider}`, targetCount);

    // Call scale script
    const { stdout, stderr } = await execAsync(
      `bash ./scripts/scale.sh ${provider} ${targetCount}`
    );

    return Response.json({
      success: true,
      provider,
      targetCount,
      output: stdout
    });
  } catch (error) {
    return Response.json({
      error: 'Scale operation failed',
      details: error.message
    }, { status: 500 });
  }
}
```

**Test:**
- [ ] Scale modal açılıyor ✅
- [ ] Slider çalışıyor ✅
- [ ] Scale API çağrısı başarılı ✅
- [ ] Worker count güncelleniy or ✅

#### Gün 40-42: Scripts ve Alert System

**Adım 3.9: Scale Script**

`scripts/scale.sh`:
```bash
#!/bin/bash

PROVIDER=$1
TARGET_COUNT=$2

# Get current worker count
CURRENT_COUNT=$(railway service list | grep "${PROVIDER}-worker" | wc -l)

echo "Current ${PROVIDER} workers: ${CURRENT_COUNT}"
echo "Target ${PROVIDER} workers: ${TARGET_COUNT}"

if [ "$TARGET_COUNT" -gt "$CURRENT_COUNT" ]; then
  # Scale up
  DIFF=$((TARGET_COUNT - CURRENT_COUNT))
  echo "Scaling up by ${DIFF} workers..."

  for i in $(seq 1 $DIFF); do
    WORKER_NUM=$((CURRENT_COUNT + i))
    WORKER_ID=$(printf "%03d" $WORKER_NUM)

    echo "Creating ${PROVIDER}-worker-${WORKER_ID}..."

    railway service create \
      --name "${PROVIDER}-worker-${WORKER_ID}" \
      --source ./workers/analysis-worker \
      --env PROVIDER=${PROVIDER} \
      --env CONCURRENCY=60 \
      --env QUEUE_NAME="${PROVIDER}-analysis-queue" \
      --env RESULT_QUEUE="analysis-results"
  done

elif [ "$TARGET_COUNT" -lt "$CURRENT_COUNT" ]; then
  # Scale down
  DIFF=$((CURRENT_COUNT - TARGET_COUNT))
  echo "Scaling down by ${DIFF} workers..."

  for i in $(seq 1 $DIFF); do
    WORKER_NUM=$((CURRENT_COUNT - i + 1))
    WORKER_ID=$(printf "%03d" $WORKER_NUM)

    echo "Deleting ${PROVIDER}-worker-${WORKER_ID}..."

    railway service delete "${PROVIDER}-worker-${WORKER_ID}" --yes
  done

else
  echo "No scaling needed"
fi

echo "Scale operation completed"
```

**Adım 3.10: Alert System Database**

```sql
CREATE TABLE "Alerts" (
    "Id" SERIAL PRIMARY KEY,
    "Timestamp" TIMESTAMP NOT NULL DEFAULT NOW(),
    "AlertType" VARCHAR(50) NOT NULL,
    "Severity" VARCHAR(20) NOT NULL, -- warning | critical
    "Provider" VARCHAR(20),
    "Message" TEXT NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'active', -- active | acknowledged | resolved
    "AcknowledgedAt" TIMESTAMP,
    "AcknowledgedBy" VARCHAR(100),
    "ResolvedAt" TIMESTAMP
);

CREATE INDEX "IX_Alerts_Status_Timestamp"
ON "Alerts" ("Status", "Timestamp" DESC);
```

**Adım 3.11: Alert Checker (Background Job)**

`workers/alert-checker/src/index.ts`:
```typescript
import { Redis } from 'ioredis';
import { Pool } from 'pg';

const redis = new Redis(process.env.REDIS_URL!);
const db = new Pool({ connectionString: process.env.DATABASE_URL! });

const alertRules = [
  {
    name: 'HighQueueDepth',
    check: async () => {
      const depth = await getTotalQueueDepth();
      return depth > 500;
    },
    severity: 'warning',
    message: 'Queue depth is high (>500). Consider scaling up workers.'
  },
  {
    name: 'ProviderDown',
    check: async () => {
      const downProviders = [];
      for (const provider of ['openai', 'gemini', 'anthropic']) {
        const health = await redis.get(`health:${provider}`);
        if (health === 'down') {
          downProviders.push(provider);
        }
      }
      return downProviders.length > 0 ? downProviders : null;
    },
    severity: 'critical',
    message: (providers: string[]) => `Provider(s) down: ${providers.join(', ')}`
  },
  // ... more rules
];

async function checkAlerts() {
  for (const rule of alertRules) {
    const result = await rule.check();

    if (result) {
      await createAlert({
        alertType: rule.name,
        severity: rule.severity,
        message: typeof rule.message === 'function'
          ? rule.message(result)
          : rule.message
      });
    }
  }
}

setInterval(checkAlerts, 60000); // Every minute
```

**Test:**
- [ ] scale.sh script çalışıyor ✅
- [ ] Alert trigger ediliyor ✅
- [ ] Alert database'e kaydediliyor ✅

### 6.3 Phase 3 Başarı Kriterleri

- [ ] Admin panel fully functional ✅
- [ ] Dashboard real-time metrics ✅
- [ ] Scale API worker count değiştiriyor ✅
- [ ] Scripts çalışıyor ✅
- [ ] Alert sistemi tetikleniyor ✅
- [ ] Cost tracking doğru ✅

### 6.4 Phase 3 Çıktıları

- ✅ Next.js Admin Panel
- ✅ Real-time monitoring dashboard
- ✅ Manual scale control
- ✅ Alert system
- ✅ Cost tracking

---

## 7. Phase 4: Production Hardening (Hafta 7-8)

### 7.1 Amaç

Production-ready sistem. Load testing, security audit, documentation, gradual rollout.

### 7.2 Adımlar (Test Edilebilir)

#### Gün 43-45: Load Testing

**Adım 4.1: k6 Setup**

```bash
# Install k6
brew install k6  # macOS
# or
wget https://github.com/grafana/k6/releases/download/v0.45.0/k6-v0.45.0-linux-amd64.tar.gz
```

**Adım 4.2: Load Test Script**

`tests/performance/target-load.js`:
```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '5m', target: 100 },   // Warm-up: 0 → 100 VUs
    { duration: '10m', target: 694 },  // Ramp-up: 100 → 694 VUs (target)
    { duration: '60m', target: 694 },  // Sustained: 694 VUs for 1 hour
    { duration: '5m', target: 0 },     // Cool-down: 694 → 0 VUs
  ],
  thresholds: {
    'http_req_duration{type:analysis}': ['p(95)<90000'], // 95% < 90s
    'http_req_failed{type:analysis}': ['rate<0.05'],     // Error rate < 5%
    'checks{check:queued}': ['rate>0.95'],               // Success rate > 95%
  },
};

const BASE_URL = __ENV.BASE_URL || 'https://api.ziraai.com';

export default function () {
  const payload = JSON.stringify({
    LeafTopUrl: 'https://example.com/leaf-top.jpg',
    LeafBottomUrl: 'https://example.com/leaf-bottom.jpg',
    PlantOverviewUrl: 'https://example.com/plant.jpg',
    FarmerId: `farmer-${__VU}-${__ITER}`,
    CropType: 'Tomato',
    Location: 'Antalya, Turkey'
  });

  const res = http.post(
    `${BASE_URL}/api/v1/plant-analyses/analyze-async`,
    payload,
    {
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${__ENV.API_TOKEN}`
      },
      tags: { type: 'analysis' }
    }
  );

  check(res, {
    'status is 200 or 202': (r) => [200, 202].includes(r.status),
    'has analysis_id': (r) => {
      try {
        const body = JSON.parse(r.body);
        return body.analysisId !== undefined;
      } catch {
        return false;
      }
    }
  }, { check: 'queued' });

  sleep(1);
}
```

**Adım 4.3: Run Load Test**

```bash
# Set environment variables
export BASE_URL=https://api.ziraai.com
export API_TOKEN=your_token_here

# Run test
k6 run tests/performance/target-load.js \
  --out influxdb=http://localhost:8086/k6

# Results will show:
# - Requests per second
# - p95 latency
# - Error rate
# - Success rate
```

**Test Başarı Kriterleri:**
- [ ] 694 req/min sustained throughput ✅
- [ ] p95 latency < 90 saniye ✅
- [ ] Error rate < 5% ✅
- [ ] Success rate > 95% ✅
- [ ] No memory leaks ✅
- [ ] Queue depth stable ✅

#### Gün 46-47: Performance Optimization

**Adım 4.4: Worker Concurrency Tuning**

Test matrix:
| Concurrency | Throughput | Memory | CPU | Latency |
|-------------|------------|--------|-----|---------|
| 30 | Low | Low | Low | OK |
| 50 | Medium | Medium | Medium | OK |
| 60 | High | High | High | OK |
| 70 | High | Very High | Very High | Degraded |

**Optimal:** 60 concurrent per worker

**Adım 4.5: RabbitMQ Prefetch Optimization**

Test matrix:
| Prefetch | Throughput | Memory | Latency |
|----------|------------|--------|---------|
| 1 | Very Low | Low | OK |
| 10 | Low | Low | OK |
| 50 | Medium | Medium | OK |
| 100 | High | High | OK |
| 200 | High | Very High | Degraded |

**Optimal:** 100 prefetch

**Adım 4.6: Connection Pooling**

Redis:
```typescript
const redis = new Redis(config.redis.url, {
  maxRetriesPerRequest: 3,
  enableReadyCheck: true,
  connectionName: 'worker-pool',
  lazyConnect: false,
  keepAlive: 30000
});
```

PostgreSQL:
```csharp
services.AddDbContext<ProjectDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions => {
        npgsqlOptions.MaxBatchSize(100);
        npgsqlOptions.CommandTimeout(30);
        npgsqlOptions.EnableRetryOnFailure(3);
    })
);
```

**Test:**
- [ ] Concurrency optimal ✅
- [ ] Prefetch optimal ✅
- [ ] Connection pools tuned ✅
- [ ] No performance regression ✅

#### Gün 48: Security Audit

**Adım 4.7: Security Checklist**

```markdown
## API Keys
- [ ] No hardcoded secrets in code
- [ ] All keys in environment variables
- [ ] Production keys different from dev
- [ ] Key rotation procedure documented

## Authentication
- [ ] Admin panel requires login
- [ ] JWT tokens validated
- [ ] Session timeout configured
- [ ] Password complexity enforced

## API Rate Limiting
- [ ] Client-based rate limiting active
- [ ] 429 responses for rate limit
- [ ] Retry-After header set
- [ ] DDoS protection configured

## Input Validation
- [ ] Request schema validation
- [ ] SQL injection prevention
- [ ] XSS prevention
- [ ] File upload validation

## HTTPS
- [ ] SSL certificate valid
- [ ] HTTP → HTTPS redirect
- [ ] HSTS header set
- [ ] TLS 1.2+ only

## CORS
- [ ] Allowed origins configured
- [ ] Credentials handling secure
- [ ] Preflight requests handled

## Logging
- [ ] No sensitive data in logs
- [ ] Audit logging enabled
- [ ] Log retention configured
```

**Test:**
- [ ] Security audit checklist complete ✅
- [ ] No critical vulnerabilities ✅
- [ ] Penetration test passed ✅

#### Gün 49: Documentation

**Adım 4.8: Documentation Checklist**

```markdown
## Architecture Documentation
- [x] System overview diagram
- [x] Component descriptions
- [x] Data flow diagrams
- [x] Technology stack

## API Documentation
- [ ] OpenAPI spec generated
- [ ] Request/response examples
- [ ] Error codes documented
- [ ] Authentication guide

## Deployment Guide
- [ ] Railway setup instructions
- [ ] Environment variables list
- [ ] Service configuration
- [ ] First deployment steps

## Operations Runbook
- [ ] Common operations
- [ ] Troubleshooting steps
- [ ] Emergency procedures
- [ ] Contact information

## Developer Onboarding
- [ ] Local development setup
- [ ] Code structure explanation
- [ ] Contribution guidelines
- [ ] Testing procedures
```

#### Gün 50: Gradual Rollout

**Adım 4.9: Feature Flag Toggle**

```csharp
// appsettings.Production.json
{
  "Features": {
    "UseNewWorkerSystem": false,  // Start with false
    "NewSystemTrafficPercentage": 0  // Start with 0%
  }
}
```

**Adım 4.10: Canary Deployment**

Phased rollout:

**Day 1: 10% Traffic**
```csharp
public async Task<IResult> PublishAnalysisRequest(PlantAnalysisRequest request)
{
    var useNewSystem = _configuration.GetValue<bool>("Features:UseNewWorkerSystem");
    var percentage = _configuration.GetValue<int>("Features:NewSystemTrafficPercentage");

    // Random routing based on percentage
    var random = new Random().Next(100);

    if (useNewSystem && random < percentage)
    {
        return await PublishToRawQueue(request); // New system
    }
    else
    {
        return await PublishToN8nQueue(request); // Old system
    }
}
```

Update config: `"NewSystemTrafficPercentage": 10`

Monitor for 24 hours:
- Error rate
- Latency
- Throughput
- Cost

**Day 2-3: 50% Traffic**

If Day 1 successful, update: `"NewSystemTrafficPercentage": 50`

Monitor for 48 hours.

**Day 4-5: 100% Traffic**

If Day 2-3 successful:
```json
{
  "Features": {
    "UseNewWorkerSystem": true,
    "NewSystemTrafficPercentage": 100
  }
}
```

**Day 6: n8n Decommission**

- Stop n8n service
- Remove n8n configuration
- Archive n8n workflows

**Rollback Procedure:**

If issues detected:
1. Set `"NewSystemTrafficPercentage": 0` (immediate)
2. Restart n8n service
3. Investigate issues
4. Fix and retry

**Test:**
- [ ] Feature flag çalışıyor ✅
- [ ] 10% canary successful ✅
- [ ] 50% canary successful ✅
- [ ] 100% rollout successful ✅
- [ ] Rollback procedure verified ✅

### 7.3 Phase 4 Başarı Kriterleri

- [ ] Load test 694 req/min passed ✅
- [ ] p95 latency < 90s ✅
- [ ] Error rate < 5% ✅
- [ ] 24h endurance test passed ✅
- [ ] Security audit passed ✅
- [ ] Documentation complete ✅
- [ ] Canary deployment successful ✅
- [ ] Production monitoring active ✅

### 7.4 Phase 4 Çıktıları

- ✅ Production-ready system
- ✅ 1M/gün kapasite doğrulanmış
- ✅ Comprehensive documentation
- ✅ Deployment automation
- ✅ Monitoring ve alerting active

---

## 8. Test Stratejisi

### 8.1 Unit Tests

**Coverage Target:** >80%

**Key Areas:**
- Provider implementations
- Rate limiter logic
- Provider selector algorithm
- Circuit breaker
- Retry mechanism

**Test Framework:** Jest

```bash
# Run unit tests
npm test

# With coverage
npm test -- --coverage
```

### 8.2 Integration Tests

**Key Scenarios:**
- End-to-end flow (Client → API → Worker → DB)
- Multi-provider distribution
- Failover mechanism
- Rate limit enforcement
- Circuit breaker activation

**Test Environment:** Docker Compose

```bash
# Start test environment
docker-compose -f docker-compose.test.yml up -d

# Run integration tests
npm run test:integration
```

### 8.3 Performance Tests

**Tool:** k6

**Test Scenarios:**
1. **Baseline:** 100 req/min for 30 min
2. **Target Load:** 694 req/min for 60 min
3. **Burst:** 1000 req/min for 15 min
4. **Endurance:** 500 req/min for 24 hours

**Metrics:**
- Throughput (req/min)
- Latency (p50, p95, p99)
- Error rate
- Memory usage
- CPU usage

### 8.4 Smoke Tests

**Post-Deployment:**
```bash
#!/bin/bash
# tests/smoke/post-deploy.sh

# 1. Health checks
curl -f https://api.ziraai.com/health || exit 1
curl -f https://admin.ziraai.com/api/health || exit 1

# 2. Submit test analysis
ANALYSIS_ID=$(curl -X POST https://api.ziraai.com/api/v2/analysis \
  -H "Content-Type: application/json" \
  -d '{"farmer_id":"smoke-test","leaf_top_url":"..."}' \
  | jq -r '.analysis_id')

# 3. Wait for result
for i in {1..120}; do
  STATUS=$(curl -s https://api.ziraai.com/api/v2/analysis/$ANALYSIS_ID | jq -r '.status')
  if [ "$STATUS" = "completed" ]; then
    echo "✅ Smoke test passed"
    exit 0
  fi
  sleep 1
done

echo "❌ Smoke test failed: timeout"
exit 1
```

---

## 9. Deployment Stratejisi

### 9.1 Railway Project Structure

```
Railway Project: ziraai-platform
│
├── Services:
│   ├── webapi (MEVCUT)
│   ├── plant-analysis-worker (MEVCUT)
│   ├── dispatcher (YENİ)
│   ├── openai-worker-001 (YENİ)
│   ├── openai-worker-002 (YENİ)
│   ├── openai-worker-003 (YENİ)
│   ├── openai-worker-004 (YENİ)
│   ├── openai-worker-005 (YENİ)
│   ├── gemini-worker-001 (YENİ)
│   ├── gemini-worker-002 (YENİ)
│   ├── gemini-worker-003 (YENİ)
│   ├── gemini-worker-004 (YENİ)
│   ├── gemini-worker-005 (YENİ)
│   ├── anthropic-worker-001 (YENİ)
│   ├── anthropic-worker-002 (YENİ)
│   ├── anthropic-worker-003 (YENİ)
│   ├── anthropic-worker-004 (YENİ)
│   ├── anthropic-worker-005 (YENİ)
│   └── admin-panel (YENİ)
│
└── Shared Resources:
    ├── postgres
    ├── redis
    └── rabbitmq
```

### 9.2 Environment Variables

**Shared:**
```bash
RABBITMQ_URL=amqp://...
REDIS_URL=redis://...
DATABASE_URL=postgresql://...
NODE_ENV=production
```

**Per Worker:**
```bash
# OpenAI Worker
PROVIDER=openai
CONCURRENCY=60
RATE_LIMIT=350
QUEUE_NAME=openai-analysis-queue
RESULT_QUEUE=analysis-results
DLQ_QUEUE=analysis-dlq
OPENAI_API_KEY=sk-...

# Gemini Worker
PROVIDER=gemini
CONCURRENCY=70
RATE_LIMIT=450
QUEUE_NAME=gemini-analysis-queue
GEMINI_API_KEY=...

# Anthropic Worker
PROVIDER=anthropic
CONCURRENCY=60
RATE_LIMIT=350
QUEUE_NAME=claude-analysis-queue
ANTHROPIC_API_KEY=sk-ant-...
```

### 9.3 CI/CD Pipeline

```yaml
# .github/workflows/deploy.yml
name: Deploy to Railway

on:
  push:
    branches: [master, staging]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Run tests
        run: |
          npm install
          npm test

  deploy:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Deploy to Railway
        run: |
          railway up --service dispatcher
          railway up --service openai-worker-001
          # ... other services
```

---

## 10. Monitoring ve Operations

### 10.1 Dashboard Metrics

**Real-Time (1 minute window):**
- Throughput (req/min)
- Queue depth (all queues)
- Active workers (sum)
- Error rate (%)

**Provider-Specific:**
- Throughput per provider
- Rate usage / limit
- Queue depth per provider
- Error rate per provider
- Avg latency (ms)
- Health status

**Historical (24 hours):**
- Throughput graph
- Latency graph (p50, p95, p99)
- Error rate graph
- Cost graph

**Daily Summary:**
- Total analyses
- Successful analyses
- Failed analyses
- Total cost
- Avg latency

### 10.2 Alert Rules

| Alert | Condition | Duration | Severity | Action |
|-------|-----------|----------|----------|--------|
| HighQueueDepth | queue_depth > 500 | 5 min | warning | notify_ops_team |
| ProviderDown | health == "down" | 1 min | critical | page_on_call |
| HighErrorRate | error_rate > 0.05 | 3 min | warning | notify_ops_team |
| RateLimitNearMax | usage / limit > 0.9 | 3 min | warning | notify_ops_team |
| HighLatency | p95 > 90000 | 5 min | warning | notify_ops_team |
| LowThroughput | throughput < 500 AND queue_depth > 100 | 5 min | critical | page_on_call |

### 10.3 Health Checks

**API Service:**
```bash
GET /health
Response: { "status": "ok", "timestamp": "..." }
```

**Worker Service:**
```bash
GET /health
Response: {
  "status": "ok",
  "provider": "openai",
  "queueDepth": 45,
  "rateUsage": 234
}
```

**Admin Panel:**
```bash
GET /api/health
Response: {
  "status": "ok",
  "services": {
    "redis": "ok",
    "rabbitmq": "ok",
    "postgres": "ok"
  }
}
```

### 10.4 Logging

**Structured Logging (pino):**
```typescript
logger.info({
  analysisId,
  provider,
  duration,
  tokens
}, 'Analysis completed');
```

**Log Levels:**
- ERROR: System errors, API failures
- WARN: Rate limit near max, high queue depth
- INFO: Normal operations, analysis completed
- DEBUG: Detailed flow, for troubleshooting

**Log Aggregation:**
- Railway logs
- External service (optional): Datadog, Grafana Loki

---

## 11. Risk Analizi

### 11.1 Risk Matrix

| Risk | Olasılık | Etki | Mitigation | Recovery |
|------|----------|------|------------|----------|
| **AI Provider Rate Limit Aşımı** | Orta | Yüksek | Multi-provider failover, Redis rate limiting | Traffic diğer provider'lara yönlendirilir |
| **Tek Provider Failure** | Orta | Orta | Circuit breaker, automatic failover | Kapas ite %33 düşer ama devam eder |
| **RabbitMQ Downtime** | Düşük | Kritik | Railway managed service, message persistence | Worker'lar reconnect, mesajlar kaybolmaz |
| **Redis Downtime** | Düşük | Yüksek | Fallback to local rate limiting | Worker'lar local limit ile devam eder |
| **Cost Overrun** | Orta | Orta | Real-time cost tracking, budget alerts | Scale down, switch to cheaper providers |
| **Worker Memory Leak** | Düşük | Orta | Memory profiling, automatic restart | Railway automatic restart |
| **Database Overload** | Düşük | Yüksek | Connection pooling, batch inserts | Result worker retry, database scale up |

### 11.2 Disaster Recovery

**RTO (Recovery Time Objective):** 15 dakika
**RPO (Recovery Point Objective):** 5 dakika

**Backup Stratejisi:**
- Database: Automated daily backup (Railway)
- Queue: Message persistence (RabbitMQ durable)
- Configuration: Git repository

**Recovery Procedures:**
1. Database failure → Restore from latest backup
2. Queue failure → Reconnect, reprocess persisted messages
3. Redis failure → Use local rate limiting, restore Redis data
4. Complete system failure → Redeploy from Git, restore database

---

## 12. Success Criteria

### 12.1 Phase Completion Checklist

**Phase 1:**
- [x] TypeScript worker projesi build ediliyor
- [x] OpenAI provider çalışıyor
- [x] RabbitMQ queue'lar oluşturulmuş
- [x] Redis rate limiting çalışıyor
- [x] WebAPI yeni queue'ya publish ediyor
- [x] End-to-end flow < 90s
- [x] Docker Compose local environment çalışıyor
- [x] n8n devre dışı bırakılmış

**Phase 2:**
- [ ] Dispatcher service çalışıyor
- [ ] 3 provider implementasyonu tamamlanmış
- [ ] Provider seçim algoritması doğru
- [ ] Circuit breaker tetikleniyor
- [ ] Failover mechanism test edilmiş
- [ ] Rate limiting 3 provider için çalışıyor
- [ ] Integration testler pass

**Phase 3:**
- [ ] Admin panel login çalışıyor
- [ ] Dashboard metrics görüntüleniyor
- [ ] Real-time updates çalışıyor
- [ ] Scale API worker count değiştiriyor
- [ ] Railway CLI integration çalışıyor
- [ ] Alert sistemi çalışıyor
- [ ] Cost tracking doğru

**Phase 4:**
- [ ] Load test 694 req/min passed
- [ ] p95 latency < 90s
- [ ] Error rate < 5%
- [ ] 24h endurance test passed
- [ ] Security audit completed
- [ ] Documentation tamamlanmış
- [ ] Canary deployment successful
- [ ] Production monitoring active

### 12.2 Production Readiness Checklist

**Technical:**
- [ ] All unit tests passing (>80% coverage)
- [ ] All integration tests passing
- [ ] Load tests passed (1M/gün verified)
- [ ] No critical bugs
- [ ] No known memory leaks
- [ ] Performance optimized
- [ ] Security audit passed
- [ ] Database migrations ready

**Operational:**
- [ ] Monitoring dashboard functional
- [ ] Alerting configured
- [ ] On-call rotation setup
- [ ] Runbook documented
- [ ] Disaster recovery plan tested
- [ ] Rollback procedure documented
- [ ] Cost tracking active
- [ ] SLA definitions clear

**Documentation:**
- [ ] Architecture documentation complete
- [ ] API documentation complete
- [ ] Deployment guide complete
- [ ] Operations runbook complete
- [ ] Troubleshooting guide complete
- [ ] Developer onboarding guide complete

### 12.3 Success Metrics

**First Week:**
| Metrik | Hedef | Ölçüm Yöntemi |
|--------|-------|---------------|
| Günlük analiz | 100,000 | PostgreSQL count |
| p95 latency | <90s | Metrics dashboard |
| Error rate | <5% | Failed/Total ratio |
| Uptime | >99% | Health check logs |
| Cost/analysis | $0.40 | Total cost / analyses |

**First Month:**
| Metrik | Hedef | Ölçüm Yöntemi |
|--------|-------|---------------|
| Günlük analiz | 1,000,000 | PostgreSQL count |
| p95 latency | <90s | Metrics dashboard |
| Error rate | <5% | Failed/Total ratio |
| Uptime | >99.5% | Health check logs |
| Monthly cost | $400K | AI API + Infrastructure |
| Customer satisfaction | >90% | Feedback survey |

---

## Sonraki Adımlar

1. **Onay Alın:** Bu planı review edin ve onaylayın
2. **Takım Oluşturun:** Frontend (Admin Panel), Backend (Workers), DevOps (Railway)
3. **Sprint Planlama:** 8 haftalık sprint'lere bölün
4. **Phase 1 Başlat:** TypeScript worker infrastructure kurulumu

**İletişim:**
- Daily standups (15 dakika)
- Weekly review (her Cuma)
- Monthly retrospective (her ayın sonu)

**Tracking:**
- Jira/GitHub Projects
- Slack channel: #ziraai-scaling
- Weekly progress reports

---

*Bu dokümanda yapılacak değişiklikler:*
- [ ] Phase başarı kriterleri update
- [ ] Timeline ayarlamaları
- [ ] Maliyet revizyonu
- [ ] Risk güncelleme
