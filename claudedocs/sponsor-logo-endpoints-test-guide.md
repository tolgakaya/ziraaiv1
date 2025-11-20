# Sponsor Logo Endpoints - Test Guide

## Implementation Summary

Sponsor logo upload system has been successfully implemented, mirroring the avatar upload architecture:

### Created Files:
- ✅ `Business/Services/Sponsor/ISponsorLogoService.cs` - Service interface
- ✅ `Business/Services/Sponsor/SponsorLogoService.cs` - Service implementation
- ✅ `Entities/Dtos/SponsorLogoDto.cs` - DTO for responses
- ✅ `Business/Handlers/SponsorProfiles/Commands/UploadSponsorLogoCommand.cs` - Upload command
- ✅ `Business/Handlers/SponsorProfiles/Commands/DeleteSponsorLogoCommand.cs` - Delete command
- ✅ `Business/Handlers/SponsorProfiles/Queries/GetSponsorLogoQuery.cs` - Retrieval query

### Modified Files:
- ✅ `Entities/Concrete/SponsorProfile.cs` - Added `SponsorLogoThumbnailUrl` field
- ✅ `DataAccess/Concrete/Configurations/SponsorProfileEntityConfiguration.cs` - Added EF config
- ✅ `WebAPI/Controllers/SponsorshipController.cs` - Added 3 endpoints
- ✅ `Business/DependencyResolvers/AutofacBusinessModule.cs` - Registered service
- ✅ Database migration applied manually

---

## Endpoints

### 1. Upload Sponsor Logo
**Endpoint**: `POST /api/v1/sponsorship/logo`  
**Authorization**: Required (Sponsor role)  
**Content-Type**: `multipart/form-data`

**Request:**
```http
POST /api/v1/sponsorship/logo HTTP/1.1
Host: localhost:5001
Authorization: Bearer {SPONSOR_TOKEN}
Content-Type: multipart/form-data
x-dev-arch-version: 1.0

file: [binary image file]
```

**Supported Formats:**
- `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`, `.svg`
- Maximum size: 5MB
- Output: 512x512 (full), 128x128 (thumbnail)
- SVG: No resize, original preserved

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Logo uploaded successfully",
  "data": {
    "logoUrl": "https://storage.example.com/sponsors/159/logo_512.jpg",
    "thumbnailUrl": "https://storage.example.com/sponsors/159/logo_128.jpg"
  }
}
```

**Error Responses:**
- `400 Bad Request`: Invalid file format or size
- `401 Unauthorized`: Missing or invalid token
- `500 Internal Server Error`: File upload failed

---

### 2. Get Sponsor Logo
**Endpoint**: `GET /api/v1/sponsorship/logo/{sponsorId?}`  
**Authorization**: Optional (public endpoint)

**Request:**
```http
GET /api/v1/sponsorship/logo/159 HTTP/1.1
Host: localhost:5001
x-dev-arch-version: 1.0
```

**Get Own Logo (authenticated):**
```http
GET /api/v1/sponsorship/logo HTTP/1.1
Host: localhost:5001
Authorization: Bearer {SPONSOR_TOKEN}
x-dev-arch-version: 1.0
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "sponsorId": 159,
    "logoUrl": "https://storage.example.com/sponsors/159/logo_512.jpg",
    "thumbnailUrl": "https://storage.example.com/sponsors/159/logo_128.jpg",
    "updatedDate": "2025-01-26T12:30:45Z"
  }
}
```

**Error Responses:**
- `404 Not Found`: Sponsor not found or no logo uploaded
- `400 Bad Request`: Invalid sponsor ID

---

### 3. Delete Sponsor Logo
**Endpoint**: `DELETE /api/v1/sponsorship/logo`  
**Authorization**: Required (Sponsor role, own resource only)

**Request:**
```http
DELETE /api/v1/sponsorship/logo HTTP/1.1
Host: localhost:5001
Authorization: Bearer {SPONSOR_TOKEN}
x-dev-arch-version: 1.0
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Logo deleted successfully"
}
```

**Error Responses:**
- `401 Unauthorized`: Missing or invalid token
- `404 Not Found`: No logo found to delete
- `500 Internal Server Error`: Deletion failed

---

## Test Scenarios

### Prerequisites:
1. PostgreSQL running on `localhost:5432`
2. API running on `https://localhost:5001`
3. Valid sponsor user with authentication token
4. Test image files (JPEG, PNG, SVG)

