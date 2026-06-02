using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Migration;
using CleanArchitecture.Migration.Seed;
using CleanArchitecture.Migration.Workers;
using CleanArchitecture.Shared;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Provide a non-HTTP IUser so AuditableEntityInterceptor (registered by Infrastructure DI)
// can be resolved in this worker. Must be registered BEFORE AddInfrastructureServices so it
// is the one picked up by the interceptor.
builder.Services.AddScoped<IUser, SystemUser>();

// Application registers MediatR, AutoMapper, FluentValidation — required because
// DispatchDomainEventsInterceptor depends on IMediator. Seeders themselves do not publish
// events, but the interceptor must still be DI-resolvable.
builder.AddApplicationServices();

builder.AddInfrastructureServices();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IDbSeeder, DevelopmentSeeder>();
}
else
{
    builder.Services.AddScoped<IDbSeeder, ProductionSeeder>();
}

builder.Services.AddHostedService<MigrationWorker>();

using var host = builder.Build();
await host.RunAsync();

return MigrationWorker.ExitCode;
