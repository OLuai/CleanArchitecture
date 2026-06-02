using Microsoft.AspNetCore.Http;
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors(static builder =>
    builder.AllowAnyMethod()
        .AllowAnyHeader()
        .AllowAnyOrigin());

app.UseFileServer();

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


app.MapDefaultEndpoints();
app.MapEndpoints(typeof(Program).Assembly);

app.MapFallbackToFile("index.html");

app.Run();
