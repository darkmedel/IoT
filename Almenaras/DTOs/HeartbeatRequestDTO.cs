using System.ComponentModel.DataAnnotations;

namespace Almenaras.DTOs
{
    public sealed class HeartbeatRequestDTO
    {
        [Required]
        [StringLength(12, MinimumLength = 12)]
        [RegularExpression("^[0-9A-Fa-f]{12}$", ErrorMessage = "El DeviceId debe ser hexadecimal de 12 caracteres.")]
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