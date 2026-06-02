using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Migration.Seed;

namespace CleanArchitecture.Migration.Workers;

public class MigrationWorker : BackgroundService
{
    public static int ExitCode { get; private set; }

    private readonly IServiceProvider _services;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<MigrationWorker> _logger;

    public MigrationWorker(
        IServiceProvider services,
        IHostApplicationLifetime lifetime,
        ILogger<MigrationWorker> logger)
    {
        _services = services;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _logger.LogInformation("Applying EF Core migrations...");
            await context.Database.MigrateAsync(stoppingToken);

            var seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder>();
            _logger.LogInformation("Seeding database with {Seeder}...", seeder.GetType().Name);
            await seeder.SeedAsync(stoppingToken);

            _logger.LogInformation("Database migration and seeding completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration worker failed.");
            ExitCode = 1;
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
