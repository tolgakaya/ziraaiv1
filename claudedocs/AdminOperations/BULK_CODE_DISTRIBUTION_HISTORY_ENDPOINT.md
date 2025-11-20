# Bulk Code Distribution Job History Endpoint

## Overview
Admin endpoint to query and view bulk code distribution job history with pagination, filtering, and sponsor information.

## Endpoint Details

### Admin Endpoint
```
GET /api/admin/sponsorship/bulk-code-distribution/history
```

**Authorization**: Admin role required
**Method**: GET

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `page` | int | No | 1 | Page number for pagination |
| `pageSize` | int | No | 50 | Number of jobs per page |
| `status` | string | No | null | Filter by job status: `Pending`, `Processing`, `Completed`, `PartialSuccess`, `Failed` |
| `sponsorId` | int | No | null | Filter jobs by specific sponsor ID |
| `startDate` | DateTime | No | null | Filter jobs created after this date (inclusive) |
| `endDate` | DateTime | No | null | Filter jobs created before this date (inclusive) |

### Response Structure

**Success Response (200 OK)**:
```json
{
  "data": {
    "totalCount": 45,
    "page": 1,
    "pageSize": 50,
    "totalPages": 1,
    "jobs": [
      {
        "jobId": 123,
        "sponsorId": 456,
        "sponsorName": "John Doe",
        "sponsorEmail": "sponsor@example.com",
        "purchaseId": 789,
        "deliveryMethod": "Both",
        "totalFarmers": 150,
        "processedFarmers": 150,
        "successfulDistributions": 145,
        "failedDistributions": 5,
        "status": "Completed",
        "createdDate": "2025-11-08T14:30:00Z",
        "startedDate": "2025-11-08T14:30:05Z",
        "completedDate": "2025-11-08T14:35:20Z",
        "originalFileName": "farmers_batch_nov.xlsx",
        "fileSize": 52480,
        "resultFileUrl": "https://storage.example.com/results/job_123.xlsx",
        "totalCodesDistributed": 145,
        "totalSmsSent": 145
      }
    ]
  },
  "success": true,
  "message": "Retrieved 1 jobs (Page 1/1, Total: 45)"
}
```

## Request Examples

### 1. Get All Jobs (Default Pagination)
```bash
GET /api/admin/sponsorship/bulk-code-distribution/history
```

### 2. Filter by Status
```bash
GET /api/admin/sponsorship/bulk-code-distribution/history?status=Completed
```

### 3. Filter by Sponsor
```bash
GET /api/admin/sponsorship/bulk-code-distribution/history?sponsorId=456
```

### 4. Filter by Date Range
```bash
GET /api/admin/sponsorship/bulk-code-distribution/history?startDate=2025-11-01&endDate=2025-11-09
```

### 5. Combined Filters with Pagination
```bash
GET /api/admin/sponsorship/bulk-code-distribution/history?page=1&pageSize=20&status=Completed&sponsorId=456&startDate=2025-11-01
```

## Status Values

| Status | Description |
|--------|-------------|
| `Pending` | Job created, waiting to be processed |
| `Processing` | Job currently being processed by worker |
| `Completed` | All farmers processed successfully |
| `PartialSuccess` | Some farmers processed, some failed |
| `Failed` | Job failed to process |

## Delivery Methods

| Method | Description |
|--------|-------------|
| `Direct` | Codes marked as distributed only (no SMS) |
| `SMS` | Codes sent via SMS only |
| `Both` | Codes marked as distributed AND sent via SMS |

## Field Descriptions

### Job Fields

- **jobId**: Unique job identifier (int, NOT Guid)
- **sponsorId**: Sponsor user ID who initiated the job
- **sponsorName**: Full name of sponsor from User table
- **sponsorEmail**: Email of sponsor from User table
- **purchaseId**: Sponsorship purchase ID used for code distribution
- **deliveryMethod**: How codes were delivered (Direct/SMS/Both)
- **totalFarmers**: Total number of farmers in uploaded Excel
- **processedFarmers**: Number of farmers processed so far
- **successfulDistributions**: Number of successful code distributions
- **failedDistributions**: Number of failed code distributions
- **status**: Current job status
- **createdDate**: When job was created
- **startedDate**: When processing started (nullable)
- **completedDate**: When processing finished (nullable)
- **originalFileName**: Name of uploaded Excel file
- **fileSize**: Size of uploaded file in bytes
- **resultFileUrl**: Download URL for result Excel file (nullable)
- **totalCodesDistributed**: Total codes distributed
- **totalSmsSent**: Total SMS messages sent

