using cl.MedelCodeFactory.IoT.Bifrost.Application;
using cl.MedelCodeFactory.IoT.Bifrost.Handlers;
using cl.MedelCodeFactory.IoT.Bifrost.Infrastructure;
using cl.MedelCodeFactory.IoT.Bifrost.Services;
using cl.MedelCodeFactory.IoT.Bifrost.WebSockets;
using Common.Contracts.Commands;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<MessageDeduplicationService>();
builder.Services.AddSingleton<ConnectionRegistry>();
builder.Services.AddSingleton<MessageProcessor>();
builder.Services.AddSingleton<WebSocketConnectionHandler>();
builder.Services.AddSingleton<DeviceCommandSender>();
builder.Services.AddSingleton<OtaCommandBuilder>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseWebSockets();

app.MapGet("/", () => Results.Ok(new
{
    service = "IoT.Bifrost",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new
{
    service = "IoT.Bifrost",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow
}));

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket request expected.");
        return;
    }

    var handler = context.RequestServices.GetRequiredService<WebSocketConnectionHandler>();
    await handler.HandleAsync(context);
});

app.MapGet("/devices", (ConnectionRegistry registry) =>
{
    var devices = registry.GetAll();
    return Results.Ok(devices.Select(x => new
    {
        x.ConnectionId,
        x.DeviceId,
        x.RemoteIp,
        x.ConnectedAtUtc,
        WebSocketState = x.WebSocket?.State.ToString()
    }));
});

app.MapPost("/devices/{deviceId}/cmd/status", async (string deviceId, DeviceCommandSender sender) =>
{
    var result = await sender.SendAsync(deviceId, "CMD|STATUS");
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
});

app.MapPost("/devices/{deviceId}/cmd/reboot", async (string deviceId, DeviceCommandSender sender) =>
{
    var result = await sender.SendAsync(deviceId, "CMD|REBOOT");
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
});

app.MapPost("/devices/{deviceId}/config", async (
    string deviceId,
    ButtonConfigRequest request,
    DeviceCommandSender sender) =>
{
    if (!string.Equals(deviceId, request.DeviceId, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { message = "Route deviceId and body DeviceId do not match." });
    }

    var payload = System.Text.Json.JsonSerializer.Serialize(request);
    var result = await sender.SendAsync(deviceId, $"CFG_BTN|{payload}");

    return result.Success ? Results.Ok(result) : Results.NotFound(result);
});

app.MapPost("/devices/{deviceId}/ota", async (
    string deviceId,
    cl.MedelCodeFactory.IoT.Common.Contracts.Commands.OtaRequest request,
    OtaCommandBuilder otaCommandBuilder,
    DeviceCommandSender sender) =>
{
    if (!string.Equals(deviceId, request.DeviceId, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { message = "Route deviceId and body DeviceId do not match." });
    }

    var command = otaCommandBuilder.Build(request);
    var result = await sender.SendAsync(deviceId, command);

    return result.Success ? Results.Ok(result) : Results.NotFound(result);
});

app.Run();