# Configuration System & Image Processing Documentation

## Overview

Bu dokümantasyon, plant analysis uygulamasına eklenen dinamik konfigürasyon sistemi ve gelişmiş image processing özelliklerini açıklamaktadır. Sistem, runtime'da değiştirilebilir konfigürasyonlar ve otomatik image resizing functionality'si sağlar.

## Architecture

### 1. Configuration System Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Controller    │───▶│   Command/      │───▶│   Service       │
│                 │    │   Query Handler │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                                       │
                                               ┌─────────────────┐
                                               │   Repository    │
                                               │                 │
                                               └─────────────────┘
                                                       │
                                               ┌─────────────────┐
                                               │   Database      │
                                               │ (Configurations)│
                                               └─────────────────┘
```

### 2. Image Processing Flow

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│ Image Upload    │───▶│ Validation      │───▶│ Configuration   │
│ (Base64)        │    │ (ValidImage)    │    │ Check           │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                                       │
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│ File Storage    │◀───│ Auto Resize     │◀───│ Size Check      │
│                 │    │ (if needed)     │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## Core Components

### 1. Configuration Entity

**Location**: `Entities/Concrete/Configuration.cs`

```csharp
public class Configuration : IEntity
{
    public int Id { get; set; }
    public string Key { get; set; }        // Unique configuration key
    public string Value { get; set; }      // Configuration value
    public string Description { get; set; } // Human-readable description
    public string Category { get; set; }   // Grouping category
    public string ValueType { get; set; }  // int, decimal, bool, string
    public bool IsActive { get; set; }     // Enable/disable flag
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}
```

### 2. Configuration Service

**Location**: `Business/Services/Configuration/ConfigurationService.cs`

**Key Features**:
- Memory caching (15 dakika TTL)
- Type-safe value getters
- CRUD operations
- Category-based filtering

**Usage Examples**:
```csharp
// Get typed values with default fallback
var maxSize = await _configService.GetDecimalValueAsync("IMAGE_MAX_SIZE_MB", 50.0m);
var enableResize = await _configService.GetBoolValueAsync("IMAGE_ENABLE_AUTO_RESIZE", true);

// Get all configurations by category
var imageConfigs = await _configService.GetByCategoryAsync("ImageProcessing");
```

### 3. Image Processing Service

**Location**: `Business/Services/ImageProcessing/ImageProcessingService.cs`

**Dependencies**: SixLabors.ImageSharp 3.1.7

**Key Methods**:
- `ResizeImageIfNeededAsync()` - Configuration-based auto-resize
- `ResizeImageAsync()` - Manual resize with quality control  
- `GetImageDimensionsAsync()` - Extract width/height
- `IsImageWithinLimitsAsync()` - Validate against configured limits
- `GetImageFormatAsync()` - Detect image format

## Configuration Keys

### Image Processing Settings

| Key | Default | Type | Description |
|-----|---------|------|-------------|
| `IMAGE_MAX_SIZE_MB` | 50.0 | decimal | Maximum file size in megabytes (supports decimal values like 0.5) |
| `IMAGE_MIN_SIZE_BYTES` | 100 | int | Minimum file size in bytes |
| `IMAGE_MAX_WIDTH` | 1920 | int | Maximum image width in pixels |
| `IMAGE_MAX_HEIGHT` | 1080 | int | Maximum image height in pixels |
| `IMAGE_MIN_WIDTH` | 100 | int | Minimum image width in pixels |
| `IMAGE_MIN_HEIGHT` | 100 | int | Minimum image height in pixels |
| `IMAGE_ENABLE_AUTO_RESIZE` | true | bool | Enable automatic resizing |
| `IMAGE_RESIZE_QUALITY` | 85 | int | JPEG quality (1-100) |
| `IMAGE_SUPPORTED_FORMATS` | JPEG,PNG,GIF,WebP,BMP,SVG,TIFF | string | Comma-separated formats |
| `IMAGE_STORAGE_PATH` | wwwroot/uploads/images | string | File storage directory |

### Application Settings

| Key | Default | Type | Description |
|-----|---------|------|-------------|
| `N8N_WEBHOOK_URL` | https://your-n8n-instance.com/webhook/plant-analysis | string | N8N webhook endpoint |
| `N8N_TIMEOUT_SECONDS` | 300 | int | Request timeout in seconds |

## Database Schema

### Configurations Table

```sql
CREATE TABLE "Configurations" (
    "Id" serial PRIMARY KEY,
    "Key" varchar(100) NOT NULL UNIQUE,
    "Value" varchar(500) NOT NULL,
    "Description" varchar(1000),
    "Category" varchar(50) NOT NULL,
    "ValueType" varchar(20) NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedDate" timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedDate" timestamptz,
    "CreatedBy" integer,
    "UpdatedBy" integer
);

