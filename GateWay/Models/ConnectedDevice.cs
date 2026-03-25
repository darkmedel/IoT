using System.Net.WebSockets;

namespace cl.MedelCodeFactory.IoT.GateWay.Models
{
    public class ConnectedDevice
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string RemoteIp { get; set; } = string.Empty;
        public DateTime ConnectedAtUtc { get; set; }
        public WebSocket? WebSocket { get; set; }
    }
}