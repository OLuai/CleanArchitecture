using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Settings.Commands.ResetEmailSettings;
using CleanArchitecture.Application.Settings.Commands.SaveEmailSettings;
using CleanArchitecture.Application.Settings.Commands.SendTestEmail;
using CleanArchitecture.Application.Settings.Queries.GetEmailSettings;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.FunctionalTests.Settings;

public class EmailSettingsTests : TestBase
{
    private const string Secret = "un-mot-de-passe-de-relais";

    private static SaveEmailSettingsCommand Valid() => new()
    {
        Host = "smtp.example.invalid",
        Port = 587,
        Security = SmtpSecurity.StartTls,
        UserName = "cleanarchitecture",
        Password = Secret,
        FromAddress = "ne-pas-repondre@cleanarchitecture.invalid",
        FromName = "CLEANARCHITECTURE"
    };

    [Test]
    public async Task ShouldRequireAHostAPortAndASenderAddress()
    {
        await Should.ThrowAsync<ValidationException>(
            () => TestApp.SendAsync(Valid() with { Host = "" }));

        await Should.ThrowAsync<ValidationException>(
            () => TestApp.SendAsync(Valid() with { Port = 0 }));

        await Should.ThrowAsync<ValidationException>(
            () => TestApp.SendAsync(Valid() with { FromAddress = "pas-une-adresse" }));
    }

    [Test]
    public async Task ShouldNeverStoreTheRelayPasswordInClear()
    {
        // The point of ISecretProtector. A database dump, a backup or a read-only support account
        // must not hand over credentials that can send mail as the platform.
        await TestApp.SendAsync(Valid());

        var stored = await TestApp.FindAsync<EmailSettings>(EmailSettings.SingletonId);

        stored.ShouldNotBeNull();
        stored!.ProtectedPassword.ShouldNotBeNull();
        stored.ProtectedPassword.ShouldNotContain(Secret);
    }

    [Test]
    public async Task ShouldNeverReturnThePasswordToTheClient()
    {
        await TestApp.SendAsync(Valid());

        var settings = await TestApp.SendAsync(new GetEmailSettingsQuery());

        // Only the fact that one exists.
        settings.HasPassword.ShouldBeTrue();

        var json = System.Text.Json.JsonSerializer.Serialize(settings);
        json.ShouldNotContain(Secret);

        // The structural guarantee behind that: the DTO carries no property that could hold a
        // password value, encrypted or otherwise, so adding one trips this test rather than
        // quietly publishing the credential. HasPassword is a bool and says nothing.
        typeof(EmailSettingsDto).GetProperties()
            .Where(p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Name)
            .ShouldBe([nameof(EmailSettingsDto.HasPassword)]);
    }

    [Test]
    public async Task ShouldKeepTheStoredPasswordWhenTheFieldIsLeftAlone()
    {
        // The screen cannot pre-fill the field — it never received the value — so saving the form
        // untouched must not wipe the credential.
        await TestApp.SendAsync(Valid());
        var before = (await TestApp.FindAsync<EmailSettings>(EmailSettings.SingletonId))!.ProtectedPassword;

        await TestApp.SendAsync(Valid() with { Password = null, Host = "smtp2.example.invalid" });

        var after = await TestApp.FindAsync<EmailSettings>(EmailSettings.SingletonId);
        after!.Host.ShouldBe("smtp2.example.invalid");
        after.ProtectedPassword.ShouldBe(before);
    }

    [Test]
    public async Task ShouldRemoveThePasswordWhenAnEmptyOneIsSent()
    {
        // Some internal relays take no credentials at all, so there has to be a way back to none.
        await TestApp.SendAsync(Valid());

        await TestApp.SendAsync(Valid() with { Password = "" });

        var stored = await TestApp.FindAsync<EmailSettings>(EmailSettings.SingletonId);
        stored!.ProtectedPassword.ShouldBeNull();
    }

    [Test]
    public async Task ShouldKeepASingleConfiguration()
    {
        await TestApp.SendAsync(Valid());
        await TestApp.SendAsync(Valid() with { Host = "smtp3.example.invalid" });

        (await TestApp.CountAsync<EmailSettings>()).ShouldBe(1);
    }

    [Test]
    public async Task ShouldSayWhereTheSettingsComeFrom()
    {
        // Nothing saved and nothing in the test host's configuration.
        var none = await TestApp.SendAsync(new GetEmailSettingsQuery());
        none.Source.ShouldBe(EmailSettingsSource.None);

        // A blank form still opens on a plausible starting point. SmtpSecurity.None is the zero
        // value, so left to the type's default an untouched form would offer "no encryption" —
        // the worst of the three to suggest.
        none.Port.ShouldBe(587);
        none.Security.ShouldBe(SmtpSecurity.StartTls);

        await TestApp.SendAsync(Valid());

        var saved = await TestApp.SendAsync(new GetEmailSettingsQuery());
        saved.Source.ShouldBe(EmailSettingsSource.Database);
        saved.Host.ShouldBe("smtp.example.invalid");
    }

    [Test]
    public async Task ShouldFallBackToTheDeploymentWhenTheSavedRowIsRemoved()
    {
        await TestApp.SendAsync(Valid());
        await TestApp.SendAsync(new ResetEmailSettingsCommand());

        (await TestApp.CountAsync<EmailSettings>()).ShouldBe(0);

        // The way out of a saved row silently masking updated environment variables.
        (await TestApp.SendAsync(new GetEmailSettingsQuery())).Source.ShouldBe(EmailSettingsSource.None);
    }

    [Test]
    public async Task ShouldRefuseToTestWhenNothingIsConfigured()
    {
        var exception = await Should.ThrowAsync<FriendlyException>(
            () => TestApp.SendAsync(new SendTestEmailCommand()));

        exception.ErrorCode.ShouldBe("EmailNotConfigured");
    }

    [Test]
    public async Task ShouldReportTheRelaysOwnErrorRatherThanThrow()
    {
        // smtp.example.invalid does not resolve, so this exercises the failure path for real.
        // "It did not work" teaches nothing to someone configuring SMTP; the relay's message does.
        await TestApp.SendAsync(Valid());

        var result = await TestApp.SendAsync(new SendTestEmailCommand());

        result.Sent.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task ShouldForbidUsersWithoutTheEmailPermission()
    {
        // Whoever holds this permission holds the relay credentials and can send as the application.
        await TestApp.RunAsUserAsync("user@local", "User1234!", [],
            Permissions.All.Where(p => p != Permissions.Settings.ManageEmail).ToArray());

        await Should.ThrowAsync<ForbiddenAccessException>(
            () => TestApp.SendAsync(new GetEmailSettingsQuery()));

        await Should.ThrowAsync<ForbiddenAccessException>(() => TestApp.SendAsync(Valid()));

        await Should.ThrowAsync<ForbiddenAccessException>(
            () => TestApp.SendAsync(new SendTestEmailCommand()));
    }
}
