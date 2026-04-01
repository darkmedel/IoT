using cl.MedelCodeFactory.IoT.HeartBeat.DTOs;
using cl.MedelCodeFactory.IoT.HeartBeat.Models;
using cl.MedelCodeFactory.IoT.HeartBeat.Repositories;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Services
{
    public sealed class HeartbeatService : IHeartbeatService
    {
        private const int DeviceIdLength = 12;

        private readonly IHeartbeatRepository _repository;
        private readonly IOperationalStatusEvaluator _evaluator;
        private readonly ILogger<HeartbeatService> _logger;

        public HeartbeatService(
            IHeartbeatRepository repository,
            IOperationalStatusEvaluator evaluator,
            ILogger<HeartbeatService> logger)
        {
            _repository = repository;
            _evaluator = evaluator;
            _logger = logger;
        }

        public async Task<HeartbeatProcessResult> ProcessAsync(
            HeartbeatRequestDTO request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return Fail(
                    code: "INVALID_REQUEST",
                    message: "El payload recibido no es válido.");
            }

            string normalizedDeviceId = NormalizeDeviceId(request.DeviceId);

            if (!IsValidDeviceId(normalizedDeviceId))
            {
                _logger.LogWarning(
                    "Heartbeat rechazado. DeviceId={DeviceId}. Motivo=INVALID_DEVICE_ID",
                    request.DeviceId);

                return Fail(
                    code: "INVALID_DEVICE_ID",
                    message: "El DeviceId no tiene un formato válido.",
                    deviceId: request.DeviceId);
            }

            request.DeviceId = normalizedDeviceId;

            DateTime receivedAtUtc = DateTime.UtcNow;

            DateTime? previousHeartbeatReceivedAtUtc =
                await _repository.GetLastHeartbeatReceivedAtUtcAsync(
                    request.DeviceId,
                    cancellationToken);

            var evaluationInput = new HeartbeatEvaluationInput
            {
                DeviceId = request.DeviceId,
                Uptime = request.Uptime,
                Rssi = request.Rssi,
                WsConnected = request.WsConnected,
                EventQueueSize = request.EventQueueSize,
                FreeHeap = request.FreeHeap,
                ReceivedAtUtc = receivedAtUtc,
                PreviousHeartbeatReceivedAtUtc = previousHeartbeatReceivedAtUtc
            };

            DeviceHealthEvaluation evaluation =
                _evaluator.Evaluate(evaluationInput, receivedAtUtc);

            await _repository.UpsertHeartbeatCurrentAsync(
                request,
                evaluation,
                receivedAtUtc,
                cancellationToken);

            await _repository.InsertHeartbeatHistoryAsync(
                request,
                evaluation,
                receivedAtUtc,
                cancellationToken);

            string? issuesJson = evaluation.GetIssuesJson();

            _logger.LogInformation(
                "Heartbeat procesado. DeviceId={DeviceId}, Status={Status}, IssuesJson={IssuesJson}, ReceivedAtUtc={ReceivedAtUtc}",
                request.DeviceId,
                evaluation.OperationalStatus,
                issuesJson,
                receivedAtUtc);

            return new HeartbeatProcessResult
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Code = "ACK",
                Message = "Heartbeat procesado correctamente.",
                DeviceId = request.DeviceId,
                OperationalStatus = evaluation.OperationalStatus,
                IssuesJson = issuesJson,
                ReceivedAtUtc = receivedAtUtc
            };
        }

        private static HeartbeatProcessResult Fail(
            string code,
            string message,
            string? deviceId = null)
        {
            return new HeartbeatProcessResult
            {
                Success = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Code = code,
                Message = message,
                DeviceId = deviceId
            };
        }

        private static string NormalizeDeviceId(string? deviceId)
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
                bool isHex = (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F');
                if (!isHex)
                {
                    return false;
                }
            }

            return true;
        }
    }
}