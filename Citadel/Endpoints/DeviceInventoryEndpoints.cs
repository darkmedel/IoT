using cl.MedelCodeFactory.IoT.Citadel.Contracts;
using cl.MedelCodeFactory.IoT.Citadel.Repositories;

namespace cl.MedelCodeFactory.IoT.Citadel.Endpoints;

public static class DeviceInventoryEndpoints
{
    public static IEndpointRouteBuilder MapDeviceInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/devices").WithTags("Devices");

        group.MapGet("/", async (IDeviceInventoryRepository repository, CancellationToken cancellationToken) =>
        {
            var rows = await repository.GetAllAsync(cancellationToken);
            return Results.Ok(rows);
        });

        group.MapGet("/{deviceId}", async (string deviceId, IDeviceInventoryRepository repository, CancellationToken cancellationToken) =>
        {
            var normalizedDeviceId = NormalizeDeviceId(deviceId);
            var row = await repository.GetByIdAsync(normalizedDeviceId, cancellationToken);

            return row is null
                ? Results.NotFound(new { message = $"Device {normalizedDeviceId} no existe." })
                : Results.Ok(row);
        });

        group.MapPost("/", async (CreateDeviceRequest request, IDeviceInventoryRepository repository, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId))
            {
                return Results.BadRequest(new { message = "deviceId es obligatorio." });
            }

            if (string.IsNullOrWhiteSpace(request.Nombre))
            {
                return Results.BadRequest(new { message = "nombre es obligatorio." });
            }

            var normalizedDeviceId = NormalizeDeviceId(request.DeviceId);
            if (normalizedDeviceId.Length != 12)
            {
                return Results.BadRequest(new { message = "deviceId debe tener exactamente 12 caracteres." });
            }

            if (await repository.ExistsAsync(normalizedDeviceId, cancellationToken))
            {
                return Results.Conflict(new { message = $"El dispositivo {normalizedDeviceId} ya existe." });
            }

            if (!await repository.HardwareTypeExistsAsync(request.TipoHardwareId, cancellationToken))
            {
                return Results.BadRequest(new { message = $"TipoHardwareId {request.TipoHardwareId} no existe." });
            }

            var created = await repository.CreateAsync(
                request with
                {
                    DeviceId = normalizedDeviceId,
                    Nombre = request.Nombre.Trim(),
                    Descripcion = request.Descripcion?.Trim(),
                    SerialNumber = request.SerialNumber?.Trim(),
                    FirmwareVersion = request.FirmwareVersion?.Trim(),
                    MacAddress = request.MacAddress?.Trim().ToUpperInvariant()
                },
                cancellationToken);

            return Results.Created($"/api/devices/{created.DeviceId}", created);
        });

        return app;
    }

    private static string NormalizeDeviceId(string value) => value.Trim().ToUpperInvariant();
}
