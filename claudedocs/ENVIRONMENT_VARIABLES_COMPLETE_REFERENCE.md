# ZiraAI - Complete Environment Variables Reference

**Last Updated:** 2025-10-07
**Version:** 2.0.0 (Railway-Validated)

---

## ⚠️ CRITICAL WARNINGS

1. **NEVER commit production values to Git**
2. **Use Railway environment variables for sensitive data**
3. **Different secrets per environment (JWT keys, passwords)**
4. **Test all variables after deployment**
5. **Keep this document updated when adding new configs**

---

## Table of Contents

1. [Critical Variables (All Environments)](#1-critical-variables-all-environments)
2. [Database Configuration](#2-database-configuration)
3. [External Services](#3-external-services)
4. [Referral & Deep Links](#4-referral--deep-links)
5. [Security & Authentication](#5-security--authentication)
6. [File Storage](#6-file-storage)
7. [Monitoring & Logging](#7-monitoring--logging)
8. [SignalR Configuration](#8-signalr-configuration) ⚠️ **Missing in Railway Staging**
9. [Background Jobs (Hangfire)](#9-background-jobs-hangfire)
10. [SMS Service Configuration](#10-sms-service-configuration) ⚠️ **Missing in Railway Staging**
11. [WhatsApp Business API Configuration](#11-whatsapp-business-api-configuration) ✨ **New - Railway Validated**
12. [Notification Settings](#12-notification-settings) ✨ **New - Railway Validated**
13. [Email Configuration](#13-email-configuration) ✨ **New - Railway Validated**
14. [MongoDB Configuration](#14-mongodb-configuration) ✨ **New - Railway Validated**
15. [AI Optimization](#15-ai-optimization) ✨ **New - Railway Validated**
16. [CORS & Startup](#16-cors--startup)
17. [Railway-Specific Variables](#17-railway-specific-variables)
18. [Development Environment](#18-development-environment-complete)
19. [Staging Environment](#19-staging-environment-railway-validated) ✅ **Updated with Railway Values**
20. [Production Environment](#20-production-environment-complete)

---

## 1. Critical Variables (All Environments)

These variables are **MANDATORY** for the application to start.

### ASPNETCORE_ENVIRONMENT
- **Description:** Sets the runtime environment
- **Type:** String
- **Required:** Yes
- **Values:**
  - `Development` - Local development
  - `Staging` - Railway staging deployment
  - `Production` - Railway production deployment

### ConnectionStrings__DArchPgContext
- **Description:** PostgreSQL database connection string
- **Type:** String (connection string format)
- **Required:** Yes
- **Format:** `Host={host};Port={port};Database={db};Username={user};Password={pass}`
- **Railway Alternative:** Can use `DATABASE_CONNECTION_STRING` (auto-mapped in Program.cs)

### UseRedis
- **Description:** Enable/disable Redis caching
- **Type:** Boolean
- **Required:** Yes
- **Default:** `false` (dev), `true` (staging/prod)

---

## 2. Database Configuration

### PostgreSQL Connection Strings

#### Development
```bash
ConnectionStrings__DArchPgContext=Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass
```

#### Staging
```bash
ConnectionStrings__DArchPgContext=Host=postgres.railway.internal;Port=5432;Database=railway;Username=postgres;Password=RAILWAY_POSTGRES_PASSWORD
```

#### Production
```bash
# Railway provides DATABASE_URL automatically, which is mapped to ConnectionStrings__DArchPgContext
DATABASE_URL=postgresql://postgres:password@postgres.railway.internal:5432/railway
# OR explicitly set:
ConnectionStrings__DArchPgContext=Host=postgres.railway.internal;Port=5432;Database=railway;Username=postgres;Password=RAILWAY_POSTGRES_PASSWORD
```

### Database Logging

#### SeriLogConfigurations__PostgreConfiguration__ConnectionString
- **Description:** Connection string for storing logs in PostgreSQL
- **Type:** String
- **Required:** No (optional for structured logging)
- **Note:** Usually same as main database connection

---

## 3. External Services

### RabbitMQ Configuration

#### RabbitMQ__ConnectionString
- **Description:** AMQP connection string for message queue
- **Type:** String (AMQP URI format)
- **Required:** Yes (if UseRabbitMQ=true)
- **Format:** `amqp://{user}:{pass}@{host}:{port}/`
- **Railway Alternative:** `RABBITMQ_URL` (provided by Railway RabbitMQ plugin)

**Examples:**
```bash
# Development
RabbitMQ__ConnectionString=amqp://dev:devpass@localhost:5672/

# Staging/Production (Railway)
RabbitMQ__ConnectionString=amqp://guest:guest@rabbitmq.railway.internal:5672/
# OR use Railway provided:
RABBITMQ_URL=amqps://user:pass@rabbitmq.railway.app:5672
```

#### RabbitMQ__Queues__PlantAnalysisRequest
- **Description:** Queue name for plant analysis requests
- **Type:** String
- **Required:** No
- **Default:** `plant-analysis-requests`

#### RabbitMQ__Queues__PlantAnalysisResult
- **Description:** Queue name for plant analysis results
- **Type:** String
- **Required:** No
- **Default:** `plant-analysis-results`

#### RabbitMQ__Queues__Notification
- **Description:** Queue name for notifications
- **Type:** String
- **Required:** No
- **Default:** `notifications`

### Redis Configuration

#### CacheOptions__Host
- **Description:** Redis server hostname
- **Type:** String
- **Required:** Yes (if UseRedis=true)
- **Railway Alternative:** `REDIS_HOST` (provided by Railway Redis plugin)

#### CacheOptions__Port
- **Description:** Redis server port
- **Type:** Number
- **Required:** Yes (if UseRedis=true)
- **Default:** 6379
- **Railway Alternative:** `REDIS_PORT`

#### CacheOptions__Password
- **Description:** Redis authentication password
- **Type:** String
- **Required:** Depends on Redis configuration
- **Railway Alternative:** `REDIS_PASSWORD`

#### CacheOptions__Ssl
- **Description:** Enable SSL/TLS for Redis connection
- **Type:** Boolean
- **Required:** No
- **Default:** `false`
- **Railway Staging:** `false` (internal network - no SSL required)
- **Note:** Railway internal network (redis.railway.internal) doesn't require SSL

#### CacheOptions__Database
- **Description:** Redis database number
- **Type:** Number
- **Required:** No
- **Default:** 0

**Examples:**
```bash
# Development (local Redis)
CacheOptions__Host=localhost
CacheOptions__Port=6379
CacheOptions__Password=devredispass
CacheOptions__Ssl=false

# Staging (Railway Redis - Internal Network)
CacheOptions__Host=redis.railway.internal
CacheOptions__Port=6379
CacheOptions__Password=RAILWAY_REDIS_PASSWORD
CacheOptions__Ssl=false
```

### N8N Webhook Configuration

#### N8N__WebhookUrl
- **Description:** N8N webhook endpoint for AI plant analysis
- **Type:** String (URL)
- **Required:** Yes
- **Format:** `https://{n8n-domain}/webhook/api/plant-analysis`

**Examples:**
```bash
# Development
N8N__WebhookUrl=http://localhost:5678/webhook/api/plant-analysis

# Staging
N8N__WebhookUrl=https://staging-n8n.ziraai.com/webhook/api/plant-analysis

# Production
N8N__WebhookUrl=https://n8n.ziraai.com/webhook/api/plant-analysis
```

#### N8N__UseImageUrl
- **Description:** Send image URL instead of base64 (reduces token usage by 99.6%)
- **Type:** Boolean
- **Required:** No
- **Default:** `true`

---

## 4. Referral & Deep Links

### Mobile App Configuration

#### MobileApp__PlayStorePackageName
- **Description:** Android package name for Play Store links
- **Type:** String
- **Required:** Yes
- **Format:** `com.{company}.{app}.{environment}`

**Values:**
```bash
# Development
MobileApp__PlayStorePackageName=com.ziraai.app.dev

# Staging
MobileApp__PlayStorePackageName=com.ziraai.app.staging

# Production
MobileApp__PlayStorePackageName=com.ziraai.app
```

### Referral Deep Links

#### Referral__DeepLinkBaseUrl
- **Description:** Base URL for referral deep links
- **Type:** String (URL with trailing slash)
- **Required:** Yes
- **Format:** `https://{domain}/ref/`

**Values:**
```bash
# Development
Referral__DeepLinkBaseUrl=https://localhost:5001/ref/

# Staging
Referral__DeepLinkBaseUrl=https://ziraai-api-sit.up.railway.app/ref/

# Production
Referral__DeepLinkBaseUrl=https://ziraai.com/ref/
```

#### Referral__FallbackDeepLinkBaseUrl
- **Description:** Fallback URL if database config fails
- **Type:** String (URL with trailing slash)
- **Required:** Yes
- **Note:** Should match `Referral__DeepLinkBaseUrl`

### Sponsor Request Deep Links

#### SponsorRequest__DeepLinkBaseUrl
- **Description:** Base URL for sponsor request deep links
- **Type:** String (URL with trailing slash)
- **Required:** Yes
- **Format:** `https://{domain}/sponsor-request/`

**Values:**
```bash
# Development
SponsorRequest__DeepLinkBaseUrl=https://localhost:5001/sponsor-request/

# Staging
SponsorRequest__DeepLinkBaseUrl=https://ziraai-api-sit.up.railway.app/sponsor-request/

# Production
SponsorRequest__DeepLinkBaseUrl=https://ziraai.com/sponsor-request/
```

#### SponsorRequest__TokenExpiryHours
- **Description:** Expiry time for sponsor request tokens
- **Type:** Number (hours)
- **Required:** No
- **Default:** 24

#### SponsorRequest__MaxRequestsPerDay
- **Description:** Maximum sponsor requests per day per farmer
- **Type:** Number
- **Required:** No
- **Default:** 10

---

## 5. Security & Authentication

### JWT Token Configuration

#### TokenOptions__SecurityKey
- **Description:** Secret key for JWT token signing (minimum 32 characters)
- **Type:** String
- **Required:** Yes
- **⚠️ CRITICAL:** Must be different per environment

**Examples (DO NOT USE THESE VALUES):**
```bash
# Development
TokenOptions__SecurityKey=ZiraAI-Dev-JWT-SecretKey-2025!@#$%^&*()1234567890

# Staging
TokenOptions__SecurityKey=ZiraAI-Staging-JWT-SecretKey-2025!@#DIFFERENT

# Production
TokenOptions__SecurityKey=GENERATE_STRONG_RANDOM_KEY_FOR_PRODUCTION_64_CHARS_MIN
```

#### TokenOptions__Audience
- **Description:** JWT token audience claim
- **Type:** String
- **Required:** Yes

**Values:**
```bash
# Development
TokenOptions__Audience=ZiraAI_Dev_Users

# Staging
TokenOptions__Audience=ZiraAI_Staging_Users

# Production
TokenOptions__Audience=ZiraAI_Production_Users
```

#### TokenOptions__Issuer
- **Description:** JWT token issuer claim
- **Type:** String
- **Required:** Yes

**Values:**
```bash
# Development
TokenOptions__Issuer=ZiraAI_Dev

# Staging
TokenOptions__Issuer=ZiraAI_Staging

# Production
TokenOptions__Issuer=ZiraAI_Production
```

#### TokenOptions__AccessTokenExpiration
- **Description:** Access token expiry in minutes
- **Type:** Number (minutes)
- **Required:** No
- **Default:** 60

#### TokenOptions__RefreshTokenExpiration
- **Description:** Refresh token expiry in minutes
- **Type:** Number (minutes)
- **Required:** No
- **Default:** 180

### Internal API Security

#### Security__RequestTokenSecret
- **Description:** Secret key for sponsor request token generation
- **Type:** String
- **Required:** Yes
- **⚠️ CRITICAL:** Must be different per environment

#### WebAPI__InternalSecret
- **Description:** Internal API authentication secret
- **Type:** String
- **Required:** Yes (for internal service-to-service communication)

---

## 6. File Storage

### Provider Selection

#### FileStorage__Provider
- **Description:** Storage provider to use
- **Type:** String (enum)
- **Required:** Yes
- **Values:** `Local`, `FreeImageHost`, `ImgBB`, `S3`

**Recommendations:**
- Development: `FreeImageHost` or `Local`
- Staging: `FreeImageHost` or `S3`
- Production: `S3` (for reliability and CDN)

### Local Storage

#### FileStorage__Local__BasePath
- **Description:** File system path for uploads
- **Type:** String (relative or absolute path)
- **Required:** Yes (if Provider=Local)
- **Default:** `wwwroot/uploads`

#### FileStorage__Local__BaseUrl
- **Description:** Public URL base for accessing files
- **Type:** String (URL)
- **Required:** Yes (if Provider=Local)

**Examples:**
```bash
# Development
FileStorage__Local__BaseUrl=https://localhost:5001

# Staging
FileStorage__Local__BaseUrl=https://ziraai-api-sit.up.railway.app

# Production
FileStorage__Local__BaseUrl=https://ziraai.com
```

### FreeImageHost

#### FileStorage__FreeImageHost__ApiKey
- **Description:** FreeImageHost API key
- **Type:** String
- **Required:** Yes (if Provider=FreeImageHost)
- **Current:** `6d207e02198a847aa98d0a2a901485a5`

### ImgBB

#### FileStorage__ImgBB__ApiKey
- **Description:** ImgBB API key
- **Type:** String
- **Required:** Yes (if Provider=ImgBB)

#### FileStorage__ImgBB__ExpirationSeconds
- **Description:** Image expiration time (0 = never)
- **Type:** Number (seconds)
- **Required:** No
- **Default:** 0 (never expire)

### AWS S3

#### FileStorage__S3__BucketName
- **Description:** S3 bucket name
- **Type:** String
- **Required:** Yes (if Provider=S3)

**Examples:**
```bash
# Staging
FileStorage__S3__BucketName=ziraai-staging-images

# Production
FileStorage__S3__BucketName=ziraai-production-images
```

#### FileStorage__S3__Region
- **Description:** AWS region
- **Type:** String
- **Required:** Yes (if Provider=S3)
- **Default:** `us-east-1`

#### FileStorage__S3__UseCloudFront
- **Description:** Use CloudFront CDN
- **Type:** Boolean
- **Required:** No
- **Default:** `false`

#### FileStorage__S3__CloudFrontDomain
- **Description:** CloudFront distribution domain
- **Type:** String (domain name)
- **Required:** Yes (if UseCloudFront=true)

**Examples:**
```bash
# Staging
FileStorage__S3__CloudFrontDomain=cdn-staging.ziraai.com

# Production
FileStorage__S3__CloudFrontDomain=cdn.ziraai.com
```

#### AWS_ACCESS_KEY_ID
- **Description:** AWS access key (standard AWS environment variable)
- **Type:** String
- **Required:** Yes (if Provider=S3)

#### AWS_SECRET_ACCESS_KEY
- **Description:** AWS secret key (standard AWS environment variable)
- **Type:** String
- **Required:** Yes (if Provider=S3)

---

## 7. Monitoring & Logging

### Serilog File Logging

#### SeriLogConfigurations__FileLogConfiguration__FolderPath
- **Description:** Directory path for log files
- **Type:** String (path)
- **Required:** No

**Values:**
```bash
# Development
SeriLogConfigurations__FileLogConfiguration__FolderPath=logs/dev/

# Staging
SeriLogConfigurations__FileLogConfiguration__FolderPath=/app/logs/staging/

# Production
SeriLogConfigurations__FileLogConfiguration__FolderPath=/app/logs/
```

#### SeriLogConfigurations__FileLogConfiguration__RollingInterval
- **Description:** Log file rotation interval
- **Type:** String (enum)
- **Required:** No
- **Values:** `Hour`, `Day`, `Week`, `Month`
- **Default:** `Hour` (dev), `Day` (staging/prod)

#### SeriLogConfigurations__FileLogConfiguration__RetainedFileCountLimit
- **Description:** Number of log files to keep
- **Type:** Number
- **Required:** No
- **Default:** 24 (dev), 7 (staging/prod)

#### SeriLogConfigurations__FileLogConfiguration__FileSizeLimitBytes
- **Description:** Maximum log file size in bytes
- **Type:** Number
- **Required:** No
- **Default:** 10485760 (10MB dev), 52428800 (50MB prod)

### Performance Monitoring

#### SeriLogConfigurations__PerformanceMonitoring__Enabled
- **Description:** Enable performance tracking
- **Type:** Boolean
- **Required:** No
- **Default:** `true`

#### SeriLogConfigurations__PerformanceMonitoring__SlowOperationThresholdMs
- **Description:** Threshold for slow operation warnings (milliseconds)
- **Type:** Number
- **Required:** No
- **Default:** 1000 (dev), 2000 (prod)

#### SeriLogConfigurations__PerformanceMonitoring__CriticalOperationThresholdMs
- **Description:** Threshold for critical operation errors (milliseconds)
- **Type:** Number
- **Required:** No
- **Default:** 3000 (dev), 5000 (prod)

---

## 8. SignalR Configuration

⚠️ **STATUS:** Missing in Railway Staging - Needs to be added

**Real-time notification system requires these variables for optimal performance.**

### SignalR__UseRedisBackplane
- **Description:** Enable Redis backplane for multi-instance SignalR
- **Type:** Boolean
- **Required:** No
- **Default:** `false`
- **Note:** Set to `true` when scaling horizontally (multiple app instances)

### SignalR__MaxConnectionsPerUser
- **Description:** Maximum simultaneous connections per user
- **Type:** Number
- **Required:** No
- **Default:** 5

### SignalR__ConnectionTimeout
- **Description:** Connection timeout in seconds
- **Type:** Number
- **Required:** No
- **Default:** 30

### SignalR__KeepAliveInterval
- **Description:** Keep-alive ping interval in seconds
- **Type:** Number
- **Required:** No
- **Default:** 15

---

## 9. Background Jobs (Hangfire)

**Note:** These apply to `PlantAnalysisWorkerService` project.

### TaskSchedulerOptions__Enabled
- **Description:** Enable Hangfire background job processing
- **Type:** Boolean
- **Required:** Yes
- **Default:** `false` (dev), `true` (staging/prod)

### TaskSchedulerOptions__StorageType
- **Description:** Hangfire storage backend
- **Type:** String (enum)
- **Required:** Yes (if Enabled=true)
- **Values:** `inmemory`, `postgresql`
- **Default:** `inmemory` (dev), `postgresql` (staging/prod)

### TaskSchedulerOptions__ConnectionString
- **Description:** Database connection for Hangfire storage
- **Type:** String (connection string)
- **Required:** Yes (if StorageType=postgresql)
- **Note:** Usually same as main database connection

### TaskSchedulerOptions__Path
- **Description:** Hangfire dashboard URL path
- **Type:** String
- **Required:** No
- **Default:** `/hangfire` (WebAPI), `/hangfire-worker` (WorkerService)

### TaskSchedulerOptions__Title
- **Description:** Hangfire dashboard title
- **Type:** String
- **Required:** No

### TaskSchedulerOptions__Username
- **Description:** Hangfire dashboard username
- **Type:** String
- **Required:** Yes (if Enabled=true)
- **⚠️ CRITICAL:** Use strong credentials in production

### TaskSchedulerOptions__Password
- **Description:** Hangfire dashboard password
- **Type:** String
- **Required:** Yes (if Enabled=true)
- **⚠️ CRITICAL:** Use strong password in production

---

## 10. SMS Service Configuration

⚠️ **STATUS:** Missing in Railway Staging - Needs to be added

### SMS Service Provider

#### SmsService__Provider
- **Description:** SMS provider to use
- **Type:** String (enum)
- **Required:** Yes
- **Values:** `Mock`, `Twilio`, `Netgsm`

**Development:** Use `Mock` for testing without costs

#### SmsService__MockSettings__UseFixedCode
- **Description:** Use fixed verification code for testing
- **Type:** Boolean
- **Required:** No (only for Mock provider)
- **Default:** `true`

#### SmsService__MockSettings__FixedCode
- **Description:** Fixed verification code value
- **Type:** String (6 digits)
- **Required:** No (only for Mock provider)
- **Default:** `123456`

### Twilio SMS

#### SmsService__TwilioSettings__AccountSid
- **Description:** Twilio account SID
- **Type:** String
- **Required:** Yes (if Provider=Twilio)

#### SmsService__TwilioSettings__AuthToken
- **Description:** Twilio auth token
- **Type:** String
- **Required:** Yes (if Provider=Twilio)

#### SmsService__TwilioSettings__FromNumber
- **Description:** Twilio phone number (from)
- **Type:** String (phone number format)
- **Required:** Yes (if Provider=Twilio)

### Netgsm SMS

#### SmsService__NetgsmSettings__UserCode
- **Description:** Netgsm user code
- **Type:** String
- **Required:** Yes (if Provider=Netgsm)

#### SmsService__NetgsmSettings__Password
- **Description:** Netgsm password
- **Type:** String
- **Required:** Yes (if Provider=Netgsm)

#### SmsService__NetgsmSettings__SenderId
- **Description:** SMS sender ID
- **Type:** String
- **Required:** No
- **Default:** `ZIRAAI`

### WhatsApp Service

#### WhatsAppService__Provider
- **Description:** WhatsApp provider to use
- **Type:** String (enum)
- **Required:** Yes
- **Values:** `Mock`, `Twilio`, `WhatsAppBusiness`

#### WhatsAppService__TwilioSettings__AccountSid
- **Description:** Twilio account SID
- **Type:** String
- **Required:** Yes (if Provider=Twilio)

#### WhatsAppService__TwilioSettings__AuthToken
- **Description:** Twilio auth token
- **Type:** String
- **Required:** Yes (if Provider=Twilio)

#### WhatsAppService__TwilioSettings__FromNumber
- **Description:** Twilio WhatsApp number
- **Type:** String
- **Required:** Yes (if Provider=Twilio)
- **Format:** `whatsapp:+14155238886`

### WhatsApp Business API

#### WhatsAppService__WhatsAppBusinessSettings__BaseUrl
- **Description:** WhatsApp Business API base URL
- **Type:** String (URL)
- **Required:** Yes (if Provider=WhatsAppBusiness)
- **Default:** `https://graph.facebook.com/v18.0`

#### WhatsAppService__WhatsAppBusinessSettings__AccessToken
- **Description:** WhatsApp Business access token
- **Type:** String
- **Required:** Yes (if Provider=WhatsAppBusiness)

#### WhatsAppService__WhatsAppBusinessSettings__BusinessPhoneNumberId
- **Description:** WhatsApp Business phone number ID
- **Type:** String
- **Required:** Yes (if Provider=WhatsAppBusiness)

#### WhatsAppService__WhatsAppBusinessSettings__WebhookVerifyToken
- **Description:** Webhook verification token
- **Type:** String
- **Required:** No

---

## 11. WhatsApp Business API Configuration

✅ **STATUS:** Configured in Railway Staging (Comprehensive Setup)

**Description:** Advanced WhatsApp Business API integration with comprehensive tracking and fallback capabilities.

### Base Configuration

#### WhatsApp__ApiUrl
- **Description:** WhatsApp Business API base URL
- **Type:** String (URL)
- **Required:** Yes (if using WhatsApp Business API)
- **Railway Staging:** `https://graph.facebook.com/v18.0/`

#### WhatsApp__AccessToken
- **Description:** WhatsApp Business access token
- **Type:** String
- **Required:** Yes
- **Railway Staging:** `YOUR_WHATSAPP_ACCESS_TOKEN_HERE` (placeholder - needs real token)
- **⚠️ ACTION:** Replace with real access token for production

#### WhatsApp__PhoneNumberId
- **Description:** WhatsApp Business phone number ID
- **Type:** String
- **Required:** Yes
- **Railway Staging:** `YOUR_PHONE_NUMBER_ID_HERE` (placeholder)
- **⚠️ ACTION:** Configure real phone number ID

#### WhatsApp__BusinessAccountId
- **Description:** WhatsApp Business account ID
- **Type:** String
- **Required:** Yes
- **Railway Staging:** `YOUR_BUSINESS_ACCOUNT_ID_HERE` (placeholder)

### Webhook Configuration

#### WhatsApp__VerifyToken
- **Description:** Webhook verification token
- **Type:** String
- **Required:** Yes (for webhook setup)
- **Railway Staging:** `ziraai_webhook_verification_token`

#### WhatsApp__WebhookUrl
- **Description:** Webhook endpoint path
- **Type:** String
- **Required:** Yes
- **Railway Staging:** `/api/v1/webhooks/whatsapp`

### Performance & Rate Limiting

#### WhatsApp__MaxRetryAttempts
- **Description:** Maximum retry attempts for failed messages
- **Type:** Number
- **Required:** No
- **Railway Staging:** `3`
- **Default:** 3

#### WhatsApp__RequestTimeoutSeconds
- **Description:** Request timeout in seconds
- **Type:** Number
- **Required:** No
- **Railway Staging:** `30`
- **Default:** 30

#### WhatsApp__RateLimitPerSecond
- **Description:** Maximum WhatsApp API calls per second
- **Type:** Number
- **Required:** No
- **Railway Staging:** `80`
- **Note:** WhatsApp Business API rate limit is 80 messages/second

### Localization

#### WhatsApp__DefaultLanguage
- **Description:** Default language for messages
- **Type:** String (ISO 639-1 code)
- **Required:** No
- **Railway Staging:** `tr` (Turkish)
- **Default:** `tr`

### Tracking & Analytics

#### WhatsApp__EnableDeliveryStatusTracking
- **Description:** Track message delivery status
- **Type:** Boolean
- **Required:** No
- **Railway Staging:** `true`
- **Default:** `true`

#### WhatsApp__EnableReadReceiptsTracking
- **Description:** Track message read receipts
- **Type:** Boolean
- **Required:** No
- **Railway Staging:** `true`
- **Default:** `true`

### Fallback Configuration

#### WhatsApp__FallbackToSMS
- **Description:** Automatically fallback to SMS if WhatsApp fails
- **Type:** Boolean
- **Required:** No
- **Railway Staging:** `true`
- **Default:** `false`
- **Note:** Excellent feature for reliability!

**Example Configuration:**
```bash
# Railway Staging WhatsApp Configuration
WhatsApp__ApiUrl=https://graph.facebook.com/v18.0/
WhatsApp__AccessToken=EAAxxxxxxxxxxxxxxxxx
WhatsApp__PhoneNumberId=1234567890123456
WhatsApp__BusinessAccountId=9876543210987654
WhatsApp__VerifyToken=ziraai_webhook_verification_token
WhatsApp__WebhookUrl=/api/v1/webhooks/whatsapp
WhatsApp__MaxRetryAttempts=3
WhatsApp__RequestTimeoutSeconds=30
WhatsApp__RateLimitPerSecond=80
WhatsApp__DefaultLanguage=tr
WhatsApp__EnableDeliveryStatusTracking=true
WhatsApp__EnableReadReceiptsTracking=true
WhatsApp__FallbackToSMS=true
```

---

## 12. Notification Settings

✅ **STATUS:** Configured in Railway Staging (Advanced Features)

**Description:** Comprehensive notification management system with quiet hours, preferences, and analytics.

### Channel Configuration

#### NotificationSettings__DefaultChannel
- **Description:** Default notification channel
- **Type:** String (enum)
- **Required:** Yes
- **Railway Staging:** `WhatsApp`
- **Values:** `WhatsApp`, `SMS`, `Email`, `Push`

### Quiet Hours

#### NotificationSettings__EnableQuietHours
- **Description:** Enable quiet hours (no notifications during sleep time)
- **Type:** Boolean
- **Required:** No
- **Railway Staging:** `true`
- **Default:** `false`

#### NotificationSettings__DefaultQuietHoursStart
- **Description:** Quiet hours start time
- **Type:** String (HH:mm format)
- **Required:** No (if EnableQuietHours=true)
- **Railway Staging:** `22:00`

#### NotificationSettings__DefaultQuietHoursEnd
- **Description:** Quiet hours end time
- **Type:** String (HH:mm format)
- **Required:** No (if EnableQuietHours=true)
- **Railway Staging:** `08:00`

### Rate Limiting

#### NotificationSettings__MaxNotificationsPerUserPerDay
- **Description:** Maximum notifications per user per day
- **Type:** Number
- **Required:** No
- **Railway Staging:** `10`
- **Default:** No limit

### User Preferences

#### NotificationSettings__EnableUserPreferences
- **Description:** Allow users to customize notification settings
- **Type:** Boolean
- **Required:** No
- **Railway Staging:** `true`
- **Default:** `false`

### Reliability

#### NotificationSettings__RetryFailedNotifications
- **Description:** Automatically retry failed notifications
- **Type:** Boolean
- **Required:** No
- **Railway Staging:** `true`
- **Default:** `true`

### Analytics

#### NotificationSettings__EnableNotificationAnalytics
- **Description:** Track notification delivery and engagement metrics
- **Type:** Boolean
- **Required:** No
- **Railway Staging:** `true`
- **Default:** `false`

**Example Configuration:**
```bash
# Railway Staging Notification Settings
NotificationSettings__DefaultChannel=WhatsApp
NotificationSettings__EnableQuietHours=true
NotificationSettings__DefaultQuietHoursStart=22:00
NotificationSettings__DefaultQuietHoursEnd=08:00
NotificationSettings__MaxNotificationsPerUserPerDay=10
NotificationSettings__EnableUserPreferences=true
NotificationSettings__RetryFailedNotifications=true
NotificationSettings__EnableNotificationAnalytics=true
```

---

## 13. Email Configuration

✅ **STATUS:** Configured in Railway Staging (Credentials Needed)

**Description:** SMTP email configuration for transactional emails.

### SMTP Server

#### MailSettings__Server
- **Description:** SMTP server hostname
- **Type:** String
- **Required:** Yes (if using email)
- **Railway Staging:** `smtp.gmail.com`

#### MailSettings__Port
- **Description:** SMTP server port
- **Type:** Number
- **Required:** Yes
- **Railway Staging:** `587`
- **Note:** 587 for TLS, 465 for SSL, 25 for unencrypted

### Sender Information

#### MailSettings__SenderFullName
- **Description:** Display name for sender
- **Type:** String
- **Required:** Yes
- **Railway Staging:** `Ziraai`

#### MailSettings__SenderEmail
- **Description:** Email address for sender
- **Type:** String (email format)
- **Required:** Yes
- **Railway Staging:** Empty (needs configuration)
- **⚠️ ACTION:** Add sender email address

### Authentication

#### MailSettings__UserName
- **Description:** SMTP authentication username
- **Type:** String
- **Required:** Yes (if SMTP requires auth)
- **Railway Staging:** Empty (needs configuration)

#### MailSettings__Password
- **Description:** SMTP authentication password
- **Type:** String
- **Required:** Yes (if SMTP requires auth)
- **Railway Staging:** Empty (needs configuration)
- **⚠️ ACTION:** Add SMTP password or app-specific password

### Security

#### MailSettings__UseSSL
- **Description:** Use SSL encryption
- **Type:** Boolean
- **Required:** No
- **Railway Staging:** `false`
- **Note:** false because UseStartTls is enabled

#### MailSettings__UseStartTls
- **Description:** Use STARTTLS encryption
- **Type:** Boolean
- **Required:** No
- **Railway Staging:** `true`
- **Recommended:** `true` for port 587

**Example Configuration:**
```bash
# Railway Staging Email Settings
MailSettings__Server=smtp.gmail.com
MailSettings__Port=587
MailSettings__SenderFullName=Ziraai
MailSettings__SenderEmail=noreply@ziraai.com
MailSettings__UserName=noreply@ziraai.com
MailSettings__Password=YOUR_APP_SPECIFIC_PASSWORD
MailSettings__UseSSL=false
MailSettings__UseStartTls=true
```

---

## 14. MongoDB Configuration

✅ **STATUS:** Configured in Railway Staging (Optional Service)

**Description:** MongoDB connection for document storage (optional, if used).

#### MongoDbSettings__ConnectionString
- **Description:** MongoDB connection URI
- **Type:** String (MongoDB URI format)
- **Required:** Yes (if using MongoDB)
- **Railway Staging:** `mongodb://localhost:27017/ziraaidb?readPreference=primary&appname=MongoDB%20Compass&ssl=false`
- **Note:** Points to localhost (likely not actively used in Railway)

#### MongoDbSettings__DatabaseName
- **Description:** MongoDB database name
- **Type:** String
- **Required:** Yes (if using MongoDB)
- **Railway Staging:** `ziraaidb`

**Example Configuration:**
```bash
# Railway Staging MongoDB Settings (if needed)
MongoDbSettings__ConnectionString=mongodb+srv://user:pass@cluster.mongodb.net/ziraaidb?retryWrites=true&w=majority
MongoDbSettings__DatabaseName=ziraaidb
```

**Note:** Currently configured but likely not actively used. ElasticSearch and PostgreSQL are primary data stores.

---

## 15. AI Optimization

✅ **STATUS:** Configured in Railway Staging (Higher Quality Settings)

**Description:** Image optimization settings for AI plant analysis (reduces OpenAI token usage by 99.6%).

#### AIOptimization__Enabled
- **Description:** Enable image optimization
- **Type:** Boolean
- **Required:** No
- **Railway Staging:** `true`
- **Default:** `true`

#### AIOptimization__MaxSizeMB
- **Description:** Maximum image size in megabytes
- **Type:** Decimal
- **Required:** No
- **Railway Staging:** `0.25` (250 KB)
- **Default:** `0.25`

#### AIOptimization__MaxWidth
- **Description:** Maximum image width in pixels
- **Type:** Number
- **Required:** No
- **Railway Staging:** `1024` (higher than doc default)
- **Documentation Default:** `800`
- **Note:** Railway uses higher quality settings

#### AIOptimization__MaxHeight
- **Description:** Maximum image height in pixels
- **Type:** Number
- **Required:** No
- **Railway Staging:** `768` (higher than doc default)
- **Documentation Default:** `600`

#### AIOptimization__Quality
- **Description:** JPEG compression quality (0-100)
- **Type:** Number
- **Required:** No
- **Railway Staging:** `80` (higher than doc default)
- **Documentation Default:** `70`

**Example Configuration:**
```bash
# Railway Staging AI Optimization (Higher Quality)
AIOptimization__Enabled=true
AIOptimization__MaxSizeMB=0.25
AIOptimization__MaxWidth=1024
AIOptimization__MaxHeight=768
AIOptimization__Quality=80
```

**Alternative Format (Railway also has these):**
```bash
AI_OPTIMIZATION_ENABLED=true
AI_OPTIMIZATION_MAX_SIZE_MB=0.25
AI_OPTIMIZATION_MAX_WIDTH=1024
AI_OPTIMIZATION_MAX_HEIGHT=768
AI_OPTIMIZATION_QUALITY=80
```

---

## 16. CORS & Startup

✅ **STATUS:** Configured in Railway Staging

### CORS Configuration

#### CORS_ALLOWED_ORIGINS
- **Description:** Allowed CORS origins
- **Type:** String (comma-separated URLs)
- **Required:** No
- **Railway Staging:** `https://ziraai-api-sit.up.railway.app`
- **Note:** Should include frontend domains

#### AllowedHosts
- **Description:** ASP.NET Core allowed hosts
- **Type:** String
- **Required:** No
- **Railway Staging:** `*` (all hosts allowed)
- **Production Recommendation:** Specify exact hosts

### Startup Configuration

#### STARTUP_DEBUG
- **Description:** Enable detailed startup debugging
- **Type:** Boolean
- **Required:** No
- **Railway Staging:** `true`
- **Recommendation:** `false` for production

#### RAILWAY_DOCKERFILE_PATH
- **Description:** Path to Dockerfile for Railway deployment
- **Type:** String
- **Required:** No (Railway-specific)
- **Railway Staging:** `Dockerfile.webapi`

**Example Configuration:**
```bash
# Railway Staging CORS & Startup
CORS_ALLOWED_ORIGINS=https://ziraai-api-sit.up.railway.app,https://staging.ziraai.com
AllowedHosts=*
STARTUP_DEBUG=true
RAILWAY_DOCKERFILE_PATH=Dockerfile.webapi
```

---

## 17. Railway-Specific Variables

These are automatically provided by Railway or Railway plugins.

### RAILWAY_ENVIRONMENT
- **Description:** Railway environment identifier
- **Type:** String
- **Auto-provided:** Yes
- **Used for:** Cloud detection in Program.cs

### DATABASE_URL
- **Description:** PostgreSQL connection URL (Railway format)
- **Type:** String (URL format)
- **Auto-provided:** Yes (by Railway PostgreSQL plugin)
- **Note:** Automatically mapped to `ConnectionStrings__DArchPgContext`

### REDIS_URL
- **Description:** Redis connection URL
- **Type:** String (URL format)
- **Auto-provided:** Yes (by Railway Redis plugin)

### REDIS_HOST
- **Description:** Redis hostname
- **Type:** String
- **Auto-provided:** Yes (by Railway Redis plugin)

### REDIS_PORT
- **Description:** Redis port
- **Type:** Number
- **Auto-provided:** Yes (by Railway Redis plugin)

### REDIS_PASSWORD
- **Description:** Redis password
- **Type:** String
- **Auto-provided:** Yes (by Railway Redis plugin)

### RABBITMQ_URL
- **Description:** RabbitMQ connection URL
- **Type:** String (AMQP URI)
- **Auto-provided:** Yes (by Railway RabbitMQ plugin)

### RAILWAY_PUBLIC_DOMAIN
- **Description:** Public domain for the service
- **Type:** String (domain)
- **Auto-provided:** Yes
- **Example:** `ziraai-api-sit.up.railway.app`

### PORT
- **Description:** Port to bind the application
- **Type:** Number
- **Auto-provided:** Yes
- **Default:** Railway assigns dynamically

---

## 12. Development Environment (Complete)

### Minimal Required Variables

```bash
# Core
ASPNETCORE_ENVIRONMENT=Development

# Database (local PostgreSQL)
ConnectionStrings__DArchPgContext=Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass

# Redis (optional for dev)
UseRedis=false
```

### Full Development Configuration

**File:** `WebAPI/appsettings.Development.json`

See the file for complete configuration. Key variables:
- All services set to `Mock` providers
- Local database and Redis
- Debug logging enabled
- File storage: `FreeImageHost`
- N8N webhook: `http://localhost:5678/webhook/api/plant-analysis`

---

## 13. Staging Environment (Railway-Validated)

✅ **STATUS:** Validated against actual Railway staging environment (2025-10-07)

### Railway Environment Variables

**⚠️ IMPORTANT:** These values are validated from the actual Railway staging deployment. Some categories are missing and need to be added (see warnings below).

```bash
# ===================================
# CRITICAL - REQUIRED ✅ CONFIGURED
# ===================================
ASPNETCORE_ENVIRONMENT=Staging
ASPNETCORE_URLS=http://0.0.0.0:8080

# Database (Railway PostgreSQL) ✅ CONFIGURED
DATABASE_CONNECTION_STRING=Host=postgres.railway.internal;Port=5432;Database=railway;Username=postgres;Password=***
ConnectionStrings__DArchPgContext=Host=postgres.railway.internal;Port=5432;Database=railway;Username=postgres;Password=***

# Redis (Railway Redis plugin) ✅ CONFIGURED
UseRedis=true
CacheOptions__Host=redis.railway.internal
CacheOptions__Port=6379  # ⚠️ NOT 38265
CacheOptions__Password=***
CacheOptions__Database=0
CacheOptions__Ssl=false  # ⚠️ Internal network = no SSL needed

# RabbitMQ (Railway RabbitMQ plugin) ✅ CONFIGURED
UseRabbitMQ=true
RabbitMQ__ConnectionString=amqp://user:pass@rabbitmq.railway.internal:5672
RabbitMQ__Queues__PlantAnalysisRequest=plant-analysis-requests
RabbitMQ__Queues__PlantAnalysisResult=plant-analysis-results
RabbitMQ__Queues__Notification=notifications
RabbitMQ__RetrySettings__MaxRetryAttempts=3
RabbitMQ__RetrySettings__RetryDelayMilliseconds=2000
RabbitMQ__ConnectionSettings__RequestedHeartbeat=120
RabbitMQ__ConnectionSettings__NetworkRecoveryInterval=15

# ===================================
# REFERRAL SYSTEM - REQUIRED
# ===================================
MobileApp__PlayStorePackageName=com.ziraai.app.staging
Referral__DeepLinkBaseUrl=https://ziraai-api-sit.up.railway.app/ref/
Referral__FallbackDeepLinkBaseUrl=https://ziraai-api-sit.up.railway.app/ref/
SponsorRequest__DeepLinkBaseUrl=https://ziraai-api-sit.up.railway.app/sponsor-request/

# ===================================
# SECURITY - REQUIRED
# ===================================
TokenOptions__SecurityKey=GENERATE_STAGING_SPECIFIC_KEY_MIN_64_CHARS
TokenOptions__Audience=ZiraAI_Staging_Users
TokenOptions__Issuer=ZiraAI_Staging
Security__RequestTokenSecret=GENERATE_STAGING_SECRET_KEY
WebAPI__InternalSecret=GENERATE_STAGING_INTERNAL_SECRET

# ===================================
# EXTERNAL SERVICES
# ===================================
# N8N Webhook
N8N__WebhookUrl=https://staging-n8n.ziraai.com/webhook/api/plant-analysis

# File Storage (S3 recommended for staging)
FileStorage__Provider=S3
FileStorage__S3__BucketName=ziraai-staging-images
FileStorage__S3__Region=us-east-1
FileStorage__S3__UseCloudFront=true
FileStorage__S3__CloudFrontDomain=cdn-staging.ziraai.com
AWS_ACCESS_KEY_ID=YOUR_AWS_ACCESS_KEY
AWS_SECRET_ACCESS_KEY=YOUR_AWS_SECRET_KEY

# SMS/WhatsApp (optional - can use Mock)
SmsService__Provider=Netgsm
SmsService__NetgsmSettings__UserCode=YOUR_NETGSM_USER
SmsService__NetgsmSettings__Password=YOUR_NETGSM_PASSWORD
SmsService__NetgsmSettings__SenderId=ZIRAAI

WhatsAppService__Provider=Twilio
WhatsAppService__TwilioSettings__AccountSid=YOUR_TWILIO_SID
WhatsAppService__TwilioSettings__AuthToken=YOUR_TWILIO_TOKEN
WhatsAppService__TwilioSettings__FromNumber=whatsapp:+14155238886

# ===================================
# WORKER SERVICE (if deploying separately)
# ===================================
TaskSchedulerOptions__Enabled=true
TaskSchedulerOptions__StorageType=postgresql
TaskSchedulerOptions__ConnectionString=Host=postgres.railway.internal;Port=5432;Database=railway;Username=postgres;Password=RAILWAY_POSTGRES_PASSWORD
TaskSchedulerOptions__Username=admin
TaskSchedulerOptions__Password=STRONG_STAGING_PASSWORD
```

---

## 14. Production Environment (Complete)

### Railway Environment Variables

```bash
# ===================================
# CRITICAL - REQUIRED
# ===================================
ASPNETCORE_ENVIRONMENT=Production

# Database (Railway PostgreSQL)
# Railway provides DATABASE_URL automatically, which is mapped in Program.cs
# OR set explicitly:
ConnectionStrings__DArchPgContext=Host=postgres.railway.internal;Port=5432;Database=railway;Username=postgres;Password=STRONG_PRODUCTION_PASSWORD

# Redis (Railway Redis plugin)
UseRedis=true
CacheOptions__Host=redis.railway.internal
CacheOptions__Port=REDIS_PORT_FROM_RAILWAY
CacheOptions__Password=STRONG_REDIS_PASSWORD
CacheOptions__Ssl=true

# RabbitMQ (Railway RabbitMQ plugin)
RabbitMQ__ConnectionString=RABBITMQ_URL_FROM_RAILWAY

# ===================================
# REFERRAL SYSTEM - REQUIRED
# ===================================
MobileApp__PlayStorePackageName=com.ziraai.app
Referral__DeepLinkBaseUrl=https://ziraai.com/ref/
Referral__FallbackDeepLinkBaseUrl=https://ziraai.com/ref/
SponsorRequest__DeepLinkBaseUrl=https://ziraai.com/sponsor-request/

# ===================================
# SECURITY - REQUIRED (UNIQUE VALUES!)
# ===================================
TokenOptions__SecurityKey=GENERATE_UNIQUE_PRODUCTION_KEY_MIN_128_CHARS_RECOMMENDED
TokenOptions__Audience=ZiraAI_Production_Users
TokenOptions__Issuer=ZiraAI_Production
Security__RequestTokenSecret=GENERATE_UNIQUE_PRODUCTION_SECRET
WebAPI__InternalSecret=GENERATE_UNIQUE_PRODUCTION_INTERNAL_SECRET

# ===================================
# EXTERNAL SERVICES
# ===================================
# N8N Webhook
N8N__WebhookUrl=https://n8n.ziraai.com/webhook/api/plant-analysis

# File Storage (S3 REQUIRED for production)
FileStorage__Provider=S3
FileStorage__S3__BucketName=ziraai-production-images
FileStorage__S3__Region=us-east-1
FileStorage__S3__UseCloudFront=true
FileStorage__S3__CloudFrontDomain=cdn.ziraai.com
AWS_ACCESS_KEY_ID=PRODUCTION_AWS_ACCESS_KEY
AWS_SECRET_ACCESS_KEY=PRODUCTION_AWS_SECRET_KEY

# SMS/WhatsApp (REQUIRED for production)
SmsService__Provider=Netgsm
SmsService__NetgsmSettings__UserCode=PRODUCTION_NETGSM_USER
SmsService__NetgsmSettings__Password=PRODUCTION_NETGSM_PASSWORD
SmsService__NetgsmSettings__SenderId=ZIRAAI

WhatsAppService__Provider=WhatsAppBusiness
WhatsAppService__WhatsAppBusinessSettings__BaseUrl=https://graph.facebook.com/v18.0
WhatsAppService__WhatsAppBusinessSettings__AccessToken=PRODUCTION_WHATSAPP_TOKEN
WhatsAppService__WhatsAppBusinessSettings__BusinessPhoneNumberId=PRODUCTION_PHONE_ID

# ===================================
# SIGNALR (enable backplane for multi-instance)
# ===================================
SignalR__UseRedisBackplane=true
SignalR__MaxConnectionsPerUser=5

# ===================================
# WORKER SERVICE (separate Railway service)
# ===================================
TaskSchedulerOptions__Enabled=true
TaskSchedulerOptions__StorageType=postgresql
TaskSchedulerOptions__ConnectionString=Host=postgres.railway.internal;Port=5432;Database=railway;Username=postgres;Password=PRODUCTION_DB_PASSWORD
TaskSchedulerOptions__Username=admin
TaskSchedulerOptions__Password=VERY_STRONG_PRODUCTION_PASSWORD

# ===================================
# MONITORING & LOGGING
# ===================================
SeriLogConfigurations__FileLogConfiguration__FolderPath=/app/logs/
SeriLogConfigurations__FileLogConfiguration__RollingInterval=Day
SeriLogConfigurations__FileLogConfiguration__RetainedFileCountLimit=30
```

---

## Testing & Verification

### Verify Environment Variables

```bash
# Check environment is set correctly
curl https://your-api-url/api/health | jq '.environment'

# Test referral link generation (should return correct URLs)
curl -X POST https://your-api-url/api/referral/generate \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"deliveryMethod": 1, "phoneNumbers": ["05321111121"]}' \
  | jq '.data'
```

**Expected Results:**
- Development: `https://localhost:5001/ref/ZIRA-XXXXXX`
- Staging: `https://ziraai-api-sit.up.railway.app/ref/ZIRA-XXXXXX`
- Production: `https://ziraai.com/ref/ZIRA-XXXXXX`

### Common Issues

#### Issue: Wrong URL in Response
**Cause:** `ASPNETCORE_ENVIRONMENT` not set or incorrect `appsettings.{Environment}.json`

**Solution:**
1. Verify `ASPNETCORE_ENVIRONMENT` in Railway dashboard
2. Check `appsettings.Staging.json` or `appsettings.Production.json` exists
3. Verify environment variables override correctly

#### Issue: Database Connection Failed
**Cause:** Connection string format incorrect or Railway variable not mapped

**Solution:**
1. Check Railway PostgreSQL plugin provides `DATABASE_URL`
2. Verify Program.cs maps `DATABASE_URL` to `ConnectionStrings__DArchPgContext`
3. Test connection string format is correct for Npgsql

#### Issue: Redis Connection Failed
**Cause:** SSL/TLS configuration mismatch

**Solution:**
1. Set `CacheOptions__Ssl=true` for Railway Redis
2. Verify Redis password is correct
3. Check Railway Redis plugin provides all required variables

---

## Security Best Practices

1. **Rotate Secrets Regularly:**
   - JWT keys every 90 days
   - Database passwords every 180 days
   - API keys when team members leave

2. **Different Secrets Per Environment:**
   - Never reuse JWT keys across environments
   - Different passwords for each database
   - Unique API tokens per environment

3. **Strong Password Policy:**
   - Minimum 32 characters for JWT keys
   - Minimum 64 characters recommended for production
   - Use cryptographically secure random generators
   - Include special characters, numbers, uppercase, lowercase

4. **Never Commit Secrets:**
   - All production values in Railway environment variables
   - `.gitignore` includes `appsettings.json` and `.env` files
   - Audit git history for accidentally committed secrets

5. **Principle of Least Privilege:**
   - Database users have only required permissions
   - API keys scoped to specific services
   - Worker service has separate credentials

---

## Changelog

### v1.0.0 (2025-10-07)
- Initial comprehensive documentation
- All environment variables documented
- Staging and production Railway configurations
- Security best practices added

---

## Support & Updates

**Maintained by:** Backend Team
**Last Review:** 2025-10-07
**Next Review:** When adding new environment-specific configuration

**⚠️ IMPORTANT:** Keep this document updated when:
- Adding new external services
- Changing configuration keys
- Adding new environment-specific values
- Modifying security configurations
