# Admin Bulk Code Distribution (On Behalf Of Sponsor)

**Status:** ✅ Implemented - Uses Modern Bulk Distribution System
**Date:** 2025-11-09
**Pattern:** Unified endpoint with "on behalf of" capability

---

## 📊 Overview

Admin'ler artık sponsor adına Excel tabanlı, asenkron bulk code distribution yapabilir.
Bu endpoint sponsor'ların kullandığı modern sistemi kullanır (RabbitMQ + Worker + SignalR).

**Özellikler:**
- ✅ Excel upload (2000+ farmer desteği)
- ✅ Asenkron işleme (RabbitMQ + Worker Service)
- ✅ Real-time progress tracking (SignalR)
- ✅ Result file download
- ✅ SMS integration
- ✅ Admin audit logging

---

## 🎯 Unified Endpoint

### Upload Excel for Bulk Distribution

**Endpoint:** `POST /api/v1/sponsorship/bulk-code-distribution?onBehalfOfSponsorId={sponsorId}`

**Authorization:** Admin or Sponsor role required

**For Admin:**
```
Headers:
  Authorization: Bearer {admin_jwt_token}
  Content-Type: multipart/form-data

Query Parameters:
  onBehalfOfSponsorId: 159 (REQUIRED for Admin)

Body (form-data):
  excelFile: farmers.xlsx
  sendSms: true
```

**For Sponsor:**
```
Headers:
  Authorization: Bearer {sponsor_jwt_token}
  Content-Type: multipart/form-data

Body (form-data):
  excelFile: farmers.xlsx
  sendSms: true
```

**Response:**
```json
{
  "success": true,
  "data": {
    "jobId": 123,
    "totalFarmers": 150,
    "status": "Pending",
    "createdDate": "2025-11-09T10:00:00Z",
    "statusCheckUrl": "/api/v1/sponsorship/bulk-code-distribution/status/123"
  },
  "message": "Bulk code distribution job queued successfully"
}
```

**Detaylı dokümantasyon için:**
- [Admin Bulk Distribution Implementation Plan](./ADMIN_BULK_DISTRIBUTION_ON_BEHALF_OF.md)
- [Bulk Farmer Code Distribution Design](../BULK_FARMER_CODE_DISTRIBUTION_DESIGN.md)

---

**Document Version:** 2.0
**Last Updated:** 2025-11-09
**Status:** ✅ Implemented
