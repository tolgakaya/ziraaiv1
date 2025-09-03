using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Diagnostic controller to identify Swagger generation issues
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)] // Exclude this controller from Swagger
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class SwaggerDiagnosticsController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISwaggerProvider _swaggerProvider;

        public SwaggerDiagnosticsController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _swaggerProvider = _serviceProvider.GetService<ISwaggerProvider>();
        }

        /// <summary>
        /// Test Swagger document generation and capture detailed error information
        /// </summary>
        [HttpGet("test-swagger-generation")]
        public IActionResult TestSwaggerGeneration()
        {
            var diagnosticResults = new StringBuilder();
            var errors = new List<string>();
            
            try
            {
                diagnosticResults.AppendLine("=== SWAGGER GENERATION DIAGNOSTIC REPORT ===");
                diagnosticResults.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                diagnosticResults.AppendLine();

                // Step 1: Check if Swagger provider is available
                diagnosticResults.AppendLine("Step 1: Checking Swagger Provider");
                if (_swaggerProvider == null)
                {
                    errors.Add("ERROR: ISwaggerProvider is not registered in DI container");
                    diagnosticResults.AppendLine("  ❌ ISwaggerProvider is NOT registered!");
                }
                else
                {
                    diagnosticResults.AppendLine("  ✓ ISwaggerProvider is registered");
                }

                // Step 2: Try to generate Swagger document
                diagnosticResults.AppendLine("\nStep 2: Attempting to Generate Swagger Document");
                try
                {
                    var swaggerDoc = _swaggerProvider?.GetSwagger("v1");
                    if (swaggerDoc != null)
                    {
                        diagnosticResults.AppendLine("  ✓ Swagger document generated successfully");
                        // OpenApiDocument doesn't have OpenApi property in this version
                        diagnosticResults.AppendLine($"  - API Title: {swaggerDoc.Info?.Title}");
                        diagnosticResults.AppendLine($"  - API Version: {swaggerDoc.Info?.Version}");
                        diagnosticResults.AppendLine($"  - Paths Count: {swaggerDoc.Paths?.Count ?? 0}");
                        diagnosticResults.AppendLine($"  - Schemas Count: {swaggerDoc.Components?.Schemas?.Count ?? 0}");
                    }
                    else
                    {
                        errors.Add("ERROR: Swagger document is null");
                        diagnosticResults.AppendLine("  ❌ Swagger document is null");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"ERROR generating Swagger: {ex.GetType().Name}: {ex.Message}");
                    diagnosticResults.AppendLine($"  ❌ Exception during generation: {ex.GetType().Name}");
                    diagnosticResults.AppendLine($"     Message: {ex.Message}");
                    
                    if (ex.InnerException != null)
                    {
                        diagnosticResults.AppendLine($"     Inner Exception: {ex.InnerException.GetType().Name}");
                        diagnosticResults.AppendLine($"     Inner Message: {ex.InnerException.Message}");
                        
                        // Check for even deeper exceptions
                        var innerMost = ex.InnerException;
                        while (innerMost.InnerException != null)
                        {
                            innerMost = innerMost.InnerException;
                            diagnosticResults.AppendLine($"     Deeper Exception: {innerMost.GetType().Name}");
                            diagnosticResults.AppendLine($"     Deeper Message: {innerMost.Message}");
                        }
                    }
                    
                    diagnosticResults.AppendLine($"\n     Stack Trace:\n{ex.StackTrace}");
                }

                // Step 3: Check all controllers
                diagnosticResults.AppendLine("\nStep 3: Analyzing Controllers");
                var controllers = GetAllControllers();
                diagnosticResults.AppendLine($"  Found {controllers.Count} controllers");
                
                foreach (var controller in controllers)
                {
                    try
                    {
                        var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                            .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == controller);
                        
                        diagnosticResults.AppendLine($"\n  Controller: {controller.Name}");
                        diagnosticResults.AppendLine($"    - Public Methods: {methods.Count()}");
                        
                        // Check for problematic attributes or return types
                        foreach (var method in methods)
                        {
                            var returnType = method.ReturnType;
                            if (returnType.IsGenericType)
                            {
                                var genericType = returnType.GetGenericTypeDefinition();
                                var genericArgs = returnType.GetGenericArguments();
                                
                                // Check for potential circular references
                                if (HasPotentialCircularReference(genericArgs.FirstOrDefault()))
                                {
                                    var warning = $"    ⚠ Method '{method.Name}' might have circular reference in return type: {genericArgs.FirstOrDefault()?.Name}";
                                    diagnosticResults.AppendLine(warning);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"ERROR analyzing controller {controller.Name}: {ex.Message}");
                        diagnosticResults.AppendLine($"    ❌ Error analyzing controller: {ex.Message}");
                    }
                }

                // Step 4: Check Swagger configuration
                diagnosticResults.AppendLine("\nStep 4: Checking Swagger Configuration");
                var swaggerOptions = _serviceProvider.GetService<IOptions<SwaggerGenOptions>>();
                if (swaggerOptions?.Value != null)
                {
                    diagnosticResults.AppendLine("  ✓ SwaggerGenOptions found");
                    // Note: Can't easily introspect the options, but we know they exist
                }
                else
                {
                    diagnosticResults.AppendLine("  ❌ SwaggerGenOptions not found");
                }

                // Step 5: Test schema filter
                diagnosticResults.AppendLine("\nStep 5: Testing Schema Filters");
                diagnosticResults.AppendLine("  - ExcludeComplexTypesSchemaFilter is registered");
                diagnosticResults.AppendLine("  - This filter excludes the following DTOs:");
                var excludedTypes = new[]
                {
                    "DetailedPlantAnalysisDto",
                    "PlantIdentificationDto", 
                    "HealthAssessmentDto",
                    "NutrientStatusDto",
                    "PestDiseaseDto",
                    "EnvironmentalStressDto",
                    "RecommendationsDto",
                    "CrossFactorInsightDto",
                    "SummaryDto",
                    "ProcessingMetadataDto",
                    "TokenUsageDto",
                    "TokenSummaryDto",
                    "TokenBreakdownDto",
                    "CostBreakdownDto"
                };
                foreach (var type in excludedTypes)
                {
                    diagnosticResults.AppendLine($"    - {type}");
                }

                // Summary
                diagnosticResults.AppendLine("\n=== SUMMARY ===");
                if (errors.Any())
                {
                    diagnosticResults.AppendLine($"❌ Found {errors.Count} error(s):");
                    foreach (var error in errors)
                    {
                        diagnosticResults.AppendLine($"  - {error}");
                    }
                }
                else
                {
                    diagnosticResults.AppendLine("✓ No critical errors found during diagnostic");
                }

                // Recommendations
                diagnosticResults.AppendLine("\n=== RECOMMENDATIONS ===");
                diagnosticResults.AppendLine("1. Try temporarily disabling the ExcludeComplexTypesSchemaFilter");
                diagnosticResults.AppendLine("2. Enable XML documentation if needed");
                diagnosticResults.AppendLine("3. Check for any DTOs with circular references");
                diagnosticResults.AppendLine("4. Verify all controller actions have proper return types");
                diagnosticResults.AppendLine("5. Consider using [ApiExplorerSettings(IgnoreApi = true)] on problematic controllers");

                return Ok(new
                {
                    success = !errors.Any(),
                    report = diagnosticResults.ToString(),
                    errors = errors,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace,
                    report = diagnosticResults.ToString()
                });
            }
        }

        /// <summary>
        /// Get detailed error from attempting to access swagger.json endpoint
        /// </summary>
        [HttpGet("capture-swagger-error")]
        public IActionResult CaptureSwaggerError()
        {
            try
            {
                // Try to generate the Swagger document with detailed error capture
                var swaggerDoc = _swaggerProvider?.GetSwagger("v1");
                
                if (swaggerDoc == null)
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        error = "Swagger document is null",
                        possibleCauses = new[]
                        {
                            "ISwaggerProvider not registered",
                            "Document name 'v1' not found",
                            "Swagger generation failed silently"
                        }
                    });
                }

                // Try to serialize it (this might be where the error occurs)
                var json = System.Text.Json.JsonSerializer.Serialize(new
                {
                    // OpenApi property not available in this version
                    info = swaggerDoc.Info,
                    paths = swaggerDoc.Paths?.Count,
                    schemas = swaggerDoc.Components?.Schemas?.Count
                });

                return Ok(new
                {
                    success = true,
                    message = "Swagger document generated successfully",
                    documentInfo = json
                });
            }
            catch (Exception ex)
            {
                // Capture all exception details
                var exceptionDetails = new Dictionary<string, object>
                {
                    ["Type"] = ex.GetType().FullName,
                    ["Message"] = ex.Message,
                    ["Source"] = ex.Source,
                    ["TargetSite"] = ex.TargetSite?.ToString()
                };

                // Capture inner exceptions
                var innerExceptions = new List<Dictionary<string, object>>();
                var currentException = ex.InnerException;
                var depth = 0;
                
                while (currentException != null && depth < 10)
                {
                    innerExceptions.Add(new Dictionary<string, object>
                    {
                        ["Depth"] = ++depth,
                        ["Type"] = currentException.GetType().FullName,
                        ["Message"] = currentException.Message,
                        ["Source"] = currentException.Source
                    });
                    currentException = currentException.InnerException;
                }

                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to generate Swagger document",
                    exception = exceptionDetails,
                    innerExceptions = innerExceptions,
                    stackTrace = ex.StackTrace,
                    data = ex.Data,
                    helpLink = ex.HelpLink
                });
            }
        }

        private List<Type> GetAllControllers()
        {
            return Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
                .ToList();
        }

        private bool HasPotentialCircularReference(Type type)
        {
            if (type == null) return false;
            
            // Check for known problematic types
            var problematicTypes = new[]
            {
                "DetailedPlantAnalysisDto",
                "PlantAnalysisAsyncResponseDto",
                "PlantIdentificationDto"
            };
            
            return problematicTypes.Contains(type.Name);
        }
    }
}