CREATE UNIQUE INDEX "IX_Configurations_Key" ON "Configurations" ("Key");
CREATE INDEX "IX_Configurations_Category" ON "Configurations" ("Category");
```

## API Endpoints

### Configuration Management

**Base URL**: `/api/configurations`

#### Get All Configurations
```http
GET /api/configurations
Authorization: Bearer {token}
```

#### Get Configurations by Category
```http
GET /api/configurations?category=ImageProcessing
Authorization: Bearer {token}
```

#### Get Single Configuration
```http
GET /api/configurations/{id}
Authorization: Bearer {token}
```

#### Create Configuration
```http
POST /api/configurations
Authorization: Bearer {token}
Content-Type: application/json

{
  "key": "NEW_SETTING",
  "value": "default_value",
  "description": "Setting description",
  "category": "Application",
  "valueType": "string"
}
```

#### Update Configuration
```http
PUT /api/configurations/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "id": 1,
  "value": "new_value",
  "description": "Updated description",
  "isActive": true
}
```

### Plant Analysis with Image Processing

**Endpoint**: `POST /api/plantanalyses/analyze`

**Enhanced Features**:
- Dynamic size validation based on configuration
- Automatic image resizing if enabled
- Support for multiple formats (JPEG, PNG, GIF, WebP, BMP, SVG, TIFF)
- Configuration-based quality control

**Request Example**:
```json
{
  "image": "data:image/jpeg;base64,/9j/4AAQSkZJRg...",
  "farmerId": "FARM001",
  "location": "Greenhouse A",
  "cropType": "Tomato"
}
```

## Validation System

### ValidImageAttribute

**Location**: `Core/Attributes/ValidImageAttribute.cs`

**Features**:
- Data URI format validation
- MIME type verification
- Base64 content validation
- File size limits (configurable via attribute parameter)
- Magic byte checking planned

**Usage**:
```csharp
public class PlantAnalysisRequestDto
{
    [Required(ErrorMessage = "Image is required")]
    [ValidImage(50)] // Max 50MB, configuration-based validation in service layer
    public string Image { get; set; }
}
```

## Image Processing Workflow

### 1. Upload & Validation
```csharp
// 1. Basic validation via ValidImageAttribute
[ValidImage(50)] // Attribute-level validation

