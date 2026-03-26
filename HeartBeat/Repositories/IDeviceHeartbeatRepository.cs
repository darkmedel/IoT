using cl.MedelCodeFactory.IoT.HeartBeat.DTOs;
using cl.MedelCodeFactory.IoT.HeartBeat.Models;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Repositories
{
    public interface IHeartbeatRepository
    {
        Task<DeviceInventoryRecord?> GetDeviceAsync(string deviceId, CancellationToken cancellationToken);
        Task SaveHeartbeatAsync(
            HeartbeatRequest request,
            string operationalStatus,
            string? issuesJson,
            DateTime receivedAtUtc,
            CancellationToken cancellationToken);
    }
}