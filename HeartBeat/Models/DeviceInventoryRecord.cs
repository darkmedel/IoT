namespace cl.MedelCodeFactory.IoT.HeartBeat.Models
{
    public class DeviceInventoryRecord
    {
        public string DeviceId { get; set; } = string.Empty;
        public bool Habilitado { get; set; }
        public DateTime? FechaBaja { get; set; }
    }
}