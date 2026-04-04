using cl.MedelCodeFactory.IoT.Almenaras.DTOs;
using cl.MedelCodeFactory.IoT.Almenaras.Services;

namespace cl.MedelCodeFactory.IoT.Almenaras.Endpoints
{
    public static class HeartbeatEndpoints
    {
        public static IEndpointRouteBuilder MapHeartbeatEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/heartbeat", async (
                HeartbeatRequestDTO request,
                HeartbeatIngestionService service) =>
            {
                var result = await service.ProcessAsync(request, CancellationToken.None);

                var response = new HeartbeatAckResponseDTO
                {
                    Success = result.Success,
                    Code = result.Code,
                    Message = result.Message,
                    DeviceId = result.DeviceId ?? string.Empty,
                    Status = result.OperationalStatus ?? "Unknown",
                    ReceivedAtUtc = result.ReceivedAtUtc ?? DateTime.UtcNow
                };

                if (!result.Success)
                {
                    return Results.BadRequest(response);
                }

                return Results.Ok(response);
            })
            .WithName("PostHeartbeat");

            return app;
        }
    }
}