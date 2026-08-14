using System.Text.Json;
using System.Threading.RateLimiting;
using Azure.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Web.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddScoped<IUser, CurrentUser>();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

        // Required so that UseStatusCodePages can fill empty 4xx/5xx responses
        // (e.g. 401 issued directly by the authentication middleware) with a ProblemDetails JSON body.
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                ctx.ProblemDetails.Instance ??= ctx.HttpContext.Request.Path;
                ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
            };
        });

        // Customise default API behaviour
        builder.Services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        });

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddOpenApi(options =>
        {
            options.AddOperationTransformer<ApiExceptionOperationTransformer>();
            options.AddOperationTransformer<IdentityApiOperationTransformer>();
            options.AddDocumentTransformer<PermissionsDocumentTransformer>();
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddSchemaTransformer<NumericSchemaTransformer>();
        });

        // Anonymous endpoints (login, register, password reset, any public read surface) are
        // otherwise wide open to brute-force and enumeration. Apply with
        // `.RequireRateLimiting(RateLimitPolicies.Public)` on the routes that need it.
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicies.Public, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        // The SPA is served same-origin (Vite proxies /api in development, wwwroot in production),
        // so no cross-origin access is granted by default. Set Cors:AllowedOrigins to open the API
        // to other origins; credentials are allowed because authentication is cookie-based, and
        // that combination forbids the AllowAnyOrigin wildcard.
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        builder.Services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
            {
                if (allowedOrigins.Length == 0)
                {
                    return;
                }

                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }));
    }

    public static void AddKeyVaultIfConfigured(this IHostApplicationBuilder builder)
    {
        var keyVaultUri = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential());
        }
    }
}
