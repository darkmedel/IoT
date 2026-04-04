namespace cl.MedelCodeFactory.IoT.WatchTower.LegacyFromHeartBeat.DTOs
{
    public sealed class DeviceDetailsResponseDTO
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string? TipoHardware { get; set; }

        public string OperationalStatus { get; set; } = string.Empty;
        public string? PersistedOperationalStatus { get; set; }

        public DateTime? LastHeartbeatReceivedAtUtc { get; set; }

        public long? Uptime { get; set; }
        public int? Rssi { get; set; }
        public bool? WsConnected { get; set; }
        public int? EventQueueSize { get; set; }
        public long? FreeHeap { get; set; }

        public string? IssuesJson { get; set; }

        public int? EmpresaId { get; set; }
        public string? EmpresaNombre { get; set; }
        public bool HasActiveAssignment { get; set; }
    }
}