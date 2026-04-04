namespace cl.MedelCodeFactory.IoT.Almenaras.Models
{
    public sealed class HeartbeatEvaluationInput
    {
        public string DeviceId { get; set; } = string.Empty;

        public long Uptime { get; set; }

        public int Rssi { get; set; }

        public bool WsConnected { get; set; }

        public int EventQueueSize { get; set; }

        public long FreeHeap { get; set; }

        public DateTime ReceivedAtUtc { get; set; }

        public DateTime? PreviousHeartbeatReceivedAtUtc { get; set; }
    }
}