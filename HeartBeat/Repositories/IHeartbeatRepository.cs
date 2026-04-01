using cl.MedelCodeFactory.IoT.HeartBeat.DTOs;
using cl.MedelCodeFactory.IoT.HeartBeat.Models;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Repositories
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