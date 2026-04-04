using cl.MedelCodeFactory.IoT.WatchTower.Services;

namespace cl.MedelCodeFactory.IoT.WatchTower.Endpoints
{
    public static class DeviceStatusQueryEndpoints
    {
        public static IEndpointRouteBuilder MapDeviceStatusQueryEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/devices", async (
                string? status,
                int? empresaId,
                MonitoringQueryService service,
                CancellationToken cancellationToken) =>
            {
                var devices = await service.GetDevicesAsync(status, empresaId, cancellationToken);
                return Results.Ok(devices);
            });

            app.MapGet("/api/devices/{deviceId}", async (
                string deviceId,
                MonitoringQueryService service,
                CancellationToken cancellationToken) =>
            {
                var device = await service.GetDeviceByIdAsync(deviceId, cancellationToken);

                return device is null
                    ? Results.NotFound(new
                    {
                        success = false,
                        code = "DEVICE_NOT_FOUND",
                        message = "No se encontró el dispositivo solicitado."
                    })
                    : Results.Ok(device);
            });

            app.MapGet("/api/devices/{deviceId}/history", async (
                string deviceId,
                int? limit,
                MonitoringQueryService service,
                CancellationToken cancellationToken) =>
            {
                var history = await service.GetDeviceHistoryAsync(deviceId, limit, cancellationToken);
                return Results.Ok(history);
            });

            return app;
        }
    }
}