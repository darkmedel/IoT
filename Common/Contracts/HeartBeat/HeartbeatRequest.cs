namespace cl.MedelCodeFactory.IoT.Common.Contracts.Heartbeat
{
    public class HeartbeatRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public long Uptime { get; set; }
        public int Rssi { get; set; }
        public bool WsConnected { get; set; }
        public int EventQueueSize { get; set; }
        public long FreeHeap { get; set; }
    }
}