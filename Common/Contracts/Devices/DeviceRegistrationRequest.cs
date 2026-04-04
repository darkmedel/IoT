namespace cl.MedelCodeFactory.IoT.Common.Contracts.Devices
{
    public class DeviceRegistrationRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public int TipoHardwareId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
    }
}