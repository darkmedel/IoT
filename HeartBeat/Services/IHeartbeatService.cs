using cl.MedelCodeFactory.IoT.HeartBeat.DTOs;
using cl.MedelCodeFactory.IoT.HeartBeat.Models;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Services
{
    public interface IHeartbeatService
    {
        Task<HeartbeatProcessResult> ProcessHeartbeatAsync(
            HeartbeatRequestDTO request,
            CancellationToken cancellationToken);

        Task<DeviceDetailsResponseDTO?> GetDeviceAsync(
            string deviceId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<DeviceListItemDTO>> GetOfflineDevicesAsync(
            CancellationToken cancellationToken);

        Task<IReadOnlyList<DeviceListItemDTO>> GetDegradedDevicesAsync(
            CancellationToken cancellationToken);
    }
}