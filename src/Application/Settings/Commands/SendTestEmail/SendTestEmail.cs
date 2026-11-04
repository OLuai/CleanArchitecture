using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;

namespace CleanArchitecture.Application.Settings.Commands.SendTestEmail;

/// <summary>What the relay answered, as-is.</summary>
public record TestEmailResultDto
{
    public required bool Sent { get; init; }

    /// <summary>
    /// The raw error from the relay. Shown on screen without rewording: a plain "failed"
    /// tells nothing to whoever is tuning an SMTP setup, whereas "535 authentication failed" points to the
    /// field to fix.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>Address that actually received the message, so the administrator knows where to look.</summary>
    public required string SentTo { get; init; }
}

/// <summary>
/// Sends a test message to the signed-in account's address.
/// <para>
/// The address is not a parameter, and that is not a simplification: an endpoint that
/// sends an arbitrary message to an arbitrary address, through the customer's relay, is a spam
/// relay with the customer's sender reputation at stake. Sending a message to oneself is enough
/// to verify a setting.
/// </para>
/// </summary>
[Authorize(Permissions = Permissions.Settings.ManageEmail)]
public record SendTestEmailCommand : IRequest<TestEmailResultDto>;

public class SendTestEmailCommandHandler : IRequestHandler<SendTestEmailCommand, TestEmailResultDto>
{
    private readonly IEmailSender _sender;
    private readonly IIdentityService _identityService;
    private readonly IUser _user;

    public SendTestEmailCommandHandler(
        IEmailSender sender, IIdentityService identityService, IUser user)
    {
        _sender = sender;
        _identityService = identityService;
        _user = user;
    }

    public async Task<TestEmailResultDto> Handle(
        SendTestEmailCommand request, CancellationToken cancellationToken)
    {
        if (!await _sender.IsConfiguredAsync(cancellationToken))
        {
            throw new FriendlyException(
                "No mail relay is configured. Enter a server before sending a test.",
                title: "Cannot send",
                errorCode: "EmailNotConfigured",
                statusCode: 409);
        }

        var me = await _identityService.GetUserAsync(_user.Id!, cancellationToken);
        var address = me?.Email;

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new FriendlyException(
                "Your account has no email address, so the test message has nowhere to go.",
                title: "Cannot send",
                errorCode: "NoRecipientAddress",
                statusCode: 409);
        }

        var result = await _sender.SendAsync(
            address,
            "CleanArchitecture — test message",
            """
            <p>This message confirms that CleanArchitecture email sending is working.</p>
            <p>You can ignore it.</p>
            """,
            cancellationToken);

        return new TestEmailResultDto
        {
            Sent = result.Sent,
            Error = result.Error,
            SentTo = address
        };
    }
}
