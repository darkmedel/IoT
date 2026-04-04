using Almenaras.DTOs;
using Almenaras.Models;

namespace Almenaras.Repositories
{
    public interface IHeartbeatService
    {
        Task<HeartbeatProcessResult> ProcessAsync(
            HeartbeatRequestDTO request,
            CancellationToken cancellationToken);
    }
}