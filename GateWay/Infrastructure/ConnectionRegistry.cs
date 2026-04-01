using cl.MedelCodeFactory.IoT.GateWay.Domain;
using System.Collections.Concurrent;

namespace cl.MedelCodeFactory.IoT.GateWay.Infrastructure
{
    public class ConnectionRegistry
    {
        private readonly ConcurrentDictionary<string, ConnectedDevice> _byConnectionId = new();
        private readonly ConcurrentDictionary<string, string> _connectionIdByDeviceId =
            new(StringComparer.OrdinalIgnoreCase);

        public void Add(ConnectedDevice device)
        {
            _byConnectionId[device.ConnectionId] = device;
        }

        public void Remove(string connectionId)
        {
            if (_byConnectionId.TryRemove(connectionId, out var removed))
            {
                if (!string.IsNullOrWhiteSpace(removed.DeviceId))
                {
                    _connectionIdByDeviceId.TryGetValue(removed.DeviceId, out var mappedConnectionId);

                    if (string.Equals(mappedConnectionId, connectionId, StringComparison.OrdinalIgnoreCase))
                    {
                        _connectionIdByDeviceId.TryRemove(removed.DeviceId, out _);
                    }
                }
            }
        }

        public void BindDevice(string connectionId, string deviceId)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new ArgumentException("connectionId is required.", nameof(connectionId));

            if (string.IsNullOrWhiteSpace(deviceId))
                throw new ArgumentException("deviceId is required.", nameof(deviceId));

            if (!_byConnectionId.TryGetValue(connectionId, out var device))
                return;

            device.DeviceId = deviceId;

            _connectionIdByDeviceId[deviceId] = connectionId;
        }

        public ConnectedDevice? GetByConnectionId(string connectionId)
        {
            return _byConnectionId.TryGetValue(connectionId, out var device)
                ? device
                : null;
        }

        public ConnectedDevice? GetByDeviceId(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return null;

            if (!_connectionIdByDeviceId.TryGetValue(deviceId, out var connectionId))
                return null;

            return _byConnectionId.TryGetValue(connectionId, out var device)
                ? device
                : null;
        }

        public bool IsConnected(string deviceId)
        {
            var device = GetByDeviceId(deviceId);

            return device?.WebSocket != null &&
                   device.WebSocket.State == System.Net.WebSockets.WebSocketState.Open;
        }

        public IReadOnlyCollection<ConnectedDevice> GetAll()
        {
            return _byConnectionId.Values.ToList().AsReadOnly();
        }
    }
}