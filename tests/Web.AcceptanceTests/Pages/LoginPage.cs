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

    /// <summary>
    /// Waits for the dashboard shell's user menu, which only renders once signed in — /login and
    /// /register are laid out without the shell.
    /// </summary>
    public Task AssertSignedIn()
        => Assertions.Expect(UserMenuTrigger).ToBeVisibleAsync();

    /// <summary>
    /// Opens the sidebar user menu and reads its log-out entry. Being signed in is what makes
    /// that menu exist at all, so finding the entry is the assertion.
    /// </summary>
    public async Task<string?> LogoutButtonText()
    {
        await UserMenuTrigger.ClickAsync();

        return await Page.Locator("[data-slot='dropdown-menu-item']:has-text('Log out')").TextContentAsync();
    }

    private ILocator UserMenuTrigger
        => Page.Locator("[data-slot='sidebar-footer'] [data-slot='dropdown-menu-trigger']");

    public Task AssertErrorVisible()
        => Assertions.Expect(Page.Locator("#login-error")).ToBeVisibleAsync();
}
