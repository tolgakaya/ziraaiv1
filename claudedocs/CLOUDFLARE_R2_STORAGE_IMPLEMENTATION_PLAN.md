# Cloudflare R2 Storage Implementation Plan

## Executive Summary

Cloudflare R2 storage servisi mevcut file storage mimarisine entegre edilerek production ortamı için default storage provider olarak kullanılacaktır. Bu doküman mevcut yapının detaylı analizini ve implementasyon planını içermektedir.

---

## 1. Mevcut Mimari Analizi

### 1.1 Storage Interface Yapısı

**Dosya:** `Business/Services/FileStorage/IFileStorageService.cs`

```csharp
public interface IFileStorageService
{
    // Core upload methods
    Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string contentType, string folder = null);
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = null);
    Task<string> UploadImageFromDataUriAsync(string dataUri, string fileName, string folder = null);

    // File management
    Task<bool> DeleteFileAsync(string fileUrl);
    Task<bool> FileExistsAsync(string fileUrl);
    Task<long> GetFileSizeAsync(string fileUrl);

    // Provider metadata
    string ProviderType { get; }
    string BaseUrl { get; }
}
```

**Özellikler:**
- ✅ Tüm metotlar async
- ✅ Multiple upload overloads (byte[], Stream, DataURI)
- ✅ File management operations (delete, exists, size)
- ✅ Provider abstraction through interface

### 1.2 Mevcut Implementasyonlar

#### A. LocalFileStorageService
**Özellikler:**
- Disk-based storage (`wwwroot/uploads/`)
- Dynamic BaseUrl detection (HTTP context aware)
- HTTPS enforcement for non-localhost
- Timestamp-based unique filenames
- File sanitization

**Configuration:**
```json
"Local": {
  "BasePath": "wwwroot/uploads",
  "BaseUrl": "https://localhost:5001"
}
```

#### B. FreeImageHostStorageService
**Özellikler:**
- External API integration (freeimage.host)
- Base64 upload support
- JSON response parsing
- No delete support (free tier limitation)
- HTTP-based file existence check

**Configuration:**
```json
"FreeImageHost": {
  "ApiKey": "6d207e02198a847aa98d0a2a901485a5"
}
```

#### C. ImgBBStorageService
**Özellikler:**
- External API integration (imgbb.com)
- Base64 upload support
- Optional expiration support
- Image-only restriction
- No delete support (free tier)

**Configuration:**
```json
"ImgBB": {
  "ApiKey": "YOUR_API_KEY",
  "ExpirationSeconds": 0
}
```

### 1.3 Dependency Injection Yapısı

**Dosya:** `Business/DependencyResolvers/AutofacBusinessModule.cs` (Lines 401-427)

```csharp
// Register all implementations
builder.RegisterType<LocalFileStorageService>().InstancePerLifetimeScope();
builder.RegisterType<ImgBBStorageService>().InstancePerLifetimeScope();
builder.RegisterType<FreeImageHostStorageService>().InstancePerLifetimeScope();

// Configuration-driven provider selection
builder.Register<IFileStorageService>(c =>
{
    var context = c.Resolve<IComponentContext>();
    var config = context.Resolve<IConfiguration>();
    var provider = config["FileStorage:Provider"] ?? "Local";

    return provider switch
    {
        "FreeImageHost" => context.Resolve<FreeImageHostStorageService>(),
        "ImgBB" => context.Resolve<ImgBBStorageService>(),
        "Local" => context.Resolve<LocalFileStorageService>(),
        _ => context.Resolve<LocalFileStorageService>()
    };
}).InstancePerLifetimeScope();
```

**Worker Service:** `PlantAnalysisWorkerService/Program.cs` (Line 229)
```csharp
builder.Services.AddScoped<IFileStorageService, FreeImageHostStorageService>();
```

### 1.4 StorageProviders Constants

**Dosya:** `Business/Services/FileStorage/IFileStorageService.cs` (Lines 90-98)

```csharp
public static class StorageProviders
{
    public const string Local = "Local";
    public const string S3 = "S3";
    public const string ImgBB = "ImgBB";
    public const string FreeImageHost = "FreeImageHost";
    public const string Azure = "Azure";
    public const string GoogleCloud = "GoogleCloud";
}
```

**Not:** R2 için yeni constant eklenecek

### 1.5 Configuration Structure (Environment-based)

#### Development
```json
"FileStorage": {
  "Provider": "FreeImageHost",  // API testing için ücretsiz
  "FreeImageHost": { "ApiKey": "..." },
  "Local": { "BasePath": "wwwroot/uploads", "BaseUrl": "https://localhost:5001" }
}
```

