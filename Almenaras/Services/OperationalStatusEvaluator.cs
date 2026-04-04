using Microsoft.Extensions.Options;
using Almenaras.Configuration;
using Almenaras.Models;

namespace Almenaras.Services
{
    public sealed class OperationalStatusEvaluator : IOperationalStatusEvaluator
    {
        private readonly HeartbeatOptions _options;

        public OperationalStatusEvaluator(IOptions<HeartbeatOptions> options)
        {
            _options = options.Value ?? new HeartbeatOptions();
        }

        public DeviceHealthEvaluation Evaluate(HeartbeatEvaluationInput input, DateTime utcNow)
        {
            if (input == null)
            {
                return new DeviceHealthEvaluation
                {
                    OperationalStatus = "Unknown",
                    Issues = new List<string> { "InvalidEvaluationInput" }
                };
            }

            var issues = new List<string>();

            if (input.ReceivedAtUtc == default)
            {
                return new DeviceHealthEvaluation
                {
                    OperationalStatus = "Unknown",
                    Issues = new List<string> { "MissingReceivedAtUtc" }
                };
            }

            double ageSeconds = (utcNow - input.ReceivedAtUtc).TotalSeconds;

            if (ageSeconds < 0)
            {
                ageSeconds = 0;
            }

            if (ageSeconds >= _options.OfflineThresholdSeconds)
            {
                issues.Add("HeartbeatTimeout");

                return new DeviceHealthEvaluation
                {
                    OperationalStatus = "Offline",
                    Issues = issues
                };
            }

            bool delayed = false;
            bool degraded = false;

            if (ageSeconds >= _options.DelayedThresholdSeconds)
            {
                delayed = true;
                issues.Add("HeartbeatDelayed");
            }

            if (!input.WsConnected)
            {
                degraded = true;
                issues.Add("WsDisconnected");
            }

            if (input.Rssi <= _options.DegradedRssiThreshold)
            {
                degraded = true;
                issues.Add("LowRssi");
            }

            if (input.EventQueueSize >= _options.DegradedEventQueueThreshold)
            {
                degraded = true;
                issues.Add("HighEventQueue");
            }

            if (input.FreeHeap <= _options.DegradedFreeHeapThreshold)
            {
                degraded = true;
                issues.Add("LowFreeHeap");
            }

            string status;
            if (delayed)
            {
                status = "Delayed";
            }
            else if (degraded)
            {
                status = "Degraded";
            }
            else
            {
                status = "Online";
            }

            return new DeviceHealthEvaluation
            {
                OperationalStatus = status,
                Issues = issues
            };
        }
    }
}