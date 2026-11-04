using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;

namespace CleanArchitecture.Application.Settings.Commands.SendTestEmail;

/// <summary>Ce que le relais a répondu, tel quel.</summary>
public record TestEmailResultDto
{
    public required bool Sent { get; init; }

    /// <summary>
    /// L'erreur brute du relais. Rendue à l'écran sans reformulation : « échec » tout court
    /// n'apprend rien à qui règle un SMTP, alors que « 535 authentication failed » désigne le
    /// champ à corriger.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>Adresse effectivement destinataire, pour que l'administrateur sache où regarder.</summary>
    public required string SentTo { get; init; }
}

/// <summary>
/// Envoie un message d'essai à l'adresse du compte connecté.
/// <para>
/// L'adresse n'est pas un paramètre, et ce n'est pas une simplification : un point d'entrée qui
/// envoie un message arbitraire à une adresse arbitraire, via le relais du client, est un relais
/// de spam avec sa réputation d'expéditeur en jeu. S'envoyer un message à soi-même suffit
/// entièrement à vérifier un réglage.
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