#### Staging
```json
"FileStorage": {
  "Provider": "FreeImageHost",
  "S3": {
    "BucketName": "ziraai-staging-images",
    "Region": "us-east-1"
  }
}
```

#### Production
```json
"FileStorage": {
  "Provider": "FreeImageHost",  // 🎯 CloudflareR2 olacak
  "FreeImageHost": { "ApiKey": "${FREEIMAGEHOST_API_KEY}" }
}
```

### 1.6 Usage Analysis

**IFileStorageService kullanan servisler:**

1. **PlantAnalysisService** - Sync plant analysis image upload
2. **PlantAnalysisAsyncService** - Async plant analysis image upload
3. **PlantAnalysisMultiImageAsyncService** - Multi-image analysis upload
4. **SponsorLogoService** - Sponsor logo/avatar upload
5. **AvatarService** - User avatar upload
6. **SendMessageWithAttachmentCommand** - Message attachment upload
7. **PlantAnalysisJobService** (Worker) - Worker service image processing

**Upload Pattern:**
```csharp
var imageUrl = await _fileStorageService.UploadImageFromDataUriAsync(
    dataUri: request.Image,
    fileName: $"plant_analysis_{Guid.NewGuid():N}",
    folder: "plant-images"
);
```

---

## 2. Cloudflare R2 Özellikleri

### 2.1 R2 vs S3 Karşılaştırması

| Özellik | AWS S3 | Cloudflare R2 |
|---------|--------|---------------|
| **API Compatibility** | Native S3 API | S3-compatible API ✅ |
| **Egress Fees** | $0.09/GB | $0 (FREE) ✅ |
| **Storage Cost** | $0.023/GB/month | $0.015/GB/month ✅ |
| **Operations** | PUT: $0.005/1K | PUT: $0.0045/1K |
| **CDN Integration** | CloudFront (extra cost) | Built-in (FREE) ✅ |
| **Regions** | Multiple regions | Global ✅ |
| **Authentication** | AWS credentials | R2 API tokens |

**Avantajlar:**
- ✅ S3-compatible API (AWSSDK.S3 kullanılabilir)
- ✅ Zero egress fees (büyük maliyet tasarrufu)
- ✅ Built-in CDN
- ✅ Global edge locations
- ✅ Cheaper storage

### 2.2 R2 Authentication

```csharp
// R2 Credentials
{
  "AccountId": "your-cloudflare-account-id",
  "AccessKeyId": "r2-access-key-id",
  "SecretAccessKey": "r2-secret-access-key",
  "BucketName": "ziraai-production-images",
  "Endpoint": "https://{account-id}.r2.cloudflarestorage.com"
}
```

**Endpoint Format:**
- API Endpoint: `https://{account-id}.r2.cloudflarestorage.com`
- Public URL: `https://{bucket}.{account-id}.r2.cloudflarestorage.com/{key}`
- Custom Domain: `https://cdn.ziraai.com/{key}` (opsiyonel)

---

## 3. Implementation Plan

### Phase 1: CloudflareR2StorageService Implementation

#### 3.1 NuGet Package
```xml
<PackageReference Include="AWSSDK.S3" Version="3.7.x" />
```
**Not:** R2, S3-compatible olduğu için AWS SDK kullanılır.

#### 3.2 Service Class Structure

**Dosya:** `Business/Services/FileStorage/CloudflareR2StorageService.cs`