// 2. Advanced validation in service layer
public async Task<PlantAnalysisResponseDto> ProcessAnalysisAsync(PlantAnalysisRequestDto request)
{
    // Decode base64 image
    var imageBytes = Convert.FromBase64String(base64Data);
    
    // Check configuration-based limits
    var isWithinLimits = await _imageProcessingService.IsImageWithinLimitsAsync(imageBytes);
    
    // Auto-resize if enabled and needed
    var processedImageBytes = await _imageProcessingService.ResizeImageIfNeededAsync(imageBytes);
    
    // Save to file system
    var imagePath = await SaveImageToFileAsync(processedImageBytes);
    
    // Continue with analysis...
}
```

### 2. Auto-Resize Logic
```csharp
public async Task<byte[]> ResizeImageIfNeededAsync(byte[] imageBytes)
{
    // Check if auto-resize is enabled
    var enableAutoResize = await _configurationService.GetBoolValueAsync(
        ConfigurationKeys.ImageProcessing.EnableAutoResize, true);

    if (!enableAutoResize) return imageBytes;

    // Get configured limits
    var maxWidth = await _configurationService.GetIntValueAsync(
        ConfigurationKeys.ImageProcessing.MaxImageWidth, 1920);
    var maxHeight = await _configurationService.GetIntValueAsync(
        ConfigurationKeys.ImageProcessing.MaxImageHeight, 1080);

    // Check if resize is needed
    var (currentWidth, currentHeight) = await GetImageDimensionsAsync(imageBytes);
    if (currentWidth <= maxWidth && currentHeight <= maxHeight)
        return imageBytes;

    // Perform resize with configured quality
    var quality = await _configurationService.GetIntValueAsync(
        ConfigurationKeys.ImageProcessing.ResizeQuality, 85);
        
    return await ResizeImageAsync(imageBytes, maxWidth, maxHeight, quality);
}
```

## Dependency Injection Setup

**Location**: `Business/DependencyResolvers/AutofacBusinessModule.cs`

```csharp
// Repository registrations
builder.RegisterType<ConfigurationRepository>().As<IConfigurationRepository>()
    .InstancePerLifetimeScope();

// Service registrations  
builder.RegisterType<ConfigurationService>().As<IConfigurationService>()
    .InstancePerLifetimeScope();
    
builder.RegisterType<ImageProcessingService>().As<IImageProcessingService>()
    .InstancePerLifetimeScope();
```

## Migration & Seeding

### Migration Files
- `20250811212114_AddConfigurationTable.cs` - Creates Configurations table
- `20250811212141_SeedConfigurationData.cs` - Inserts default configuration values

### Seed Data
**Location**: `Business/Seeds/ConfigurationSeeds.cs`

Default configurations are automatically seeded during database migration, including all image processing settings and application configurations.

## Performance Considerations

### 1. Configuration Service
- **Memory Caching**: 15 dakika TTL ile configuration değerleri cache'lenir
- **Type Safety**: Generic GetValueAsync<T> method'u ile type-safe erişim
- **Batch Operations**: Category-based filtering için optimize edilmiş queries

### 2. Image Processing
- **Lazy Resizing**: Sadece gerektiğinde resize işlemi yapılır
- **Quality Control**: Configurable JPEG quality settings
- **Memory Management**: Using blocks ile proper resource disposal
- **Format Detection**: Efficient format detection using SixLabors.ImageSharp

### 3. Database Optimization
- **Unique Index**: Configuration.Key field'da unique index
- **Category Index**: Category-based filtering için index
- **UTC Timestamps**: Timezone-aware timestamp columns

## Configuration Management Best Practices

### 1. Adding New Configuration
```csharp
// 1. Add constant to ConfigurationKeys
public static class ConfigurationKeys
{
    public static class ImageProcessing 
    {
        public const string NewSetting = "IMAGE_NEW_SETTING";
    }
}

// 2. Add seed data to ConfigurationSeeds
new Configuration
{
    Key = "IMAGE_NEW_SETTING",
    Value = "default_value",
    Description = "Description of new setting",
    Category = "ImageProcessing",
    ValueType = "string",
    IsActive = true,
    CreatedDate = DateTime.UtcNow
}

// 3. Create migration
dotnet ef migrations add AddNewConfigurationSetting --project DataAccess --startup-project WebAPI --context ProjectDbContext
```

### 2. Using Configuration in Services
```csharp
public class YourService
{
    private readonly IConfigurationService _configService;
    
