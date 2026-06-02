namespace CleanArchitecture.Web.AcceptanceTests.Pages;

public class LoginPage(IPage page) : BasePage(page)
{
    public override string PagePath => $"{BaseUrl}/login";

    public Task SetLogin(string login)
        => Page.FillAsync("#login", login);

    public Task SetPassword(string password)
        => Page.FillAsync("#password", password);

    public Task ClickLogin()
        => Page.Locator("button[type='submit']").ClickAsync();

    public Task<string?> LogoutButtonText()
        => Page.Locator("button:has-text('Log out')").TextContentAsync();

    public Task AssertErrorVisible()
        => Assertions.Expect(Page.Locator("#login-error")).ToBeVisibleAsync();
}
