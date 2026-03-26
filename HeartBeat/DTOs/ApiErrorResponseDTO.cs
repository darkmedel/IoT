namespace cl.MedelCodeFactory.IoT.HeartBeat.DTOs
{
    public sealed class ApiErrorResponseDTO
    {
        public bool Success { get; set; }

        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public string? DeviceId { get; set; }
    }
}