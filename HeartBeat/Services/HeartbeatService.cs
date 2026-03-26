using System.Text.Json;
using cl.MedelCodeFactory.IoT.HeartBeat.DTOs;
using cl.MedelCodeFactory.IoT.HeartBeat.Enums;
using cl.MedelCodeFactory.IoT.HeartBeat.Repositories;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Services
{
    public class HeartbeatService : IHeartbeatService
    {
        private readonly IHeartbeatRepository _repository;
        private readonly ILogger<HeartbeatService> _logger;

        public HeartbeatService(
            IHeartbeatRepository repository,
            ILogger<HeartbeatService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<HeartbeatAckResponse> ProcessAsync(
            HeartbeatRequest request,
            CancellationToken cancellationToken)
        {
            var device = await _repository.GetDeviceAsync(request.DeviceId, cancellationToken);

            if (device == null)
            {
                _logger.LogWarning("Heartbeat rechazado. DeviceId no registrado: {DeviceId}", request.DeviceId);
                throw new InvalidOperationException("DEVICE_NOT_REGISTERED");
            }

            if (!device.Habilitado || device.FechaBaja.HasValue)
            {
                _logger.LogWarning("Heartbeat rechazado. DeviceId deshabilitado o dado de baja: {DeviceId}", request.DeviceId);
                throw new UnauthorizedAccessException("DEVICE_DISABLED");
            }

            DateTime receivedAtUtc = DateTime.UtcNow;

            List<string> issues = new List<string>();
            DeviceOperationalStatus status = CalculateStatus(request, issues);

            string? issuesJson = issues.Count == 0
                ? null
                : JsonSerializer.Serialize(issues);

            await _repository.SaveHeartbeatAsync(
                request,
                status.ToString(),
                issuesJson,
                receivedAtUtc,
                cancellationToken);

            _logger.LogInformation(
                "Heartbeat aceptado. DeviceId: {DeviceId}, Status: {Status}, Rssi: {Rssi}, WsConnected: {WsConnected}, Queue: {Queue}, FreeHeap: {FreeHeap}",
                request.DeviceId,
                status,
                request.Rssi,
                request.WsConnected,
                request.EventQueueSize,
                request.FreeHeap);

            return new HeartbeatAckResponse
            {
                Ack = true,
                DeviceId = request.DeviceId,
                ReceivedAtUtc = receivedAtUtc,
                Status = status.ToString(),
                Message = "Heartbeat accepted."
            };
        }

        private static DeviceOperationalStatus CalculateStatus(
            HeartbeatRequest request,
            List<string> issues)
        {
            bool degraded = false;

            if (!request.WsConnected)
            {
                degraded = true;
                issues.Add("WebSocketDisconnected");
            }

            if (request.Rssi <= -80)
            {
                degraded = true;
                issues.Add("WeakSignal");
            }

            if (request.EventQueueSize >= 10)
            {
                degraded = true;
                issues.Add("HighEventQueue");
            }

            if (request.FreeHeap < 65536)
            {
                degraded = true;
                issues.Add("LowMemory");
            }

            return degraded
                ? DeviceOperationalStatus.Degraded
                : DeviceOperationalStatus.Online;
        }
    }
}