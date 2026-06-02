namespace CleanArchitecture.Web.AcceptanceTests.Pages;

public class CounterPage(IPage page) : BasePage(page)
{
    public override string PagePath => $"{BaseUrl}/counter";

    public Task AssertHeading(string text)
        => Assertions.Expect(Page.Locator("[data-slot='card-title']")).ToHaveTextAsync(text);

    public Task AssertCurrentCount(int count)
        => Assertions.Expect(Page.Locator("span[aria-live='polite']")).ToHaveTextAsync(count.ToString());

    public Task ClickIncrement()
        => Page.Locator("button[aria-label='Increment']").ClickAsync();
}
