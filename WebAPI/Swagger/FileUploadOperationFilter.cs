using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace WebAPI.Swagger
{
    /// <summary>
    /// Swagger operation filter to properly handle IFormFile parameters in file upload endpoints
    /// Handles both simple IFormFile parameters and complex types containing IFormFile properties
    /// </summary>
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Check if action has [Consumes("multipart/form-data")]
            var hasConsumesAttribute = context.ApiDescription.ActionDescriptor.EndpointMetadata
                .OfType<Microsoft.AspNetCore.Mvc.ConsumesAttribute>()
                .Any(a => a.ContentTypes.Contains("multipart/form-data"));

            if (!hasConsumesAttribute)
                return;

            // Check if any parameter has IFormFile in it (either directly or in a complex type)
            var hasFormFile = context.ApiDescription.ParameterDescriptions.Any(p =>
                p.ModelMetadata != null &&
                (p.ModelMetadata.ModelType == typeof(IFormFile) ||
                 HasIFormFileProperty(p.ModelMetadata.ModelType)));

            if (!hasFormFile)
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
                    
                    if (param.IsRequired)
                        required.Add(param.Name);
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
                    
                    if (param.IsRequired)
                        required.Add(param.Name);
                }
                else if (HasIFormFileProperty(paramType))
                {
                    // Complex type with IFormFile properties
                    var typeProperties = paramType.GetProperties();
                    foreach (var prop in typeProperties)
                    {
                        if (prop.PropertyType == typeof(IFormFile))
                        {
                            properties[prop.Name] = new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary"
                            };
                            
                            if (!IsNullable(prop.PropertyType) && !HasDefaultValue(prop))
                                required.Add(prop.Name);
                        }
                        else if (prop.PropertyType == typeof(IEnumerable<IFormFile>) || 
                                 prop.PropertyType == typeof(List<IFormFile>))
                        {
                            properties[prop.Name] = new OpenApiSchema
                            {
                                Type = "array",
                                Items = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary"
                                }
                            };
                            
                            if (!IsNullable(prop.PropertyType) && !HasDefaultValue(prop))
                                required.Add(prop.Name);
                        }
                        else
                        {
                            properties[prop.Name] = new OpenApiSchema
                            {
                                Type = GetSchemaType(prop.PropertyType)
                            };
                            
                            if (!IsNullable(prop.PropertyType) && !HasDefaultValue(prop))
                                required.Add(prop.Name);
                        }
                    }
                }
                else
                {
                    properties[param.Name] = new OpenApiSchema
                    {
                        Type = GetSchemaType(paramType)
                    };
                    
                    if (param.IsRequired)
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

        private static bool HasIFormFileProperty(Type type)
        {
            if (type == null || type.IsPrimitive || type == typeof(string))
                return false;

            var properties = type.GetProperties();
            return properties.Any(p => 
                p.PropertyType == typeof(IFormFile) ||
                p.PropertyType == typeof(IEnumerable<IFormFile>) ||
                p.PropertyType == typeof(List<IFormFile>));
        }

        private static bool IsNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null || !type.IsValueType;
        }

        private static bool HasDefaultValue(PropertyInfo property)
        {
            var defaultAttribute = property.GetCustomAttributes(typeof(DefaultValueAttribute), false)
                .FirstOrDefault() as DefaultValueAttribute;
            return defaultAttribute != null;
        }
    }
}
