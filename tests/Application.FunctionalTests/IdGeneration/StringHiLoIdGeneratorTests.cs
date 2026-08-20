using Microsoft.Extensions.DependencyInjection;
using CleanArchitecture.Application.FunctionalTests.Infrastructure;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.IdGeneration;

namespace CleanArchitecture.Application.FunctionalTests.IdGeneration;

/// <summary>
/// Covers the block reservation against a real PostgreSQL: the upsert is raw SQL, so nothing but
/// an actual round trip proves it runs. Each test uses its own radical — the generator caches the
/// current block in memory for the whole run, while the database is reset between tests.
/// </summary>
public class StringHiLoIdGeneratorTests : TestBase
{
    private static StringIdRegistration RegistrationFor(string radical, int blockSize) =>
        new(typeof(object), radical, PadLength: 6, Separator: "-", HiLoBlockSize: blockSize);

    [Test]
    public async Task ShouldReserveBlocksAndNumberSequentially()
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<StringHiLoIdGenerator>();

        // A block of two, taken five times: the reservation has to run three times, so a failure
        // to compose the INSERT ... RETURNING would surface on the very first call.
        var registration = RegistrationFor($"HILO{Guid.NewGuid():N}"[..12], blockSize: 2);

        var ids = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            ids.Add(await generator.NextAsync(registration));
        }

        ids.ShouldBe([
            $"{registration.Radical}-000001",
            $"{registration.Radical}-000002",
            $"{registration.Radical}-000003",
            $"{registration.Radical}-000004",
            $"{registration.Radical}-000005"
        ]);
    }

    [Test]
    public async Task ShouldPersistTheReservedHighValue()
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<StringHiLoIdGenerator>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var registration = RegistrationFor($"HILO{Guid.NewGuid():N}"[..12], blockSize: 10);

        await generator.NextAsync(registration);

        // One id handed out, but a whole block is committed: a restart resumes past it rather
        // than replaying ids that may already be in use.
        var sequence = await context.IdSequences.FindAsync(registration.Radical);

        sequence.ShouldNotBeNull();
        sequence!.CurrentValue.ShouldBe(10);
    }
}
