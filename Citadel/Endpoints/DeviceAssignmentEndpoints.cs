using cl.MedelCodeFactory.IoT.Citadel.Contracts;
using cl.MedelCodeFactory.IoT.Citadel.Repositories;

namespace cl.MedelCodeFactory.IoT.Citadel.Endpoints;

public static class DeviceAssignmentEndpoints
{
    public static IEndpointRouteBuilder MapDeviceAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/asignaciones", async (
            CreateAssignmentRequest request,
            IEmpresaRepository empresaRepository,
            IDeviceInventoryRepository deviceRepository,
            IDeviceAssignmentRepository assignmentRepository,
            CancellationToken cancellationToken) =>
        {
            if (request.EmpresaId <= 0)
            {
                return Results.BadRequest(new { message = "empresaId es obligatorio y debe ser mayor a 0." });
            }

            if (string.IsNullOrWhiteSpace(request.DeviceId))
            {
                return Results.BadRequest(new { message = "deviceId es obligatorio." });
            }

            if (string.IsNullOrWhiteSpace(request.NombreDispositivo))
            {
                return Results.BadRequest(new { message = "nombreDispositivo es obligatorio." });
            }

            if (string.IsNullOrWhiteSpace(request.Descripcion))
            {
                return Results.BadRequest(new { message = "descripcion es obligatoria." });
            }

            var normalizedDeviceId = request.DeviceId.Trim().ToUpperInvariant();

            if (!await empresaRepository.ExistsAsync(request.EmpresaId, cancellationToken))
            {
                return Results.BadRequest(new { message = $"La empresa {request.EmpresaId} no existe." });
            }

            if (!await deviceRepository.ExistsAsync(normalizedDeviceId, cancellationToken))
            {
                return Results.BadRequest(new { message = $"El dispositivo {normalizedDeviceId} no existe." });
            }

            if (await assignmentRepository.HasActiveAssignmentAsync(normalizedDeviceId, cancellationToken))
            {
                return Results.Conflict(new { message = $"El dispositivo {normalizedDeviceId} ya tiene una asignación activa." });
            }

            var created = await assignmentRepository.AssignAsync(
                request with
                {
                    DeviceId = normalizedDeviceId,
                    NombreDispositivo = request.NombreDispositivo.Trim(),
                    Descripcion = request.Descripcion.Trim()
                },
                cancellationToken);

            return Results.Ok(created);
        })
        .WithTags("Asignaciones");

        app.MapPost("/api/asignaciones/{deviceId}/desasignar", async (
            string deviceId,
            string? usuario,
            IDeviceAssignmentRepository assignmentRepository,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Results.BadRequest(new { message = "deviceId es obligatorio." });
            }

            var normalizedDeviceId = deviceId.Trim().ToUpperInvariant();
            var result = await assignmentRepository.UnassignAsync(normalizedDeviceId, usuario, cancellationToken);

            return result.Unassigned
                ? Results.Ok(result)
                : Results.NotFound(result);
        })
        .WithTags("Asignaciones");

        app.MapGet("/api/devices/{deviceId}/empresa", async (
            string deviceId,
            IDeviceAssignmentRepository assignmentRepository,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Results.BadRequest(new { message = "deviceId es obligatorio." });
            }

            var normalizedDeviceId = deviceId.Trim().ToUpperInvariant();
            var current = await assignmentRepository.GetCurrentAssignmentAsync(normalizedDeviceId, cancellationToken);

            return current is null
                ? Results.NotFound(new { message = $"El dispositivo {normalizedDeviceId} no tiene empresa asignada." })
                : Results.Ok(current);
        })
        .WithTags("Asignaciones");

        return app;
    }
}
