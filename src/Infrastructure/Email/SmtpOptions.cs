using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Infrastructure.Email;

/// <summary>
/// SMTP settings provided by the deployment. <c>Smtp</c> section, hence environment
/// variables: <c>Smtp__Host</c>, <c>Smtp__Port</c>, <c>Smtp__Security</c>,
/// <c>Smtp__UserName</c>, <c>Smtp__Password</c>, <c>Smtp__FromAddress</c>, <c>Smtp__FromName</c>.
/// </summary>
public class SmtpOptions : IEmailConfiguration
{
    public const string Section = "Smtp";

    public string? Host { get; set; }

    /// <summary>587 by default: the submission port, used with STARTTLS.</summary>
    public int Port { get; set; } = 587;

    /// <summary><c>StartTls</c>, <c>Tls</c> or <c>None</c>, literally.</summary>
    public SmtpSecurity Security { get; set; } = SmtpSecurity.StartTls;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string? FromAddress { get; set; }

    public string? FromName { get; set; } = "CleanArchitecture";

    /// <summary>Beyond this, give up rather than let a request wait on a silent relay.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);
}
