using System.Text.Json;
using cl.MedelCodeFactory.IoT.GateWay.Application;

namespace cl.MedelCodeFactory.IoT.GateWay.Endpoints
{
    public static class DeviceEndpoints
    {
        public static void MapDeviceEndpoints(this WebApplication app)
        {
            app.MapPost("/devices/{deviceId}/cmd/status",
                async (string deviceId, DeviceCommandSender sender, CancellationToken ct) =>
                {
                    var result = await sender.SendStatusAsync(deviceId, ct);

                    return ToHttpResult(result);
                });

            app.MapPost("/devices/{deviceId}/cmd/reboot",
                async (string deviceId, DeviceCommandSender sender, CancellationToken ct) =>
                {
                    var result = await sender.SendRebootAsync(deviceId, ct);

                    return ToHttpResult(result);
                });

            app.MapPost("/devices/{deviceId}/config",
                async (string deviceId, JsonElement body, DeviceCommandSender sender, CancellationToken ct) =>
                {
                    string rawJson = body.GetRawText();

                    int configVersion = 0;
                    if (body.TryGetProperty("configVersion", out var versionProperty) &&
                        versionProperty.ValueKind == JsonValueKind.Number)
                    {
                        configVersion = versionProperty.GetInt32();
                    }

                    string payload = $"CFG_BTN|{configVersion}|{rawJson}";

                    var result = await sender.SendAsync(deviceId, payload, ct);

                    if (!result.Success)
                    {
                        return ToHttpResult(result);
                    }

                    return Results.Ok(new
                    {
                        success = true,
                        deviceId,
                        command = payload,
                        configVersion,
                        code = result.Code,
                        message = result.Message
                    });
                });
        }

        private static IResult ToHttpResult(DeviceCommandSendResult result)
        {
            if (result.Success)
            {
                return Results.Ok(new
                {
                    success = result.Success,
                    deviceId = result.DeviceId,
                    command = result.Command,
                    code = result.Code,
                    message = result.Message
                });
            }

            return result.Code switch
            {
                "INVALID_REQUEST" => Results.BadRequest(new
                {
                    success = result.Success,
                    deviceId = result.DeviceId,
                    command = result.Command,
                    code = result.Code,
                    message = result.Message
                }),

                "DEVICE_NOT_CONNECTED" => Results.NotFound(new
                {
                    success = result.Success,
                    deviceId = result.DeviceId,
                    command = result.Command,
                    code = result.Code,
                    message = result.Message
                }),

                _ => Results.Problem(
                    detail: result.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: result.Code)
            };
        }
    }
}