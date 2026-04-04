namespace cl.MedelCodeFactory.IoT.Common.Contracts.Commands
{
    public class CommandResult
    {
        public bool Success { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}