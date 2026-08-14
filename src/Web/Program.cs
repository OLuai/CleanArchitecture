using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

// Behind a reverse proxy (Traefik or nginx in production, Aspire in development) the backend
// receives plain HTTP even though the client request was HTTPS. Without this, Request.Scheme is
// wrong: HTTPS redirects loop, Secure cookies are not recognised as secure, and any absolute URL
// the app builds (password-reset links, OAuth redirect_uri) points at the internal scheme/host.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;

    // On .NET 8+ an empty KnownIPNetworks/KnownProxies means "no proxy is trusted", so the
    // X-Forwarded-* headers are ignored — it does not mean "trust any proxy". Container proxies
    // get dynamic addresses, so trust the whole range. Only do this when the app is never
    // reachable directly, i.e. the proxy is the sole ingress.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Any, 0));
    options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.IPv6Any, 0));
    options.ForwardLimit = null;
});

var app = builder.Build();

// Must run before any middleware that depends on the scheme (HTTPS redirect, cookies, auth).
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors();

app.UseRateLimiter();

#if (!UseApiOnly)
app.UseFileServer();
#endif

app.MapOpenApi();
app.MapScalarApiReference();

app.UseExceptionHandler(options => { });

// Convert any "naked" 4xx/5xx response (e.g. 401 from the auth middleware, 403 from authorization,
// 404 routing miss, 405 method not allowed) into a ProblemDetails JSON body via IProblemDetailsService.
app.UseStatusCodePages(async statusCodeContext =>
{
    var http = statusCodeContext.HttpContext;
    if (http.Response.HasStarted) return;

    var problemDetailsService = http.RequestServices.GetRequiredService<IProblemDetailsService>();
    var statusCode = http.Response.StatusCode;

    await problemDetailsService.WriteAsync(new ProblemDetailsContext
    {
        HttpContext = http,
        ProblemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Type = $"https://tools.ietf.org/html/rfc9110#status.{statusCode}"
        }
    });
});

// Force the Routing -> Authentication -> Authorization order to come after UseForwardedHeaders.
// Without these explicit calls WebApplication auto-inserts them at the very start of the
// pipeline, so the authentication middleware would run before the forwarded scheme is applied.
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapEndpoints(typeof(Program).Assembly);

// Unmatched /api routes must not reach the SPA fallback below: returning 200 + index.html for a
// typo or a stale client build is a silent success the client cannot distinguish from real data.
app.MapFallback("/api/{**path}", () => Results.Problem(statusCode: StatusCodes.Status404NotFound));

#if (!UseApiOnly)
app.MapFallbackToFile("index.html");
#endif

app.Run();
