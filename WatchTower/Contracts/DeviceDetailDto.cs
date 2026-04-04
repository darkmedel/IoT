namespace cl.MedelCodeFactory.IoT.WatchTower.Contracts
{
    public sealed class DeviceDetailDto
    {
        public string DeviceId { get; set; } = string.Empty;
        public string EmpresaNombre { get; set; } = "Sin empresa";
        public string OperationalStatus { get; set; } = string.Empty;
        public DateTime? LastHeartbeatReceivedAtUtc { get; set; }
        public long? Uptime { get; set; }
        public int? Rssi { get; set; }
        public bool WsConnected { get; set; }
        public int EventQueueSize { get; set; }
        public long? FreeHeap { get; set; }
        public string? IssuesJson { get; set; }
    }
}