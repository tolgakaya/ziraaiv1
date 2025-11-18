# Ticketing System API Documentation

Bu dokuman, musteri destek sistemi (Ticketing System) API endpointlerini frontend ve mobil ekipler icin detayli olarak aciklamaktadir.

## Genel Bilgiler

- **Base URL (User)**: `/api/v1/tickets`
- **Base URL (Admin)**: `/api/admin/admintickets`
- **Authentication**: JWT Bearer Token
- **Content-Type**: `application/json`

---

## User Endpoints (Farmer/Sponsor)

### 1. Create Ticket
Yeni destek talebi olusturur.

**Endpoint**: `POST /api/v1/tickets`

**Authorization**: `Farmer, Sponsor`

**Request Body**:
```json
{
  "subject": "string (zorunlu, max 200 karakter)",
  "description": "string (zorunlu, max 2000 karakter)",
  "category": "string (Technical|Billing|Account|General)",
  "priority": "string (Low|Normal|High) - default: Normal"
}
```

**Response (Success - 200)**:
```json
{
  "data": 123,
  "success": true,
  "message": "Destek talebi basariyla olusturuldu."
}
```

**Response (Error - 400)**:
```json
{
  "data": 0,
  "success": false,
  "message": "Gecersiz kategori. Gecerli degerler: Technical, Billing, Account, General"
}
```

---

### 2. Get My Tickets
Kullanicinin kendi destek taleplerini listeler.

**Endpoint**: `GET /api/v1/tickets`

**Authorization**: `Farmer, Sponsor`

**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| status | string | No | Filter by status (Open, InProgress, Resolved, Closed) |
| category | string | No | Filter by category (Technical, Billing, Account, General) |

**Response (Success - 200)**:
```json
{
  "data": {
    "tickets": [
      {
        "id": 1,
        "subject": "Bitki analizi calismadi",
        "category": "Technical",
        "priority": "Normal",
        "status": "Open",
        "createdDate": "2024-01-15T10:30:00",
        "lastResponseDate": null,
        "hasUnreadMessages": false
      }
    ],
    "totalCount": 1
  },
  "success": true,
  "message": null
}
```

---

### 3. Get Ticket Detail
Belirli bir destek talebinin detaylarini ve mesajlarini getirir.

**Endpoint**: `GET /api/v1/tickets/{ticketId}`

**Authorization**: `Farmer, Sponsor`

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| ticketId | int | Ticket ID |

**Response (Success - 200)**:
```json
{
  "data": {
    "id": 1,
    "subject": "Bitki analizi calismadi",
    "description": "Fotografimi yukledim ama sonuc gelmedi",
    "category": "Technical",
    "priority": "Normal",
    "status": "InProgress",
    "createdDate": "2024-01-15T10:30:00",
    "updatedDate": "2024-01-15T14:00:00",
    "resolvedDate": null,
    "closedDate": null,
    "resolutionNotes": null,
    "satisfactionRating": null,
    "satisfactionFeedback": null,
    "messages": [
      {
        "id": 1,
        "message": "Yardimci olabilir misiniz?",
        "isAdminResponse": false,
        "createdDate": "2024-01-15T10:30:00"
      },
      {
        "id": 2,
        "message": "Sorununuzu inceliyoruz.",
        "isAdminResponse": true,
        "createdDate": "2024-01-15T14:00:00"
      }
    ]
  },
  "success": true,
  "message": null
}
```

**Response (Error - 404)**:
```json
{
  "data": null,
  "success": false,
  "message": "Destek talebi bulunamadi."
}
```

---

### 4. Add Message to Ticket
Mevcut destek talebine mesaj ekler.

**Endpoint**: `POST /api/v1/tickets/{ticketId}/messages`

**Authorization**: `Farmer, Sponsor`

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| ticketId | int | Ticket ID |

**Request Body**:
```json
{
  "message": "string (zorunlu, max 2000 karakter)"
}
```

