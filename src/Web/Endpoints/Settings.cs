using CleanArchitecture.Application.Settings.Commands.ResetEmailSettings;
using CleanArchitecture.Application.Settings.Commands.SaveEmailSettings;
using CleanArchitecture.Application.Settings.Commands.SendTestEmail;
using CleanArchitecture.Application.Settings.Queries.GetEmailSettings;
using CleanArchitecture.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using DomainPermissions = CleanArchitecture.Domain.Constants.Permissions;

namespace CleanArchitecture.Web.Endpoints;

public class Settings : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetEmailSettings, "email")
            .RequireAuthorization($"Permission:{DomainPermissions.Settings.ManageEmail}");
        groupBuilder.MapPut(SaveEmailSettings, "email")
            .RequireAuthorization($"Permission:{DomainPermissions.Settings.ManageEmail}");
        groupBuilder.MapDelete(ResetEmailSettings, "email")
            .RequireAuthorization($"Permission:{DomainPermissions.Settings.ManageEmail}");

        // Each try sends a real message through the relay. Rate-limited: a button that can be
        // hammered must not be able to damage the sending domain's reputation.
        groupBuilder.MapPost(SendTestEmail, "email/test")
            .RequireAuthorization($"Permission:{DomainPermissions.Settings.ManageEmail}")
            .RequireRateLimiting(RateLimitPolicies.Costly);
    }

    [EndpointSummary("Read the email settings in force")]
    [EndpointDescription("Says which server is used and where the settings come from — this screen, or the deployment's Smtp__* variables. Never carries the password, in any form: it reports only whether one is set.")]
    public static async Task<Ok<EmailSettingsDto>> GetEmailSettings(ISender sender)
    {
        var settings = await sender.Send(new GetEmailSettingsQuery());

        return TypedResults.Ok(settings);
    }

    [EndpointSummary("Save the email settings")]
    [EndpointDescription("Stores them, overriding the deployment's. Omitting the password leaves the stored one untouched; sending an empty one removes it.")]
    public static async Task<NoContent> SaveEmailSettings(ISender sender, SaveEmailSettingsCommand command)
    {
        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Drop the saved email settings")]
    [EndpointDescription("Removes the stored row so the deployment's Smtp__* variables apply again.")]
    public static async Task<NoContent> ResetEmailSettings(ISender sender)
    {
        await sender.Send(new ResetEmailSettingsCommand());

        return TypedResults.NoContent();
    }

    [EndpointSummary("Send a test message")]
    [EndpointDescription("Sends to the caller's own address — deliberately not a parameter, so this cannot become a relay for sending arbitrary mail through the customer's SMTP account. Answers 200 with the relay's own error when it fails, since that is what says which field to fix.")]
    public static async Task<Ok<TestEmailResultDto>> SendTestEmail(ISender sender)
    {
        var result = await sender.Send(new SendTestEmailCommand());

        return TypedResults.Ok(result);
    }
}
