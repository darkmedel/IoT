using cl.MedelCodeFactory.IoT.GateWay.Domain;
using cl.MedelCodeFactory.IoT.GateWay.Infrastructure;

namespace cl.MedelCodeFactory.IoT.GateWay.Application
{
    public class MessageProcessor
    {
        private readonly ILogger<MessageProcessor> _logger;
        private readonly MessageDeduplicationService _deduplicationService;
        private readonly ConnectionRegistry _connectionRegistry;

        public MessageProcessor(
            ILogger<MessageProcessor> logger,
            MessageDeduplicationService deduplicationService,
            ConnectionRegistry connectionRegistry)
        {
            _logger = logger;
            _deduplicationService = deduplicationService;
            _connectionRegistry = connectionRegistry;
        }

        public Task<string> ProcessAsync(
            string rawMessage,
            ConnectedDevice device,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
            {
                _logger.LogWarning("[MSG] Empty message received.");
                return Task.FromResult("ERR|EMPTY");
            }

            string[] parts = rawMessage.Split('|', StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                _logger.LogWarning("[MSG] Invalid message format. RawMessage={rawMessage}", rawMessage);
                return Task.FromResult("ERR|FORMAT");
            }

            string command = parts[0].ToUpperInvariant();

            string response = command switch
            {
                "HELLO" => ProcessHello(parts, device),
                "BTN" => ProcessButton(parts, device),
                "STATUS" => ProcessStatus(parts, device),
                "PING" => "PONG",
                "ACK" => ProcessAck(parts, device),
                "CONFIG_APPLIED" => ProcessConfigApplied(parts, device),
                _ => $"ERR|UNKNOWN|{command}"
            };

            return Task.FromResult(response);
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

                _logger.LogInformation(
                    "[MSG] Device bound to connection. ConnectionId={connectionId}, DeviceId={deviceId}",
                    device.ConnectionId,
                    device.DeviceId);

                return;
            }

            if (!string.Equals(device.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase))
            {
                string previousDeviceId = device.DeviceId;

                device.DeviceId = deviceId;
                _connectionRegistry.BindDevice(device.ConnectionId, device.DeviceId);

                _logger.LogWarning(
                    "[MSG] DeviceId rebound on existing connection. ConnectionId={connectionId}, PreviousDeviceId={previousDeviceId}, NewDeviceId={deviceId}",
                    device.ConnectionId,
                    previousDeviceId,
                    device.DeviceId);
            }
        }

        private string ProcessHello(string[] parts, ConnectedDevice device)
        {
            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
            {
                _logger.LogWarning("[MSG] HELLO invalid: missing deviceId.");
                return "ERR|HELLO|DEVICEID";
            }

            string deviceId = parts[1];

            EnsureDeviceBound(device, deviceId);

            _logger.LogInformation(
                "[MSG] HELLO | DeviceId={deviceId} | IP={remoteIp}",
                device.DeviceId,
                device.RemoteIp);

            return $"ACK|HELLO|{device.DeviceId}";
        }

        private string ProcessStatus(string[] parts, ConnectedDevice device)
        {
            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
            {
                _logger.LogWarning("[MSG] STATUS invalid: missing deviceId.");
                return "ERR|STATUS|DEVICEID";
            }

            string deviceId = parts[1];

            EnsureDeviceBound(device, deviceId);

            string payload = string.Join('|', parts);

            _logger.LogInformation(
                "[MSG] STATUS | DeviceId={deviceId} | Payload={payload}",
                device.DeviceId,
                payload);

            return "ACK|STATUS";
        }

        private string ProcessButton(string[] parts, ConnectedDevice device)
        {
            if (parts.Length < 5)
            {
                _logger.LogWarning("[MSG] BTN invalid: insufficient format.");
                return "ERR|BTN|FORMAT";
            }

            string deviceId = parts[1];
            string msgId = parts[2];
            string uptime = parts[3];
            string buttonNumber = parts[4];

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                _logger.LogWarning("[MSG] BTN invalid: empty deviceId.");
                return "ERR|BTN|DEVICEID";
            }

            if (string.IsNullOrWhiteSpace(msgId))
            {
                _logger.LogWarning("[MSG] BTN invalid: empty msgId. DeviceId={deviceId}", deviceId);
                return "ERR|BTN|MSGID";
            }

            EnsureDeviceBound(device, deviceId);

            bool isDuplicate = _deduplicationService.IsDuplicate(deviceId, msgId);
            if (isDuplicate)
            {
                _logger.LogWarning(
                    "[MSG] BTN DUPLICATE | DeviceId={deviceId} | MsgId={msgId}",
                    deviceId,
                    msgId);

                return $"ACK|BTN|{msgId}|DUPLICATE";
            }

            _deduplicationService.MarkProcessed(deviceId, msgId);

            _logger.LogInformation(
                "[MSG] BTN | DeviceId={deviceId} | MsgId={msgId} | Uptime={uptime} | Button={buttonNumber}",
                deviceId,
                msgId,
                uptime,
                buttonNumber);

            return $"ACK|BTN|{msgId}";
        }

        private string ProcessAck(string[] parts, ConnectedDevice device)
        {
            string payload = string.Join('|', parts);

            _logger.LogDebug(
                "[MSG] ACK | DeviceId={deviceId} | Payload={payload}",
                string.IsNullOrWhiteSpace(device.DeviceId) ? "UNKNOWN" : device.DeviceId,
                payload);

            return "ACK_RECEIVED";
        }

        private string ProcessConfigApplied(string[] parts, ConnectedDevice device)
        {
            if (parts.Length < 3)
            {
                _logger.LogWarning("[MSG] CONFIG_APPLIED invalid: insufficient format.");
                return "ERR|CONFIG_APPLIED|FORMAT";
            }

            string deviceId = parts[1];
            string configVersion = parts[2];

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                _logger.LogWarning("[MSG] CONFIG_APPLIED invalid: empty deviceId.");
                return "ERR|CONFIG_APPLIED|DEVICEID";
            }

            EnsureDeviceBound(device, deviceId);

            _logger.LogInformation(
                "[MSG] CONFIG_APPLIED | DeviceId={deviceId} | ConfigVersion={configVersion}",
                deviceId,
                configVersion);

            return $"ACK|CONFIG_APPLIED|{configVersion}";
        }
    }
}