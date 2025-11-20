# ZiraAI Admin Operations - Frontend Integration Guide (Validated)

**Version:** 2.0
**Last Updated:** 2025-03-26
**Status:** ✅ Code-Validated & Production Ready

## Table of Contents

1. [Overview](#overview)
2. [Authentication](#authentication)
3. [API Endpoints](#api-endpoints)
4. [TypeScript Interfaces](#typescript-interfaces)
5. [Request/Response Examples](#requestresponse-examples)
6. [Error Handling](#error-handling)
7. [Best Practices](#best-practices)

---

## Overview

Bu doküman, ZiraAI Admin Operations API'sinin frontend entegrasyonu için **kod ile doğrulanmış** tam ve detaylı rehberdir. Tüm endpoint'ler, payload'lar ve response yapıları backend kodundan direkt olarak kontrol edilmiş ve doğrulanmıştır.

## 🔒 Critical Security Notice

**Admin User Protection**: All admin user management endpoints automatically exclude users with Admin role (ClaimId = 1) from operations. This is a security feature to prevent admins from viewing or managing other admin accounts.

### Protected Operations:
- **Query Endpoints** (GetAll, Search, GetById): Admin users are automatically filtered from results
- **Deactivate/Reactivate**: Operations on admin users will be rejected with "Access denied" error
- **Bulk Operations**: Admin users are automatically filtered from input lists

**Important**: This protection is applied at the database/business logic layer and cannot be bypassed. Admin users are completely isolated from admin panel operations.

### API Base URL

```typescript
// Staging
const API_BASE_URL = "https://ziraai-api-sit.up.railway.app/api/admin";

// Production
const API_BASE_URL = "https://api.ziraai.com/api/admin";
```

### Core Features

- ✅ **6 Admin Controllers** - Users, Subscriptions, Sponsorship, Analytics, Audit, PlantAnalysis
- ✅ **35+ Endpoints** - Full CRUD operations with advanced features
- ✅ **Full Audit Trail** - Every operation logged with AdminOperationLog
- ✅ **Pagination** - All list endpoints support page/pageSize
- ✅ **Filtering** - Date ranges, status filters, role filters
- ✅ **Export** - CSV export for analytics data

---

## Authentication

### Token Requirements

All admin endpoints require:
- **JWT Bearer Token** in Authorization header
- **Admin Role** in token claims

### Login Flow

```typescript
interface LoginRequest {
  email: string;
  password: string;
}

interface LoginResponse {
  success: boolean;
  data: {
    accessToken: string;
    refreshToken: string;
    expiration: string;
    user: {
      userId: number;
      fullName: string;
      email: string;
      roles: string[];
    }
  };
}

// Login
const login = async (credentials: LoginRequest): Promise<LoginResponse> => {
  const response = await fetch(`${API_BASE_URL}/../auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(credentials)
  });
  return response.json();
};

// Store token
localStorage.setItem('adminToken', data.accessToken);
```

### API Client Setup

```typescript
class AdminApiClient {
  private baseURL: string;
  private token: string | null;

  constructor(baseURL: string) {
    this.baseURL = baseURL;
    this.token = localStorage.getItem('adminToken');
  }

  private getHeaders(): HeadersInit {
    const headers: HeadersInit = {
      'Content-Type': 'application/json'
    };

    if (this.token) {
      headers['Authorization'] = `Bearer ${this.token}`;
    }

    return headers;
  }

  async get<T>(endpoint: string, params?: Record<string, any>): Promise<ApiResponse<T>> {
    const url = new URL(`${this.baseURL}${endpoint}`);
    if (params) {
      Object.keys(params).forEach(key => {
        if (params[key] !== undefined && params[key] !== null) {
          url.searchParams.append(key, String(params[key]));
        }
      });
    }

    const response = await fetch(url.toString(), {
      method: 'GET',
      headers: this.getHeaders()
    });

    return this.handleResponse<T>(response);
  }

  async post<T>(endpoint: string, data?: any): Promise<ApiResponse<T>> {
    const response = await fetch(`${this.baseURL}${endpoint}`, {
      method: 'POST',
      headers: this.getHeaders(),
      body: data ? JSON.stringify(data) : undefined
    });

    return this.handleResponse<T>(response);
  }

  private async handleResponse<T>(response: Response): Promise<ApiResponse<T>> {
    if (!response.ok) {
      if (response.status === 401) {
        // Redirect to login
        window.location.href = '/admin/login';
      }
      const error = await response.json();
      throw new Error(error.message || 'API request failed');
    }

    return response.json();
  }
}

// Initialize client
const adminApi = new AdminApiClient(API_BASE_URL);
```

---

## API Endpoints

### 1. User Management (`/api/admin/users`)

#### 1.1 Get All Users

**✅ Validated:** Controller + Handler + DTO

```typescript
interface GetUsersParams {
  page?: number;          // Default: 1
  pageSize?: number;      // Default: 50, Max: 100
  isActive?: boolean;     // Filter: true/false
  status?: string;        // Filter by status
}

interface UserDto {
  userId: number;
  fullName: string;
  email: string;
  mobilePhones: string;
  isActive: boolean;
  status: boolean;
  recordDate: string;     // ISO date
  lastLoginDate?: string; // ISO date
  deactivatedDate?: string;
  deactivatedByAdminId?: number;
  deactivationReason?: string;
}

interface PaginatedResponse<T> {
  success: boolean;
  message: string;
  data: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// GET /api/admin/users?page=1&pageSize=20&isActive=true
const getUsers = async (params: GetUsersParams): Promise<PaginatedResponse<UserDto>> => {
  return adminApi.get('/users', params);
};
```

**Example Request:**
```http
GET /api/admin/users?page=1&pageSize=20&isActive=true
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Example Response:**
```json
{
  "success": true,
  "message": "Users retrieved successfully",
  "data": [
    {
      "userId": 123,
      "fullName": "Ahmet Yılmaz",
      "email": "ahmet@example.com",
      "mobilePhones": "+905551234567",
      "isActive": true,
      "status": true,
      "recordDate": "2024-12-15T10:30:00",
      "lastLoginDate": "2025-03-20T14:25:00",
      "deactivatedDate": null,
      "deactivatedByAdminId": null
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8
}
```

---

#### 1.2 Get User By ID

**✅ Validated:** Controller + Handler

```typescript
// GET /api/admin/users/{userId}
const getUserById = async (userId: number): Promise<ApiResponse<UserDto>> => {
  return adminApi.get(`/users/${userId}`);
};
```

---

#### 1.3 Search Users

**✅ Validated:** Controller + Handler

```typescript
interface SearchUsersParams {
  searchTerm: string;    // Required
  page?: number;
  pageSize?: number;
}

// GET /api/admin/users/search?searchTerm=ahmet&page=1&pageSize=20
const searchUsers = async (params: SearchUsersParams): Promise<PaginatedResponse<UserDto>> => {
  return adminApi.get('/users/search', params);
};
```

---

#### 1.4 Deactivate User

**✅ Validated:** Controller + Command Handler + Request Model

```typescript
interface DeactivateUserRequest {
  reason: string;        // Required for audit trail
}

interface ApiResponse<T = void> {
  success: boolean;
  message: string;
  data?: T;
}

// POST /api/admin/users/{userId}/deactivate
const deactivateUser = async (
  userId: number,
  request: DeactivateUserRequest
): Promise<ApiResponse> => {
  return adminApi.post(`/users/${userId}/deactivate`, request);
};
```

**Example Request:**
```json
{
  "reason": "Account violation - multiple spam reports"
}
```

**Example Response:**
```json
{
  "success": true,
  "message": "User Ahmet Yılmaz deactivated successfully"
}
```

---

#### 1.5 Reactivate User

**✅ Validated:** Controller + Command Handler

```typescript
interface ReactivateUserRequest {
  reason: string;
}

// POST /api/admin/users/{userId}/reactivate
const reactivateUser = async (
  userId: number,
  request: ReactivateUserRequest
): Promise<ApiResponse> => {
  return adminApi.post(`/users/${userId}/reactivate`, request);
};
```

---

#### 1.6 Bulk Deactivate Users

**✅ Validated:** Controller + Command Handler + Request Model

```typescript
interface BulkDeactivateUsersRequest {
  userIds: number[];     // Required
  reason: string;        // Required
}

interface BulkOperationResult {
  totalRequested: number;
  successCount: number;
  failedCount: number;
  results: Array<{
    userId: number;
    success: boolean;
    message: string;
  }>;
}

// POST /api/admin/users/bulk/deactivate
const bulkDeactivateUsers = async (
  request: BulkDeactivateUsersRequest
): Promise<ApiResponse<BulkOperationResult>> => {
  return adminApi.post('/users/bulk/deactivate', request);
};
```

**Example Request:**
```json
{
  "userIds": [123, 456, 789],
  "reason": "Bulk cleanup - inactive accounts over 1 year"
}
```

**Example Response:**
```json
{
  "success": true,
  "message": "Successfully deactivated 3 users",
  "data": {
    "totalRequested": 3,
    "successCount": 3,
    "failedCount": 0,
    "results": [
      {
        "userId": 123,
        "success": true,
        "message": "Deactivated successfully"
      },
      {
        "userId": 456,
        "success": true,
        "message": "Deactivated successfully"
      },
      {
        "userId": 789,
        "success": true,
        "message": "Deactivated successfully"
      }
    ]
  }
}
```

---

### 2. Subscription Management (`/api/admin/subscriptions`)

#### 2.1 Get All Subscriptions

**✅ Validated:** Controller + Query Handler

```typescript
interface GetSubscriptionsParams {
  page?: number;
  pageSize?: number;
  status?: string;                    // "Active" | "Expired" | "Cancelled"
  isActive?: boolean;
  isSponsoredSubscription?: boolean;
}

interface SubscriptionDto {
  id: number;
  userId: number;
  userName: string;
  subscriptionTierId: number;
  tierName: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
  status: string;
  dailyRequestLimit: number;
  monthlyRequestLimit: number;
  currentDailyUsage: number;
  currentMonthlyUsage: number;
  isTrialSubscription: boolean;
  isSponsoredSubscription: boolean;
  autoRenew: boolean;
  createdDate: string;
}

// GET /api/admin/subscriptions?page=1&status=Active&pageSize=20
const getSubscriptions = async (
  params: GetSubscriptionsParams
): Promise<PaginatedResponse<SubscriptionDto>> => {
  return adminApi.get('/subscriptions', params);
};
```

---

#### 2.2 Get Subscription By ID

**✅ Validated:** Controller + Query Handler

```typescript
// GET /api/admin/subscriptions/{subscriptionId}
const getSubscriptionById = async (
  subscriptionId: number
): Promise<ApiResponse<SubscriptionDto>> => {
  return adminApi.get(`/subscriptions/${subscriptionId}`);
};
```

---

#### 2.3 Assign Subscription

**✅ Validated:** Controller + Command Handler + Request Model

```typescript
interface AssignSubscriptionRequest {
  userId: number;                    // Required
  subscriptionTierId: number;        // Required (1=Trial, 2=S, 3=M, 4=L, 5=XL)
  durationMonths: number;            // Required
  isSponsoredSubscription: boolean;  // Default: false
  sponsorId?: number;                // Required if isSponsoredSubscription=true
  notes?: string;                    // Optional
}

interface AssignSubscriptionResponse {
  id: number;
  userId: number;
  tierName: string;
  startDate: string;
  endDate: string;
  status: string;
}

// POST /api/admin/subscriptions/assign
const assignSubscription = async (
  request: AssignSubscriptionRequest
): Promise<ApiResponse<AssignSubscriptionResponse>> => {
  return adminApi.post('/subscriptions/assign', request);
};
```

**Example Request:**
```json
{
  "userId": 123,
  "subscriptionTierId": 3,
  "durationMonths": 12,
  "isSponsoredSubscription": false,
  "notes": "Premium tier for beta tester"
}
```

**Example Response:**
```json
{
  "success": true,
  "message": "Subscription assigned successfully to Ahmet Yılmaz",
  "data": {
    "id": 789,
    "userId": 123,
    "tierName": "M",
    "startDate": "2025-03-26T00:00:00",
    "endDate": "2026-03-26T00:00:00",
    "status": "Active"
  }
}
```

---

#### 2.4 Extend Subscription

**✅ Validated:** Controller + Command Handler + Request Model

```typescript
interface ExtendSubscriptionRequest {
  extensionMonths: number;  // Required
  notes?: string;
}

interface ExtendSubscriptionResponse {
  id: number;
  oldEndDate: string;
  newEndDate: string;
}

// POST /api/admin/subscriptions/{subscriptionId}/extend
const extendSubscription = async (
  subscriptionId: number,
  request: ExtendSubscriptionRequest
): Promise<ApiResponse<ExtendSubscriptionResponse>> => {
  return adminApi.post(`/subscriptions/${subscriptionId}/extend`, request);
};
```

**Example Request:**
```json
{
  "extensionMonths": 3,
  "notes": "Compensation for service downtime"
}
```

---

#### 2.5 Cancel Subscription

**✅ Validated:** Controller + Command Handler + Request Model

```typescript
interface CancelSubscriptionRequest {
  cancellationReason: string;  // Required
}

// POST /api/admin/subscriptions/{subscriptionId}/cancel
const cancelSubscription = async (
  subscriptionId: number,
  request: CancelSubscriptionRequest
): Promise<ApiResponse> => {
  return adminApi.post(`/subscriptions/${subscriptionId}/cancel`, request);
};
```

---

#### 2.6 Bulk Cancel Subscriptions

**✅ Validated:** Controller + Command Handler + Request Model

```typescript
interface BulkCancelSubscriptionsRequest {
  subscriptionIds: number[];  // Required
  cancellationReason: string; // Required
}

// POST /api/admin/subscriptions/bulk/cancel
const bulkCancelSubscriptions = async (
  request: BulkCancelSubscriptionsRequest
): Promise<ApiResponse<BulkOperationResult>> => {
  return adminApi.post('/subscriptions/bulk/cancel', request);
};
```

---

### 3. Sponsorship Management (`/api/admin/sponsorship`)

#### 3.1 Get All Purchases

**✅ Validated:** Controller + Query Handler

```typescript
interface GetPurchasesParams {
  page?: number;
  pageSize?: number;
  status?: string;         // "Active" | "Pending" | "Cancelled"
  paymentStatus?: string;  // "Completed" | "Pending" | "Refunded"
  sponsorId?: number;
}

interface SponsorshipPurchaseDto {
  id: number;
  sponsorId: number;
  sponsorName: string;
  subscriptionTierId: number;
  tierName: string;
  quantity: number;
  unitPrice: number;
  totalAmount: number;
  currency: string;
  purchaseDate: string;
  paymentMethod: string;
  paymentReference?: string;
  paymentStatus: string;
  paymentCompletedDate?: string;
  companyName: string;
  taxNumber?: string;
  invoiceAddress?: string;
  status: string;
  codesGenerated: number;
  codesUsed: number;
  createdDate: string;
}

// GET /api/admin/sponsorship/purchases?page=1&paymentStatus=Completed
const getPurchases = async (
  params: GetPurchasesParams
): Promise<PaginatedResponse<SponsorshipPurchaseDto>> => {
  return adminApi.get('/sponsorship/purchases', params);
};
```

---

#### 3.2 Get Purchase By ID

**✅ Validated:** Controller + Query Handler

```typescript
// GET /api/admin/sponsorship/purchases/{purchaseId}
const getPurchaseById = async (
  purchaseId: number
): Promise<ApiResponse<SponsorshipPurchaseDto>> => {
  return adminApi.get(`/sponsorship/purchases/${purchaseId}`);
};
```

---

#### 3.3 Get Sponsorship Statistics

**✅ Validated:** Controller + Query Handler + DTO (from code)

```typescript
interface GetStatisticsParams {
  startDate?: string;  // ISO date
  endDate?: string;    // ISO date
}

interface SponsorshipStatisticsDto {
  totalPurchases: number;
  completedPurchases: number;
  pendingPurchases: number;
  refundedPurchases: number;
  totalRevenue: number;
  totalCodesGenerated: number;
  totalCodesUsed: number;
  totalCodesActive: number;
  totalCodesExpired: number;
  codeRedemptionRate: number;        // Percentage (0-100)
  averagePurchaseAmount: number;
  totalQuantityPurchased: number;
  uniqueSponsorCount: number;
  startDate?: string;
  endDate?: string;
  generatedAt: string;
}

// GET /api/admin/sponsorship/statistics?startDate=2025-01-01&endDate=2025-03-26
const getSponsorshipStatistics = async (
  params: GetStatisticsParams
): Promise<ApiResponse<SponsorshipStatisticsDto>> => {
  return adminApi.get('/sponsorship/statistics', params);
};
```

**Example Response:**
```json
{
  "success": true,
  "data": {
    "totalPurchases": 458,
    "completedPurchases": 412,
    "pendingPurchases": 28,
    "refundedPurchases": 18,
    "totalRevenue": 2850000.00,
    "totalCodesGenerated": 45800,
    "totalCodesUsed": 32145,
    "totalCodesActive": 8920,
    "totalCodesExpired": 4735,
    "codeRedemptionRate": 70.2,
    "averagePurchaseAmount": 6222.71,
    "totalQuantityPurchased": 45800,
    "uniqueSponsorCount": 124,
    "generatedAt": "2025-03-26T11:00:00"
  }
}
```

---

#### 3.4 Approve Purchase

**✅ Validated:** Controller + Command Handler + Request Model

```typescript
interface ApprovePurchaseRequest {
  notes?: string;
}

// POST /api/admin/sponsorship/purchases/{purchaseId}/approve
const approvePurchase = async (
  purchaseId: number,
  request: ApprovePurchaseRequest
): Promise<ApiResponse> => {
  return adminApi.post(`/sponsorship/purchases/${purchaseId}/approve`, request);
};
```

---

#### 3.5 Refund Purchase

**✅ Validated:** Controller + Command Handler + Request Model

```typescript
interface RefundPurchaseRequest {
  refundReason: string;  // Required
}

// POST /api/admin/sponsorship/purchases/{purchaseId}/refund
const refundPurchase = async (
  purchaseId: number,
  request: RefundPurchaseRequest
): Promise<ApiResponse> => {
  return adminApi.post(`/sponsorship/purchases/${purchaseId}/refund`, request);
};
```

---

#### 3.6 Create Purchase On Behalf Of Sponsor

**✅ Validated:** Controller + Command Handler + Request Model

```typescript
interface CreatePurchaseOnBehalfOfRequest {
  sponsorId: number;                // Required
  subscriptionTierId: number;       // Required (2=S, 3=M, 4=L, 5=XL)
  quantity: number;                 // Required
  unitPrice: number;                // Required
  autoApprove: boolean;             // Default: false
  paymentMethod: string;            // "Manual" | "Offline" | "BankTransfer"
  paymentReference?: string;
  companyName: string;              // Required
  taxNumber?: string;
  invoiceAddress?: string;
  codePrefix?: string;              // Optional custom prefix
  validityDays?: number;            // Default: 365
  notes?: string;
}

interface CreatePurchaseResponse {
  id: number;
  sponsorId: number;
  quantity: number;
  totalAmount: number;
  currency: string;
  paymentStatus: string;
  status: string;
  purchaseDate: string;
}

// POST /api/admin/sponsorship/purchases/create-on-behalf-of
const createPurchaseOnBehalfOf = async (
  request: CreatePurchaseOnBehalfOfRequest
): Promise<ApiResponse<CreatePurchaseResponse>> => {
  return adminApi.post('/sponsorship/purchases/create-on-behalf-of', request);
};
```

**Example Request:**
```json
{
  "sponsorId": 234,
  "subscriptionTierId": 3,
  "quantity": 50,
  "unitPrice": 99.99,
  "autoApprove": true,
  "paymentMethod": "BankTransfer",
  "paymentReference": "TRX-2025-0326-001",
  "companyName": "Tarım Teknolojileri A.Ş.",
  "taxNumber": "1234567890",
  "invoiceAddress": "İstanbul, Turkey",
  "codePrefix": "TARIM",
  "validityDays": 365,
  "notes": "Corporate partnership - annual package"
}
```

**Use Cases:**
1. **Manual/Offline Payments**: Set `autoApprove: true` for bank transfers
2. **Corporate Partnerships**: Create packages without online payment
3. **Test Accounts**: Generate demo codes for testing

---

#### 3.7 Get All Codes

**✅ Validated:** Controller + Query Handler

```typescript
interface GetCodesParams {
  page?: number;
  pageSize?: number;
  isUsed?: boolean;
  isActive?: boolean;
  sponsorId?: number;
  purchaseId?: number;
}

interface SponsorshipCodeDto {
  id: number;
  code: string;
  purchaseId: number;
  sponsorId: number;
  subscriptionTierId: number;
  tierName: string;
  isUsed: boolean;
  isActive: boolean;
  expiryDate: string;
  usedDate?: string;
  usedByUserId?: number;
  usedByUserName?: string;
  distributionDate?: string;
  createdDate: string;
}

// GET /api/admin/sponsorship/codes?page=1&isUsed=false&isActive=true
const getCodes = async (
  params: GetCodesParams
): Promise<PaginatedResponse<SponsorshipCodeDto>> => {
  return adminApi.get('/sponsorship/codes', params);
};
```

---

#### 3.8 Get Code By ID

**✅ Validated:** Controller + Query Handler

```typescript
// GET /api/admin/sponsorship/codes/{codeId}
const getCodeById = async (
  codeId: number
): Promise<ApiResponse<SponsorshipCodeDto>> => {
  return adminApi.get(`/sponsorship/codes/${codeId}`);
};
```

---

#### 3.9 Deactivate Code

**✅ Validated:** Controller + Command Handler + Request Model

```typescript
interface DeactivateCodeRequest {
  reason: string;  // Required
}

// POST /api/admin/sponsorship/codes/{codeId}/deactivate
const deactivateCode = async (
  codeId: number,
  request: DeactivateCodeRequest
): Promise<ApiResponse> => {
  return adminApi.post(`/sponsorship/codes/${codeId}/deactivate`, request);
};
```

---

#### 3.10 Bulk Send Codes

**✅ Validated:** Controller + Command Handler + Request Model

```typescript
interface BulkSendCodesRequest {
  sponsorId: number;      // Required
  purchaseId: number;     // Required
  recipients: Array<{     // Required
    phoneNumber: string;  // Required, format: +905551234567
    name?: string;        // Optional
  }>;
  sendVia?: string;       // "SMS" | "WhatsApp" | "Email", default: "SMS"
}

// POST /api/admin/sponsorship/codes/bulk-send
const bulkSendCodes = async (
  request: BulkSendCodesRequest
): Promise<ApiResponse> => {
  return adminApi.post('/sponsorship/codes/bulk-send', request);
};
```

**Example Request:**
```json
{
  "sponsorId": 234,
  "purchaseId": 892,
  "recipients": [
    {
      "phoneNumber": "+905551234567",
      "name": "Mehmet Demir"
    },
    {
      "phoneNumber": "+905557654321",
      "name": "Ayşe Kaya"
    },
    {
      "phoneNumber": "+905559876543",
      "name": "Ali Öztürk"
    }
  ],
  "sendVia": "SMS"
}
```

**Process Flow:**
1. Validates sponsor and purchase exist
2. Fetches unused codes from the purchase
3. Assigns each code to a recipient
4. Sends link via selected method (SMS/WhatsApp/Email)
5. Updates code records with recipient info and send date
6. Creates audit log entry

---

#### 3.11 Get Sponsor Detailed Report

**✅ Validated:** Controller + Query Handler

```typescript
interface SponsorDetailedReportDto {
  sponsorId: number;
  sponsorName: string;
  sponsorEmail: string;

  // Purchase Statistics
  totalPurchases: number;
  activePurchases: number;
  pendingPurchases: number;
  cancelledPurchases: number;
  completedPurchases: number;
  totalSpent: number;

  // Code Statistics
  totalCodesGenerated: number;
  totalCodesSent: number;
  totalCodesUsed: number;
  totalCodesActive: number;
  totalCodesExpired: number;
  codeRedemptionRate: number;

  // Detailed Purchases
  purchases: Array<{
    id: number;
    tierName: string;
    quantity: number;
    totalAmount: number;
    currency: string;
    status: string;
    paymentStatus: string;
    purchaseDate: string;
    codesGenerated: number;
    codesUsed: number;
    codesSent: number;
  }>;

  // Code Distribution
  codeDistribution: {
    unused: number;
    used: number;
    expired: number;
    deactivated: number;
    sent: number;
    notSent: number;
  };
}

// GET /api/admin/sponsorship/sponsors/{sponsorId}/detailed-report
const getSponsorDetailedReport = async (
  sponsorId: number
): Promise<ApiResponse<SponsorDetailedReportDto>> => {
  return adminApi.get(`/sponsorship/sponsors/${sponsorId}/detailed-report`);
};
```

---

### 4. Analytics & Reporting (`/api/admin/analytics`)

#### 4.1 Get User Statistics

**✅ Validated:** Controller + Query Handler + DTO (from code)

```typescript
interface GetStatisticsParams {
  startDate?: string;  // ISO date
  endDate?: string;    // ISO date
}

interface UserStatisticsDto {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  farmerUsers: number;
  sponsorUsers: number;
  adminUsers: number;
  usersRegisteredToday: number;
  usersRegisteredThisWeek: number;
  usersRegisteredThisMonth: number;
  startDate?: string;
  endDate?: string;
  generatedAt: string;
}

// GET /api/admin/analytics/user-statistics?startDate=2025-01-01
const getUserStatistics = async (
  params: GetStatisticsParams
): Promise<ApiResponse<UserStatisticsDto>> => {
  return adminApi.get('/analytics/user-statistics', params);
};
```

**Example Response:**
```json
{
  "success": true,
  "data": {
    "totalUsers": 5420,
    "activeUsers": 4850,
    "inactiveUsers": 570,
    "farmerUsers": 4200,
    "sponsorUsers": 150,
    "adminUsers": 10,
    "usersRegisteredToday": 12,
    "usersRegisteredThisWeek": 85,
    "usersRegisteredThisMonth": 245,
    "generatedAt": "2025-03-26T11:00:00"
  }
}
```

---

#### 4.2 Get Subscription Statistics

**✅ Validated:** Controller + Query Handler + DTO (from code)

```typescript
interface SubscriptionStatisticsDto {
  totalSubscriptions: number;
  activeSubscriptions: number;
  expiredSubscriptions: number;
  trialSubscriptions: number;
  sponsoredSubscriptions: number;
  paidSubscriptions: number;
  subscriptionsByTier: Record<string, number>;  // { "Trial": 1250, "S": 450, ... }
  totalRevenue: number;
  averageSubscriptionDuration: number;          // In days
  startDate?: string;
  endDate?: string;
  generatedAt: string;
}

// GET /api/admin/analytics/subscription-statistics
const getSubscriptionStatistics = async (
  params: GetStatisticsParams
): Promise<ApiResponse<SubscriptionStatisticsDto>> => {
  return adminApi.get('/analytics/subscription-statistics', params);
};
```

**Example Response:**
```json
{
  "success": true,
  "data": {
    "totalSubscriptions": 3245,
    "activeSubscriptions": 2890,
    "expiredSubscriptions": 355,
    "trialSubscriptions": 1250,
    "sponsoredSubscriptions": 1200,
    "paidSubscriptions": 795,
    "subscriptionsByTier": {
      "Trial": 1250,
      "S": 450,
      "M": 780,
      "L": 550,
      "XL": 215
    },
    "totalRevenue": 125000.00,
    "averageSubscriptionDuration": 38.5,
    "generatedAt": "2025-03-26T11:00:00"
  }
}
```

---

#### 4.3 Get Sponsorship Statistics

**✅ Validated:** Already covered in section 3.3

---

#### 4.4 Get Dashboard Overview

**✅ Validated:** Controller (parallel queries)

```typescript
interface DashboardOverviewDto {
  userStatistics: UserStatisticsDto;
  subscriptionStatistics: SubscriptionStatisticsDto;
  sponsorshipStatistics: SponsorshipStatisticsDto;
  generatedAt: string;
}

// GET /api/admin/analytics/dashboard-overview
const getDashboardOverview = async (): Promise<ApiResponse<DashboardOverviewDto>> => {
  return adminApi.get('/analytics/dashboard-overview');
};
```

**Performance Note:** This endpoint executes all three statistics queries in parallel using `Task.WhenAll()` for optimal performance.

---

#### 4.5 Get Activity Logs

**✅ Validated:** Controller + Query Handler

```typescript
interface GetActivityLogsParams {
  page?: number;          // Default: 1
  pageSize?: number;      // Default: 10
  userId?: number;        // Filter by admin or target user
  actionType?: string;    // Filter by action type
  startDate?: string;
  endDate?: string;
}

interface ActivityLogDto {
  id: number;
  action: string;
  adminUserId: number;
  adminUserName: string;
  targetUserId?: number;
  targetUserName?: string;
  entityType?: string;
  entityId?: number;
  isOnBehalfOf: boolean;
  ipAddress: string;
  userAgent: string;
  requestPath: string;
  reason?: string;
  beforeState?: string;  // JSON
  afterState?: string;   // JSON
  createdDate: string;
}

// GET /api/admin/analytics/activity-logs?page=1&pageSize=20&userId=1
const getActivityLogs = async (
  params: GetActivityLogsParams
): Promise<PaginatedResponse<ActivityLogDto>> => {
  return adminApi.get('/analytics/activity-logs', params);
};
```

---

#### 4.6 Export Statistics

**✅ Validated:** Controller + Query Handler

```typescript
interface ExportParams {
  startDate?: string;
  endDate?: string;
}

// GET /api/admin/analytics/export?startDate=2025-01-01&endDate=2025-03-26
const exportStatistics = async (params: ExportParams): Promise<Blob> => {
  const url = new URL(`${adminApi.baseURL}/analytics/export`);
  Object.keys(params).forEach(key => {
    if (params[key]) {
      url.searchParams.append(key, params[key]);
    }
  });

  const response = await fetch(url.toString(), {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${adminApi.token}`
    }
  });

  if (!response.ok) {
    throw new Error('Export failed');
  }

  return response.blob();
};

// Usage
const downloadCSV = async () => {
  const blob = await exportStatistics({
    startDate: '2025-01-01',
    endDate: '2025-03-26'
  });

  const url = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `ziraai-statistics-${new Date().toISOString()}.csv`;
  a.click();
  window.URL.revokeObjectURL(url);
};
```

**Response:** CSV file download with naming: `ziraai-statistics-{timestamp}.csv`

---

### 5. Audit Logs (`/api/admin/audit`)

#### 5.1 Get All Audit Logs

**✅ Validated:** Controller + Query Handler

```typescript
interface GetAuditLogsParams {
  page?: number;
  pageSize?: number;
  action?: string;         // Filter by action type
  entityType?: string;     // Filter by entity type
  isOnBehalfOf?: boolean;  // Filter OBO operations
  startDate?: string;
  endDate?: string;
}

interface AuditLogDto {
  id: number;
  action: string;
  adminUserId: number;
  adminUserName: string;
  targetUserId?: number;
  targetUserName?: string;
  entityType?: string;
  entityId?: number;
  isOnBehalfOf: boolean;
  ipAddress: string;
  userAgent: string;
  requestPath: string;
  reason?: string;
  beforeState?: string;  // JSON
  afterState?: string;   // JSON
  createdDate: string;
}

// GET /api/admin/audit?page=1&pageSize=50&isOnBehalfOf=true
const getAllAuditLogs = async (
  params: GetAuditLogsParams
): Promise<PaginatedResponse<AuditLogDto>> => {
  return adminApi.get('/audit', params);
};
```

---

#### 5.2 Get Audit Logs By Admin

**✅ Validated:** Controller + Query Handler

```typescript
interface GetAuditLogsByAdminParams {
  page?: number;
  pageSize?: number;
  startDate?: string;
  endDate?: string;
}

// GET /api/admin/audit/admin/{adminUserId}?page=1&pageSize=50
const getAuditLogsByAdmin = async (
  adminUserId: number,
  params: GetAuditLogsByAdminParams
): Promise<PaginatedResponse<AuditLogDto>> => {
  return adminApi.get(`/audit/admin/${adminUserId}`, params);
};
```

---

#### 5.3 Get Audit Logs By Target User

**✅ Validated:** Controller + Query Handler

```typescript
// GET /api/admin/audit/target/{targetUserId}?page=1&pageSize=50
const getAuditLogsByTarget = async (
  targetUserId: number,
  params: GetAuditLogsByAdminParams
): Promise<PaginatedResponse<AuditLogDto>> => {
  return adminApi.get(`/audit/target/${targetUserId}`, params);
};
```

---

#### 5.4 Get On-Behalf-Of Logs

**✅ Validated:** Controller + Query Handler

```typescript
// GET /api/admin/audit/on-behalf-of?page=1&pageSize=20
const getOnBehalfOfLogs = async (
  params: GetAuditLogsByAdminParams
): Promise<PaginatedResponse<AuditLogDto>> => {
  return adminApi.get('/audit/on-behalf-of', params);
};
```

---

### 6. Plant Analysis Management (`/api/admin/plant-analysis`)

#### 6.1 Create Analysis On Behalf Of User

**✅ Validated:** Controller + Command Handler + Request Model

```typescript
interface CreateAnalysisOnBehalfOfRequest {
  targetUserId: number;  // Required
  imageUrl: string;      // Required
  notes?: string;
}

interface CreateAnalysisResponse {
  id: number;
  userId: number;
  imageUrl: string;
  analysisStatus: string;
  isOnBehalfOf: boolean;
  createdByAdminId: number;
  createdDate: string;
}

// POST /api/admin/plant-analysis/on-behalf-of
const createAnalysisOnBehalfOf = async (
  request: CreateAnalysisOnBehalfOfRequest
): Promise<ApiResponse<CreateAnalysisResponse>> => {
  return adminApi.post('/plant-analysis/on-behalf-of', request);
};
```

**Example Request:**
```json
{
  "targetUserId": 123,
  "imageUrl": "https://storage.ziraai.com/images/plant-12345.jpg",
  "notes": "Sample analysis for demonstration"
}
```

---

#### 6.2 Get User Analyses

**✅ Validated:** Controller + Query Handler

```typescript
interface GetUserAnalysesParams {
  page?: number;
  pageSize?: number;
  status?: string;        // "completed" | "pending" | "failed"
  isOnBehalfOf?: boolean;
}

interface PlantAnalysisDto {
  id: number;
  userId: number;
  imageUrl: string;
  analysisStatus: string;
  analysisResult?: string;
  disease?: string;
  cropType?: string;
  severity?: string;
  confidence?: number;
  isOnBehalfOf: boolean;
  createdByAdminId?: number;
  createdDate: string;
  completedDate?: string;
}

// GET /api/admin/plant-analysis/user/{userId}?page=1&isOnBehalfOf=true
const getUserAnalyses = async (
  userId: number,
  params: GetUserAnalysesParams
): Promise<PaginatedResponse<PlantAnalysisDto>> => {
  return adminApi.get(`/plant-analysis/user/${userId}`, params);
};
```

---

#### 6.3 Get All OBO Analyses

**✅ Validated:** Controller + Query Handler

```typescript
interface GetAllOBOAnalysesParams {
  page?: number;
  pageSize?: number;
  adminUserId?: number;
  targetUserId?: number;
  status?: string;
}

// GET /api/admin/plant-analysis/on-behalf-of?page=1&adminUserId=1
const getAllOBOAnalyses = async (
  params: GetAllOBOAnalysesParams
): Promise<PaginatedResponse<PlantAnalysisDto>> => {
  return adminApi.get('/plant-analysis/on-behalf-of', params);
};
```

---

## TypeScript Interfaces

### Complete Type Definitions

```typescript
// ======================
// Base Types
// ======================
interface ApiResponse<T = any> {
  success: boolean;
  message: string;
  data?: T;
}

interface PaginatedResponse<T> extends ApiResponse<T[]> {
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

interface BulkOperationResult {
  totalRequested: number;
  successCount: number;
  failedCount: number;
  results: Array<{
    userId?: number;
    subscriptionId?: number;
    success: boolean;
    message: string;
  }>;
}

// ======================
// Admin Base Controller Properties
// ======================
interface AdminContext {
  adminUserId: number;
  adminUserEmail: string;
  adminUserName: string;
  clientIpAddress: string;
  userAgent: string;
  requestPath: string;
  isOnBehalfOfOperation: boolean;
  onBehalfOfTargetUserId?: number;
  onBehalfOfReason?: string;
}

// ======================
// Users
// ======================
interface UserDto {
  userId: number;
  fullName: string;
  email: string;
  mobilePhones: string;
  isActive: boolean;
  status: boolean;
  recordDate: string;
  lastLoginDate?: string;
  deactivatedDate?: string;
  deactivatedByAdminId?: number;
  deactivationReason?: string;
}

interface DeactivateUserRequest {
  reason: string;
}

interface ReactivateUserRequest {
  reason: string;
}

interface BulkDeactivateUsersRequest {
  userIds: number[];
  reason: string;
}

// ======================
// Subscriptions
// ======================
interface SubscriptionDto {
  id: number;
  userId: number;
  userName: string;
  subscriptionTierId: number;
  tierName: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
  status: string;
  dailyRequestLimit: number;
  monthlyRequestLimit: number;
  currentDailyUsage: number;
  currentMonthlyUsage: number;
  isTrialSubscription: boolean;
  isSponsoredSubscription: boolean;
  autoRenew: boolean;
  createdDate: string;
}

interface AssignSubscriptionRequest {
  userId: number;
  subscriptionTierId: number;
  durationMonths: number;
  isSponsoredSubscription: boolean;
  sponsorId?: number;
  notes?: string;
}

interface ExtendSubscriptionRequest {
  extensionMonths: number;
  notes?: string;
}

interface CancelSubscriptionRequest {
  cancellationReason: string;
}

interface BulkCancelSubscriptionsRequest {
  subscriptionIds: number[];
  cancellationReason: string;
}

// ======================
// Sponsorship
// ======================
interface SponsorshipPurchaseDto {
  id: number;
  sponsorId: number;
  sponsorName: string;
  subscriptionTierId: number;
  tierName: string;
  quantity: number;
  unitPrice: number;
  totalAmount: number;
  currency: string;
  purchaseDate: string;
  paymentMethod: string;
  paymentReference?: string;
  paymentStatus: string;
  paymentCompletedDate?: string;
  companyName: string;
  taxNumber?: string;
  invoiceAddress?: string;
  status: string;
  codesGenerated: number;
  codesUsed: number;
  createdDate: string;
}

interface SponsorshipCodeDto {
  id: number;
  code: string;
  purchaseId: number;
  sponsorId: number;
  subscriptionTierId: number;
  tierName: string;
  isUsed: boolean;
  isActive: boolean;
  expiryDate: string;
  usedDate?: string;
  usedByUserId?: number;
  usedByUserName?: string;
  distributionDate?: string;
  createdDate: string;
}

interface CreatePurchaseOnBehalfOfRequest {
  sponsorId: number;
  subscriptionTierId: number;
  quantity: number;
  unitPrice: number;
  autoApprove: boolean;
  paymentMethod: string;
  paymentReference?: string;
  companyName: string;
  taxNumber?: string;
  invoiceAddress?: string;
  codePrefix?: string;
  validityDays?: number;
  notes?: string;
}

interface BulkSendCodesRequest {
  sponsorId: number;
  purchaseId: number;
  recipients: Array<{
    phoneNumber: string;
    name?: string;
  }>;
  sendVia?: 'SMS' | 'WhatsApp' | 'Email';
}

interface ApprovePurchaseRequest {
  notes?: string;
}

interface RefundPurchaseRequest {
  refundReason: string;
}

interface DeactivateCodeRequest {
  reason: string;
}

interface SponsorDetailedReportDto {
  sponsorId: number;
  sponsorName: string;
  sponsorEmail: string;
  totalPurchases: number;
  activePurchases: number;
  pendingPurchases: number;
  cancelledPurchases: number;
  completedPurchases: number;
  totalSpent: number;
  totalCodesGenerated: number;
  totalCodesSent: number;
  totalCodesUsed: number;
  totalCodesActive: number;
  totalCodesExpired: number;
  codeRedemptionRate: number;
  purchases: Array<{
    id: number;
    tierName: string;
    quantity: number;
    totalAmount: number;
    currency: string;
    status: string;
    paymentStatus: string;
    purchaseDate: string;
    codesGenerated: number;
    codesUsed: number;
    codesSent: number;
  }>;
  codeDistribution: {
    unused: number;
    used: number;
    expired: number;
    deactivated: number;
    sent: number;
    notSent: number;
  };
}

// ======================
// Analytics
// ======================
interface UserStatisticsDto {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  farmerUsers: number;
  sponsorUsers: number;
  adminUsers: number;
  usersRegisteredToday: number;
  usersRegisteredThisWeek: number;
  usersRegisteredThisMonth: number;
  startDate?: string;
  endDate?: string;
  generatedAt: string;
}

interface SubscriptionStatisticsDto {
  totalSubscriptions: number;
  activeSubscriptions: number;
  expiredSubscriptions: number;
  trialSubscriptions: number;
  sponsoredSubscriptions: number;
  paidSubscriptions: number;
  subscriptionsByTier: Record<string, number>;
  totalRevenue: number;
  averageSubscriptionDuration: number;
  startDate?: string;
  endDate?: string;
  generatedAt: string;
}

interface SponsorshipStatisticsDto {
  totalPurchases: number;
  completedPurchases: number;
  pendingPurchases: number;
  refundedPurchases: number;
  totalRevenue: number;
  totalCodesGenerated: number;
  totalCodesUsed: number;
  totalCodesActive: number;
  totalCodesExpired: number;
  codeRedemptionRate: number;
  averagePurchaseAmount: number;
  totalQuantityPurchased: number;
  uniqueSponsorCount: number;
  startDate?: string;
  endDate?: string;
  generatedAt: string;
}

interface DashboardOverviewDto {
  userStatistics: UserStatisticsDto;
  subscriptionStatistics: SubscriptionStatisticsDto;
  sponsorshipStatistics: SponsorshipStatisticsDto;
  generatedAt: string;
}

interface ActivityLogDto {
  id: number;
  action: string;
  adminUserId: number;
  adminUserName: string;
  targetUserId?: number;
  targetUserName?: string;
  entityType?: string;
  entityId?: number;
  isOnBehalfOf: boolean;
  ipAddress: string;
  userAgent: string;
  requestPath: string;
  reason?: string;
  beforeState?: string;
  afterState?: string;
  createdDate: string;
}

// ======================
// Audit Logs
// ======================
interface AuditLogDto {
  id: number;
  action: string;
  adminUserId: number;
  adminUserName: string;
  targetUserId?: number;
  targetUserName?: string;
  entityType?: string;
  entityId?: number;
  isOnBehalfOf: boolean;
  ipAddress: string;
  userAgent: string;
  requestPath: string;
  reason?: string;
  beforeState?: string;
  afterState?: string;
  createdDate: string;
}

// ======================
// Plant Analysis
// ======================
interface PlantAnalysisDto {
  id: number;
  userId: number;
  imageUrl: string;
  analysisStatus: string;
  analysisResult?: string;
  disease?: string;
  cropType?: string;
  severity?: string;
  confidence?: number;
  isOnBehalfOf: boolean;
  createdByAdminId?: number;
  createdDate: string;
  completedDate?: string;
}

interface CreateAnalysisOnBehalfOfRequest {
  targetUserId: number;
  imageUrl: string;
  notes?: string;
}
```

---

## Error Handling

### Standard Error Responses

**✅ Validated:** From BaseApiController pattern

```typescript
interface ErrorResponse {
  success: false;
  message: string;
  errors?: string[];
  statusCode: number;
}

// 400 Bad Request
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    "UserId is required",
    "Reason must be provided for deactivation"
  ],
  "statusCode": 400
}

