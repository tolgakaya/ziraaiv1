# Swagger IFormFile Troubleshooting Guide

## Problem Overview

When adding file upload endpoints to ASP.NET Core Web API, Swagger generation fails with error:

```
SwaggerGeneratorException: Error reading parameter(s) for action as [FromForm] attribute used with IFormFile.
Please refer to https://github.com/domaindrivendev/Swashbuckle.AspNetCore#handle-forms-and-file-uploads
```

## Root Cause

The `[FromForm]` attribute combined with `IFormFile` parameters causes Swagger/Swashbuckle to fail during OpenAPI schema generation. This is a known limitation in Swashbuckle.AspNetCore.

## Solution: Step-by-Step Fix

### Step 1: Configure Swagger to Map IFormFile (REQUIRED)

Add this to `WebAPI/Startup.cs` in the `AddSwaggerGen` configuration:

```csharp
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "ZiraAI API", 
        Version = "v1",
        Description = "ZiraAI Plant Analysis API"
    });
    
    c.CustomSchemaIds(type => type.FullName);
    
    // ⭐ CRITICAL: Map IFormFile to binary schema
    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
    
    // Optional: Add operation filter for complex scenarios
    c.OperationFilter<Swagger.FileUploadOperationFilter>();
});
```

### Step 2: Remove [FromForm] Attributes (REQUIRED)

**❌ WRONG - Causes Swagger Error:**
```csharp
[HttpPost("avatar")]
[Consumes("multipart/form-data")]
public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
{
    // ...
}
```

**✅ CORRECT - Works with Swagger:**
```csharp
[HttpPost("avatar")]
[Consumes("multipart/form-data")]
public async Task<IActionResult> UploadAvatar(IFormFile file)
{
    // No [FromForm] attribute needed!
}
```

### Step 3: Use DTOs for Multiple Parameters (BEST PRACTICE)

When endpoint has multiple parameters including IFormFile, wrap them in a DTO.

**❌ WRONG - Multiple [FromForm] Parameters:**
```csharp
[HttpPost("messages/voice")]
[Consumes("multipart/form-data")]
public async Task<IActionResult> SendVoiceMessage(
    [FromForm] int toUserId,
    [FromForm] int plantAnalysisId,
    [FromForm] int duration,
    [FromForm] string waveform,
    [FromForm] IFormFile voiceFile)
{
    // This will FAIL Swagger generation
}
```

**✅ CORRECT - Use DTO:**

1. Create DTO in `Entities/Dtos/`:
```csharp
using Microsoft.AspNetCore.Http;

namespace Entities.Dtos
{
    public class SendVoiceMessageDto
    {
        public int ToUserId { get; set; }
        public int PlantAnalysisId { get; set; }
        public int Duration { get; set; }
        public string Waveform { get; set; }
        public IFormFile VoiceFile { get; set; }
    }
}
```

2. Update endpoint:
```csharp
[HttpPost("messages/voice")]
[Consumes("multipart/form-data")]
public async Task<IActionResult> SendVoiceMessage(SendVoiceMessageDto dto)
{
    // No [FromForm] attribute on DTO parameter!
    var command = new SendVoiceMessageCommand
    {
        FromUserId = userId.Value,
        ToUserId = dto.ToUserId,
        PlantAnalysisId = dto.PlantAnalysisId,
        Duration = dto.Duration,
        Waveform = dto.Waveform,
        VoiceFile = dto.VoiceFile
    };
}
```

## Complete Checklist

When adding file upload endpoints:

- [ ] Add `c.MapType<IFormFile>()` to Swagger configuration in Startup.cs
- [ ] Use `[Consumes("multipart/form-data")]` attribute on endpoint
- [ ] **DO NOT** use `[FromForm]` attribute on parameters
- [ ] For single IFormFile parameter: use directly without DTO
- [ ] For multiple parameters: create DTO class in `Entities/Dtos/`
- [ ] Test Swagger JSON generation: `https://your-api/swagger/v1/swagger.json`
- [ ] Test Swagger UI: `https://your-api/swagger`

## Why This Works

1. **[Consumes("multipart/form-data")]**: Tells ASP.NET Core this endpoint accepts form data
2. **No [FromForm]**: ASP.NET Core automatically infers binding from Consumes attribute
3. **c.MapType<IFormFile>()**: Tells Swagger how to represent IFormFile in OpenAPI schema (as binary)
4. **DTO Pattern**: Cleaner API design, better Swagger documentation, no parameter binding conflicts

## Testing the Fix

### 1. Build Locally
```bash
dotnet build Ziraai.sln
# Should succeed with 0 errors
```

