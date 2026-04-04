namespace Common.Contracts.Commands
{
    public class ButtonConfigRequest
    {
        public string DeviceId { get; set; }
        public int ConfigVersion { get; set; }
        public List<ButtonRule> Rules { get; set; } = new List<ButtonRule>();
    }
}