### Test Case 1: Upload Logo (Success)
```bash
# Using curl
curl -X POST "https://localhost:5001/api/v1/sponsorship/logo" \
  -H "Authorization: Bearer {SPONSOR_TOKEN}" \
  -H "x-dev-arch-version: 1.0" \
  -F "file=@test-logo.png"
```

**Expected Result:**
- Status: 200 OK
- Response contains `logoUrl` and `thumbnailUrl`
- Database: `SponsorProfiles.SponsorLogoUrl` and `SponsorLogoThumbnailUrl` updated
- Storage: Two files uploaded (512x512 and 128x128)

---

### Test Case 2: Upload Logo (Invalid Format)
```bash
curl -X POST "https://localhost:5001/api/v1/sponsorship/logo" \
  -H "Authorization: Bearer {SPONSOR_TOKEN}" \
  -H "x-dev-arch-version: 1.0" \
  -F "file=@document.pdf"
```

**Expected Result:**
- Status: 400 Bad Request
- Error message: "Invalid file format. Allowed formats: jpg, jpeg, png, gif, webp, svg"

---

### Test Case 3: Upload Logo (Too Large)
```bash
curl -X POST "https://localhost:5001/api/v1/sponsorship/logo" \
  -H "Authorization: Bearer {SPONSOR_TOKEN}" \
  -H "x-dev-arch-version: 1.0" \
  -F "file=@large-image-10mb.jpg"
```

**Expected Result:**
- Status: 400 Bad Request
- Error message: "File size exceeds maximum limit of 5MB"

---

### Test Case 4: Upload SVG Logo
```bash
curl -X POST "https://localhost:5001/api/v1/sponsorship/logo" \
  -H "Authorization: Bearer {SPONSOR_TOKEN}" \
  -H "x-dev-arch-version: 1.0" \
  -F "file=@logo.svg"
```

**Expected Result:**
- Status: 200 OK
- Both `logoUrl` and `thumbnailUrl` point to same SVG file (no resize)
- Original SVG preserved

---

### Test Case 5: Get Logo (Public Access)
```bash
curl -X GET "https://localhost:5001/api/v1/sponsorship/logo/159" \
  -H "x-dev-arch-version: 1.0"
```

**Expected Result:**
- Status: 200 OK
- Response contains sponsor logo information
- No authentication required

---

### Test Case 6: Get Own Logo (Authenticated)
```bash
curl -X GET "https://localhost:5001/api/v1/sponsorship/logo" \
  -H "Authorization: Bearer {SPONSOR_TOKEN}" \
  -H "x-dev-arch-version: 1.0"
```

**Expected Result:**
- Status: 200 OK
- Response contains authenticated sponsor's logo information
- `sponsorId` matches token's user ID

---

### Test Case 7: Delete Logo (Success)
```bash
curl -X DELETE "https://localhost:5001/api/v1/sponsorship/logo" \
  -H "Authorization: Bearer {SPONSOR_TOKEN}" \
  -H "x-dev-arch-version: 1.0"
```

**Expected Result:**
- Status: 200 OK
- Database: `SponsorLogoUrl` and `SponsorLogoThumbnailUrl` set to NULL
- Storage: Files removed from file storage provider

---

### Test Case 8: Delete Logo (No Logo)
```bash
# When sponsor has no logo uploaded
curl -X DELETE "https://localhost:5001/api/v1/sponsorship/logo" \
  -H "Authorization: Bearer {SPONSOR_TOKEN}" \
  -H "x-dev-arch-version: 1.0"
```

**Expected Result:**
- Status: 404 Not Found
- Error message: "No logo found to delete"

---

### Test Case 9: Upload Without Authentication
```bash
curl -X POST "https://localhost:5001/api/v1/sponsorship/logo" \
  -H "x-dev-arch-version: 1.0" \
  -F "file=@test-logo.png"
```

**Expected Result:**
- Status: 401 Unauthorized
- Error message: "User not authenticated"

---

### Test Case 10: Replace Existing Logo
```bash
# Upload first logo
curl -X POST "https://localhost:5001/api/v1/sponsorship/logo" \
  -H "Authorization: Bearer {SPONSOR_TOKEN}" \
  -H "x-dev-arch-version: 1.0" \
  -F "file=@logo1.png"

# Upload second logo (should replace first)
curl -X POST "https://localhost:5001/api/v1/sponsorship/logo" \
  -H "Authorization: Bearer {SPONSOR_TOKEN}" \
  -H "x-dev-arch-version: 1.0" \
  -F "file=@logo2.png"
```