```csharp
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Business.Services.FileStorage
{
    /// <summary>
    /// Cloudflare R2 storage service implementation using S3-compatible API
    /// Zero egress fees, built-in CDN, global distribution
    /// </summary>
    public class CloudflareR2StorageService : IFileStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CloudflareR2StorageService> _logger;
        private readonly string _bucketName;
        private readonly string _publicDomain;

        public string ProviderType => StorageProviders.CloudflareR2;
        public string BaseUrl => _publicDomain;

        public CloudflareR2StorageService(
            IConfiguration configuration,
            ILogger<CloudflareR2StorageService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Read R2 configuration
            var accountId = _configuration["FileStorage:CloudflareR2:AccountId"];
            var accessKeyId = _configuration["FileStorage:CloudflareR2:AccessKeyId"];
            var secretAccessKey = _configuration["FileStorage:CloudflareR2:SecretAccessKey"];
            _bucketName = _configuration["FileStorage:CloudflareR2:BucketName"];

            // Public URL configuration (R2 auto-generated or custom domain)
            _publicDomain = _configuration["FileStorage:CloudflareR2:PublicDomain"]
                ?? $"https://{_bucketName}.{accountId}.r2.cloudflarestorage.com";

            ValidateConfiguration(accountId, accessKeyId, secretAccessKey, _bucketName);

            // Initialize S3 client with R2 endpoint
            var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
                ForcePathStyle = false,
                SignatureVersion = "4"
            };

            _s3Client = new AmazonS3Client(credentials, config);
            _logger.LogInformation($"[CloudflareR2] Initialized with bucket: {_bucketName}");
        }

        public async Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string contentType, string folder = null)
        {
            // Implementation details below
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = null)
        {
            // Implementation details below
        }

        public async Task<string> UploadImageFromDataUriAsync(string dataUri, string fileName, string folder = null)
        {
            // Implementation details below
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            // Implementation details below
        }

        public async Task<bool> FileExistsAsync(string fileUrl)
        {
            // Implementation details below
        }

        public async Task<long> GetFileSizeAsync(string fileUrl)
        {
            // Implementation details below
        }

        private void ValidateConfiguration(string accountId, string accessKeyId, string secretAccessKey, string bucketName)
        {
            if (string.IsNullOrEmpty(accountId))
                throw new InvalidOperationException("CloudflareR2:AccountId is required");
            if (string.IsNullOrEmpty(accessKeyId))
                throw new InvalidOperationException("CloudflareR2:AccessKeyId is required");
            if (string.IsNullOrEmpty(secretAccessKey))
                throw new InvalidOperationException("CloudflareR2:SecretAccessKey is required");
            if (string.IsNullOrEmpty(bucketName))
                throw new InvalidOperationException("CloudflareR2:BucketName is required");
        }

        private string GenerateS3Key(string fileName, string folder = null)
        {
            // Timestamp-based unique key generation
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var uniqueFileName = $"{nameWithoutExtension}_{timestamp}{extension}";

            return !string.IsNullOrEmpty(folder)
                ? $"{folder}/{uniqueFileName}".Replace("\\", "/")
                : uniqueFileName;
        }

        private string GeneratePublicUrl(string s3Key)
        {
            // Return public URL (R2 auto-generated or custom domain)
            return $"{_publicDomain}/{s3Key}";
        }

        private string ExtractS3KeyFromUrl(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return string.Empty;

            if (!fileUrl.StartsWith("http"))
                return fileUrl;

            var uri = new Uri(fileUrl);
            return uri.AbsolutePath.TrimStart('/');
        }

        private string ExtractMimeType(string dataUriPrefix)
        {
            var parts = dataUriPrefix.Split(':')[1].Split(';')[0];
            return parts;
        }

        private string GetExtensionFromMimeType(string mimeType)
        {
            return mimeType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                "image/svg+xml" => ".svg",
                "image/tiff" => ".tiff",
                _ => ".jpg"
            };
        }
    }
}
```

#### 3.3 Method Implementations

##### UploadFileAsync (byte[])
```csharp
public async Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string contentType, string folder = null)
{
    try
    {
        var s3Key = GenerateS3Key(fileName, folder);

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = s3Key,
            InputStream = new MemoryStream(fileBytes),
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead,
            Metadata =
            {
                ["x-amz-meta-original-filename"] = fileName,
                ["x-amz-meta-upload-timestamp"] = DateTime.UtcNow.ToString("O")
            }
        };

        var response = await _s3Client.PutObjectAsync(putRequest);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"R2 upload failed: {response.HttpStatusCode}");
        }

        var publicUrl = GeneratePublicUrl(s3Key);
        _logger.LogInformation($"[CloudflareR2] File uploaded: {s3Key} -> {publicUrl}");

        return publicUrl;
    }
    catch (AmazonS3Exception ex)
    {
        _logger.LogError(ex, $"[CloudflareR2] S3 error uploading {fileName}: {ex.ErrorCode}");
        throw new InvalidOperationException($"Failed to upload to Cloudflare R2: {ex.Message}", ex);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"[CloudflareR2] Failed to upload {fileName}");
        throw new InvalidOperationException($"Failed to upload to Cloudflare R2: {ex.Message}", ex);
    }
}
```

##### UploadFileAsync (Stream)
```csharp
public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = null)
{
    try
    {
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();

        return await UploadFileAsync(fileBytes, fileName, contentType, folder);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"[CloudflareR2] Failed to upload stream {fileName}");
        throw new InvalidOperationException($"Failed to upload stream to Cloudflare R2: {ex.Message}", ex);
    }
}
```

##### UploadImageFromDataUriAsync
```csharp
public async Task<string> UploadImageFromDataUriAsync(string dataUri, string fileName, string folder = null)
{
    try
    {
        if (string.IsNullOrEmpty(dataUri))
            throw new ArgumentException("Data URI is required");

        var parts = dataUri.Split(',');
        if (parts.Length != 2)
            throw new ArgumentException("Invalid data URI format");

        var mimeType = ExtractMimeType(parts[0]);
        var extension = GetExtensionFromMimeType(mimeType);
        var fileNameWithExtension = $"{fileName}{extension}";

        var base64Data = parts[1];
        var fileBytes = Convert.FromBase64String(base64Data);

        return await UploadFileAsync(fileBytes, fileNameWithExtension, mimeType, folder);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"[CloudflareR2] Failed to upload image from data URI: {fileName}");
        throw new InvalidOperationException($"Failed to upload data URI to Cloudflare R2: {ex.Message}", ex);
    }
}
```

