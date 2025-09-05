# Swagger JSON Troubleshooting Runbook

## Quick Diagnosis

**Current Status**: ✅ Configuration OK | ❌ SchemaId Conflict | ✅ Controllers OK | ⚠️ Package Mismatch

---

## 🚨 **CRITICAL ISSUE IDENTIFIED**

### **Problem**: Duplicate Class Names Causing SchemaId Conflicts

**Symptom**: `500 Internal Server Error` on `/swagger/v1/swagger.json`  
**Error**: `"An item with the same key has already been added. Key: 403"`  
**Root Cause**: Multiple classes with same name `SponsorshipDashboard` in different namespaces

**Conflicting Classes**:
1. `Entities.Dtos.SponsorshipDashboard` (Line 6 in AnalyticsDtos.cs)
2. `Business.Services.Analytics.SponsorshipDashboard` (Line 19 in ISponsorshipAnalyticsService.cs)

---

## 📋 **Step-by-Step Fix Plan**

### **STEP 1: Fix SchemaId Conflicts** ⭐ **PRIORITY 1**

**Check**: Do you have duplicate class names across namespaces?
```bash
rg "public class.*Dashboard" . --type cs
```

**Fix**: Add CustomSchemaIds configuration to `Startup.cs`

```csharp
// In ConfigureServices method, modify AddSwaggerGen():
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "ZiraAI API", 
        Version = "v1" 
    });
    
    // FIX: Use full type name including namespace to avoid conflicts
    c.CustomSchemaIds(type => type.FullName);
    
    // Optional: Include XML documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});
```

**Alternative Fix**: Rename one of the conflicting classes
```csharp
// Option 1: Rename in Entities/Dtos/AnalyticsDtos.cs
public class AdminSponsorshipDashboard  // Instead of SponsorshipDashboard

// Option 2: Rename in Business/Services/Analytics/ISponsorshipAnalyticsService.cs  
public class SponsorshipDashboardResponse  // Instead of SponsorshipDashboard
```

### **STEP 2: Verify Swagger Configuration** ✅ **STATUS: OK**

**Check**: Startup.cs configuration
```csharp
// ✅ VERIFIED: These are correctly configured in your project
services.AddSwaggerGen();  // Line 104 in Startup.cs
app.UseSwagger();          // Line 181 in Startup.cs  
app.UseSwaggerUI();        // Lines 183-187 in Startup.cs
```

### **STEP 3: Check Missing Service Registrations** ⚠️ **ISSUE FOUND**

**Check**: Are all controller dependencies registered in DI?
```bash
rg "ISponsorshipAnalyticsService" . --type cs
```

**Problem Found**: `AnalyticsController` depends on `ISponsorshipAnalyticsService` but implementation is `.exclude`d

**Fix Options**:
1. **Temporary**: Exclude controller (already done)
   ```bash
   mv WebAPI/Controllers/AnalyticsController.cs WebAPI/Controllers/AnalyticsController.cs.exclude
   ```

2. **Permanent**: Implement and register the service
   ```csharp
   // In Business/DependencyResolvers/AutofacBusinessModule.cs
   builder.RegisterType<SponsorshipAnalyticsService>()
          .As<ISponsorshipAnalyticsService>()
          .InstancePerLifetimeScope();
   ```

### **STEP 4: Package Version Compatibility** ⚠️ **NEEDS UPDATE**

**Current**: Swashbuckle.AspNetCore 7.2.0 + .NET 9.0  
**Issue**: Version mismatch may cause compatibility issues

**Fix**: Update to compatible version
```xml
<!-- In WebAPI/WebAPI.csproj -->
<PackageReference Include="Swashbuckle.AspNetCore" Version="7.3.0" />
```

**Update Command**:
```bash
dotnet add WebAPI package Swashbuckle.AspNetCore --version 7.3.0
```

### **STEP 5: Controller Method Attributes** ✅ **STATUS: OK**

**Verified**: All controller methods have proper HTTP attributes (`[HttpGet]`, `[HttpPost]`, etc.)

---

## 🧪 **Smoke Test Checklist**

### **Test 1: Swagger JSON Generation**
```bash
curl -f -H "Accept: application/json" \
  https://localhost:5001/swagger/v1/swagger.json

# Expected: HTTP 200, Content-Type: application/json
# Body contains: "openapi": "3.0.1"
```

### **Test 2: Swagger UI Access**
```bash
curl -f https://localhost:5001/swagger/index.html

# Expected: HTTP 200, Content-Type: text/html
# Body contains: "Swagger UI"
```

### **Test 3: API Health Check**
```bash
curl -f https://localhost:5001/health

# Expected: HTTP 200, JSON response with status
```

### **Test 4: Schema Validation**
```bash
# Validate the generated schema
curl -s https://localhost:5001/swagger/v1/swagger.json | \
  jq '.components.schemas | keys | length'

# Expected: Number > 0 (should return count of schemas)
```

---

## 🔄 **Implementation Priority**

1. **🔥 IMMEDIATE** (Blocks Swagger): Fix SchemaId conflicts
2. **📋 HIGH** (Missing functionality): Register Analytics service  
3. **🔧 MEDIUM** (Compatibility): Update Swashbuckle package
4. **✅ LOW** (Monitoring): Set up health checks

---

## 📝 **Code Examples**

### **Fixed Startup.cs - ConfigureServices Method**
```csharp
public override void ConfigureServices(IServiceCollection services)
{
    // ... existing configuration ...
    
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo 
        { 
            Title = "ZiraAI API", 
            Version = "v1",
            Description = "ZiraAI Plant Analysis API"
        });
        
        // CRITICAL FIX: Prevent schema conflicts
        c.CustomSchemaIds(type => type.FullName);
        
        // Enhanced error handling
        c.OperationFilter<SwaggerDefaultValues>();
        c.DocumentFilter<SwaggerExcludeFilter>();
        
        // Include XML documentation if available
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });
    
    // ... rest of configuration ...
}
```

### **Sample Controller with Proper Attributes**
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class SampleController : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetData()
    {
        // Implementation
    }
    
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateData([FromBody] CreateDataRequest request)
    {
        // Implementation
    }
}
```

---

## 🚀 **Quick Recovery Steps**

If Swagger is completely broken:

1. **Emergency Fix**: Exclude problematic controllers
   ```bash
   find WebAPI/Controllers -name "*.cs" -exec mv {} {}.exclude \;
   # Keep only essential controllers
   ```

2. **Minimal Configuration**: Strip down to basics
   ```csharp
   services.AddSwaggerGen(); // No custom configuration
   ```

3. **Gradual Restoration**: Add controllers back one by one
   ```bash
   mv WebAPI/Controllers/HealthController.cs.exclude WebAPI/Controllers/HealthController.cs
   # Test, then add next controller
   ```

---

## 📊 **Success Metrics**

- ✅ `/swagger/v1/swagger.json` returns 200 OK
- ✅ Swagger UI loads without errors  
- ✅ All schemas generate without conflicts
- ✅ API documentation is complete and accurate
- ✅ No 500 errors in application logs

---

**Last Updated**: 2025-01-24  
**Environment**: .NET 9.0, Swashbuckle.AspNetCore 7.2.0  
**Status**: SchemaId conflict identified and fix provided