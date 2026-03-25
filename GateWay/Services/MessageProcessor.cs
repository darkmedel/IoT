using cl.MedelCodeFactory.IoT.GateWay.Models;

namespace cl.MedelCodeFactory.IoT.GateWay.Services
{
    public class MessageProcessor
    {
        private readonly MessageDeduplicationService _deduplicationService;
        private readonly ConnectionRegistry _connectionRegistry;

        public MessageProcessor(
            MessageDeduplicationService deduplicationService,
            ConnectionRegistry connectionRegistry)
        {
            _deduplicationService = deduplicationService;
            _connectionRegistry = connectionRegistry;
        }

        public string Process(string rawMessage, ConnectedDevice device)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
            {
                return "ERR|EMPTY";
            }

            string[] parts = rawMessage.Split('|', StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                return "ERR|FORMAT";
            }

            string command = parts[0].ToUpperInvariant();

            switch (command)
            {
                case "HELLO":
                    return ProcessHello(parts, device);

                case "BTN":
                    return ProcessButton(parts, device);

                case "STATUS":
                    return ProcessStatus(parts, device);

                case "PING":
                    return "PONG";

                case "ACK":
                    return ProcessAck(parts, device);
                default:
                    return $"ERR|UNKNOWN|{command}";
            }
        }

        private string ProcessHello(string[] parts, ConnectedDevice device)
        {
            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
            {
                return "ERR|HELLO|DEVICEID";
            }

            device.DeviceId = parts[1];
            _connectionRegistry.UpdateDeviceId(device.ConnectionId, device.DeviceId);

            Console.WriteLine($"[MSG] HELLO | DeviceId={device.DeviceId} | IP={device.RemoteIp}");

            return $"ACK|HELLO|{device.DeviceId}";
        }

        private string ProcessButton(string[] parts, ConnectedDevice device)
        {
            if (parts.Length < 5)
            {
                return "ERR|BTN|FORMAT";
            }

            string deviceId = parts[1];
            string msgId = parts[2];
            string timestamp = parts[3];
            string button = parts[4];

            if (string.IsNullOrWhiteSpace(device.DeviceId))
            {
                device.DeviceId = deviceId;
                _connectionRegistry.UpdateDeviceId(device.ConnectionId, device.DeviceId);
            }

            if (_deduplicationService.IsDuplicate(deviceId, msgId))
            {
                Console.WriteLine($"[MSG] BTN DUPLICATE | DeviceId={deviceId} | MsgId={msgId}");
                return $"ACK|BTN|{msgId}";
            }

            _deduplicationService.MarkProcessed(deviceId, msgId);

            Console.WriteLine($"[MSG] BTN | DeviceId={deviceId} | MsgId={msgId} | Timestamp={timestamp} | Button={button}");

            return $"ACK|BTN|{msgId}";
        }

        private string ProcessStatus(string[] parts, ConnectedDevice device)
        {
            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
            {
                return "ERR|STATUS|DEVICEID";
            }

            if (string.IsNullOrWhiteSpace(device.DeviceId))
            {
                device.DeviceId = parts[1];
                _connectionRegistry.UpdateDeviceId(device.ConnectionId, device.DeviceId);
            }

            string payload = string.Join('|', parts);

            Console.WriteLine($"[MSG] STATUS | DeviceId={device.DeviceId} | Payload={payload}");

            return "ACK|STATUS";
        }

        private string ProcessAck(string[] parts, ConnectedDevice device)
        {
            Console.WriteLine($"[MSG] ACK | DeviceId={device.DeviceId} | Payload={string.Join('|', parts)}");

            // Por ahora no respondemos nada a ACK
            return string.Empty;
        }
    }
}