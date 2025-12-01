# ZiraAI Platform - Teknik Analiz ve Mimari Tasarım Dokümanı

**Versiyon:** 1.0  
**Tarih:** Kasım 2025  
**Hazırlayan:** Claude AI  
**Proje:** ZiraAI - Tarımsal Yapay Zeka Analiz Platformu

---

## İçindekiler

1. [Yönetici Özeti](#1-yönetici-özeti)
2. [Mevcut Durum Analizi](#2-mevcut-durum-analizi)
3. [Hedefler ve Kısıtlar](#3-hedefler-ve-kısıtlar)
4. [Önerilen Mimari](#4-önerilen-mimari)
5. [Komponent Detayları](#5-komponent-detayları)
6. [Veri Akışı](#6-veri-akışı)
7. [Rate Limiting ve Throttling Stratejisi](#7-rate-limiting-ve-throttling-stratejisi)
8. [Multi-Provider AI Stratejisi](#8-multi-provider-ai-stratejisi)
9. [Scale Yönetimi](#9-scale-yönetimi)
10. [Failover ve Resilience](#10-failover-ve-resilience)
11. [Deployment Stratejisi](#11-deployment-stratejisi)
12. [Monitoring ve Observability](#12-monitoring-ve-observability)
13. [Maliyet Analizi](#13-maliyet-analizi)
14. [Uygulama Yol Haritası](#14-uygulama-yol-haritası)
15. [Teknik Spesifikasyonlar](#15-teknik-spesifikasyonlar)

---

## 1. Yönetici Özeti

### 1.1 Proje Tanımı

ZiraAI, çiftçilerin bitki sağlığını analiz etmelerine yardımcı olan bir yapay zeka platformudur. Kullanıcılar, bitkilerinin fotoğraflarını (yaprak üstü, yaprak altı, genel görünüm, kök) göndererek hastalık, zararlı, besin eksikliği ve çevresel stres faktörleri hakkında detaylı analiz raporları alabilirler.

### 1.2 Mevcut Kapasite vs Hedef

| Metrik | Mevcut Durum | Hedef |
|--------|--------------|-------|
| Günlük Analiz | ~1,200 | 1,000,000 |
| Dakikalık Throughput | ~0.85 | 694 |
| Concurrent İşlem | 1 | ~850 |
| Response Time | ~70 saniye | ~70 saniye |

### 1.3 Temel Sorun

Mevcut n8n tabanlı mimari, **1 milyon günlük analiz** hedefini karşılayamaz. Bu ölçeğe ulaşmak için:
- n8n'den native worker'lara geçiş gerekli
- Multi-provider AI stratejisi şart
- Merkezi rate limiting ve queue management zorunlu

### 1.4 Önerilen Çözüm

Railway üzerinde manuel scale edilebilen, TypeScript tabanlı native worker mimarisi. Bu mimari:
- 3 AI provider (OpenAI, Gemini, Anthropic) kullanır
- Redis tabanlı merkezi rate limiting sağlar
- RabbitMQ ile queue-based asenkron işlem yapar
- Admin panel ile manuel scale kontrolü sunar

---

## 2. Mevcut Durum Analizi

### 2.1 Mevcut Mimari

```
┌──────────────┐     ┌──────────────────────────┐     ┌──────────────────┐
│   Client     │────▶│      API Service         │────▶│    RabbitMQ      │
│  (Mobile/Web)│     │      (Railway)           │     │ (Request Queue)  │
└──────────────┘     └──────────────────────────┘     └────────┬─────────┘
                                                               │
                                                               ▼
                                                      ┌──────────────────┐
                                                      │   n8n Workflow   │
                                                      │ (Single Instance)│
                                                      │ parallelMsg: 1   │
                                                      └────────┬─────────┘
                                                               │
                                                               ▼
                                                      ┌──────────────────┐
                                                      │    RabbitMQ      │
                                                      │ (Result Queue)   │
                                                      └────────┬─────────┘
                                                               │
                                                               ▼
                                                      ┌──────────────────┐
                                                      │  Worker Service  │
                                                      │    (Railway)     │
                                                      └────────┬─────────┘
                                                               │
                                                               ▼
                                                      ┌──────────────────┐
                                                      │   PostgreSQL     │
                                                      └──────────────────┘
```

### 2.2 Mevcut n8n Flow Yapısı

```
Flow: ZiraaiV3Async_MultiImage

┌─────────────────────────────┐
│ RabbitMQ Trigger            │
│ Queue: plant-analysis-...   │
│ parallelMessages: 1  ⚠️     │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│ Parse and Validate          │
│ RabbitMQ Message            │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐     ┌─────────────────────┐
│ Agricultural Analysis       │◄────│ OpenAI GPT-5-mini   │
│ AI Agent                    │     │                     │
└──────────────┬──────────────┘     └─────────────────────┘
               │
               ▼
┌─────────────────────────────┐
│ Token Usage Calculator      │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│ Parse and Validate Analysis │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│ Send to Response Queue      │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│ Check if Failed             │
├─────────────┬───────────────┤
│  Failed     │    Success    │
└──────┬──────┴────────┬──────┘
       ▼               ▼
┌─────────────┐  ┌─────────────┐
│ Send to DLQ │  │ Log Success │
└─────────────┘  └─────────────┘
```

### 2.3 Mevcut Sorunlar

| Sorun | Açıklama | Etki |
|-------|----------|------|
| **parallelMessages: 1** | n8n aynı anda sadece 1 mesaj işliyor | Throughput: ~0.85/dk |
| **Tek AI Provider** | Sadece OpenAI kullanılıyor | Rate limit: 400/dk max |
| **n8n Overhead** | Her flow için ~70 saniye | Concurrent limit çok düşük |
| **Rate Limit Yönetimi Yok** | Merkezi kontrol mekanizması yok | Burst traffic'te hata |
| **Scale Zorluğu** | n8n instance yönetimi karmaşık | 1M hedefi için 41 instance lazım |

### 2.4 Mevcut Teknoloji Stack

| Katman | Teknoloji | Hosting |
|--------|-----------|---------|
| API | .NET / Node.js | Railway |
| Workflow | n8n | Self-hosted / Railway |
| Queue | RabbitMQ | Railway / CloudAMQP |
| Database | PostgreSQL | Railway |
| AI | OpenAI gpt-5-mini | API |

---

## 3. Hedefler ve Kısıtlar

### 3.1 İş Hedefleri

| Hedef | Değer | Öncelik |
|-------|-------|---------|
| Günlük Analiz Kapasitesi | 1,000,000 | Kritik |
| Response Time | <90 saniye | Yüksek |
| Uptime | %99.5 | Yüksek |
| Maliyet Optimizasyonu | Min. TCO | Orta |

### 3.2 Teknik Kısıtlar

#### 3.2.1 AI Model Rate Limitleri

| Provider | Model | Token Limit | Request Limit | Günlük Kapasite |
|----------|-------|-------------|---------------|-----------------|
| OpenAI | gpt-5-mini | 2,000,000 TPM | 5,000 RPM | ~350/dk* |
| Google | Gemini 2.5 Pro | - | ~500 RPM | ~450/dk* |
| Anthropic | Claude Sonnet | - | ~400 RPM | ~350/dk* |

*5,000 token/analiz varsayımıyla hesaplanmıştır.

#### 3.2.2 Flow Süresi Kısıtı

- Ortalama flow süresi: **~70 saniye**
- AI çağrısı: ~30-40 saniye
- Pre/Post processing: ~30-40 saniye

#### 3.2.3 n8n Limitleri

| Parametre | Limit | Açıklama |
|-----------|-------|----------|
| Concurrent Executions (Self-hosted) | ~20-50 / instance | Bellek ve CPU'ya bağlı |
| Concurrent Executions (Cloud) | Plan bazlı | Genellikle düşük |

### 3.3 Kapasite Hesaplaması

```
Hedef: 1,000,000 analiz/gün

Dakikalık ihtiyaç:
1,000,000 ÷ 24 saat ÷ 60 dakika = 694 analiz/dakika

Concurrent ihtiyacı (70 sn flow süresi ile):
694 × (70 ÷ 60) = ~810 concurrent işlem

AI Rate Limit kontrolü:
- OpenAI: 350/dk
- Gemini: 450/dk  
- Anthropic: 350/dk
- TOPLAM: 1,150/dk ✅ (694'ten fazla, yeterli)
```

---

## 4. Önerilen Mimari

### 4.1 Yüksek Seviye Mimari

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                                                                                     │
│                              ZiraAI PLATFORM v2.0                                   │
│                                                                                     │
│  ┌───────────────────────────────────────────────────────────────────────────────┐ │
│  │                            SHARED INFRASTRUCTURE                               │ │
│  │                                                                                │ │
│  │   ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐            │ │
│  │   │     Redis       │   │    RabbitMQ     │   │   PostgreSQL    │            │ │
│  │   │   (Railway)     │   │   (Railway)     │   │   (Railway)     │            │ │
│  │   │                 │   │                 │   │                 │            │ │
│  │   │ • Rate Limits   │   │ • Raw Queue     │   │ • Analysis      │            │ │
│  │   │ • Health Status │   │ • Provider Qs   │   │ • Users         │            │ │
│  │   │ • Metrics       │   │ • Result Queue  │   │ • Metrics       │            │ │
│  │   │ • Scale Config  │   │ • DLQ           │   │                 │            │ │
│  │   └─────────────────┘   └─────────────────┘   └─────────────────┘            │ │
│  │                                                                                │ │
│  └───────────────────────────────────────────────────────────────────────────────┘ │
│                                                                                     │
│  ┌───────────────────────────────────────────────────────────────────────────────┐ │
│  │                              CORE SERVICES                                     │ │
│  │                                                                                │ │
│  │   ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐            │ │
│  │   │   API Service   │   │   Dispatcher    │   │  Admin Panel    │            │ │
│  │   │   (Railway)     │   │   (Railway)     │   │   (Railway)     │            │ │
│  │   │                 │   │                 │   │                 │            │ │
│  │   │ • Auth          │   │ • Provider      │   │ • Scale Control │            │ │
│  │   │ • Validation    │   │   Selection     │   │ • Monitoring    │            │ │
│  │   │ • Queue Publish │   │ • Rate Check    │   │ • Health View   │            │ │
│  │   │ • Quick ACK     │   │ • Load Balance  │   │ • Alerts        │            │ │
│  │   │                 │   │ • Retry Logic   │   │                 │            │ │
│  │   └─────────────────┘   └─────────────────┘   └─────────────────┘            │ │
│  │                                                                                │ │
│  └───────────────────────────────────────────────────────────────────────────────┘ │
│                                                                                     │
│  ┌───────────────────────────────────────────────────────────────────────────────┐ │
│  │                            AI WORKER POOLS                                     │ │
│  │                                                                                │ │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐ │ │
│  │   │                        OpenAI Worker Pool                                │ │ │
│  │   │                                                                          │ │ │
│  │   │   ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐          │ │ │
│  │   │   │Worker 1 │ │Worker 2 │ │Worker 3 │ │Worker 4 │ │Worker 5 │          │ │ │
│  │   │   │Conc: 60 │ │Conc: 60 │ │Conc: 60 │ │Conc: 60 │ │Conc: 60 │          │ │ │
│  │   │   └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘          │ │ │
│  │   │                                                                          │ │ │
│  │   │   Queue: openai-analysis-queue | Rate: 350/dk | Total Conc: 300         │ │ │
│  │   └─────────────────────────────────────────────────────────────────────────┘ │ │
│  │                                                                                │ │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐ │ │
│  │   │                        Gemini Worker Pool                                │ │ │
│  │   │                                                                          │ │ │
│  │   │   ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐          │ │ │
│  │   │   │Worker 1 │ │Worker 2 │ │Worker 3 │ │Worker 4 │ │Worker 5 │          │ │ │
│  │   │   │Conc: 70 │ │Conc: 70 │ │Conc: 70 │ │Conc: 70 │ │Conc: 70 │          │ │ │
│  │   │   └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘          │ │ │
│  │   │                                                                          │ │ │
│  │   │   Queue: gemini-analysis-queue | Rate: 450/dk | Total Conc: 350         │ │ │
│  │   └─────────────────────────────────────────────────────────────────────────┘ │ │
│  │                                                                                │ │
│  │   ┌─────────────────────────────────────────────────────────────────────────┐ │ │
│  │   │                       Anthropic Worker Pool                              │ │ │
│  │   │                                                                          │ │ │
│  │   │   ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐          │ │ │
│  │   │   │Worker 1 │ │Worker 2 │ │Worker 3 │ │Worker 4 │ │Worker 5 │          │ │ │
│  │   │   │Conc: 60 │ │Conc: 60 │ │Conc: 60 │ │Conc: 60 │ │Conc: 60 │          │ │ │
│  │   │   └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘          │ │ │
│  │   │                                                                          │ │ │
│  │   │   Queue: claude-analysis-queue | Rate: 350/dk | Total Conc: 300         │ │ │
│  │   └─────────────────────────────────────────────────────────────────────────┘ │ │
│  │                                                                                │ │
│  └───────────────────────────────────────────────────────────────────────────────┘ │
│                                                                                     │
│  ┌───────────────────────────────────────────────────────────────────────────────┐ │
│  │                           RESULT PROCESSING                                    │ │
│  │                                                                                │ │
│  │   ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐                │ │
│  │   │Result   │ │Result   │ │Result   │ │Result   │ │Result   │                │ │
│  │   │Worker 1 │ │Worker 2 │ │Worker 3 │ │Worker 4 │ │Worker 5 │                │ │
│  │   └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘                │ │
│  │                                                                                │ │
│  │   Queue: analysis-results | Task: DB Write, Webhook Callback, Notification   │ │
│  │                                                                                │ │
│  └───────────────────────────────────────────────────────────────────────────────┘ │
│                                                                                     │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### 4.2 Mimari Kararların Gerekçeleri

| Karar | Gerekçe |
|-------|---------|
| **n8n'den Native Worker'a Geçiş** | 41 n8n instance yerine 15 worker pod ile aynı kapasite. Daha düşük maliyet ve karmaşıklık. |
| **Multi-Provider AI** | Tek provider ile max 504K/gün. 3 provider ile 1.65M/gün potansiyel. |
| **Redis Rate Limiting** | Merkezi, atomic, düşük latency rate limit kontrolü. |
| **RabbitMQ Queue Per Provider** | İzole failure domain, bağımsız scale, kolay monitoring. |
| **Railway (K8s Yerine)** | Mevcut deneyim, kolay deployment, yeterli scale kapasitesi. |

---

## 5. Komponent Detayları

### 5.1 API Service

**Sorumluluklar:**
- HTTP endpoint'leri sunma
- Request validation
- Authentication & Authorization
- Rate limiting (client bazlı)
- RabbitMQ'ya mesaj publish etme
- Quick ACK (hemen response dönme)

**Teknoloji:** Mevcut .NET veya Node.js projesi kullanılabilir.

**Endpoint'ler:**

```
POST /api/v2/analysis
  - Request: { images: [...], farmer_id, location, crop_type, ... }
  - Response: { analysis_id, status: "queued", estimated_time: 70 }

GET /api/v2/analysis/:id
  - Response: { analysis_id, status, result?, error? }

GET /api/v2/analysis/:id/status
  - Response: { status: "queued" | "processing" | "completed" | "failed" }
```

**Konfigürasyon:**

```typescript
interface APIConfig {
  port: number;                    // 3000
  rabbitmq: {
    url: string;
    rawQueue: string;              // "raw-analysis-queue"
  };
  redis: {
    url: string;
  };
  rateLimit: {
    windowMs: number;              // 60000 (1 dakika)
    maxRequestsPerWindow: number;  // Client başına limit
  };
}
```

### 5.2 Dispatcher Service

**Sorumluluklar:**
- Raw queue'dan mesaj okuma
- Provider seçimi (rate limit, health, priority)
- Provider queue'larına mesaj yönlendirme
- Retry logic (tüm provider'lar doluysa)
- Metrics güncelleme

**Akış:**

```
1. Raw Queue'dan mesaj al
2. Redis'ten provider durumlarını kontrol et:
   - rate:openai:{window} < 350?
   - rate:gemini:{window} < 450?
   - rate:anthropic:{window} < 350?
   - health:openai === "ok"?
   - health:gemini === "ok"?
   - health:anthropic === "ok"?
3. Uygun provider seç (priority: openai > gemini > anthropic)
4. Seçilen provider queue'suna publish et
5. Rate counter'ı increment et
6. Eğer hiç uygun provider yoksa: requeue with delay
```

**Kod Yapısı:**

```typescript
// dispatcher/src/index.ts
interface DispatcherConfig {
  providers: ProviderConfig[];
  rabbitmq: {
    url: string;
    rawQueue: string;
    prefetch: number;          // 100
  };
  redis: {
    url: string;
  };
  retryDelay: number;          // 5000ms
  maxRetries: number;          // 10
}

interface ProviderConfig {
  name: string;                // "openai" | "gemini" | "anthropic"
  queue: string;               // "openai-analysis-queue"
  rateLimit: number;           // 350
  priority: number;            // 1 (en yüksek)
  costPerRequest: number;      // 0.015
}
```

### 5.3 AI Analysis Worker

**Sorumluluklar:**
- Provider queue'dan mesaj okuma
- Rate limit kontrolü (double-check)
- AI API çağrısı
- Response parsing ve validation
- Result queue'ya publish
- Error handling ve retry
- Health status güncelleme

**Kod Yapısı:**

```typescript
// worker/src/analysis-worker.ts
interface WorkerConfig {
  provider: "openai" | "gemini" | "anthropic";
  concurrency: number;         // 60
  rateLimit: number;           // 350
  queueName: string;           // "openai-analysis-queue"
  resultQueue: string;         // "analysis-results"
  dlqQueue: string;            // "analysis-dlq"
}

class AnalysisWorker {
  private redis: Redis;
  private channel: Channel;
  private limiter: pLimit.Limit;
  private aiClient: AIClient;
  
  async start(): Promise<void>;
  async processMessage(msg: Message): Promise<void>;
  async waitForRateLimit(): Promise<void>;
  async analyze(request: AnalysisRequest): Promise<AnalysisResult>;
  async recordSuccess(): Promise<void>;
  async recordFailure(error: Error): Promise<void>;
}
```

**Provider-Specific Implementasyonlar:**

```typescript
// worker/src/providers/openai.ts
class OpenAIProvider implements AIProvider {
  private client: OpenAI;
  
  async analyze(request: AnalysisRequest): Promise<AnalysisResult> {
    const response = await this.client.chat.completions.create({
      model: "gpt-5-mini",
      max_tokens: 5000,
      messages: [{
        role: "user",
        content: [
          { type: "text", text: this.buildPrompt(request) },
          ...this.buildImageContents(request)
        ]
      }]
    });
    return JSON.parse(response.choices[0].message.content);
  }
}

// worker/src/providers/anthropic.ts
class AnthropicProvider implements AIProvider {
  private client: Anthropic;
  
  async analyze(request: AnalysisRequest): Promise<AnalysisResult> {
    const response = await this.client.messages.create({
      model: "claude-sonnet-4-20250514",
      max_tokens: 5000,
      messages: [{
        role: "user",
        content: [
          ...this.buildImageContents(request),
          { type: "text", text: this.buildPrompt(request) }
        ]
      }]
    });
    return JSON.parse(response.content[0].text);
  }
}

// worker/src/providers/gemini.ts
class GeminiProvider implements AIProvider {
  private client: GoogleGenerativeAI;
  
  async analyze(request: AnalysisRequest): Promise<AnalysisResult> {
    const model = this.client.getGenerativeModel({ model: "gemini-2.5-pro" });
    const result = await model.generateContent([
      this.buildPrompt(request),
      ...await this.buildImageParts(request)
    ]);
    return JSON.parse(result.response.text());
  }
}
```

### 5.4 Result Worker

**Sorumluluklar:**
- Result queue'dan mesaj okuma
- Analiz sonuçlarını PostgreSQL'e kaydetme
- Webhook callback (varsa)
- Push notification (varsa)
- Metrics güncelleme

**Kod Yapısı:**

```typescript
// result-worker/src/index.ts
interface ResultWorkerConfig {
  rabbitmq: {
    url: string;
    resultQueue: string;
    prefetch: number;          // 50
  };
  database: {
    url: string;
    poolSize: number;          // 20
  };
  webhook: {
    timeout: number;           // 5000ms
    retries: number;           // 3
  };
}

class ResultWorker {
  async processResult(result: AnalysisResult): Promise<void> {
    // 1. DB'ye kaydet
    await this.saveToDatabase(result);
    
    // 2. Callback URL varsa çağır
    if (result.callbackUrl) {
      await this.sendWebhook(result);
    }
    
    // 3. Push notification gönder
    if (result.pushToken) {
      await this.sendPushNotification(result);
    }
    
    // 4. Metrics güncelle
    await this.updateMetrics(result);
  }
}
```

### 5.5 Admin Panel

**Sorumluluklar:**
- Worker scale kontrolü
- Real-time monitoring
- Health status görüntüleme
- Alert yönetimi
- Maliyet takibi

**Özellikler:**

```typescript
// admin-panel/src/types.ts
interface DashboardData {
  summary: {
    totalThroughput: number;       // per minute
    totalWorkers: number;
    queueDepth: number;
    dailyCapacity: number;
  };
  providers: ProviderStatus[];
  recentAlerts: Alert[];
  costMetrics: CostMetrics;
}

interface ProviderStatus {
  name: string;
  activeWorkers: number;
  queueDepth: number;
  rateUsage: number;
  rateLimit: number;
  throughput: number;
  errorRate: number;
  health: "ok" | "degraded" | "down";
}
```

**Scale API:**

```typescript
// admin-panel/src/pages/api/scale.ts
POST /api/scale
  Request: { provider: string, targetCount: number }
  Response: { 
    success: boolean, 
    previousCount: number, 
    newCount: number,
    action: "scaled_up" | "scaled_down" | "no_change"
  }

GET /api/status
  Response: DashboardData
```

---

## 6. Veri Akışı

### 6.1 Ana İş Akışı

```
┌─────────┐    ┌─────────┐    ┌───────────┐    ┌────────────┐    ┌──────────┐
│ Client  │───▶│   API   │───▶│ Raw Queue │───▶│ Dispatcher │───▶│ Provider │
│         │    │ Service │    │           │    │            │    │  Queue   │
└─────────┘    └─────────┘    └───────────┘    └────────────┘    └────┬─────┘
                                                                      │
     ┌────────────────────────────────────────────────────────────────┘
     │
     ▼
┌──────────┐    ┌───────────┐    ┌──────────┐    ┌────────────┐    ┌──────────┐
│ AI       │───▶│ Result    │───▶│ Result   │───▶│ PostgreSQL │    │  Client  │
│ Worker   │    │ Queue     │    │ Worker   │    │            │───▶│ (Poll/   │
│          │    │           │    │          │    │            │    │ Webhook) │
└──────────┘    └───────────┘    └──────────┘    └────────────┘    └──────────┘
```

### 6.2 Mesaj Formatları

**Raw Queue Message:**

```typescript
interface RawAnalysisMessage {
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
  gps_coordinates?: { lat: number; lng: number };
  crop_type?: string;
  soil_type?: string;
  weather_conditions?: string;
  temperature?: number;
  humidity?: number;
  
  // Metadata
  urgency_level: "low" | "normal" | "high" | "critical";
  callback_url?: string;
  
  // Internal
  _retryCount?: number;
  _createdAt: string;
}
```

**Provider Queue Message:**

```typescript
interface ProviderQueueMessage extends RawAnalysisMessage {
  _routing: {
    provider: string;
    dispatchedAt: number;
    rateWindow: number;
  };
}
```

**Result Queue Message:**

```typescript
interface ResultQueueMessage {
  // Original request data
  analysis_id: string;
  farmer_id?: string;
  sponsor_id?: string;
  
  // Analysis result
  result: AnalysisResult;
  
  // Processing metadata
  processing_metadata: {
    provider: string;
    processing_time_ms: number;
    completed_at: string;
    token_usage: TokenUsage;
  };
  
  // Callback info
  callback_url?: string;
}
```

### 6.3 Queue Yapılandırması

| Queue | Durable | Auto-Delete | TTL | DLX |
|-------|---------|-------------|-----|-----|
| raw-analysis-queue | ✅ | ❌ | - | analysis-dlq |
| openai-analysis-queue | ✅ | ❌ | 5dk | analysis-dlq |
| gemini-analysis-queue | ✅ | ❌ | 5dk | analysis-dlq |
| claude-analysis-queue | ✅ | ❌ | 5dk | analysis-dlq |
| analysis-results | ✅ | ❌ | - | result-dlq |
| analysis-dlq | ✅ | ❌ | 7gün | - |

---

## 7. Rate Limiting ve Throttling Stratejisi

### 7.1 Rate Limiting Mekanizması

**Sliding Window Counter (Redis):**

```typescript
// Rate limit check
async function checkRateLimit(provider: string, limit: number): Promise<boolean> {
  const window = Math.floor(Date.now() / 60000); // 1-minute window
  const key = `rate:${provider}:${window}`;
  
  const current = await redis.incr(key);
  await redis.expire(key, 120); // 2 minute TTL
  
  if (current > limit) {
    await redis.decr(key); // Rollback
    return false;
  }
  
  return true;
}
```

### 7.2 Redis Key Yapısı

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

### 7.3 Throttling Stratejisi

```
1. Dispatcher seviyesi (proactive):
   - Her mesaj için rate check
   - Uygun provider yoksa: requeue with exponential backoff
   - Backoff: 1s, 2s, 4s, 8s, 16s, 32s (max)

2. Worker seviyesi (reactive):
   - AI çağrısından önce double-check
   - Rate limit aşılmışsa: wait loop (100ms intervals)
   - Max wait: 30 saniye, sonra fail

3. Client seviyesi (API):
   - IP/User başına rate limit
   - 429 Too Many Requests response
   - Retry-After header
```

---

## 8. Multi-Provider AI Stratejisi

### 8.1 Provider Seçim Algoritması

```typescript
async function selectProvider(): Promise<ProviderConfig | null> {
  const window = Math.floor(Date.now() / 60000);
  const candidates: Array<ProviderConfig & { score: number }> = [];
  
  for (const provider of PROVIDERS) {
    // Health check
    const health = await redis.get(`health:${provider.name}`);
    if (health === "down") continue;
    
    // Error check (circuit breaker)
    const errors = parseInt(await redis.get(`errors:${provider.name}:${window}`) || "0");
    if (errors >= 5) continue;
    
    // Rate limit check
    const usage = parseInt(await redis.get(`rate:${provider.name}:${window}`) || "0");
    const remaining = provider.rateLimit - usage;
    if (remaining <= 0) continue;
    
    // Calculate score (higher = better)
    // Factors: remaining capacity, priority (cost), health status
    const score = (remaining / provider.rateLimit) * (1 / provider.priority);
    
    candidates.push({ ...provider, score });
  }
  
  if (candidates.length === 0) return null;
  
  // Sort by score descending
  candidates.sort((a, b) => b.score - a.score);
  
  return candidates[0];
}
```

### 8.2 Provider Konfigürasyonu

```typescript
const PROVIDERS: ProviderConfig[] = [
  {
    name: "openai",
    queue: "openai-analysis-queue",
    rateLimit: 350,        // per minute
    priority: 1,           // highest priority (cheapest)
    costPerRequest: 0.015, // USD
    model: "gpt-5-mini",
    healthKey: "health:openai"
  },
  {
    name: "gemini",
    queue: "gemini-analysis-queue",
    rateLimit: 450,
    priority: 2,
    costPerRequest: 0.018,
    model: "gemini-2.5-pro",
    healthKey: "health:gemini"
  },
  {
    name: "anthropic",
    queue: "claude-analysis-queue",
    rateLimit: 350,
    priority: 3,           // lowest priority (most expensive)
    costPerRequest: 0.020,
    model: "claude-sonnet-4-20250514",
    healthKey: "health:anthropic"
  }
];
```

### 8.3 Prompt Standardizasyonu

Tüm provider'lar için aynı prompt kullanılmalı (mevcut n8n prompt'u temel alınacak):

```typescript
function buildPrompt(request: AnalysisRequest): string {
  return `You are an expert agricultural analyst...
  
  [Mevcut n8n prompt'unun tamamı buraya gelecek]
  
  Context:
  - Analysis ID: ${request.analysis_id}
  - Farmer ID: ${request.farmer_id}
  - Location: ${request.location}
  - Crop Type: ${request.crop_type}
  ...
  
  Images to analyze:
  - Leaf Top: ${request.leaf_top_url || 'Not provided'}
  - Leaf Bottom: ${request.leaf_bottom_url || 'Not provided'}
  - Plant Overview: ${request.plant_overview_url || 'Not provided'}
  - Root: ${request.root_url || 'Not provided'}
  
  Return ONLY valid JSON with the specified structure.`;
}
```

---

## 9. Scale Yönetimi

### 9.1 Manuel Scale Presets

| Preset | OpenAI | Gemini | Claude | Total | Günlük Kapasite |
|--------|--------|--------|--------|-------|-----------------|
| 🔻 Minimum | 1 | 0 | 0 | 1 | ~50,000 |
| 📉 Low | 2 | 2 | 2 | 6 | ~400,000 |
| 📊 Medium | 3 | 4 | 3 | 10 | ~700,000 |
| 🚀 High (1M Target) | 5 | 5 | 5 | 15 | ~1,170,000 |
| 🔥 Maximum | 7 | 7 | 7 | 21 | ~1,600,000 |

### 9.2 Scale Prosedürü

**Scale Up:**

```bash
# Via Admin Panel
POST /api/scale
{
  "provider": "openai",
  "targetCount": 7
}

# Via CLI
railway service create \
  --name openai-worker-006 \
  --source ./workers \
  --env PROVIDER=openai \
  --env CONCURRENCY=60
```

**Scale Down (Graceful):**

```typescript
async function scaleDown(provider: string, targetCount: number) {
  const currentWorkers = await getActiveWorkers(provider);
  const workersToRemove = currentWorkers.slice(targetCount);
  
  for (const worker of workersToRemove) {
    // 1. Mark as draining (stop accepting new messages)
    await redis.set(`worker:${worker.id}:draining`, "true");
    
    // 2. Wait for current jobs to complete (max 2 min)
    await waitForDrain(worker.id, 120000);
    
    // 3. Delete service
    await railway.deleteService(worker.id);
  }
}
```

### 9.3 Scale Triggerları

| Trigger | Condition | Action |
|---------|-----------|--------|
| Queue Depth High | depth > 500 for 5 min | Scale up by 2 |
| Queue Depth Low | depth < 50 for 10 min | Scale down by 1 |
| Rate Limit Hit | usage > 90% for 3 min | Alert (manual decision) |
| Error Rate High | errors > 5% | Circuit breaker + alert |

---

## 10. Failover ve Resilience

### 10.1 Failure Senaryoları

| Senaryo | Etki | Otomatik Çözüm |
|---------|------|----------------|
| Tek Provider Down | Kapasite -%33 | Traffic diğer provider'lara yönlenir |
| İki Provider Down | Kapasite -%66 | Kalan provider ile devam |
| Tüm Provider'lar Down | Sistem durur | Mesajlar queue'da birikir, recovery sonrası devam |
| Worker Crash | Concurrent -%60 | RabbitMQ mesajları diğer worker'lara dağıtır |
| Redis Down | Rate limit çalışmaz | Fallback: local rate limit (riskli) |
| RabbitMQ Down | Tüm flow durur | N/A - kritik bağımlılık |

### 10.2 Circuit Breaker Pattern

```typescript
class CircuitBreaker {
  private state: "closed" | "open" | "half-open" = "closed";
  private failureCount: number = 0;
  private lastFailure: number = 0;
  
  private readonly threshold: number = 5;
  private readonly resetTimeout: number = 60000; // 1 minute
  
  async execute<T>(operation: () => Promise<T>): Promise<T> {
    if (this.state === "open") {
      if (Date.now() - this.lastFailure > this.resetTimeout) {
        this.state = "half-open";
      } else {
        throw new Error("Circuit breaker is open");
      }
    }
    
    try {
      const result = await operation();
      this.onSuccess();
      return result;
    } catch (error) {
      this.onFailure();
      throw error;
    }
  }
  
  private onSuccess() {
    this.failureCount = 0;
    this.state = "closed";
  }
  
  private onFailure() {
    this.failureCount++;
    this.lastFailure = Date.now();
    
    if (this.failureCount >= this.threshold) {
      this.state = "open";
    }
  }
}
```

### 10.3 Retry Stratejisi

```typescript
async function withRetry<T>(
  operation: () => Promise<T>,
  options: RetryOptions
): Promise<T> {
  const { maxRetries = 3, baseDelay = 1000, maxDelay = 30000 } = options;
  
  for (let attempt = 0; attempt <= maxRetries; attempt++) {
    try {
      return await operation();
    } catch (error) {
      if (attempt === maxRetries) throw error;
      
      const delay = Math.min(
        baseDelay * Math.pow(2, attempt), // Exponential backoff
        maxDelay
      );
      
      await sleep(delay + Math.random() * 1000); // Jitter
    }
  }
  
  throw new Error("Should not reach here");
}
```

---

## 11. Deployment Stratejisi

### 11.1 Railway Proje Yapısı

```
ziraai-platform/
├── services/
│   ├── api-service/
│   │   ├── src/
│   │   ├── Dockerfile
│   │   ├── package.json
│   │   └── railway.json
│   │
│   ├── dispatcher/
│   │   ├── src/
│   │   ├── Dockerfile
│   │   ├── package.json
│   │   └── railway.json
│   │
│   └── admin-panel/
│       ├── src/
│       ├── Dockerfile
│       ├── package.json
│       └── railway.json
│
├── workers/
│   ├── analysis-worker/        # Ortak worker kodu
│   │   ├── src/
│   │   │   ├── index.ts
│   │   │   ├── worker.ts
│   │   │   ├── providers/
│   │   │   │   ├── openai.ts
│   │   │   │   ├── gemini.ts
│   │   │   │   └── anthropic.ts
│   │   │   └── utils/
│   │   ├── Dockerfile
│   │   └── package.json
│   │
│   └── result-worker/
│       ├── src/
│       ├── Dockerfile
│       └── package.json
│
├── shared/
│   ├── types/
│   ├── utils/
│   └── constants/
│
├── scripts/
│   ├── deploy.sh
│   ├── scale.sh
│   └── health-check.sh
│
└── docker-compose.local.yml    # Local development
```

### 11.2 Docker Configuration

```dockerfile
# workers/analysis-worker/Dockerfile

FROM node:20-alpine AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM node:20-alpine AS runner
WORKDIR /app

# Install production dependencies only
COPY package*.json ./
RUN npm ci --only=production

# Copy built files
COPY --from=builder /app/dist ./dist

# Environment variables (overridden by Railway)
ENV NODE_ENV=production
ENV PROVIDER=openai
ENV CONCURRENCY=60
ENV RATE_LIMIT=350
ENV QUEUE_NAME=openai-analysis-queue

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD node dist/health-check.js || exit 1

CMD ["node", "dist/index.js"]
```

### 11.3 Environment Variables

```bash
# Shared (tüm servisler)
RABBITMQ_URL=amqp://user:pass@rabbitmq.railway.internal:5672
REDIS_URL=redis://default:pass@redis.railway.internal:6379
DATABASE_URL=postgresql://user:pass@postgres.railway.internal:5432/ziraai

# API Service
PORT=3000
JWT_SECRET=xxx
API_RATE_LIMIT=1000

# Dispatcher
DISPATCHER_PREFETCH=100
RETRY_DELAY=5000
MAX_RETRIES=10

# AI Workers
PROVIDER=openai              # openai | gemini | anthropic
CONCURRENCY=60
RATE_LIMIT=350
QUEUE_NAME=openai-analysis-queue
RESULT_QUEUE=analysis-results
DLQ_QUEUE=analysis-dlq

# Provider API Keys
OPENAI_API_KEY=sk-xxx
GEMINI_API_KEY=xxx
ANTHROPIC_API_KEY=sk-ant-xxx

# Result Worker
DB_POOL_SIZE=20
WEBHOOK_TIMEOUT=5000
WEBHOOK_RETRIES=3
```

### 11.4 Railway Service Configuration

```json
// railway.json (example for worker)
{
  "$schema": "https://railway.app/railway.schema.json",
  "build": {
    "builder": "DOCKERFILE",
    "dockerfilePath": "Dockerfile"
  },
  "deploy": {
    "numReplicas": 1,
    "restartPolicyType": "ON_FAILURE",
    "restartPolicyMaxRetries": 10
  }
}
```

---

## 12. Monitoring ve Observability

### 12.1 Metrikler

```typescript
// Prometheus metrics
const metrics = {
  // Throughput
  analysisTotal: new Counter({
    name: 'ziraai_analysis_total',
    help: 'Total number of analyses',
    labelNames: ['provider', 'status']
  }),
  
  // Latency
  analysisLatency: new Histogram({
    name: 'ziraai_analysis_duration_seconds',
    help: 'Analysis duration in seconds',
    labelNames: ['provider'],
    buckets: [10, 30, 60, 90, 120, 180]
  }),
  
  // Queue depth
  queueDepth: new Gauge({
    name: 'ziraai_queue_depth',
    help: 'Current queue depth',
    labelNames: ['queue']
  }),
  
  // Rate limit usage
  rateLimitUsage: new Gauge({
    name: 'ziraai_rate_limit_usage',
    help: 'Current rate limit usage',
    labelNames: ['provider']
  }),
  
  // Active workers
  activeWorkers: new Gauge({
    name: 'ziraai_active_workers',
    help: 'Number of active workers',
    labelNames: ['provider']
  }),
  
  // Cost
  costTotal: new Counter({
    name: 'ziraai_cost_usd_total',
    help: 'Total cost in USD',
    labelNames: ['provider']
  })
};
```

### 12.2 Alert Kuralları

```yaml
# Prometheus alerting rules
groups:
  - name: ziraai
    rules:
      - alert: HighQueueDepth
        expr: ziraai_queue_depth > 500
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Queue depth is high"
          
      - alert: ProviderDown
        expr: ziraai_provider_health == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "AI provider is down"
          
      - alert: HighErrorRate
        expr: rate(ziraai_analysis_total{status="error"}[5m]) > 0.05
        for: 3m
        labels:
          severity: warning
        annotations:
          summary: "Error rate is above 5%"
          
      - alert: RateLimitNearMax
        expr: ziraai_rate_limit_usage / ziraai_rate_limit_max > 0.9
        for: 3m
        labels:
          severity: warning
        annotations:
          summary: "Rate limit usage above 90%"
```

### 12.3 Logging

```typescript
// Structured logging with pino
const logger = pino({
  level: process.env.LOG_LEVEL || 'info',
  formatters: {
    level: (label) => ({ level: label })
  },
  base: {
    service: process.env.SERVICE_NAME,
    provider: process.env.PROVIDER,
    instance: process.env.RAILWAY_REPLICA_ID
  }
});

// Log examples
logger.info({ analysisId, farmerId }, 'Analysis started');
logger.info({ analysisId, duration, tokens }, 'Analysis completed');
logger.error({ analysisId, error: err.message }, 'Analysis failed');
```

---

## 13. Maliyet Analizi

### 13.1 AI API Maliyetleri

| Provider | Model | Input Cost | Output Cost | Avg/Analysis |
|----------|-------|------------|-------------|--------------|
| OpenAI | gpt-5-mini | $0.25/1M | $2.00/1M | ~$0.015 |
| Google | Gemini 2.5 Pro | $0.25/1M | $1.50/1M | ~$0.012 |
| Anthropic | Claude Sonnet | $0.30/1M | $1.50/1M | ~$0.013 |

**1M günlük analiz maliyeti:**

```
Dağılım (eşit): 333K OpenAI + 334K Gemini + 333K Anthropic

OpenAI:    333,000 × $0.015 = $4,995
Gemini:    334,000 × $0.012 = $4,008
Anthropic: 333,000 × $0.013 = $4,329
────────────────────────────────────
Günlük Toplam:              $13,332
Aylık Toplam:              ~$400,000
```

### 13.2 Infrastructure Maliyetleri (Railway)

| Service | Instance | Count | Est. Cost/mo |
|---------|----------|-------|--------------|
| API Service | 512MB RAM | 2 | $20 |
| Dispatcher | 512MB RAM | 2 | $20 |
| AI Workers | 1GB RAM | 15 | $150 |
| Result Workers | 512MB RAM | 5 | $50 |
| Admin Panel | 512MB RAM | 1 | $10 |
| Redis | 256MB | 1 | $10 |
| RabbitMQ | 512MB | 1 | $20 |
| PostgreSQL | 1GB | 1 | $30 |
| **TOPLAM** | | | **~$310/mo** |

### 13.3 Toplam Aylık Maliyet

```
AI API:         ~$400,000
Infrastructure:     ~$310
────────────────────────
TOPLAM:        ~$400,310/mo

Per analysis:  $0.40/analiz
```

---

## 14. Uygulama Yol Haritası

### Phase 1: Temel Altyapı (Hafta 1-2)

**Hedef:** Core worker yapısını oluştur, tek provider ile test et.

- [ ] Worker projesi scaffold (TypeScript)
- [ ] OpenAI provider implementasyonu
- [ ] RabbitMQ consumer/producer logic
- [ ] Redis rate limiting
- [ ] Local development environment (Docker Compose)
- [ ] Unit testler
- [ ] Railway deployment (single worker)
- [ ] Mevcut n8n flow'u devre dışı bırak

**Çıktı:** OpenAI ile çalışan tek worker, ~50K/gün kapasite

### Phase 2: Multi-Provider (Hafta 3-4)

**Hedef:** Tüm provider'ları ekle, dispatcher kur.

- [ ] Gemini provider implementasyonu
- [ ] Anthropic provider implementasyonu
- [ ] Dispatcher service
- [ ] Provider-specific queue'lar
- [ ] Circuit breaker pattern
- [ ] Health check endpoints
- [ ] Integration testler

**Çıktı:** 3 provider ile çalışan sistem, ~400K/gün kapasite

### Phase 3: Scale & Admin (Hafta 5-6)

**Hedef:** Scale mekanizması ve admin panel.

- [ ] Admin panel (Next.js)
- [ ] Scale API endpoints
- [ ] Real-time monitoring dashboard
- [ ] Railway CLI integration
- [ ] Scale presets
- [ ] Alerting rules

**Çıktı:** Manuel scale edilebilen sistem, admin panel

### Phase 4: Production Hardening (Hafta 7-8)

**Hedef:** Production-ready sistem.

- [ ] Load testing (1M/gün simülasyonu)
- [ ] Performance optimization
- [ ] Security audit
- [ ] Documentation
- [ ] Runbook (operasyon kılavuzu)
- [ ] Disaster recovery planı
- [ ] Gradual rollout

**Çıktı:** Production-ready, 1M/gün kapasiteli sistem

---

## 15. Teknik Spesifikasyonlar

### 15.1 API Spesifikasyonu

```yaml
openapi: 3.0.0
info:
  title: ZiraAI Analysis API
  version: 2.0.0

paths:
  /api/v2/analysis:
    post:
      summary: Submit plant analysis request
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/AnalysisRequest'
      responses:
        '202':
          description: Analysis queued
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/AnalysisQueued'
        '400':
          description: Invalid request
        '429':
          description: Rate limit exceeded
          headers:
            Retry-After:
              schema:
                type: integer

  /api/v2/analysis/{id}:
    get:
      summary: Get analysis result
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
      responses:
        '200':
          description: Analysis result
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/AnalysisResult'
        '202':
          description: Analysis in progress
        '404':
          description: Analysis not found

components:
  schemas:
    AnalysisRequest:
      type: object
      required:
        - farmer_id
      properties:
        farmer_id:
          type: string
        sponsor_id:
          type: string
        leaf_top_url:
          type: string
          format: uri
        leaf_bottom_url:
          type: string
          format: uri
        plant_overview_url:
          type: string
          format: uri
        root_url:
          type: string
          format: uri
        location:
          type: string
        crop_type:
          type: string
        urgency_level:
          type: string
          enum: [low, normal, high, critical]
        callback_url:
          type: string
          format: uri

    AnalysisQueued:
      type: object
      properties:
        analysis_id:
          type: string
        status:
          type: string
          enum: [queued]
        estimated_time_seconds:
          type: integer
        position_in_queue:
          type: integer

    AnalysisResult:
      type: object
      properties:
        analysis_id:
          type: string
        status:
          type: string
          enum: [completed, failed]
        result:
          $ref: '#/components/schemas/PlantAnalysis'
        error:
          type: string
        processing_time_ms:
          type: integer
        provider:
          type: string
```

### 15.2 Database Schema

```sql
-- Analyses table
CREATE TABLE analyses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    analysis_id VARCHAR(100) UNIQUE NOT NULL,
    farmer_id VARCHAR(100),
    sponsor_id VARCHAR(100),
    
    -- Request data
    leaf_top_url TEXT,
    leaf_bottom_url TEXT,
    plant_overview_url TEXT,
    root_url TEXT,
    location VARCHAR(255),
    crop_type VARCHAR(100),
    
    -- Result data
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    result JSONB,
    error_message TEXT,
    
    -- Processing metadata
    provider VARCHAR(20),
    processing_time_ms INTEGER,
    token_usage JSONB,
    cost_usd DECIMAL(10, 6),
    
    -- Timestamps
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    started_at TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,
    
    -- Indexes
    CONSTRAINT valid_status CHECK (status IN ('pending', 'processing', 'completed', 'failed'))
);

CREATE INDEX idx_analyses_farmer_id ON analyses(farmer_id);
CREATE INDEX idx_analyses_sponsor_id ON analyses(sponsor_id);
CREATE INDEX idx_analyses_status ON analyses(status);
CREATE INDEX idx_analyses_created_at ON analyses(created_at);
CREATE INDEX idx_analyses_provider ON analyses(provider);

-- Daily metrics table
CREATE TABLE daily_metrics (
    id SERIAL PRIMARY KEY,
    date DATE NOT NULL,
    provider VARCHAR(20) NOT NULL,
    
    total_requests INTEGER DEFAULT 0,
    successful_requests INTEGER DEFAULT 0,
    failed_requests INTEGER DEFAULT 0,
    
    total_tokens INTEGER DEFAULT 0,
    total_cost_usd DECIMAL(12, 4) DEFAULT 0,
    
    avg_processing_time_ms INTEGER,
    p95_processing_time_ms INTEGER,
    
    UNIQUE(date, provider)
);
```

### 15.3 Prompt Şablonu

Mevcut n8n flow'undaki prompt'un tamamı worker'lara taşınacak. Prompt, `shared/prompts/agricultural-analysis.ts` dosyasında tutulacak ve tüm provider'lar tarafından kullanılacak.

---

## Sonuç

Bu doküman, ZiraAI platformunun **günlük 1 milyon analiz** hedefine ulaşması için gerekli mimari değişiklikleri detaylandırmaktadır.

**Kritik Kararlar:**
1. n8n'den TypeScript native worker'lara geçiş
2. Multi-provider AI stratejisi (OpenAI + Gemini + Anthropic)
3. Redis tabanlı merkezi rate limiting
4. Railway üzerinde manuel scale yönetimi
5. Queue-per-provider izolasyon modeli

**Beklenen Sonuçlar:**
- 1,170,000+ günlük analiz kapasitesi
- %99.5+ uptime
- ~70 saniye ortalama response time
- ~$0.40 / analiz maliyeti

---

*Bu doküman, Claude Code ile devam edecek implementasyon çalışmaları için temel referans olarak kullanılacaktır.*
