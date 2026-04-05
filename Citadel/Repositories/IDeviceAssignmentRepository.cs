using cl.MedelCodeFactory.IoT.Citadel.Contracts;

namespace cl.MedelCodeFactory.IoT.Citadel.Repositories;

public interface IDeviceAssignmentRepository
{
    Task<bool> HasActiveAssignmentAsync(string deviceId, CancellationToken cancellationToken);
    Task<DeviceEmpresaResponse?> GetCurrentAssignmentAsync(string deviceId, CancellationToken cancellationToken);
    Task<DeviceEmpresaResponse> AssignAsync(CreateAssignmentRequest request, CancellationToken cancellationToken);
    Task<UnassignDeviceResponse> UnassignAsync(string deviceId, string? usuario, CancellationToken cancellationToken);
}
