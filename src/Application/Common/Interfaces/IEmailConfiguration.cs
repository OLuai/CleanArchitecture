using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.Common.Interfaces;

/// <summary>
/// The SMTP settings as the deployment provides them: the <c>Smtp</c> section of the
/// configuration, in practice the <c>Smtp__Host</c>, <c>Smtp__Port</c>, <c>Smtp__Security</c>,
/// <c>Smtp__UserName</c>, <c>Smtp__Password</c> and <c>Smtp__FromAddress</c> environment variables…
/// <para>
/// This is the baseline: a platform starts up configured without anyone opening the
/// administration screen. An <c>EmailSettings</c> row in the database then overrides it.
/// </para>
/// </summary>
public interface IEmailConfiguration
{
    string? Host { get; }

    int Port { get; }

    SmtpSecurity Security { get; }

    string? UserName { get; }

    /// <summary>
    /// Plain text, because it comes from the environment. It never leaves through the API: the DTO only
    /// carries a boolean saying that it exists.
    /// </summary>
    string? Password { get; }

    string? FromAddress { get; }

    string? FromName { get; }
}
