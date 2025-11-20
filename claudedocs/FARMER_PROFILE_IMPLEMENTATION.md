# Farmer Profile Management - Secure Implementation Guide

## Overview
This document describes the **secure implementation** of Farmer profile management endpoints. All security vulnerabilities identified in the analysis phase have been addressed.

## What Was Fixed

### 🔒 Security Issues Resolved

1. **User Ownership Validation** ✅
   - JWT token-based user identification
   - Users can ONLY update their own profile
   - UserId cannot be manipulated in requests

2. **Sensitive Data Exposure** ✅
   - New `FarmerProfileDto` excludes Password and RefreshToken
   - Clean separation of concerns

3. **Input Validation** ✅
   - FluentValidation for all input fields
   - Email format validation
   - Phone number format validation
   - Field length constraints
   - BirthDate range validation

4. **Extended Updateable Fields** ✅
   - Now supports: FullName, Email, MobilePhones, BirthDate, Gender, Address, Notes
   - Previous limitation (6 fields) expanded to 7 fields

---

## New Implementation

### 🎯 New Endpoints

#### 1. Get Farmer Profile
```http
GET /api/v1/farmer/profile
Authorization: Bearer {JWT_TOKEN}
```

**Security**: 
- Requires authentication (`[Authorize]`)
- Requires `Farmer` or `Admin` role
- UserId automatically extracted from JWT token

**Response**:
```json
{
  "data": {
    "userId": 123,
    "citizenId": 12345678901,
    "fullName": "Ahmet Yılmaz",
    "email": "ahmet@example.com",
    "mobilePhones": "+90 532 123 4567",
    "birthDate": "1985-03-15T00:00:00",
    "gender": 1,
    "address": "İstanbul, Türkiye",
    "notes": "Test notları",
    "status": true,
    "isActive": true,
    "recordDate": "2024-01-01T10:00:00",
    "updateContactDate": "2024-12-01T15:30:00",
    "avatarUrl": "https://storage.example.com/avatars/123.jpg",
    "avatarThumbnailUrl": "https://storage.example.com/avatars/123_thumb.jpg",
    "avatarUpdatedDate": "2024-11-15T14:20:00",
    "registrationReferralCode": "REF123",
    "deactivatedDate": null,
    "deactivationReason": null
  },
  "success": true,
  "message": null
}
```

**Note**: Password, RefreshToken, and other sensitive fields are **NOT** exposed.

---

#### 2. Update Farmer Profile
```http
PUT /api/v1/farmer/profile
Authorization: Bearer {JWT_TOKEN}
Content-Type: application/json

{
  "fullName": "Ahmet Yılmaz",
  "email": "ahmet.yilmaz@example.com",
  "mobilePhones": "+90 532 123 4567",
  "birthDate": "1985-03-15",
  "gender": 1,
  "address": "İstanbul, Kadıköy",
  "notes": "Güncellenmiş notlar"
}
```

**Security**:
- Requires authentication (`[Authorize]`)
- Requires `Farmer` or `Admin` role
- UserId automatically extracted from JWT token (cannot be manipulated)
- Input validated via `UpdateFarmerProfileValidator`

**Request Body** (`UpdateFarmerProfileDto`):
```typescript
{
  "fullName": string,        // Required, 2-100 chars
  "email": string,           // Required, valid email, max 100 chars
  "mobilePhones": string,    // Required, 10-20 chars, valid phone format
  "birthDate": string?,      // Optional, must be valid past date
  "gender": number?,         // Optional, 0=Unspecified, 1=Male, 2=Female
  "address": string?,        // Optional, max 500 chars
  "notes": string?           // Optional, max 1000 chars
}
```

**Success Response**:
```json
{
  "success": true,
  "message": "Updated"
}
```

**Validation Error Response** (400 Bad Request):
```json
{
  "success": false,
  "message": "Validation errors",
  "errors": [
    "E-posta adresi boş olamaz.",
    "Geçerli bir e-posta adresi giriniz.",
    "Telefon numarası en az 10 karakter olmalıdır."
  ]
}
```

---

## 🏗️ Architecture

### Created Files

