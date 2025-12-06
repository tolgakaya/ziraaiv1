# API Documentation - Production Readiness

**Project:** ZiraAI Platform Modernization
**Branch:** feature/production-readiness
**Version:** 1.0
**Last Updated:** 30 Kasım 2025

---

## 📋 Genel Bilgiler

### Base URLs
- **Railway Staging:** `https://ziraai-api-staging.up.railway.app`
- **Production:** `https://api.ziraai.com`

### Authentication
Tüm endpoint'ler JWT Bearer token gerektirir:
```
Authorization: Bearer <jwt_token>
```

### API Versiyonlama
- **Farmer Endpoints:** `/api/v1/` (versiyonlu)
- **Admin Endpoints:** `/api/admin/` (versiyonsuz)

---

## 🚀 Yeni Endpoint'ler

*(Bu bölüm implementasyon sırasında güncellenecek)*

### Planlanan Endpoint'ler

#### 1. Worker System Status
*(Phase 1 - Implementation sonrası eklenecek)*

#### 2. Provider Management
*(Phase 2 - Implementation sonrası eklenecek)*

#### 3. Admin Dashboard API
*(Phase 3 - Implementation sonrası eklenecek)*

---

## 📝 Endpoint Template

Her yeni endpoint için bu template kullanılacak:

```markdown
### [Endpoint Name]

**Endpoint:** `[METHOD] /api/v1/endpoint-path`
**Authorization:** Required/Optional
**Roles:** Admin | Sponsor | Farmer
**Version:** v1 | v2 | none

#### Amaç
[Bu endpoint ne için kullanılacak]

#### Kullanım Senaryosu
1. [Senaryo adım 1]
2. [Senaryo adım 2]
3. [Senaryo adım 3]

#### Request

**Headers:**
```json
{
  "Authorization": "Bearer <token>",
  "Content-Type": "application/json"
}
```

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| param1 | string | Yes | [Açıklama] |
| param2 | int | No | [Açıklama] |

**Body:**
```json
{
  "field1": "value",
  "field2": 123,
  "nested": {
    "field3": true
  }
}
```

#### Response

**Success (200/201):**
```json
{
  "success": true,
  "data": {
    "id": "123",
    "field": "value"
  },
  "message": "Operation successful"
}
```

**Error (400):**
```json
{
  "success": false,
  "message": "Validation error",
  "errors": [
    {
      "field": "field1",
      "message": "Field is required"
    }
  ]
}
```

**Error (401):**
```json
{
  "success": false,
  "message": "Unauthorized"
}
```

**Error (403):**
```json
{
  "success": false,
  "message": "Forbidden - insufficient permissions"
}
```

**Error (500):**
```json
{
  "success": false,
  "message": "Internal server error",
  "error": "Error details"
}
```

#### Validation Rules
- field1: Required, min 3 chars, max 100 chars
- field2: Required, range 1-1000
- field3: Optional, boolean

#### Security
- [SecuredOperation attribute ile korunan claim]
- Rate limit: [X requests per minute]

#### Examples

**cURL:**
```bash
curl -X POST https://ziraai-api-staging.up.railway.app/api/v1/endpoint \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "field1": "value",
    "field2": 123
  }'
```

**Postman Collection:**
[Link to Postman collection item]

#### Notes
- [Önemli not 1]
- [Önemli not 2]

#### Frontend/Mobile Integration
**React Native örnek:**
```typescript
const response = await fetch(
  'https://api.ziraai.com/api/v1/endpoint',
  {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      field1: 'value',
      field2: 123
    })
  }
);

const data = await response.json();
```

**Response TypeScript Interface:**
```typescript
interface EndpointResponse {
  success: boolean;
  data: {
    id: string;
    field: string;
  };
  message: string;
}
```
```

---

## 📊 Changelog

### 2025-11-30
- Initial API documentation structure created
- Template defined for new endpoints
- Waiting for implementation to begin

---

**Sonraki Güncelleme:** Phase 1 implementasyonu tamamlandığında
