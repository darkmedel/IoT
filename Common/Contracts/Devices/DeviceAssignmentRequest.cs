namespace cl.MedelCodeFactory.IoT.Common.Contracts.Devices
{
    public class DeviceAssignmentRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public int EmpresaId { get; set; }
        public string NombreDispositivo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }
}