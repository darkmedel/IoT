namespace cl.MedelCodeFactory.IoT.Common.Contracts.Commands
{
    public class CommandResult
    {
        public bool Success { get; set; }
        public string DeviceId { get; set; }
        public string Command { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
    }
}