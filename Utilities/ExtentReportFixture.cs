using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using Xunit;

namespace PlaywrightMcpServer.Tests;

public sealed class ExtentReportFixture : IAsyncLifetime
{
    private readonly ExtentReports _reports;

    public ExtentReportFixture()
    {
        var reportPath = Environment.GetEnvironmentVariable("EXTENT_REPORT_PATH");
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            DirectoryInfo? repositoryRoot = new(AppContext.BaseDirectory);
            while (repositoryRoot is not null &&
                   !File.Exists(Path.Combine(repositoryRoot.FullName, "PlaywrightMcpServer.csproj")))
            {
                repositoryRoot = repositoryRoot.Parent;
            }

            reportPath = Path.Combine(
                repositoryRoot?.FullName ?? Environment.CurrentDirectory,
                "TestResults",
                "ExtentReport.html");
        }

        reportPath = Path.GetFullPath(reportPath);
        var reportDirectory = Path.GetDirectoryName(reportPath);
        if (reportDirectory is null)
        {
            throw new InvalidOperationException("The Extent report path must include a directory.");
        }

        Directory.CreateDirectory(reportDirectory);

        var sparkReporter = new ExtentSparkReporter(reportPath);
        _reports = new ExtentReports();
        _reports.AttachReporter(sparkReporter);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _reports.Flush();
        return Task.CompletedTask;
    }

    public async Task RunAsync(string testName, Func<Task> testBody)
    {
        var test = _reports.CreateTest(testName);

        try
        {
            await testBody();
            test.Pass("Test passed");
        }
        catch (Exception exception)
        {
            test.Fail(exception.ToString());
            throw;
        }
    }
}
