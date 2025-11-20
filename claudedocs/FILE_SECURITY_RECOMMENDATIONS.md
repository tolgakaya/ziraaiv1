# File Security Recommendations for Voice Messages & Attachments

**Date:** 2025-01-20
**Status:** 🔴 CRITICAL - Current Implementation is Insecure
**Priority:** HIGH - Should be addressed before production

## 🔴 Current Security Problem

### Vulnerable Implementation

```
URL: https://ziraai-api-sit.up.railway.app/uploads/voice-messages/voice_msg_165_638965887035374118_1760991903.m4a
```

**Security Risks:**

1. ❌ **No Authorization** - Anyone with URL can access the file
2. ❌ **Predictable URLs** - Pattern: `voice_msg_{userId}_{timestamp}_{random}.m4a`
3. ❌ **No Expiration** - URLs work forever
4. ❌ **Cross-User Access** - User A can access User B's voice messages
5. ❌ **No Audit Trail** - No logging of file access
6. ❌ **Information Leakage** - UserId exposed in filename

### Attack Scenarios

**Scenario 1: URL Guessing**
```
Attacker knows:
- UserId: 165 (from API responses)
- Timestamp pattern: Unix timestamp
- Can iterate through recent timestamps

Result: Download all voice messages from user 165
```

**Scenario 2: URL Sharing**
```
User shares conversation screenshot with voice message URL
Anyone with the URL can:
- Download and listen
- Share further
- No way to revoke access
```

**Scenario 3: Data Mining**
```
Crawl /uploads directory structure
Extract all voice messages
Analyze conversations without authentication
```

---

## ✅ Recommended Solutions

### 🥇 Solution 1: Signed URLs (Quick Win - Recommended for Immediate Implementation)

**Implementation Time:** 1-2 hours
**Complexity:** Low
**Security Level:** Medium-High

#### How It Works

```
Normal URL (insecure):
/uploads/voice-messages/file.m4a

Signed URL (secure):
/uploads/voice-messages/file.m4a?sig=abc123def456&exp=1737382800
```

**Flow:**
1. User requests voice message
2. Backend generates signed URL with expiration
3. URL includes HMAC signature
4. Middleware validates signature before serving file
5. URL expires after 5-15 minutes

#### Implementation

**Step 1: Create Signed URL Service**

```csharp
// Business/Services/FileStorage/SignedUrlService.cs
using System.Security.Cryptography;
using System.Text;

public interface ISignedUrlService
{
    string SignUrl(string url, int expiresInMinutes = 15);
    bool ValidateSignature(string path, string signature, long expires);
}

public class SignedUrlService : ISignedUrlService
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;

    public SignedUrlService(IConfiguration configuration)
    {
        _configuration = configuration;
        _secretKey = _configuration["FileStorage:SignatureSecret"]
            ?? throw new InvalidOperationException("FileStorage:SignatureSecret not configured");
    }

    public string SignUrl(string url, int expiresInMinutes = 15)
    {
        // Remove any existing query parameters
        var baseUrl = url.Split('?')[0];

        // Calculate expiration timestamp
        var expires = DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes).ToUnixTimeSeconds();

        // Generate HMAC signature
        var signature = ComputeHMAC($"{baseUrl}:{expires}");

        // Return signed URL
        return $"{baseUrl}?sig={signature}&exp={expires}";
    }

    public bool ValidateSignature(string path, string signature, long expires)
    {
        // Check expiration
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expires)
            return false;

        // Validate signature
        var expectedSignature = ComputeHMAC($"{path}:{expires}");
        return signature == expectedSignature;
    }

    private string ComputeHMAC(string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
```

**Step 2: Add Middleware**

