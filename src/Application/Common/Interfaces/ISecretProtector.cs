namespace CleanArchitecture.Application.Common.Interfaces;

/// <summary>
/// Encrypts a sensitive value meant to be stored in the database — today the SMTP relay
/// password.
/// <para>
/// Relies on the ASP.NET Data Protection key ring, itself persisted in the database: the secret
/// therefore survives redeployments and stays readable by every instance, which a key generated
/// at startup would not allow.
/// </para>
/// <para>
/// <see cref="Unprotect"/> has a single legitimate caller, the email sender, when it
/// connects to the relay. No DTO carries a decrypted value.
/// </para>
/// </summary>
public interface ISecretProtector
{
    string Protect(string plaintext);

    /// <summary>
    /// Returns <c>null</c> if the value cannot be decrypted — lost key ring,
    /// corrupted data. A send then fails cleanly instead of throwing at startup.
    /// </summary>
    string? Unprotect(string protectedValue);
}