### Pagination Fields

- **totalCount**: Total number of jobs matching filters
- **page**: Current page number
- **pageSize**: Number of jobs per page
- **totalPages**: Total number of pages

## Important Notes for Frontend/Mobile Teams

### ⚠️ Data Type Differences from Spec

The spec document `BULK_CODE_DISTRIBUTION_JOB_QUEUE_SPEC.md` shows `jobId` as `Guid`, but the **actual implementation uses `int`**. This is because:

1. The existing `BulkCodeDistributionJob` entity uses `int Id` (NOT Guid)
2. The RabbitMQ-based job processing system already works with int IDs
3. No entity changes were made to preserve system stability
4. All references in codebase use int, not Guid

**Action Required**: Update your client code to use `int` for `jobId`, not `Guid`.

### Integration with Existing Endpoints

This history endpoint complements the existing endpoints:

1. **POST /api/v1/sponsorship/bulk-code-distribution** - Create new job (upload Excel)
2. **GET /api/v1/sponsorship/bulk-code-distribution/status/{jobId}** - Poll single job status
3. **GET /api/admin/sponsorship/bulk-code-distribution/history** - NEW: Query job history

### Recommended Usage Pattern

Instead of continuous polling:

1. User uploads Excel → Receive `jobId` from POST response
2. Poll status endpoint 2-3 times during processing
3. After completion, use history endpoint to:
   - View all past jobs
   - Filter by date range
   - Check sponsor-specific jobs
   - Review job statistics

### Performance Considerations

- Default page size: 50 jobs
- Maximum recommended page size: 100 jobs
- Results ordered by `CreatedDate DESC` (newest first)
- Database indexes on: `SponsorId`, `Status`, `CreatedDate`

## Error Responses

### Unauthorized (401)
```json
{
  "success": false,
  "message": "Unauthorized"
}
```

### Forbidden (403) - Non-admin user
```json
{
  "success": false,
  "message": "Insufficient permissions"
}
```

### Bad Request (400) - Invalid filters
```json
{
  "success": false,
  "message": "Invalid status value. Must be: Pending, Processing, Completed, PartialSuccess, Failed"
}
```

## Database Schema

The endpoint queries the `BulkCodeDistributionJobs` table:

```sql
-- Relevant indexes for performance
CREATE INDEX IX_BulkCodeDistributionJobs_SponsorId ON BulkCodeDistributionJobs(SponsorId);
CREATE INDEX IX_BulkCodeDistributionJobs_Status ON BulkCodeDistributionJobs(Status);
CREATE INDEX IX_BulkCodeDistributionJobs_CreatedDate ON BulkCodeDistributionJobs(CreatedDate);
CREATE INDEX IX_BulkCodeDistributionJobs_SponsorId_CreatedDate ON BulkCodeDistributionJobs(SponsorId, CreatedDate);
```

## Testing

### Using cURL
```bash
# Get all jobs
curl -X GET "https://api.ziraai.com/api/admin/sponsorship/bulk-code-distribution/history" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"

# Filter by status
curl -X GET "https://api.ziraai.com/api/admin/sponsorship/bulk-code-distribution/history?status=Completed&page=1&pageSize=20" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

### Using Postman
1. Set method to GET
2. URL: `{{baseUrl}}/api/admin/sponsorship/bulk-code-distribution/history`
3. Add Authorization header with admin JWT token
4. Add query parameters as needed
5. Send request

## Version History

- **2025-11-09**: Initial implementation
  - Created `GetBulkCodeDistributionJobHistoryQuery` handler
  - Created `BulkCodeDistributionJobHistoryDto` and response DTO
  - Added controller endpoint in `AdminSponsorshipController`
  - Uses existing `BulkCodeDistributionJob` entity (no schema changes)
  - Returns int jobId (not Guid as in spec)
