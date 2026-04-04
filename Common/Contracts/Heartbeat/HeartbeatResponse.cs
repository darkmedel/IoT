namespace cl.MedelCodeFactory.IoT.Common.Contracts.Heartbeat
{
    public class HeartbeatResponse
    {
        public bool Success { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string OperationalStatus { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}