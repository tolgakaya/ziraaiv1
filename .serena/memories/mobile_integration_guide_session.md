# Mobile Integration Guide Session - 2025-10-04

## Session Summary
Created comprehensive mobile integration guide for Flutter app developers covering phone authentication and referral system features from the `feature/referrer-tier-system` branch.

## Deliverable
**File**: `claudedocs/mobile-integration-guide.md` (~1200+ lines)

## Key Features Documented

### 1. Phone OTP Authentication System
- **Registration Flow**: 2-step (request OTP → verify OTP)
  - Endpoint 1: `POST /api/v1/Auth/register-phone` (request OTP)
  - Endpoint 2: `POST /api/v1/Auth/verify-phone-register` (verify & create user)
  - Optional referral code support during registration
  - OTP expiry: 300 seconds for registration
  - Mock SMS service in development (fixed code: 123456)

- **Login Flow**: 2-step (request OTP → verify OTP)
  - Endpoint 1: `POST /api/v1/Auth/login-phone` (request OTP)
  - Endpoint 2: `POST /api/v1/Auth/verify-phone-otp` (verify & get token)
  - OTP expiry: 100 seconds for login
  - Returns JWT token with 60-minute expiry

### 2. Referral System Integration
**8 API Endpoints Documented:**
1. `POST /api/v1/Referral/generate` - Generate referral links (SMS/WhatsApp/Both)
2. `POST /api/v1/Referral/track-click` - Track link clicks (public, no auth)
3. `POST /api/v1/Referral/validate` - Validate referral code (public)
4. `GET /api/v1/Referral/stats` - Get referral statistics
5. `GET /api/v1/Referral/codes` - Get user's referral codes
6. `GET /api/v1/Referral/credits` - Get credit breakdown
7. `GET /api/v1/Referral/rewards` - Get reward history
8. `DELETE /api/v1/Referral/disable/{code}` - Disable referral code

**Key Referral System Features:**
- 10 credits per successful referral
- 30-day link expiry
- Real-time data (cache removed in recent fixes)
- Credits have priority over subscription quota
- Anti-abuse mechanisms (self-referral prevention, duplicate detection)
- 4-stage tracking: Clicked → Registered → Validated → Rewarded

### 3. Deep Linking Implementation
**Android Configuration:**
- Custom scheme: `ziraai://referral?code=ZIRA-ABC123`
- HTTPS scheme: `https://ziraai.com/ref/ZIRA-ABC123`
- AndroidManifest.xml configuration with intent filters
- Play Store install referrer tracking
- Single-top launch mode to prevent duplicate activities

**Flutter Implementation:**
- uni_links package integration
- DeepLinkService with stream subscription
- Initial link capture on app launch
- Runtime link capture while app is running
- ReferralCodeCapture widget for handling captured codes

## Complete Flutter Services Provided

### 1. AuthService
- Email login/register (existing)
- Phone OTP registration (2-step)
- Phone OTP login (2-step)
- Token refresh
- Logout
- Comprehensive error handling

### 2. ReferralService
- Generate referral link with delivery method selection
- Track referral clicks
- Validate referral codes
- Get referral statistics
- Get referral codes list
- Get credit breakdown
- Get reward history
- Disable referral code

### 3. TokenManager
- Secure token storage using flutter_secure_storage
- Token retrieval and validation
- Token refresh logic
- Token deletion on logout

### 4. DeepLinkService
- Initial link capture
- Runtime link monitoring
- Play Store referrer extraction
- Referral code parsing from multiple URL formats

### 5. ErrorHandler
- Centralized error handling
- Turkish error message translations
- HTTP status code mapping
- User-friendly error messages

## Guide Structure (11 Sections)
1. **Executive Summary** - Feature overview and timeline
2. **Architecture Overview** - System diagrams and data flow
3. **Authentication Integration** - Email + Phone OTP implementations
4. **Referral System Integration** - Complete API documentation
5. **Deep Linking Setup** - Android configuration and Flutter implementation
6. **API Reference** - All 12 endpoints with request/response examples
7. **Flutter Code Examples** - Production-ready service implementations
8. **Error Handling** - Centralized error handler with translations
9. **Testing Guide** - Unit and integration test examples
10. **Best Practices** - Security, state management, performance, UX
11. **Troubleshooting** - Common issues and solutions

## Technical Stack Covered
- **Backend**: .NET 9.0 Web API, PostgreSQL, JWT Bearer Auth
- **Frontend**: Flutter/Dart, Dio HTTP client, Provider/Riverpod state management
- **Storage**: SharedPreferences, FlutterSecureStorage
- **Packages**: uni_links (deep linking), sms_autofill (OTP auto-fill)
- **Platform**: Android deep linking configuration

## Security Best Practices Documented
1. Secure token storage with flutter_secure_storage
2. HTTPS-only API communication
3. Certificate pinning for production
4. Input validation and sanitization
5. No sensitive data in logs
6. Token expiry handling
7. Refresh token rotation
8. Secure deep link validation

## Testing Coverage Provided
1. **Unit Tests**:
   - AuthService test examples
   - ReferralService test examples
   - Mock HTTP responses with Mockito

2. **Integration Tests**:
   - Complete registration flow testing
   - Phone OTP authentication testing
   - Referral link generation and tracking
   - Deep link handling validation

## Key Technical Decisions
1. **Phone Format**: E.164 format (+905551234567) for international compatibility
2. **Token Storage**: flutter_secure_storage for sensitive data
3. **State Management**: Provider recommended, Riverpod alternative provided
4. **Error Handling**: Centralized handler with Turkish translations
5. **Deep Linking**: Dual scheme support (custom + HTTPS) for maximum compatibility
6. **OTP Auto-fill**: sms_autofill package for improved UX

## Reference Documentation Used
1. `claudedocs/referral-system-documentation.md` (1317 lines) - Complete referral system architecture
2. `claudedocs/phone-authentication-implementation.md` (1290 lines) - Phone OTP authentication implementation
3. Previous session memories about referral system fixes and improvements

## Development Environment Details
- **Mock SMS Service**: Fixed OTP code 123456 for development testing
- **API Base URL**: https://localhost:5001 (development)
- **API Versioning**: Header-based with x-dev-arch-version
- **Swagger Documentation**: Available at /swagger endpoint

## Common Issues Addressed in Troubleshooting
1. Deep link not captured from Play Store
2. OTP not received or expired
3. Referral code validation failures
4. Token refresh issues
5. HTTP interceptor configuration
6. Android manifest configuration errors
7. SMS auto-fill not working
8. State management synchronization

## Next Steps for Mobile Team
1. Review the complete guide in `claudedocs/mobile-integration-guide.md`
2. Set up development environment with API base URL
3. Implement AuthService for phone OTP authentication
4. Implement ReferralService for referral system features
5. Configure AndroidManifest.xml for deep linking
6. Set up DeepLinkService for referral code capture
7. Implement error handling and Turkish translations
8. Write unit tests for all services
9. Perform integration testing with backend API
10. Test deep linking from Play Store (staging environment)

## Production Readiness
- All code examples are production-ready
- Security best practices included
- Error handling comprehensive
- Testing guide complete
- Performance optimization covered
- UX considerations documented

## Session Completion Status
✅ All documentation files read successfully
✅ Comprehensive guide created with 11 major sections
✅ Complete Flutter service implementations provided
✅ Deep linking configuration documented
✅ API reference with 12 endpoints complete
✅ Error handling and testing examples included
✅ Best practices and troubleshooting documented
✅ No errors encountered during session
