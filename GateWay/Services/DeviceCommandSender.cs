using System.Net.WebSockets;
using System.Text;

namespace cl.MedelCodeFactory.IoT.GateWay.Services
{
    public class DeviceCommandSender
    {
        private readonly ConnectionRegistry _connectionRegistry;

        public DeviceCommandSender(ConnectionRegistry connectionRegistry)
        {
            _connectionRegistry = connectionRegistry;
        }

        public async Task<SendToDeviceResult> SendTextAsync(
            string deviceId,
            string payload,
            CancellationToken cancellationToken = default)
        {
            var device = _connectionRegistry.GetByDeviceId(deviceId);

            if (device == null)
            {
                return SendToDeviceResult.NotConnected(deviceId, "Device not registered in active connections.");
            }

            if (device.WebSocket == null || device.WebSocket.State != WebSocketState.Open)
            {
                return SendToDeviceResult.NotConnected(deviceId, "Device socket is not open.");
            }

            byte[] bytes = Encoding.UTF8.GetBytes(payload);

            await device.WebSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken);

            Console.WriteLine($"[WS] DirectedSend | DeviceId={deviceId} | Payload={payload}");

            return SendToDeviceResult.Ok(deviceId);
        }
    }

    public record SendToDeviceResult(bool Success, string DeviceId, string? Error)
    {
        public static SendToDeviceResult Ok(string deviceId) => new(true, deviceId, null);

        public static SendToDeviceResult NotConnected(string deviceId, string error) =>
            new(false, deviceId, error);
    }
}