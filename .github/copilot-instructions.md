# Workspace instructions

- This workspace contains a .NET 10 C# MCP server using the official Model Context Protocol C# SDK and Microsoft.Playwright.
- Keep MCP protocol traffic on stdout; write application logs to stderr.
- Preserve the stdio transport and shared headless Chromium page unless requirements change.
- Use README.md for build, browser installation, and run instructions.
- Keep Playwright page objects in Pages, tests in Tests, and reusable test helpers in Utilities.
- Use the focused agents in `.github/agents/` for bounded automation work; use Automation Lead to coordinate cross-cutting changes.
