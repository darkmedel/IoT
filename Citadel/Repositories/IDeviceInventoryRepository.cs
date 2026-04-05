using cl.MedelCodeFactory.IoT.Citadel.Contracts;

namespace cl.MedelCodeFactory.IoT.Citadel.Repositories;

public interface IDeviceInventoryRepository
{
    Task<IReadOnlyList<DeviceResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<DeviceResponse?> GetByIdAsync(string deviceId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string deviceId, CancellationToken cancellationToken);
    Task<bool> HardwareTypeExistsAsync(int tipoHardwareId, CancellationToken cancellationToken);
    Task<DeviceResponse> CreateAsync(CreateDeviceRequest request, CancellationToken cancellationToken);
}
