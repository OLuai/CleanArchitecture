namespace CleanArchitecture.Migration.Seed;

public interface IDbSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
