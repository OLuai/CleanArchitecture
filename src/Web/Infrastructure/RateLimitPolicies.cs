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

    /// <summary>
    /// Fixed window, partitioned by user (falling back to the remote IP), for endpoints where every
    /// call has an outside cost: a real email through the relay, a paid API. A button that can be
    /// hammered must not be able to burn a quota or a sending domain's reputation.
    /// </summary>
    public const string Costly = "costly";
}
