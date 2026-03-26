namespace cl.MedelCodeFactory.IoT.HeartBeat.DTOs
{
    public class HeartbeatAckResponse
    {
        public bool Ack { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public DateTime ReceivedAtUtc { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}