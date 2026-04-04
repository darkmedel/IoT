using cl.MedelCodeFactory.IoT.Almenaras.DTOs;
using cl.MedelCodeFactory.IoT.Almenaras.Models;

namespace cl.MedelCodeFactory.IoT.Almenaras.Repositories
{
    public interface IHeartbeatService
    {
        Task<HeartbeatProcessResult> ProcessAsync(
            HeartbeatRequestDTO request,
            CancellationToken cancellationToken);
    }
}