    public async Task YourMethodAsync()
    {
        // Always provide sensible defaults
        var setting = await _configService.GetValueAsync<string>(
            ConfigurationKeys.ImageProcessing.NewSetting, 
            "default_value");
            
        // Use type-specific methods when possible
        var intSetting = await _configService.GetIntValueAsync(
            "SOME_INT_SETTING", 100);
            
        // For decimal image size limits
        var maxSizeMB = await _configService.GetDecimalValueAsync(
            "IMAGE_MAX_SIZE_MB", 50.0m); // Supports 0.5, 1.2, 50.0 etc.
    }
}
```

### 3. Example: Setting Custom Size Limits
```csharp
// Configure for small images (500KB max)
await _configService.UpdateAsync(new UpdateConfigurationDto 
{
    Id = 1, // IMAGE_MAX_SIZE_MB record ID
    Value = "0.5", // 500KB = 0.5MB
    IsActive = true
}, userId);

// Configure for large images (100MB max)  
await _configService.UpdateAsync(new UpdateConfigurationDto 
{
    Id = 1,
    Value = "100.0", // 100MB
    IsActive = true  
}, userId);
```

### 4. Configuration Categories

**ImageProcessing**: Image upload, validation, processing settings
**Application**: General application settings, external service URLs
**Security**: Authentication, authorization settings (future)
**Notifications**: Email, SMS, push notification settings (future)

## Error Handling

### Configuration Service Errors
- **Missing Configuration**: Returns provided default value
- **Type Conversion Errors**: Returns default value, logs warning
- **Database Connection Issues**: Falls back to default values

### Image Processing Errors
- **Invalid Format**: Throws `InvalidOperationException` with descriptive message
- **Corrupted Data**: Throws `FormatException` during base64 decode
- **Memory Issues**: Using statements ensure proper disposal

## Security Considerations

### 1. Configuration Access
- **Authentication Required**: All configuration endpoints require valid JWT token
- **Role-Based Access**: Admin role required for configuration management
- **Audit Trail**: CreatedBy/UpdatedBy fields track changes

### 2. Image Processing Security
- **File Type Validation**: Magic byte checking prevents malicious uploads
- **Size Limits**: Configurable limits prevent DoS attacks
- **Sandboxed Processing**: SixLabors.ImageSharp provides safe image processing

## Future Enhancements

### 1. Configuration UI
- Admin panel for configuration management
- Category-based configuration groups
- Configuration validation rules
- Configuration history/versioning

### 2. Image Processing Enhancements
- Watermark support
- Advanced image analytics (EXIF data extraction)
- Batch processing capabilities
- CDN integration for image delivery

### 3. Performance Improvements
- Distributed caching (Redis) for multi-instance deployments
- Background job processing for large image operations
- Image format optimization (WebP conversion)

## Troubleshooting

### Common Issues

**1. Configuration Not Found**
- Check if configuration exists in database
- Verify configuration key spelling
- Ensure configuration is active (IsActive = true)

**2. Image Processing Fails**
- Verify SixLabors.ImageSharp package is installed
- Check image format is supported
- Validate base64 data is not corrupted

**3. Performance Issues**
- Monitor cache hit ratios
- Check for memory leaks in image processing
- Optimize database queries with proper indexing

### Debugging Tips

**Enable Detailed Logging**:
```csharp
// Add to appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Business.Services.Configuration": "Debug",
      "Business.Services.ImageProcessing": "Debug"
    }
  }
}
```

**Monitor Cache Performance**:
```csharp
// Check cache statistics
var cacheStats = _memoryCache.GetType()
    .GetField("_coherentState", BindingFlags.NonPublic | BindingFlags.Instance);
```

## Conclusion

Bu configuration sistemi ve image processing functionality'si, plant analysis uygulamasına güçlü bir esneklik ve performans katmıştır. Runtime'da değiştirilebilir konfigürasyonlar, otomatik image resizing ve güçlü validation sistemleri ile uygulama artık daha ölçeklenebilir ve yönetilebilir hale gelmiştir.

Sistem, Clean Architecture prensiplerine uygun olarak tasarlanmış, SOLID ilkelerini takip etmekte ve gelecekteki genişlemelere açık bir yapı sunmaktadır.