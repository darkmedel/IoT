using cl.MedelCodeFactory.IoT.Common.Contracts.Devices;

namespace cl.MedelCodeFactory.IoT.Citadel.Endpoints
{
    public static class DeviceAssignmentEndpoints
    {
        public static IEndpointRouteBuilder MapDeviceAssignmentEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/devices/{deviceId}/assign", (string deviceId, DeviceAssignmentRequest request) =>
            {
                if (!string.Equals(deviceId, request.DeviceId, StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { message = "Route deviceId and body DeviceId do not match." });
                }

                return Results.Ok(new
                {
                    success = true,
                    action = "device_assigned_stub",
                    request.DeviceId,
                    request.EmpresaId
                });
            });

            return app;
        }
    }
}