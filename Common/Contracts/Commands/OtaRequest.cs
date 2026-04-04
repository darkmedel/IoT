namespace cl.MedelCodeFactory.IoT.Common.Contracts.Commands
{
    public class OtaRequest
    {
        public string DeviceId { get; set; }
        public string FirmwareVersion { get; set; }
        public string Url { get; set; }
    }
}