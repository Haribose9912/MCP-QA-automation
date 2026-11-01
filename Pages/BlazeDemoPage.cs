using Microsoft.Playwright;

namespace PlaywrightMcpServer.Tests;

public sealed class BlazeDemoPage(IPage page)
{
    public const string HomeUrl = "https://blazedemo.com/";

    public ILocator DepartureCity => page.Locator("select[name='fromPort']");

    public ILocator DestinationCity => page.Locator("select[name='toPort']");

    public ILocator SearchButton => page.GetByRole(AriaRole.Button, new() { Name = "Find Flights" });

    public async Task<IResponse?> OpenAsync()
    {
        return await page.GotoAsync(HomeUrl);
    }

    public async Task SelectCitiesAsync(string departure, string destination)
    {
        await DepartureCity.SelectOptionAsync(departure);
        await DestinationCity.SelectOptionAsync(destination);
    }

    public async Task SearchFlightsAsync(string departure, string destination)
    {
        await OpenAsync();
        await SelectCitiesAsync(departure, destination);
        await SearchButton.ClickAsync();
        await page.WaitForURLAsync("**/reserve.php");
    }

    public async Task ChooseFirstFlightAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Choose This Flight" }).First.ClickAsync();
        await page.WaitForURLAsync("**/purchase.php");
    }

    public async Task PurchaseTestFlightAsync()
    {
        await page.Locator("#inputName").FillAsync("Demo Traveler");
        await page.Locator("#address").FillAsync("123 Test Street");
        await page.Locator("#city").FillAsync("Test City");
        await page.Locator("#state").FillAsync("CA");
        await page.Locator("#zipCode").FillAsync("90210");
        await page.Locator("#cardType").SelectOptionAsync("visa");
        await page.Locator("#creditCardNumber").FillAsync("4111111111111111");
        await page.Locator("#creditCardMonth").FillAsync("12");
        await page.Locator("#creditCardYear").FillAsync("2030");
        await page.Locator("#nameOnCard").FillAsync("Demo Traveler");
        await page.GetByRole(AriaRole.Button, new() { Name = "Purchase Flight" }).ClickAsync();
        await page.WaitForURLAsync("**/confirmation.php");
    }
}