**Expected Result:**
- Old logo files deleted from storage
- New logo uploaded successfully
- Database updated with new URLs

---

## Database Verification

### Check SponsorProfile after upload:
```sql
SELECT 
    "Id",
    "SponsorId",
    "CompanyName",
    "SponsorLogoUrl",
    "SponsorLogoThumbnailUrl",
    "UpdatedDate"
FROM "SponsorProfiles"
WHERE "SponsorId" = 159;
```

**Expected:**
- `SponsorLogoUrl`: Full size image URL (512x512)
- `SponsorLogoThumbnailUrl`: Thumbnail image URL (128x128)
- `UpdatedDate`: Recent timestamp

---

## File Storage Verification

### Local Storage:
Check files in `wwwroot/uploads/sponsor-logos/`:
```bash
ls -lh wwwroot/uploads/sponsor-logos/
# Should show:
# sponsor_159_logo_512.jpg
# sponsor_159_logo_128.jpg
```

### ImgBB/FreeImageHost/S3:
- Verify URLs are accessible
- Check image dimensions match expected sizes
- Confirm old images removed after replacement

---

## Postman Collection

Add these requests to your existing ZiraAI Postman collection:

**Collection**: `Sponsorship`  
**Folder**: `Logo Management`

1. **Upload Sponsor Logo**
   - Method: POST
   - URL: `{{baseUrl}}/api/v1/sponsorship/logo`
   - Headers: `Authorization: Bearer {{sponsorToken}}`, `x-dev-arch-version: 1.0`
   - Body: form-data with `file` key

2. **Get Sponsor Logo**
   - Method: GET
   - URL: `{{baseUrl}}/api/v1/sponsorship/logo/{{sponsorId}}`
   - Headers: `x-dev-arch-version: 1.0`

3. **Get Own Logo**
   - Method: GET
   - URL: `{{baseUrl}}/api/v1/sponsorship/logo`
   - Headers: `Authorization: Bearer {{sponsorToken}}`, `x-dev-arch-version: 1.0`

4. **Delete Sponsor Logo**
   - Method: DELETE
   - URL: `{{baseUrl}}/api/v1/sponsorship/logo`
   - Headers: `Authorization: Bearer {{sponsorToken}}`, `x-dev-arch-version: 1.0`

---

## Technical Details

### Image Processing:
- Library: `SixLabors.ImageSharp`
- Full size: 512x512 pixels
- Thumbnail: 128x128 pixels
- Format: JPEG (quality 85%) for raster images
- SVG: Original preserved, no processing

### Service Layer:
- `SponsorLogoService` handles all business logic
- Automatic cleanup of old logos on upload
- Validates file size, format, and permissions
- Uses `IFileStorageService` abstraction for storage

### Security:
- JWT authentication required for upload/delete
- Authorization check: Only owner can upload/delete
- Public GET access for transparency
- File type validation prevents malicious uploads
- Size limit prevents DoS attacks

---

## Comparison with Avatar System

Both systems follow the same pattern:

| Feature | Avatar System | Sponsor Logo System |
|---------|--------------|-------------------|
| **Endpoint Base** | `/api/v1/users/avatar` | `/api/v1/sponsorship/logo` |
| **Service** | `AvatarService` | `SponsorLogoService` |
| **Full Size** | 512x512 | 512x512 |
| **Thumbnail** | 128x128 | 128x128 |
| **Max Size** | 5MB | 5MB |
| **Formats** | jpg, png, gif, webp, svg | jpg, png, gif, webp, svg |
| **Authorization** | User must own profile | Sponsor must own profile |
| **Public GET** | ✅ Yes | ✅ Yes |
| **SVG Support** | ✅ Yes | ✅ Yes |

---

## Next Steps

1. ✅ Database migration applied
2. ✅ Code implementation complete
3. ⏳ **Manual testing required** (PostgreSQL connection needed)
4. ⏳ Update Postman collection with new endpoints
5. ⏳ Test all scenarios in staging environment
6. ⏳ Document in API documentation / Swagger

---

## Notes

- Implementation follows existing patterns from `AvatarService`
- All files created successfully and integrated
- Build completed without errors
- Ready for testing once PostgreSQL is available
