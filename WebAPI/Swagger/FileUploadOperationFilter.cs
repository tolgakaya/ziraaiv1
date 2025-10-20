using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebAPI.Swagger
{
    /// <summary>
    /// Swagger operation filter to properly handle IFormFile parameters in file upload endpoints
    /// </summary>
    /// <summary>
    /// Swagger operation filter to properly handle IFormFile parameters in file upload endpoints
    /// </summary>
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var formFileParameters = context.ApiDescription.ParameterDescriptions
                .Where(p => p.ModelMetadata != null && 
                           (p.ModelMetadata.ModelType == typeof(IFormFile) || 
                            p.ModelMetadata.ModelType == typeof(IEnumerable<IFormFile>) ||
                            p.ModelMetadata.ModelType == typeof(List<IFormFile>)))
                .ToList();

            if (!formFileParameters.Any())
                return;

            // Get all parameters from the endpoint
            var allParameters = context.ApiDescription.ParameterDescriptions.ToList();
            
            // Create schema properties
            var properties = new Dictionary<string, OpenApiSchema>();
            var required = new HashSet<string>();

            foreach (var param in allParameters)
            {
                var paramType = param.ModelMetadata?.ModelType;
                
                if (paramType == typeof(IFormFile))
                {
                    properties[param.Name] = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    };
                }
                else if (paramType == typeof(IEnumerable<IFormFile>) || paramType == typeof(List<IFormFile>))
                {
                    properties[param.Name] = new OpenApiSchema
                    {
                        Type = "array",
                        Items = new OpenApiSchema
                        {
                            Type = "string",
                            Format = "binary"
                        }
                    };
                }
                else
                {
                    properties[param.Name] = new OpenApiSchema
                    {
                        Type = GetSchemaType(paramType)
                    };
                }

                if (param.IsRequired)
                {
                    required.Add(param.Name);
                }
            }

            // Clear parameters and set request body
            operation.Parameters?.Clear();
            
            operation.RequestBody = new OpenApiRequestBody
            {
                Content =
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = properties,
                            Required = required
                        }
                    }
                }
            };
        }

        private static string GetSchemaType(Type type)
        {
            if (type == null) return "string";
            
            if (type == typeof(int) || type == typeof(long)) return "integer";
            if (type == typeof(bool)) return "boolean";
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float)) return "number";
            
            return "string";
        }
    }
}
