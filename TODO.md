# TODO - ZiraAI Project Tasks

## 🔴 Critical Issues (Priority 1)

### 1. Redemption Endpoint Routing Issue
**Status**: 🟡 In Progress  
**Error**: 404/500 errors when accessing `/redeem/{code}` endpoint  
**Details**:
- The RedemptionController is configured with `[Route("~/redeem/{code}")]` but not working properly
- JSON requests return 404, HTML requests return 500
- The API is running on HTTPS port 5001 in Staging environment
- All dependencies are registered (IRedemptionService, IUserSubscriptionRepository, ISubscriptionTierRepository)

**Files to Check**:
- `WebAPI/Controllers/RedemptionController.cs`
- `Business/Services/Redemption/RedemptionService.cs`
- `WebAPI/Startup.cs` (routing configuration)

**Possible Solutions**:
1. Check if the route attribute is being properly recognized
2. Verify RedemptionController is being registered in DI container
3. Check for any middleware interfering with the route
4. Consider adding explicit route in Startup.cs endpoints configuration

**Test Commands**:
```bash
curl -k "https://localhost:5001/redeem/SPONSOR-CODE-HERE" -H "Accept: application/json"
```

---

## 🟠 High Priority (Priority 2)

### 2. JWT Token Expiration Handling
**Status**: ⏳ Pending  
**Details**:
- Current test token expires after 1 hour
- Need to implement automatic token refresh mechanism
- Consider implementing refresh token flow

**Implementation Tasks**:
- Add refresh token endpoint
- Update authentication service to handle expired tokens
- Add automatic retry with new token in test scripts

### 3. Real SMS/WhatsApp Integration
**Status**: ⏳ Pending  
**Current State**: Using mock implementation  
**Details**:
- Currently `SendMessageAsync` in RedemptionService.cs just logs and returns true
- Need to integrate with actual SMS gateway (Twilio, AWS SNS, etc.)
- WhatsApp Business API integration needed

**Files to Update**:
- `Business/Services/Redemption/RedemptionService.cs:526-538`
- Add configuration for SMS provider in appsettings.json
- Create interface for SMS service providers

---

## 🟡 Medium Priority (Priority 3)

### 4. Analytics Endpoint Implementation
**Status**: ⏳ Pending  
**Details**:
- `/api/v1/sponsorship/link-statistics` endpoint not implemented
- Need to track and report:
  - Link click counts
  - Conversion rates
  - Geographic distribution
  - Time-based analytics

**Required Handlers**:
- `GetLinkStatisticsQuery.cs`
- `GetSponsorshipAnalyticsQuery.cs`

### 5. Database Migration Issues
**Status**: ⏳ Pending  
**Details**:
- Entity Framework migrations failing due to existing tables
- DateTime.UtcNow issues with PostgreSQL (partially fixed)
- Need proper migration strategy for production

**Tasks**:
- Clean up existing migrations
- Create proper initial migration
- Document migration commands for different environments

### 6. Error Handling & Logging
**Status**: ⏳ Pending  
**Details**:
- Generic "Something went wrong" messages not helpful for debugging
- Need structured logging with correlation IDs
- Add detailed error responses in development/staging

**Implementation**:
- Add global exception handler middleware
- Implement structured logging with Serilog
- Add correlation ID to all requests
- Create custom exception types for business logic errors

---

## 🟢 Low Priority (Priority 4)

### 7. Mobile Deep Linking Implementation
**Status**: ⏳ Pending  
**Details**:
- Deep link format designed: `ziraai://redeem?code={code}&token={token}`
- Need actual mobile app integration
- User-Agent detection implemented but not tested with real devices

**Files**:
- Mobile app URL schemes configuration
- Update RedemptionController for mobile responses
- Add mobile-specific success/error pages

### 8. Subscription Auto-Renewal
**Status**: ⏳ Pending  
**Details**:
- Auto-renewal flag exists but logic not implemented
- Payment gateway integration needed
- Scheduled job for checking and renewing subscriptions

**Tasks**:
- Implement Hangfire recurring job for auto-renewal
- Add payment processing service
- Create renewal notification system

### 9. Email Notifications
**Status**: ⏳ Pending  
**Details**:
- Email service not implemented
- Need to send:
  - Welcome emails for new farmers
  - Subscription activation confirmations
  - Expiry reminders

