using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CleanArchitecture.Web.Infrastructure;

/// <summary>
/// Collapses the <c>["integer", "string"]</c> (and <c>["number", "string"]</c>) union that
/// ASP.NET Core emits for every numeric property down to the numeric type alone.
/// <para>
/// The union is technically accurate — the JSON parser would accept <c>"42"</c> — but it makes
/// every integer in the document generate as <c>number | string</c> in TypeScript, which forces
/// a cast at every use site for a case the API never actually produces. Serialization only ever
/// writes numbers, so the document should say so.
/// </para>
/// </summary>
internal sealed class NumericSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (schema.Type is not { } type || !type.HasFlag(JsonSchemaType.String))
        {
            return Task.CompletedTask;
        }

        if (type.HasFlag(JsonSchemaType.Integer) || type.HasFlag(JsonSchemaType.Number))
        {
            // Keep Null, so a nullable int stays nullable.
            schema.Type = type & ~JsonSchemaType.String;

            // The pattern only existed to describe the string half of the union.
            schema.Pattern = null;
        }

        return Task.CompletedTask;
    }
}
