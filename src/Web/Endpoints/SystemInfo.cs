using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Hosting;
using CleanArchitecture.Web.Infrastructure;

namespace CleanArchitecture.Web.Endpoints;

/// <summary>
/// Anonymous endpoint letting the SPA identify the running environment so it can show a
/// "Staging" or "Development" banner. The server is the source of truth — no Vite build-time
/// variable — which means one Docker image can serve both Production and Staging.
/// </summary>
public class SystemInfo : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetSystemInfo)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Public);
    }

    [EndpointSummary("Get system info")]
    [EndpointDescription("Returns the current environment name (Development / Staging / Production). Anonymous; consumed by the SPA to display a test-environment banner.")]
    public static Ok<SystemInfoResponse> GetSystemInfo(IHostEnvironment environment)
        => TypedResults.Ok(new SystemInfoResponse(environment.EnvironmentName));
}

public record SystemInfoResponse(string Environment);
