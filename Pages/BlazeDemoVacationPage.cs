using Microsoft.Playwright;

namespace PlaywrightMcpServer.Tests;

public sealed class BlazeDemoVacationPage(IPage page)
{
    public ILocator Body => page.Locator("body");

    public ILocator HomeLink => page.GetByRole(AriaRole.Link, new() { Name = "home", Exact = true });

    public async Task ReturnHomeAsync()
    {
        await HomeLink.ClickAsync();
        await page.WaitForURLAsync("**/home");
    }
}
