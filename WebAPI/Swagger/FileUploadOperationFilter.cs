using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace WebAPI.Swagger
{
    /// <summary>
    /// Swagger operation filter to properly handle IFormFile parameters in file upload endpoints
    /// </summary>
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var formFileParameters = context.ApiDescription.ParameterDescriptions
                .Where(p => p.ModelMetadata?.ModelType == typeof(IFormFile))
                .ToList();

            if (!formFileParameters.Any())
                return;

            // Clear existing parameters
            operation.Parameters?.Clear();

            // Set request body to multipart/form-data
            operation.RequestBody = new OpenApiRequestBody
            {
                Content =
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = context.ApiDescription.ParameterDescriptions
                                .ToDictionary(
                                    p => p.Name,
                                    p => p.ModelMetadata?.ModelType == typeof(IFormFile)
                                        ? new OpenApiSchema
                                        {
                                            Type = "string",
                                            Format = "binary"
                                        }
                                        : new OpenApiSchema
                                        {
                                            Type = GetSchemaType(p.ModelMetadata?.ModelType)
                                        }
                                ),
                            Required = context.ApiDescription.ParameterDescriptions
                                .Where(p => p.IsRequired)
                                .Select(p => p.Name)
                                .ToHashSet()
                        }
                    }
                }
            };
        }

        private static string GetSchemaType(System.Type type)
        {
            if (type == null) return "string";
            
            if (type == typeof(int) || type == typeof(long)) return "integer";
            if (type == typeof(bool)) return "boolean";
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float)) return "number";
            
            return "string";
        }
    }
}
