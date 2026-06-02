using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CleanArchitecture.Web.Infrastructure;
/// <summary>
/// Adds standard error responses to every OpenAPI operation, complete with typed schemas
/// (<see cref="ProblemDetails"/> / <see cref="HttpValidationProblemDetails"/>) so the
/// generated NSwag TypeScript client exposes them as classes. A 400 Bad Request response
/// is added to all operations because every request passes through <c>ValidationBehaviour</c>
/// in the MediatR pipeline. 401 Unauthorized and 403 Forbidden are added only to operations
/// carrying <see cref="IAuthorizeData"/> metadata. 404 and 500 are added for FriendlyException
/// / NotFoundException / unhandled errors.
/// </summary>
internal sealed class ApiExceptionOperationTransformer : IOpenApiOperationTransformer
{
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        operation.Responses ??= [];

        var validationSchema = await context.GetOrCreateSchemaAsync(typeof(HttpValidationProblemDetails), null, cancellationToken);
        var problemSchema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), null, cancellationToken);

        AddResponse(operation, "400", "Bad Request", validationSchema);
        AddResponse(operation, "404", "Not Found", problemSchema);
        AddResponse(operation, "500", "Internal Server Error", problemSchema);

        var requiresAuth = context.Description.ActionDescriptor.EndpointMetadata
            .Any(m => m is IAuthorizeData);

        if (requiresAuth)
        {
            AddResponse(operation, "401", "Unauthorized", problemSchema);
            AddResponse(operation, "403", "Forbidden", problemSchema);
        }
    }

    private static void AddResponse(OpenApiOperation operation, string statusCode, string description, IOpenApiSchema schema)
    {
        if (operation.Responses!.ContainsKey(statusCode)) return;

        operation.Responses[statusCode] = new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/problem+json"] = new OpenApiMediaType { Schema = schema }
            }
        };
    }
}