### 10. Frontend Development
**Status**: ⏳ Pending  
**Details**:
- Success/Error pages are inline HTML in controller
- Need proper frontend application
- Dashboard for sponsors to manage codes

**Tasks**:
- Create React/Angular frontend
- Implement sponsor dashboard
- Create farmer portal
- Mobile app development

---

## 🔵 Nice to Have (Priority 5)

### 11. API Documentation
**Status**: ⏳ Pending  
**Details**:
- Swagger not accessible at `/swagger`
- Need comprehensive API documentation
- Add example requests/responses

### 12. Performance Optimization
**Status**: ⏳ Pending  
**Details**:
- Add caching for frequently accessed data
- Optimize database queries
- Implement pagination for list endpoints

### 13. Security Enhancements
**Status**: ⏳ Pending  
**Details**:
- Add rate limiting
- Implement CAPTCHA for public endpoints
- Add request signing for webhook calls
- Audit trail for all operations

### 14. Internationalization (i18n)
**Status**: ⏳ Pending  
**Details**:
- Currently hardcoded Turkish messages
- Need multi-language support
- Database-driven translations exist but not fully utilized

---

## 📝 Technical Debt

### 15. Package Version Conflicts
**Status**: ⚠️ Warning  
**Details**:
- MediatR version mismatch warnings during build
- SixLabors.ImageSharp has known vulnerability
- Need to update and align package versions

### 16. Code Quality Issues
**Status**: ⚠️ Warning  
**Details**:
- Missing async/await in some async methods
- CA2200 warning about re-throwing exceptions
- DevArchitectureCodeAnalysis.ruleset file missing

### 17. Test Coverage
**Status**: ⏳ Pending  
**Details**:
- No unit tests for RedemptionService
- No integration tests for sponsorship flow
- Need automated E2E tests

---

## 🚀 Deployment Tasks

### 18. Production Deployment Preparation
**Status**: ⏳ Pending  
**Details**:
- SSL certificate configuration for production
- Environment-specific configuration
- Database backup strategy
- Monitoring and alerting setup

### 19. CI/CD Pipeline
**Status**: ⏳ Pending  
**Details**:
- GitHub Actions or Azure DevOps pipeline
- Automated testing on PR
- Automated deployment to staging/production

### 20. Docker Configuration
**Status**: ⏳ Pending  
**Details**:
- Dockerfile exists but needs testing
- Docker Compose for full stack
- Kubernetes deployment manifests

---

## 🐛 Known Bugs

### 21. Connection Closed Issues with PowerShell
**Status**: 🔍 Investigating  
**Details**:
- PowerShell scripts sometimes get "connection closed" errors
- Might be related to SSL/TLS configuration
- Works with curl but not always with Invoke-RestMethod

### 22. Process Lock Issues
**Status**: 🔍 Investigating  
**Details**:
- DLL files get locked by running processes
- Need better process management in development
- Consider using `dotnet watch` properly

---

## 📊 Completed Items (Reference)

✅ Sponsorship code creation endpoint  
✅ Mock SMS/WhatsApp link sending  
✅ PostgreSQL database integration  
✅ Staging environment configuration  
✅ HTTPS setup on port 5001  
✅ Operation claims security  
✅ Dependency injection setup  
✅ User subscription repository  
✅ Subscription tier repository  

---

## 📌 Quick Wins (Can be done in < 1 hour each)

1. Fix Swagger accessibility issue
2. Add health check endpoint
3. Improve error messages
4. Add README with setup instructions
5. Create postman collection with all endpoints

---

## 🎯 Next Session Focus

**Recommended order of implementation**:
1. Fix Redemption Endpoint (Critical)
2. Implement proper error handling
3. Add real SMS integration
4. Create analytics endpoints
5. Implement auto-renewal logic

---

## 📞 Contact Points & Resources

- PostgreSQL Connection: `Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass`
- Container Name: `dev-postgres`
- API URL: `https://localhost:5001` (Staging)
- Test Sponsor JWT: Expires every hour, need to regenerate

---

## 💡 Notes & Decisions

- Always use **Staging** environment for testing, not Development
- Always use **HTTPS** on port 5001, not HTTP
- All datetime values should use `DateTime.Now` not `DateTime.UtcNow` for PostgreSQL compatibility
- Mock implementations are acceptable for MVP but need replacement for production
- Follow existing project patterns (CQRS, Repository, aspects)

---

Last Updated: 2025-08-14 23:27