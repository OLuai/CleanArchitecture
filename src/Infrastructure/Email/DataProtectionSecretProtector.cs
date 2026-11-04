using CleanArchitecture.Application.Common.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Email;

/// <summary>
/// <see cref="ISecretProtector"/> au-dessus d'ASP.NET Data Protection, dont l'anneau de clés est
/// persisté en base — voir <c>PersistKeysToDbContext</c>. Un secret chiffré survit donc aux
/// redéploiements et reste lisible par toutes les instances.
/// </summary>
public sealed class DataProtectionSecretProtector : ISecretProtector
{
    /// <summary>
    /// La finalité isole ce chiffrement des autres usages de Data Protection : un cookie
    /// d'authentification ne peut pas être présenté ici comme un mot de passe, ni l'inverse.
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
            // Anneau de clés perdu, ou donnée abîmée. Journalisé sans la valeur, et l'appelant
            // échoue proprement : un envoi qui ne part pas vaut mieux qu'un démarrage qui casse.
            _logger.LogError(ex, "A stored secret could not be decrypted; the key ring may have changed");
            return null;
        }
    }
}