#### DTOs
1. **`Entities/Dtos/FarmerProfileDto.cs`**
   - Response DTO for profile data
   - Excludes sensitive fields (Password, RefreshToken)
   - Includes all safe user fields

2. **`Entities/Dtos/UpdateFarmerProfileDto.cs`**
   - Request DTO for profile updates
   - Does NOT include UserId (comes from JWT)
   - 7 updateable fields

#### Handlers

3. **`Business/Handlers/Farmers/Queries/GetFarmerProfileQuery.cs`**
   - Retrieves farmer profile by JWT userId
   - Uses LINQ projection for efficient queries
   - Returns `FarmerProfileDto`

4. **`Business/Handlers/Farmers/Commands/UpdateFarmerProfileCommand.cs`**
   - Updates farmer profile
   - **Security**: UserId comes from controller (JWT token)
   - Includes logging and cache invalidation aspects
   - Updates `UpdateContactDate` automatically

#### Validation

5. **`Business/Handlers/Farmers/ValidationRules/UpdateFarmerProfileValidator.cs`**
   - FluentValidation for all input fields
   - Email format validation
   - Phone number pattern validation (`^[0-9\s\-\+\(\)]+$`)
   - BirthDate range validation (not in future, within 150 years)
   - Gender validation (0, 1, or 2)
   - Field length constraints

#### Controller

6. **`WebAPI/Controllers/FarmerController.cs`**
   - Two endpoints: GET and PUT `/api/farmer/profile`
   - Role-based authorization: `Farmer` or `Admin`
   - JWT token parsing for secure user identification
   - Comprehensive XML documentation
   - HTTP status code attributes for Swagger

---

## 🔐 Security Features

### JWT-Based Authentication
```csharp
// Controller extracts userId from JWT token
var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

if (userId == 0)
{
    return Unauthorized(new { message = "Geçersiz kullanıcı token'ı." });
}

// Pass to command
var command = new UpdateFarmerProfileCommand
{
    UserId = userId, // From JWT, not user input ✅
    // ... other fields from request body
};
```

### Ownership Validation
```csharp
// Handler validates that user exists and belongs to JWT userId
var user = await _userRepository.GetAsync(u => u.UserId == request.UserId);

if (user == null)
{
    return new ErrorResult("Kullanıcı bulunamadı.");
}
```

### Input Validation
```csharp
// FluentValidation automatically validates all inputs
RuleFor(x => x.Email)
    .NotEmpty().WithMessage("E-posta adresi boş olamaz.")
    .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
    .MaximumLength(100).WithMessage("E-posta adresi en fazla 100 karakter olabilir.");
```

---

## 🧪 Testing Scenarios