// 401 Unauthorized
{
  "success": false,
  "message": "Authorization token is required",
  "statusCode": 401
}

// 403 Forbidden
{
  "success": false,
  "message": "Insufficient permissions. Admin role required.",
  "statusCode": 403
}

// 404 Not Found
{
  "success": false,
  "message": "User with ID 999 not found",
  "statusCode": 404
}

// 409 Conflict
{
  "success": false,
  "message": "User already has an active subscription",
  "statusCode": 409
}

// 500 Internal Server Error
{
  "success": false,
  "message": "An error occurred while processing your request",
  "statusCode": 500
}
```

### Error Handling Implementation

```typescript
class AdminApiError extends Error {
  constructor(
    public statusCode: number,
    message: string,
    public errors?: string[]
  ) {
    super(message);
    this.name = 'AdminApiError';
  }
}

const handleApiError = async (response: Response): Promise<never> => {
  const errorData = await response.json();

  throw new AdminApiError(
    response.status,
    errorData.message || 'API request failed',
    errorData.errors
  );
};

// Usage in components
try {
  const users = await getUsers({ page: 1, pageSize: 20 });
} catch (error) {
  if (error instanceof AdminApiError) {
    switch (error.statusCode) {
      case 401:
        // Redirect to login
        router.push('/admin/login');
        break;
      case 403:
        // Show permission denied message
        toast.error('You do not have permission to perform this action');
        break;
      case 404:
        toast.error('Resource not found');
        break;
      default:
        toast.error(error.message);
    }
  }
}
```

---

## Best Practices

### 1. Always Provide Reason/Notes

```typescript
// ❌ BAD
await deactivateUser(123, {});

