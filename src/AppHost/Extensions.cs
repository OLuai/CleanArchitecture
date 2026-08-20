internal static class AspireExtensions
{
    /// <summary>
    /// Propagates the AppHost's own environment to a child resource.
    /// <para>
    /// Both variables are set on purpose: an ASP.NET Core host reads <c>ASPNETCORE_ENVIRONMENT</c>,
    /// but a worker built with <c>Host.CreateApplicationBuilder</c> reads <c>DOTNET_ENVIRONMENT</c>
    /// and ignores the ASP.NET one. A resource given only <c>ASPNETCORE_ENVIRONMENT</c> therefore
    /// starts in Production while the AppHost runs in Development — silently, since nothing fails.
    /// </para>
    /// </summary>
    public static IResourceBuilder<T> WithHostEnvironment<T>(this IResourceBuilder<T> builder)
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
