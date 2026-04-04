namespace Almenaras.DTOs
{
    public sealed class HeartbeatAckResponseDTO
    {
        public bool Success { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ReceivedAtUtc { get; set; }
    }
}