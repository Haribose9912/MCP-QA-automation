using Microsoft.Playwright;
using Xunit;

namespace PlaywrightMcpServer.Tests;

[Collection(BrowserTestsCollection.Name)]
public sealed class BlazeDemoTests(BlazeDemoFixture fixture, ExtentReportFixture report) : IAsyncLifetime
{
    private IPage _page = null!;
    private BlazeDemoHomePage _homePage = null!;
    private BlazeDemoFlightResultsPage _resultsPage = null!;
    private BlazeDemoPurchasePage _purchasePage = null!;
    private BlazeDemoConfirmationPage _confirmationPage = null!;
    private BlazeDemoVacationPage _vacationPage = null!;

    public async Task InitializeAsync()
    {
        _page = await fixture.Browser.NewPageAsync();
        _homePage = new BlazeDemoHomePage(_page);
        _resultsPage = new BlazeDemoFlightResultsPage(_page);
        _purchasePage = new BlazeDemoPurchasePage(_page);
        _confirmationPage = new BlazeDemoConfirmationPage(_page);
        _vacationPage = new BlazeDemoVacationPage(_page);
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
            Assert.Equal(
                new[] { "Paris", "Philadelphia", "Boston", "Portland", "San Diego", "Mexico City", "São Paolo" },
                (await _homePage.DepartureCity.Locator("option").AllInnerTextsAsync())
                    .Select(city => city.Trim())
                    .ToArray());
            Assert.Equal(
                new[] { "Buenos Aires", "Rome", "London", "Berlin", "New York", "Dublin", "Cairo" },
                (await _homePage.DestinationCity.Locator("option").AllInnerTextsAsync())
                    .Select(city => city.Trim())
                    .ToArray());
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
    public async Task HomeNavigation_LinksReturnToTheFlightSearchPage()
    {
        await report.RunAsync(nameof(HomeNavigation_LinksReturnToTheFlightSearchPage), async () =>
        {
            await _homePage.OpenAsync();
            await _homePage.OpenVacationPageAsync();

            Assert.Contains("vacation.html", _page.Url, StringComparison.OrdinalIgnoreCase);
            var vacationContent = await _vacationPage.Body.InnerTextAsync();
            Assert.Contains("Beach", vacationContent, StringComparison.OrdinalIgnoreCase);
            await _vacationPage.ReturnHomeAsync();

            Assert.Contains("/home", _page.Url, StringComparison.OrdinalIgnoreCase);
            await _homePage.DepartureCity.WaitForAsync();
            await _homePage.DestinationCity.WaitForAsync();
        });
    }

    [Fact]
    public async Task BrandLink_OpensTheHomePageRoute()
    {
        await report.RunAsync(nameof(BrandLink_OpensTheHomePageRoute), async () =>
        {
            await _homePage.OpenAsync();
            await _homePage.TravelTheWorldLink.ClickAsync();
            await _page.WaitForURLAsync("**/index.php");

            await _homePage.DepartureCity.WaitForAsync();
            await _homePage.DestinationCity.WaitForAsync();
            Assert.Equal("BlazeDemo", await _page.TitleAsync());
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
            Assert.Equal(5, await _resultsPage.FlightResults.CountAsync());
            Assert.Equal(5, await _resultsPage.ChooseFlightButtons.CountAsync());
            Assert.Equal(5, await _resultsPage.FlightNumberCells.CountAsync());
            Assert.Equal(5, await _resultsPage.AirlineCells.CountAsync());
            Assert.Equal(5, await _resultsPage.PriceCells.CountAsync());
            var headers = await _resultsPage.ColumnHeaders.AllInnerTextsAsync();
            Assert.Contains("Flight #", headers);
            Assert.Contains("Airline", headers);
            Assert.Contains("Departs: Boston", headers);
            Assert.Contains("Arrives: London", headers);
            Assert.Contains("Price", headers);
            Assert.All(await _resultsPage.PriceCells.AllInnerTextsAsync(), price => Assert.Matches(@"^\$\d+\.\d{2}$", price.Trim()));
        });
    }

    [Fact]
    public async Task AlternateFlightSearch_ShowsSelectedRoute()
    {
        await report.RunAsync(nameof(AlternateFlightSearch_ShowsSelectedRoute), async () =>
        {
            await _homePage.SearchFlightsAsync("San Diego", "Dublin");

            await _page.GetByRole(AriaRole.Heading, new() { Name = "Flights from San Diego to Dublin:" }).WaitForAsync();
            Assert.Equal(5, await _resultsPage.FlightResults.CountAsync());
        });
    }

    [Theory]
    [InlineData("Philadelphia", "Rome")]
    [InlineData("Portland", "Berlin")]
    [InlineData("Mexico City", "Cairo")]
    public async Task FlightSearch_DisplaysResultsForAdditionalRoutes(string departure, string destination)
    {
        await report.RunAsync($"{nameof(FlightSearch_DisplaysResultsForAdditionalRoutes)} [{departure} to {destination}]", async () =>
        {
            await _homePage.SearchFlightsAsync(departure, destination);

            await _page.GetByRole(AriaRole.Heading, new() { Name = $"Flights from {departure} to {destination}:" }).WaitForAsync();
            Assert.Equal(5, await _resultsPage.FlightResults.CountAsync());
            Assert.All(await _resultsPage.FlightNumberCells.AllInnerTextsAsync(), number => Assert.False(string.IsNullOrWhiteSpace(number)));
            Assert.All(await _resultsPage.AirlineCells.AllInnerTextsAsync(), airline => Assert.False(string.IsNullOrWhiteSpace(airline)));
        });
    }

    [Fact]
    public async Task FlightResults_ChoosingSpecificFlightOpensPurchaseForm()
    {
        await report.RunAsync(nameof(FlightResults_ChoosingSpecificFlightOpensPurchaseForm), async () =>
        {
            await _homePage.SearchFlightsAsync("Boston", "London");
            await _resultsPage.ChooseFlightAsync(2);

            Assert.True(await _purchasePage.Form.IsVisibleAsync());
            Assert.Contains("Your flight from", await _page.GetByRole(AriaRole.Heading).First.InnerTextAsync());
            Assert.Contains("Total Cost:", await _page.Locator("body").InnerTextAsync());
        });
    }

    [Fact]
    public async Task ChoosingFlight_OpensPassengerAndPaymentForm()
    {
        await report.RunAsync(nameof(ChoosingFlight_OpensPassengerAndPaymentForm), async () =>
        {
            await _homePage.SearchFlightsAsync("Paris", "Rome");
            await _resultsPage.ChooseFlightAsync(0);

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

            await _purchasePage.CardType.WaitForAsync();
            Assert.Equal(
                new[] { "Visa", "American Express", "Diner's Club" },
                (await _purchasePage.CardType.Locator("option").AllInnerTextsAsync())
                    .Select(option => option.Trim())
                    .ToArray());
            await _purchasePage.PurchaseButton.WaitForAsync();
        });
    }

    [Theory]
    [InlineData("visa")]
    [InlineData("amex")]
    [InlineData("dinersclub")]
    public async Task PurchaseForm_AllowsSelectingEachCardType(string cardType)
    {
        await report.RunAsync($"{nameof(PurchaseForm_AllowsSelectingEachCardType)} [{cardType}]", async () =>
        {
            await _homePage.SearchFlightsAsync("Paris", "Rome");
            await _resultsPage.ChooseFlightAsync(0);
            await _purchasePage.CardType.SelectOptionAsync(cardType);

            Assert.Equal(cardType, await _purchasePage.CardType.InputValueAsync());
        });
    }

    [Fact]
    public async Task PurchaseFlight_ShowsConfirmationWithMaskedCard()
    {
        await report.RunAsync(nameof(PurchaseFlight_ShowsConfirmationWithMaskedCard), async () =>
        {
            await _homePage.SearchFlightsAsync("Boston", "Cairo");
            await _resultsPage.ChooseFlightAsync(0);
            await _purchasePage.PurchaseTestFlightAsync();

            Assert.Equal("https://blazedemo.com/confirmation.php", _page.Url);
            await _confirmationPage.Heading.WaitForAsync();
            var confirmation = await _confirmationPage.Body.InnerTextAsync();
            Assert.Contains("PendingCapture", confirmation);
            Assert.Contains("xxxxxxxxxxxx1111", confirmation);
            Assert.DoesNotContain("4111111111111111", confirmation);
            Assert.Contains("Auth Code", confirmation);
        });
    }
}
