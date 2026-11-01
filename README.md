# Playwright MCP Server (.NET)

A local stdio MCP server that automates a shared headless Chromium page with Playwright for .NET.

## Requirements

- .NET 10 SDK
- PowerShell (`pwsh`)

## Build and install Chromium

From the workspace root, restore and build the project, then install Playwright's Chromium browser:

```powershell
dotnet restore .\PlaywrightMcpServer.csproj
dotnet build .\PlaywrightMcpServer.csproj
pwsh .\bin\Debug\net10.0\playwright.ps1 install chromium
```

## Run

```powershell
dotnet run --project .\PlaywrightMcpServer.csproj
```

The server uses MCP stdio transport. Its tools are configured for VS Code in `.vscode/mcp.json`; after building and installing Chromium, reload the MCP server from the VS Code MCP controls.

## Run the demo tests

Install Chromium as described above, then run the Playwright tests:

```powershell
dotnet test .\Tests\PlaywrightMcpServer.Tests.csproj
```

The tests cover the browser tools against a local demo page and BlazeDemo acceptance criteria: visible/enabled search controls, selected city values, route-specific flight results, flight selection, booking confirmation, usable controls, and page errors. BlazeDemo tests need internet access and submit test-only booking details to the public demo site. The no-page-errors scenario currently exposes JavaScript errors on BlazeDemo caused by its blocked jQuery dependency.

Each test run creates an Extent HTML report at `.\TestResults\ExtentReport.html`, including passed and failed tests. Set `EXTENT_REPORT_PATH` to choose another output location. The Azure Pipelines definition in `azure-pipelines.yml` builds the test project on a Windows agent, installs Chromium, runs tests tagged `Category=Smoke`, and publishes the HTML report as the `ExtentReport` pipeline artifact. Its YAML PR trigger targets all branches where YAML PR triggers are supported. For Azure Repos Git, add this pipeline as a required Build validation branch policy on the target branches, because Azure Repos does not use YAML `pr` triggers. Run the same smoke subset locally with:

```powershell
dotnet test .\Tests\PlaywrightMcpServer.Tests.csproj --filter "Category=Smoke"
```

Test files live in `Tests`, BlazeDemo page objects in `Pages`, and reusable test fixtures/server helpers in `Utilities`. The current no-page-errors scenario is tagged `Category=KnownIssue`, so it remains available in the full test suite without making the pull-request smoke pipeline fail on BlazeDemo's existing JavaScript errors.

## Tools

- `browser_navigate` — navigate to an absolute HTTP or HTTPS URL.
- `browser_read_page` — return the current page title and visible body text (up to 12,000 characters).
- `browser_screenshot` — save a full-page PNG to `artifacts/browser-page.png`.

The tools share one headless Chromium page for the lifetime of the server. Navigation is limited to HTTP/HTTPS and URLs with embedded credentials are rejected. MCP logs are sent to stderr so stdout remains available for protocol traffic.