**Response (Success - 200)**:
```json
{
  "success": true,
  "message": "Mesaj basariyla eklendi."
}
```

**Response (Error - 400)**:
```json
{
  "success": false,
  "message": "Kapatilmis destek talebine mesaj eklenemez."
}
```

---

### 5. Close Ticket
Kullanicinin kendi destek talebini kapatir.

**Endpoint**: `POST /api/v1/tickets/{ticketId}/close`

**Authorization**: `Farmer, Sponsor`

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| ticketId | int | Ticket ID |

**Response (Success - 200)**:
```json
{
  "success": true,
  "message": "Destek talebi basariyla kapatildi."
}
```

---

### 6. Rate Resolution
Cozulmus destek talebini puanlar (1-5 yildiz).

**Endpoint**: `POST /api/v1/tickets/{ticketId}/rate`

**Authorization**: `Farmer, Sponsor`

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| ticketId | int | Ticket ID |

**Request Body**:
```json
{
  "rating": 5,
  "feedback": "string (optional, max 500 karakter)"
}
```

**Response (Success - 200)**:
```json
{
  "success": true,
  "message": "Puanlamaniz icin tesekkur ederiz."
}
```

**Response (Error - 400)**:
```json
{
  "success": false,
  "message": "Sadece cozulmus veya kapatilmis destek talepleri puanlanabilir."
}
```

---

## Admin Endpoints

### 7. Get All Tickets
Tum destek taleplerini listeler (filtreleme destegi ile).

**Endpoint**: `GET /api/admin/admintickets`

**Authorization**: `Admin`

**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| status | string | No | Filter by status |
| category | string | No | Filter by category |
| priority | string | No | Filter by priority |

**Response (Success - 200)**:
```json
{
  "data": {
    "tickets": [
      {
        "id": 1,
        "userId": 42,
        "userName": "Ahmet Yilmaz",
        "userRole": "Farmer",
        "subject": "Bitki analizi calismadi",
        "category": "Technical",
        "priority": "High",
        "status": "Open",
        "assignedToUserId": null,
        "assignedToUserName": null,
        "createdDate": "2024-01-15T10:30:00",
        "lastResponseDate": null,
        "messageCount": 1
      }
    ],
    "totalCount": 1
  },
  "success": true,
  "message": null
}
```

---

### 8. Get Ticket Statistics
Destek talebi istatistiklerini getirir.

**Endpoint**: `GET /api/admin/admintickets/stats`

**Authorization**: `Admin`

**Response (Success - 200)**:
```json
{
  "data": {
    "openCount": 15,
    "inProgressCount": 8,
    "resolvedCount": 42,
    "closedCount": 120,
    "totalCount": 185
  },
  "success": true,
  "message": null
}
```

---

### 9. Get Ticket Detail (Admin)
Admin icin talep detayini getirir (dahili notlar dahil).

**Endpoint**: `GET /api/admin/admintickets/{ticketId}`

**Authorization**: `Admin`

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| ticketId | int | Ticket ID |

**Response (Success - 200)**:
```json
{
  "data": {
    "id": 1,
    "userId": 42,
    "userName": "Ahmet Yilmaz",
    "userEmail": "ahmet@example.com",
    "userRole": "Farmer",
    "subject": "Bitki analizi calismadi",
    "description": "Fotografimi yukledim ama sonuc gelmedi",
    "category": "Technical",
    "priority": "High",
    "status": "InProgress",
    "assignedToUserId": 1,
    "assignedToUserName": "Admin User",
    "createdDate": "2024-01-15T10:30:00",
    "updatedDate": "2024-01-15T14:00:00",
    "resolvedDate": null,
    "closedDate": null,
    "resolutionNotes": null,
    "satisfactionRating": null,
    "satisfactionFeedback": null,
    "messages": [
      {
        "id": 1,
        "fromUserId": 42,
        "fromUserName": "Ahmet Yilmaz",
        "message": "Yardimci olabilir misiniz?",
        "isAdminResponse": false,
        "isInternal": false,
        "isRead": true,
        "readDate": "2024-01-15T14:00:00",
        "createdDate": "2024-01-15T10:30:00"
      },
      {
        "id": 2,
        "fromUserId": 1,
        "fromUserName": "Admin User",
        "message": "Dahili not: Log dosyalarini kontrol et",
        "isAdminResponse": true,
        "isInternal": true,
        "isRead": false,
        "readDate": null,
        "createdDate": "2024-01-15T14:05:00"
      }
    ]
  },
  "success": true,
  "message": null
}
```

