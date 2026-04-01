using cl.MedelCodeFactory.IoT.HeartBeat.DTOs;
using cl.MedelCodeFactory.IoT.HeartBeat.Models;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Services
{
    public interface IHeartbeatService
    {
        Task<HeartbeatProcessResult> ProcessAsync(
            HeartbeatRequestDTO request,
            CancellationToken cancellationToken);
    }
}