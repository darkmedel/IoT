using System.Net.WebSockets;

namespace cl.MedelCodeFactory.IoT.Bifrost.Domain
{
    public class ConnectedDevice
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string RemoteIp { get; set; } = string.Empty;
        public DateTime ConnectedAtUtc { get; set; } = DateTime.UtcNow;
        public WebSocket? WebSocket { get; set; }
    }
}