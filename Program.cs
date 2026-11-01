using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using PlaywrightMcpServer;

var builder = Host.CreateApplicationBuilder(args);

// MCP stdio uses stdout for protocol messages, so send all logs to stderr.
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<BrowserTools>();

var app = builder.Build();
try
{
    await app.RunAsync();
}
finally
{
    await BrowserTools.DisposeBrowserAsync();
}