---

### 10. Assign Ticket
Destek talebini bir admin kullanicisina atar.

**Endpoint**: `POST /api/admin/admintickets/{ticketId}/assign`

**Authorization**: `Admin`

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| ticketId | int | Ticket ID |

**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| assignedToUserId | int | No | Admin user ID (null to unassign) |

**Response (Success - 200)**:
```json
{
  "success": true,
  "message": "Destek talebi basariyla atandi."
}
```

---

### 11. Respond to Ticket (Admin)
Admin olarak destek talebine yanit verir.

**Endpoint**: `POST /api/admin/admintickets/{ticketId}/respond`

**Authorization**: `Admin`

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| ticketId | int | Ticket ID |

**Request Body**:
```json
{
  "message": "string (zorunlu, max 2000 karakter)",
  "isInternal": false
}
```

**Notes**:
- `isInternal: true` olursa mesaj sadece adminler tarafindan gorulebilir
- `isInternal: false` olursa mesaj kullaniciya da gosterilir

**Response (Success - 200)**:
```json
{
  "success": true,
  "message": "Yanit basariyla gonderildi."
}
```

---

### 12. Update Ticket Status
Destek talebi durumunu gunceller.

**Endpoint**: `PUT /api/admin/admintickets/{ticketId}/status`

**Authorization**: `Admin`

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| ticketId | int | Ticket ID |

**Request Body**:
```json
{
  "status": "string (Open|InProgress|Resolved|Closed)",
  "resolutionNotes": "string (Resolved durumu icin zorunlu, max 1000 karakter)"
}
```

**Response (Success - 200)**:
```json
{
  "success": true,
  "message": "Destek talebi durumu basariyla guncellendi."
}
```

**Response (Error - 400)**:
```json
{
  "success": false,
  "message": "Cozum notlari zorunludur."
}
```

---

## Status Values

| Status | Description |
|--------|-------------|
| Open | Yeni acilmis, henuz islem yapilmamis |
| InProgress | Uzerinde calisiliyor |
| Resolved | Cozuldu, kullanici onayini bekliyor |
| Closed | Kapatildi |

## Category Values

| Category | Description |
|----------|-------------|
| Technical | Teknik sorunlar (analiz hatalari, vb.) |
| Billing | Fatura ve odeme sorunlari |
| Account | Hesap ile ilgili sorunlar |
| General | Genel sorular ve oneriler |

## Priority Values

| Priority | Description |
|----------|-------------|
| Low | Dusuk oncelik |
| Normal | Normal oncelik (default) |
| High | Yuksek oncelik |

---

## Error Codes

| HTTP Code | Description |
|-----------|-------------|
| 200 | Basarili |
| 400 | Gecersiz istek (validasyon hatasi) |
| 401 | Yetkisiz erisim (JWT eksik/gecersiz) |
| 403 | Erisim yasak (yetki yok) |
| 404 | Kaynak bulunamadi |

---

## Notes for Implementation

1. **Unread Messages**: `hasUnreadMessages` alani kullaniciya okunmamis admin mesaji oldugunu gosterir
2. **Internal Notes**: `isInternal: true` olan mesajlar sadece admin panelinde gosterilmeli
3. **Auto-assignment**: Admin yanit verdiginde, atanmamissa otomatik olarak kendine atanir
4. **Status Flow**: Open -> InProgress -> Resolved -> Closed (tipik akis)
5. **Rating**: Sadece Resolved veya Closed durumundaki talepler puanlanabilir
