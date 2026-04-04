using cl.MedelCodeFactory.IoT.Almenaras.DTOs;
using cl.MedelCodeFactory.IoT.Almenaras.Models;
using cl.MedelCodeFactory.IoT.Almenaras.Repositories;

namespace cl.MedelCodeFactory.IoT.Almenaras.Services
{
    public sealed class HeartbeatIngestionService
    {
        private readonly IHeartbeatService _heartbeatService;

        public HeartbeatIngestionService(IHeartbeatService heartbeatService)
        {
            _heartbeatService = heartbeatService;
        }

        public Task<HeartbeatProcessResult> ProcessAsync(
            HeartbeatRequestDTO request,
            CancellationToken cancellationToken)
        {
            return _heartbeatService.ProcessAsync(request, cancellationToken);
        }
    }
}