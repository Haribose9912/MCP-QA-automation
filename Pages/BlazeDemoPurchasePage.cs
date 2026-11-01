using Microsoft.Playwright;

namespace PlaywrightMcpServer.Tests;

public sealed class BlazeDemoPurchasePage(IPage page)
{
    public ILocator Form => page.Locator("form[action*='confirmation.php']");

    public ILocator CardType => page.Locator("#cardType");

    public ILocator PurchaseButton =>
        page.GetByRole(AriaRole.Button, new() { Name = "Purchase Flight" });

    public async Task PurchaseTestFlightAsync()
    {
        await page.Locator("#inputName").FillAsync("Demo Traveler");
        await page.Locator("#address").FillAsync("123 Test Street");
        await page.Locator("#city").FillAsync("Test City");
        await page.Locator("#state").FillAsync("CA");
        await page.Locator("#zipCode").FillAsync("90210");
        await CardType.SelectOptionAsync("visa");
        await page.Locator("#creditCardNumber").FillAsync("4111111111111111");
        await page.Locator("#creditCardMonth").FillAsync("12");
        await page.Locator("#creditCardYear").FillAsync("2030");
        await page.Locator("#nameOnCard").FillAsync("Demo Traveler");
        await PurchaseButton.ClickAsync();
        await page.WaitForURLAsync("**/confirmation.php");
    }
}
