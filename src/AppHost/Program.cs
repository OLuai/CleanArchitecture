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
