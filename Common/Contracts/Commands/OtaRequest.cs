namespace cl.MedelCodeFactory.IoT.Common.Contracts.Commands
{
    public class OtaRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}