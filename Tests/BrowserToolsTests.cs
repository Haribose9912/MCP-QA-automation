using PlaywrightMcpServer;
using Xunit;

namespace PlaywrightMcpServer.Tests;

[CollectionDefinition("MCP Browser Tests", DisableParallelization = true)]
public sealed class BrowserTestsCollection :
    ICollectionFixture<BlazeDemoFixture>,
    ICollectionFixture<ExtentReportFixture>
{
    public const string Name = "MCP Browser Tests";
}

[Collection(BrowserTestsCollection.Name)]
public sealed class BrowserToolsTests(ExtentReportFixture report) : IAsyncLifetime
{
    private DemoPageServer _server = null!;

    public Task InitializeAsync()
    {
        _server = DemoPageServer.Start();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await BrowserTools.DisposeBrowserAsync();
        await _server.DisposeAsync();
    }

    [Fact]
    public async Task NavigateAsync_ReturnsPageTitleAndStatus()
    {
        await report.RunAsync(nameof(NavigateAsync_ReturnsPageTitleAndStatus), async () =>
        {
            var result = await BrowserTools.NavigateAsync(_server.Url);

            Assert.Contains($"URL: {_server.Url}", result);
            Assert.Contains("Title: Demo page", result);
            Assert.Contains("HTTP status: 200", result);
        });
    }

    [Fact]
    public async Task ReadPageAsync_ReturnsVisiblePageText()
    {
        await report.RunAsync(nameof(ReadPageAsync_ReturnsVisiblePageText), async () =>
        {
            await BrowserTools.NavigateAsync(_server.Url);

            var result = await BrowserTools.ReadPageAsync();

            Assert.Contains("Title: Demo page", result);
            Assert.Contains("Playwright MCP demo", result);
            Assert.Contains("A local page used by the demo tests.", result);
        });
    }

    [Fact]
    public async Task ScreenshotAsync_SavesPngInArtifactsDirectory()
    {
        await report.RunAsync(nameof(ScreenshotAsync_SavesPngInArtifactsDirectory), async () =>
        {
            await BrowserTools.NavigateAsync(_server.Url);

            var originalDirectory = Environment.CurrentDirectory;
            var temporaryDirectory = Path.Combine(Path.GetTempPath(), $"playwright-mcp-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(temporaryDirectory);

            try
            {
                Environment.CurrentDirectory = temporaryDirectory;
                var result = await BrowserTools.ScreenshotAsync();
                var screenshotPath = Path.Combine(temporaryDirectory, "artifacts", "browser-page.png");

                Assert.Contains(screenshotPath, result);
                Assert.True(File.Exists(screenshotPath));
                Assert.Equal(
                    new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 },
                    (await File.ReadAllBytesAsync(screenshotPath))[..8]);
            }
            finally
            {
                Environment.CurrentDirectory = originalDirectory;
                Directory.Delete(temporaryDirectory, recursive: true);
            }
        });
    }

    [Theory]
    [InlineData("file:///C:/private.txt")]
    [InlineData("javascript:alert(1)")]
    [InlineData("http://user:password@example.com/")]
    public async Task NavigateAsync_RejectsUnsupportedOrCredentialedUrls(string url)
    {
        await report.RunAsync($"NavigateAsync_RejectsUnsupportedOrCredentialedUrls [{url}]", async () =>
        {
            await Assert.ThrowsAsync<ArgumentException>(() => BrowserTools.NavigateAsync(url));
        });
    }
}
