using cl.MedelCodeFactory.IoT.HeartBeat.DTOs;
using cl.MedelCodeFactory.IoT.HeartBeat.Models;
using cl.MedelCodeFactory.IoT.HeartBeat.Repositories;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Services
{
    public sealed class HeartbeatService : IHeartbeatService
    {
        private const int DeviceIdLength = 12;

        private readonly IHeartbeatRepository _repository;
        private readonly IOperationalStatusEvaluator _statusEvaluator;
        private readonly ILogger<HeartbeatService> _logger;

        public HeartbeatService(
            IHeartbeatRepository repository,
            IOperationalStatusEvaluator statusEvaluator,
            ILogger<HeartbeatService> logger)
        {
            _repository = repository;
            _statusEvaluator = statusEvaluator;
            _logger = logger;
        }

        public async Task<HeartbeatProcessResult> ProcessHeartbeatAsync(
            HeartbeatRequestDTO request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return new HeartbeatProcessResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Code = "INVALID_REQUEST",
                    Message = "El payload recibido no es válido."
                };
            }

            string normalizedDeviceId = NormalizeDeviceId(request.DeviceId);

            if (!IsValidDeviceId(normalizedDeviceId))
            {
                _logger.LogWarning(
                    "Heartbeat rechazado. DeviceId={DeviceId}. Motivo=INVALID_DEVICE_ID",
                    request.DeviceId);

                return new HeartbeatProcessResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Code = "INVALID_DEVICE_ID",
                    Message = "El DeviceId no tiene un formato válido.",
                    DeviceId = request.DeviceId
                };
            }

            request.DeviceId = normalizedDeviceId;

            bool exists = await _repository.DeviceExistsAsync(request.DeviceId, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning(
                    "Heartbeat rechazado. DeviceId={DeviceId}. Motivo=DEVICE_NOT_FOUND",
                    request.DeviceId);

                return new HeartbeatProcessResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Code = "DEVICE_NOT_FOUND",
                    Message = "El dispositivo no existe en el inventario.",
                    DeviceId = request.DeviceId
                };
            }

            bool hasActiveAssignment = await _repository.HasActiveAssignmentAsync(request.DeviceId, cancellationToken);
            if (!hasActiveAssignment)
            {
                _logger.LogWarning(
                    "Heartbeat rechazado. DeviceId={DeviceId}. Motivo=DEVICE_NOT_ASSIGNED",
                    request.DeviceId);

                return new HeartbeatProcessResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Code = "DEVICE_NOT_ASSIGNED",
                    Message = "El dispositivo no tiene una asignación activa en EmpresaDispositivo.",
                    DeviceId = request.DeviceId
                };
            }

            DateTime receivedAtUtc = DateTime.UtcNow;

            DeviceHeartbeatSnapshot transientSnapshot = new DeviceHeartbeatSnapshot
            {
                DeviceId = request.DeviceId,
                Uptime = request.Uptime,
                Rssi = request.Rssi,
                WsConnected = request.WsConnected,
                EventQueueSize = request.EventQueueSize,
                FreeHeap = request.FreeHeap,
                LastHeartbeatReceivedAtUtc = receivedAtUtc
            };

            string operationalStatus = _statusEvaluator.Evaluate(transientSnapshot, receivedAtUtc);

            await _repository.UpsertHeartbeatCurrentAsync(
                request,
                operationalStatus,
                receivedAtUtc,
                cancellationToken);

            await _repository.InsertHeartbeatHistoryAsync(
                request,
                operationalStatus,
                receivedAtUtc,
                cancellationToken);

            _logger.LogInformation(
                "Heartbeat procesado correctamente. DeviceId={DeviceId}, Status={Status}, ReceivedAtUtc={ReceivedAtUtc}",
                request.DeviceId,
                operationalStatus,
                receivedAtUtc);

            return new HeartbeatProcessResult
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Code = "ACK",
                Message = "Heartbeat procesado correctamente.",
                DeviceId = request.DeviceId,
                OperationalStatus = operationalStatus,
                ReceivedAtUtc = receivedAtUtc
            };
        }

        public async Task<DeviceDetailsResponseDTO?> GetDeviceAsync(
            string deviceId,
            CancellationToken cancellationToken)
        {
            string normalizedDeviceId = NormalizeDeviceId(deviceId);

            if (!IsValidDeviceId(normalizedDeviceId))
            {
                return null;
            }

            DeviceHeartbeatSnapshot? snapshot = await _repository.GetDeviceSnapshotAsync(
                normalizedDeviceId,
                cancellationToken);

            if (snapshot == null)
            {
                return null;
            }

            DateTime utcNow = DateTime.UtcNow;
            string effectiveStatus = _statusEvaluator.Evaluate(snapshot, utcNow);

            return new DeviceDetailsResponseDTO
            {
                DeviceId = snapshot.DeviceId,
                DeviceName = snapshot.DeviceName,
                TipoHardware = snapshot.TipoHardware,
                OperationalStatus = effectiveStatus,
                LastHeartbeatReceivedAtUtc = snapshot.LastHeartbeatReceivedAtUtc,
                Uptime = snapshot.Uptime,
                Rssi = snapshot.Rssi,
                WsConnected = snapshot.WsConnected,
                EventQueueSize = snapshot.EventQueueSize,
                FreeHeap = snapshot.FreeHeap,
                IssuesJson = snapshot.IssuesJson,
                EmpresaId = snapshot.EmpresaId,
                EmpresaNombre = snapshot.EmpresaNombre,
                HasActiveAssignment = snapshot.HasActiveAssignment
            };
        }

        public async Task<IReadOnlyList<DeviceListItemDTO>> GetOfflineDevicesAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<DeviceHeartbeatSnapshot> snapshots = await _repository.GetAllCurrentSnapshotsAsync(cancellationToken);
            DateTime utcNow = DateTime.UtcNow;

            return snapshots
                .Select(x => new
                {
                    Snapshot = x,
                    Status = _statusEvaluator.Evaluate(x, utcNow)
                })
                .Where(x => x.Status == "Offline")
                .Select(x => MapListItem(x.Snapshot, x.Status))
                .OrderBy(x => x.LastHeartbeatReceivedAtUtc)
                .ToList();
        }

        public async Task<IReadOnlyList<DeviceListItemDTO>> GetDegradedDevicesAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<DeviceHeartbeatSnapshot> snapshots = await _repository.GetAllCurrentSnapshotsAsync(cancellationToken);
            DateTime utcNow = DateTime.UtcNow;

            return snapshots
                .Select(x => new
                {
                    Snapshot = x,
                    Status = _statusEvaluator.Evaluate(x, utcNow)
                })
                .Where(x => x.Status == "Degraded")
                .Select(x => MapListItem(x.Snapshot, x.Status))
                .OrderBy(x => x.DeviceId)
                .ToList();
        }

        private static DeviceListItemDTO MapListItem(DeviceHeartbeatSnapshot snapshot, string status)
        {
            return new DeviceListItemDTO
            {
                DeviceId = snapshot.DeviceId,
                DeviceName = snapshot.DeviceName,
                TipoHardware = snapshot.TipoHardware,
                OperationalStatus = status,
                LastHeartbeatReceivedAtUtc = snapshot.LastHeartbeatReceivedAtUtc,
                Rssi = snapshot.Rssi,
                WsConnected = snapshot.WsConnected,
                EventQueueSize = snapshot.EventQueueSize,
                FreeHeap = snapshot.FreeHeap,
                EmpresaId = snapshot.EmpresaId,
                EmpresaNombre = snapshot.EmpresaNombre
            };
        }

        private static string NormalizeDeviceId(string deviceId)
        {
            return string.IsNullOrWhiteSpace(deviceId)
                ? string.Empty
                : deviceId.Trim().ToUpperInvariant();
        }

        private static bool IsValidDeviceId(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId) || deviceId.Length != DeviceIdLength)
            {
                return false;
            }

            foreach (char c in deviceId)
            {
                bool isHex =
                    (c >= '0' && c <= '9') ||
                    (c >= 'A' && c <= 'F');

                if (!isHex)
                {
                    return false;
                }
            }

            return true;
        }
    }
}