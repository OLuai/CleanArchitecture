using System.Reflection;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Data.Interceptors;
using CleanArchitecture.Infrastructure.Email;
using CleanArchitecture.Infrastructure.Identity;
using CleanArchitecture.Infrastructure.Codification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString(Services.Database);
        Guard.Against.Null(connectionString, message: $"Connection string '{Services.Database}' not found.");

        // Codes are assigned first so that the domain events dispatched below can use them.
        builder.Services.AddScoped<ISaveChangesInterceptor, CodeGenerationInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        builder.Services.AddSingleton<CodeRegistry>();
        builder.Services.AddSingleton<HiLoCodeGenerator>();

        // Business codes beside the GUID keys: what a person reads, dictates over the phone and quotes
        // in a ticket. Each IHasCode entity declares its radical here.
        builder.Services.RegisterCode<TodoList>("LST");

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
        //
        // Except while the build generates the OpenAPI document: dotnet-getdocument starts the app
        // to read its endpoints, with no database running. Loading the key ring from it would sit
        // through every connection retry (about a minute and a half per build) for keys the
        // document never uses.
        var dataProtection = builder.Services.AddDataProtection();
        if (!IsOpenApiDocumentGeneration())
        {
            dataProtection.PersistKeysToDbContext<ApplicationDbContext>();
        }

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

        // Outgoing mail. Settings come from configuration (Smtp__* variables) and are overridden by
        // the EmailSettings row once an administrator has saved one from /admin/email.
        builder.Services.AddOptions<SmtpOptions>()
            .Bind(builder.Configuration.GetSection(SmtpOptions.Section));

        builder.Services.AddSingleton<IEmailConfiguration>(sp =>
            sp.GetRequiredService<IOptions<SmtpOptions>>().Value);

        builder.Services.AddScoped<ISecretProtector, DataProtectionSecretProtector>();
        builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

        // Routes Identity's own messages (confirmation, password reset) through the same relay.
        // Without this, AddApiEndpoints keeps a sender that does nothing.
        builder.Services.AddSingleton<
            Microsoft.AspNetCore.Identity.IEmailSender<ApplicationUser>, IdentityEmailSender>();
    }

    // dotnet-getdocument (Microsoft.Extensions.ApiDescription.Server) runs the app through this
    // entry assembly when it writes wwwroot/openapi at build time.
    private static bool IsOpenApiDocumentGeneration() =>
        Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
}
