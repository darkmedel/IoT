using System.Collections.Generic;
namespace cl.MedelCodeFactory.IoT.Common.Contracts.Commands
{
    public class ButtonConfigRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public int ConfigVersion { get; set; }
        public List<ButtonRule> Rules { get; set; } = new List<ButtonRule>();
    }
}