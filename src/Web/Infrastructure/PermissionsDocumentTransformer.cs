using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using CleanArchitecture.Domain.Constants;

namespace CleanArchitecture.Web.Infrastructure;

/// <summary>
/// Injects the application's <see cref="Permissions"/> catalogue into the OpenAPI document as a
/// string enum named <c>Permission</c>, and rewires <c>UserInfoResponse.permissions[]</c> to
/// reference it. Orval picks it up at <c>npm run generate-api</c> time and emits a TypeScript
/// const that the frontend uses as the single source of truth for the permission catalogue.
/// </summary>
internal sealed class PermissionsDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();

        var permissionSchema = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Description = "Permission identifiers granted to users through their roles.",
            Enum = Permissions.All.Select(p => (JsonNode)JsonValue.Create(p)!).ToList()
        };

        document.Components.Schemas["Permission"] = permissionSchema;

        // Rewire UserInfoResponse.permissions items to reference the Permission enum so the
        // generated TypeScript type is Permission[] rather than string[].
        if (document.Components.Schemas.TryGetValue("UserInfoResponse", out var userInfo)
            && userInfo is OpenApiSchema userInfoSchema
            && userInfoSchema.Properties is not null
            && userInfoSchema.Properties.TryGetValue("permissions", out var permsProp)
            && permsProp is OpenApiSchema permsSchema
            && permsSchema.Items is not null)
        {
            permsSchema.Items = new OpenApiSchemaReference("Permission", document);
        }

        return Task.CompletedTask;
    }
}
