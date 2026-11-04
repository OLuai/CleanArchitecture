using CleanArchitecture.Application.Common.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Email;

/// <summary>
/// <see cref="ISecretProtector"/> on top of ASP.NET Data Protection, whose key ring is
/// persisted in the database — see <c>PersistKeysToDbContext</c>. An encrypted secret therefore survives
/// redeployments and stays readable by every instance.
/// </summary>
public sealed class DataProtectionSecretProtector : ISecretProtector
{
    /// <summary>
    /// The purpose isolates this encryption from other Data Protection uses: an authentication
    /// cookie cannot be presented here as a password, nor the reverse.
    /// </summary>
    private const string Purpose = "CleanArchitecture.Settings.Secret.v1";

    private readonly IDataProtector _protector;
    private readonly ILogger<DataProtectionSecretProtector> _logger;

    public DataProtectionSecretProtector(
        IDataProtectionProvider provider, ILogger<DataProtectionSecretProtector> logger)
    {
        _protector = provider.CreateProtector(Purpose);
        _logger = logger;
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string? Unprotect(string protectedValue)
    {
        try
        {
            return _protector.Unprotect(protectedValue);
        }
        catch (Exception ex)
        {
            // Lost key ring, or corrupted data. Logged without the value, and the caller
            // fails cleanly: a send that does not go out beats a startup that breaks.
            _logger.LogError(ex, "A stored secret could not be decrypted; the key ring may have changed");
            return null;
        }
    }
}
