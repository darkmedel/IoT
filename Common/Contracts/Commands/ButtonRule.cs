namespace cl.MedelCodeFactory.IoT.Common.Contracts.Commands
{
    public class ButtonRule
    {
        public int ButtonGpio { get; set; }
        public bool Enabled { get; set; }
        public int TargetGpio { get; set; }
        public int PulseCount { get; set; }
        public int OnMs { get; set; }
        public int OffMs { get; set; }
        public bool ActiveHigh { get; set; }
    }
}