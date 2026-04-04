using cl.MedelCodeFactory.IoT.Common.Contracts.Heartbeat;

namespace cl.MedelCodeFactory.IoT.Almenaras.Services
{
    public class HeartbeatIngestionService
    {
        public Task<HeartbeatResponse> ProcessAsync(HeartbeatRequest request)
        {
            var response = new HeartbeatResponse
            {
                Success = true,
                DeviceId = request.DeviceId,
                OperationalStatus = "Online",
                Message = "Heartbeat processed."
            };

            return Task.FromResult(response);
        }
    }
}