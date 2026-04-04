namespace cl.MedelCodeFactory.IoT.Common.Contracts.Firmware
{
    public class FirmwareCreateRequest
    {
        public string Version { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool Habilitado { get; set; } = true;
    }
}