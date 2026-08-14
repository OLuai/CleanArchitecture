using Aspire.Hosting;

namespace CleanArchitecture.Web.AcceptanceTests;

[SetUpFixture]
public class AspireSetup
{
    // Starting the real AppHost is not cheap: PostgreSQL and pgAdmin containers (pulled on a
    // cold machine), the migration worker, the API, then the Vite dev server, which runs
    // npm install and regenerates the API client before it reports healthy.
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    public static IDistributedApplicationTestingBuilder Builder { get; private set; } = null!;
    public static DistributedApplication App { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        // These tests start the real AppHost, which needs the shared PostgreSQL password. Without
        // it the parameter never resolves, PostgreSQL never starts, and the run would sit here
        // until the timeout with nothing explaining why. Say so immediately instead.
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Services.Shared.PostgresPasswordEnvVar)))
        {
            Assert.Fail(
                $"{Services.Shared.PostgresPasswordEnvVar} is not set. The acceptance tests run the " +
                "real AppHost against the shared PostgreSQL container, so they need its password. " +
                $"Set it once per machine: setx {Services.Shared.PostgresPasswordEnvVar} \"<password>\"");
        }

        var cts = new CancellationTokenSource(DefaultTimeout);
        var cancellationToken = cts.Token;

        Builder = await DistributedApplicationTestingBuilder
             .CreateAsync<Projects.AppHost>(
                args: [],
                configureBuilder: (options, _) =>
                {
                    options.DisableDashboard = false; // Enable the dashboard for testing purposes
                });

        Builder.Configuration["ASPIRE_ALLOW_UNSECURED_TRANSPORT"] = "true";

        Builder.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            // Override the logging filters from the app's configuration
            logging.AddFilter(Builder.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });

        Builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        App = await Builder
            .BuildAsync(cancellationToken)
            .WaitAsync(cancellationToken);

        await App
            .StartAsync(cancellationToken)
            .WaitAsync(cancellationToken);

        await Task.WhenAll(
            App.ResourceNotifications.WaitForResourceHealthyAsync(Services.WebApi, cancellationToken).WaitAsync(cancellationToken),
            App.ResourceNotifications.WaitForResourceHealthyAsync(Services.WebFrontend, cancellationToken).WaitAsync(cancellationToken));
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await App.DisposeAsync();
    }
}