```csharp
// WebAPI/Middleware/SignedUrlMiddleware.cs
public class SignedUrlMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SignedUrlMiddleware> _logger;

    public SignedUrlMiddleware(RequestDelegate next, ILogger<SignedUrlMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISignedUrlService signedUrlService)
    {
        // Only check /uploads paths (voice messages, attachments, etc.)
        if (context.Request.Path.StartsWithSegments("/uploads"))
        {
            var signature = context.Request.Query["sig"].ToString();
            var expiresStr = context.Request.Query["exp"].ToString();

            // Check if signature parameters exist
            if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(expiresStr))
            {
                _logger.LogWarning("Unsigned URL access attempt: {Path}", context.Request.Path);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "Signature required" });
                return;
            }

            // Validate signature
            if (!long.TryParse(expiresStr, out var expires))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid expiration" });
                return;
            }

            if (!signedUrlService.ValidateSignature(context.Request.Path, signature, expires))
            {
                _logger.LogWarning("Invalid signature for path: {Path}", context.Request.Path);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired signature" });
                return;
            }

            _logger.LogInformation("Valid signed URL access: {Path}", context.Request.Path);
        }

        await _next(context);
    }
}
```

**Step 3: Register in Startup**

```csharp
// WebAPI/Startup.cs

// In ConfigureServices
services.AddSingleton<ISignedUrlService, SignedUrlService>();

// In Configure (BEFORE app.UseStaticFiles!)
app.UseMiddleware<SignedUrlMiddleware>();
app.UseStaticFiles();
```

**Step 4: Update LocalFileStorageService**

```csharp
// Business/Services/FileStorage/LocalFileStorageService.cs

private readonly ISignedUrlService _signedUrlService;

public LocalFileStorageService(
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor,
    ILogger<LocalFileStorageService> logger,
    ISignedUrlService signedUrlService)  // ✅ Add dependency
{
    _configuration = configuration;
    _httpContextAccessor = httpContextAccessor;
    _logger = logger;
    _signedUrlService = signedUrlService;

    // ... existing code
}

private string GeneratePublicUrl(string filePath)
{
    // Convert backslashes to forward slashes for URLs
    var urlPath = filePath.Replace('\\', '/');

    // Remove leading slash if present
    if (urlPath.StartsWith("/"))
        urlPath = urlPath.Substring(1);

    // Get current base URL (dynamic)
    var currentBaseUrl = GetBaseUrl();

    // Generate base URL with uploads prefix
    var baseUrl = $"{currentBaseUrl}/uploads/{urlPath}";

    // ✅ Sign the URL with 15 minute expiration
    return _signedUrlService.SignUrl(baseUrl, expiresInMinutes: 15);
}
```

**Step 5: Configuration**

```json
// appsettings.json
{
  "FileStorage": {
    "SignatureSecret": "CHANGE_THIS_TO_RANDOM_SECRET_KEY_MIN_32_CHARS"
  }
}
```

**⚠️ IMPORTANT:** Generate a strong random secret:
```bash
openssl rand -base64 32
```

#### Pros & Cons

**Advantages:**
- ✅ Quick implementation (1-2 hours)
- ✅ URLs expire automatically
- ✅ No database changes needed
- ✅ Works with existing static file serving
- ✅ CDN compatible
- ✅ Minimal performance impact

