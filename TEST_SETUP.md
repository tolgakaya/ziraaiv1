# Asenkron PlantAnalysis Test Rehberi

## 1. Ön Gereksinimler

### RabbitMQ Kurulumu
```bash
# Docker ile (Önerilen)
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# Web UI: http://localhost:15672 (guest/guest)
```

### Database Migration & Seeds
```bash
# Migration'ları çalıştır
dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext

# Seeds manual olarak eklenecek (Configuration tablosuna)
```

## 2. Test Adımları

### Step 1: API Başlatma
```bash
cd WebAPI
dotnet run
# API: https://localhost:5001
# Worker Service otomatik başlar
```

### Step 2: Async Endpoint Test
```bash
# POST https://localhost:5001/api/plantanalyses/analyze-async
# Content-Type: application/json

{
  "image": "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD...", 
  "farmer_id": "farmer_test_001",
  "sponsor_id": "sponsor_test_xyz",
  "location": "Antalya, Turkey",
  "gps_coordinates": {
    "lat": 36.8969,
    "lng": 30.7133
  },
  "crop_type": "tomato",
  "field_id": "field_test_01",
  "urgency_level": "high",
  "notes": "Test mesajı - yapraklarda sararma var"
}
```

### Step 3: RabbitMQ Kontrol
```bash
# Web UI: http://localhost:15672
# Queues sekmesinde "plant-analysis-requests" kuyruğunu kontrol et
```

### Step 4: Mock N8N Response
```bash
# POST https://localhost:5001/mock-n8n-response (Manuel test için)
# Bu endpoint'i aşağıda oluşturacağız
```

### Step 5: Worker Logs Kontrol
```bash
# Console output'ta şu log'ları arayın:
# "Plant Analysis Result Worker starting..."
# "Starting to consume messages from queue: plant-analysis-requests"
# "Processing plant analysis result for ID: ..."
```

### Step 6: Database Kontrol
```sql
-- PostgreSQL'de kontrol et
SELECT * FROM "PlantAnalyses" ORDER BY "CreatedDate" DESC LIMIT 5;
```

### Step 7: Notification Test
```bash
# GET https://localhost:5001/api/notification/analysis-status/{analysisId}
```

## 3. Test Data

### Minimal Test Image (Base64)
```
data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/2wBDAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/wAARCAABAAEDAREAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAv/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwA/AB
```

## 4. Beklenen Sonuçlar

### Başarılı Request Response:
```json
{
  "success": true,
  "analysis_id": "async_analysis_20250812_123456_abc12345",
  "estimated_processing_time": "2-5 minutes",
  "status_check_endpoint": "/api/plantanalyses/status/async_analysis_20250812_123456_abc12345"
}
```

### Worker Log Output:
```
[INFO] Plant Analysis Result Worker starting...
[INFO] Starting to consume messages from queue: plant-analysis-requests
[INFO] Processing plant analysis result for ID: async_analysis_20250812_123456_abc12345
[INFO] Successfully saved plant analysis result: async_analysis_20250812_123456_abc12345
```