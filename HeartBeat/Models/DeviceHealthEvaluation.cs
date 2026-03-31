using System.Text.Json;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Models
{
    public sealed class DeviceHealthEvaluation
    {
        public string OperationalStatus { get; set; } = string.Empty;

        public List<string> Issues { get; set; } = new();

        public bool HasIssues => Issues != null && Issues.Count > 0;

        public string? GetIssuesJson()
        {
            if (!HasIssues)
            {
                return null;
            }

            return JsonSerializer.Serialize(Issues);
        }
    }
}