// ✅ GOOD
await deactivateUser(123, {
  reason: "Account verification issue - user unable to upload documents"
});
```

### 2. Use Pagination for Large Datasets

```typescript
// ❌ BAD - No pagination
const users = await getUsers({});

// ✅ GOOD - With pagination
const users = await getUsers({
  page: 1,
  pageSize: 50
});
```

### 3. Filter by Date Range for Analytics

```typescript
// ❌ BAD - Loads all data
const stats = await getUserStatistics({});

// ✅ GOOD - With date range
const stats = await getUserStatistics({
  startDate: '2025-01-01',
  endDate: '2025-03-26'
});
```

### 4. Verify Operations with Audit Logs

```typescript
// After critical operation
await bulkDeactivateUsers({
  userIds: [123, 456],
  reason: "Spam accounts"
});

// Immediately verify
const auditLogs = await getAllAuditLogs({
  action: "BulkDeactivateUsers",
  startDate: new Date().toISOString()
});
```

### 5. Handle Loading and Error States

```typescript
const [users, setUsers] = useState<UserDto[]>([]);
const [loading, setLoading] = useState(true);
const [error, setError] = useState<string | null>(null);

useEffect(() => {
  const fetchUsers = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await getUsers({ page: 1, pageSize: 20 });
      setUsers(response.data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch users');
    } finally {
      setLoading(false);
    }
  };

  fetchUsers();
}, []);
```

### 6. Use TypeScript for Type Safety

```typescript
// ✅ GOOD - Type-safe API calls
const assignSubscription = async (request: AssignSubscriptionRequest) => {
  // TypeScript ensures all required fields are present
  return adminApi.post<AssignSubscriptionResponse>('/subscriptions/assign', request);
};

