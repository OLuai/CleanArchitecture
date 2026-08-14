using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CleanArchitecture.Web.Infrastructure;

/// <summary>
/// Adds the Bearer security scheme to the OpenAPI document when bearer authentication is
/// configured, so Scalar offers an Authorize button and sends
/// <c>Authorization: Bearer &lt;token&gt;</c> from the interactive documentation. Obtain a token
/// from <c>POST /api/Users/identity/login?useCookies=false</c>.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        // ASP.NET Identity registers its bearer handler as "Identity.Bearer", not "Bearer".
        if (authenticationSchemes.Any(authScheme => authScheme.Name == IdentityConstants.BearerScheme))
        {
            var requirements = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "Json Web Token"
                }
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = requirements;
        }
    }
}
