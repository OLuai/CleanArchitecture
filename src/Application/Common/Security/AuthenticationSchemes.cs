namespace CleanArchitecture.Application.Common.Security;

public static class AuthenticationSchemes
{
    /// <summary>
    /// Policy scheme that dispatches to bearer-token authentication when the request carries an
    /// <c>Authorization: Bearer</c> header, and to the Identity application cookie otherwise.
    /// Lets the SPA stay on cookies while non-browser clients use tokens, with one pipeline.
    /// </summary>
    public const string CookieOrBearer = "CookieOrBearer";
}
