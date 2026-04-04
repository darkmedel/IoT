namespace cl.MedelCodeFactory.IoT.Common.Contracts.Devices
{
    public class DeviceRegistrationRequest
    {
        public string DeviceId { get; set; }
        public int TipoHardwareId { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string SerialNumber { get; set; }
        public string FirmwareVersion { get; set; }
        public string MacAddress { get; set; }
    }
}