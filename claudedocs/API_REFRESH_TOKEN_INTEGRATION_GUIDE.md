# 🔐 Refresh Token Expiration - API Integration Guide

> **Version:** 2.0
> **Date:** January 29, 2025
> **Target Audience:** Mobile & Web Development Teams
> **Status:** ✅ Production Ready

---

## 📋 Table of Contents

1. [Executive Summary](#executive-summary)
2. [What Changed](#what-changed)
3. [Breaking Changes](#breaking-changes)
4. [Affected Endpoints](#affected-endpoints)
5. [Migration Guide](#migration-guide)
6. [Implementation Examples](#implementation-examples)
7. [Error Handling](#error-handling)
8. [Testing Checklist](#testing-checklist)
9. [FAQ](#faq)

---

## 🎯 Executive Summary

### What This Update Does

We've implemented **automatic refresh token expiration** to enhance security. Previously, refresh tokens had **infinite lifetime** (major security vulnerability). Now they expire after **3 hours (180 minutes)**.

### Impact Level

| Aspect | Impact |
|--------|--------|
| **API Contract** | ✅ **Backward Compatible** - New field added to responses |
| **Client Code** | ⚠️ **Recommended Update** - Add expiration tracking |
| **Authentication Flow** | ⚠️ **Modified** - Expired refresh tokens rejected |
| **User Experience** | ℹ️ **Improved** - Users must re-login after 3 hours of inactivity |

### Timeline

- **Development:** ✅ Complete (January 29, 2025)
- **Staging Deployment:** 🔄 Pending
- **Production Deployment:** 📅 TBD
- **Client Update Deadline:** 🎯 Before production deployment

---

## 🔄 What Changed

### Previous Behavior (v1.x)

```json
{
  "data": {
    "token": "eyJhbGc...",
    "expiration": "2025-01-29T13:00:00",
    "refreshToken": "abc123...",
    "claims": ["Farmer"]
  },
  "success": true,
  "message": "Giriş başarılı"
}
```

**Problems:**
- ❌ Refresh tokens **never expired** (infinite lifetime)
- ❌ Stolen refresh tokens valid forever
- ❌ No way to track when refresh token expires
- ❌ Major security vulnerability

### New Behavior (v2.0)

```json
{
  "data": {
    "token": "eyJhbGc...",
    "expiration": "2025-01-29T13:00:00",
    "refreshToken": "abc123...",
    "refreshTokenExpiration": "2025-01-29T16:00:00",  // ✅ NEW
    "claims": ["Farmer"]
  },
  "success": true,
  "message": "Giriş başarılı"
}
```

**Improvements:**
- ✅ Refresh tokens expire after **3 hours (180 minutes)**
- ✅ New `refreshTokenExpiration` field in response
- ✅ Automatic validation on refresh token usage
- ✅ Enhanced security with time-limited tokens

---

## ⚠️ Breaking Changes

### None! (Fully Backward Compatible)

✅ **Existing clients will continue working without changes**

- New field is **additive** (doesn't break existing JSON parsers)
- Existing fields remain unchanged
- Request payloads unchanged
- Only **response payloads** include new field

### Recommended Changes

While not breaking, we **strongly recommend** updating your client to:

1. **Store** the `refreshTokenExpiration` value
2. **Check** expiration before using refresh token
3. **Handle** expiration errors gracefully
4. **Prompt** user to re-login when refresh token expires

---

## 📡 Affected Endpoints

### Login Endpoints (Token Returned)

| Endpoint | Method | Change | Priority |
|----------|--------|--------|----------|
| `/api/v{version}/Auth/login` | POST | ✅ `refreshTokenExpiration` added | 🔴 High |
| `/api/v{version}/Auth/refresh-token` | POST | ✅ `refreshTokenExpiration` added | 🔴 Critical |
| `/api/v{version}/Auth/login-phone` | POST | ✅ `refreshTokenExpiration` added | 🔴 High |
| `/api/v{version}/Auth/verify-phone-otp` | POST | ✅ `refreshTokenExpiration` added | 🔴 High |
| `/api/v{version}/Auth/verify-phone-register` | POST | ✅ `refreshTokenExpiration` added | 🟡 Medium |

### Register Endpoints (No Token)

| Endpoint | Method | Change | Priority |
|----------|--------|--------|----------|
| `/api/v{version}/Auth/register` | POST | ❌ No change | 🟢 Low |
| `/api/v{version}/Auth/register-phone` | POST | ❌ No change | 🟢 Low |

**Note:** Register endpoints only return success messages (no tokens), so they are **not affected**.

---

## 🚀 Migration Guide

### Phase 1: Preparation (Before Deployment)

#### Step 1: Update API Models

**Before:**
```typescript
// TypeScript/Angular/React
interface LoginResponse {
  data: {
    token: string;
    expiration: string;
    refreshToken: string;
    claims: string[];
  };
  success: boolean;
  message: string;
}
```

**After:**
```typescript
interface LoginResponse {
  data: {
    token: string;
    expiration: string;
    refreshToken: string;
    refreshTokenExpiration: string;  // ✅ NEW
    claims: string[];
  };
  success: boolean;
  message: string;
}
```

#### Step 2: Update Storage Logic

**Before:**
```typescript
// Store only access token and refresh token
localStorage.setItem('accessToken', response.data.token);
localStorage.setItem('accessTokenExpiration', response.data.expiration);
localStorage.setItem('refreshToken', response.data.refreshToken);
```

**After:**
```typescript
// Store refresh token expiration as well
localStorage.setItem('accessToken', response.data.token);
localStorage.setItem('accessTokenExpiration', response.data.expiration);
localStorage.setItem('refreshToken', response.data.refreshToken);
localStorage.setItem('refreshTokenExpiration', response.data.refreshTokenExpiration);  // ✅ NEW
```

#### Step 3: Add Expiration Check

**Before:**
```typescript
async function refreshAccessToken() {
  const refreshToken = localStorage.getItem('refreshToken');

  // No expiration check - always try to refresh
  const response = await api.post('/api/v1/Auth/refresh-token', { refreshToken });
  return response.data;
}
```

**After:**
```typescript
async function refreshAccessToken() {
  const refreshToken = localStorage.getItem('refreshToken');
  const refreshExpiration = localStorage.getItem('refreshTokenExpiration');

  // ✅ NEW: Check if refresh token is expired
  if (new Date(refreshExpiration) < new Date()) {
    console.log('Refresh token expired, redirecting to login');
    redirectToLogin();
    return null;
  }

  const response = await api.post('/api/v1/Auth/refresh-token', { refreshToken });
  return response.data;
}
```

### Phase 2: Deployment (During Release)

1. **Deploy API to Staging** - Test with existing clients
2. **Verify Backward Compatibility** - Confirm old clients work
3. **Deploy Updated Clients** - Release mobile/web updates
4. **Monitor Error Rates** - Watch for refresh token errors
5. **Deploy to Production** - Gradual rollout

### Phase 3: Post-Deployment (After Release)

1. **Monitor refresh token usage** - Track expiration errors
2. **Analyze user behavior** - 3 hours sufficient or adjust?
3. **Update analytics** - Add refresh token expiration events
4. **User communication** - Inform users of security improvements

---

## 💻 Implementation Examples

### Example 1: React/TypeScript (Web)

#### AuthService.ts

```typescript
import axios from 'axios';

interface AuthTokens {
  accessToken: string;
  accessTokenExpiration: string;
  refreshToken: string;
  refreshTokenExpiration: string;  // ✅ NEW
}

class AuthService {
  private readonly API_BASE = 'https://api.ziraai.com/api/v1';

  // Login with email/password
  async login(email: string, password: string): Promise<AuthTokens> {
    const response = await axios.post(`${this.API_BASE}/Auth/login`, {
      email,
      password
    });

    if (response.data.success) {
      const tokens: AuthTokens = {
        accessToken: response.data.data.token,
        accessTokenExpiration: response.data.data.expiration,
        refreshToken: response.data.data.refreshToken,
        refreshTokenExpiration: response.data.data.refreshTokenExpiration  // ✅ NEW
      };

      this.saveTokens(tokens);
      return tokens;
    }

    throw new Error(response.data.message);
  }

  // Refresh access token
  async refreshToken(): Promise<AuthTokens | null> {
    const refreshToken = localStorage.getItem('refreshToken');
    const refreshExpiration = localStorage.getItem('refreshTokenExpiration');

    // ✅ NEW: Check expiration before making request
    if (!refreshToken || !refreshExpiration) {
      return null;
    }

    if (new Date(refreshExpiration) < new Date()) {
      console.log('Refresh token expired, clearing auth state');
      this.clearTokens();
      return null;
    }

    try {
      const response = await axios.post(`${this.API_BASE}/Auth/refresh-token`, {
        refreshToken
      });

      if (response.data.success) {
        const tokens: AuthTokens = {
          accessToken: response.data.data.token,
          accessTokenExpiration: response.data.data.expiration,
          refreshToken: response.data.data.refreshToken,
          refreshTokenExpiration: response.data.data.refreshTokenExpiration  // ✅ NEW
        };

        this.saveTokens(tokens);
        return tokens;
      }
    } catch (error: any) {
      // Handle expired refresh token error
      if (error.response?.data?.message?.includes('expired')) {
        console.log('Refresh token expired on server, clearing auth state');
        this.clearTokens();
      }
      throw error;
    }

    return null;
  }

  // Check if refresh token is still valid
  isRefreshTokenValid(): boolean {
    const refreshExpiration = localStorage.getItem('refreshTokenExpiration');
    if (!refreshExpiration) return false;
    return new Date(refreshExpiration) > new Date();
  }

  // Save tokens to storage
  private saveTokens(tokens: AuthTokens): void {
    localStorage.setItem('accessToken', tokens.accessToken);
    localStorage.setItem('accessTokenExpiration', tokens.accessTokenExpiration);
    localStorage.setItem('refreshToken', tokens.refreshToken);
    localStorage.setItem('refreshTokenExpiration', tokens.refreshTokenExpiration);  // ✅ NEW
  }

  // Clear all tokens
  private clearTokens(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('accessTokenExpiration');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('refreshTokenExpiration');
  }
}

export default new AuthService();
```

#### Axios Interceptor (Auto-Refresh)

```typescript
import axios from 'axios';
import authService from './AuthService';

// Request interceptor - attach access token
axios.interceptors.request.use(
  async (config) => {
    const accessToken = localStorage.getItem('accessToken');
    const accessExpiration = localStorage.getItem('accessTokenExpiration');

    // Check if access token is expired
    if (accessExpiration && new Date(accessExpiration) < new Date()) {
      // ✅ NEW: Check if refresh token is still valid before attempting refresh
      if (authService.isRefreshTokenValid()) {
        try {
          const newTokens = await authService.refreshToken();
          if (newTokens) {
            config.headers.Authorization = `Bearer ${newTokens.accessToken}`;
            return config;
          }
        } catch (error) {
          // Refresh failed, redirect to login
          window.location.href = '/login';
          return Promise.reject(error);
        }
      } else {
        // Refresh token expired, redirect to login
        console.log('Refresh token expired, redirecting to login');
        window.location.href = '/login';
        return Promise.reject(new Error('Session expired'));
      }
    }

    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }

    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor - handle 401 errors
axios.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // If 401 and haven't retried yet
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      // ✅ NEW: Check if refresh token is valid before attempting refresh
      if (authService.isRefreshTokenValid()) {
        try {
          const newTokens = await authService.refreshToken();
          if (newTokens) {
            originalRequest.headers.Authorization = `Bearer ${newTokens.accessToken}`;
            return axios(originalRequest);
          }
        } catch (refreshError) {
          window.location.href = '/login';
          return Promise.reject(refreshError);
        }
      } else {
        // Refresh token expired
        console.log('Refresh token expired, redirecting to login');
        window.location.href = '/login';
      }
    }

    return Promise.reject(error);
  }
);
```

---

### Example 2: Flutter/Dart (Mobile)

#### auth_service.dart

```dart
import 'package:dio/dio.dart';
import 'package:shared_preferences/shared_preferences.dart';

class AuthTokens {
  final String accessToken;
  final DateTime accessTokenExpiration;
  final String refreshToken;
  final DateTime refreshTokenExpiration;  // ✅ NEW

  AuthTokens({
    required this.accessToken,
    required this.accessTokenExpiration,
    required this.refreshToken,
    required this.refreshTokenExpiration,
  });

  factory AuthTokens.fromJson(Map<String, dynamic> json) {
    return AuthTokens(
      accessToken: json['token'],
      accessTokenExpiration: DateTime.parse(json['expiration']),
      refreshToken: json['refreshToken'],
      refreshTokenExpiration: DateTime.parse(json['refreshTokenExpiration']),  // ✅ NEW
    );
  }
}

class AuthService {
  final Dio _dio;
  static const String _apiBase = 'https://api.ziraai.com/api/v1';

  AuthService(this._dio);

  // Login with email/password
  Future<AuthTokens> login(String email, String password) async {
    final response = await _dio.post(
      '$_apiBase/Auth/login',
      data: {'email': email, 'password': password},
    );

    if (response.data['success']) {
      final tokens = AuthTokens.fromJson(response.data['data']);
      await _saveTokens(tokens);
      return tokens;
    }

    throw Exception(response.data['message']);
  }

  // Login with phone/OTP
  Future<AuthTokens> verifyPhoneOtp(String phone, String code) async {
    final response = await _dio.post(
      '$_apiBase/Auth/verify-phone-otp',
      data: {'mobilePhone': phone, 'code': code},
    );

    if (response.data['success']) {
      final tokens = AuthTokens.fromJson(response.data['data']);
      await _saveTokens(tokens);
      return tokens;
    }

    throw Exception(response.data['message']);
  }

  // Refresh access token
  Future<AuthTokens?> refreshToken() async {
    final prefs = await SharedPreferences.getInstance();
    final refreshToken = prefs.getString('refreshToken');
    final refreshExpirationStr = prefs.getString('refreshTokenExpiration');

    if (refreshToken == null || refreshExpirationStr == null) {
      return null;
    }

    // ✅ NEW: Check expiration before making request
    final refreshExpiration = DateTime.parse(refreshExpirationStr);
    if (refreshExpiration.isBefore(DateTime.now())) {
      print('Refresh token expired, clearing auth state');
      await clearTokens();
      return null;
    }

    try {
      final response = await _dio.post(
        '$_apiBase/Auth/refresh-token',
        data: {'refreshToken': refreshToken},
      );

      if (response.data['success']) {
        final tokens = AuthTokens.fromJson(response.data['data']);
        await _saveTokens(tokens);
        return tokens;
      }
    } on DioException catch (e) {
      // Handle expired refresh token error
      if (e.response?.data['message']?.contains('expired') ?? false) {
        print('Refresh token expired on server, clearing auth state');
        await clearTokens();
      }
      rethrow;
    }

    return null;
  }

  // Check if refresh token is still valid
  Future<bool> isRefreshTokenValid() async {
    final prefs = await SharedPreferences.getInstance();
    final refreshExpirationStr = prefs.getString('refreshTokenExpiration');

    if (refreshExpirationStr == null) return false;

    final refreshExpiration = DateTime.parse(refreshExpirationStr);
    return refreshExpiration.isAfter(DateTime.now());
  }

  // Save tokens to storage
  Future<void> _saveTokens(AuthTokens tokens) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('accessToken', tokens.accessToken);
    await prefs.setString('accessTokenExpiration', tokens.accessTokenExpiration.toIso8601String());
    await prefs.setString('refreshToken', tokens.refreshToken);
    await prefs.setString('refreshTokenExpiration', tokens.refreshTokenExpiration.toIso8601String());  // ✅ NEW
  }

  // Clear all tokens
  Future<void> clearTokens() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('accessToken');
    await prefs.remove('accessTokenExpiration');
    await prefs.remove('refreshToken');
    await prefs.remove('refreshTokenExpiration');
  }
}
```

#### dio_interceptor.dart

```dart
import 'package:dio/dio.dart';
import 'auth_service.dart';

class AuthInterceptor extends Interceptor {
  final AuthService _authService;

  AuthInterceptor(this._authService);

  @override
  void onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    final prefs = await SharedPreferences.getInstance();
    final accessToken = prefs.getString('accessToken');
    final accessExpirationStr = prefs.getString('accessTokenExpiration');

    // Check if access token is expired
    if (accessExpirationStr != null) {
      final accessExpiration = DateTime.parse(accessExpirationStr);

      if (accessExpiration.isBefore(DateTime.now())) {
        // ✅ NEW: Check if refresh token is valid before attempting refresh
        if (await _authService.isRefreshTokenValid()) {
          try {
            final newTokens = await _authService.refreshToken();
            if (newTokens != null) {
              options.headers['Authorization'] = 'Bearer ${newTokens.accessToken}';
              return handler.next(options);
            }
          } catch (e) {
            // Refresh failed, redirect to login
            _redirectToLogin();
            return handler.reject(
              DioException(
                requestOptions: options,
                error: 'Session expired',
              ),
            );
          }
        } else {
          // Refresh token expired, redirect to login
          print('Refresh token expired, redirecting to login');
          _redirectToLogin();
          return handler.reject(
            DioException(
              requestOptions: options,
              error: 'Session expired',
            ),
          );
        }
      }
    }

    if (accessToken != null) {
      options.headers['Authorization'] = 'Bearer $accessToken';
    }

    handler.next(options);
  }

  @override
  void onError(
    DioException err,
    ErrorInterceptorHandler handler,
  ) async {
    // If 401 and haven't retried yet
    if (err.response?.statusCode == 401 &&
        err.requestOptions.extra['_retry'] != true) {

      // ✅ NEW: Check if refresh token is valid before attempting refresh
      if (await _authService.isRefreshTokenValid()) {
        try {
          final newTokens = await _authService.refreshToken();
          if (newTokens != null) {
            // Retry the request with new token
            final opts = err.requestOptions;
            opts.headers['Authorization'] = 'Bearer ${newTokens.accessToken}';
            opts.extra['_retry'] = true;

            final response = await Dio().fetch(opts);
            return handler.resolve(response);
          }
        } catch (e) {
          _redirectToLogin();
          return handler.reject(err);
        }
      } else {
        // Refresh token expired
        print('Refresh token expired, redirecting to login');
        _redirectToLogin();
      }
    }

    handler.next(err);
  }

  void _redirectToLogin() {
    // Navigate to login screen
    // Implementation depends on your navigation setup
  }
}
```

---

### Example 3: Swift/iOS (Mobile)

#### AuthService.swift

```swift
import Foundation

struct AuthTokens: Codable {
    let accessToken: String
    let accessTokenExpiration: Date
    let refreshToken: String
    let refreshTokenExpiration: Date  // ✅ NEW

    enum CodingKeys: String, CodingKey {
        case accessToken = "token"
        case accessTokenExpiration = "expiration"
        case refreshToken
        case refreshTokenExpiration
    }
}

class AuthService {
    static let shared = AuthService()
    private let apiBase = "https://api.ziraai.com/api/v1"

    // Login with email/password
    func login(email: String, password: String) async throws -> AuthTokens {
        let url = URL(string: "\(apiBase)/Auth/login")!
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")

        let body = ["email": email, "password": password]
        request.httpBody = try JSONEncoder().encode(body)

        let (data, _) = try await URLSession.shared.data(for: request)
        let response = try JSONDecoder().decode(LoginResponse.self, from: data)

        if response.success {
            let tokens = response.data
            saveTokens(tokens)
            return tokens
        }

        throw NSError(domain: "AuthService", code: -1, userInfo: [NSLocalizedDescriptionKey: response.message])
    }

    // Refresh access token
    func refreshToken() async throws -> AuthTokens? {
        guard let refreshToken = UserDefaults.standard.string(forKey: "refreshToken"),
              let refreshExpirationStr = UserDefaults.standard.string(forKey: "refreshTokenExpiration") else {
            return nil
        }

        // ✅ NEW: Check expiration before making request
        let formatter = ISO8601DateFormatter()
        guard let refreshExpiration = formatter.date(from: refreshExpirationStr) else {
            return nil
        }

        if refreshExpiration < Date() {
            print("Refresh token expired, clearing auth state")
            clearTokens()
            return nil
        }

        let url = URL(string: "\(apiBase)/Auth/refresh-token")!
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")

        let body = ["refreshToken": refreshToken]
        request.httpBody = try JSONEncoder().encode(body)

        do {
            let (data, _) = try await URLSession.shared.data(for: request)
            let response = try JSONDecoder().decode(LoginResponse.self, from: data)

            if response.success {
                let tokens = response.data
                saveTokens(tokens)
                return tokens
            }
        } catch {
            // Handle expired refresh token error
            if let errorMessage = (error as NSError).userInfo[NSLocalizedDescriptionKey] as? String,
               errorMessage.contains("expired") {
                print("Refresh token expired on server, clearing auth state")
                clearTokens()
            }
            throw error
        }

        return nil
    }

    // Check if refresh token is still valid
    func isRefreshTokenValid() -> Bool {
        guard let refreshExpirationStr = UserDefaults.standard.string(forKey: "refreshTokenExpiration") else {
            return false
        }

        let formatter = ISO8601DateFormatter()
        guard let refreshExpiration = formatter.date(from: refreshExpirationStr) else {
            return false
        }

        return refreshExpiration > Date()
    }

    // Save tokens to storage
    private func saveTokens(_ tokens: AuthTokens) {
        let formatter = ISO8601DateFormatter()

        UserDefaults.standard.set(tokens.accessToken, forKey: "accessToken")
        UserDefaults.standard.set(formatter.string(from: tokens.accessTokenExpiration), forKey: "accessTokenExpiration")
        UserDefaults.standard.set(tokens.refreshToken, forKey: "refreshToken")
        UserDefaults.standard.set(formatter.string(from: tokens.refreshTokenExpiration), forKey: "refreshTokenExpiration")  // ✅ NEW
    }

    // Clear all tokens
    private func clearTokens() {
        UserDefaults.standard.removeObject(forKey: "accessToken")
        UserDefaults.standard.removeObject(forKey: "accessTokenExpiration")
        UserDefaults.standard.removeObject(forKey: "refreshToken")
        UserDefaults.standard.removeObject(forKey: "refreshTokenExpiration")
    }
}

struct LoginResponse: Codable {
    let data: AuthTokens
    let success: Bool
    let message: String
}
```

---

## ❌ Error Handling

### New Error Scenarios

#### 1. Expired Refresh Token (Client-Side Check)

**Scenario:** Client checks expiration locally before API call

**Error Handling:**
```typescript
// TypeScript
if (new Date(refreshTokenExpiration) < new Date()) {
  // Clear tokens
  localStorage.clear();

  // Show user-friendly message
  showNotification('Your session has expired. Please login again.', 'warning');

  // Redirect to login
  router.push('/login');
}
```

#### 2. Expired Refresh Token (Server-Side Validation)

**Scenario:** Server rejects expired refresh token

**HTTP Response:**
```json
{
  "data": null,
  "success": false,
  "message": "Refresh token has expired. Please login again."
}
```

**Error Handling:**
```typescript
try {
  const response = await api.post('/api/v1/Auth/refresh-token', { refreshToken });
} catch (error) {
  if (error.response?.data?.message?.includes('expired')) {
    // Clear tokens
    authService.clearTokens();

    // Show user-friendly message
    showNotification('Your session has expired. Please login again.', 'warning');

    // Redirect to login
    router.push('/login');
  }
}
```

#### 3. Invalid Refresh Token

**HTTP Response:**
```json
{
  "data": null,
  "success": false,
  "message": "User not found"
}
```

**Error Handling:** Same as expired token

---

### Error Code Reference

| Error Message | Cause | Solution |
|---------------|-------|----------|
| `Refresh token has expired. Please login again.` | Refresh token older than 3 hours | Clear tokens, redirect to login |
| `User not found` | Invalid/revoked refresh token | Clear tokens, redirect to login |
| `Unauthorized` (401) | Access token expired | Attempt refresh, then login if refresh fails |

---

## ✅ Testing Checklist

### Pre-Deployment Testing

- [ ] **Login Flow**
  - [ ] Email/password login returns `refreshTokenExpiration`
  - [ ] Phone/OTP login returns `refreshTokenExpiration`
  - [ ] Phone register + verify returns `refreshTokenExpiration`

- [ ] **Token Refresh Flow**
  - [ ] Valid refresh token returns new tokens with expiration
  - [ ] Expired refresh token (3+ hours) returns error
  - [ ] Invalid refresh token returns error
  - [ ] Client-side expiration check works correctly

- [ ] **Backward Compatibility**
  - [ ] Old client (ignoring new field) can still login
  - [ ] Old client can still refresh tokens
  - [ ] No breaking changes in request payloads

- [ ] **Storage**
  - [ ] `refreshTokenExpiration` saved to localStorage/SharedPreferences/UserDefaults
  - [ ] Tokens cleared on logout
  - [ ] Tokens cleared on expiration error

- [ ] **UI/UX**
  - [ ] User sees friendly message on session expiration
  - [ ] Automatic redirect to login on expiration
  - [ ] No infinite refresh loops

### Production Monitoring

- [ ] **Metrics**
  - [ ] Track refresh token expiration errors
  - [ ] Monitor user re-login frequency
  - [ ] Measure impact on active session duration

- [ ] **Alerts**
  - [ ] Spike in "refresh token expired" errors
  - [ ] Unusual increase in login attempts
  - [ ] High rate of 401 errors

---

## ❓ FAQ

### Q1: What is the refresh token expiration time?

**A:** 3 hours (180 minutes) from token creation. This is configured in `appsettings.json`:

```json
"TokenOptions": {
  "AccessTokenExpiration": 60,      // 1 hour
  "RefreshTokenExpiration": 180     // 3 hours
}
```

### Q2: Will existing users be logged out after deployment?

**A:** No. Existing refresh tokens without expiration will continue working until they refresh. On next token refresh, they'll receive the new expiration field.

### Q3: Can we change the expiration time?

**A:** Yes, adjust `RefreshTokenExpiration` in `appsettings.json` and restart the API. Recommended range: 180-1440 minutes (3-24 hours).

### Q4: What happens if user is inactive for 3+ hours?

**A:** When they return:
1. Access token expired → Auto-refresh attempted
2. Refresh token also expired → Refresh fails
3. Client detects expiration → Redirects to login
4. User must re-authenticate

### Q5: Do I need to update my mobile app immediately?

**A:** No (backward compatible), but **strongly recommended** for better UX:
- **Without update:** User sees generic error, manual retry needed
- **With update:** Friendly message, automatic redirect to login

### Q6: How do I test refresh token expiration?

**Option 1: Wait 3 hours** (Not practical)

**Option 2: Temporarily reduce expiration** (Recommended)
```json
// appsettings.Development.json
"TokenOptions": {
  "AccessTokenExpiration": 1,       // 1 minute
  "RefreshTokenExpiration": 2       // 2 minutes
}
```

**Option 3: Manual database update**
```sql
-- Force expire a specific user's refresh token
UPDATE "Users"
SET "RefreshTokenExpires" = NOW() - INTERVAL '1 hour'
WHERE "UserId" = 123;
```

### Q7: What if server and client clocks are out of sync?

**A:** Server time is authoritative. Client-side expiration check is an **optimization** to avoid unnecessary API calls. Server always validates expiration regardless of client check.

### Q8: Can refresh tokens be revoked manually?

**A:** Yes, set `RefreshTokenExpires` to a past date:

```sql
UPDATE "Users"
SET "RefreshTokenExpires" = '2020-01-01 00:00:00'
WHERE "UserId" = 123;
```

### Q9: Are refresh tokens rotated on each refresh?

**A:** Yes! Each refresh request:
1. Validates old refresh token
2. Generates **new** refresh token
3. Returns new refresh token + expiration
4. Old refresh token becomes invalid

### Q10: What timezone is used for expiration?

**A:** Server local time (not UTC) for PostgreSQL compatibility. Always use `DateTime.Now` in API, client should store as-is (ISO 8601 string).

---

## 📚 Additional Resources

### API Documentation
- Swagger UI: `https://api.ziraai.com/swagger`
- Postman Collection: `ZiraAI_Complete_API_Collection_v6.1.json`

### Related Documentation
- [Environment Configuration Guide](./environment-configuration.md)
- [Authentication Flow Diagram](./authentication-flow.md)
- [Security Best Practices](./security-best-practices.md)

### Support Contacts
- Backend Team: backend@ziraai.com
- Mobile Team: mobile@ziraai.com
- Web Team: web@ziraai.com
- DevOps: devops@ziraai.com

---

## 📝 Change Log

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 2.0 | 2025-01-29 | Added refresh token expiration | System |
| 1.0 | 2024-XX-XX | Initial API release | System |

---

## ✅ Approval & Sign-off

| Role | Name | Signature | Date |
|------|------|-----------|------|
| Backend Lead | __________ | __________ | ______ |
| Mobile Lead | __________ | __________ | ______ |
| Web Lead | __________ | __________ | ______ |
| QA Lead | __________ | __________ | ______ |
| Product Manager | __________ | __________ | ______ |

---

**Document Version:** 1.0
**Last Updated:** January 29, 2025
**Next Review:** March 1, 2025
