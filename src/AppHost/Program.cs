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

var migration = builder.AddProject<Projects.Migration>(Services.Migration)
    .WithReference(postgres)
    .WaitFor(postgres)
    // Migration is a worker: Host.CreateApplicationBuilder resolves its environment from
    // DOTNET_ENVIRONMENT, but Aspire only forwards ASPNETCORE_ENVIRONMENT to child projects.
    // Without this the worker always came up as Production, so DevelopmentSeeder never ran.
    .WithEnvironment("DOTNET_ENVIRONMENT", builder.Environment.EnvironmentName);

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
    .WithAspNetCoreEnvironment()
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar API Reference";
        url.Url = "/scalar";
    });

if (builder.ExecutionContext.IsRunMode)
{
    // Development mailbox: receives everything the application sends and shows it on its web UI,
    // linked from the dashboard. Passed to the Web as Smtp__* configuration, so /admin/email always
    // has an environment source on a development machine; a row saved from that screen overrides
    // it, exactly as in production. Shared across solutions like PostgreSQL (see
    // Services.Shared), on fixed host ports so a test plan can name the address. In publish mode
    // none of this: the deployment brings its own relay.
    var mailpit = builder.AddMailPit(Services.Mailpit,
            httpPort: Services.Shared.MailpitHttpHostPort,
            smtpPort: Services.Shared.MailpitSmtpHostPort)
        .WithContainerName(Services.Shared.MailpitContainer)
        .WithLifetime(ContainerLifetime.Persistent)
        .WithUrlForEndpoint("http", url => url.DisplayText = "Mailpit");

    var smtp = mailpit.GetEndpoint("smtp");

    // Mailpit does not offer STARTTLS and SmtpEmailSender never negotiates it, so without "None"
    // the connection would fail. The value is the literal name of Domain's SmtpSecurity, which the
    // AppHost does not reference.
    web.WithEnvironment("Smtp__Host", ReferenceExpression.Create($"{smtp.Property(EndpointProperty.Host)}"))
        .WithEnvironment("Smtp__Port", ReferenceExpression.Create($"{smtp.Property(EndpointProperty.Port)}"))
        .WithEnvironment("Smtp__Security", "None")
        .WithEnvironment("Smtp__FromAddress", "noreply@cleanarchitecture.test")
        .WaitFor(mailpit);
#if (!UseApiOnly)

    builder.AddJavaScriptApp(Services.WebFrontend, "./../Web/ClientApp")
        .WithRunScript("start")
        .WithReference(web)
        .WaitFor(web)
        .WithHttpEndpoint(env: "PORT")
        .WithExternalHttpEndpoints();
#endif
}

builder.Build().Run();
