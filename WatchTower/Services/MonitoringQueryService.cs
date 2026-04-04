using cl.MedelCodeFactory.IoT.WatchTower.Configuration;
using cl.MedelCodeFactory.IoT.WatchTower.Contracts;
using cl.MedelCodeFactory.IoT.WatchTower.Repositories;
using Microsoft.Extensions.Options;

namespace cl.MedelCodeFactory.IoT.WatchTower.Services
{
    public sealed class MonitoringQueryService
    {
        private readonly IMonitoringQueryRepository _repository;
        private readonly WatchTowerOptions _options;

        public MonitoringQueryService(
            IMonitoringQueryRepository repository,
            IOptions<WatchTowerOptions> options)
        {
            _repository = repository;
            _options = options.Value;
        }

        public Task<IReadOnlyList<DeviceListItemDto>> GetDevicesAsync(
            string? status,
            int? empresaId,
            CancellationToken cancellationToken)
        {
            status = NormalizeStatus(status);
            return _repository.GetDevicesAsync(status, empresaId, cancellationToken);
        }

        public Task<DeviceDetailDto?> GetDeviceByIdAsync(
            string deviceId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentException("deviceId es obligatorio.", nameof(deviceId));
            }

            return _repository.GetDeviceByIdAsync(deviceId.Trim(), cancellationToken);
        }

        public Task<IReadOnlyList<DeviceHistoryItemDto>> GetDeviceHistoryAsync(
            string deviceId,
            int? limit,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentException("deviceId es obligatorio.", nameof(deviceId));
            }

            var safeLimit = limit ?? _options.DefaultHistoryLimit;

            if (safeLimit <= 0)
            {
                safeLimit = _options.DefaultHistoryLimit;
            }

            if (safeLimit > _options.MaxHistoryLimit)
            {
                safeLimit = _options.MaxHistoryLimit;
            }

            return _repository.GetDeviceHistoryAsync(deviceId.Trim(), safeLimit, cancellationToken);
        }

        private static string? NormalizeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return null;
            }

            var normalized = status.Trim();

            return normalized.ToLowerInvariant() switch
            {
                "online" => "Online",
                "degraded" => "Degraded",
                "offline" => "Offline",
                "delayed" => "Delayed",
                _ => normalized
            };
        }
    }
}