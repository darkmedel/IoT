using cl.MedelCodeFactory.IoT.HeartBeat.Configuration;
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

        public string Evaluate(DeviceHeartbeatSnapshot snapshot, DateTime utcNow)
        {
            if (snapshot == null)
            {
                return StatusOffline;
            }

            bool hasHeartbeat = HasHeartbeat(snapshot);
            bool wsConnected = IsWebSocketConnected(snapshot);

            // Caso extremo:
            // No hay heartbeat registrado.
            // - Si tampoco hay WS, el dispositivo está realmente Offline.
            // - Si el WS aparece como activo, el dispositivo sigue vivo pero inconsistente => Degraded.
            if (!hasHeartbeat)
            {
                return wsConnected
                    ? StatusDegraded
                    : StatusOffline;
            }

            TimeSpan heartbeatAge = GetHeartbeatAge(snapshot, utcNow);

            bool heartbeatOffline = IsHeartbeatOffline(heartbeatAge);
            bool heartbeatDelayed = IsHeartbeatDelayed(heartbeatAge);
            bool websocketDegraded = IsWebSocketDegraded(snapshot);
            bool metricsDegraded = IsMetricsDegraded(snapshot);

            // Prioridad 1:
            // Heartbeat demasiado antiguo.
            // - Sin WS => Offline
            // - Con WS => Degraded (sigue vivo, pero el canal HB falló o está desfasado)
            if (heartbeatOffline)
            {
                return wsConnected
                    ? StatusDegraded
                    : StatusOffline;
            }

            // Prioridad 2:
            // Heartbeat atrasado, pero aún no vencido.
            // - Si además WS o métricas están mal => Degraded
            // - Si no => Delayed
            if (heartbeatDelayed)
            {
                return (websocketDegraded || metricsDegraded)
                    ? StatusDegraded
                    : StatusDelayed;
            }

            // Prioridad 3:
            // Heartbeat reciente, pero hay degradaciones operativas.
            if (websocketDegraded || metricsDegraded)
            {
                return StatusDegraded;
            }

            // Todo sano.
            return StatusOnline;
        }

        private static bool HasHeartbeat(DeviceHeartbeatSnapshot snapshot)
        {
            return snapshot.LastHeartbeatReceivedAtUtc.HasValue;
        }

        private static bool IsWebSocketConnected(DeviceHeartbeatSnapshot snapshot)
        {
            return snapshot.WsConnected.GetValueOrDefault(false);
        }

        private static TimeSpan GetHeartbeatAge(DeviceHeartbeatSnapshot snapshot, DateTime utcNow)
        {
            return utcNow - snapshot.LastHeartbeatReceivedAtUtc!.Value;
        }

        private bool IsHeartbeatOffline(TimeSpan heartbeatAge)
        {
            return heartbeatAge.TotalSeconds > _options.OfflineThresholdSeconds;
        }

        private bool IsHeartbeatDelayed(TimeSpan heartbeatAge)
        {
            return heartbeatAge.TotalSeconds > _options.DelayedThresholdSeconds;
        }

        private static bool IsWebSocketDegraded(DeviceHeartbeatSnapshot snapshot)
        {
            return snapshot.WsConnected.HasValue && !snapshot.WsConnected.Value;
        }

        private bool IsMetricsDegraded(DeviceHeartbeatSnapshot snapshot)
        {
            return IsRssiDegraded(snapshot)
                || IsEventQueueDegraded(snapshot)
                || IsFreeHeapDegraded(snapshot);
        }

        private bool IsRssiDegraded(DeviceHeartbeatSnapshot snapshot)
        {
            return snapshot.Rssi.HasValue
                && snapshot.Rssi.Value <= _options.DegradedRssiThreshold;
        }

        private bool IsEventQueueDegraded(DeviceHeartbeatSnapshot snapshot)
        {
            return snapshot.EventQueueSize.HasValue
                && snapshot.EventQueueSize.Value >= _options.DegradedEventQueueThreshold;
        }

        private bool IsFreeHeapDegraded(DeviceHeartbeatSnapshot snapshot)
        {
            return snapshot.FreeHeap.HasValue
                && snapshot.FreeHeap.Value <= _options.DegradedFreeHeapThreshold;
        }
    }
}