### 2. Test Swagger JSON (Railway Staging)
```bash
curl https://ziraai-api-sit.up.railway.app/swagger/v1/swagger.json
# Should return valid JSON, not 500 error
```

### 3. Test Swagger UI
Visit: https://ziraai-api-sit.up.railway.app/swagger
- Should load without errors
- File upload endpoints should show file input fields
- Should display proper request body schema

## Common Mistakes to Avoid

### ❌ Mistake 1: Using [FromForm] with IFormFile
```csharp
public async Task<IActionResult> Upload([FromForm] IFormFile file)
```
**Fix**: Remove `[FromForm]`

### ❌ Mistake 2: Multiple [FromForm] Parameters with Files
```csharp
public async Task<IActionResult> Upload(
    [FromForm] int id, 
    [FromForm] IFormFile file)
```
**Fix**: Create DTO class

### ❌ Mistake 3: Forgetting MapType Configuration
```csharp
// Missing c.MapType<IFormFile>() in Startup.cs
```
**Fix**: Add MapType to AddSwaggerGen

### ❌ Mistake 4: Using [FromBody] for File Uploads
```csharp
[HttpPost]
public async Task<IActionResult> Upload([FromBody] IFormFile file)
```
**Fix**: Remove `[FromBody]`, use `[Consumes("multipart/form-data")]`

## Related Files

- `WebAPI/Startup.cs` - Swagger configuration (line ~158)
- `WebAPI/Swagger/FileUploadOperationFilter.cs` - Operation filter (optional)
- `Entities/Dtos/SendVoiceMessageDto.cs` - Example DTO for voice messages
- `Entities/Dtos/SendMessageWithAttachmentsDto.cs` - Example DTO for attachments
- `WebAPI/Controllers/UsersController.cs` - UploadAvatar endpoint (single file example)
- `WebAPI/Controllers/SponsorshipController.cs` - Complex file upload examples

## Historical Context

### Issue Timeline:
1. **Initial Issue**: Added SendVoiceMessage endpoint with multiple [FromForm] parameters
2. **Error**: Swagger JSON returned 500 error on Railway staging
3. **First Attempt**: Added operation filter → Didn't work
4. **Second Attempt**: Added MapType<IFormFile> → Still failed
5. **Root Cause**: [FromForm] attribute was the problem
6. **Solution**: Remove [FromForm], use DTOs, keep MapType configuration

### Commits:
- `dbd514e`: Added ProducesResponseType (partial fix)
- `6004c69`: Added FileUploadOperationFilter (didn't solve issue)
- `0301132`: Added MapType<IFormFile> (necessary but not sufficient)
- `6596ca8`: Refactored to use DTOs (necessary but still had [FromForm])
- `8355ebb`: **FINAL FIX** - Removed [FromForm] attributes

## Prevention Tips

### During Development:
1. **Before adding file upload endpoint**: Review this document
2. **Use DTO pattern**: Always for multiple parameters
3. **No [FromForm]**: Never use with file upload endpoints
4. **Test Swagger immediately**: After adding endpoint, check Swagger JSON

### Code Review Checklist:
```
File upload endpoint PR:
□ No [FromForm] attributes on IFormFile parameters
□ Uses DTO for multiple parameters
□ Has [Consumes("multipart/form-data")]
□ MapType<IFormFile> exists in Startup.cs
□ Tested Swagger JSON generation locally or on staging
```

### IDE Snippet (Recommended)
Create snippet for file upload endpoints:

```csharp
// File: single-file-upload.snippet
[HttpPost("$endpoint$")]
[Consumes("multipart/form-data")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> $MethodName$(IFormFile file)
{
    $END$
}

// File: multi-param-file-upload.snippet
// Step 1: Create DTO in Entities/Dtos/$DtoName$.cs
[HttpPost("$endpoint$")]
[Consumes("multipart/form-data")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> $MethodName$($DtoName$ dto)
{
    $END$
}
```

## References

- [Swashbuckle Documentation - File Uploads](https://github.com/domaindrivendev/Swashbuckle.AspNetCore#handle-forms-and-file-uploads)
- [ASP.NET Core File Upload](https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads)
- [OpenAPI File Upload Specification](https://swagger.io/docs/specification/describing-request-body/file-upload/)

## Support

If Swagger still fails after following this guide:
1. Check Railway logs for detailed error stack trace
2. Verify all three fixes are applied (MapType + No FromForm + DTO pattern)
3. Test with single file endpoint first (UploadAvatar) to isolate issue
4. Check Swashbuckle.AspNetCore version compatibility (.NET 9.0)

---

**Last Updated**: 2025-10-20  
**Tested On**: .NET 9.0, Swashbuckle.AspNetCore 6.x  
**Environment**: Railway, PostgreSQL, ZiraAI API
