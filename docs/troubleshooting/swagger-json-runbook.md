# Swagger JSON Troubleshooting Runbook

## Quick Status: ✅ Configuration Analysis Complete

Based on analysis of your ZiraAI .NET 9.0 WebAPI project, here's the comprehensive troubleshooting guide:

---

## 🔧 Configuration Analysis Results

### ✅ Swagger Configuration Status
**Location**: `WebAPI/Startup.cs:103-116`
```csharp
// ✅ CORRECTLY CONFIGURED
services.AddSwaggerGen(c =>
{
    c.IncludeXmlComments(Path.ChangeExtension(typeof(Startup).Assembly.Location, ".xml"));
    c.IgnoreObsoleteActions();
    c.IgnoreObsoleteProperties();
    c.CustomSchemaIds(type => type.FullName);  // ✅ SchemaId conflicts resolved
    c.SchemaFilter<ExcludeComplexTypesSchemaFilter>();  // ✅ Custom filter applied
});
```

**Runtime Configuration**: `WebAPI/Startup.cs:167-174`
```csharp
// ✅ CORRECTLY CONFIGURED (Development only)
if (!env.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("v1/swagger.json", "DevArchitecture");
        c.DocExpansion(DocExpansion.None);
    });
}
```

### ✅ Package Version Compatibility
**Swashbuckle Version**: 7.2.0 (from WebAPI.csproj:55)
**Target Framework**: .NET 9.0
**Compatibility**: ✅ **GOOD** - Swashbuckle 7.2.0 supports .NET 9.0

### ✅ Controller Method Analysis
**Result**: All controller methods properly decorated with HTTP attributes (`[HttpGet]`, `[HttpPost]`, etc.)
**No issues found** with missing HTTP method attributes.

---

## 🚨 Step-by-Step Troubleshooting Runbook

### Symptom → Check → Fix

#### 1. **500 Internal Server Error on `/swagger/v1/swagger.json`**

**Check 1: Missing Services Configuration**
```bash
# Verify these lines exist in Startup.cs ConfigureServices():
grep -n "AddSwaggerGen\|AddEndpointsApiExplorer" WebAPI/Startup.cs
```

**Fix 1: Add Missing Services** (if needed)
```csharp
public override void ConfigureServices(IServiceCollection services)
{
    // Add this if missing:
    services.AddEndpointsApiExplorer();  // For minimal APIs
    services.AddSwaggerGen();
    // ... rest of configuration
}
```

**Check 2: Missing Pipeline Configuration**
```bash
# Verify these lines exist in Startup.cs Configure():
grep -n "UseSwagger\|UseSwaggerUI" WebAPI/Startup.cs
```

**Fix 2: Add Missing Pipeline** (if needed)
```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // Add this if missing:
    if (!env.IsProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    // ... rest of pipeline
}
```

#### 2. **SchemaId Conflicts (Duplicate Class Names)**

**Symptom**: Error like "Conflicting schemaIds: Multiple types were found with the same schemaId"

**Check**: Look for duplicate class names across namespaces
```bash
# Search for potential conflicts:
find . -name "*.cs" -exec grep -l "public class.*Dto\|public class.*Response" {} \; | head -10
```

**Fix**: Use FullName for SchemaIds ✅ **ALREADY IMPLEMENTED**
```csharp
services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName);  // ✅ Already configured
});
```

#### 3. **Controller Action Method Issues**

**Symptom**: Methods appear in Swagger that shouldn't, or methods missing HTTP attributes

**Check**: Find public methods without HTTP attributes
```bash
# Search for problematic methods:
grep -r "public.*IActionResult\|public.*Task<" WebAPI/Controllers/ | grep -v "\[Http"
```

**Fix**: Add proper attributes
```csharp
// ❌ Wrong - will cause issues:
public async Task<IActionResult> SomeMethod() { }

// ✅ Correct - add HTTP attribute:
[HttpGet("some-endpoint")]
public async Task<IActionResult> SomeMethod() { }

// ✅ Or mark as non-action:
[NonAction]
public async Task<IActionResult> HelperMethod() { }
```

#### 4. **Complex DTO Serialization Issues**

**Symptom**: Swagger fails to serialize complex nested objects

**Check**: Look for circular references in DTOs
```bash
# Check for complex DTOs:
grep -r "DetailedPlantAnalysisDto\|HealthAssessmentDto" Entities/
```

