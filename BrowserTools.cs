using System.ComponentModel;
using Microsoft.Playwright;
using ModelContextProtocol.Server;

namespace PlaywrightMcpServer;

[McpServerToolType]
public sealed class BrowserTools
{
    private static readonly SemaphoreSlim BrowserLock = new(1, 1);
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;
    private static IPage? _page;

    [McpServerTool(Name = "browser_navigate"), Description("Open an HTTP or HTTPS page in the shared headless Chromium session.")]
    public static async Task<string> NavigateAsync(
        [Description("The absolute HTTP or HTTPS URL to open.")] string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException("Provide an absolute HTTP or HTTPS URL without embedded credentials.", nameof(url));
        }

        await BrowserLock.WaitAsync();
        try
        {
            var page = await GetPageAsync();
            var response = await page.GotoAsync(uri.AbsoluteUri, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30_000
            });

            return $"URL: {page.Url}\nTitle: {await page.TitleAsync()}\nHTTP status: {response?.Status.ToString() ?? "unknown"}";
        }
        finally
        {
            BrowserLock.Release();
        }
    }

    [McpServerTool(Name = "browser_read_page"), Description("Read the title and visible text from the current page. Text output is capped at 12,000 characters.")]
    public static async Task<string> ReadPageAsync()
    {
        await BrowserLock.WaitAsync();
        try
        {
            var page = await GetPageAsync();
            var text = await page.Locator("body").InnerTextAsync(new LocatorInnerTextOptions { Timeout = 10_000 });
            if (text.Length > 12_000)
            {
                text = text[..12_000] + "\n[Output truncated]";
            }

            return $"URL: {page.Url}\nTitle: {await page.TitleAsync()}\n\n{text}";
        }
        finally
        {
            BrowserLock.Release();
        }
    }

    [McpServerTool(Name = "browser_screenshot"), Description("Save a full-page screenshot of the current page to artifacts/browser-page.png.")]
    public static async Task<string> ScreenshotAsync()
    {
        await BrowserLock.WaitAsync();
        try
        {
            var page = await GetPageAsync();
            var outputDirectory = Path.Combine(Environment.CurrentDirectory, "artifacts");
            Directory.CreateDirectory(outputDirectory);
            var screenshotPath = Path.Combine(outputDirectory, "browser-page.png");

            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = screenshotPath,
                FullPage = true
            });

            return $"Screenshot saved to: {screenshotPath}";
        }
        finally
        {
            BrowserLock.Release();
        }
    }

    private static async Task<IPage> GetPageAsync()
    {
        if (_page is not null)
        {
            return _page;
        }

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        _page = await _browser.NewPageAsync();
        return _page;
    }

    internal static async Task DisposeBrowserAsync()
    {
        await BrowserLock.WaitAsync();
        try
        {
            if (_browser is not null)
            {
                await _browser.CloseAsync();
                _browser = null;
                _page = null;
            }

            _playwright?.Dispose();
            _playwright = null;
        }
        finally
        {
            BrowserLock.Release();
        }
    }
}