**Disadvantages:**
- ❌ No fine-grained authorization (can't check if user is message participant)
- ❌ No access logging/analytics
- ❌ URL can be shared within expiration window

**Best For:**
- Immediate security improvement
- MVP/Early stage
- High-traffic scenarios (CDN-friendly)

---

### 🥈 Solution 2: Controller-Based File Serving (Long-term Recommended)

**Implementation Time:** 4-6 hours
**Complexity:** Medium
**Security Level:** High

#### How It Works

```
Old URL: /uploads/voice-messages/file.m4a
New URL: /api/v1/files/voice-messages/{messageId}
         ↓
         Authorization check (user is sender or receiver)
         ↓
         Serve file with PhysicalFileResult
```

**Flow:**
1. User requests `/api/v1/files/voice-messages/123`
2. Controller validates JWT token
3. Controller checks if user is sender OR receiver of message #123
4. If authorized, serves file from disk
5. Logs access for audit trail

#### Implementation

**Step 1: Create Files Controller**

```csharp
// WebAPI/Controllers/FilesController.cs
[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/files")]
public class FilesController : BaseApiController
{
    private readonly IAnalysisMessageRepository _messageRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FilesController> _logger;
    private readonly string _basePath;

    public FilesController(
        IAnalysisMessageRepository messageRepository,
        IConfiguration configuration,
        ILogger<FilesController> logger)
    {
        _messageRepository = messageRepository;
        _configuration = configuration;
        _logger = logger;
        _basePath = _configuration["FileStorage:Local:BasePath"] ?? "wwwroot/uploads";
    }

    /// <summary>
    /// Get voice message file (authorization required)
    /// Only sender and receiver can access
    /// </summary>
    [HttpGet("voice-messages/{messageId}")]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetVoiceMessage(int messageId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        // Get message
        var message = await _messageRepository.GetAsync(m => m.Id == messageId);
        if (message == null)
        {
            _logger.LogWarning("Voice message not found: {MessageId}", messageId);
            return NotFound(new ErrorResult("Voice message not found"));
        }

        // Authorization: Only sender or receiver can access
        if (message.FromUserId != userId.Value && message.ToUserId != userId.Value)
        {
            _logger.LogWarning(
                "Unauthorized voice message access attempt. User: {UserId}, Message: {MessageId}, Owner: {OwnerId}",
                userId.Value, messageId, message.FromUserId);
            return Forbid();
        }

        // Get file path from URL
        if (string.IsNullOrEmpty(message.VoiceUrl))
            return NotFound(new ErrorResult("Voice file not found"));

        var filePath = ExtractFilePathFromUrl(message.VoiceUrl);
        var fullPath = Path.Combine(_basePath, filePath);

        if (!System.IO.File.Exists(fullPath))
        {
            _logger.LogError("Voice file missing on disk: {FilePath}", fullPath);
            return NotFound(new ErrorResult("Voice file not found"));
        }

        // Log access for audit
        _logger.LogInformation(
            "Voice message accessed. User: {UserId}, Message: {MessageId}, File: {FileName}",
            userId.Value, messageId, Path.GetFileName(fullPath));

        // Serve file with range support (for audio seeking)
        return PhysicalFile(fullPath, "audio/m4a", enableRangeProcessing: true);
    }

    /// <summary>
    /// Get attachment file (authorization required)
    /// Only sender and receiver can access
    /// </summary>
    [HttpGet("attachments/{messageId}/{attachmentIndex}")]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAttachment(int messageId, int attachmentIndex)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        // Get message
        var message = await _messageRepository.GetAsync(m => m.Id == messageId);
        if (message == null)
            return NotFound(new ErrorResult("Message not found"));

        // Authorization check
        if (message.FromUserId != userId.Value && message.ToUserId != userId.Value)
        {
            _logger.LogWarning(
                "Unauthorized attachment access attempt. User: {UserId}, Message: {MessageId}",
                userId.Value, messageId);
            return Forbid();
        }

        // Get attachment URL
        if (message.AttachmentUrls == null || attachmentIndex >= message.AttachmentUrls.Count)
            return NotFound(new ErrorResult("Attachment not found"));

        var attachmentUrl = message.AttachmentUrls[attachmentIndex];
        var filePath = ExtractFilePathFromUrl(attachmentUrl);
        var fullPath = Path.Combine(_basePath, filePath);

        if (!System.IO.File.Exists(fullPath))
            return NotFound(new ErrorResult("Attachment file not found"));

        // Determine content type from extension
        var contentType = GetContentType(fullPath);

        _logger.LogInformation(
            "Attachment accessed. User: {UserId}, Message: {MessageId}, Index: {Index}",
            userId.Value, messageId, attachmentIndex);

        return PhysicalFile(fullPath, contentType, enableRangeProcessing: true);
    }

    private string ExtractFilePathFromUrl(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl))
            return string.Empty;

        // If it's already a relative path, return as is
        if (!fileUrl.StartsWith("http"))
            return fileUrl;

        // Extract path from URL
        var uri = new Uri(fileUrl);
        var path = uri.AbsolutePath;

        // Remove leading slash and "uploads" prefix
        if (path.StartsWith("/"))
            path = path.Substring(1);
        if (path.StartsWith("uploads/"))
            path = path.Substring(8);

        return path;
    }

    private string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".m4a" => "audio/m4a",
            ".mp3" => "audio/mpeg",
            ".aac" => "audio/aac",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }
}
```

**Step 2: Update URL Generation**

```csharp
// Business/Handlers/AnalysisMessages/Commands/SendVoiceMessageCommand.cs

// Instead of storing physical file URL:
// OLD: message.VoiceUrl = voiceUrl; // https://host/uploads/voice-messages/file.m4a

// Store API endpoint URL:
// Create message first to get ID
var message = new AnalysisMessage
{
    PlantAnalysisId = request.PlantAnalysisId,
    FromUserId = request.FromUserId,
    ToUserId = request.ToUserId,
    MessageType = MessageType.Voice,
    Duration = request.Duration,
    Waveform = request.Waveform,
    SentAt = DateTime.Now,
    IsRead = false
};

await _messageRepository.AddAsync(message);
await _messageRepository.SaveChangesAsync(); // Get the ID

// Upload file (stores physically on disk, returns physical URL)
var physicalUrl = await _localFileStorage.UploadFileAsync(...);

// Generate API endpoint URL
var baseUrl = GetBaseUrl(); // From HttpContext
message.VoiceUrl = $"{baseUrl}/api/v1/files/voice-messages/{message.Id}";

// Also store physical path in new column for internal use
message.VoiceFilePath = physicalUrl; // Keep for file serving

await _messageRepository.SaveChangesAsync();
```

**Step 3: Database Migration (Optional - for storing physical path)**

```csharp
// Add migration for VoiceFilePath column
public partial class AddVoiceFilePath : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "VoiceFilePath",
            table: "AnalysisMessages",
            nullable: true);
    }
}
```

#### Pros & Cons

**Advantages:**
- ✅ Full authorization control (message participants only)
- ✅ Access logging and audit trail
- ✅ Analytics (most accessed files, download counts)
- ✅ Can revoke access by changing permissions
- ✅ No URL guessing possible
- ✅ Works with complex permission scenarios

**Disadvantages:**
- ❌ More implementation time
- ❌ Database schema changes needed
- ❌ Higher server load (every file goes through controller)
- ❌ Not CDN-friendly (requires authentication)
- ❌ Range requests need special handling

**Best For:**
- Production systems with strict security requirements
- Compliance scenarios (HIPAA, GDPR)
- Enterprise applications
- Long-term maintainability

---

### 🥉 Solution 3: Pre-Signed URLs (AWS S3-style)

**Implementation Time:** 6-8 hours
**Complexity:** High
**Security Level:** High

#### How It Works

Similar to Solution 1, but uses encrypted tokens containing file metadata:

```
URL: /api/v1/secure-files/{encrypted-token}

Token contains (encrypted):
{
  "fileKey": "voice-messages/file.m4a",
  "messageId": 123,
  "userId": 165,
  "expires": 1737382800
}
```

#### Implementation Overview

```csharp
public class SecureFileUrlService
{
    public string GenerateSecureUrl(int messageId, string fileKey, int expiresInMinutes = 15)
    {
        var data = new SecureFileToken
        {
            MessageId = messageId,
            FileKey = fileKey,
            UserId = GetCurrentUserId(),
            Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes)
        };

        var json = JsonSerializer.Serialize(data);
        var encrypted = Encrypt(json, _secretKey);
        var token = Base64UrlEncode(encrypted);

        return $"{_baseUrl}/api/v1/secure-files/{token}";
    }

    [HttpGet("secure-files/{token}")]
    public async Task<IActionResult> GetSecureFile(string token)
    {
        // Decrypt token
        var fileData = DecryptAndValidateToken(token);
        if (fileData == null || fileData.Expires < DateTime.UtcNow)
            return Unauthorized();

        // Verify user still has access
        var message = await _messageRepository.GetAsync(m => m.Id == fileData.MessageId);
        if (!CanUserAccessMessage(message, GetUserId()))
            return Forbid();

        // Serve file
        return PhysicalFile(fileData.FileKey, contentType);
    }
}
```

#### Pros & Cons

**Advantages:**
- ✅ Combines benefits of Solution 1 & 2
- ✅ Authorization embedded in URL
- ✅ URLs can be shared (within expiration)
- ✅ Stateless (no database lookups for validation)

**Disadvantages:**
- ❌ Complex encryption/decryption logic
- ❌ Token size (longer URLs)
- ❌ Difficult to revoke individual URLs

**Best For:**
- Transition to cloud storage (S3, Azure Blob)
- External sharing scenarios
- Mobile apps with offline support

---

### 🏆 Solution 4: Hybrid Approach (Recommended Long-term)

**Combine Solution 1 (Signed URLs) + Solution 2 (Controller)**

**Short-term (Week 1):**
- Implement Signed URLs middleware
- All existing URLs get signed
- Quick security win

**Long-term (Month 1-2):**
- Migrate to Controller-based serving
- Add access logging
- Implement analytics

**Migration path:**
```csharp
// Support both during transition
if (url.Contains("/api/v1/files/"))
{
    // New controller-based
    return await GetViaController(messageId);
}
else if (url.Contains("?sig="))
{
    // Legacy signed URL (deprecated)
    return await GetViaSignedUrl(url);
}
else
{
    // Insecure - reject
    return Unauthorized();
}
```

---

## 🛡️ Additional Security Measures

### 1. Rate Limiting

Prevent abuse of file download endpoints:

```csharp
// Install: AspNetCoreRateLimit
services.AddMemoryCache();
services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "GET:/uploads/*",
            Limit = 100,
            Period = "1m"
        },
        new RateLimitRule
        {
            Endpoint = "GET:/api/*/files/*",
            Limit = 50,
            Period = "1m"
        }
    };
});
```

### 2. File Name Obfuscation

Make filenames unpredictable:

```csharp
// Current (predictable):
var fileName = $"voice_msg_{userId}_{timestamp}_{random}.m4a";

// Better (GUID-based):
var fileName = $"{Guid.NewGuid()}.m4a";

// Best (GUID + hash):
var hash = ComputeSHA256($"{userId}:{timestamp}:{random}");
var fileName = $"{Guid.NewGuid()}_{hash.Substring(0, 8)}.m4a";
```

### 3. Audio Watermarking

Embed user info in audio file (for leak tracing):

```csharp
// Add inaudible watermark with user ID
public async Task<byte[]> WatermarkAudio(byte[] audioData, int userId, int messageId)
{
    // Use audio steganography library
    // Embed: userId + messageId + timestamp
    // If leaked, can trace back to source
}
```

### 4. CORS Configuration

Restrict file access to your mobile app only:

```csharp
services.AddCors(options =>
{
    options.AddPolicy("FileAccess", builder =>
    {
        builder
            .WithOrigins(
                "capacitor://localhost",  // iOS
                "http://localhost",        // Android
                "https://ziraai.com"       // Web
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

app.UseCors("FileAccess");
```

### 5. Referrer Policy

Prevent URL leakage in referrer headers:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Referrer-Policy", "no-referrer");
    await next();
});
```

### 6. Access Logging & Monitoring

Track all file access for security audits:

```csharp
public class FileAccessLogger
{
    public async Task LogAccess(int userId, string fileUrl, string ipAddress, bool success)
    {
        await _auditRepository.AddAsync(new FileAccessLog
        {
            UserId = userId,
            FileUrl = fileUrl,
            IpAddress = ipAddress,
            Success = success,
            AccessedAt = DateTime.UtcNow,
            UserAgent = GetUserAgent()
        });
    }
}

