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

                case "CONFIG_APPLIED":
                    return ProcessConfigApplied(parts, device);

                default:
                    return $"ERR|UNKNOWN|{command}";
            }
        }

        private void EnsureDeviceBound(ConnectedDevice device, string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(device.DeviceId))
            {
                device.DeviceId = deviceId;
                _connectionRegistry.BindDevice(device.ConnectionId, device.DeviceId);
                return;
            }

            if (!string.Equals(device.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase))
            {
                device.DeviceId = deviceId;
                _connectionRegistry.BindDevice(device.ConnectionId, device.DeviceId);
            }
        }

        private string ProcessHello(string[] parts, ConnectedDevice device)
        {
            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
            {
                return "ERR|HELLO|DEVICEID";
            }

            string deviceId = parts[1];

            EnsureDeviceBound(device, deviceId);

            Console.WriteLine($"[MSG] HELLO | DeviceId={device.DeviceId} | IP={device.RemoteIp}");

            return $"ACK|HELLO|{device.DeviceId}";
        }

        private string ProcessStatus(string[] parts, ConnectedDevice device)
        {
            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
            {
                return "ERR|STATUS|DEVICEID";
            }

            string deviceId = parts[1];

            EnsureDeviceBound(device, deviceId);

            string payload = string.Join('|', parts);

            Console.WriteLine($"[MSG] STATUS | DeviceId={device.DeviceId} | Payload={payload}");

            return "ACK|STATUS";
        }

        private string ProcessButton(string[] parts, ConnectedDevice device)
        {
            if (parts.Length < 5)
            {
                return "ERR|BTN|FORMAT";
            }

            string deviceId = parts[1];
            string msgId = parts[2];
            string uptime = parts[3];
            string buttonNumber = parts[4];

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return "ERR|BTN|DEVICEID";
            }

            if (string.IsNullOrWhiteSpace(msgId))
            {
                return "ERR|BTN|MSGID";
            }

            EnsureDeviceBound(device, deviceId);

            bool isDuplicate = _deduplicationService.IsDuplicate(deviceId, msgId);
            if (isDuplicate)
            {
                Console.WriteLine($"[MSG] BTN DUPLICATE | DeviceId={deviceId} | MsgId={msgId}");
                return $"ACK|BTN|{msgId}|DUPLICATE";
            }

            _deduplicationService.MarkProcessed(deviceId, msgId);

            Console.WriteLine(
                $"[MSG] BTN | DeviceId={deviceId} | MsgId={msgId} | Uptime={uptime} | Button={buttonNumber}");

            return $"ACK|BTN|{msgId}";
        }

        private string ProcessAck(string[] parts, ConnectedDevice device)
        {
            string payload = string.Join('|', parts);

            Console.WriteLine($"[MSG] ACK | DeviceId={device.DeviceId ?? "UNKNOWN"} | Payload={payload}");

            return "ACK_RECEIVED";
        }

        private string ProcessConfigApplied(string[] parts, ConnectedDevice device)
        {
            if (parts.Length < 3)
            {
                return "ERR|CONFIG_APPLIED|FORMAT";
            }

            string deviceId = parts[1];
            string configVersion = parts[2];

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return "ERR|CONFIG_APPLIED|DEVICEID";
            }

            EnsureDeviceBound(device, deviceId);

            Console.WriteLine(
                $"[MSG] CONFIG_APPLIED | DeviceId={deviceId} | ConfigVersion={configVersion}");

            return $"ACK|CONFIG_APPLIED|{configVersion}";
        }
    }
}