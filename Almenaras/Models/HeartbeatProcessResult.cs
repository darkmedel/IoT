namespace cl.MedelCodeFactory.IoT.Almenaras.Models
{
    public sealed class HeartbeatProcessResult
    {
        public bool Success { get; set; }

        public int StatusCode { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string? DeviceId { get; set; }

        public string? OperationalStatus { get; set; }

        public string? IssuesJson { get; set; }

        public DateTime? ReceivedAtUtc { get; set; }
    }
}