using System.Net.WebSockets;
using System.Text;
using cl.MedelCodeFactory.IoT.Bifrost.Domain;
using cl.MedelCodeFactory.IoT.Bifrost.Infrastructure;

namespace cl.MedelCodeFactory.IoT.Bifrost.Application
{
    public class DeviceCommandSender
    {
        private readonly ILogger<DeviceCommandSender> _logger;
        private readonly ConnectionRegistry _connectionRegistry;

        public DeviceCommandSender(
            ILogger<DeviceCommandSender> logger,
            ConnectionRegistry connectionRegistry)
        {
            _logger = logger;
            _connectionRegistry = connectionRegistry;
        }

        public Task<DeviceCommandSendResult> SendStatusAsync(
            string deviceId,
            CancellationToken cancellationToken = default)
        {
            return SendAsync(deviceId, "CMD|STATUS", cancellationToken);
        }

        public Task<DeviceCommandSendResult> SendRebootAsync(
            string deviceId,
            CancellationToken cancellationToken = default)
        {
            return SendAsync(deviceId, "CMD|REBOOT", cancellationToken);
        }

        public async Task<DeviceCommandSendResult> SendAsync(
            string deviceId,
            string command,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return DeviceCommandSendResult.Invalid("deviceId is required.");
            }

            if (string.IsNullOrWhiteSpace(command))
            {
                return DeviceCommandSendResult.Invalid("command is required.");
            }

            ConnectedDevice? device = _connectionRegistry.GetByDeviceId(deviceId);

            if (device is null)
            {
                _logger.LogWarning(
                    "[CMD] Device not found in registry | DeviceId={deviceId} | Command={command}",
                    deviceId,
                    command);

                return DeviceCommandSendResult.NotConnected(deviceId, command, "Device is not connected.");
            }

            if (device.WebSocket is null)
            {
                _logger.LogWarning(
                    "[CMD] Device has no WebSocket instance | DeviceId={deviceId} | Command={command}",
                    deviceId,
                    command);

                return DeviceCommandSendResult.NotConnected(deviceId, command, "Device socket is null.");
            }

            if (device.WebSocket.State != WebSocketState.Open)
            {
                _logger.LogWarning(
                    "[CMD] Device socket is not open | DeviceId={deviceId} | Command={command} | State={socketState}",
                    deviceId,
                    command,
                    device.WebSocket.State);

                return DeviceCommandSendResult.NotConnected(
                    deviceId,
                    command,
                    $"Device socket is not open. Current state: {device.WebSocket.State}");
            }

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(command);

                await device.WebSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    cancellationToken);

                _logger.LogInformation(
                    "[CMD] Command sent | DeviceId={deviceId} | Command={command} | ConnectionId={connectionId}",
                    deviceId,
                    command,
                    device.ConnectionId);

                return DeviceCommandSendResult.SuccessResult(deviceId, command);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "[CMD] Command send cancelled | DeviceId={deviceId} | Command={command}",
                    deviceId,
                    command);

                return DeviceCommandSendResult.Failed(deviceId, command, "Command send was cancelled.");
            }
            catch (WebSocketException ex)
            {
                _logger.LogWarning(
                    ex,
                    "[CMD] WebSocket error while sending command | DeviceId={deviceId} | Command={command}",
                    deviceId,
                    command);

                return DeviceCommandSendResult.Failed(deviceId, command, $"WebSocket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[CMD] Unexpected error while sending command | DeviceId={deviceId} | Command={command}",
                    deviceId,
                    command);

                return DeviceCommandSendResult.Failed(deviceId, command, $"Unexpected error: {ex.Message}");
            }
        }
    }

    public class DeviceCommandSendResult
    {
        public bool Success { get; init; }
        public string DeviceId { get; init; } = string.Empty;
        public string Command { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;

        public static DeviceCommandSendResult SuccessResult(string deviceId, string command)
        {
            return new DeviceCommandSendResult
            {
                Success = true,
                DeviceId = deviceId,
                Command = command,
                Code = "SENT",
                Message = "Command sent successfully."
            };
        }

        public static DeviceCommandSendResult Invalid(string message)
        {
            return new DeviceCommandSendResult
            {
                Success = false,
                Code = "INVALID_REQUEST",
                Message = message
            };
        }

        public static DeviceCommandSendResult NotConnected(string deviceId, string command, string message)
        {
            return new DeviceCommandSendResult
            {
                Success = false,
                DeviceId = deviceId,
                Command = command,
                Code = "DEVICE_NOT_CONNECTED",
                Message = message
            };
        }

        public static DeviceCommandSendResult Failed(string deviceId, string command, string message)
        {
            return new DeviceCommandSendResult
            {
                Success = false,
                DeviceId = deviceId,
                Command = command,
                Code = "SEND_FAILED",
                Message = message
            };
        }
    }
}