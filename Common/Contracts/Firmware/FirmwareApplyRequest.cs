namespace cl.MedelCodeFactory.IoT.Common.Contracts.Firmware
{
    public class FirmwareApplyRequest
    {
        public string DeviceId { get; set; }
        public string FirmwareVersion { get; set; }
        public string Url { get; set; }
    }
}