// Alert on suspicious patterns:
// - Same IP accessing many different users' files
// - Bulk downloads
// - Access from unusual locations
```

### 7. Content Security Policy

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add(
        "Content-Security-Policy",
        "default-src 'self'; media-src 'self' blob:; connect-src 'self'"
    );
    await next();
});
```

---

## 📊 Implementation Priority Matrix

| Solution | Security | Effort | Performance | Priority |
|----------|----------|--------|-------------|----------|
| **Signed URLs** | 🟡 Medium | 🟢 Low | 🟢 High | ⭐⭐⭐⭐⭐ |
| **Controller-based** | 🟢 High | 🟡 Medium | 🟡 Medium | ⭐⭐⭐⭐ |
| **Pre-Signed URLs** | 🟢 High | 🔴 High | 🟢 High | ⭐⭐⭐ |
| **Hybrid** | 🟢 High | 🟡 Medium | 🟢 High | ⭐⭐⭐⭐⭐ |

---

## 🎯 Action Plan

### Phase 1: Immediate (This Week)
1. ✅ Implement Signed URLs (Solution 1)
2. ✅ Add signature validation middleware
3. ✅ Update LocalFileStorageService to generate signed URLs
4. ✅ Configure secret key in appsettings
5. ✅ Test with mobile app

