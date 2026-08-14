namespace CleanArchitecture.Web.Infrastructure;

/// <summary>
/// Names of the rate-limiting policies registered in <c>AddWebServices</c>.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>
    /// Fixed window, partitioned by remote IP address, for anonymous endpoints.
    /// </summary>
    public const string Public = "public";
}
