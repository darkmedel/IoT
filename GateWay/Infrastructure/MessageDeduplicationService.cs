using System.Collections.Concurrent;

namespace cl.MedelCodeFactory.IoT.GateWay.Infrastructure
{
    public class MessageDeduplicationService
    {
        private readonly ConcurrentDictionary<string, DateTime> _processed =
            new ConcurrentDictionary<string, DateTime>();

        private readonly TimeSpan _retention = TimeSpan.FromMinutes(5);
        private DateTime _lastCleanupUtc = DateTime.UtcNow;

        public bool IsDuplicate(string deviceId, string msgId)
        {
            CleanupIfNeeded();

            string key = BuildKey(deviceId, msgId);
            return _processed.ContainsKey(key);
        }

        public void MarkProcessed(string deviceId, string msgId)
        {
            CleanupIfNeeded();

            string key = BuildKey(deviceId, msgId);
            _processed[key] = DateTime.UtcNow;
        }

        private static string BuildKey(string deviceId, string msgId)
        {
            return $"{deviceId}|{msgId}";
        }

        private void CleanupIfNeeded()
        {
            DateTime now = DateTime.UtcNow;

            if ((now - _lastCleanupUtc) < TimeSpan.FromMinutes(1))
            {
                return;
            }

            DateTime threshold = now - _retention;

            foreach (var item in _processed)
            {
                if (item.Value < threshold)
                {
                    _processed.TryRemove(item.Key, out _);
                }
            }

            _lastCleanupUtc = now;
        }
    }
}