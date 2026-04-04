using Bifrost.Application;
using Bifrost.Contracts;
using Bifrost.Endpoints;
using Bifrost.Infrastructure;
using Bifrost.WebSockets;

var builder = WebApplication.CreateBuilder(args);

// Servicios
builder.Services.AddSingleton<MessageDeduplicationService>();
builder.Services.AddSingleton<ConnectionRegistry>();
builder.Services.AddSingleton<IButtonEventRepository, SqlButtonEventRepository>();
builder.Services.AddSingleton<MessageProcessor>();
builder.Services.AddSingleton<WebSocketConnectionHandler>();
builder.Services.AddSingleton<DeviceCommandSender>();

var app = builder.Build();

// WebSockets
app.UseWebSockets();

// Health básico
app.MapGet("/", () => Results.Ok(new
{
    service = "IoT.GateWay",
    status = "running",
    timestampUtc = DateTime.UtcNow
}));

// Logging básico de requests
app.Use(async (context, next) =>
{
    app.Logger.LogInformation(
        "[REQ] {method} {path} | RemoteIp={remoteIp}",
        context.Request.Method,
        context.Request.Path,
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown");

    await next();
});

// WebSocket endpoint
app.Map("/ws", async context =>
{
    app.Logger.LogInformation(
        "[HTTP] /ws hit | RemoteIp={remoteIp} | IsWebSocket={isWebSocket}",
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        context.WebSockets.IsWebSocketRequest);

    foreach (var header in context.Request.Headers)
    {
        app.Logger.LogDebug(
            "[HTTP] Header | {key}: {value}",
            header.Key,
            header.Value.ToString());
    }

    if (!context.WebSockets.IsWebSocketRequest)
    {
        app.Logger.LogWarning(
            "[WS] Invalid upgrade request received on /ws | RemoteIp={remoteIp}",
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown");

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket request expected.");
        return;
    }

    app.Logger.LogInformation("[WS] Valid WebSocket upgrade request received.");

    var handler = context.RequestServices.GetRequiredService<WebSocketConnectionHandler>();
    await handler.HandleAsync(context);
});

// Consulta simple de dispositivos conectados
app.MapGet("/devices", (ConnectionRegistry registry) =>
{
    var devices = registry
        .GetAll()
        .Select(x => new
        {
            x.ConnectionId,
            x.DeviceId,
            x.RemoteIp,
            x.ConnectedAtUtc,
            WebSocketState = x.WebSocket?.State.ToString()
        });

    return Results.Ok(devices);
});

// Endpoints HTTP del dominio Device
app.MapDeviceEndpoints();

app.Run("http://0.0.0.0:5000");