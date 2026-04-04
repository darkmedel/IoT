namespace cl.MedelCodeFactory.IoT.WatchTower.Configuration
{
    public sealed class WatchTowerOptions
    {
        public const string SectionName = "WatchTower";

        public string ConnectionStringName { get; set; } = "IoTMonitoreo";
        public int DefaultHistoryLimit { get; set; } = 100;
        public int MaxHistoryLimit { get; set; } = 500;
    }
}