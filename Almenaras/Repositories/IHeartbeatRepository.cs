using cl.MedelCodeFactory.IoT.Almenaras.DTOs;
using cl.MedelCodeFactory.IoT.Almenaras.Models;

namespace cl.MedelCodeFactory.IoT.Almenaras.Repositories
{
    public interface IHeartbeatRepository
    {
        Task<DateTime?> GetLastHeartbeatReceivedAtUtcAsync(string deviceId, CancellationToken cancellationToken);

        Task UpsertHeartbeatCurrentAsync(
            HeartbeatRequestDTO request,
            DeviceHealthEvaluation evaluation,
            DateTime receivedAtUtc,
            CancellationToken cancellationToken);

        Task InsertHeartbeatHistoryAsync(
            HeartbeatRequestDTO request,
            DeviceHealthEvaluation evaluation,
            DateTime receivedAtUtc,
            CancellationToken cancellationToken);
    }
}