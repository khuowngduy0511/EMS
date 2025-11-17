using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Reflection;

namespace ExamManagement.API.Swagger;

/// <summary>
/// Operation filter to handle file uploads with [FromForm] parameters.
/// This filter prevents Swagger from generating parameters for [FromForm] actions
/// and instead creates a proper multipart/form-data request body.
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if this action has any [FromForm] parameters
        var hasFormParameters = context.MethodInfo.GetParameters()
            .Any(p => p.GetCustomAttribute<FromFormAttribute>() != null);

        if (!hasFormParameters)
            return;

        // Get all [FromForm] parameters
        var formParameters = context.MethodInfo.GetParameters()
            .Where(p => p.GetCustomAttribute<FromFormAttribute>() != null)
            .ToList();

        // Check if any of them is IFormFile
        var hasFormFile = formParameters.Any(p => p.ParameterType == typeof(IFormFile));

        if (!hasFormFile)
            return;

        // Clear any existing parameters that Swagger might have generated incorrectly
        operation.Parameters?.Clear();

        // Remove any existing request body that Swagger might have generated
        operation.RequestBody = null;

        // Create new request body for multipart/form-data
        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>(),
                        Required = new HashSet<string>()
                    }
                }
            }
        };

        var schema = operation.RequestBody.Content["multipart/form-data"].Schema;

        // Add each [FromForm] parameter to the schema
        foreach (var parameter in formParameters)
        {
            OpenApiSchema paramSchema;

            if (parameter.ParameterType == typeof(IFormFile))
            {
                paramSchema = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                    Description = "File to upload"
                };
                schema.Required.Add(parameter.Name);
            }
            else if (parameter.ParameterType == typeof(int))
            {
                paramSchema = new OpenApiSchema
                {
                    Type = "integer",
                    Format = "int32"
                };
                schema.Required.Add(parameter.Name);
            }
            else if (parameter.ParameterType == typeof(int?))
            {
                paramSchema = new OpenApiSchema
                {
                    Type = "integer",
                    Format = "int32",
                    Nullable = true
                };
            }
            else if (parameter.ParameterType == typeof(string))
            {
                paramSchema = new OpenApiSchema
                {
                    Type = "string"
                };
                schema.Required.Add(parameter.Name);
            }
            else
            {
                // Handle nullable string (string?)
                var underlyingType = Nullable.GetUnderlyingType(parameter.ParameterType);
                if (underlyingType == typeof(string))
                {
                    paramSchema = new OpenApiSchema
                    {
                        Type = "string",
                        Nullable = true
                    };
                    // Nullable string is optional, don't add to required
                }
                else
                {
                    // Fallback to string for unknown types
                    paramSchema = new OpenApiSchema
                    {
                        Type = "string"
                    };
                    // Check if parameter is required
                    if (!parameter.HasDefaultValue && !parameter.IsOptional)
                    {
                        schema.Required.Add(parameter.Name);
                    }
                }
            }

            schema.Properties[parameter.Name] = paramSchema;
        }
    }
}