// Compile-time error if missing required fields
assignSubscription({
  userId: 123,
  subscriptionTierId: 3,
  durationMonths: 12
  // TypeScript error: missing isSponsoredSubscription
});
```

### 7. Implement Retry Logic for Failed Requests

```typescript
const retryRequest = async <T>(
  fn: () => Promise<T>,
  maxRetries: number = 3
): Promise<T> => {
  for (let i = 0; i < maxRetries; i++) {
    try {
      return await fn();
    } catch (error) {
      if (i === maxRetries - 1) throw error;
      await new Promise(resolve => setTimeout(resolve, 1000 * (i + 1)));
    }
  }
  throw new Error('Max retries exceeded');
};

// Usage
const users = await retryRequest(() => getUsers({ page: 1 }));
```

### 8. Cache Dashboard Statistics

```typescript
const CACHE_DURATION = 5 * 60 * 1000; // 5 minutes

const getCachedDashboard = (() => {
  let cache: { data: DashboardOverviewDto; timestamp: number } | null = null;

  return async (): Promise<DashboardOverviewDto> => {
    const now = Date.now();

    if (cache && now - cache.timestamp < CACHE_DURATION) {
      return cache.data;
    }

    const response = await getDashboardOverview();
    cache = { data: response.data, timestamp: now };
    return response.data;
  };
})();
```

---

## Validation Rules

### User Management
- **UserId**: Must be > 0
- **Reason**: Required for deactivation/reactivation (min 10 chars recommended)
- **SearchTerm**: Required for search, min 2 characters
- **Page**: Must be >= 1
- **PageSize**: Must be between 1-100

### Subscription Management
- **SubscriptionTierId**: Must be 1-5 (Trial, S, M, L, XL)
- **DurationMonths**: Must be > 0
- **ExtensionMonths**: Must be > 0
- **CancellationReason**: Required (min 10 chars recommended)

### Sponsorship Management
- **Quantity**: Must be > 0
- **UnitPrice**: Must be > 0
- **PhoneNumber**: Must match format `+905XXXXXXXXX`
- **CodePrefix**: Optional, max 10 characters
- **ValidityDays**: Default 365, must be > 0

### Analytics
- **StartDate/EndDate**: Must be valid ISO date strings
- **EndDate**: Must be >= StartDate

---

## Testing Guide

### Unit Testing Example

```typescript
import { describe, it, expect, vi } from 'vitest';

