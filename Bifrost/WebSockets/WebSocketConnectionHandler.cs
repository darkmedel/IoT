using cl.MedelCodeFactory.IoT.Bifrost.Application;
using cl.MedelCodeFactory.IoT.Bifrost.Infrastructure;
using cl.MedelCodeFactory.IoT.Bifrost.Domain;
using System.Net.WebSockets;
using System.Text;

namespace cl.MedelCodeFactory.IoT.Bifrost.WebSockets
{
    public class WebSocketConnectionHandler
    {
        private readonly ILogger<WebSocketConnectionHandler> _logger;
        private readonly MessageProcessor _messageProcessor;
        private readonly ConnectionRegistry _connectionRegistry;

        public WebSocketConnectionHandler(
            ILogger<WebSocketConnectionHandler> logger,
            MessageProcessor messageProcessor,
            ConnectionRegistry connectionRegistry)
        {
            _logger = logger;
            _messageProcessor = messageProcessor;
            _connectionRegistry = connectionRegistry;
        }

        public async Task HandleAsync(HttpContext context)
        {
            using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

            CancellationToken cancellationToken = context.RequestAborted;

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

            _logger.LogInformation(
                "[WS] Connected | ConnectionId={connectionId} | IP={remoteIp}",
                connectionId,
                remoteIp);

            byte[] buffer = new byte[4096];

            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation(
                            "[WS] Close requested | ConnectionId={connectionId} | DeviceId={deviceId}",
                            connectionId,
                            string.IsNullOrWhiteSpace(device.DeviceId) ? "(sin bind)" : device.DeviceId);

                        break;
                    }

                    if (result.MessageType != WebSocketMessageType.Text)
                    {
                        _logger.LogWarning(
                            "[WS] Non-text message ignored | ConnectionId={connectionId} | Type={messageType}",
                            connectionId,
                            result.MessageType);

                        continue;
                    }

                    string message = await ReadFullMessageAsync(
                        webSocket,
                        buffer,
                        result,
                        cancellationToken);

                    _logger.LogInformation(
                        "[WS] Received | ConnectionId={connectionId} | DeviceId={deviceId} | Message={message}",
                        connectionId,
                        string.IsNullOrWhiteSpace(device.DeviceId) ? "(sin bind)" : device.DeviceId,
                        message);

                    string response = await _messageProcessor.ProcessAsync(
                        message,
                        device,
                        cancellationToken);

                    if (!string.IsNullOrWhiteSpace(response) &&
                        !string.Equals(response, "ACK_RECEIVED", StringComparison.OrdinalIgnoreCase) &&
                        webSocket.State == WebSocketState.Open)
                    {
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);

                        await webSocket.SendAsync(
                            new ArraySegment<byte>(responseBytes),
                            WebSocketMessageType.Text,
                            true,
                            cancellationToken);

                        _logger.LogInformation(
                            "[WS] Sent | ConnectionId={connectionId} | DeviceId={deviceId} | Response={response}",
                            connectionId,
                            string.IsNullOrWhiteSpace(device.DeviceId) ? "(sin bind)" : device.DeviceId,
                            response);
                    }
                    else if (string.Equals(response, "ACK_RECEIVED", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug(
                            "[WS] ACK inbound procesado | ConnectionId={connectionId} | DeviceId={deviceId}",
                            connectionId,
                            string.IsNullOrWhiteSpace(device.DeviceId) ? "(sin bind)" : device.DeviceId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "[WS] Request aborted / operation canceled | ConnectionId={connectionId} | DeviceId={deviceId}",
                    connectionId,
                    string.IsNullOrWhiteSpace(device.DeviceId) ? "(sin bind)" : device.DeviceId);
            }
            catch (WebSocketException ex)
            {
                _logger.LogWarning(
                    ex,
                    "[WS] Error | ConnectionId={connectionId} | DeviceId={deviceId}",
                    connectionId,
                    string.IsNullOrWhiteSpace(device.DeviceId) ? "(sin bind)" : device.DeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[WS] Unexpected error | ConnectionId={connectionId} | DeviceId={deviceId}",
                    connectionId,
                    string.IsNullOrWhiteSpace(device.DeviceId) ? "(sin bind)" : device.DeviceId);
            }
            finally
            {
                _connectionRegistry.Remove(connectionId);

                _logger.LogInformation(
                    "[WS] Removed from registry | ConnectionId={connectionId} | DeviceId={deviceId}",
                    connectionId,
                    string.IsNullOrWhiteSpace(device.DeviceId) ? "(sin bind)" : device.DeviceId);

                if (webSocket.State == WebSocketState.Open ||
                    webSocket.State == WebSocketState.CloseReceived)
                {
                    try
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing",
                            CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "[WS] Failed to close socket cleanly | ConnectionId={connectionId} | DeviceId={deviceId}",
                            connectionId,
                            string.IsNullOrWhiteSpace(device.DeviceId) ? "(sin bind)" : device.DeviceId);
                    }
                }

                _logger.LogInformation(
                    "[WS] Disconnected | ConnectionId={connectionId} | DeviceId={deviceId}",
                    connectionId,
                    string.IsNullOrWhiteSpace(device.DeviceId) ? "(sin bind)" : device.DeviceId);
            }
        }

        private static async Task<string> ReadFullMessageAsync(
            WebSocket webSocket,
            byte[] buffer,
            WebSocketReceiveResult initialResult,
            CancellationToken cancellationToken)
        {
            using var ms = new MemoryStream();

            ms.Write(buffer, 0, initialResult.Count);

            while (!initialResult.EndOfMessage)
            {
                initialResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

                ms.Write(buffer, 0, initialResult.Count);
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}