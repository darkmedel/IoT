namespace cl.MedelCodeFactory.IoT.GateWay.Domain
{
    public class DeviceButtonEvent
    {
        public string DeviceId { get; set; } = string.Empty;
        public long MsgId { get; set; }
        public long Uptime { get; set; }
        public int ButtonNumber { get; set; }
        public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;

        public string? ConnectionId { get; set; }
        public string? RemoteIp { get; set; }
        public string? RawMessage { get; set; }
    }
}