using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Data.Interceptors;
using CleanArchitecture.Infrastructure.Identity;
using CleanArchitecture.Infrastructure.IdGeneration;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString(Services.Database);
        Guard.Against.Null(connectionString, message: $"Connection string '{Services.Database}' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, StringIdGenerationInterceptor>();

        builder.Services.AddSingleton<StringIdRegistry>();
        builder.Services.AddSingleton<StringHiLoIdGenerator>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name));
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        builder.EnrichNpgsqlDbContext<ApplicationDbContext>();

        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Two credential shapes, one pipeline: browsers get the Identity application cookie,
        // non-browser clients get bearer tokens from the Identity endpoints
        // (POST /api/Users/identity/login?useCookies=false). A policy scheme picks per request,
        // so endpoints and authorization policies never have to care which was used.
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = AuthenticationSchemes.CookieOrBearer;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddPolicyScheme(AuthenticationSchemes.CookieOrBearer, AuthenticationSchemes.CookieOrBearer, options =>
            {
                options.ForwardDefaultSelector = context =>
                    context.Request.Headers.Authorization.ToString()
                        .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? IdentityConstants.BearerScheme
                        : IdentityConstants.ApplicationScheme;
            })
            .AddBearerToken(IdentityConstants.BearerScheme)
            .AddIdentityCookies();

        // The Identity Application cookie defaults to redirecting (302) to /Account/Login on
        // unauthenticated access and /Account/AccessDenied on forbidden access. For an API,
        // we want clean 401/403 responses so the global ProblemDetails handler can write a JSON body.
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        // Without a durable key ring, Data Protection generates keys in the container filesystem:
        // every redeploy invalidates all auth cookies and antiforgery tokens, and two replicas
        // cannot read each other's. Persisting to the database fixes both.
        builder.Services.AddDataProtection()
            .PersistKeysToDbContext<ApplicationDbContext>();

        // Permissions are carried as claims inside the auth cookie, so a role change only reaches
        // an open session when the cookie is revalidated. Five minutes bounds how long a user can
        // keep permissions that have just been revoked (the default is 30).
        builder.Services.Configure<SecurityStampValidatorOptions>(options =>
            options.ValidationInterval = TimeSpan.FromMinutes(5));

        builder.Services.AddAuthorizationBuilder();
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        builder.Services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddApiEndpoints();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<IIdentityService, IdentityService>();
    }
}