##### DeleteFileAsync
```csharp
public async Task<bool> DeleteFileAsync(string fileUrl)
{
    try
    {
        var s3Key = ExtractS3KeyFromUrl(fileUrl);

        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = s3Key
        };

        var response = await _s3Client.DeleteObjectAsync(deleteRequest);

        _logger.LogInformation($"[CloudflareR2] File deleted: {s3Key}");
        return response.HttpStatusCode == HttpStatusCode.NoContent;
    }
    catch (AmazonS3Exception ex)
    {
        _logger.LogError(ex, $"[CloudflareR2] S3 error deleting {fileUrl}: {ex.ErrorCode}");
        return false;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"[CloudflareR2] Failed to delete {fileUrl}");
        return false;
    }
}
```

##### FileExistsAsync
```csharp
public async Task<bool> FileExistsAsync(string fileUrl)
{
    try
    {
        var s3Key = ExtractS3KeyFromUrl(fileUrl);

        var request = new GetObjectMetadataRequest
        {
            BucketName = _bucketName,
            Key = s3Key
        };

        await _s3Client.GetObjectMetadataAsync(request);
        return true;
    }
    catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        return false;
    }
    catch
    {
        return false;
    }
}
```

##### GetFileSizeAsync
```csharp
public async Task<long> GetFileSizeAsync(string fileUrl)
{
    try
    {
        var s3Key = ExtractS3KeyFromUrl(fileUrl);

        var request = new GetObjectMetadataRequest
        {
            BucketName = _bucketName,
            Key = s3Key
        };

        var response = await _s3Client.GetObjectMetadataAsync(request);
        return response.ContentLength;
    }
    catch
    {
        return -1;
    }
}
```

### Phase 2: StorageProviders Constant Update

**Dosya:** `Business/Services/FileStorage/IFileStorageService.cs`

```csharp
public static class StorageProviders
{
    public const string Local = "Local";
    public const string S3 = "S3";
    public const string ImgBB = "ImgBB";
    public const string FreeImageHost = "FreeImageHost";
    public const string Azure = "Azure";
    public const string GoogleCloud = "GoogleCloud";
    public const string CloudflareR2 = "CloudflareR2"; // 🆕 NEW
}
```

### Phase 3: Dependency Injection Update

#### 3.1 AutofacBusinessModule Update

**Dosya:** `Business/DependencyResolvers/AutofacBusinessModule.cs`

```csharp
// Register all storage implementations
builder.RegisterType<LocalFileStorageService>().InstancePerLifetimeScope();
builder.RegisterType<ImgBBStorageService>().InstancePerLifetimeScope();
builder.RegisterType<FreeImageHostStorageService>().InstancePerLifetimeScope();
builder.RegisterType<CloudflareR2StorageService>().InstancePerLifetimeScope(); // 🆕 NEW

// Configuration-driven provider selection
builder.Register<IFileStorageService>(c =>
{
    var context = c.Resolve<IComponentContext>();
    var config = context.Resolve<IConfiguration>();
    var provider = config["FileStorage:Provider"] ?? "Local";

    Console.WriteLine($"[FileStorage DI] Selected provider: {provider}");

    return provider switch
    {
        "FreeImageHost" => context.Resolve<FreeImageHostStorageService>(),
        "ImgBB" => context.Resolve<ImgBBStorageService>(),
        "Local" => context.Resolve<LocalFileStorageService>(),
        "CloudflareR2" => context.Resolve<CloudflareR2StorageService>(), // 🆕 NEW
        _ => context.Resolve<LocalFileStorageService>()
    };
}).InstancePerLifetimeScope();
```

#### 3.2 Worker Service Update

**Dosya:** `PlantAnalysisWorkerService/Program.cs`

```csharp
// Current (Line 229)
builder.Services.AddScoped<IFileStorageService, FreeImageHostStorageService>();

// 🆕 NEW: Configuration-driven (matching WebAPI pattern)
builder.Services.AddScoped<IFileStorageService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<CloudflareR2StorageService>>();
    var provider = config["FileStorage:Provider"] ?? "FreeImageHost";

    Console.WriteLine($"[Worker FileStorage] Selected provider: {provider}");

    return provider switch
    {
        "CloudflareR2" => new CloudflareR2StorageService(config, logger),
        "FreeImageHost" => sp.GetRequiredService<FreeImageHostStorageService>(),
        _ => sp.GetRequiredService<FreeImageHostStorageService>()
    };
});
```

