using cl.MedelCodeFactory.IoT.GateWay.Application;
using cl.MedelCodeFactory.IoT.GateWay.Domain;
using cl.MedelCodeFactory.IoT.GateWay.Infrastructure;
using System.Net.WebSockets;
using System.Text;

namespace cl.MedelCodeFactory.IoT.GateWay.WebSockets
{
    public class WebSocketHandler
    {
        private readonly ILogger<WebSocketHandler> _logger;
        private readonly ConnectionRegistry _connectionRegistry;
        private readonly MessageProcessor _messageProcessor;

        public WebSocketHandler(
            ILogger<WebSocketHandler> logger,
            ConnectionRegistry connectionRegistry,
            MessageProcessor messageProcessor)
        {
            _logger = logger;
            _connectionRegistry = connectionRegistry;
            _messageProcessor = messageProcessor;
        }

        public async Task HandleAsync(WebSocket webSocket, CancellationToken cancellationToken = default)
        {
            string connectionId = Guid.NewGuid().ToString("N");

            var connectedDevice = new ConnectedDevice
            {
                ConnectionId = connectionId,
                ConnectedAtUtc = DateTime.UtcNow,
                WebSocket = webSocket
            };

            _connectionRegistry.Add(connectedDevice);

            var buffer = new byte[4096];

            try
            {
                _logger.LogInformation("[WS] Nueva conexión aceptada. connectionId={connectionId}", connectionId);

                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation(
                            "[WS] Cliente solicitó cierre. connectionId={connectionId}, deviceId={deviceId}",
                            connectionId,
                            connectedDevice.DeviceId ?? "(sin bind)");
                        break;
                    }

                    if (result.MessageType != WebSocketMessageType.Text)
                    {
                        _logger.LogWarning(
                            "[WS] Tipo de mensaje no soportado. connectionId={connectionId}, type={messageType}",
                            connectionId,
                            result.MessageType);
                        continue;
                    }

                    string rawMessage = await ReadCompleteMessageAsync(webSocket, buffer, result, cancellationToken);

                    if (string.IsNullOrWhiteSpace(rawMessage))
                    {
                        _logger.LogWarning("[WS] Mensaje vacío recibido. connectionId={connectionId}", connectionId);
                        continue;
                    }

                    _logger.LogInformation(
                        "[WS] Mensaje recibido. connectionId={connectionId}, deviceId={deviceId}, raw={rawMessage}",
                        connectionId,
                        string.IsNullOrWhiteSpace(connectedDevice.DeviceId) ? "(sin bind)" : connectedDevice.DeviceId,
                        rawMessage);

                    string response = await _messageProcessor.ProcessAsync(
                        rawMessage,
                        connectedDevice,
                        cancellationToken);

                    // 🔥 SOLO enviar respuestas válidas al dispositivo
                    if (!string.IsNullOrWhiteSpace(response) &&
                        !string.Equals(response, "ACK_RECEIVED", StringComparison.OrdinalIgnoreCase))
                    {
                        await SendTextAsync(webSocket, response, cancellationToken);

                        _logger.LogInformation(
                            "[WS] Respuesta enviada. connectionId={connectionId}, deviceId={deviceId}, response={response}",
                            connectionId,
                            string.IsNullOrWhiteSpace(connectedDevice.DeviceId) ? "(sin bind)" : connectedDevice.DeviceId,
                            response);
                    }
                    else if (string.Equals(response, "ACK_RECEIVED", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug(
                            "[WS] ACK inbound procesado. connectionId={connectionId}, deviceId={deviceId}",
                            connectionId,
                            string.IsNullOrWhiteSpace(connectedDevice.DeviceId) ? "(sin bind)" : connectedDevice.DeviceId);
                    }
                }
            }
            catch (WebSocketException ex)
            {
                _logger.LogWarning(
                    ex,
                    "[WS] WebSocketException. connectionId={connectionId}, deviceId={deviceId}",
                    connectionId,
                    connectedDevice.DeviceId ?? "(sin bind)");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "[WS] Operación cancelada. connectionId={connectionId}, deviceId={deviceId}",
                    connectionId,
                    connectedDevice.DeviceId ?? "(sin bind)");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[WS] Error no controlado. connectionId={connectionId}, deviceId={deviceId}",
                    connectionId,
                    connectedDevice.DeviceId ?? "(sin bind)");
            }
            finally
            {
                _connectionRegistry.Remove(connectionId);

                _logger.LogInformation(
                    "[WS] Conexión removida del registry. connectionId={connectionId}, deviceId={deviceId}",
                    connectionId,
                    connectedDevice.DeviceId ?? "(sin bind)");

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
                            "[WS] No fue posible cerrar limpiamente el socket. connectionId={connectionId}, deviceId={deviceId}",
                            connectionId,
                            connectedDevice.DeviceId ?? "(sin bind)");
                    }
                }

                webSocket.Dispose();

                _logger.LogInformation(
                    "[WS] Conexión finalizada. connectionId={connectionId}, deviceId={deviceId}",
                    connectionId,
                    connectedDevice.DeviceId ?? "(sin bind)");
            }
        }

        private static async Task<string> ReadCompleteMessageAsync(
            WebSocket webSocket,
            byte[] buffer,
            WebSocketReceiveResult initialResult,
            CancellationToken cancellationToken)
        {
            using var ms = new MemoryStream();

            ms.Write(buffer, 0, initialResult.Count);

            while (!initialResult.EndOfMessage)
            {
                initialResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                ms.Write(buffer, 0, initialResult.Count);
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static async Task SendTextAsync(
            WebSocket webSocket,
            string message,
            CancellationToken cancellationToken)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);

            await webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                cancellationToken);
        }
    }
}