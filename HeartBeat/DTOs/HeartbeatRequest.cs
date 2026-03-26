using System.ComponentModel.DataAnnotations;

namespace cl.MedelCodeFactory.IoT.HeartBeat.DTOs
{
    public class HeartbeatRequest
    {
        [Required]
        [MaxLength(100)]
        public string DeviceId { get; set; } = string.Empty;

        [Range(0, long.MaxValue)]
        public long Uptime { get; set; }

        [Range(-120, 0)]
        public int Rssi { get; set; }

        public bool WsConnected { get; set; }

        [Range(0, int.MaxValue)]
        public int EventQueueSize { get; set; }

        [Range(0, long.MaxValue)]
        public long FreeHeap { get; set; }
    }
}