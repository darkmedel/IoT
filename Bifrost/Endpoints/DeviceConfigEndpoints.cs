using System.Text.Json;
using cl.MedelCodeFactory.IoT.Common.Contracts.Commands;
using cl.MedelCodeFactory.IoT.Bifrost.Infrastructure;
using cl.MedelCodeFactory.IoT.Bifrost.Application;

namespace cl.MedelCodeFactory.IoT.GateWay.Endpoints
{
    public static class DeviceConfigEndpoints
    {
        public static IEndpointRouteBuilder MapDeviceConfigEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/devices/{deviceId}/config", async (
                string deviceId,
                ButtonConfigRequest request,
                DeviceCommandSender sender) =>
            {
                if (!string.Equals(deviceId, request.DeviceId, StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { message = "Route deviceId and body DeviceId do not match." });
                }

                string payload = JsonSerializer.Serialize(request);
                var result = await sender.SendAsync(deviceId, $"CFG_BTN|{payload}");

                return result.Success
                    ? Results.Ok(result)
                    : Results.NotFound(result);
            });

            return app;
        }
    }
}