namespace Almenaras.Configuration
{
    public sealed class HeartbeatOptions
    {
        public int DelayedThresholdSeconds { get; set; } = 90;
        public int OfflineThresholdSeconds { get; set; } = 300;
        public int DegradedRssiThreshold { get; set; } = -80;
        public int DegradedEventQueueThreshold { get; set; } = 10;
        public long DegradedFreeHeapThreshold { get; set; } = 150000;
    }
}