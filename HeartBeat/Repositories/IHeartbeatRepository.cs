using cl.MedelCodeFactory.IoT.HeartBeat.DTOs;
using cl.MedelCodeFactory.IoT.HeartBeat.Models;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Repositories
{
    public interface IHeartbeatRepository
    {
        Task<bool> DeviceExistsAsync(string deviceId, CancellationToken cancellationToken);

        Task<bool> HasActiveAssignmentAsync(string deviceId, CancellationToken cancellationToken);

        Task<DateTime?> GetLastHeartbeatReceivedAtUtcAsync(
            string deviceId,
            CancellationToken cancellationToken);

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

        Task<DeviceHeartbeatSnapshot?> GetDeviceSnapshotAsync(
            string deviceId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<DeviceHeartbeatSnapshot>> GetAllCurrentSnapshotsAsync(
            CancellationToken cancellationToken);
    }
}