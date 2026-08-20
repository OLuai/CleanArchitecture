using CleanArchitecture.Shared;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("aca-env");

// Every solution generated from this template shares one PostgreSQL container and one pgAdmin,
// so they must all present the same password. A machine-wide environment variable is what makes
// that possible: user secrets are keyed by UserSecretsId, which dotnet new renames per project,
// so each solution would hold a different password while the container keeps the one it was
// first initialised with.
//
// Without the variable, fall back to the prompted parameter — fine for a single solution, but
// the first one to run decides the container's password for all the others.
var sharedPassword = Environment.GetEnvironmentVariable(Services.Shared.PostgresPasswordEnvVar);

var postgresPassword = string.IsNullOrWhiteSpace(sharedPassword)
    ? builder.AddParameter(Services.PostgresPasswordParameter, secret: true)
    : builder.AddParameter(Services.PostgresPasswordParameter, sharedPassword, secret: true);

// Container and volume names carry no project name, so a second solution reuses this container
// instead of starting a rival one on the same host ports. Each solution still gets its own
// database inside it (Services.Database is renamed per project).
var postgres = builder
    .AddPostgres(Services.PostgresServer, password: postgresPassword)
    .WithContainerName(Services.Shared.PostgresContainer)
    .WithHostPort(Services.Shared.PostgresHostPort)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume(Services.Shared.PostgresDataVolume)
    .WithPgAdmin(pgAdmin => pgAdmin
        .WithContainerName(Services.Shared.PgAdminContainer)
        .WithHostPort(Services.Shared.PgAdminHostPort)
        .WithLifetime(ContainerLifetime.Persistent))
    .AddDatabase(Services.Database);

// The environment must be forwarded explicitly: this worker is a Host.CreateApplicationBuilder
// host, so it reads DOTNET_ENVIRONMENT, not ASPNETCORE_ENVIRONMENT. Without it the worker runs in
// Production even during local development, which picks the ProductionSeeder and skips the sample
// data — and turns off the DI validation that would have caught a broken service graph.
var migration = builder.AddProject<Projects.Migration>(Services.Migration)
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithHostEnvironment();

// Configuration is injected here rather than duplicated across appsettings.<Environment>.json:
// Aspire owns the topology, so it also owns the values that describe it. Use the double
// underscore to reach into a configuration section, and pass endpoints from other resources
// instead of hard-coding URLs.
//
//   var storage = builder.AddContainer("storage", "some/image:tag")
//       .WithHttpEndpoint(port: 8333, targetPort: 8333, name: "api")
//       .WithVolume("cleanarchitecture-storage-data", "/data");   // renamed per project
//
//   var web = builder.AddProject<Projects.Web>(Services.WebApi)
//       .WithEnvironment("Storage__Provider", "SomeProvider")
//       .WithEnvironment("Storage__Endpoint", storage.GetEndpoint("api"))
//       .WaitFor(storage)
//
// Secrets go through builder.AddParameter(name, secret: true), which prompts once and stores
// the value in this project's user secrets, never in the repository. Use the environment-variable
// pattern above instead only for values that must be identical across every generated solution.
var web = builder.AddProject<Projects.Web>(Services.WebApi)
    .WithReference(postgres)
    .WaitForCompletion(migration)
    .WithExternalHttpEndpoints()
    .WithHostEnvironment()
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar API Reference";
        url.Url = "/scalar";
    });

#if (!UseApiOnly)
if (builder.ExecutionContext.IsRunMode)
{
    builder.AddJavaScriptApp(Services.WebFrontend, "./../Web/ClientApp")
        .WithRunScript("start")
        .WithReference(web)
        .WaitFor(web)
        .WithHttpEndpoint(env: "PORT")
        .WithExternalHttpEndpoints();
}
#endif

builder.Build().Run();