### Phase 4: Configuration Updates

#### 4.1 Development (appsettings.Development.json)

```json
{
  "FileStorage": {
    "Provider": "FreeImageHost",
    "FreeImageHost": {
      "ApiKey": "6d207e02198a847aa98d0a2a901485a5"
    },
    "CloudflareR2": {
      "AccountId": "dev-account-id",
      "AccessKeyId": "dev-access-key",
      "SecretAccessKey": "dev-secret-key",
      "BucketName": "ziraai-dev-images",
      "PublicDomain": "https://dev-cdn.ziraai.com"
    },
    "Local": {
      "BasePath": "wwwroot/uploads",
      "BaseUrl": "https://localhost:5001"
    }
  }
}
```

#### 4.2 Staging (appsettings.Staging.json)

```json
{
  "FileStorage": {
    "Provider": "CloudflareR2",
    "CloudflareR2": {
      "AccountId": "${CLOUDFLARE_R2_ACCOUNT_ID}",
      "AccessKeyId": "${CLOUDFLARE_R2_ACCESS_KEY_ID}",
      "SecretAccessKey": "${CLOUDFLARE_R2_SECRET_ACCESS_KEY}",
      "BucketName": "ziraai-staging-images",
      "PublicDomain": "https://staging-cdn.ziraai.com"
    },
    "FreeImageHost": {
      "ApiKey": "6d207e02198a847aa98d0a2a901485a5"
    }
  }
}
```

#### 4.3 Production (appsettings.Production.json)

```json
{
  "FileStorage": {
    "Provider": "CloudflareR2",
    "CloudflareR2": {
      "AccountId": "${CLOUDFLARE_R2_ACCOUNT_ID}",
      "AccessKeyId": "${CLOUDFLARE_R2_ACCESS_KEY_ID}",
      "SecretAccessKey": "${CLOUDFLARE_R2_SECRET_ACCESS_KEY}",
      "BucketName": "ziraai-production-images",
      "PublicDomain": "https://cdn.ziraai.com"
    },
    "FreeImageHost": {
      "ApiKey": "${FREEIMAGEHOST_API_KEY}"
    }
  }
}
```

### Phase 5: Railway Environment Variables

**Railway Dashboard'da eklenecek:**

```bash
# Cloudflare R2 Credentials
CLOUDFLARE_R2_ACCOUNT_ID=your-account-id
CLOUDFLARE_R2_ACCESS_KEY_ID=r2-access-key-id
CLOUDFLARE_R2_SECRET_ACCESS_KEY=r2-secret-access-key

# Optional: Override bucket names per environment
CLOUDFLARE_R2_BUCKET_NAME=ziraai-production-images

# Optional: Custom domain (requires Cloudflare DNS configuration)
CLOUDFLARE_R2_PUBLIC_DOMAIN=https://cdn.ziraai.com
```

---

## 4. Testing Strategy

### 4.1 Unit Tests

**Dosya:** `Tests/Services/FileStorage/CloudflareR2StorageServiceTests.cs`

```csharp
[TestClass]
public class CloudflareR2StorageServiceTests
{
    [TestMethod]
    public async Task UploadImageFromDataUri_ValidInput_ReturnsPublicUrl()
    {
        // Arrange
        var service = CreateService();
        var dataUri = "data:image/jpeg;base64,/9j/4AAQSkZJRg...";
        var fileName = "test_image";

        // Act
        var result = await service.UploadImageFromDataUriAsync(dataUri, fileName, "test-folder");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StartsWith("https://"));
        Assert.IsTrue(result.Contains("test-folder"));
    }

    [TestMethod]
    public async Task DeleteFileAsync_ExistingFile_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var uploadedUrl = await UploadTestImage(service);

        // Act
        var result = await service.DeleteFileAsync(uploadedUrl);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task FileExistsAsync_ExistingFile_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var uploadedUrl = await UploadTestImage(service);

        // Act
        var exists = await service.FileExistsAsync(uploadedUrl);

        // Assert
        Assert.IsTrue(exists);
    }
}
```

### 4.2 Integration Tests

**Test Scenarios:**

1. **Upload Flow Test**
   - ✅ Single image upload via DataURI
   - ✅ Multi-image upload (5 images)
   - ✅ Large file upload (>1MB)
   - ✅ Concurrent uploads (10 simultaneous)

2. **Delete Flow Test**
   - ✅ Delete uploaded file
   - ✅ Delete non-existent file (should not throw)

3. **Metadata Test**
   - ✅ File existence check
   - ✅ File size retrieval
   - ✅ Custom metadata preservation

