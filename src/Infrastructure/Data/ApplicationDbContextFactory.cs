using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CleanArchitecture.Infrastructure.Data;

/// <summary>
/// Used by EF Core CLI tools (Add-Migration, Remove-Migration, Update-Database, migrations script)
/// when no host is running. Reads the connection string from the
/// <c>ConnectionStrings__CleanArchitectureDb</c> environment variable; falls back to a local dev default
/// matching the Aspire-managed Postgres container (port 5431, password "postgres").
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string EnvVarName = "ConnectionStrings__CleanArchitectureDb";
    private const string DevFallback =
        "Host=localhost;Port=5431;Database=CleanArchitectureDb;Username=postgres;Password=postgres";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(EnvVarName) ?? DevFallback;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name))
            .Options;

        return new ApplicationDbContext(options);
    }
}