describe('AdminApiClient', () => {
  it('should fetch users with pagination', async () => {
    const mockResponse = {
      success: true,
      data: [{ userId: 1, fullName: 'Test User' }],
      totalCount: 1,
      page: 1,
      pageSize: 20,
      totalPages: 1
    };

    global.fetch = vi.fn(() =>
      Promise.resolve({
        ok: true,
        json: () => Promise.resolve(mockResponse)
      })
    );

    const adminApi = new AdminApiClient('http://test');
    const result = await adminApi.get('/users', { page: 1 });

    expect(result.success).toBe(true);
    expect(result.data).toHaveLength(1);
  });

  it('should handle 401 errors', async () => {
    global.fetch = vi.fn(() =>
      Promise.resolve({
        ok: false,
        status: 401,
        json: () => Promise.resolve({
          success: false,
          message: 'Unauthorized'
        })
      })
    );

    const adminApi = new AdminApiClient('http://test');

    await expect(adminApi.get('/users'))
      .rejects
      .toThrow('Unauthorized');
  });
});
```

---

## Changelog

### Version 2.0 (2025-03-26)
- ✅ **Code-Validated:** All endpoints, payloads, and responses validated against backend code
- ✅ **Complete DTO Mapping:** All TypeScript interfaces match C# DTOs exactly
- ✅ **Query Handler Verification:** Statistics DTOs validated from actual query handlers
- ✅ **Request Model Validation:** All request bodies confirmed from controller code
- ✅ **Comprehensive Coverage:** 35+ endpoints fully documented with examples
- ✅ **Production Ready:** Ready for frontend implementation

### Version 1.0 (2025-01-23)
- Initial documentation based on controller analysis

---

## Support

**Frontend Team Contact:**
- Documentation: `claudedocs/AdminOperations/`
- API Testing: Postman Collection available
- Backend Support: Contact backend team for endpoint issues

**Important Notes:**
- All endpoints require Admin role
- JWT tokens expire after 60 minutes
- Pagination default: page=1, pageSize=50, max=100
- All dates in ISO 8601 format
- All monetary values in TRY (Turkish Lira)

---

**Generated:** 2025-03-26
**Validated:** ✅ Backend Code
**Status:** Production Ready
**Version:** 2.0
