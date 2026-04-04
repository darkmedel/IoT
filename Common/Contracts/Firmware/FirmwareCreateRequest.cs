namespace cl.MedelCodeFactory.IoT.Common.Contracts.Firmware
{
    public class FirmwareCreateRequest
    {
        public string Version { get; set; }
        public string Url { get; set; }
        public string Notes { get; set; }
        public bool Habilitado { get; set; }
    }
}