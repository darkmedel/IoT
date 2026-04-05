using cl.MedelCodeFactory.IoT.Citadel.Contracts;
using cl.MedelCodeFactory.IoT.Citadel.Repositories;

namespace cl.MedelCodeFactory.IoT.Citadel.Endpoints;

public static class DeviceAssignmentEndpoints
{
    public static IEndpointRouteBuilder MapDeviceAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        var assignments = app.MapGroup("/api/asignaciones").WithTags("Asignaciones");

        assignments.MapPost("/", async (
            CreateAssignmentRequest request,
            IEmpresaRepository empresaRepository,
            IDeviceInventoryRepository deviceRepository,
            IDeviceAssignmentRepository assignmentRepository,
            CancellationToken cancellationToken) =>
        {
            var normalizedDeviceId = NormalizeDeviceId(request.DeviceId);

            var empresa = await empresaRepository.GetByIdAsync(request.EmpresaId, cancellationToken);
            if (empresa is null)
            {
                return Results.BadRequest(new { message = $"Empresa {request.EmpresaId} no existe." });
            }

            if (!await deviceRepository.ExistsAsync(normalizedDeviceId, cancellationToken))
            {
                return Results.BadRequest(new { message = $"Device {normalizedDeviceId} no existe." });
            }

            if (await assignmentRepository.HasActiveAssignmentAsync(normalizedDeviceId, cancellationToken))
            {
                return Results.Conflict(new { message = $"El dispositivo {normalizedDeviceId} ya tiene una asignación activa." });
            }

            var created = await assignmentRepository.AssignAsync(
                request with
                {
                    DeviceId = normalizedDeviceId,
                    NombreDispositivo = request.NombreDispositivo?.Trim(),
                    Descripcion = request.Descripcion?.Trim()
                },
                cancellationToken);

            return Results.Created($"/api/devices/{created.DeviceId}/empresa", created);
        });

        assignments.MapPost("/{deviceId}/desasignar", async (
            string deviceId,
            HttpContext httpContext,
            IDeviceAssignmentRepository assignmentRepository,
            CancellationToken cancellationToken) =>
        {
            var normalizedDeviceId = NormalizeDeviceId(deviceId);
            var usuario = httpContext.Request.Headers["X-User"].FirstOrDefault();
            var result = await assignmentRepository.UnassignAsync(normalizedDeviceId, usuario, cancellationToken);

            return result.Unassigned
                ? Results.Ok(result)
                : Results.NotFound(result);
        });

        app.MapGet("/api/devices/{deviceId}/empresa", async (
            string deviceId,
            IDeviceAssignmentRepository assignmentRepository,
            CancellationToken cancellationToken) =>
        {
            var normalizedDeviceId = NormalizeDeviceId(deviceId);
            var row = await assignmentRepository.GetCurrentAssignmentAsync(normalizedDeviceId, cancellationToken);

            return row is null
                ? Results.NotFound(new { message = $"El dispositivo {normalizedDeviceId} no tiene empresa asignada." })
                : Results.Ok(row);
        })
        .WithTags("Asignaciones");

        return app;
    }

    private static string NormalizeDeviceId(string value) => value.Trim().ToUpperInvariant();
}
