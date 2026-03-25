using System.Net.WebSockets;
using System.Text;
using cl.MedelCodeFactory.IoT.GateWay.Services;
using cl.MedelCodeFactory.IoT.GateWay.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MessageProcessor>();
builder.Services.AddSingleton<WebSocketConnectionHandler>();
builder.Services.AddSingleton<MessageDeduplicationService>();
builder.Services.AddSingleton<ConnectionRegistry>();

var app = builder.Build();

app.UseWebSockets();

app.MapGet("/", () => Results.Ok("IoT Gateway running"));

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
    var devices = registry.GetAll()
        .Select(x => new
        {
            x.ConnectionId,
            x.DeviceId,
            x.RemoteIp,
            x.ConnectedAtUtc
        });

    return Results.Ok(devices);
});

app.MapPost("/devices/{deviceId}/cmd/status", async (string deviceId, ConnectionRegistry registry) =>
{
    var device = registry.GetByDeviceId(deviceId);

    if (device == null || device.WebSocket == null || device.WebSocket.State != WebSocketState.Open)
    {
        return Results.NotFound($"Device '{deviceId}' not connected.");
    }

    var payload = Encoding.UTF8.GetBytes("CMD|STATUS");

    await device.WebSocket.SendAsync(
        new ArraySegment<byte>(payload),
        WebSocketMessageType.Text,
        true,
        CancellationToken.None);

    return Results.Ok($"CMD|STATUS sent to {deviceId}");
});

app.MapPost("/devices/{deviceId}/cmd/reboot", async (string deviceId, ConnectionRegistry registry) =>
{
    var device = registry.GetByDeviceId(deviceId);

    if (device == null || device.WebSocket == null || device.WebSocket.State != WebSocketState.Open)
    {
        return Results.NotFound($"Device '{deviceId}' not connected.");
    }

    var payload = Encoding.UTF8.GetBytes("CMD|REBOOT");

    await device.WebSocket.SendAsync(
        new ArraySegment<byte>(payload),
        WebSocketMessageType.Text,
        true,
        CancellationToken.None);

    return Results.Ok($"CMD|REBOOT sent to {deviceId}");
});

app.Run("http://0.0.0.0:5000");