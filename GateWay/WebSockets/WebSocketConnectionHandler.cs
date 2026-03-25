using System.Net.WebSockets;
using System.Text;
using cl.MedelCodeFactory.IoT.GateWay.Models;
using cl.MedelCodeFactory.IoT.GateWay.Services;

namespace cl.MedelCodeFactory.IoT.GateWay.WebSockets
{
    public class WebSocketConnectionHandler
    {
        private readonly MessageProcessor _messageProcessor;
        private readonly ConnectionRegistry _connectionRegistry;

        public WebSocketConnectionHandler(
            MessageProcessor messageProcessor,
            ConnectionRegistry connectionRegistry)
        {
            _messageProcessor = messageProcessor;
            _connectionRegistry = connectionRegistry;
        }

        public async Task HandleAsync(HttpContext context)
        {
            using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

            string connectionId = Guid.NewGuid().ToString("N");
            string remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var device = new ConnectedDevice
            {
                ConnectionId = connectionId,
                RemoteIp = remoteIp,
                ConnectedAtUtc = DateTime.UtcNow,
                WebSocket = webSocket
            };

            _connectionRegistry.Add(device);

            Console.WriteLine($"[WS] Connected | ConnectionId={connectionId} | IP={remoteIp}");

            byte[] buffer = new byte[4096];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine($"[WS] Close requested | ConnectionId={connectionId}");

                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closed by client",
                            CancellationToken.None);

                        break;
                    }

                    if (result.MessageType != WebSocketMessageType.Text)
                    {
                        Console.WriteLine($"[WS] Non-text message ignored | ConnectionId={connectionId}");
                        continue;
                    }

                    string message = await ReadFullMessageAsync(webSocket, buffer, result);

                    Console.WriteLine($"[WS] Received | ConnectionId={connectionId} | Message={message}");

                    string response = _messageProcessor.Process(message, device);

                    if (!string.IsNullOrWhiteSpace(response) &&
                        webSocket.State == WebSocketState.Open)
                    {
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);

                        await webSocket.SendAsync(
                            new ArraySegment<byte>(responseBytes),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);

                        Console.WriteLine($"[WS] Sent | ConnectionId={connectionId} | Response={response}");
                    }
                }
            }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"[WS] Error | ConnectionId={connectionId} | {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WS] Unexpected error | ConnectionId={connectionId} | {ex}");
            }
            finally
            {
                _connectionRegistry.Remove(connectionId);
                Console.WriteLine($"[WS] Disconnected | ConnectionId={connectionId}");
            }
        }

        private static async Task<string> ReadFullMessageAsync(
            WebSocket webSocket,
            byte[] buffer,
            WebSocketReceiveResult initialResult)
        {
            using var ms = new MemoryStream();

            ms.Write(buffer, 0, initialResult.Count);

            while (!initialResult.EndOfMessage)
            {
                initialResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                ms.Write(buffer, 0, initialResult.Count);
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}