### Test Case 1: Successful Profile Retrieval
```http
GET /api/v1/farmer/profile
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Expected**: 
- Status: 200 OK
- Returns complete profile data
- No sensitive fields exposed

---

### Test Case 2: Successful Profile Update
```http
PUT /api/v1/farmer/profile
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "fullName": "Test User Updated",
  "email": "test.updated@example.com",
  "mobilePhones": "+90 532 999 8877",
  "birthDate": "1990-05-20",
  "gender": 1,
  "address": "Ankara, Çankaya",
  "notes": "Updated notes"
}
```

**Expected**:
- Status: 200 OK
- Profile updated successfully
- `UpdateContactDate` updated to current time

---

### Test Case 3: Validation Errors
```http
PUT /api/v1/farmer/profile
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "fullName": "A",                    // Too short
  "email": "invalid-email",           // Invalid format
  "mobilePhones": "123",              // Too short
  "birthDate": "2030-01-01",          // Future date
  "gender": 5                         // Invalid value
}
```

**Expected**:
- Status: 400 Bad Request
- Validation error messages:
  - "Ad Soyad en az 2 karakter olmalıdır."
  - "Geçerli bir e-posta adresi giriniz."
  - "Telefon numarası en az 10 karakter olmalıdır."
  - "Doğum tarihi geçerli bir tarih olmalıdır ve gelecekte olamaz."
  - "Cinsiyet değeri 0 (Belirtilmemiş), 1 (Erkek) veya 2 (Kadın) olmalıdır."

---

### Test Case 4: Unauthorized Access
```http
GET /api/v1/farmer/profile
# No Authorization header
```

**Expected**:
- Status: 401 Unauthorized

---

### Test Case 5: Invalid Token
```http
GET /api/v1/farmer/profile
Authorization: Bearer invalid_token_here
```

**Expected**:
- Status: 401 Unauthorized
- Message: "Geçersiz kullanıcı token'ı."

---

### Test Case 6: User Not Found
**Scenario**: JWT token contains userId that doesn't exist in database

**Expected**:
- Status: 404 Not Found
- Message: "Kullanıcı bulunamadı."

---

## 📊 Comparison: Old vs New

| Aspect | Old Implementation | New Implementation |
|--------|-------------------|-------------------|
| **Endpoint** | `/api/users/{id}` (generic) | `/api/farmer/profile` (dedicated) |
| **User ID Source** | Request body/URL parameter | JWT token (secure) |
| **Ownership Validation** | ❌ Missing | ✅ Automatic via JWT |
| **Sensitive Data** | ❌ Exposed (Password, RefreshToken) | ✅ Filtered out |
| **Input Validation** | ❌ None | ✅ FluentValidation |
| **Updateable Fields** | 6 fields (Email, FullName, MobilePhones, Address, Notes) | 7 fields (+ BirthDate, Gender) |
| **Security Risk** | 🔴 High (any user can update any profile) | 🟢 Low (users can only update own profile) |

---

## 🚀 Migration Guide

### For Mobile/Frontend Developers

**Old API Usage** (DEPRECATED - DO NOT USE):
```javascript
// ❌ Insecure - allows updating any user's profile
PUT /api/users
{
  "userId": 123,  // Can be manipulated
  "email": "new@example.com",
  "fullName": "New Name"
}
```

**New API Usage** (RECOMMENDED):
```javascript
// ✅ Secure - automatically uses authenticated user
PUT /api/v1/farmer/profile
Headers: { "Authorization": "Bearer {token}" }
{
  "email": "new@example.com",
  "fullName": "New Name",
  "mobilePhones": "+90 532 123 4567",
  "birthDate": "1990-01-01",
  "gender": 1,
  "address": "İstanbul",
  "notes": "My notes"
}
```

---

## 📝 Notes

### Why Separate Farmer Controller?
1. **Security Isolation**: Farmer-specific logic separate from generic user management
2. **Role-Based Access**: Clear authorization boundaries
3. **Future Extensibility**: Easy to add farmer-specific features (analytics, farm info, etc.)
4. **Consistency**: Matches existing pattern (SponsorController for sponsors)

### Admin Access
- Admins can access farmer profiles via `/api/farmer/profile` (requires Farmer or Admin role)
- For admin-specific user management, use existing `/api/adminusers` endpoints

### Avatar Management
- Avatar upload/delete remains in `/api/users/avatar` endpoints
- These endpoints are already secure (use JWT token)
- No changes needed for avatar management

---

## 🔗 Related Documentation

- [FARMER_PROFILE_MANAGEMENT_ANALYSIS.md](./FARMER_PROFILE_MANAGEMENT_ANALYSIS.md) - Security analysis and findings
- [Postman Collection](../ZiraAI_Complete_API_Collection_v6.1.json) - API testing collection (to be updated)

---

## ✅ Security Checklist

- [x] JWT-based user identification
- [x] User ownership validation
- [x] Sensitive data filtering (Password, RefreshToken)
- [x] Input validation (FluentValidation)
- [x] Email format validation
- [x] Phone format validation
- [x] BirthDate range validation
- [x] Field length constraints
- [x] Role-based authorization
- [x] Cache invalidation on updates
- [x] Audit logging (via LogAspect)

---

## 🎯 Next Steps

1. **Update Postman Collection**: Add new farmer profile endpoints
2. **Mobile App Integration**: Update mobile apps to use new endpoints
3. **Deprecation Notice**: Mark old `/api/users` PUT endpoint for deprecation
4. **Documentation**: Update API documentation/Swagger descriptions
5. **Testing**: Comprehensive integration testing with real JWT tokens
