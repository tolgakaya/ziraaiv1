# Sponsor Logo Management - Frontend Integration Guide

> **⚠️ IMPORTANT**: Bu dokümantasyon backend kod tarafından doğrudan kontrol edilerek hazırlanmıştır.  
> Tüm endpoint'ler, request/response yapıları ve validasyon kuralları gerçek koddan alınmıştır.

---

## 📋 İçindekiler
1. [API Genel Bakış](#api-genel-bakış)
2. [Authentication](#authentication)
3. [Endpoint 1: Upload Sponsor Logo](#endpoint-1-upload-sponsor-logo)
4. [Endpoint 2: Get Sponsor Logo](#endpoint-2-get-sponsor-logo)
5. [Endpoint 3: Delete Sponsor Logo](#endpoint-3-delete-sponsor-logo)
6. [TypeScript Type Definitions](#typescript-type-definitions)
7. [Angular Service Implementation](#angular-service-implementation)
8. [React/Next.js Implementation](#reactnextjs-implementation)
9. [Flutter/Dart Implementation](#flutterdart-implementation)
10. [Error Handling](#error-handling)
11. [File Upload Best Practices](#file-upload-best-practices)
12. [Testing Checklist](#testing-checklist)

---

## API Genel Bakış

### Base URL
```
Production:  https://api.ziraai.com/api/v1/sponsorship
Staging:     https://ziraai-api-sit.up.railway.app/api/v1/sponsorship
Development: https://localhost:5001/api/v1/sponsorship
```

### API Version
- **Header Name**: `x-dev-arch-version`
- **Value**: `1.0`
- **Required**: ✅ Yes (tüm request'lerde bulunmalı)

### Content Type
- **Upload**: `multipart/form-data`
- **Response**: `application/json`

### Response Wrapper Structure
Tüm endpoint'ler aşağıdaki wrapper yapısını kullanır:

```typescript
// Başarılı response (data içeren)
interface IDataResult<T> {
  success: boolean;    // true
  message: string;     // İşlem mesajı
  data: T;            // Gerçek veri
}

// Başarılı response (data içermeyen)
interface IResult {
  success: boolean;    // true
  message: string;     // İşlem mesajı
}

// Hata response
interface IErrorResult {
  success: boolean;    // false
  message: string;     // Hata mesajı
}
```

---

## Authentication

### JWT Bearer Token
Tüm **upload** ve **delete** işlemleri için JWT token gereklidir:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Token'dan User ID Çıkarımı
Backend, token'daki `NameIdentifier` claim'inden kullanıcı ID'sini otomatik olarak alır:
- **Claim Type**: `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`
- **Kod referansı**: `SponsorshipController.GetUserId()` methodu

### Authorization Rules
- ✅ **Upload**: Sadece kendi sponsor profili için
- ✅ **Delete**: Sadece kendi sponsor profili için
- ✅ **Get**: Public endpoint, token gerekmez (herkes herhangi bir sponsor'un logosunu görebilir)

---

## Endpoint 1: Upload Sponsor Logo

### HTTP Request
```http
POST /api/v1/sponsorship/logo
Content-Type: multipart/form-data
Authorization: Bearer {token}
x-dev-arch-version: 1.0

file: [binary file data]
```

### Request Parameters

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| `file` | File | Form-data | ✅ Yes | Logo image dosyası |

### File Validation Rules

#### ✅ Allowed File Extensions
```javascript
const ALLOWED_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.gif', '.webp', '.svg'];
```

#### 📏 File Size Limit
```javascript
const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB (5,242,880 bytes)
```

#### 🖼️ Image Processing Rules

**Raster Images (JPG, PNG, GIF, WebP):**
```javascript
const IMAGE_PROCESSING = {
  fullSize: {
    maxWidth: 512,
    maxHeight: 512,
    mode: 'ResizeMode.Max',        // Aspect ratio korunur
    format: 'JPEG',
    quality: 85
  },
  thumbnail: {
    maxWidth: 128,
    maxHeight: 128,
    mode: 'ResizeMode.Max',        // Aspect ratio korunur
    format: 'JPEG',
    quality: 85
  }
};
```

**Vector Images (SVG):**
```javascript
const SVG_PROCESSING = {
  resize: false,                    // SVG resize edilmez
  thumbnail: 'same-as-original',    // Thumbnail = Full size (scalable)
  preserveOriginal: true
};
```

### Success Response (200 OK)

#### Response Structure
```typescript
interface UploadLogoSuccessResponse {
  success: true;
  message: "Logo uploaded successfully";
  data: {
    logoUrl: string;        // Full size logo URL (512x512 for raster, original for SVG)
    thumbnailUrl: string;   // Thumbnail URL (128x128 for raster, same as logoUrl for SVG)
  }
}
```

#### Example Response (Raster Image)
```json
{
  "success": true,
  "message": "Logo uploaded successfully",
  "data": {
    "logoUrl": "https://i.ibb.co/abc123/sponsor_logo_159_638123456789.jpg",
    "thumbnailUrl": "https://i.ibb.co/def456/sponsor_logo_thumb_159_638123456789.jpg"
  }
}
```

#### Example Response (SVG)
```json
{
  "success": true,
  "message": "Logo uploaded successfully",
  "data": {
    "logoUrl": "https://i.ibb.co/ghi789/sponsor_logo_159_638123456789.svg",
    "thumbnailUrl": "https://i.ibb.co/ghi789/sponsor_logo_159_638123456789.svg"
  }
}
```

### Error Responses

#### 400 Bad Request - No File Provided
```json
{
  "success": false,
  "message": "No file provided"
}
```

#### 400 Bad Request - File Too Large
```json
{
  "success": false,
  "message": "File size exceeds maximum limit of 5MB"
}
```

#### 400 Bad Request - Invalid File Type
```json
{
  "success": false,
  "message": "Invalid file type. Allowed types: .jpg, .jpeg, .png, .gif, .webp, .svg"
}
```

#### 401 Unauthorized - Missing Token
```json
{
  "success": false,
  "message": "User not authenticated"
}
```

#### 404 Not Found - Sponsor Profile Not Found
```json
{
  "success": false,
  "message": "Sponsor profile not found"
}
```

#### 500 Internal Server Error - Upload Failed
```json
{
  "success": false,
  "message": "Failed to upload logo: [error details]"
}
```

### cURL Example
```bash
curl -X POST "https://api.ziraai.com/api/v1/sponsorship/logo" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "x-dev-arch-version: 1.0" \
  -F "file=@/path/to/logo.png"
```

### JavaScript Fetch Example
```javascript
async function uploadSponsorLogo(file, token) {
  const formData = new FormData();
  formData.append('file', file);

  const response = await fetch('https://api.ziraai.com/api/v1/sponsorship/logo', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'x-dev-arch-version': '1.0'
    },
    body: formData
  });

  const result = await response.json();
  
  if (!result.success) {
    throw new Error(result.message);
  }
  
  return result.data;
}
```

### Axios Example
```javascript
import axios from 'axios';

async function uploadSponsorLogo(file, token) {
  const formData = new FormData();
  formData.append('file', file);

  const response = await axios.post(
    'https://api.ziraai.com/api/v1/sponsorship/logo',
    formData,
    {
      headers: {
        'Authorization': `Bearer ${token}`,
        'x-dev-arch-version': '1.0',
        'Content-Type': 'multipart/form-data'
      }
    }
  );

  return response.data.data; // { logoUrl, thumbnailUrl }
}
```

---

## Endpoint 2: Get Sponsor Logo

### HTTP Request
```http
GET /api/v1/sponsorship/logo/{sponsorId?}
x-dev-arch-version: 1.0
```

> **Note**: Authorization header optional (public endpoint)

### Request Parameters

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| `sponsorId` | integer | Path | ❌ No | Sponsor ID (boş bırakılırsa authenticated user'ın ID'si kullanılır) |

### URL Patterns

#### Pattern 1: Get Specific Sponsor's Logo (Public)
```http
GET /api/v1/sponsorship/logo/159
```

#### Pattern 2: Get Own Logo (Authenticated)
```http
GET /api/v1/sponsorship/logo
Authorization: Bearer {token}
```

### Success Response (200 OK)

#### Response Structure
```typescript
interface GetLogoSuccessResponse {
  success: true;
  message: "Logo retrieved successfully";
  data: {
    sponsorId: number;        // Sponsor ID
    logoUrl: string;          // Full size logo URL
    thumbnailUrl: string;     // Thumbnail logo URL
    updatedDate: string;      // ISO 8601 format date (nullable)
  }
}
```

#### Example Response
```json
{
  "success": true,
  "message": "Logo retrieved successfully",
  "data": {
    "sponsorId": 159,
    "logoUrl": "https://i.ibb.co/abc123/sponsor_logo_159_638123456789.jpg",
    "thumbnailUrl": "https://i.ibb.co/def456/sponsor_logo_thumb_159_638123456789.jpg",
    "updatedDate": "2025-01-26T12:30:45.123Z"
  }
}
```

#### Example Response (No UpdatedDate)
```json
{
  "success": true,
  "message": "Logo retrieved successfully",
  "data": {
    "sponsorId": 159,
    "logoUrl": "https://i.ibb.co/abc123/sponsor_logo_159_638123456789.jpg",
    "thumbnailUrl": "https://i.ibb.co/def456/sponsor_logo_thumb_159_638123456789.jpg",
    "updatedDate": null
  }
}
```

### Error Responses

#### 400 Bad Request - Invalid Sponsor ID
```json
{
  "success": false,
  "message": "Invalid sponsor ID"
}
```

#### 404 Not Found - Sponsor Profile Not Found
```json
{
  "success": false,
  "message": "Sponsor profile not found"
}
```

#### 404 Not Found - No Logo Set
```json
{
  "success": false,
  "message": "No logo set for this sponsor"
}
```

### cURL Example
```bash
# Get specific sponsor's logo (public)
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/logo/159" \
  -H "x-dev-arch-version: 1.0"

# Get own logo (authenticated)
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/logo" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "x-dev-arch-version: 1.0"
```

### JavaScript Fetch Example
```javascript
// Get specific sponsor's logo (public)
async function getSponsorLogo(sponsorId) {
  const response = await fetch(
    `https://api.ziraai.com/api/v1/sponsorship/logo/${sponsorId}`,
    {
      headers: {
        'x-dev-arch-version': '1.0'
      }
    }
  );

  const result = await response.json();
  
  if (!result.success) {
    throw new Error(result.message);
  }
  
  return result.data;
}

// Get own logo (authenticated)
async function getOwnLogo(token) {
  const response = await fetch(
    'https://api.ziraai.com/api/v1/sponsorship/logo',
    {
      headers: {
        'Authorization': `Bearer ${token}`,
        'x-dev-arch-version': '1.0'
      }
    }
  );

  const result = await response.json();
  
  if (!result.success) {
    throw new Error(result.message);
  }
  
  return result.data;
}
```

---

## Endpoint 3: Delete Sponsor Logo

### HTTP Request
```http
DELETE /api/v1/sponsorship/logo
Authorization: Bearer {token}
x-dev-arch-version: 1.0
```

### Request Parameters
❌ No parameters required (sponsor ID extracted from JWT token)

### Success Response (200 OK)

#### Response Structure
```typescript
interface DeleteLogoSuccessResponse {
  success: true;
  message: "Logo deleted successfully";
}
```

#### Example Response
```json
{
  "success": true,
  "message": "Logo deleted successfully"
}
```

#### Example Response (No Logo to Delete)
```json
{
  "success": true,
  "message": "No logo to delete"
}
```

### Error Responses

#### 401 Unauthorized - Missing Token
```json
{
  "success": false,
  "message": "User not authenticated"
}
```

#### 404 Not Found - Sponsor Profile Not Found
```json
{
  "success": false,
  "message": "Sponsor profile not found"
}
```

#### 500 Internal Server Error - Deletion Failed
```json
{
  "success": false,
  "message": "Failed to delete logo: [error details]"
}
```

### What Happens on Delete?

Backend performs following operations:
1. ✅ Deletes full size logo from storage provider
2. ✅ Deletes thumbnail from storage provider
3. ✅ Sets `SponsorProfile.SponsorLogoUrl` to `NULL`
4. ✅ Sets `SponsorProfile.SponsorLogoThumbnailUrl` to `NULL`
5. ✅ Updates `SponsorProfile.UpdatedDate` to current timestamp
6. ✅ Updates `SponsorProfile.UpdatedByUserId` to current user ID

### cURL Example
```bash
curl -X DELETE "https://api.ziraai.com/api/v1/sponsorship/logo" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "x-dev-arch-version: 1.0"
```

### JavaScript Fetch Example
```javascript
async function deleteSponsorLogo(token) {
  const response = await fetch(
    'https://api.ziraai.com/api/v1/sponsorship/logo',
    {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${token}`,
        'x-dev-arch-version': '1.0'
      }
    }
  );

  const result = await response.json();
  
  if (!result.success) {
    throw new Error(result.message);
  }
  
  return result;
}
```

---

## TypeScript Type Definitions

### Complete Type Definitions
```typescript
// ============================================
// API Response Types
// ============================================

/**
 * Base result interface
 */
export interface IResult {
  success: boolean;
  message: string;
}

/**
 * Data result interface (contains data payload)
 */
export interface IDataResult<T> extends IResult {
  data: T;
}

// ============================================
// Sponsor Logo DTOs
// ============================================

/**
 * Upload logo result payload
 */
export interface SponsorLogoUploadResult {
  logoUrl: string;        // Full size logo URL (512x512 or original SVG)
  thumbnailUrl: string;   // Thumbnail URL (128x128 or same SVG)
}

/**
 * Get logo result payload
 */
export interface SponsorLogoDto {
  sponsorId: number;           // Sponsor ID
  logoUrl: string;             // Full size logo URL
  thumbnailUrl: string;        // Thumbnail logo URL
  updatedDate: string | null;  // ISO 8601 date or null
}

// ============================================
// API Response Types
// ============================================

/**
 * Upload Logo Response
 */
export type UploadLogoResponse = IDataResult<SponsorLogoUploadResult>;

/**
 * Get Logo Response
 */
export type GetLogoResponse = IDataResult<SponsorLogoDto>;

/**
 * Delete Logo Response
 */
export type DeleteLogoResponse = IResult;

// ============================================
// Validation Constants
// ============================================

export const SPONSOR_LOGO_CONSTRAINTS = {
  allowedExtensions: ['.jpg', '.jpeg', '.png', '.gif', '.webp', '.svg'] as const,
  maxFileSize: 5 * 1024 * 1024, // 5MB in bytes
  maxFileSizeMB: 5,
  fullSizeWidth: 512,
  fullSizeHeight: 512,
  thumbnailWidth: 128,
  thumbnailHeight: 128,
  imageQuality: 85,
  outputFormat: 'JPEG' as const,
} as const;

// ============================================
// Helper Type Guards
// ============================================

/**
 * Type guard for successful data result
 */
export function isSuccessDataResult<T>(
  result: IDataResult<T>
): result is IDataResult<T> & { success: true } {
  return result.success === true;
}

/**
 * Type guard for error result
 */
export function isErrorResult(
  result: IResult
): result is IResult & { success: false } {
  return result.success === false;
}

/**
 * Validates file extension
 */
export function isValidLogoExtension(fileName: string): boolean {
  const extension = fileName.toLowerCase().substring(fileName.lastIndexOf('.'));
  return SPONSOR_LOGO_CONSTRAINTS.allowedExtensions.includes(
    extension as typeof SPONSOR_LOGO_CONSTRAINTS.allowedExtensions[number]
  );
}

/**
 * Validates file size
 */
export function isValidLogoSize(fileSizeBytes: number): boolean {
  return fileSizeBytes <= SPONSOR_LOGO_CONSTRAINTS.maxFileSize;
}
```

---

## Angular Service Implementation

### sponsor-logo.service.ts
```typescript
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { environment } from '@environments/environment';
import {
  UploadLogoResponse,
  GetLogoResponse,
  DeleteLogoResponse,
  SponsorLogoUploadResult,
  SponsorLogoDto,
  SPONSOR_LOGO_CONSTRAINTS,
  isValidLogoExtension,
  isValidLogoSize,
} from '@app/models/sponsor-logo.types';

@Injectable({
  providedIn: 'root'
})
export class SponsorLogoService {
  private readonly API_URL = `${environment.apiUrl}/api/v1/sponsorship`;
  private readonly API_VERSION = '1.0';

  constructor(private http: HttpClient) {}

  /**
   * Upload sponsor logo
   * @param file Image file to upload
   * @param token JWT authentication token
   * @returns Observable with upload result
   * @throws Error if file validation fails
   */
  uploadLogo(file: File, token: string): Observable<SponsorLogoUploadResult> {
    // Client-side validation
    this.validateFile(file);

    const formData = new FormData();
    formData.append('file', file);

    const headers = new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'x-dev-arch-version': this.API_VERSION
    });

    return this.http.post<UploadLogoResponse>(
      `${this.API_URL}/logo`,
      formData,
      { headers }
    ).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message);
        }
        return response.data;
      }),
      catchError(this.handleError)
    );
  }

  /**
   * Get sponsor logo by ID (public)
   * @param sponsorId Sponsor ID
   * @returns Observable with logo information
   */
  getLogo(sponsorId: number): Observable<SponsorLogoDto> {
    const headers = new HttpHeaders({
      'x-dev-arch-version': this.API_VERSION
    });

    return this.http.get<GetLogoResponse>(
      `${this.API_URL}/logo/${sponsorId}`,
      { headers }
    ).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message);
        }
        return response.data;
      }),
      catchError(this.handleError)
    );
  }

  /**
   * Get own logo (authenticated)
   * @param token JWT authentication token
   * @returns Observable with logo information
   */
  getOwnLogo(token: string): Observable<SponsorLogoDto> {
    const headers = new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'x-dev-arch-version': this.API_VERSION
    });

    return this.http.get<GetLogoResponse>(
      `${this.API_URL}/logo`,
      { headers }
    ).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message);
        }
        return response.data;
      }),
      catchError(this.handleError)
    );
  }

  /**
   * Delete sponsor logo
   * @param token JWT authentication token
   * @returns Observable with deletion result
   */
  deleteLogo(token: string): Observable<void> {
    const headers = new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'x-dev-arch-version': this.API_VERSION
    });

    return this.http.delete<DeleteLogoResponse>(
      `${this.API_URL}/logo`,
      { headers }
    ).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message);
        }
        return;
      }),
      catchError(this.handleError)
    );
  }

  /**
   * Validate file before upload
   * @param file File to validate
   * @throws Error if validation fails
   */
  private validateFile(file: File): void {
    // Check if file exists
    if (!file) {
      throw new Error('No file provided');
    }

    // Validate file extension
    if (!isValidLogoExtension(file.name)) {
      throw new Error(
        `Invalid file type. Allowed types: ${SPONSOR_LOGO_CONSTRAINTS.allowedExtensions.join(', ')}`
      );
    }

    // Validate file size
    if (!isValidLogoSize(file.size)) {
      throw new Error(
        `File size exceeds maximum limit of ${SPONSOR_LOGO_CONSTRAINTS.maxFileSizeMB}MB`
      );
    }
  }

  /**
   * Handle HTTP errors
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An unknown error occurred';

    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = error.error.message;
    } else {
      // Server-side error
      if (error.error?.message) {
        errorMessage = error.error.message;
      } else {
        errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
      }
    }

    return throwError(() => new Error(errorMessage));
  }
}
```

### Angular Component Example
```typescript
import { Component } from '@angular/core';
import { SponsorLogoService } from '@app/services/sponsor-logo.service';
import { AuthService } from '@app/services/auth.service';

@Component({
  selector: 'app-sponsor-logo-upload',
  template: `
    <div class="logo-upload-container">
      <input 
        type="file" 
        #fileInput
        (change)="onFileSelected($event)"
        accept=".jpg,.jpeg,.png,.gif,.webp,.svg"
      />
      
      <button (click)="uploadLogo()" [disabled]="!selectedFile || uploading">
        {{ uploading ? 'Uploading...' : 'Upload Logo' }}
      </button>

      <div *ngIf="logoUrl" class="logo-preview">
        <img [src]="thumbnailUrl" alt="Sponsor Logo" />
        <button (click)="deleteLogo()">Delete Logo</button>
      </div>

      <div *ngIf="error" class="error-message">{{ error }}</div>
    </div>
  `
})
export class SponsorLogoUploadComponent {
  selectedFile: File | null = null;
  uploading = false;
  logoUrl: string | null = null;
  thumbnailUrl: string | null = null;
  error: string | null = null;

  constructor(
    private sponsorLogoService: SponsorLogoService,
    private authService: AuthService
  ) {
    this.loadCurrentLogo();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
      this.error = null;
    }
  }

  uploadLogo(): void {
    if (!this.selectedFile) return;

    const token = this.authService.getToken();
    if (!token) {
      this.error = 'Authentication required';
      return;
    }

    this.uploading = true;
    this.error = null;

    this.sponsorLogoService.uploadLogo(this.selectedFile, token).subscribe({
      next: (result) => {
        this.logoUrl = result.logoUrl;
        this.thumbnailUrl = result.thumbnailUrl;
        this.selectedFile = null;
        this.uploading = false;
        console.log('Logo uploaded successfully');
      },
      error: (error) => {
        this.error = error.message;
        this.uploading = false;
      }
    });
  }

  deleteLogo(): void {
    const token = this.authService.getToken();
    if (!token) return;

    this.sponsorLogoService.deleteLogo(token).subscribe({
      next: () => {
        this.logoUrl = null;
        this.thumbnailUrl = null;
        console.log('Logo deleted successfully');
      },
      error: (error) => {
        this.error = error.message;
      }
    });
  }

  private loadCurrentLogo(): void {
    const token = this.authService.getToken();
    if (!token) return;

    this.sponsorLogoService.getOwnLogo(token).subscribe({
      next: (logo) => {
        this.logoUrl = logo.logoUrl;
        this.thumbnailUrl = logo.thumbnailUrl;
      },
      error: (error) => {
        // Logo not found is expected for new sponsors
        if (!error.message.includes('No logo set')) {
          this.error = error.message;
        }
      }
    });
  }
}
```

---

## React/Next.js Implementation

### sponsor-logo.service.ts
```typescript
import axios, { AxiosError } from 'axios';
import {
  UploadLogoResponse,
  GetLogoResponse,
  DeleteLogoResponse,
  SponsorLogoUploadResult,
  SponsorLogoDto,
  SPONSOR_LOGO_CONSTRAINTS,
  isValidLogoExtension,
  isValidLogoSize,
} from '@/types/sponsor-logo.types';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'https://api.ziraai.com';
const API_VERSION = '1.0';

class SponsorLogoService {
  /**
   * Upload sponsor logo
   */
  async uploadLogo(file: File, token: string): Promise<SponsorLogoUploadResult> {
    // Client-side validation
    this.validateFile(file);

    const formData = new FormData();
    formData.append('file', file);

    try {
      const response = await axios.post<UploadLogoResponse>(
        `${API_URL}/api/v1/sponsorship/logo`,
        formData,
        {
          headers: {
            'Authorization': `Bearer ${token}`,
            'x-dev-arch-version': API_VERSION,
            'Content-Type': 'multipart/form-data',
          },
        }
      );

      if (!response.data.success) {
        throw new Error(response.data.message);
      }

      return response.data.data;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Get sponsor logo by ID (public)
   */
  async getLogo(sponsorId: number): Promise<SponsorLogoDto> {
    try {
      const response = await axios.get<GetLogoResponse>(
        `${API_URL}/api/v1/sponsorship/logo/${sponsorId}`,
        {
          headers: {
            'x-dev-arch-version': API_VERSION,
          },
        }
      );

      if (!response.data.success) {
        throw new Error(response.data.message);
      }

      return response.data.data;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Get own logo (authenticated)
   */
  async getOwnLogo(token: string): Promise<SponsorLogoDto> {
    try {
      const response = await axios.get<GetLogoResponse>(
        `${API_URL}/api/v1/sponsorship/logo`,
        {
          headers: {
            'Authorization': `Bearer ${token}`,
            'x-dev-arch-version': API_VERSION,
          },
        }
      );

      if (!response.data.success) {
        throw new Error(response.data.message);
      }

      return response.data.data;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Delete sponsor logo
   */
  async deleteLogo(token: string): Promise<void> {
    try {
      const response = await axios.delete<DeleteLogoResponse>(
        `${API_URL}/api/v1/sponsorship/logo`,
        {
          headers: {
            'Authorization': `Bearer ${token}`,
            'x-dev-arch-version': API_VERSION,
          },
        }
      );

      if (!response.data.success) {
        throw new Error(response.data.message);
      }
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Validate file before upload
   */
  private validateFile(file: File): void {
    if (!file) {
      throw new Error('No file provided');
    }

    if (!isValidLogoExtension(file.name)) {
      throw new Error(
        `Invalid file type. Allowed types: ${SPONSOR_LOGO_CONSTRAINTS.allowedExtensions.join(', ')}`
      );
    }

    if (!isValidLogoSize(file.size)) {
      throw new Error(
        `File size exceeds maximum limit of ${SPONSOR_LOGO_CONSTRAINTS.maxFileSizeMB}MB`
      );
    }
  }

  /**
   * Handle errors
   */
  private handleError(error: unknown): Error {
    if (axios.isAxiosError(error)) {
      const axiosError = error as AxiosError<{ message?: string }>;
      return new Error(
        axiosError.response?.data?.message || axiosError.message || 'An error occurred'
      );
    }
    return error instanceof Error ? error : new Error('An unknown error occurred');
  }
}

export const sponsorLogoService = new SponsorLogoService();
```

### React Hook Example
```typescript
import { useState, useCallback } from 'react';
import { sponsorLogoService } from '@/services/sponsor-logo.service';
import { SponsorLogoUploadResult, SponsorLogoDto } from '@/types/sponsor-logo.types';

export function useSponsorLogo(token: string | null) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [logo, setLogo] = useState<SponsorLogoDto | null>(null);

  const uploadLogo = useCallback(async (file: File) => {
    if (!token) {
      setError('Authentication required');
      return null;
    }

    setLoading(true);
    setError(null);

    try {
      const result = await sponsorLogoService.uploadLogo(file, token);
      
      // Update local state
      setLogo({
        sponsorId: 0, // Will be set from backend
        logoUrl: result.logoUrl,
        thumbnailUrl: result.thumbnailUrl,
        updatedDate: new Date().toISOString(),
      });

      return result;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Upload failed';
      setError(errorMessage);
      return null;
    } finally {
      setLoading(false);
    }
  }, [token]);

  const deleteLogo = useCallback(async () => {
    if (!token) {
      setError('Authentication required');
      return false;
    }

    setLoading(true);
    setError(null);

    try {
      await sponsorLogoService.deleteLogo(token);
      setLogo(null);
      return true;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Deletion failed';
      setError(errorMessage);
      return false;
    } finally {
      setLoading(false);
    }
  }, [token]);

  const loadLogo = useCallback(async (sponsorId?: number) => {
    setLoading(true);
    setError(null);

    try {
      let result: SponsorLogoDto;
      
      if (sponsorId) {
        result = await sponsorLogoService.getLogo(sponsorId);
      } else if (token) {
        result = await sponsorLogoService.getOwnLogo(token);
      } else {
        throw new Error('Either sponsorId or token required');
      }

      setLogo(result);
      return result;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load logo';
      
      // Don't set error for "No logo set" case
      if (!errorMessage.includes('No logo set')) {
        setError(errorMessage);
      }
      
      return null;
    } finally {
      setLoading(false);
    }
  }, [token]);

  return {
    logo,
    loading,
    error,
    uploadLogo,
    deleteLogo,
    loadLogo,
  };
}
```

### React Component Example
```typescript
import React, { useEffect, useRef, useState } from 'react';
import { useSponsorLogo } from '@/hooks/useSponsorLogo';
import { useAuth } from '@/hooks/useAuth';

export function SponsorLogoUpload() {
  const { token } = useAuth();
  const { logo, loading, error, uploadLogo, deleteLogo, loadLogo } = useSponsorLogo(token);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    loadLogo();
  }, [loadLogo]);

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      setSelectedFile(file);
    }
  };

  const handleUpload = async () => {
    if (!selectedFile) return;

    const result = await uploadLogo(selectedFile);
    if (result) {
      setSelectedFile(null);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  const handleDelete = async () => {
    if (confirm('Are you sure you want to delete the logo?')) {
      await deleteLogo();
    }
  };

  return (
    <div className="sponsor-logo-upload">
      <h2>Sponsor Logo</h2>

      {error && (
        <div className="error-message" role="alert">
          {error}
        </div>
      )}

      {logo ? (
        <div className="logo-preview">
          <img src={logo.thumbnailUrl} alt="Sponsor Logo" />
          <div className="logo-actions">
            <button onClick={handleDelete} disabled={loading}>
              Delete Logo
            </button>
          </div>
        </div>
      ) : (
        <div className="logo-upload">
          <input
            ref={fileInputRef}
            type="file"
            accept=".jpg,.jpeg,.png,.gif,.webp,.svg"
            onChange={handleFileSelect}
            disabled={loading}
          />
          <button onClick={handleUpload} disabled={!selectedFile || loading}>
            {loading ? 'Uploading...' : 'Upload Logo'}
          </button>
        </div>
      )}
    </div>
  );
}
```

---

## Flutter/Dart Implementation

### sponsor_logo_service.dart
```dart
import 'dart:io';
import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

/// Sponsor Logo Upload Result
class SponsorLogoUploadResult {
  final String logoUrl;
  final String thumbnailUrl;

  SponsorLogoUploadResult({
    required this.logoUrl,
    required this.thumbnailUrl,
  });

  factory SponsorLogoUploadResult.fromJson(Map<String, dynamic> json) {
    return SponsorLogoUploadResult(
      logoUrl: json['logoUrl'] as String,
      thumbnailUrl: json['thumbnailUrl'] as String,
    );
  }
}

/// Sponsor Logo DTO
class SponsorLogoDto {
  final int sponsorId;
  final String logoUrl;
  final String thumbnailUrl;
  final DateTime? updatedDate;

  SponsorLogoDto({
    required this.sponsorId,
    required this.logoUrl,
    required this.thumbnailUrl,
    this.updatedDate,
  });

  factory SponsorLogoDto.fromJson(Map<String, dynamic> json) {
    return SponsorLogoDto(
      sponsorId: json['sponsorId'] as int,
      logoUrl: json['logoUrl'] as String,
      thumbnailUrl: json['thumbnailUrl'] as String,
      updatedDate: json['updatedDate'] != null
          ? DateTime.parse(json['updatedDate'] as String)
          : null,
    );
  }
}

/// API Response Wrapper
class ApiResult<T> {
  final bool success;
  final String message;
  final T? data;

  ApiResult({
    required this.success,
    required this.message,
    this.data,
  });

  factory ApiResult.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>)? fromJsonT,
  ) {
    return ApiResult<T>(
      success: json['success'] as bool,
      message: json['message'] as String,
      data: json['data'] != null && fromJsonT != null
          ? fromJsonT(json['data'] as Map<String, dynamic>)
          : null,
    );
  }
}

/// Sponsor Logo Service
class SponsorLogoService {
  final Dio _dio;
  final String _baseUrl;
  static const String _apiVersion = '1.0';
  
  // Validation constants (from backend code)
  static const List<String> allowedExtensions = [
    '.jpg',
    '.jpeg',
    '.png',
    '.gif',
    '.webp',
    '.svg'
  ];
  static const int maxFileSizeBytes = 5 * 1024 * 1024; // 5MB
  static const int maxFileSizeMB = 5;

  SponsorLogoService({
    required String baseUrl,
    Dio? dio,
  })  : _baseUrl = baseUrl,
        _dio = dio ?? Dio();

  /// Upload sponsor logo
  Future<SponsorLogoUploadResult> uploadLogo({
    required File file,
    required String token,
  }) async {
    // Client-side validation
    _validateFile(file);

    final fileName = file.path.split('/').last;
    final formData = FormData.fromMap({
      'file': await MultipartFile.fromFile(
        file.path,
        filename: fileName,
      ),
    });

    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '$_baseUrl/api/v1/sponsorship/logo',
        data: formData,
        options: Options(
          headers: {
            'Authorization': 'Bearer $token',
            'x-dev-arch-version': _apiVersion,
          },
        ),
      );

      final result = ApiResult<SponsorLogoUploadResult>.fromJson(
        response.data!,
        (json) => SponsorLogoUploadResult.fromJson(json),
      );

      if (!result.success) {
        throw Exception(result.message);
      }

      return result.data!;
    } on DioException catch (e) {
      throw Exception(_handleDioError(e));
    }
  }

  /// Get sponsor logo by ID (public)
  Future<SponsorLogoDto> getLogo(int sponsorId) async {
    try {
      final response = await _dio.get<Map<String, dynamic>>(
        '$_baseUrl/api/v1/sponsorship/logo/$sponsorId',
        options: Options(
          headers: {
            'x-dev-arch-version': _apiVersion,
          },
        ),
      );

      final result = ApiResult<SponsorLogoDto>.fromJson(
        response.data!,
        (json) => SponsorLogoDto.fromJson(json),
      );

      if (!result.success) {
        throw Exception(result.message);
      }

      return result.data!;
    } on DioException catch (e) {
      throw Exception(_handleDioError(e));
    }
  }

  /// Get own logo (authenticated)
  Future<SponsorLogoDto> getOwnLogo(String token) async {
    try {
      final response = await _dio.get<Map<String, dynamic>>(
        '$_baseUrl/api/v1/sponsorship/logo',
        options: Options(
          headers: {
            'Authorization': 'Bearer $token',
            'x-dev-arch-version': _apiVersion,
          },
        ),
      );

      final result = ApiResult<SponsorLogoDto>.fromJson(
        response.data!,
        (json) => SponsorLogoDto.fromJson(json),
      );

      if (!result.success) {
        throw Exception(result.message);
      }

      return result.data!;
    } on DioException catch (e) {
      throw Exception(_handleDioError(e));
    }
  }

  /// Delete sponsor logo
  Future<void> deleteLogo(String token) async {
    try {
      final response = await _dio.delete<Map<String, dynamic>>(
        '$_baseUrl/api/v1/sponsorship/logo',
        options: Options(
          headers: {
            'Authorization': 'Bearer $token',
            'x-dev-arch-version': _apiVersion,
          },
        ),
      );

      final result = ApiResult<void>.fromJson(response.data!, null);

      if (!result.success) {
        throw Exception(result.message);
      }
    } on DioException catch (e) {
      throw Exception(_handleDioError(e));
    }
  }

  /// Validate file before upload
  void _validateFile(File file) {
    // Check if file exists
    if (!file.existsSync()) {
      throw Exception('File does not exist');
    }

    // Validate file extension
    final fileName = file.path.toLowerCase();
    final hasValidExtension = allowedExtensions.any(
      (ext) => fileName.endsWith(ext),
    );

    if (!hasValidExtension) {
      throw Exception(
        'Invalid file type. Allowed types: ${allowedExtensions.join(', ')}',
      );
    }

    // Validate file size
    final fileSizeBytes = file.lengthSync();
    if (fileSizeBytes > maxFileSizeBytes) {
      throw Exception(
        'File size exceeds maximum limit of ${maxFileSizeMB}MB',
      );
    }
  }

  /// Handle Dio errors
  String _handleDioError(DioException error) {
    if (error.response?.data != null) {
      final data = error.response!.data;
      if (data is Map<String, dynamic> && data['message'] != null) {
        return data['message'] as String;
      }
    }
    return error.message ?? 'An error occurred';
  }
}
```

### Flutter Widget Example
```dart
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'dart:io';

class SponsorLogoUploadWidget extends StatefulWidget {
  final String? token;

  const SponsorLogoUploadWidget({
    Key? key,
    required this.token,
  }) : super(key: key);

  @override
  State<SponsorLogoUploadWidget> createState() => _SponsorLogoUploadWidgetState();
}

class _SponsorLogoUploadWidgetState extends State<SponsorLogoUploadWidget> {
  final SponsorLogoService _logoService = SponsorLogoService(
    baseUrl: 'https://api.ziraai.com',
  );
  final ImagePicker _picker = ImagePicker();

  SponsorLogoDto? _currentLogo;
  bool _isLoading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadCurrentLogo();
  }

  Future<void> _loadCurrentLogo() async {
    if (widget.token == null) return;

    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final logo = await _logoService.getOwnLogo(widget.token!);
      setState(() {
        _currentLogo = logo;
        _isLoading = false;
      });
    } catch (e) {
      // Don't show error for "No logo set"
      if (!e.toString().contains('No logo set')) {
        setState(() {
          _error = e.toString();
          _isLoading = false;
        });
      } else {
        setState(() {
          _isLoading = false;
        });
      }
    }
  }

  Future<void> _pickAndUploadLogo() async {
    if (widget.token == null) {
      _showError('Authentication required');
      return;
    }

    try {
      // Pick image
      final XFile? image = await _picker.pickImage(
        source: ImageSource.gallery,
        maxWidth: 2048,
        maxHeight: 2048,
      );

      if (image == null) return;

      setState(() {
        _isLoading = true;
        _error = null;
      });

      // Upload logo
      final result = await _logoService.uploadLogo(
        file: File(image.path),
        token: widget.token!,
      );

      setState(() {
        _currentLogo = SponsorLogoDto(
          sponsorId: 0,
          logoUrl: result.logoUrl,
          thumbnailUrl: result.thumbnailUrl,
          updatedDate: DateTime.now(),
        );
        _isLoading = false;
      });

      _showSuccess('Logo uploaded successfully');
    } catch (e) {
      setState(() {
        _error = e.toString();
        _isLoading = false;
      });
    }
  }

  Future<void> _deleteLogo() async {
    if (widget.token == null) return;

    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Delete Logo'),
        content: const Text('Are you sure you want to delete the logo?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Delete'),
          ),
        ],
      ),
    );

    if (confirm != true) return;

    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      await _logoService.deleteLogo(widget.token!);
      setState(() {
        _currentLogo = null;
        _isLoading = false;
      });
      _showSuccess('Logo deleted successfully');
    } catch (e) {
      setState(() {
        _error = e.toString();
        _isLoading = false;
      });
    }
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: Colors.red,
      ),
    );
  }

  void _showSuccess(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: Colors.green,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Sponsor Logo',
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 16),
            if (_error != null)
              Container(
                padding: const EdgeInsets.all(8),
                color: Colors.red[100],
                child: Text(
                  _error!,
                  style: const TextStyle(color: Colors.red),
                ),
              ),
            const SizedBox(height: 16),
            if (_isLoading)
              const Center(child: CircularProgressIndicator())
            else if (_currentLogo != null)
              Column(
                children: [
                  Image.network(
                    _currentLogo!.thumbnailUrl,
                    width: 128,
                    height: 128,
                    fit: BoxFit.contain,
                  ),
                  const SizedBox(height: 16),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      ElevatedButton(
                        onPressed: _pickAndUploadLogo,
                        child: const Text('Change Logo'),
                      ),
                      const SizedBox(width: 8),
                      ElevatedButton(
                        onPressed: _deleteLogo,
                        style: ElevatedButton.styleFrom(
                          backgroundColor: Colors.red,
                        ),
                        child: const Text('Delete Logo'),
                      ),
                    ],
                  ),
                ],
              )
            else
              ElevatedButton(
                onPressed: _pickAndUploadLogo,
                child: const Text('Upload Logo'),
              ),
          ],
        ),
      ),
    );
  }
}
```

---

## Error Handling

### Error Response Structure
All error responses follow this structure:
```typescript
interface ErrorResponse {
  success: false;
  message: string;  // Human-readable error message
}
```

### HTTP Status Codes

| Status Code | Meaning | When It Occurs |
|------------|---------|----------------|
| `200 OK` | Success | Request completed successfully |
| `400 Bad Request` | Client Error | Invalid input, file too large, wrong file type |
| `401 Unauthorized` | Authentication Required | Missing or invalid JWT token |
| `404 Not Found` | Resource Not Found | Sponsor profile not found, no logo set |
| `500 Internal Server Error` | Server Error | File upload failed, database error, storage error |

### Common Error Messages

| Error Message | HTTP Status | Cause | Solution |
|--------------|------------|-------|----------|
| `"No file provided"` | 400 | FormData doesn't contain file | Check FormData.append('file', ...) |
| `"File size exceeds maximum limit of 5MB"` | 400 | File > 5MB | Compress image before upload |
| `"Invalid file type. Allowed types: ..."` | 400 | Wrong file extension | Use .jpg, .png, .gif, .webp, or .svg |
| `"User not authenticated"` | 401 | Missing/invalid token | Check Authorization header |
| `"Sponsor profile not found"` | 404 | No profile for user | Create sponsor profile first |
| `"No logo set for this sponsor"` | 404 | GET request for non-existent logo | This is expected for new sponsors |
| `"Failed to upload logo: ..."` | 500 | Storage provider error | Retry or contact support |
| `"Failed to delete logo: ..."` | 500 | Storage provider error | Retry or contact support |

### Error Handling Best Practices

#### 1. Client-Side Validation (Before API Call)
```typescript
function validateBeforeUpload(file: File): string | null {
  // File existence
  if (!file) {
    return 'No file selected';
  }

  // File size
  if (file.size > 5 * 1024 * 1024) {
    return 'File size must be less than 5MB';
  }

  // File type
  const validExtensions = ['.jpg', '.jpeg', '.png', '.gif', '.webp', '.svg'];
  const fileName = file.name.toLowerCase();
  const hasValidExtension = validExtensions.some(ext => fileName.endsWith(ext));
  
  if (!hasValidExtension) {
    return `Invalid file type. Allowed: ${validExtensions.join(', ')}`;
  }

  return null; // Valid
}
```

#### 2. Graceful Error Display
```typescript
function handleApiError(error: unknown): string {
  if (axios.isAxiosError(error)) {
    // API error response
    if (error.response?.data?.message) {
      return error.response.data.message;
    }
    
    // Network error
    if (error.message === 'Network Error') {
      return 'Network connection failed. Please check your internet.';
    }
    
    // Timeout
    if (error.code === 'ECONNABORTED') {
      return 'Request timeout. Please try again.';
    }
  }
  
  // Generic error
  return error instanceof Error ? error.message : 'An unexpected error occurred';
}
```

#### 3. Retry Logic for Transient Errors
```typescript
async function uploadWithRetry(
  file: File,
  token: string,
  maxRetries = 3
): Promise<SponsorLogoUploadResult> {
  let lastError: Error | null = null;
  
  for (let attempt = 1; attempt <= maxRetries; attempt++) {
    try {
      return await sponsorLogoService.uploadLogo(file, token);
    } catch (error) {
      lastError = error as Error;
      
      // Don't retry client errors (4xx)
      if (axios.isAxiosError(error) && error.response?.status) {
        const status = error.response.status;
        if (status >= 400 && status < 500) {
          throw error; // Client error, don't retry
        }
      }
      
      // Wait before retry (exponential backoff)
      if (attempt < maxRetries) {
        const delayMs = Math.pow(2, attempt) * 1000;
        await new Promise(resolve => setTimeout(resolve, delayMs));
      }
    }
  }
  
  throw lastError || new Error('Upload failed after retries');
}
```

---

## File Upload Best Practices

### 1. Image Optimization Before Upload

#### Client-Side Compression (JavaScript)
```typescript
import imageCompression from 'browser-image-compression';

async function optimizeImage(file: File): Promise<File> {
  const options = {
    maxSizeMB: 5,              // Max 5MB
    maxWidthOrHeight: 2048,    // Max dimension
    useWebWorker: true,
    fileType: 'image/jpeg',    // Convert to JPEG for smaller size
  };

  try {
    const compressedFile = await imageCompression(file, options);
    return compressedFile;
  } catch (error) {
    console.error('Image optimization failed:', error);
    return file; // Return original if compression fails
  }
}

// Usage
const optimizedFile = await optimizeImage(selectedFile);
await uploadLogo(optimizedFile, token);
```

### 2. File Type Detection

```typescript
function getFileTypeFromSignature(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    
    reader.onload = (e) => {
      const arr = new Uint8Array(e.target?.result as ArrayBuffer).subarray(0, 4);
      let header = '';
      for (let i = 0; i < arr.length; i++) {
        header += arr[i].toString(16);
      }
      
      // File signatures
      const signatures: Record<string, string> = {
        '89504e47': 'image/png',
        'ffd8ffe0': 'image/jpeg',
        'ffd8ffe1': 'image/jpeg',
        'ffd8ffe2': 'image/jpeg',
        '47494638': 'image/gif',
        '52494646': 'image/webp', // Partial, needs more validation
        '3c3f786d': 'image/svg+xml',
        '3c737667': 'image/svg+xml',
      };
      
      resolve(signatures[header] || 'unknown');
    };
    
    reader.onerror = reject;
    reader.readAsArrayBuffer(file.slice(0, 4));
  });
}

// Usage
const detectedType = await getFileTypeFromSignature(file);
if (!detectedType.startsWith('image/')) {
  throw new Error('File is not a valid image');
}
```

### 3. Upload Progress Tracking

```typescript
async function uploadWithProgress(
  file: File,
  token: string,
  onProgress: (percent: number) => void
): Promise<SponsorLogoUploadResult> {
  const formData = new FormData();
  formData.append('file', file);

  const response = await axios.post<UploadLogoResponse>(
    `${API_URL}/api/v1/sponsorship/logo`,
    formData,
    {
      headers: {
        'Authorization': `Bearer ${token}`,
        'x-dev-arch-version': '1.0',
      },
      onUploadProgress: (progressEvent) => {
        const percent = Math.round(
          (progressEvent.loaded * 100) / (progressEvent.total || 100)
        );
        onProgress(percent);
      },
    }
  );

  if (!response.data.success) {
    throw new Error(response.data.message);
  }

  return response.data.data;
}

// Usage
await uploadWithProgress(file, token, (percent) => {
  console.log(`Upload progress: ${percent}%`);
  setUploadProgress(percent);
});
```

### 4. Image Preview Before Upload

```typescript
function createImagePreview(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    
    reader.onload = (e) => {
      resolve(e.target?.result as string);
    };
    
    reader.onerror = reject;
    reader.readAsDataURL(file);
  });
}

// Usage
const previewUrl = await createImagePreview(selectedFile);
setPreviewImage(previewUrl);
```

### 5. Drag and Drop Support

```typescript
function setupDragAndDrop(
  dropZoneElement: HTMLElement,
  onFileDrop: (file: File) => void
): void {
  dropZoneElement.addEventListener('dragover', (e) => {
    e.preventDefault();
    dropZoneElement.classList.add('drag-over');
  });

  dropZoneElement.addEventListener('dragleave', () => {
    dropZoneElement.classList.remove('drag-over');
  });

  dropZoneElement.addEventListener('drop', (e) => {
    e.preventDefault();
    dropZoneElement.classList.remove('drag-over');

    const files = e.dataTransfer?.files;
    if (files && files.length > 0) {
      onFileDrop(files[0]);
    }
  });
}
```

---

## Testing Checklist

### ✅ Upload Endpoint Tests

- [ ] **Valid Upload (JPEG)**
  - [ ] File < 5MB uploads successfully
  - [ ] Response contains `logoUrl` and `thumbnailUrl`
  - [ ] Both URLs are accessible
  - [ ] Full size is 512x512 (or smaller with aspect ratio maintained)
  - [ ] Thumbnail is 128x128 (or smaller with aspect ratio maintained)

- [ ] **Valid Upload (PNG)**
  - [ ] PNG file converts to JPEG
  - [ ] Response URLs point to JPEG files

- [ ] **Valid Upload (SVG)**
  - [ ] SVG file uploaded without modification
  - [ ] `logoUrl` === `thumbnailUrl` (same file)
  - [ ] SVG is accessible and renders correctly

- [ ] **File Size Validation**
  - [ ] File > 5MB returns 400 error
  - [ ] Error message: "File size exceeds maximum limit of 5MB"

- [ ] **File Type Validation**
  - [ ] .txt file returns 400 error
  - [ ] .pdf file returns 400 error
  - [ ] Error message includes allowed types list

- [ ] **Authentication**
  - [ ] Missing token returns 401 error
  - [ ] Invalid token returns 401 error
  - [ ] Valid token allows upload

- [ ] **Replace Existing Logo**
  - [ ] Second upload deletes first logo from storage
  - [ ] Database updated with new URLs
  - [ ] Old URLs no longer accessible

### ✅ Get Endpoint Tests

- [ ] **Public Access**
  - [ ] GET /logo/{sponsorId} works without token
  - [ ] Returns correct sponsor's logo
  - [ ] Returns 404 if no logo set

- [ ] **Authenticated Access**
  - [ ] GET /logo with token returns own logo
  - [ ] Token's user ID used automatically

- [ ] **Response Data**
  - [ ] `sponsorId` matches request
  - [ ] `logoUrl` is accessible
  - [ ] `thumbnailUrl` is accessible
  - [ ] `updatedDate` in ISO 8601 format (or null)

### ✅ Delete Endpoint Tests

- [ ] **Successful Deletion**
  - [ ] Logo deleted from storage
  - [ ] Thumbnail deleted from storage
  - [ ] Database fields set to NULL
  - [ ] Returns success message

- [ ] **No Logo to Delete**
  - [ ] Returns success (idempotent operation)
  - [ ] Message: "No logo to delete"

- [ ] **Authentication**
  - [ ] Missing token returns 401
  - [ ] Invalid token returns 401

### ✅ Integration Tests

- [ ] **Full Workflow**
  - [ ] Upload → Get → Delete → Get (404)
  - [ ] Upload → Upload (replace) → Get (new logo)

- [ ] **Concurrent Uploads**
  - [ ] Multiple uploads from same sponsor
  - [ ] Only last upload persists

### ✅ Frontend Integration Tests

- [ ] **File Selection**
  - [ ] File input opens system picker
  - [ ] Selected file displays preview
  - [ ] File validation shows errors

- [ ] **Upload Progress**
  - [ ] Progress bar updates during upload
  - [ ] Success message shown on completion
  - [ ] Error message shown on failure

- [ ] **Logo Display**
  - [ ] Thumbnail displays correctly
  - [ ] Full size opens on click
  - [ ] Placeholder shown when no logo

- [ ] **Delete Confirmation**
  - [ ] Confirmation dialog appears
  - [ ] Cancel keeps logo
  - [ ] Confirm deletes logo

---

## Summary

### Endpoint Quick Reference

| Operation | Method | Endpoint | Auth Required | Description |
|-----------|--------|----------|---------------|-------------|
| **Upload** | POST | `/api/v1/sponsorship/logo` | ✅ Yes | Upload sponsor logo (max 5MB, jpg/png/gif/webp/svg) |
| **Get** | GET | `/api/v1/sponsorship/logo/{id?}` | ❌ No | Get logo info (public or authenticated) |
| **Delete** | DELETE | `/api/v1/sponsorship/logo` | ✅ Yes | Delete sponsor logo |

### Key Implementation Notes

1. **Always include `x-dev-arch-version: 1.0` header** in all requests
2. **JWT token required** for upload and delete operations
3. **FormData** required for upload (not JSON)
4. **SVG files** uploaded as-is, no resizing
5. **Raster images** resized to 512x512 (full) and 128x128 (thumbnail)
6. **Old logos automatically deleted** when uploading new logo
7. **Public GET access** allows viewing any sponsor's logo
8. **Authenticated GET** returns current user's logo (no ID needed)

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-26  
**Backend Code Reference**: Verified against actual implementation  
**Maintainer**: ZiraAI Development Team
