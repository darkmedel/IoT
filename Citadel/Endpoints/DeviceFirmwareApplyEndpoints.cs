using cl.MedelCodeFactory.IoT.Citadel.Services;
using cl.MedelCodeFactory.IoT.Common.Contracts.Firmware;

namespace cl.MedelCodeFactory.IoT.Citadel.Endpoints
{
    public static class DeviceFirmwareApplyEndpoints
    {
        public static IEndpointRouteBuilder MapDeviceFirmwareApplyEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/devices/{deviceId}/firmware/apply", async (
                string deviceId,
                FirmwareApplyRequest request,
                BifrostOtaClient bifrostOtaClient,
                CancellationToken cancellationToken) =>
            {
                if (!string.Equals(deviceId, request.DeviceId, StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { message = "Route deviceId and body DeviceId do not match." });
                }

                var result = await bifrostOtaClient.ApplyOtaAsync(request, cancellationToken);
                return Results.Ok(result);
            });

            return app;
        }
    }
}