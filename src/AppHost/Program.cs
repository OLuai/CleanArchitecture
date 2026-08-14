using CleanArchitecture.Shared;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("aca-env");

var postgresPassword = builder.AddParameter(Services.PostgresPasswordParameter, secret: true);

var postgres = builder
    .AddPostgres(Services.PostgresServer, password: postgresPassword)
    .WithContainerName("cleanarchitecture-postgres")
    .WithHostPort(5431)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("cleanarchitecture-pg-data")
    .WithPgAdmin(pgAdmin => pgAdmin
        .WithContainerName("cleanarchitecture-pgadmin")
        .WithHostPort(5050)
        .WithLifetime(ContainerLifetime.Persistent))
    .AddDatabase(Services.Database);

var migration = builder.AddProject<Projects.Migration>(Services.Migration)
    .WithReference(postgres)
    .WaitFor(postgres);

// Configuration is injected here rather than duplicated across appsettings.<Environment>.json:
// Aspire owns the topology, so it also owns the values that describe it. Use the double
// underscore to reach into a configuration section, and pass endpoints from other resources
// instead of hard-coding URLs.
//
//   var storage = builder.AddContainer("storage", "some/image:tag")
//       .WithHttpEndpoint(port: 8333, targetPort: 8333, name: "api")
//       .WithVolume("cleanarchitecture-storage-data", "/data");
//
//   var web = builder.AddProject<Projects.Web>(Services.WebApi)
//       .WithEnvironment("Storage__Provider", "SomeProvider")
//       .WithEnvironment("Storage__Endpoint", storage.GetEndpoint("api"))
//       .WaitFor(storage)
//
// Secrets go through builder.AddParameter(name, secret: true) — as done for the Postgres
// password above — so they are prompted for once and stored in user secrets, never committed.
var web = builder.AddProject<Projects.Web>(Services.WebApi)
    .WithReference(postgres)
    .WaitForCompletion(migration)
    .WithExternalHttpEndpoints()
    .WithAspNetCoreEnvironment()
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar API Reference";
        url.Url = "/scalar";
    });

if (builder.ExecutionContext.IsRunMode)
{
    builder.AddJavaScriptApp(Services.WebFrontend, "./../Web/ClientApp")
        .WithRunScript("start")
        .WithReference(web)
        .WaitFor(web)
        .WithHttpEndpoint(env: "PORT")
        .WithExternalHttpEndpoints();
}

builder.Build().Run();
