namespace CleanArchitecture.Application.FunctionalTests.Infrastructure;

public abstract class TestBase
{
    [SetUp]
    public async Task SetUp()
    {
        await TestApp.ResetState();

        // Sign in by default: almost every test exercises a use case, not the authorization
        // layer, and every command and query is permission-gated. Tests that care about being
        // anonymous call TestApp.RunAsAnonymous() explicitly.
        await TestApp.RunAsDefaultUserAsync();
    }
}
