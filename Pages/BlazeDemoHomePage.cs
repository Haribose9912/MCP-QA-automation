using Microsoft.Playwright;

namespace PlaywrightMcpServer.Tests;

public sealed class BlazeDemoHomePage(IPage page)
{
    public const string HomeUrl = "https://blazedemo.com/";

    public ILocator DepartureCity => page.Locator("select[name='fromPort']");

    public ILocator DestinationCity => page.Locator("select[name='toPort']");

    public ILocator SearchButton => page.GetByRole(AriaRole.Button, new() { Name = "Find Flights" });

    public ILocator DestinationOfTheWeekLink =>
        page.GetByRole(AriaRole.Link, new() { Name = "destination of the week! The Beach!" });

    public ILocator TravelTheWorldLink =>
        page.GetByRole(AriaRole.Link, new() { Name = "Travel The World" });

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

    public async Task OpenVacationPageAsync()
    {
        await DestinationOfTheWeekLink.ClickAsync();
        await page.WaitForURLAsync("**/vacation.html");
    }
}
