using System.Collections.Concurrent;
using cl.MedelCodeFactory.IoT.GateWay.Models;

namespace cl.MedelCodeFactory.IoT.GateWay.Services
{
    public class ConnectionRegistry
    {
        private readonly ConcurrentDictionary<string, ConnectedDevice> _byConnectionId =
            new ConcurrentDictionary<string, ConnectedDevice>();

        public void Add(ConnectedDevice device)
        {
            _byConnectionId[device.ConnectionId] = device;
        }

        public void Remove(string connectionId)
        {
            _byConnectionId.TryRemove(connectionId, out _);
        }

        public void UpdateDeviceId(string connectionId, string deviceId)
        {
            if (_byConnectionId.TryGetValue(connectionId, out var device))
            {
                device.DeviceId = deviceId;
            }
        }

        public ConnectedDevice? GetByDeviceId(string deviceId)
        {
            return _byConnectionId.Values.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.DeviceId) &&
                x.DeviceId.Equals(deviceId, StringComparison.OrdinalIgnoreCase));
        }

        public IReadOnlyCollection<ConnectedDevice> GetAll()
        {
            return _byConnectionId.Values.ToList();
        }
    }
}