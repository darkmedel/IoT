using Almenaras.DTOs;
using Almenaras.Models;

namespace Almenaras.Repositories
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