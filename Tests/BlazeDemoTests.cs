using Microsoft.Playwright;
using Xunit;

namespace PlaywrightMcpServer.Tests;

[Collection(BrowserTestsCollection.Name)]
public sealed class BlazeDemoTests(BlazeDemoFixture fixture, ExtentReportFixture report) : IAsyncLifetime
{
    private IPage _page = null!;
    private BlazeDemoPage _homePage = null!;

    public async Task InitializeAsync()
    {
        _page = await fixture.Browser.NewPageAsync();
        _homePage = new BlazeDemoPage(_page);
    }

    public async Task DisposeAsync()
    {
        await _page.CloseAsync();
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task HomePage_ShowsFlightSearchAndCityOptions()
    {
        await report.RunAsync(nameof(HomePage_ShowsFlightSearchAndCityOptions), async () =>
        {
            var response = await _homePage.OpenAsync();

            Assert.Equal(200, response?.Status);
            Assert.Equal("BlazeDemo", await _page.TitleAsync());
            await _page.GetByRole(AriaRole.Heading, new() { Name = "Welcome to the Simple Travel Agency!" }).WaitForAsync();
            await _homePage.DepartureCity.WaitForAsync();
            await _homePage.DestinationCity.WaitForAsync();
            Assert.Contains("Paris", await _homePage.DepartureCity.InnerTextAsync());
            Assert.Contains("Buenos Aires", await _homePage.DestinationCity.InnerTextAsync());
            await _homePage.SearchButton.WaitForAsync();
            Assert.True(await _homePage.SearchButton.IsEnabledAsync());
        });
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task CitySelection_DisplaysSelectedDepartureAndDestination()
    {
        await report.RunAsync(nameof(CitySelection_DisplaysSelectedDepartureAndDestination), async () =>
        {
            await _homePage.OpenAsync();
            await _homePage.SelectCitiesAsync("Boston", "London");

            Assert.Equal("Boston", await _homePage.DepartureCity.InputValueAsync());
            Assert.Equal("London", await _homePage.DestinationCity.InputValueAsync());
        });
    }

    [Fact]
    [Trait("Category", "KnownIssue")]
    public async Task HomePage_ControlsAreUsableAndHaveNoPageErrors()
    {
        await report.RunAsync(nameof(HomePage_ControlsAreUsableAndHaveNoPageErrors), async () =>
        {
            var pageErrors = new List<string>();
            _page.PageError += (_, error) => pageErrors.Add(error);

            await _homePage.OpenAsync();
            Assert.True(await _homePage.DepartureCity.IsVisibleAsync());
            Assert.True(await _homePage.DestinationCity.IsVisibleAsync());
            Assert.True(await _homePage.SearchButton.IsVisibleAsync());
            Assert.True(await _homePage.SearchButton.IsEnabledAsync());
            await _homePage.SelectCitiesAsync("Boston", "London");
            await _homePage.SearchButton.ClickAsync();
            await _page.WaitForURLAsync("**/reserve.php");
            Assert.Empty(pageErrors);
        });
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task FlightSearch_ShowsResultsForSelectedCities()
    {
        await report.RunAsync(nameof(FlightSearch_ShowsResultsForSelectedCities), async () =>
        {
            await _homePage.SearchFlightsAsync("Boston", "London");

            Assert.Equal("https://blazedemo.com/reserve.php", _page.Url);
            await _page.GetByRole(AriaRole.Heading, new() { Name = "Flights from Boston to London:" }).WaitForAsync();
            Assert.Equal(5, await _page.Locator("table tbody tr").CountAsync());
            Assert.Equal(5, await _page.GetByRole(AriaRole.Button, new() { Name = "Choose This Flight" }).CountAsync());
        });
    }

    [Fact]
    public async Task AlternateFlightSearch_ShowsSelectedRoute()
    {
        await report.RunAsync(nameof(AlternateFlightSearch_ShowsSelectedRoute), async () =>
        {
            await _homePage.SearchFlightsAsync("San Diego", "Dublin");

            await _page.GetByRole(AriaRole.Heading, new() { Name = "Flights from San Diego to Dublin:" }).WaitForAsync();
            Assert.Equal(5, await _page.Locator("table tbody tr").CountAsync());
        });
    }

    [Fact]
    public async Task ChoosingFlight_OpensPassengerAndPaymentForm()
    {
        await report.RunAsync(nameof(ChoosingFlight_OpensPassengerAndPaymentForm), async () =>
        {
            await _homePage.SearchFlightsAsync("Paris", "Rome");
            await _homePage.ChooseFirstFlightAsync();

            Assert.Equal("https://blazedemo.com/purchase.php", _page.Url);
            await _page.GetByText("Please submit the form below to purchase the flight.").WaitForAsync();
            foreach (var field in new[]
            {
                "inputName", "address", "city", "state", "zipCode",
                "creditCardNumber", "creditCardMonth", "creditCardYear", "nameOnCard"
            })
            {
                await _page.Locator($"#{field}").WaitForAsync();
            }

            await _page.Locator("#cardType").WaitForAsync();
            await _page.GetByRole(AriaRole.Button, new() { Name = "Purchase Flight" }).WaitForAsync();
        });
    }

    [Fact]
    public async Task PurchaseFlight_ShowsConfirmationWithMaskedCard()
    {
        await report.RunAsync(nameof(PurchaseFlight_ShowsConfirmationWithMaskedCard), async () =>
        {
            await _homePage.SearchFlightsAsync("Boston", "Cairo");
            await _homePage.ChooseFirstFlightAsync();
            await _homePage.PurchaseTestFlightAsync();

            Assert.Equal("https://blazedemo.com/confirmation.php", _page.Url);
            await _page.GetByRole(AriaRole.Heading, new() { Name = "Thank you for your purchase today!" }).WaitForAsync();
            var confirmation = await _page.Locator("body").InnerTextAsync();
            Assert.Contains("PendingCapture", confirmation);
            Assert.Contains("xxxxxxxxxxxx1111", confirmation);
            Assert.DoesNotContain("4111111111111111", confirmation);
        });
    }
}
