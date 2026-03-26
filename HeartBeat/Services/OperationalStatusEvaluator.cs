using cl.MedelCodeFactory.IoT.HeartBeat.Configuration;
using cl.MedelCodeFactory.IoT.HeartBeat.Models;
using Microsoft.Extensions.Options;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Services
{
    public sealed class OperationalStatusEvaluator : IOperationalStatusEvaluator
    {
        private readonly HeartbeatOptions _options;

        public OperationalStatusEvaluator(IOptions<HeartbeatOptions> options)
        {
            _options = options.Value;
        }

        public string Evaluate(DeviceHeartbeatSnapshot snapshot, DateTime utcNow)
        {
            if (snapshot == null || !snapshot.LastHeartbeatReceivedAtUtc.HasValue)
            {
                return "Offline";
            }

            TimeSpan age = utcNow - snapshot.LastHeartbeatReceivedAtUtc.Value;

            if (age.TotalSeconds > _options.OfflineThresholdSeconds)
            {
                return "Offline";
            }

            if (age.TotalSeconds > _options.DelayedThresholdSeconds)
            {
                return "Delayed";
            }

            bool degraded =
                snapshot.WsConnected.HasValue && !snapshot.WsConnected.Value
                || snapshot.Rssi.HasValue && snapshot.Rssi.Value <= _options.DegradedRssiThreshold
                || snapshot.EventQueueSize.HasValue && snapshot.EventQueueSize.Value >= _options.DegradedEventQueueThreshold
                || snapshot.FreeHeap.HasValue && snapshot.FreeHeap.Value <= _options.DegradedFreeHeapThreshold;

            if (degraded)
            {
                return "Degraded";
            }

            return "Online";
        }
    }
}