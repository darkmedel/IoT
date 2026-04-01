using System.Text.Json;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Models
{
    public sealed class DeviceHealthEvaluation
    {
        public string OperationalStatus { get; set; } = "Unknown";

        public List<string> Issues { get; set; } = new List<string>();

        public string? GetIssuesJson()
        {
            return Issues == null || Issues.Count == 0
                ? null
                : JsonSerializer.Serialize(Issues);
        }
    }
}