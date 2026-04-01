using System.Text.Json;
using cl.MedelCodeFactory.IoT.GateWay.Services;

namespace cl.MedelCodeFactory.IoT.GateWay.Endpoints
{
    public static class DeviceEndpoints
    {
        public static void MapDeviceEndpoints(this WebApplication app)
        {
            app.MapPost("/devices/{deviceId}/config",
                async (string deviceId, JsonElement body, DeviceCommandSender sender, CancellationToken ct) =>
                {
                    var rawJson = body.GetRawText();

                    int configVersion = 0;
                    if (body.TryGetProperty("configVersion", out var versionProperty) &&
                        versionProperty.ValueKind == JsonValueKind.Number)
                    {
                        configVersion = versionProperty.GetInt32();
                    }

                    var payload = $"CFG_BTN|{configVersion}|{rawJson}";
                    var result = await sender.SendTextAsync(deviceId, payload, ct);

                    if (!result.Success)
                    {
                        return Results.NotFound(new
                        {
                            deviceId,
                            message = result.Error
                        });
                    }

                    return Results.Ok(new
                    {
                        deviceId,
                        configVersion,
                        dispatched = true
                    });
                });
        }
    }
}