4. **Error Handling Test**
   - ✅ Invalid credentials
   - ✅ Non-existent bucket
   - ✅ Network timeout
   - ✅ Invalid DataURI format

### 4.3 Production Validation

**Pre-deployment Checklist:**

- [ ] R2 bucket created with correct permissions
- [ ] Railway environment variables configured
- [ ] Custom domain DNS configured (if using)
- [ ] Upload test in staging environment
- [ ] Download test from public URL
- [ ] Delete test in staging
- [ ] Performance test (10 concurrent uploads)
- [ ] Cost monitoring enabled
- [ ] Logging verification

---

## 5. Migration Strategy

### 5.1 Phased Rollout

**Phase 1: Development Testing (Week 1)**
- ✅ Implement CloudflareR2StorageService
- ✅ Local testing with R2 dev bucket
- ✅ Unit tests passing

**Phase 2: Staging Deployment (Week 1)**
- ✅ Deploy to staging with R2 as default provider
- ✅ Integration tests passing
- ✅ Performance benchmarks
- ✅ Monitor for 3 days

**Phase 3: Production Deployment (Week 2)**
- ✅ Deploy to production with R2 as default
- ✅ Monitor upload success rate
- ✅ Monitor R2 costs
- ✅ Fallback to FreeImageHost if issues

### 5.2 Fallback Strategy

**Configuration-driven fallback:**

```json
{
  "FileStorage": {
    "Provider": "CloudflareR2",
    "FallbackProvider": "FreeImageHost",
    "EnableFallback": true
  }
}
```

**Implementation:**
```csharp
public async Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string contentType, string folder = null)
{
    try
    {
        return await _primaryProvider.UploadFileAsync(fileBytes, fileName, contentType, folder);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[Storage] Primary provider failed, falling back");
        if (_enableFallback)
        {
            return await _fallbackProvider.UploadFileAsync(fileBytes, fileName, contentType, folder);
        }
        throw;
    }
}
```

### 5.3 Data Migration (Optional)

**Eski FreeImageHost resimlerini R2'ye taşıma:**

```csharp
public async Task MigrateExistingImages()
{
    // 1. Get all PlantAnalysis records with FreeImageHost URLs
    var analyses = await _plantAnalysisRepository.GetListAsync(
        a => a.ImageUrl.Contains("iili.io"));

    foreach (var analysis in analyses)
    {
        try
        {
            // 2. Download from FreeImageHost
            var imageBytes = await DownloadImageAsync(analysis.ImageUrl);

            // 3. Upload to R2
            var newUrl = await _r2Service.UploadFileAsync(
                imageBytes,
                $"migrated_{analysis.Id}.jpg",
                "image/jpeg",
                "plant-images");

            // 4. Update database
            analysis.ImageUrl = newUrl;
            await _plantAnalysisRepository.UpdateAsync(analysis);

            _logger.LogInformation($"Migrated analysis {analysis.Id} to R2");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to migrate analysis {analysis.Id}");
        }
    }
}
```

---

## 6. Cost Analysis

### 6.1 Current Costs (FreeImageHost)

- **Storage:** FREE (limited to 64MB per file)
- **Bandwidth:** FREE (rate-limited)
- **Reliability:** Medium (free tier, may have downtime)
- **Control:** Low (no delete, no versioning)

### 6.2 Projected Costs (Cloudflare R2)

**Assumptions:**
- 1,000 plant analysis uploads/month
- Average image size: 250KB (after optimization)
- 10,000 image views/month

**Monthly Costs:**
```
Storage: 1,000 images × 0.25MB = 250MB = 0.25GB
Storage cost: 0.25GB × $0.015 = $0.00375/month

PUT operations: 1,000 uploads / 1,000 = 1K operations
PUT cost: 1K × $0.0045 = $0.0045/month

GET operations: 10,000 views / 1,000 = 10K operations
GET cost: FREE (Class A reads free)

Egress: 10,000 views × 0.25MB = 2.5GB
Egress cost: FREE ✅

Total: ~$0.01/month
```

**Comparison:**
- **AWS S3:** ~$9.50/month (mostly egress: $0.09/GB × 2.5GB = $0.225 + storage)
- **Cloudflare R2:** ~$0.01/month ✅ **99% cheaper**

### 6.3 Cost Scaling (1M uploads/year)

```
Storage: 1M images × 0.25MB = 250GB
Storage cost: 250GB × $0.015 = $3.75/month

PUT operations: 1M / 1,000 = 1,000K operations
PUT cost: 1,000K × $0.0045 = $4.50/month

Egress: FREE ✅

Total: ~$8.25/month (~$100/year)
```

**vs AWS S3:** ~$950/month ($11,400/year)

---

## 7. Security Considerations

### 7.1 Bucket Access Control

