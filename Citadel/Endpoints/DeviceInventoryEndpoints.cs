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
                ? Results.NotFound(new { message = $"El dispositivo {normalizedDeviceId} no existe." })
                : Results.Ok(row);
        });

        group.MapPost("/", async (CreateDeviceRequest request, IDeviceInventoryRepository repository, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId))
            {
                return Results.BadRequest(new { message = "deviceId es obligatorio." });
            }

            var normalizedDeviceId = NormalizeDeviceId(request.DeviceId);

            if (normalizedDeviceId.Length != 12)
            {
                return Results.BadRequest(new { message = "deviceId debe tener exactamente 12 caracteres." });
            }

            if (!IsHex(normalizedDeviceId))
            {
                return Results.BadRequest(new { message = "deviceId debe contener solo caracteres hexadecimales en mayúscula." });
            }

            if (await repository.ExistsAsync(normalizedDeviceId, cancellationToken))
            {
                return Results.Conflict(new { message = $"El dispositivo {normalizedDeviceId} ya existe." });
            }

            if (!await repository.HardwareTypeExistsAsync(request.TipoHardwareId, cancellationToken))
            {
                return Results.BadRequest(new { message = $"TipoHardwareId {request.TipoHardwareId} no existe o está deshabilitado." });
            }

            if (request.FirmwareId.HasValue)
            {
                if (!await repository.FirmwareExistsAsync(request.FirmwareId.Value, cancellationToken))
                {
                    return Results.BadRequest(new { message = $"FirmwareId {request.FirmwareId.Value} no existe o está deshabilitado." });
                }

                if (!await repository.FirmwareBelongsToHardwareAsync(request.FirmwareId.Value, request.TipoHardwareId, cancellationToken))
                {
                    return Results.BadRequest(new { message = $"FirmwareId {request.FirmwareId.Value} no pertenece al TipoHardwareId {request.TipoHardwareId}." });
                }
            }

            var created = await repository.CreateAsync(
                request with
                {
                    DeviceId = normalizedDeviceId,
                    FirmwareVersion = string.IsNullOrWhiteSpace(request.FirmwareVersion)
                        ? "UNKNOWN"
                        : request.FirmwareVersion.Trim()
                },
                cancellationToken);

            return Results.Created($"/api/devices/{created.DeviceId}", created);
        });

        return app;
    }

    private static string NormalizeDeviceId(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static bool IsHex(string value)
    {
        return value.All(c =>
            (c >= '0' && c <= '9') ||
            (c >= 'A' && c <= 'F'));
    }
}
