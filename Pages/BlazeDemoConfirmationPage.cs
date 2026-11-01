using Microsoft.Playwright;

namespace PlaywrightMcpServer.Tests;

public sealed class BlazeDemoConfirmationPage(IPage page)
{
    public ILocator Heading =>
        page.GetByRole(AriaRole.Heading, new() { Name = "Thank you for your purchase today!" });

    public ILocator Body => page.Locator("body");
}
