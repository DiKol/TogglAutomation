using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TogglAutomationApp;

internal partial class WebSocket : IDisposable
{
    private Uri SocketUri { get; }
    private ClientWebSocket Socket { get; set; }
    private Action<WebSocketMessage> OnMessage { get; }
    private Action<string> OnWsError { get; }
    private CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();
    private CancellationToken CancellationToken { get; }
    private bool IsDisposed { get; set; } = false;
    internal WebSocket(Guid sessionId, string appVersion, Action<WebSocketMessage> onMessage, Action<string> onWsError)
    {
#if DEBUG
        const string url = "ws://localhost:5006";
#else
        const string url = "wss://luka.getremail.com:8213";
#endif

        SocketUri = new Uri($"{url}/agent/{sessionId}/{appVersion}");
        Socket = new ClientWebSocket();
        OnMessage = onMessage;
        CancellationToken = CancellationTokenSource.Token;
        OnWsError = onWsError;
        Start();
    }

    private async void Start()
    {
        StartPinging();

        try
        {
            while (true)
            {
                await ConnectToServer();
                await StartReceiving();
            }
        }
        catch (TaskCanceledException) { return; }
        catch(Exception ex)
        {
            Console.WriteLine("Error on method start, {0}", ex);
        }
    }
    private async ValueTask ConnectToServer()
    {
        while (true)
        {
            try
            {
                await Socket.ConnectAsync(SocketUri, CancellationToken);
                break;
            }
            catch (TaskCanceledException) { throw; }

            catch(Exception ex)
            {
                Console.WriteLine("Failed to connect retrying...({0})", ex.Message);
                if(ex is WebSocketException)
                {
                    var match = CodeRegex.Match(ex.Message);
                    if(match != null)
                    {
                        if(match.Groups.Count >= 2)
                        {
                            if(int.TryParse(match.Groups[1].Value, out var code))
                            {
                                Console.WriteLine("Error code {0}", code);
                                string message;
                                if (code == 401) message = "Version invalid please restart app";
                                else if (code == 404) message = "Session invalid please restart app";
                                else message = "Unknown server error try restarting the app";

                                OnWsError(message);
                            }
                            
                        }
                    }
                }

                try
                {
                    await Task.Delay(1000, CancellationToken);
                }
                catch(TaskCanceledException) { throw; }
                Socket.Dispose();
                Socket = new ClientWebSocket();
            }
        }
    }

    private async ValueTask StartReceiving()
    {
        try
        {
            while (true)
            {
                byte[] buffer = new byte[2048];
                var result = await Socket.ReceiveAsync(buffer, CancellationToken);
                if(result.MessageType == WebSocketMessageType.Close)
                {
                    await TryClose();
                    return;
                }
                int count = result.Count;
                if (count == 0 || result.MessageType != WebSocketMessageType.Text) continue;

                var text = Encoding.UTF8.GetString(buffer, 0, count);
                try
                {
                    var message = JsonSerializer.Deserialize<WebSocketMessage>(text);
                    if(message == null)
                    {
                        Console.WriteLine("Json message was null");
                        return;
                    }
                    Console.WriteLine("Received: {0}", text);
                    OnMessage?.Invoke(message);
                }catch(Exception ex)
                {
                    Console.WriteLine("Error reading json, {0}", ex);
                }
            }
        }
        catch (TaskCanceledException) { throw; }
        catch(Exception ex)
        {
            Console.WriteLine("Error on receive: {0}", ex);
            await TryClose();
        }
    }

    private async ValueTask TryClose()
    {
        try
        {
            if (Socket.State == WebSocketState.Closed) return;
            await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Socket closed", CancellationToken.None);
        }catch(Exception ex)
        {
            Console.WriteLine("Error on closing, {0}", ex);
        }
    }

    private async void StartPinging()
    {
        try
        {
            while (true)
            {
                try
                {
                    while (Socket.State != WebSocketState.Open) await Task.Delay(1000, CancellationToken);
                    await Send("ping", 1);
                    await Task.Delay(1_000, CancellationToken);
                }
                catch(Exception ex)
                {
                    if (ex is not ObjectDisposedException) throw;
                }
            }
        }
        catch (TaskCanceledException)
        {
            return;
        }
        catch(Exception ex)
        {
            Console.WriteLine("Error on pinging, {0}", ex);
        }
    }

    internal async Task Send<T>(string type, T data)
    {
        try
        {
            var json = JsonSerializer.Serialize(new
            {
                type,
                data
            });
            var bytes = Encoding.UTF8.GetBytes(json);
            await Socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken);
        }
        catch(Exception ex)
        {
            if (ex is ObjectDisposedException) return;
            Console.WriteLine("Error sending message, {0}", ex);
            await TryClose();
        }
    }

    public void Dispose()
    {
        if (IsDisposed) return;
        CancellationTokenSource.Cancel();
        try
        {
            Socket.Dispose();
        }
        catch { }
        IsDisposed = true;
    }

    private static Regex CodeRegex = new Regex("The server returned status code '([0-9]+)' when status code '101' was expected");
}


public class WebSocketMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;
    [JsonPropertyName("data")]

    public JsonElement Data { get; set; }
}
