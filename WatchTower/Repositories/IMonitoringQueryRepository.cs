using cl.MedelCodeFactory.IoT.WatchTower.Contracts;

namespace cl.MedelCodeFactory.IoT.WatchTower.Repositories
{
    public interface IMonitoringQueryRepository
    {
        Task<IReadOnlyList<DeviceListItemDto>> GetDevicesAsync(string? status, int? empresaId, CancellationToken cancellationToken);
        Task<DeviceDetailDto?> GetDeviceByIdAsync(string deviceId, CancellationToken cancellationToken);
        Task<IReadOnlyList<DeviceHistoryItemDto>> GetDeviceHistoryAsync(string deviceId, int limit, CancellationToken cancellationToken);
    }
}