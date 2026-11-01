using Microsoft.Playwright;

namespace PlaywrightMcpServer.Tests;

public sealed class BlazeDemoFlightResultsPage(IPage page)
{
    public ILocator ColumnHeaders => page.Locator("table thead th");

    public ILocator FlightResults => page.Locator("table tbody tr");

    public ILocator FlightNumberCells => page.Locator("table tbody tr td:nth-child(2)");

    public ILocator AirlineCells => page.Locator("table tbody tr td:nth-child(3)");

    public ILocator PriceCells => page.Locator("table tbody tr td:nth-child(6)");

    public ILocator ChooseFlightButtons =>
        page.GetByRole(AriaRole.Button, new() { Name = "Choose This Flight" });

    public async Task ChooseFlightAsync(int index)
    {
        await ChooseFlightButtons.Nth(index).ClickAsync();
        await page.WaitForURLAsync("**/purchase.php");
    }
}