**Estimated Time:** 2-3 hours
**Risk Reduction:** 70%

### Phase 2: Short-term (Next 2 Weeks)
1. Add file access logging
2. Implement rate limiting
3. Add CORS policy
4. Obfuscate file names for new uploads

**Estimated Time:** 4-6 hours
**Risk Reduction:** 85%

### Phase 3: Long-term (Next 1-2 Months)
1. Migrate to Controller-based serving (Solution 2)
2. Add database schema for file tracking
3. Implement analytics dashboard
4. Consider cloud storage migration (S3/Azure)

**Estimated Time:** 2-3 days
**Risk Reduction:** 95%

---

## 🧪 Testing Checklist

### Security Tests

- [ ] **Unsigned URL Test** - Try accessing `/uploads/voice-messages/file.m4a` without signature → Should fail
- [ ] **Expired URL Test** - Generate URL, wait for expiration, try access → Should fail
- [ ] **Tampered Signature Test** - Modify signature parameter → Should fail
- [ ] **Wrong User Test** - User A tries to access User B's voice message → Should fail (Solution 2 only)
- [ ] **Rate Limit Test** - Exceed rate limit → Should throttle
- [ ] **CORS Test** - Access from unauthorized origin → Should block

### Functionality Tests

- [ ] **Voice Message Playback** - Signed URL works in mobile app
- [ ] **Audio Seeking** - Range requests work correctly
- [ ] **Attachment Download** - Images/PDFs download correctly
- [ ] **URL Refresh** - Generate new URL when old one expires
- [ ] **Offline Support** - Downloaded files cached locally

