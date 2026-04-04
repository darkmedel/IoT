namespace cl.MedelCodeFactory.IoT.WatchTower.Contracts
{
    public sealed class DeviceHistoryItemDto
    {
        public string DeviceId { get; set; } = string.Empty;
        public DateTime ReceivedAtUtc { get; set; }
        public string OperationalStatus { get; set; } = string.Empty;
        public int? Rssi { get; set; }
        public bool WsConnected { get; set; }
        public int EventQueueSize { get; set; }
        public long? FreeHeap { get; set; }
        public string? IssuesJson { get; set; }
    }
}