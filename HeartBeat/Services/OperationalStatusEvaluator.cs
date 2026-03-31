using cl.MedelCodeFactory.IoT.HeartBeat.Configuration;
using cl.MedelCodeFactory.IoT.HeartBeat.Enums;
using cl.MedelCodeFactory.IoT.HeartBeat.Models;
using Microsoft.Extensions.Options;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Services
{
    public sealed class OperationalStatusEvaluator : IOperationalStatusEvaluator
    {
        private const string StatusOnline = "Online";
        private const string StatusDegraded = "Degraded";
        private const string StatusDelayed = "Delayed";
        private const string StatusOffline = "Offline";

        private readonly HeartbeatOptions _options;

        public OperationalStatusEvaluator(IOptions<HeartbeatOptions> options)
        {
            _options = options.Value;
        }

        public DeviceHealthEvaluation Evaluate(DeviceHeartbeatSnapshot snapshot, DateTime utcNow)
        {
            if (snapshot == null)
            {
                return new DeviceHealthEvaluation
                {
                    OperationalStatus = StatusOffline,
                    Issues = new List<string> { DeviceIssueCodes.HeartbeatMissing }
                };
            }

            var issues = new HashSet<string>(StringComparer.Ordinal);

            bool wsConnected = snapshot.WsConnected.GetValueOrDefault(false);

            if (snapshot.LastHeartbeatReceivedAtUtc is not DateTime lastHeartbeat)
            {
                issues.Add(DeviceIssueCodes.HeartbeatMissing);

                if (!wsConnected)
                {
                    issues.Add(DeviceIssueCodes.WsDisconnected);
                }

                return new DeviceHealthEvaluation
                {
                    OperationalStatus = wsConnected ? StatusDegraded : StatusOffline,
                    Issues = issues.OrderBy(x => x).ToList()
                };
            }

            if (snapshot.PreviousHeartbeatReceivedAtUtc.HasValue)
            {
                TimeSpan ingestGap = lastHeartbeat - snapshot.PreviousHeartbeatReceivedAtUtc.Value;

                if (ingestGap.TotalSeconds > _options.DelayedThresholdSeconds)
                {
                    issues.Add(DeviceIssueCodes.HeartbeatDelayed);
                }
            }

            TimeSpan currentAge = utcNow - lastHeartbeat;

            bool isOfflineByAge = currentAge.TotalSeconds > _options.OfflineThresholdSeconds;
            bool isDelayedByAge = currentAge.TotalSeconds > _options.DelayedThresholdSeconds;

            if (isOfflineByAge)
            {
                issues.Add(DeviceIssueCodes.HeartbeatMissing);
            }
            else if (isDelayedByAge)
            {
                issues.Add(DeviceIssueCodes.HeartbeatDelayed);
            }

            if (!wsConnected)
            {
                issues.Add(DeviceIssueCodes.WsDisconnected);
            }

            if (snapshot.Rssi.HasValue && snapshot.Rssi.Value <= _options.DegradedRssiThreshold)
            {
                issues.Add(DeviceIssueCodes.LowRssi);
            }

            if (snapshot.EventQueueSize.HasValue && snapshot.EventQueueSize.Value >= _options.DegradedEventQueueThreshold)
            {
                issues.Add(DeviceIssueCodes.HighEventQueue);
            }

            if (snapshot.FreeHeap.HasValue && snapshot.FreeHeap.Value <= _options.DegradedFreeHeapThreshold)
            {
                issues.Add(DeviceIssueCodes.LowFreeHeap);
            }

            string status;

            if (isOfflineByAge)
            {
                status = wsConnected ? StatusDegraded : StatusOffline;
            }
            else if (issues.Contains(DeviceIssueCodes.HeartbeatDelayed))
            {
                status = issues.Count == 1 ? StatusDelayed : StatusDegraded;
            }
            else if (issues.Count > 0)
            {
                status = StatusDegraded;
            }
            else
            {
                status = StatusOnline;
            }

            return new DeviceHealthEvaluation
            {
                OperationalStatus = status,
                Issues = issues.OrderBy(x => x).ToList()
            };
        }
    }
}