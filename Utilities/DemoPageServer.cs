using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PlaywrightMcpServer.Tests;

internal sealed class DemoPageServer : IAsyncDisposable
{
    private const string Html = """
        <!doctype html>
        <html>
          <head><title>Demo page</title></head>
          <body>
            <h1>Playwright MCP demo</h1>
            <p>A local page used by the demo tests.</p>
          </body>
        </html>
        """;

    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task _requestLoop;

    private DemoPageServer(TcpListener listener)
    {
        _listener = listener;
        var endpoint = (IPEndPoint)listener.LocalEndpoint;
        Url = $"http://127.0.0.1:{endpoint.Port}/";
        _requestLoop = RunAsync();
    }

    public string Url { get; }

    public static DemoPageServer Start()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return new DemoPageServer(listener);
    }

    public async ValueTask DisposeAsync()
    {
        _cancellation.Cancel();
        _listener.Stop();
        await _requestLoop;
        _cancellation.Dispose();
    }

    private async Task RunAsync()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_cancellation.Token);
            }
            catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
            {
                break;
            }
            catch (SocketException) when (_cancellation.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (_cancellation.IsCancellationRequested)
            {
                break;
            }

            using (client)
            {
                await RespondAsync(client);
            }
        }
    }

    private static async Task RespondAsync(TcpClient client)
    {
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        while (await reader.ReadLineAsync() is { Length: > 0 })
        {
        }

        var body = Encoding.UTF8.GetBytes(Html);
        var headers = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(headers);
        await stream.WriteAsync(body);
    }
}
