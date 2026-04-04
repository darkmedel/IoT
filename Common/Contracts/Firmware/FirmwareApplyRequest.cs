namespace cl.MedelCodeFactory.IoT.Common.Contracts.Firmware
{
    public class FirmwareApplyRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}