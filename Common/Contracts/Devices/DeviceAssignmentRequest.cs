namespace cl.MedelCodeFactory.IoT.Common.Contracts.Devices
{
    public class DeviceAssignmentRequest
    {
        public string DeviceId { get; set; }
        public int EmpresaId { get; set; }
        public string NombreDispositivo { get; set; }
        public string Descripcion { get; set; }
    }
}