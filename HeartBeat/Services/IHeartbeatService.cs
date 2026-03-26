using cl.MedelCodeFactory.IoT.HeartBeat.DTOs;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Services
{
    public interface IHeartbeatService
    {
        Task<HeartbeatAckResponse> ProcessAsync(HeartbeatRequest request, CancellationToken cancellationToken);
    }
}