**Fix**: Use Schema Filter ✅ **ALREADY IMPLEMENTED**
```csharp
// ✅ Already configured in your project:
c.SchemaFilter<ExcludeComplexTypesSchemaFilter>();

// The filter excludes these complex types:
// - DetailedPlantAnalysisDto
// - PlantIdentificationDto  
// - HealthAssessmentDto
// - NutrientStatusDto
// ... and others
```

---

## 🧪 Smoke Test Checklist

### Test 1: Swagger JSON Endpoint
```bash
# Test Swagger JSON generation
curl -f -s -o /dev/null -w "%{http_code}" http://localhost:5000/swagger/v1/swagger.json
# Expected: 200

# Test with headers
curl -I http://localhost:5000/swagger/v1/swagger.json
# Expected: Content-Type: application/json

# Test response structure
curl http://localhost:5000/swagger/v1/swagger.json | head -5
# Expected: Should contain "openapi": "3.0.1"
```

### Test 2: Swagger UI Access
```bash
# Test Swagger UI page
curl -f -s -o /dev/null -w "%{http_code}" http://localhost:5000/swagger/index.html
# Expected: 200

# Alternative endpoint
curl -f -s -o /dev/null -w "%{http_code}" http://localhost:5000/swagger/
# Expected: 200
```

### Test 3: API Endpoint Discovery
```bash
# Test if API endpoints are discoverable
curl http://localhost:5000/swagger/v1/swagger.json | jq '.paths | keys | length'
# Expected: Number > 0 (should show count of API endpoints)

# Test specific controller endpoints
curl http://localhost:5000/swagger/v1/swagger.json | jq '.paths | keys' | grep -i "auth\|plant"
# Expected: Should show auth and plant analysis endpoints
```

---

## 🔧 Your Project Status Summary

| Component | Status | Notes |
|-----------|--------|-------|
| **Swagger Services** | ✅ **GOOD** | Properly configured with custom SchemaIds |
| **Swagger Pipeline** | ✅ **GOOD** | Correctly placed in development environment |
| **Package Version** | ✅ **GOOD** | Swashbuckle 7.2.0 compatible with .NET 9.0 |
| **Controller Methods** | ✅ **GOOD** | All methods have proper HTTP attributes |
| **Complex DTO Handling** | ✅ **EXCELLENT** | Custom filter prevents serialization issues |

---

## 🚀 Critical Issues Found: **NONE**

Your Swagger configuration appears to be **production-ready** with several advanced optimizations:

1. **Schema Conflict Prevention**: `c.CustomSchemaIds(type => type.FullName)`
2. **Complex Type Filtering**: Custom `ExcludeComplexTypesSchemaFilter`
3. **Environment Safety**: Swagger only enabled in non-production
4. **Documentation Support**: XML comments integration

---

## 🔍 Advanced Debugging Commands

If you encounter Swagger JSON errors despite good configuration:

### Debug 1: Verbose Swagger Generation
```bash
# Enable detailed logging for Swagger
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --project WebAPI --verbosity detailed 2>&1 | grep -i swagger
```

### Debug 2: Check for Endpoint Conflicts
```bash
# Verify no duplicate routes
curl http://localhost:5000/swagger/v1/swagger.json | jq '.paths' | grep -o '"/[^"]*"' | sort | uniq -d
# Expected: No duplicates
```

### Debug 3: Validate OpenAPI Specification
```bash
# Download and validate the generated spec
curl http://localhost:5000/swagger/v1/swagger.json > swagger.json
npx @apidevtools/swagger-parser validate swagger.json
# Expected: "API is valid"
```

---

## 📋 Emergency Fixes

### If Swagger Completely Broken

**Quick Reset Configuration**:
```csharp
// Minimal working Swagger configuration:
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c => 
{
    c.CustomSchemaIds(type => type.FullName);
});

// In Configure():
app.UseSwagger();
app.UseSwaggerUI();
```

### If SchemaId Conflicts Persist

**Enhanced SchemaId Configuration**:
```csharp
services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => 
    {
        if (type.IsGenericType)
        {
            var genericTypeName = type.GetGenericTypeDefinition().Name.Replace("`1", "");
            var genericArgs = string.Join("", type.GetGenericArguments().Select(x => x.Name));
            return $"{type.Namespace}.{genericTypeName}Of{genericArgs}";
        }
        return type.FullName?.Replace("+", ".");
    });
});
```

---

## ✅ Conclusion

Your ZiraAI project has **excellent Swagger configuration** with advanced features for handling complex DTOs. The configuration should work reliably for API documentation generation.

**Recommended Actions**: 
1. Run smoke tests to verify current functionality
2. Monitor logs for any schema generation warnings
3. Keep the existing configuration as it's well-architected
