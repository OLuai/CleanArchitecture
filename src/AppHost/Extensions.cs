internal static class AspireExtensions
{
    public static IResourceBuilder<T> WithAspNetCoreEnvironment<T>(this IResourceBuilder<T> builder)
        where T : IResourceWithEnvironment
    {
        builder.WithEnvironment(context =>
        {
            var aspNetCoreEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var dotnetEnv = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

            // Use the AppHost's environment if set. Otherwise default to Development
            // when running locally and Production when publishing or deploying.
            var environment = aspNetCoreEnv ?? dotnetEnv
                ?? (context.ExecutionContext.IsRunMode ? "Development" : "Production");

            context.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = environment;
            context.EnvironmentVariables["DOTNET_ENVIRONMENT"] = environment;
        });

        return builder;
    }
}