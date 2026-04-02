using cl.MedelCodeFactory.IoT.GateWay.Contracts;
using cl.MedelCodeFactory.IoT.GateWay.Domain;
using cl.MedelCodeFactory.IoT.GateWay.Infrastructure;
using System.Globalization;

namespace cl.MedelCodeFactory.IoT.GateWay.Application
{
    public class MessageProcessor
    {
        private readonly ILogger<MessageProcessor> _logger;
        private readonly MessageDeduplicationService _deduplicationService;
        private readonly ConnectionRegistry _connectionRegistry;
        private readonly IButtonEventRepository _buttonEventRepository;

        public MessageProcessor(
            ILogger<MessageProcessor> logger,
            MessageDeduplicationService deduplicationService,
            ConnectionRegistry connectionRegistry,
            IButtonEventRepository buttonEventRepository)
        {
            _logger = logger;
            _deduplicationService = deduplicationService;
            _connectionRegistry = connectionRegistry;
            _buttonEventRepository = buttonEventRepository;
        }

        public async Task<string> ProcessAsync(
            string rawMessage,
            ConnectedDevice device,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
            {
                _logger.LogWarning("[MSG] Empty message received.");
                return "ERR|EMPTY";
            }

            string[] parts = rawMessage.Split('|', StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                _logger.LogWarning("[MSG] Invalid message format. RawMessage={rawMessage}", rawMessage);
                return "ERR|FORMAT";
            }

            string command = parts[0].ToUpperInvariant();

            return command switch
            {
                "HELLO" => ProcessHello(parts, device),
                "BTN" => await ProcessButtonAsync(parts, rawMessage, device, cancellationToken),
                "STATUS" => ProcessStatus(parts, device),
                "PING" => "PONG",
                "ACK" => ProcessAck(parts, device),
                "CONFIG_APPLIED" => ProcessConfigApplied(parts, device),
                _ => $"ERR|UNKNOWN|{command}"
            };
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

        private async Task<string> ProcessButtonAsync(
            string[] parts,
            string rawMessage,
            ConnectedDevice device,
            CancellationToken cancellationToken)
        {
            if (parts.Length < 5)
            {
                _logger.LogWarning("[MSG] BTN invalid: insufficient format.");
                return "ERR|BTN|FORMAT";
            }

            string deviceId = parts[1];
            string msgIdRaw = parts[2];
            string uptimeRaw = parts[3];
            string buttonNumberRaw = parts[4];

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                _logger.LogWarning("[MSG] BTN invalid: empty deviceId.");
                return "ERR|BTN|DEVICEID";
            }

            if (!long.TryParse(msgIdRaw, NumberStyles.None, CultureInfo.InvariantCulture, out long msgId))
            {
                _logger.LogWarning(
                    "[MSG] BTN invalid: MsgId is not numeric. DeviceId={deviceId} | MsgId={msgId}",
                    deviceId,
                    msgIdRaw);

                return "ERR|BTN|MSGID";
            }

            if (!long.TryParse(uptimeRaw, NumberStyles.None, CultureInfo.InvariantCulture, out long uptime))
            {
                _logger.LogWarning(
                    "[MSG] BTN invalid: Uptime is not numeric. DeviceId={deviceId} | Uptime={uptime}",
                    deviceId,
                    uptimeRaw);

                return "ERR|BTN|UPTIME";
            }

            if (!int.TryParse(buttonNumberRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int buttonNumber))
            {
                _logger.LogWarning(
                    "[MSG] BTN invalid: ButtonNumber is not numeric. DeviceId={deviceId} | Button={button}",
                    deviceId,
                    buttonNumberRaw);

                return "ERR|BTN|BUTTON";
            }

            EnsureDeviceBound(device, deviceId);

            if (_deduplicationService.IsDuplicate(deviceId, msgIdRaw))
            {
                _logger.LogWarning(
                    "[MSG] BTN DUPLICATE (memory) | DeviceId={deviceId} | MsgId={msgId}",
                    deviceId,
                    msgId);

                return $"ACK|BTN|{msgId}";
            }

            var buttonEvent = new DeviceButtonEvent
            {
                DeviceId = deviceId,
                MsgId = msgId,
                Uptime = uptime,
                ButtonNumber = buttonNumber,
                ReceivedAtUtc = DateTime.UtcNow,
                RawMessage = rawMessage,
                ConnectionId = device.ConnectionId,
                RemoteIp = device.RemoteIp
            };

            ButtonEventSaveResult saveResult =
                await _buttonEventRepository.SaveAsync(buttonEvent, cancellationToken);

            if (!saveResult.Success)
            {
                _logger.LogError(
                    "[MSG] BTN persistence failed | DeviceId={deviceId} | MsgId={msgId}",
                    deviceId,
                    msgId);

                return "ERR|BTN|PERSISTENCE";
            }

            _deduplicationService.MarkProcessed(deviceId, msgIdRaw);

            if (saveResult.IsDuplicate)
            {
                _logger.LogWarning(
                    "[MSG] BTN DUPLICATE (sql) | DeviceId={deviceId} | MsgId={msgId} | Uptime={uptime} | Button={buttonNumber}",
                    deviceId,
                    msgId,
                    uptime,
                    buttonNumber);

                return $"ACK|BTN|{msgId}";
            }

            _logger.LogInformation(
                "[MSG] BTN persisted | DeviceId={deviceId} | MsgId={msgId} | Uptime={uptime} | Button={buttonNumber}",
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