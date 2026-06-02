internal static class AspireExtensions
{
    public static IResourceBuilder<T> WithAspNetCoreEnvironment<T>(this IResourceBuilder<T> builder)
        where T : IResourceWithEnvironment
    {
        builder.WithEnvironment(context =>
        {
            var aspNetCoreEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var dotnetEnv = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

            var environment = aspNetCoreEnv ?? dotnetEnv ?? "Development";

            context.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = environment;
            context.EnvironmentVariables["DOTNET_ENVIRONMENT"] = environment;
        });

        return builder;
    }
}