**Public Read, Authenticated Write:**
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "PublicRead",
      "Effect": "Allow",
      "Principal": "*",
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::ziraai-production-images/*"
    },
    {
      "Sid": "AuthenticatedWrite",
      "Effect": "Allow",
      "Principal": {
        "AWS": "arn:aws:iam::ACCOUNT_ID:user/ziraai-api"
      },
      "Action": [
        "s3:PutObject",
        "s3:DeleteObject"
      ],
      "Resource": "arn:aws:s3:::ziraai-production-images/*"
    }
  ]
}
```

### 7.2 API Token Rotation

**Best Practices:**
- ✅ Use separate tokens per environment (dev, staging, prod)
- ✅ Rotate tokens quarterly
- ✅ Store in Railway environment variables (encrypted)
- ✅ Never commit tokens to git
- ✅ Monitor API usage for anomalies

### 7.3 CORS Configuration

```json
{
  "CORSRules": [
    {
      "AllowedOrigins": [
        "https://ziraai.com",
        "https://app.ziraai.com",
        "https://staging.ziraai.com"
      ],
      "AllowedMethods": ["GET", "HEAD"],
      "AllowedHeaders": ["*"],
      "MaxAgeSeconds": 3600
    }
  ]
}
```

---

## 8. Monitoring & Logging

### 8.1 Key Metrics

```csharp
// Upload success rate
_logger.LogInformation($"[R2_UPLOAD_SUCCESS] File: {fileName}, Size: {fileBytes.Length}, Duration: {duration}ms");

// Upload failure
_logger.LogError($"[R2_UPLOAD_FAILURE] File: {fileName}, Error: {ex.Message}");

// Performance tracking
_logger.LogWarning($"[R2_SLOW_UPLOAD] File: {fileName}, Duration: {duration}ms (threshold: 5000ms)");
```

### 8.2 Cloudflare Analytics

**Dashboard Metrics:**
- ✅ Total requests (PUT, GET, DELETE)
- ✅ Bandwidth usage (ingress, egress)
- ✅ Storage size over time
- ✅ Error rates (4xx, 5xx)
- ✅ Geographic distribution

### 8.3 Alerts

**Railway Monitoring:**
```bash
# Alert if upload failures > 5% in 1 hour
ALERT: R2 upload failure rate = 8% (threshold: 5%)

# Alert if average upload time > 10s
ALERT: R2 average upload time = 12.5s (threshold: 10s)
```

---

## 9. Implementation Checklist

### Phase 1: Development (Week 1, Days 1-3)
- [ ] Create `CloudflareR2StorageService.cs`
- [ ] Add `AWSSDK.S3` NuGet package
- [ ] Implement all interface methods
- [ ] Add `CloudflareR2` to `StorageProviders` constants
- [ ] Update `AutofacBusinessModule.cs` DI registration
- [ ] Update `PlantAnalysisWorkerService/Program.cs` DI
- [ ] Add unit tests
- [ ] Local testing with R2 dev bucket

### Phase 2: Configuration (Week 1, Day 4)
- [ ] Update `appsettings.Development.json`
- [ ] Update `appsettings.Staging.json`
- [ ] Update `appsettings.Production.json`
- [ ] Add Railway environment variables (staging)
- [ ] Test environment variable substitution

### Phase 3: Staging Deployment (Week 1, Days 5-7)
- [ ] Deploy to staging
- [ ] Run integration tests
- [ ] Performance benchmarks (100 uploads)
- [ ] Monitor for 3 days
- [ ] Review Cloudflare analytics
- [ ] Cost validation

### Phase 4: Production Deployment (Week 2, Days 1-2)
- [ ] Add Railway environment variables (production)
- [ ] Deploy to production
- [ ] Monitor upload success rate (target: >99%)
- [ ] Monitor performance (target: <5s per upload)
- [ ] Monitor R2 costs

### Phase 5: Documentation (Week 2, Day 3)
- [ ] Update API documentation
- [ ] Update deployment guide
- [ ] Create troubleshooting guide
- [ ] Create cost monitoring guide

---

## 10. Troubleshooting Guide

### Issue 1: "Access Denied" Error

**Symptoms:**
```
AmazonS3Exception: Access Denied (403)
```

**Solutions:**
1. Verify R2 API token has PutObject permission
2. Check bucket policy allows authenticated writes
3. Verify AccountId in endpoint URL is correct

### Issue 2: "Bucket Not Found" Error

**Symptoms:**
```
AmazonS3Exception: The specified bucket does not exist (404)
```

**Solutions:**
1. Verify bucket name in configuration
2. Check bucket created in correct R2 account
3. Verify endpoint URL format: `https://{accountId}.r2.cloudflarestorage.com`

### Issue 3: Slow Upload Times

**Symptoms:**
- Upload takes >10 seconds

**Solutions:**
1. Check network connectivity to Cloudflare edge
2. Verify image optimization is working (target: <250KB)
3. Consider enabling compression before upload
4. Check Railway region latency

### Issue 4: Public URLs Not Accessible

**Symptoms:**
- Upload succeeds but URL returns 404

**Solutions:**
1. Verify bucket has public read permission
2. Check CORS configuration allows origin
3. Verify PublicDomain configuration is correct
4. Test with R2 auto-generated URL first

---

## 11. Future Enhancements

### 11.1 Image Optimization Pipeline

```csharp
public async Task<string> UploadOptimizedImageAsync(byte[] imageBytes, string fileName)
{
    // 1. Optimize before upload (reduce R2 storage costs)
    var optimizedBytes = await _imageProcessingService.OptimizeImageAsync(imageBytes, maxSizeKB: 250);

    // 2. Upload optimized version to R2
    var url = await _r2Service.UploadFileAsync(optimizedBytes, fileName, "image/jpeg", "optimized");

    return url;
}
```

### 11.2 CDN Cache Control

```csharp
var putRequest = new PutObjectRequest
{
    BucketName = _bucketName,
    Key = s3Key,
    CacheControl = "public, max-age=31536000, immutable", // 1 year cache
    Expires = DateTime.UtcNow.AddYears(1)
};
```

### 11.3 Image Variants (Thumbnails)

```csharp
public async Task<ImageUploadResult> UploadWithVariantsAsync(byte[] imageBytes, string fileName)
{
    // Original
    var originalUrl = await UploadAsync(imageBytes, $"{fileName}_original.jpg", "full");

    // Thumbnail (200x200)
    var thumbnailBytes = await _imageProcessingService.ResizeAsync(imageBytes, 200, 200);
    var thumbnailUrl = await UploadAsync(thumbnailBytes, $"{fileName}_thumb.jpg", "thumbnails");

    // Medium (800x800)
    var mediumBytes = await _imageProcessingService.ResizeAsync(imageBytes, 800, 800);
    var mediumUrl = await UploadAsync(mediumBytes, $"{fileName}_medium.jpg", "medium");

    return new ImageUploadResult
    {
        OriginalUrl = originalUrl,
        ThumbnailUrl = thumbnailUrl,
        MediumUrl = mediumUrl
    };
}
```

### 11.4 Signed URLs (Private Files)

```csharp
public string GenerateSignedUrl(string s3Key, int expirationMinutes = 60)
{
    var request = new GetPreSignedUrlRequest
    {
        BucketName = _bucketName,
        Key = s3Key,
        Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
    };

    return _s3Client.GetPreSignedURL(request);
}
```

---

## 12. Rollback Plan

### Emergency Rollback (< 5 minutes)

**Step 1: Update Configuration**
```json
{
  "FileStorage": {
    "Provider": "FreeImageHost"  // Revert to previous provider
  }
}
```

**Step 2: Restart Services**
```bash
# Railway auto-deploys on config change
# Or manual restart via Railway dashboard
```

**Step 3: Verify**
- Test upload endpoint
- Check logs for errors
- Verify images accessible

### Partial Rollback (Fallback Mode)

```json
{
  "FileStorage": {
    "Provider": "CloudflareR2",
    "FallbackProvider": "FreeImageHost",
    "EnableFallback": true,
    "FallbackOnError": true
  }
}
```

---

## 13. Success Criteria

### Technical Metrics
- ✅ Upload success rate: >99%
- ✅ Average upload time: <5 seconds
- ✅ Image availability: >99.9%
- ✅ Zero data loss
- ✅ All unit tests passing
- ✅ All integration tests passing

### Business Metrics
- ✅ Monthly storage cost: <$10
- ✅ Zero egress charges
- ✅ 99% cost reduction vs S3
- ✅ Image load time: <2 seconds (global CDN)

### Operational Metrics
- ✅ Zero production incidents
- ✅ Monitoring dashboards operational
- ✅ Alerting functional
- ✅ Documentation complete

---

## 14. Conclusion

Cloudflare R2 storage servisi mevcut file storage mimarisine sorunsuz bir şekilde entegre edilebilir. S3-compatible API sayesinde mevcut AWS SDK kullanılarak implementasyon basittir. Zero egress fees ve built-in CDN özellikleri ile hem maliyet hem performans açısından ideal bir çözümdür.

**Önerilen Timeline:**
- Week 1: Implementation + Staging deployment
- Week 2: Production deployment + Monitoring

**Risk Level:** LOW (S3-compatible, proven technology, easy rollback)

**Expected Outcome:** 99% cost reduction, improved global performance, production-ready storage solution.