---

## 📝 Configuration Reference

### Required Settings

```json
{
  "FileStorage": {
    "Local": {
      "BasePath": "wwwroot/uploads",
      "BaseUrl": "https://ziraai-api-sit.up.railway.app"
    },
    "SignatureSecret": "CHANGE_THIS_32_CHAR_MIN_SECRET",
    "SignedUrlExpiration": 15,  // minutes
    "MaxDownloadsPerMinute": 100
  }
}
```

### Environment Variables (Production)

```bash
FILE_STORAGE__SIGNATURE_SECRET=<strong-random-secret>
FILE_STORAGE__SIGNED_URL_EXPIRATION=15
FILE_STORAGE__MAX_DOWNLOADS_PER_MINUTE=50
```

---

## 🚨 Migration Notes

### Breaking Changes

**For Mobile Team:**

When implementing Solution 1 (Signed URLs):
- ✅ No changes needed - URLs generated by backend already include signature
- ✅ Just use the URL from API response
- ⚠️ URLs expire after 15 minutes - refresh if needed

When migrating to Solution 2 (Controller-based):
- ❌ URL format changes: `/uploads/...` → `/api/v1/files/voice-messages/{id}`
- ✅ Response structure stays the same
- ⚠️ Update URL parsing logic

### Database Migration

If implementing Solution 2:

```sql
-- Add column for physical file path
ALTER TABLE AnalysisMessages
ADD COLUMN VoiceFilePath VARCHAR(500) NULL;

-- Migrate existing data
UPDATE AnalysisMessages
SET VoiceFilePath = VoiceUrl
WHERE VoiceUrl IS NOT NULL;

-- Update URLs to API endpoints
UPDATE AnalysisMessages
SET VoiceUrl = CONCAT('https://ziraai-api-sit.up.railway.app/api/v1/files/voice-messages/', Id)
WHERE VoiceUrl IS NOT NULL;
```

---

## 📚 References

- [ASP.NET Core Static Files](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/static-files)
- [HMAC Authentication](https://en.wikipedia.org/wiki/HMAC)
- [AWS S3 Pre-Signed URLs](https://docs.aws.amazon.com/AmazonS3/latest/userguide/PresignedUrlUploadObject.html)
- [OWASP File Upload Security](https://owasp.org/www-community/vulnerabilities/Unrestricted_File_Upload)

---

## 🤝 Questions & Discussion

For implementation questions or security concerns, please discuss with the team before proceeding.

**Key Decision Points:**
1. Signature expiration time (5 min vs 15 min vs 30 min)
2. Rate limiting thresholds
3. Migration timeline to controller-based serving
4. Cloud storage migration timeline

---

**Document Owner:** Backend Team
**Last Updated:** 2025-01-20
**Next Review:** After Phase 1 implementation
