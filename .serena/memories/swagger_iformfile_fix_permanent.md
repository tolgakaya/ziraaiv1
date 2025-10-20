# Swagger IFormFile Fix - Permanent Reference

## Critical Rule for File Upload Endpoints

**NEVER use [FromForm] attribute with IFormFile parameters in ASP.NET Core endpoints.**

## The Problem

When adding file upload endpoints, Swagger generation fails with:
```
SwaggerGeneratorException: Error reading parameter(s) for action as [FromForm] attribute used with IFormFile
```

## The Complete Solution (3 Steps)

### 1. Configure Swagger in Startup.cs (ONE TIME SETUP)

```csharp
services.AddSwaggerGen(c =>
{
    // ... other config ...
    
    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
});
```

### 2. Single File Parameter Pattern

```csharp
// ✅ CORRECT
[HttpPost("avatar")]
[Consumes("multipart/form-data")]
public async Task<IActionResult> UploadAvatar(IFormFile file)
{
    // No [FromForm] attribute!
}

// ❌ WRONG
public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
```

### 3. Multiple Parameters Pattern (Use DTO)

```csharp
// Create DTO in Entities/Dtos/
public class SendVoiceMessageDto
{
    public int ToUserId { get; set; }
    public int PlantAnalysisId { get; set; }
    public IFormFile VoiceFile { get; set; }
}

// ✅ CORRECT - Endpoint
[HttpPost("messages/voice")]
[Consumes("multipart/form-data")]
public async Task<IActionResult> SendVoiceMessage(SendVoiceMessageDto dto)
{
    // No [FromForm] on dto parameter!
}

// ❌ WRONG - Individual parameters
public async Task<IActionResult> SendVoiceMessage(
    [FromForm] int toUserId,
    [FromForm] IFormFile file)
```

## Why This Works

1. `[Consumes("multipart/form-data")]` tells ASP.NET Core to expect form data
2. ASP.NET Core automatically infers parameter binding from Consumes attribute
3. Adding `[FromForm]` is redundant and breaks Swagger's schema generator
4. `c.MapType<IFormFile>()` tells Swagger to represent files as binary in OpenAPI

## Quick Checklist for New File Upload Endpoints

- [ ] Verify `c.MapType<IFormFile>()` exists in Startup.cs AddSwaggerGen
- [ ] Use `[Consumes("multipart/form-data")]` on endpoint
- [ ] **DO NOT** add `[FromForm]` to any parameter
- [ ] Single file? Use `IFormFile file` directly
- [ ] Multiple params? Create DTO in `Entities/Dtos/`
- [ ] Test Swagger JSON: `https://your-api/swagger/v1/swagger.json`

## Common Errors to Avoid

1. ❌ `[FromForm] IFormFile file` → Remove [FromForm]
2. ❌ Multiple `[FromForm]` parameters → Use DTO pattern
3. ❌ `[FromBody] IFormFile` → Wrong binding, use Consumes attribute
4. ❌ Forgetting `c.MapType<IFormFile>()` → Add to Startup.cs

## Testing After Implementation

```bash
# 1. Build locally
dotnet build Ziraai.sln

# 2. Test Swagger JSON on staging
curl https://ziraai-api-sit.up.railway.app/swagger/v1/swagger.json
# Should return valid JSON, not 500 error

# 3. Visit Swagger UI
# https://ziraai-api-sit.up.railway.app/swagger
# Should load and show file upload fields
```

## Reference Files in Project

- `claudedocs/swagger-iformfile-troubleshooting.md` - Full detailed guide
- `WebAPI/Startup.cs` - Swagger MapType configuration (line ~158)
- `WebAPI/Controllers/UsersController.cs` - UploadAvatar (single file example)
- `WebAPI/Controllers/SponsorshipController.cs` - SendVoiceMessage (DTO example)
- `Entities/Dtos/SendVoiceMessageDto.cs` - Example DTO
- `Entities/Dtos/SendMessageWithAttachmentsDto.cs` - Example DTO

## Historical Fix (Oct 2025)

Problem encountered when adding SendVoiceMessage, SendMessageWithAttachments, and UploadAvatar endpoints.

**Root cause**: [FromForm] attribute with IFormFile parameters
**Solution**: Remove all [FromForm] attributes, use DTO pattern for multiple parameters
**Final commit**: 8355ebb - "fix: Remove [FromForm] attributes from file upload endpoints"

---

**REMEMBER**: File uploads work WITHOUT [FromForm]. Trust the [Consumes] attribute and MapType configuration.
